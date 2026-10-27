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
        public void PactMagicTabPage_SymptomsDashboard_RendersSignAndPowers() {
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

                tabPage.Picker.DeliverVestigeByName("Amon");
                Application.DoEvents();

                Assert.IsTrue(slot0.SymptomsPanel.Visible);
                Assert.IsTrue(slot0.SymptomsSignText.IndexOf("Ram horns", StringComparison.OrdinalIgnoreCase) >= 0);
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
                    int initialCount = picker.VisibleCardsCount;
                    Assert.IsTrue(initialCount >= 9);

                    picker.SearchQuery = "Amon";
                    Application.DoEvents();

                    Assert.AreEqual(1, picker.VisibleCardsCount);
                    Assert.AreEqual("Amon", picker.Cards.First(c => c.Visible).Vestige.Name);

                    picker.SearchQuery = "";
                    Application.DoEvents();
                    Assert.AreEqual(initialCount, picker.VisibleCardsCount);
                }
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
