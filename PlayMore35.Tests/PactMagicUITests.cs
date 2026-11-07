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

                Assert.AreEqual(360, tabPage.GetSlotsColumnWidth());
                Assert.IsTrue(tabPage.GetIsEmptyPromptVisible(), "Initial state should show empty prompt in details panel.");
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

                tabPage.GetPicker().DeliverVestigeByName("Amon");
                Application.DoEvents();

                Assert.IsFalse(tabPage.GetIsEmptyPromptVisible());
                Assert.IsTrue(tabPage.GetDetailsHeader().Contains("Amon"));
                Assert.IsTrue(tabPage.GetDetailsSignText().IndexOf("curling ram's horns", StringComparison.OrdinalIgnoreCase) >= 0);
                Assert.IsTrue(tabPage.GetDetailsInfluenceText().IndexOf("fire", StringComparison.OrdinalIgnoreCase) >= 0);
                Assert.IsTrue(tabPage.GetDetailsAbilitiesText().IndexOf("Fire Breath", StringComparison.OrdinalIgnoreCase) >= 0);
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

                var picker = tabPage.GetPicker();
                Assert.IsFalse(picker.GetPinnedContainer().Visible, "Pinned row should be hidden when zero vestiges are bound.");

                // Select Amon
                var amonCard = picker.GetCards().First(c => c.Vestige.Name == "Amon");
                picker.DeliverVestigeByName("Amon");
                Application.DoEvents();

                Assert.IsTrue(amonCard.GetGoodPactButton().Visible, "Selected unbound card must show Good Pact button.");
                Assert.IsTrue(amonCard.GetPoorPactButton().Visible, "Selected unbound card must show Poor Pact button.");

                // Click Good Pact button
                amonCard.GetGoodPactButton().PerformClick();
                Application.DoEvents();

                // Pinned container should now be visible and hold Amon card
                Assert.IsTrue(picker.GetPinnedContainer().Visible, "Pinned row must be visible when a vestige is bound.");
                Assert.AreEqual(1, picker.GetPinnedContainer().Controls.Count);
                Assert.AreSame(amonCard, picker.GetPinnedContainer().Controls[0]);
                Assert.IsTrue(amonCard.IsBound);
                Assert.IsTrue(amonCard.GetUnbindButton().Visible, "Bound card must show Unbind button.");
                Assert.IsFalse(amonCard.GetGoodPactButton().Visible);
                Assert.IsFalse(amonCard.GetPoorPactButton().Visible);

                // Unbound cards grid must NOT contain Amon
                Assert.IsFalse(picker.GetCardsContainer().Controls.Contains(amonCard));

                // Session engine must reflect binding
                var engine = session.GetSubsystem("pact_magic");
                Assert.IsNotNull(engine);
                var slot0 = engine.GetSlot("vestiges", 0);
                Assert.IsNotNull(slot0);
                Assert.AreEqual("Amon", slot0.Element);
                Assert.AreEqual("good_pact", slot0.State);

                // Unbind Amon
                amonCard.GetUnbindButton().PerformClick();
                Application.DoEvents();

                Assert.IsFalse(picker.GetPinnedContainer().Visible, "Pinned container should be hidden once all vestiges unbind.");
                Assert.IsFalse(amonCard.IsBound);
                Assert.IsTrue(picker.GetCardsContainer().Controls.Contains(amonCard), "Amon must return to the cards grid.");
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

                var picker = tabPage.GetPicker();
                var amonCard = picker.GetCards().First(c => c.Vestige.Name == "Amon");
                picker.DeliverVestigeByName("Amon");
                amonCard.GetGoodPactButton().PerformClick();
                Application.DoEvents();

                // Character is now at max capacity (1/1).
                // Select Aym (unbound)
                var aymCard = picker.GetCards().First(c => c.Vestige.Name == "Aym");
                picker.DeliverVestigeByName("Aym");
                Application.DoEvents();

                Assert.IsTrue(aymCard.IsSelected);
                Assert.IsTrue(aymCard.GetCapacityNoticeLabel().Visible, "At max capacity, unbound card must display capacity notice.");
                Assert.IsTrue(aymCard.GetCapacityNoticeLabel().Text.IndexOf("max vestige capacity", StringComparison.OrdinalIgnoreCase) >= 0);
                Assert.IsFalse(aymCard.GetGoodPactButton().Visible, "Good pact button must not be visible when at max capacity.");
                Assert.IsFalse(aymCard.GetPoorPactButton().Visible, "Poor pact button must not be visible when at max capacity.");
            }
        }

        [TestMethod]
        public void PactMagicElementPicker_CardGridAndFiltering_FiltersCardsInRealTime() {
            var catalog = new GameRulesCatalog();
            var character = new Character { race = "Human" };
            character.classLevels.Add("Binder");
            var session = new CharacterSession(character, catalog);
            session.Recalculate();

            using (var picker = new PactMagicElementPicker(session, new ElementPickerContext {
                ElementType = "vestige",
                WarningProvider = elem => session.ValidateProgressionChoice(
                    new ProgressionChoiceContext(ProgressionChoiceDomain.Subsystem, elem.Name, targetSource: "pact_magic"))
            })) {
                using (var form = new Form()) {
                    form.Controls.Add(picker);
                    form.Show();
                    Application.DoEvents();

                    Assert.IsTrue(picker.GetCards().Count > 0);
                    Assert.IsTrue(picker.GetHideUnavailable(), "Hide unavailable should be checked by default.");
                    // Level 1 Binder with HideUnavailable checked will only show Level 1 vestiges (5 vestiges)
                    int initialCount = picker.GetVisibleCardsCount();
                    Assert.AreEqual(5, initialCount);

                    picker.SetSearchQuery("Amon");
                    Application.DoEvents();

                    Assert.AreEqual(1, picker.GetVisibleCardsCount());
                    Assert.AreEqual("Amon", picker.GetCards().First(c => c.Visible).Vestige.Name);

                    picker.SetSearchQuery("");
                    Application.DoEvents();
                    Assert.AreEqual(initialCount, picker.GetVisibleCardsCount());

                    // Unchecking hide unavailable shows all 9 vestiges
                    picker.SetHideUnavailable(false);
                    Application.DoEvents();
                    Assert.IsTrue(picker.GetVisibleCardsCount() >= 9);
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

            using (var picker = new PactMagicElementPicker(session, new ElementPickerContext {
                ElementType = "vestige",
                WarningProvider = elem => session.ValidateProgressionChoice(
                    new ProgressionChoiceContext(ProgressionChoiceDomain.Subsystem, elem.Name, targetSource: "pact_magic"))
            })) {
                picker.SetHideUnavailable(false); // allow searching all vestiges

                // Search by Sign keyword
                picker.SetSearchQuery("curling ram's horns");
                Assert.AreEqual(1, picker.GetVisibleCardsCount());
                Assert.AreEqual("Amon", picker.GetCards().First(c => c.Visible).Vestige.Name);

                // Search by Influence keyword
                picker.SetSearchQuery("dwarf");
                Assert.AreEqual(1, picker.GetVisibleCardsCount());
                Assert.AreEqual("Aym", picker.GetCards().First(c => c.Visible).Vestige.Name);

                // Search by Ability descriptive text keyword
                picker.SetSearchQuery("fire");
                Assert.IsTrue(picker.GetVisibleCardsCount() > 1);
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

                Assert.AreEqual("Poor Pact", card.GetStatusLabel().Text);
                Assert.IsTrue(card.GetUnbindButton().Visible);
                Assert.IsFalse(card.GetGoodPactButton().Visible);
                Assert.IsFalse(card.GetPoorPactButton().Visible);

                card.PactState = "good_pact";
                Assert.AreEqual("Good Pact", card.GetStatusLabel().Text);
            }
        }

        [TestMethod]
        public void VestigeCardControl_CardStatusVisuals_UnmetLevelGate_DisplaysExceedsDarkGoldenrodAndTooltip() {
            var catalog = new GameRulesCatalog();
            var pactDef = catalog.GetSubsystem("pact_magic");
            Assert.IsNotNull(pactDef);
            var elem = pactDef.Elements["Amon"];

            using (var card = new VestigeCardControl(elem)) {
                card.Warnings = new[] {
                    new ValidationWarning("Level 2 exceeds maximum vestige level 1.", ValidationCategory.LevelGate)
                };

                Assert.AreEqual("⚠ Exceeds", card.GetStatusLabel().Text);
                Assert.AreEqual(Color.DarkGoldenrod, card.GetStatusLabel().ForeColor);
                Assert.AreEqual("Level 2 exceeds maximum vestige level 1.", card.GetStatusToolTip());
                Assert.IsTrue(card.IsInvalid);
            }
        }

        [TestMethod]
        public void VestigeCardControl_CardStatusVisuals_UnmetOtherCategory_DisplaysIneligibleDarkGoldenrodAndTooltip() {
            var catalog = new GameRulesCatalog();
            var pactDef = catalog.GetSubsystem("pact_magic");
            Assert.IsNotNull(pactDef);
            var elem = pactDef.Elements["Amon"];

            using (var card = new VestigeCardControl(elem)) {
                card.Warnings = new[] {
                    new ValidationWarning("Requires Bind Vestige feat.", ValidationCategory.Prerequisite)
                };

                Assert.AreEqual("⚠ Ineligible", card.GetStatusLabel().Text);
                Assert.AreEqual(Color.DarkGoldenrod, card.GetStatusLabel().ForeColor);
                Assert.AreEqual("Requires Bind Vestige feat.", card.GetStatusToolTip());
                Assert.IsTrue(card.IsInvalid);
            }
        }

        [TestMethod]
        public void VestigeCardControl_CardStatusVisuals_ItemDependent_DisplaysItemDepDodgerBlueAndTooltip() {
            var catalog = new GameRulesCatalog();
            var pactDef = catalog.GetSubsystem("pact_magic");
            Assert.IsNotNull(pactDef);
            var elem = pactDef.Elements["Amon"];

            using (var card = new VestigeCardControl(elem)) {
                card.Warnings = new[] {
                    new ValidationWarning("Requires Str 13", WarningKind.ItemDependent, ValidationCategory.Prerequisite)
                };

                Assert.AreEqual("🛈 Item-Dep", card.GetStatusLabel().Text);
                Assert.AreEqual(Color.DodgerBlue, card.GetStatusLabel().ForeColor);
                Assert.AreEqual("Requires Str 13\n(Satisfied by currently equipped items)", card.GetStatusToolTip());
                Assert.IsFalse(card.IsInvalid);
            }
        }

        [TestMethod]
        public void VestigeCardControl_CardStatusVisuals_NoWarnings_DisplaysAvailableAndGreen() {
            var catalog = new GameRulesCatalog();
            var pactDef = catalog.GetSubsystem("pact_magic");
            Assert.IsNotNull(pactDef);
            var elem = pactDef.Elements["Amon"];

            using (var card = new VestigeCardControl(elem)) {
                card.Warnings = Array.Empty<ValidationWarning>();

                Assert.AreEqual("Available", card.GetStatusLabel().Text);
                Assert.AreEqual(Color.FromArgb(46, 125, 50), card.GetStatusLabel().ForeColor);
                Assert.AreEqual(string.Empty, card.GetStatusToolTip());
                Assert.IsFalse(card.IsInvalid);
            }
        }

        [TestMethod]
        public void VestigeCardControl_IsInvalid_TrueOnlyWhenUnmetWarningsPresent() {
            var catalog = new GameRulesCatalog();
            var pactDef = catalog.GetSubsystem("pact_magic");
            Assert.IsNotNull(pactDef);
            var elem = pactDef.Elements["Amon"];

            using (var card = new VestigeCardControl(elem)) {
                Assert.IsFalse(card.IsInvalid);

                card.Warnings = new[] {
                    new ValidationWarning("Requires item", WarningKind.ItemDependent, ValidationCategory.Prerequisite)
                };
                Assert.IsFalse(card.IsInvalid);

                card.Warnings = new[] {
                    new ValidationWarning("Exceeds level gate", WarningKind.Unmet, ValidationCategory.LevelGate)
                };
                Assert.IsTrue(card.IsInvalid);

                card.Warnings = new[] {
                    new ValidationWarning("Exceeds level gate", WarningKind.Unmet, ValidationCategory.LevelGate),
                    new ValidationWarning("Requires item", WarningKind.ItemDependent, ValidationCategory.Prerequisite)
                };
                Assert.IsTrue(card.IsInvalid);

                card.Warnings = Array.Empty<ValidationWarning>();
                Assert.IsFalse(card.IsInvalid);
            }
        }

        [TestMethod]
        public void PactMagicElementPicker_Filtering_HideUnavailable_HidesOnlyCardsWithUnmetWarnings() {
            var catalog = new GameRulesCatalog();
            var character = new Character { race = "Human" };
            character.classLevels.Add("Binder");
            var session = new CharacterSession(character, catalog);
            session.Recalculate();

            var warningMap = new Dictionary<string, IReadOnlyList<ValidationWarning>>(StringComparer.OrdinalIgnoreCase) {
                ["Amon"] = Array.Empty<ValidationWarning>(),
                ["Aym"] = new[] { new ValidationWarning("Level 2 exceeds max 1", ValidationCategory.LevelGate) },
                ["Leraje"] = new[] { new ValidationWarning("Requires Point Blank Shot", WarningKind.ItemDependent, ValidationCategory.Prerequisite) }
            };

            var context = new ElementPickerContext {
                ElementType = "vestige",
                WarningProvider = elem => warningMap.TryGetValue(elem.Name, out var list) ? list : Array.Empty<ValidationWarning>()
            };

            using (var picker = new PactMagicElementPicker(session, context)) {
                using (var form = new Form()) {
                    form.Controls.Add(picker);
                    form.Show();
                    Application.DoEvents();

                    var amonCard = picker.GetCards().First(c => c.Vestige.Name == "Amon");
                    var aymCard = picker.GetCards().First(c => c.Vestige.Name == "Aym");
                    var lerajeCard = picker.GetCards().First(c => c.Vestige.Name == "Leraje");

                    // HideUnavailable is true by default
                    Assert.IsTrue(picker.GetHideUnavailable());
                    Assert.IsTrue(amonCard.Visible, "Available card with 0 warnings must be visible.");
                    Assert.IsTrue(lerajeCard.Visible, "Card with ONLY item-dependent warnings must remain visible when HideUnavailable is checked.");
                    Assert.IsFalse(aymCard.Visible, "Card with Unmet warnings must be hidden when HideUnavailable is checked.");

                    // Unchecking HideUnavailable should make unmet card visible
                    picker.SetHideUnavailable(false);
                    Application.DoEvents();
                    Assert.IsTrue(aymCard.Visible, "Card with Unmet warnings becomes visible when HideUnavailable is unchecked.");
                }
            }
        }

        [TestMethod]
        public void PactMagicElementPicker_ProgressionMode_SuppressesButtonsAndPinnedContainer() {
            var catalog = new GameRulesCatalog();
            var character = new Character { race = "human" };
            character.classLevels.Clear();
            character.classLevels.Add("Binder");
            var session = new CharacterSession(character, catalog);
            session.Recalculate();

            // Bind Amon in daily pact session
            session.PerformAction(new SheetAction {
                Name = "Bind Amon slot 0",
                Subsystem = "pact_magic",
                SlotGroup = "vestiges",
                SlotIndex = 0,
                Element = "Amon",
                Verb = "bind_good_pact"
            });

            // AcquisitionLevel = 1 signals progression choice mode (e.g., LevelDetailsDialog)
            var context = new ElementPickerContext {
                ElementType = "vestige",
                AcquisitionLevel = 1,
                WarningProvider = elem => session.ValidateProgressionChoice(
                    new ProgressionChoiceContext(ProgressionChoiceDomain.Subsystem, elem.Name, acquisitionLevel: 1, targetSource: "Binder"))
            };

            using (var picker = new PactMagicElementPicker(session, context)) {
                using (var form = new Form()) {
                    form.Controls.Add(picker);
                    form.Show();
                    Application.DoEvents();

                    // Pinned container must be hidden in progression mode even though Amon is bound in session
                    Assert.IsFalse(picker.GetPinnedContainer().Visible, "Pinned container must be hidden when AcquisitionLevel > 0.");
                    Assert.AreEqual(0, picker.GetPinnedContainer().Controls.Count);

                    var amonCard = picker.GetCards().First(c => c.Vestige.Name == "Amon");
                    Assert.IsTrue(picker.GetCardsContainer().Controls.Contains(amonCard), "Amon must be hosted in CardsContainer, not pinned.");
                    Assert.IsFalse(amonCard.IsBound, "Amon must not be marked as IsBound in progression mode.");

                    // Daily pact buttons must be suppressed (hidden and disabled)
                    Assert.IsFalse(amonCard.GetGoodPactButton().Visible);
                    Assert.IsFalse(amonCard.GetGoodPactButton().Enabled);
                    Assert.IsFalse(amonCard.GetPoorPactButton().Visible);
                    Assert.IsFalse(amonCard.GetPoorPactButton().Enabled);
                    Assert.IsFalse(amonCard.GetUnbindButton().Visible);
                    Assert.IsFalse(amonCard.GetUnbindButton().Enabled);
                    Assert.IsFalse(amonCard.GetCapacityNoticeLabel().Visible);

                    // Selecting Amon delivers it via ElementDelivered without showing daily pact buttons
                    SubsystemElement? deliveredElement = null;
                    picker.ElementDelivered += elem => deliveredElement = elem;

                    picker.DeliverVestigeByName("Amon");
                    Application.DoEvents();

                    Assert.IsNotNull(deliveredElement);
                    Assert.AreEqual("Amon", deliveredElement.Name);
                    Assert.IsTrue(amonCard.IsSelected);
                    Assert.IsFalse(amonCard.GetGoodPactButton().Visible, "Good Pact button must remain suppressed when selected in progression mode.");
                    Assert.IsFalse(amonCard.GetPoorPactButton().Visible, "Poor Pact button must remain suppressed when selected in progression mode.");
                    Assert.IsFalse(amonCard.GetUnbindButton().Visible, "Unbind button must remain suppressed in progression mode.");
                }
            }
        }

        [TestMethod]
        public void PactMagicTabPage_Picker_ConfiguredWithWarningProvider_DelegatesValidation() {
            var catalog = new GameRulesCatalog();
            var character = new Character { race = "Human" };
            character.classLevels.Add("Binder"); // Binder 1 -> max vestige level = 1
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

                var amonCard = tabPage.GetPicker().GetCards().First(c => c.Vestige.Name == "Amon"); // Level 1
                var savnokCard = tabPage.GetPicker().GetCards().First(c => c.Vestige.Name == "Savnok"); // Level 2

                Assert.IsFalse(amonCard.IsInvalid, "Level 1 vestige is eligible for Level 1 Binder.");
                Assert.AreEqual("Available", amonCard.GetStatusLabel().Text);

                Assert.IsTrue(savnokCard.IsInvalid, "Level 2 vestige exceeds max vestige level for Level 1 Binder.");
                Assert.AreEqual("⚠ Exceeds", savnokCard.GetStatusLabel().Text);
                Assert.AreEqual(Color.DarkGoldenrod, savnokCard.GetStatusLabel().ForeColor);
                Assert.IsTrue(savnokCard.GetStatusToolTip().Contains("exceeds maximum"), $"Expected tooltip to mention exceeds maximum, got: {savnokCard.GetStatusToolTip()}");
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
