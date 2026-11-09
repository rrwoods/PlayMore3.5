using System;
using System.Collections.Generic;
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
                var picker = new PactMagicElementPicker(session, new ElementPickerContext { ElementType = "vestige" });
                var tabPage = new PactMagicTabPage(host, picker);
                tabControl.TabPages.Add(tabPage);
                form.Controls.Add(tabControl);
                form.Show();
                Application.DoEvents();

                Assert.IsNull(picker.ViewModel.SelectedCard, "Initial state should have no selected card in view model.");
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
                var picker = new PactMagicElementPicker(session, new ElementPickerContext { ElementType = "vestige" });
                var tabPage = new PactMagicTabPage(host, picker);
                tabControl.TabPages.Add(tabPage);
                form.Controls.Add(tabControl);
                form.Show();
                Application.DoEvents();

                picker.SelectVestige("Amon");
                Application.DoEvents();

                var selected = picker.ViewModel.SelectedCard;
                Assert.IsNotNull(selected);
                Assert.IsTrue(selected.DetailsTitle.Contains("Amon"));
                Assert.IsTrue(selected.DetailsSignText.IndexOf("curling ram's horns", StringComparison.OrdinalIgnoreCase) >= 0);
                Assert.IsTrue(selected.DetailsInfluenceText.IndexOf("fire", StringComparison.OrdinalIgnoreCase) >= 0);
                Assert.IsTrue(selected.DetailsAbilitiesText.IndexOf("Fire Breath", StringComparison.OrdinalIgnoreCase) >= 0);
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
                var picker = new PactMagicElementPicker(session, new ElementPickerContext { ElementType = "vestige" });
                var tabPage = new PactMagicTabPage(host, picker);
                tabControl.TabPages.Add(tabPage);
                form.Controls.Add(tabControl);
                form.Show();
                Application.DoEvents();

                var vm = picker.ViewModel;
                Assert.IsFalse(vm.IsPinnedContainerVisible, "Pinned row should be hidden when zero vestiges are bound.");

                // Select Amon
                var amonCard = vm.FindCard("Amon")!;
                Assert.IsNotNull(amonCard);
                vm.SelectVestige("Amon");
                Application.DoEvents();

                Assert.IsTrue(amonCard.IsGoodPactVisible, "Selected unbound card must show Good Pact button.");
                Assert.IsTrue(amonCard.IsPoorPactVisible, "Selected unbound card must show Poor Pact button.");

                // Request Good Pact binding
                amonCard.BindGoodPact();
                Application.DoEvents();

                // Pinned container should now be visible and hold Amon card
                Assert.IsTrue(vm.IsPinnedContainerVisible, "Pinned row must be visible when a vestige is bound.");
                Assert.AreEqual(1, vm.PinnedCards.Count);
                Assert.AreSame(amonCard, vm.PinnedCards[0]);
                Assert.IsTrue(amonCard.IsBound);
                Assert.IsTrue(amonCard.IsUnbindVisible, "Bound card must show Unbind button.");
                Assert.IsFalse(amonCard.IsGoodPactVisible);
                Assert.IsFalse(amonCard.IsPoorPactVisible);

                // Session engine must reflect binding
                var engine = session.GetSubsystem("pact_magic");
                Assert.IsNotNull(engine);
                var slot0 = engine.GetSlot("vestiges", 0);
                Assert.IsNotNull(slot0);
                Assert.AreEqual("Amon", slot0.Element);
                Assert.AreEqual("good_pact", slot0.State);

                // Unbind Amon
                amonCard.Unbind();
                Application.DoEvents();

                Assert.IsFalse(vm.IsPinnedContainerVisible, "Pinned container should be hidden once all vestiges unbind.");
                Assert.IsFalse(amonCard.IsBound);
                Assert.AreEqual(0, vm.PinnedCards.Count);
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
                var picker = new PactMagicElementPicker(session, new ElementPickerContext { ElementType = "vestige" });
                var tabPage = new PactMagicTabPage(host, picker);
                tabControl.TabPages.Add(tabPage);
                form.Controls.Add(tabControl);
                form.Show();
                Application.DoEvents();

                var vm = picker.ViewModel;
                var amonCard = vm.FindCard("Amon")!;
                vm.SelectVestige("Amon");
                amonCard.BindGoodPact();
                Application.DoEvents();

                // Character is now at max capacity (1/1).
                // Select Aym (unbound)
                var aymCard = vm.FindCard("Aym")!;
                vm.SelectVestige("Aym");
                Application.DoEvents();

                Assert.IsTrue(aymCard.IsSelected);
                Assert.IsTrue(aymCard.IsCapacityNoticeVisible, "At max capacity, unbound card must display capacity notice.");
                Assert.IsTrue(aymCard.CapacityNoticeText.IndexOf("max vestige capacity", StringComparison.OrdinalIgnoreCase) >= 0);
                Assert.IsFalse(aymCard.IsGoodPactVisible, "Good pact button must not be visible when at max capacity.");
                Assert.IsFalse(aymCard.IsPoorPactVisible, "Poor pact button must not be visible when at max capacity.");
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

                    var vm = picker.ViewModel;
                    Assert.IsTrue(vm.Cards.Count > 0);
                    Assert.IsTrue(vm.HideUnavailable, "Hide unavailable should be checked by default.");
                    // Level 1 Binder with HideUnavailable checked will only show Level 1 vestiges (5 vestiges)
                    int initialCount = vm.VisibleCardsCount;
                    Assert.AreEqual(5, initialCount);

                    vm.SearchText = "Amon";
                    Application.DoEvents();

                    Assert.AreEqual(1, vm.VisibleCardsCount);
                    Assert.AreEqual("Amon", vm.Cards.First(c => c.IsVisible).Vestige.Name);

                    vm.SearchText = "";
                    Application.DoEvents();
                    Assert.AreEqual(initialCount, vm.VisibleCardsCount);

                    // Unchecking hide unavailable shows all 9 vestiges
                    vm.HideUnavailable = false;
                    Application.DoEvents();
                    Assert.IsTrue(vm.VisibleCardsCount >= 9);
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

            var vm = new PactMagicPickerViewModel(session, new ElementPickerContext {
                ElementType = "vestige",
                WarningProvider = elem => session.ValidateProgressionChoice(
                    new ProgressionChoiceContext(ProgressionChoiceDomain.Subsystem, elem.Name, targetSource: "pact_magic"))
            });

            vm.HideUnavailable = false; // allow searching all vestiges

            // Search by Sign keyword
            vm.SearchText = "curling ram's horns";
            Assert.AreEqual(1, vm.VisibleCardsCount);
            Assert.AreEqual("Amon", vm.Cards.First(c => c.IsVisible).Vestige.Name);

            // Search by Influence keyword
            vm.SearchText = "dwarf";
            Assert.AreEqual(1, vm.VisibleCardsCount);
            Assert.AreEqual("Aym", vm.Cards.First(c => c.IsVisible).Vestige.Name);

            // Search by Ability descriptive text keyword
            vm.SearchText = "fire";
            Assert.IsTrue(vm.VisibleCardsCount > 1);
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

            var vm = new VestigeCardViewModel(elem);
            using (var card = new VestigeCardControl(vm)) {
                Assert.IsTrue(card.Height >= 210, $"Card height was {card.Height}, expected >= 210");
                var powersText = card.ViewModel.CardAbilitiesExcerpt;
                Assert.IsFalse(powersText.Contains("more..."), "Abilities excerpt should not contain '+n more...'");
                foreach (var abilityName in distinctAbilities) {
                    Assert.IsTrue(powersText.Contains(abilityName), $"Abilities text missing: {abilityName}");
                }
            }
        }

        [TestMethod]
        public void VestigeCardControl_PactState_DisplaysPactStateAndUnbindWhenBound() {
            var catalog = new GameRulesCatalog();
            var pactDef = catalog.GetSubsystem("pact_magic");
            Assert.IsNotNull(pactDef);
            var elem = pactDef.Elements["Amon"];

            var vm = new VestigeCardViewModel(elem);
            vm.IsBound = true;
            vm.PactState = "poor_pact";

            Assert.AreEqual("Poor Pact", vm.StatusText);
            Assert.AreEqual(VestigeCardStatusKind.PoorPact, vm.StatusKind);
            Assert.IsTrue(vm.IsUnbindVisible);
            Assert.IsFalse(vm.IsGoodPactVisible);
            Assert.IsFalse(vm.IsPoorPactVisible);

            vm.PactState = "good_pact";
            Assert.AreEqual("Good Pact", vm.StatusText);
            Assert.AreEqual(VestigeCardStatusKind.GoodPact, vm.StatusKind);
        }

        [TestMethod]
        public void VestigeCardControl_CardStatusVisuals_UnmetLevelGate_DisplaysExceedsDarkGoldenrodAndTooltip() {
            var catalog = new GameRulesCatalog();
            var pactDef = catalog.GetSubsystem("pact_magic");
            Assert.IsNotNull(pactDef);
            var elem = pactDef.Elements["Amon"];

            var vm = new VestigeCardViewModel(elem);
            vm.Warnings = new[] {
                new ValidationWarning("Level 2 exceeds maximum vestige level 1.", ValidationCategory.LevelGate)
            };

            Assert.AreEqual("⚠ Exceeds", vm.StatusText);
            Assert.AreEqual(VestigeCardStatusKind.Exceeds, vm.StatusKind);
            Assert.AreEqual("Level 2 exceeds maximum vestige level 1.", vm.StatusTooltip);
            Assert.IsTrue(vm.IsInvalid);
        }

        [TestMethod]
        public void VestigeCardControl_CardStatusVisuals_UnmetOtherCategory_DisplaysIneligibleDarkGoldenrodAndTooltip() {
            var catalog = new GameRulesCatalog();
            var pactDef = catalog.GetSubsystem("pact_magic");
            Assert.IsNotNull(pactDef);
            var elem = pactDef.Elements["Amon"];

            var vm = new VestigeCardViewModel(elem);
            vm.Warnings = new[] {
                new ValidationWarning("Requires Bind Vestige feat.", ValidationCategory.Prerequisite)
            };

            Assert.AreEqual("⚠ Ineligible", vm.StatusText);
            Assert.AreEqual(VestigeCardStatusKind.Ineligible, vm.StatusKind);
            Assert.AreEqual("Requires Bind Vestige feat.", vm.StatusTooltip);
            Assert.IsTrue(vm.IsInvalid);
        }

        [TestMethod]
        public void VestigeCardControl_CardStatusVisuals_ItemDependent_DisplaysItemDepDodgerBlueAndTooltip() {
            var catalog = new GameRulesCatalog();
            var pactDef = catalog.GetSubsystem("pact_magic");
            Assert.IsNotNull(pactDef);
            var elem = pactDef.Elements["Amon"];

            var vm = new VestigeCardViewModel(elem);
            vm.Warnings = new[] {
                new ValidationWarning("Requires Str 13", WarningKind.ItemDependent, ValidationCategory.Prerequisite)
            };

            Assert.AreEqual("🛈 Item-Dep", vm.StatusText);
            Assert.AreEqual(VestigeCardStatusKind.ItemDependent, vm.StatusKind);
            Assert.AreEqual("Requires Str 13\n(Satisfied by currently equipped items)", vm.StatusTooltip);
            Assert.IsFalse(vm.IsInvalid);
        }

        [TestMethod]
        public void VestigeCardControl_CardStatusVisuals_NoWarnings_DisplaysAvailableAndGreen() {
            var catalog = new GameRulesCatalog();
            var pactDef = catalog.GetSubsystem("pact_magic");
            Assert.IsNotNull(pactDef);
            var elem = pactDef.Elements["Amon"];

            var vm = new VestigeCardViewModel(elem);
            vm.Warnings = Array.Empty<ValidationWarning>();

            Assert.AreEqual("Available", vm.StatusText);
            Assert.AreEqual(VestigeCardStatusKind.Available, vm.StatusKind);
            Assert.AreEqual(string.Empty, vm.StatusTooltip);
            Assert.IsFalse(vm.IsInvalid);
        }

        [TestMethod]
        public void VestigeCardControl_IsInvalid_TrueOnlyWhenUnmetWarningsPresent() {
            var catalog = new GameRulesCatalog();
            var pactDef = catalog.GetSubsystem("pact_magic");
            Assert.IsNotNull(pactDef);
            var elem = pactDef.Elements["Amon"];

            var vm = new VestigeCardViewModel(elem);
            Assert.IsFalse(vm.IsInvalid);

            vm.Warnings = new[] {
                new ValidationWarning("Requires item", WarningKind.ItemDependent, ValidationCategory.Prerequisite)
            };
            Assert.IsFalse(vm.IsInvalid);

            vm.Warnings = new[] {
                new ValidationWarning("Exceeds level gate", WarningKind.Unmet, ValidationCategory.LevelGate)
            };
            Assert.IsTrue(vm.IsInvalid);

            vm.Warnings = new[] {
                new ValidationWarning("Exceeds level gate", WarningKind.Unmet, ValidationCategory.LevelGate),
                new ValidationWarning("Requires item", WarningKind.ItemDependent, ValidationCategory.Prerequisite)
            };
            Assert.IsTrue(vm.IsInvalid);

            vm.Warnings = Array.Empty<ValidationWarning>();
            Assert.IsFalse(vm.IsInvalid);
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

            var vm = new PactMagicPickerViewModel(session, context);
            var amonCard = vm.FindCard("Amon")!;
            var aymCard = vm.FindCard("Aym")!;
            var lerajeCard = vm.FindCard("Leraje")!;

            // HideUnavailable is true by default
            Assert.IsTrue(vm.HideUnavailable);
            Assert.IsTrue(amonCard.IsVisible, "Available card with 0 warnings must be visible.");
            Assert.IsTrue(lerajeCard.IsVisible, "Card with ONLY item-dependent warnings must remain visible when HideUnavailable is checked.");
            Assert.IsFalse(aymCard.IsVisible, "Card with Unmet warnings must be hidden when HideUnavailable is checked.");

            // Unchecking HideUnavailable should make unmet card visible
            vm.HideUnavailable = false;
            Assert.IsTrue(aymCard.IsVisible, "Card with Unmet warnings becomes visible when HideUnavailable is unchecked.");
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

            var vm = new PactMagicPickerViewModel(session, context);

            // Pinned container must be hidden in progression mode even though Amon is bound in session
            Assert.IsFalse(vm.IsPinnedContainerVisible, "Pinned container must be hidden when AcquisitionLevel > 0.");
            Assert.AreEqual(0, vm.PinnedCards.Count);

            var amonCard = vm.FindCard("Amon")!;
            Assert.IsFalse(amonCard.IsBound, "Amon must not be marked as IsBound in progression mode.");

            // Daily pact buttons must be suppressed (hidden and disabled)
            Assert.IsFalse(amonCard.IsGoodPactVisible);
            Assert.IsFalse(amonCard.IsGoodPactEnabled);
            Assert.IsFalse(amonCard.IsPoorPactVisible);
            Assert.IsFalse(amonCard.IsPoorPactEnabled);
            Assert.IsFalse(amonCard.IsUnbindVisible);
            Assert.IsFalse(amonCard.IsUnbindEnabled);
            Assert.IsFalse(amonCard.IsCapacityNoticeVisible);

            // Selecting Amon delivers it via ElementDelivered without showing daily pact buttons
            SubsystemElement? deliveredElement = null;
            vm.ElementDelivered += elem => deliveredElement = elem;

            vm.SelectVestige("Amon");

            Assert.IsNotNull(deliveredElement);
            Assert.AreEqual("Amon", deliveredElement.Name);
            Assert.IsTrue(amonCard.IsSelected);
            Assert.IsFalse(amonCard.IsGoodPactVisible, "Good Pact button must remain suppressed when selected in progression mode.");
            Assert.IsFalse(amonCard.IsPoorPactVisible, "Poor Pact button must remain suppressed when selected in progression mode.");
            Assert.IsFalse(amonCard.IsUnbindVisible, "Unbind button must remain suppressed in progression mode.");
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
                var picker = new PactMagicElementPicker(session, new ElementPickerContext {
                    ElementType = "vestige",
                    WarningProvider = elem => session.ValidateProgressionChoice(
                        new ProgressionChoiceContext(ProgressionChoiceDomain.Subsystem, elem.Name, targetSource: "pact_magic"))
                });
                var tabPage = new PactMagicTabPage(host, picker);
                tabControl.TabPages.Add(tabPage);
                form.Controls.Add(tabControl);
                form.Show();
                Application.DoEvents();

                var amonCard = picker.ViewModel.FindCard("Amon")!;
                var savnokCard = picker.ViewModel.FindCard("Savnok")!;

                Assert.IsFalse(amonCard.IsInvalid, "Level 1 vestige is eligible for Level 1 Binder.");
                Assert.AreEqual("Available", amonCard.StatusText);

                Assert.IsTrue(savnokCard.IsInvalid, "Level 2 vestige exceeds max vestige level for Level 1 Binder.");
                Assert.AreEqual("⚠ Exceeds", savnokCard.StatusText);
                Assert.AreEqual(VestigeCardStatusKind.Exceeds, savnokCard.StatusKind);
                Assert.IsTrue(savnokCard.StatusTooltip.Contains("exceeds maximum"), $"Expected tooltip to mention exceeds maximum, got: {savnokCard.StatusTooltip}");
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
