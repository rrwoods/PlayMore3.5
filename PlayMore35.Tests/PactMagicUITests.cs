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
            character.classLevels.Add("Binder");
            var session = new CharacterSession(character, catalog);
            session.Recalculate();

            var host = new MockSubsystemUIHost(session);
            var provider = new PactMagicUI();

            var context = new ElementPickerContext { ElementType = "vestige" };
            var picker = provider.CreateElementPicker(session, context);
            Assert.IsNotNull(picker);
            Assert.IsInstanceOfType(picker, typeof(PactMagicElementPicker));
        }

        [TestMethod]
        public void PactMagicTabPage_LeftColumn_DisplaysDetailsPanelAndEmptyPromptInitially() {
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

                Assert.AreEqual(360, tabPage.SlotsColumnWidth);
                Assert.IsTrue(tabPage.IsEmptyPromptVisible, "Initial state should show empty prompt in details panel.");
            }
        }

        [TestMethod]
        public void PactMagicTabPage_SelectingCard_PopulatesLeftDetailsPanel() {
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

                Assert.IsFalse(tabPage.IsEmptyPromptVisible);
                Assert.IsTrue(tabPage.DetailsHeader.Contains("Amon"));
                Assert.IsTrue(tabPage.DetailsSignText.IndexOf("curling ram's horns", StringComparison.OrdinalIgnoreCase) >= 0);
                Assert.IsTrue(tabPage.DetailsInfluenceText.IndexOf("fire", StringComparison.OrdinalIgnoreCase) >= 0);
                Assert.IsTrue(tabPage.DetailsAbilitiesText.IndexOf("Fire Breath", StringComparison.OrdinalIgnoreCase) >= 0);
            }
        }

        [TestMethod]
        public void PactMagicTabPage_BindingAndPinnedRow_MovesBoundCardToTopRow() {
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
                Assert.IsFalse(picker.PinnedContainer.Visible, "Pinned row should be hidden when zero vestiges are bound.");

                // Select Amon
                var amonCard = picker.Cards.First(c => c.Vestige.Name == "Amon");
                picker.DeliverVestigeByName("Amon");
                Application.DoEvents();

                Assert.IsTrue(amonCard.GoodPactButton.Visible, "Selected unbound card must show Good Pact button.");
                Assert.IsTrue(amonCard.PoorPactButton.Visible, "Selected unbound card must show Poor Pact button.");

                // Click Good Pact button
                amonCard.GoodPactButton.PerformClick();
                Application.DoEvents();

                // Pinned container should now be visible and hold Amon card
                Assert.IsTrue(picker.PinnedContainer.Visible, "Pinned row must be visible when a vestige is bound.");
                Assert.AreEqual(1, picker.PinnedContainer.Controls.Count);
                Assert.AreSame(amonCard, picker.PinnedContainer.Controls[0]);
                Assert.IsTrue(amonCard.IsBound);
                Assert.IsTrue(amonCard.UnbindButton.Visible, "Bound card must show Unbind button.");
                Assert.IsFalse(amonCard.GoodPactButton.Visible);
                Assert.IsFalse(amonCard.PoorPactButton.Visible);

                // Unbound cards grid must NOT contain Amon
                Assert.IsFalse(picker.CardsContainer.Controls.Contains(amonCard));

                // Session engine must reflect binding
                var engine = session.Subsystems.Get("pact_magic") as SubsystemEngine;
                Assert.IsNotNull(engine);
                var slot0 = engine.GetSlot("vestiges", 0);
                Assert.IsNotNull(slot0);
                Assert.AreEqual("Amon", slot0.Element);
                Assert.AreEqual("good_pact", slot0.State);

                // Unbind Amon
                amonCard.UnbindButton.PerformClick();
                Application.DoEvents();

                Assert.IsFalse(picker.PinnedContainer.Visible, "Pinned container should be hidden once all vestiges unbind.");
                Assert.IsFalse(amonCard.IsBound);
                Assert.IsTrue(picker.CardsContainer.Controls.Contains(amonCard), "Amon must return to the cards grid.");
                var unslotted = engine.GetSlot("vestiges", 0);
                Assert.IsTrue(string.IsNullOrEmpty(unslotted?.Element) || unslotted?.State == "empty");
            }
        }

        [TestMethod]
        public void PactMagicTabPage_MaxCapacity_DisplaysAtMaxCapacityNotice() {
            var catalog = new GameRulesCatalog();
            var character = new Character { race = "Human" };
            character.classLevels.Add("Binder"); // Capacity = 1
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
                var amonCard = picker.Cards.First(c => c.Vestige.Name == "Amon");
                picker.DeliverVestigeByName("Amon");
                amonCard.GoodPactButton.PerformClick();
                Application.DoEvents();

                // Character is now at max capacity (1/1).
                // Select Aym (unbound)
                var aymCard = picker.Cards.First(c => c.Vestige.Name == "Aym");
                picker.DeliverVestigeByName("Aym");
                Application.DoEvents();

                Assert.IsTrue(aymCard.IsSelected);
                Assert.IsTrue(aymCard.CapacityNoticeLabel.Visible, "At max capacity, unbound card must display capacity notice.");
                Assert.IsTrue(aymCard.CapacityNoticeLabel.Text.IndexOf("max vestige capacity", StringComparison.OrdinalIgnoreCase) >= 0);
                Assert.IsFalse(aymCard.GoodPactButton.Visible, "Good pact button must not be visible when at max capacity.");
                Assert.IsFalse(aymCard.PoorPactButton.Visible, "Poor pact button must not be visible when at max capacity.");
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
                picker.SearchQuery = "curling ram's horns";
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
            var catalog = new GameRulesCatalog();
            var pactDef = catalog.GetSubsystem("pact_magic");
            Assert.IsNotNull(pactDef);
            var elem = pactDef.Elements["Aym"];
            var abilities = elem.GetEffects("abilities");
            var distinctAbilities = abilities.Select(a => a.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            Assert.AreEqual(6, distinctAbilities.Count);

            using (var card = new VestigeCardControl(elem)) {
                Assert.IsTrue(card.Height >= 210, $"Card height was {card.Height}, expected >= 210");
                var powersLabel = card.Controls.OfType<Label>().FirstOrDefault(l => l.Text.Contains("Dwarven Step"));
                Assert.IsNotNull(powersLabel);
                Assert.IsFalse(powersLabel.Text.Contains("more..."), "Abilities excerpt should not contain '+n more...'");
                foreach (var abilityName in distinctAbilities) {
                    Assert.IsTrue(powersLabel.Text.Contains(abilityName), $"Abilities text missing: {abilityName}");
                }
            }
        }

        [TestMethod]
        public void VestigeCardControl_PactState_DisplaysPactStateAndUnbindWhenBound() {
            var catalog = new GameRulesCatalog();
            var pactDef = catalog.GetSubsystem("pact_magic");
            Assert.IsNotNull(pactDef);
            var elem = pactDef.Elements["Amon"];

            using (var card = new VestigeCardControl(elem)) {
                card.IsBound = true;
                card.PactState = "poor_pact";

                Assert.AreEqual("Poor Pact", card.StatusLabel.Text);
                Assert.IsTrue(card.UnbindButton.Visible);
                Assert.IsFalse(card.GoodPactButton.Visible);
                Assert.IsFalse(card.PoorPactButton.Visible);

                card.PactState = "good_pact";
                Assert.AreEqual("Good Pact", card.StatusLabel.Text);
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
