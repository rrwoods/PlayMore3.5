using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlayMore35.CharacterChoices;
using PlayMore35.Global;
using PlayMore35.Rules;
using PlayMore35.Subsystems;
using PlayMore35.UI.Subsystems;
using PlayMore35.UI.Subsystems.PactMagic;

namespace PlayMore35.Tests {
    [TestClass]
    public class PactMagicUITests {
        [TestMethod]
        public void PactMagicUI_Registration_RegistersPactMagicAndVestigeAlias() {
            var registry = SubsystemUIRegistry.CreateDefault();
            var provider = registry.GetBySubsystemId("pact_magic");
            Assert.IsNotNull(provider);
            Assert.IsInstanceOfType(provider, typeof(PactMagicUI));

            var providerByAlias = registry.GetByElementType("vestige");
            Assert.AreSame(provider, providerByAlias);
        }

        [TestMethod]
        public void PactMagicUI_IsActive_TrueOnlyWhenVestigeCapacityOrBoundVestigesExist() {
            var catalog = new GameRulesCatalog();
            var character = new Character { race = "Human" };
            var session = new CharacterSession(character, catalog);
            var provider = new PactMagicUI();

            // Untrained character: no capacity, no vestiges bound
            Assert.IsFalse(provider.IsActive(session));

            // Binder level 1: capacity = 1
            character.classLevels.Add("Binder");
            session.Recalculate();
            Assert.IsTrue(provider.IsActive(session));
        }

        [TestMethod]
        public void PactMagicUI_CreateDedicatedTab_ReturnsPactMagicTabPage() {
            var catalog = new GameRulesCatalog();
            var character = new Character { race = "Human" };
            character.classLevels.Add("Binder");
            var session = new CharacterSession(character, catalog);
            session.Recalculate();

            var host = new MockSubsystemUIHost(session);
            var provider = new PactMagicUI();

            var tab = provider.CreateDedicatedTab(host);
            Assert.IsNotNull(tab);
            Assert.IsInstanceOfType(tab, typeof(PactMagicTabPage));
        }

        [TestMethod]
        public void PactMagicUI_CreateElementPicker_ReturnsPactMagicElementPicker() {
            var catalog = new GameRulesCatalog();
            var character = new Character { race = "Human" };
            var session = new CharacterSession(character, catalog);
            var provider = new PactMagicUI();

            var context = new ElementPickerContext { ElementType = "vestige" };
            var picker = provider.CreateElementPicker(session, context);
            Assert.IsNotNull(picker);
            Assert.IsInstanceOfType(picker, typeof(PactMagicElementPicker));
        }

        [TestMethod]
        public void PactMagicTabPage_SlotColumnScaling_DisplaysCorrectNumberOfSlots() {
            var catalog = new GameRulesCatalog();
            var character = new Character { race = "Human" };
            for (int i = 0; i < 8; i++) character.classLevels.Add("Binder"); // Level 8 Binder has 2 slots
            var session = new CharacterSession(character, catalog);
            session.Recalculate();

            var host = new MockSubsystemUIHost(session);
            using (var form = new Form())
            using (var tabControl = new TabControl()) {
                var tabPage = new PactMagicTabPage(host);
                tabControl.TabPages.Add(tabPage);
                form.Controls.Add(tabControl);
                form.Show();
                Application.DoEvents();

                Assert.AreEqual(2, tabPage.SlotCards.Count);
                Assert.AreEqual(360, tabPage.SlotsColumnWidth);
            }
        }

        [TestMethod]
        public void PactMagicTabPage_SlotSelection_TargetsPickerAndUpdatesActiveSlot() {
            var catalog = new GameRulesCatalog();
            var character = new Character { race = "Human" };
            for (int i = 0; i < 8; i++) character.classLevels.Add("Binder");
            var session = new CharacterSession(character, catalog);
            session.Recalculate();

            var host = new MockSubsystemUIHost(session);
            using (var form = new Form())
            using (var tabControl = new TabControl()) {
                var tabPage = new PactMagicTabPage(host);
                tabControl.TabPages.Add(tabPage);
                form.Controls.Add(tabControl);
                form.Show();
                Application.DoEvents();

                Assert.AreEqual(0, tabPage.ActiveSlotIndex);
                Assert.AreEqual(ReceptorState.ActiveSelecting, tabPage.SlotCards[0].Receptor.State);
                Assert.AreEqual(ReceptorState.Unmade, tabPage.SlotCards[1].Receptor.State);

                // Click slot 1
                tabPage.SlotCards[1].Receptor.PerformClick();
                Application.DoEvents();

                Assert.AreEqual(1, tabPage.ActiveSlotIndex);
                Assert.AreEqual(ReceptorState.Unmade, tabPage.SlotCards[0].Receptor.State);
                Assert.AreEqual(ReceptorState.ActiveSelecting, tabPage.SlotCards[1].Receptor.State);
            }
        }

        [TestMethod]
        public void PactMagicTabPage_VestigeDelivery_UpdatesSlotReceptorAndBindsInSession() {
            var catalog = new GameRulesCatalog();
            var character = new Character { race = "Human" };
            character.classLevels.Add("Binder");
            var session = new CharacterSession(character, catalog);
            session.Recalculate();

            var host = new MockSubsystemUIHost(session);
            using (var form = new Form())
            using (var tabControl = new TabControl()) {
                var tabPage = new PactMagicTabPage(host);
                tabControl.TabPages.Add(tabPage);
                form.Controls.Add(tabControl);
                form.Show();
                Application.DoEvents();

                var picker = tabPage.Picker;
                picker.DeliverVestigeByName("Amon");
                Application.DoEvents();

                var slot0 = tabPage.SlotCards[0];
                Assert.AreEqual(ReceptorState.ActiveSelecting, slot0.Receptor.State);
                Assert.IsNotNull(slot0.Receptor.SelectedElement);
                Assert.AreEqual("Amon", slot0.Receptor.SelectedElement?.Name);

                var engine = session.Subsystems.Get("pact_magic") as SubsystemEngine;
                Assert.IsNotNull(engine);
                var assignedSlot = engine.GetSlot("vestiges", 0);
                Assert.IsNotNull(assignedSlot);
                Assert.AreEqual("Amon", assignedSlot.Element);
                Assert.AreEqual("good_pact", assignedSlot.State);
            }
        }

        [TestMethod]
        public void PactMagicTabPage_GoodPoorRadioButtons_ReactiveWithoutModalPrompts() {
            var catalog = new GameRulesCatalog();
            var character = new Character { race = "Human" };
            character.classLevels.Add("Binder");
            var session = new CharacterSession(character, catalog);
            session.Recalculate();

            var host = new MockSubsystemUIHost(session);
            using (var form = new Form())
            using (var tabControl = new TabControl()) {
                var tabPage = new PactMagicTabPage(host);
                tabControl.TabPages.Add(tabPage);
                form.Controls.Add(tabControl);
                form.Show();
                Application.DoEvents();

                var slot0 = tabPage.SlotCards[0];
                // Unbound: radios disabled
                Assert.IsFalse(slot0.GoodPactRadio.Enabled);
                Assert.IsFalse(slot0.PoorPactRadio.Enabled);

                // Deliver vestige
                tabPage.Picker.DeliverVestigeByName("Amon");
                Application.DoEvents();

                Assert.IsTrue(slot0.GoodPactRadio.Enabled);
                Assert.IsTrue(slot0.PoorPactRadio.Enabled);
                Assert.IsTrue(slot0.GoodPactRadio.Checked);
                Assert.IsFalse(slot0.PoorPactRadio.Checked);

                // Toggle Poor pact
                slot0.PoorPactRadio.Checked = true;
                Application.DoEvents();

                var engine = session.Subsystems.Get("pact_magic") as SubsystemEngine;
                Assert.IsNotNull(engine);
                Assert.AreEqual("poor_pact", engine.GetSlot("vestiges", 0)?.State);

                // Toggle Good pact back
                slot0.GoodPactRadio.Checked = true;
                Application.DoEvents();
                Assert.AreEqual("good_pact", engine.GetSlot("vestiges", 0)?.State);
            }
        }

        [TestMethod]
        public void PactMagicTabPage_UnbindingViaClear_ResetsSlotAndReceptor() {
            var catalog = new GameRulesCatalog();
            var character = new Character { race = "Human" };
            character.classLevels.Add("Binder");
            var session = new CharacterSession(character, catalog);
            session.Recalculate();

            var host = new MockSubsystemUIHost(session);
            using (var form = new Form())
            using (var tabControl = new TabControl()) {
                var tabPage = new PactMagicTabPage(host);
                tabControl.TabPages.Add(tabPage);
                form.Controls.Add(tabControl);
                form.Show();
                Application.DoEvents();

                tabPage.Picker.DeliverVestigeByName("Amon");
                Application.DoEvents();

                Assert.IsTrue(host.TimelineRefreshRequested, "Binding a vestige must request a timeline refresh.");

                var slot0 = tabPage.SlotCards[0];
                Assert.IsNotNull(slot0.Receptor.SelectedElement);

                // Click unbind
                slot0.Receptor.PerformClear();
                Application.DoEvents();

                Assert.IsNull(slot0.Receptor.SelectedElement);
                Assert.IsFalse(slot0.GoodPactRadio.Enabled);
                Assert.IsFalse(slot0.PoorPactRadio.Enabled);

                var engine = session.Subsystems.Get("pact_magic") as SubsystemEngine;
                Assert.IsNotNull(engine);
                var slot = engine.GetSlot("vestiges", 0);
                Assert.IsTrue(string.IsNullOrEmpty(slot?.Element) || slot?.State == "empty");
            }
        }

        [TestMethod]
        public void PactMagicTabPage_SymptomsDashboard_RendersSignInfluenceAndPowers() {
            var catalog = new GameRulesCatalog();
            var character = new Character { race = "Human" };
            character.classLevels.Add("Binder");
            var session = new CharacterSession(character, catalog);
            session.Recalculate();

            var host = new MockSubsystemUIHost(session);
            using (var form = new Form())
            using (var tabControl = new TabControl()) {
                var tabPage = new PactMagicTabPage(host);
                tabControl.TabPages.Add(tabPage);
                form.Controls.Add(tabControl);
                form.Show();
                Application.DoEvents();

                var slot0 = tabPage.SlotCards[0];
                Assert.IsFalse(slot0.SymptomsPanel.Visible);
                Assert.IsTrue(slot0.Height >= 240, $"Slot card height was {slot0.Height}, expected >= 240");

                tabPage.Picker.DeliverVestigeByName("Amon");
                Application.DoEvents();

                Assert.IsTrue(slot0.SymptomsPanel.Visible);
                Assert.IsTrue(slot0.SymptomsSignText.IndexOf("Ram horns", StringComparison.OrdinalIgnoreCase) >= 0);
                Assert.IsTrue(slot0.SymptomsInfluenceText.IndexOf("fire", StringComparison.OrdinalIgnoreCase) >= 0);
                Assert.IsTrue(slot0.SymptomsPowersText.IndexOf("Fire Breath", StringComparison.OrdinalIgnoreCase) >= 0);
            }
        }

        [TestMethod]
        public void PactMagicElementPicker_CardGridAndFiltering_FiltersCardsInRealTime() {
            var catalog = new GameRulesCatalog();
            var character = new Character { race = "Human" };
            character.classLevels.Add("Binder");
            var session = new CharacterSession(character, catalog);
            session.Recalculate();

            using (var picker = new PactMagicElementPicker(session, new ElementPickerContext { ElementType = "vestige" })) {
                using (var form = new Form()) {
                    form.Controls.Add(picker);
                    form.Show();
                    Application.DoEvents();

                    Assert.IsTrue(picker.Cards.Count > 0);
                    Assert.IsTrue(picker.HideUnavailable, "Hide unavailable should be checked by default.");
                    // Level 1 Binder with HideUnavailable checked will only show Level 1 vestiges (5 vestiges)
                    int initialCount = picker.VisibleCardsCount;
                    Assert.AreEqual(5, initialCount);

                    picker.SearchQuery = "Amon";
                    Application.DoEvents();

                    Assert.AreEqual(1, picker.VisibleCardsCount);
                    Assert.AreEqual("Amon", picker.Cards.First(c => c.Visible).Vestige.Name);

                    picker.SearchQuery = "";
                    Application.DoEvents();
                    Assert.AreEqual(initialCount, picker.VisibleCardsCount);

                    // Unchecking hide unavailable shows all 9 vestiges
                    picker.HideUnavailable = false;
                    Application.DoEvents();
                    Assert.IsTrue(picker.VisibleCardsCount >= 9);
                }
            }
        }

        [TestMethod]
        public void PactMagicElementPicker_Search_MatchesAbilitiesInfluenceAndSigns() {
            var catalog = new GameRulesCatalog();
            var character = new Character { race = "Human" };
            character.classLevels.Add("Binder");
            var session = new CharacterSession(character, catalog);
            session.Recalculate();

            using (var picker = new PactMagicElementPicker(session, new ElementPickerContext { ElementType = "vestige" })) {
                picker.HideUnavailable = false; // allow searching all vestiges

                // Search by Sign keyword
                picker.SearchQuery = "ram horns";
                Assert.AreEqual(1, picker.VisibleCardsCount);
                Assert.AreEqual("Amon", picker.Cards.First(c => c.Visible).Vestige.Name);

                // Search by Influence keyword
                picker.SearchQuery = "dwarf";
                Assert.AreEqual(1, picker.VisibleCardsCount);
                Assert.AreEqual("Aym", picker.Cards.First(c => c.Visible).Vestige.Name);

                // Search by Ability descriptive text keyword
                picker.SearchQuery = "fire";
                Assert.IsTrue(picker.VisibleCardsCount > 1);
            }
        }

        [TestMethod]
        public void VestigeCardControl_CardHeightAndAbilities_ShowsAllAbilitiesWithoutMoreEllipsis() {
            var elem = new SubsystemElement { Name = "Aym" };
            var rules = VestigeRulesRepository.GetRulesData(elem);
            Assert.AreEqual(6, rules.Abilities.Count);

            using (var card = new VestigeCardControl(elem, rules)) {
                Assert.IsTrue(card.Height >= 210, $"Card height was {card.Height}, expected >= 210");
                var powersLabel = card.Controls.OfType<Label>().FirstOrDefault(l => l.Text.Contains("Dwarven Step"));
                Assert.IsNotNull(powersLabel);
                Assert.IsFalse(powersLabel.Text.Contains("more..."), "Abilities excerpt should not contain '+n more...'");
                foreach (var ability in rules.Abilities) {
                    Assert.IsTrue(powersLabel.Text.Contains(ability.Name), $"Abilities text missing: {ability.Name}");
                }
            }
        }

        [TestMethod]
        public void VestigeCardControl_BindingIndicator_DisplaysBoundWithoutSlotNumbers() {
            var catalog = new GameRulesCatalog();
            var character = new Character { race = "Human" };
            character.classLevels.Add("Binder");
            var session = new CharacterSession(character, catalog);
            session.Recalculate();

            var host = new MockSubsystemUIHost(session);
            using (var tab = new PactMagicTabPage(host))
            using (var tabControl = new TabControl())
            using (var form = new Form()) {
                tabControl.TabPages.Add(tab);
                form.Controls.Add(tabControl);
                form.Show();
                Application.DoEvents();

                tab.Picker.DeliverVestigeByName("Amon");
                Application.DoEvents();

                var amonCard = tab.Picker.Cards.First(c => c.Vestige.Name == "Amon");
                var footer = amonCard.Controls.OfType<Label>().FirstOrDefault(l => l.Dock == DockStyle.Bottom);
                Assert.IsNotNull(footer);
                Assert.IsTrue(footer.Text.Contains("Bound"), $"Footer text expected 'Bound', got: {footer.Text}");
                Assert.IsFalse(footer.Text.Contains("Slot"), $"Footer text should not contain 'Slot', got: {footer.Text}");
            }
        }

        [TestMethod]
        public void VestigeSlotCard_WarningBadge_ShowsWarningWhenSlottedVestigeExceedsMaxLevel() {
            using (var card = new VestigeSlotCard(0)) {
                Assert.IsFalse(card.WarningBadgeVisible);

                // Slotted vestige within max level (Amon is level 1, max level 1)
                var amon = new SubsystemElement { Name = "Amon" };
                card.SetBoundVestige(amon, "good_pact", maxVestigeLevel: 1);
                Assert.IsFalse(card.WarningBadgeVisible);

                // Slotted vestige exceeding max level (Dahlver-Nar is level 2, max level 1)
                var dahlver = new SubsystemElement { Name = "Dahlver-Nar" };
                card.SetBoundVestige(dahlver, "good_pact", maxVestigeLevel: 1);
                Assert.IsTrue(card.WarningBadgeVisible);
                Assert.IsTrue(card.WarningMessage?.Contains("exceeds") == true);

                // Unbind clears warning
                card.SetBoundVestige(null, "good_pact", maxVestigeLevel: 1);
                Assert.IsFalse(card.WarningBadgeVisible);
            }
        }

        [TestMethod]
        public void VestigeSlotCard_Header_MovesStatusToTopLeftAndEliminatesSlotNumber() {
            using (var card = new VestigeSlotCard(0)) {
                var pnlHeader = card.Controls.OfType<Panel>().First(p => p.Dock == DockStyle.Top && p.Height <= 25);
                var statusLabel = pnlHeader.Controls.OfType<Label>().FirstOrDefault(l => l.Text == "Empty");
                Assert.IsNotNull(statusLabel);
                Assert.AreEqual(DockStyle.Left, statusLabel.Dock);

                // Header should NOT contain a label saying "Slot 1"
                var slotLabel = pnlHeader.Controls.OfType<Label>().FirstOrDefault(l => l.Text.StartsWith("Slot "));
                Assert.IsNull(slotLabel, "Card header must not contain 'Slot n' label");
            }
        }

        [TestMethod]
        public void PactMagicTabPage_LeftColumn_EliminatesVestigeSlotsHeader() {
            var catalog = new GameRulesCatalog();
            var character = new Character { race = "Human" };
            character.classLevels.Add("Binder");
            var session = new CharacterSession(character, catalog);
            session.Recalculate();

            var host = new MockSubsystemUIHost(session);
            using (var tab = new PactMagicTabPage(host)) {
                var leftCol = tab.Controls.OfType<Panel>().First(p => p.Dock == DockStyle.Left);
                var headerPanel = leftCol.Controls.OfType<Panel>().FirstOrDefault(p => p.Controls.OfType<Label>().Any(l => l.Text == "Vestige Slots"));
                Assert.IsNull(headerPanel, "Left column must not contain 'Vestige Slots' header panel");
            }
        }

        [TestMethod]
        public void PactMagicElementPicker_LowerDetailsPanel_DisplaysRulesTextAndAbilities() {
            var catalog = new GameRulesCatalog();
            var character = new Character { race = "Human" };
            character.classLevels.Add("Binder");
            var session = new CharacterSession(character, catalog);
            session.Recalculate();

            using (var picker = new PactMagicElementPicker(session, new ElementPickerContext { ElementType = "vestige" })) {
                using (var form = new Form()) {
                    form.Controls.Add(picker);
                    form.Show();
                    Application.DoEvents();

                    picker.SelectVestigeDetail("Amon");
                    Application.DoEvents();

                    Assert.IsTrue(picker.DetailsHeader.Contains("Amon"));
                    Assert.IsTrue(picker.DetailsSignText.Contains("Ram horns"));
                    Assert.IsTrue(picker.DetailsInfluenceText.Length > 0);
                    Assert.IsTrue(picker.DetailsAbilitiesText.Contains("Fire Breath"));
                }
            }
        }

        private class MockSubsystemUIHost : ISubsystemUIHost {
            public ICharacterSession Session { get; }
            public bool RecalculateRequested { get; private set; }
            public bool EffectsRefreshRequested { get; private set; }
            public bool TimelineRefreshRequested { get; private set; }

            public MockSubsystemUIHost(ICharacterSession session) => Session = session;
            public void RequestRecalculate() => RecalculateRequested = true;
            public void RequestEffectsRefresh() => EffectsRefreshRequested = true;
            public void RequestTimelineRefresh() => TimelineRefreshRequested = true;
        }
    }
}
