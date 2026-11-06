using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlayMore35.CharacterChoices;
using PlayMore35.Global;
using PlayMore35.Rules;
using PlayMore35.Subsystems;
using PlayMore35.UI;
using PlayMore35.UI.Subsystems;

namespace PlayMore35.Tests {
    [TestClass]
    public class SubsystemUITests {
        [TestMethod]
        public void SubsystemUIRegistry_RegisterAndRetrieveByIdAndElementType() {
            var registry = new SubsystemUIRegistry();
            var mockUI = new MockSubsystemUI("pact_magic");

            registry.Register(mockUI, "vestige");

            Assert.AreSame(mockUI, registry.GetBySubsystemId("pact_magic"));
            Assert.AreSame(mockUI, registry.GetByElementType("vestige"));
            Assert.AreEqual(1, new List<ISubsystemUI>(registry.GetAll()).Count);
        }

        [TestMethod]
        public void SubsystemUIRegistry_GetByElementType_UnregisteredReturnsFallbackWithDefaultPicker() {
            var registry = new SubsystemUIRegistry();
            var session = CreateTestSession();

            var provider = registry.GetByElementType("unknown_element");
            Assert.IsNotNull(provider);
            Assert.IsFalse(provider.IsActive(session));
            Assert.IsNull(provider.CreateDedicatedTab(new MockSubsystemUIHost(session)));

            var context = new ElementPickerContext { ElementType = "unknown_element" };
            var picker = provider.CreateElementPicker(session, context);
            Assert.IsNotNull(picker);
            Assert.IsInstanceOfType(picker, typeof(DefaultElementPicker));
        }

        [TestMethod]
        public void SubsystemUIRegistry_CreateDefault_DeterministicRegistrations() {
            var registry = SubsystemUIRegistry.CreateDefault();
            Assert.IsNotNull(registry);
            var all = new List<ISubsystemUI>(registry.GetAll());
            Assert.IsTrue(all.Count >= 0);
        }

        [TestMethod]
        public void ElementReceptorControl_InitialState_Unmade() {
            using (var receptor = new ElementReceptorControl()) {
                Assert.AreEqual(ReceptorState.Unmade, receptor.State);
                Assert.IsNull(receptor.SelectedElement);
                Assert.IsFalse(receptor.ClearButtonVisible);
                Assert.IsFalse(receptor.WarningBadgeVisible);
            }
        }

        [TestMethod]
        public void ElementReceptorControl_SetSelectedElement_TransitionsToMadeState() {
            using (var receptor = new ElementReceptorControl()) {
                var elem = new SubsystemElement { Name = "Aym", Type = "vestige" };
                receptor.SelectedElement = elem;

                Assert.AreEqual(ReceptorState.Made, receptor.State);
                Assert.AreSame(elem, receptor.SelectedElement);
                Assert.IsTrue(receptor.ClearButtonVisible);
                Assert.IsTrue(receptor.DisplayText.Contains("Aym"));
            }
        }

        [TestMethod]
        public void ElementReceptorControl_IsActiveSelecting_TransitionsBorderAccent() {
            using (var receptor = new ElementReceptorControl()) {
                receptor.IsActiveSelecting = true;
                Assert.AreEqual(ReceptorState.ActiveSelecting, receptor.State);

                receptor.IsActiveSelecting = false;
                Assert.AreEqual(ReceptorState.Unmade, receptor.State);

                receptor.SelectedElement = new SubsystemElement { Name = "Aym", Type = "vestige" };
                Assert.AreEqual(ReceptorState.Made, receptor.State);

                receptor.IsActiveSelecting = true;
                Assert.AreEqual(ReceptorState.ActiveSelecting, receptor.State);

                receptor.IsActiveSelecting = false;
                Assert.AreEqual(ReceptorState.Made, receptor.State);
            }
        }

        [TestMethod]
        public void ElementReceptorControl_Click_FiresActivatedEvent() {
            using (var receptor = new ElementReceptorControl()) {
                bool activatedFired = false;
                receptor.Activated += (s, e) => activatedFired = true;

                receptor.PerformClick();
                Assert.IsTrue(activatedFired);
            }
        }

        [TestMethod]
        public void ElementReceptorControl_ClearClick_ClearsElementAndFiresElementCleared() {
            using (var receptor = new ElementReceptorControl()) {
                receptor.SelectedElement = new SubsystemElement { Name = "Aym", Type = "vestige" };
                bool clearedFired = false;
                SubsystemElement? deliveredAfterClear = new SubsystemElement();
                receptor.ElementCleared += (s, e) => clearedFired = true;
                receptor.ElementDelivered += elem => deliveredAfterClear = elem;

                receptor.PerformClear();

                Assert.IsNull(receptor.SelectedElement);
                Assert.AreEqual(ReceptorState.Unmade, receptor.State);
                Assert.IsTrue(clearedFired);
                Assert.IsNull(deliveredAfterClear);
            }
        }

        [TestMethod]
        public void ElementReceptorControl_WarningHandling_SetsAndClearsWarning() {
            using (var receptor = new ElementReceptorControl()) {
                receptor.SetWarning("Prerequisites not met");
                Assert.IsTrue(receptor.WarningBadgeVisible);
                Assert.AreEqual("Prerequisites not met", receptor.GetWarningBadge().GetTooltip());

                receptor.ClearWarning();
                Assert.IsFalse(receptor.WarningBadgeVisible);
                Assert.AreEqual("", receptor.GetWarningBadge().GetTooltip());
            }
        }

        [TestMethod]
        public void DefaultElementPicker_DeliversChoiceImmediately_AndRetainsDisplay() {
            var session = CreateTestSession();
            var context = new ElementPickerContext {
                ElementType = "vestige"
            };

            var elements = new List<SubsystemElement> {
                new SubsystemElement { Name = "Aym", Type = "vestige" },
                new SubsystemElement { Name = "Ronove", Type = "vestige" }
            };

            using (var picker = new DefaultElementPicker(session, context, elements)) {
                SubsystemElement? delivered = null;
                picker.ElementDelivered += e => delivered = e;

                picker.SelectElementByName("Aym");

                Assert.IsNotNull(delivered);
                Assert.AreEqual("Aym", delivered?.Name);
                Assert.IsTrue(picker.Control.Visible);
            }
        }

        [TestMethod]
        public void DefaultElementPicker_SelectingNone_DeliversNull() {
            var session = CreateTestSession();
            var context = new ElementPickerContext {
                ElementType = "vestige"
            };

            var elements = new List<SubsystemElement> {
                new SubsystemElement { Name = "Aym", Type = "vestige" }
            };

            using (var picker = new DefaultElementPicker(session, context, elements)) {
                SubsystemElement? delivered = new SubsystemElement();
                picker.ElementDelivered += e => delivered = e;

                picker.SelectNone();

                Assert.IsNull(delivered);
            }
        }

        [TestMethod]
        public void DefaultElementPicker_SearchFilter_FiltersElementList() {
            var session = CreateTestSession();
            var context = new ElementPickerContext {
                ElementType = "vestige"
            };

            var elements = new List<SubsystemElement> {
                new SubsystemElement { Name = "Aym", Type = "vestige" },
                new SubsystemElement { Name = "Ronove", Type = "vestige" }
            };

            using (var picker = new DefaultElementPicker(session, context, elements)) {
                picker.SearchFilterText = "Aym";
                var visible = new List<string>(picker.GetFilteredElementNames());
                CollectionAssert.Contains(visible, "Aym");
                CollectionAssert.DoesNotContain(visible, "Ronove");
            }
        }

        [TestMethod]
        public void DefaultElementPicker_HideInvalidChoices_FiltersByWarningProvider() {
            var session = CreateTestSession();
            var context = new ElementPickerContext {
                ElementType = "vestige",
                WarningProvider = elem => elem.Name == "Ronove"
                    ? new[] { new ValidationWarning("Requires Binder Level 3", WarningKind.Unmet) }
                    : Array.Empty<ValidationWarning>()
            };

            var elements = new List<SubsystemElement> {
                new SubsystemElement { Name = "Aym", Type = "vestige" },
                new SubsystemElement { Name = "Ronove", Type = "vestige" }
            };

            using (var picker = new DefaultElementPicker(session, context, elements)) {
                var visibleInitial = new List<string>(picker.GetFilteredElementNames());
                CollectionAssert.Contains(visibleInitial, "Aym");
                CollectionAssert.Contains(visibleInitial, "Ronove");

                picker.HideInvalidChoices = true;
                var visibleAfterHide = new List<string>(picker.GetFilteredElementNames());
                CollectionAssert.Contains(visibleAfterHide, "Aym");
                CollectionAssert.DoesNotContain(visibleAfterHide, "Ronove");
            }
        }

        [TestMethod]
        public void DefaultElementPicker_HideInvalidChoices_RetainsItemDependentWarnings_AndFiltersUnmet() {
            var session = CreateTestSession();
            var context = new ElementPickerContext {
                ElementType = "vestige",
                WarningProvider = elem => {
                    if (elem.Name == "UnmetElem") {
                        return new[] { new ValidationWarning("Unmet requirement", WarningKind.Unmet) };
                    }
                    if (elem.Name == "ItemDepElem") {
                        return new[] { new ValidationWarning("Item reliant requirement", WarningKind.ItemDependent) };
                    }
                    if (elem.Name == "MixedElem") {
                        return new[] {
                            new ValidationWarning("Item reliant", WarningKind.ItemDependent),
                            new ValidationWarning("Unmet", WarningKind.Unmet)
                        };
                    }
                    return Array.Empty<ValidationWarning>();
                }
            };

            var elements = new List<SubsystemElement> {
                new SubsystemElement { Name = "ValidElem", Type = "vestige" },
                new SubsystemElement { Name = "ItemDepElem", Type = "vestige" },
                new SubsystemElement { Name = "UnmetElem", Type = "vestige" },
                new SubsystemElement { Name = "MixedElem", Type = "vestige" }
            };

            using (var picker = new DefaultElementPicker(session, context, elements)) {
                var initial = new List<string>(picker.GetFilteredElementNames());
                CollectionAssert.Contains(initial, "(None)");
                CollectionAssert.Contains(initial, "ValidElem");
                CollectionAssert.Contains(initial, "ItemDepElem");
                CollectionAssert.Contains(initial, "UnmetElem");
                CollectionAssert.Contains(initial, "MixedElem");

                picker.HideInvalidChoices = true;
                var filtered = new List<string>(picker.GetFilteredElementNames());
                CollectionAssert.Contains(filtered, "(None)");
                CollectionAssert.Contains(filtered, "ValidElem");
                CollectionAssert.Contains(filtered, "ItemDepElem");
                CollectionAssert.DoesNotContain(filtered, "UnmetElem");
                CollectionAssert.DoesNotContain(filtered, "MixedElem");
            }
        }

        [TestMethod]
        public void DefaultElementPicker_ItemBadges_WorstKindPrecedenceAndColors() {
            var session = CreateTestSession();
            var context = new ElementPickerContext {
                ElementType = "vestige",
                WarningProvider = elem => {
                    if (elem.Name == "ItemDepElem") {
                        return new[] { new ValidationWarning("Item reliant requirement", WarningKind.ItemDependent) };
                    }
                    if (elem.Name == "UnmetElem") {
                        return new[] { new ValidationWarning("Unmet requirement", WarningKind.Unmet) };
                    }
                    if (elem.Name == "MixedElem") {
                        return new[] {
                            new ValidationWarning("Item reliant", WarningKind.ItemDependent),
                            new ValidationWarning("Unmet requirement", WarningKind.Unmet)
                        };
                    }
                    return Array.Empty<ValidationWarning>();
                }
            };

            var elements = new List<SubsystemElement> {
                new SubsystemElement { Name = "ValidElem", Type = "vestige" },
                new SubsystemElement { Name = "ItemDepElem", Type = "vestige" },
                new SubsystemElement { Name = "UnmetElem", Type = "vestige" },
                new SubsystemElement { Name = "MixedElem", Type = "vestige" }
            };

            using (var picker = new DefaultElementPicker(session, context, elements)) {
                // Index 0: (None)
                Assert.IsNull(picker.GetItemBadge(0));

                // Index 1: ValidElem (no warnings)
                Assert.IsNull(picker.GetItemBadge(1));

                // Index 2: ItemDepElem (all ItemDependent -> blue 🛈)
                var itemDepBadge = picker.GetItemBadge(2);
                Assert.IsNotNull(itemDepBadge);
                Assert.AreEqual("🛈", itemDepBadge.Value.BadgeText);
                Assert.AreEqual(Color.DodgerBlue, itemDepBadge.Value.BadgeColor);

                // Index 3: UnmetElem (has Unmet -> goldenrod ⚠)
                var unmetBadge = picker.GetItemBadge(3);
                Assert.IsNotNull(unmetBadge);
                Assert.AreEqual("⚠", unmetBadge.Value.BadgeText);
                Assert.AreEqual(Color.DarkGoldenrod, unmetBadge.Value.BadgeColor);

                // Index 4: MixedElem (both Unmet and ItemDependent -> goldenrod ⚠ worst-kind precedence)
                var mixedBadge = picker.GetItemBadge(4);
                Assert.IsNotNull(mixedBadge);
                Assert.AreEqual("⚠", mixedBadge.Value.BadgeText);
                Assert.AreEqual(Color.DarkGoldenrod, mixedBadge.Value.BadgeColor);

                // Exercise owner-draw rendering for all items on a real Graphics surface
                using (var bmp = new Bitmap(200, 100))
                using (var g = Graphics.FromImage(bmp)) {
                    for (int i = 0; i < 5; i++) {
                        picker.DrawItemForTest(g, new Rectangle(0, i * 20, 200, 20), i);
                    }
                }
            }
        }

        [TestMethod]
        public void DefaultElementPicker_DetailsPane_FormatsWarningMessagesByKind() {
            var session = CreateTestSession();
            var context = new ElementPickerContext {
                ElementType = "vestige",
                WarningProvider = elem => {
                    if (elem.Name == "UnmetElem") {
                        return new[] { new ValidationWarning("Requires Binder Level 3", WarningKind.Unmet) };
                    }
                    if (elem.Name == "ItemDepElem") {
                        return new[] { new ValidationWarning("Requires BAB +3", WarningKind.ItemDependent) };
                    }
                    if (elem.Name == "MixedElem") {
                        return new[] {
                            new ValidationWarning("Missing prerequisite feat", WarningKind.Unmet),
                            new ValidationWarning("Requires Str 13", WarningKind.ItemDependent)
                        };
                    }
                    return Array.Empty<ValidationWarning>();
                }
            };

            var elemValid = new SubsystemElement { Name = "ValidElem", Type = "vestige" };
            elemValid.SetProperty("description", "Lore text");
            var elemUnmet = new SubsystemElement { Name = "UnmetElem", Type = "vestige" };
            elemUnmet.SetProperty("description", "Unmet lore");
            var elemItemDep = new SubsystemElement { Name = "ItemDepElem", Type = "vestige" };
            elemItemDep.SetProperty("description", "ItemDep lore");
            var elemMixed = new SubsystemElement { Name = "MixedElem", Type = "vestige" };
            elemMixed.SetProperty("description", "Mixed lore");

            var elements = new List<SubsystemElement> {
                elemValid,
                elemUnmet,
                elemItemDep,
                elemMixed
            };

            using (var picker = new DefaultElementPicker(session, context, elements)) {
                // Select ValidElem
                picker.SelectElementByName("ValidElem");
                Assert.AreEqual("ValidElem\nLore text", picker.DetailsText);

                // Select UnmetElem
                picker.SelectElementByName("UnmetElem");
                Assert.IsTrue(picker.DetailsText.Contains("UnmetElem\nUnmet lore\n\n[Warning: Requires Binder Level 3]"));

                // Select ItemDepElem
                picker.SelectElementByName("ItemDepElem");
                Assert.IsTrue(picker.DetailsText.Contains("ItemDepElem\nItemDep lore\n\n[Item-Dependent: Requires BAB +3 (satisfied by equipped items)]"));

                // Select MixedElem
                picker.SelectElementByName("MixedElem");
                Assert.IsTrue(picker.DetailsText.Contains("[Warning: Missing prerequisite feat]"));
                Assert.IsTrue(picker.DetailsText.Contains("[Item-Dependent: Requires Str 13 (satisfied by equipped items)]"));

                // Select None
                picker.SelectNone();
                Assert.AreEqual("(None selected)", picker.DetailsText);
            }
        }

        [TestMethod]
        public void DefaultElementPicker_PreviewDetailsPaneToggle_CollapsesAndExpands() {
            var session = CreateTestSession();
            var context = new ElementPickerContext { ElementType = "vestige" };
            var elements = new List<SubsystemElement> {
                new SubsystemElement { Name = "Aym", Type = "vestige" }
            };

            using (var picker = new DefaultElementPicker(session, context, elements)) {
                Assert.IsTrue(picker.IsDetailsExpanded);
                Assert.IsTrue(picker.DetailsPane.Visible);
                Assert.AreEqual("▼ Hide Details", picker.DetailsToggleButton.Text);

                picker.ToggleDetailsPane();
                Assert.IsFalse(picker.IsDetailsExpanded);
                Assert.IsFalse(picker.DetailsPane.Visible);
                Assert.AreEqual("▲ Show Details", picker.DetailsToggleButton.Text);

                picker.ToggleDetailsPane();
                Assert.IsTrue(picker.IsDetailsExpanded);
                Assert.IsTrue(picker.DetailsPane.Visible);
                Assert.AreEqual("▼ Hide Details", picker.DetailsToggleButton.Text);
            }
        }

        [TestMethod]
        public void DefaultElementPicker_FilterManeuvers_IncludesStrikesBoostsCountersOther_AndExcludesStances() {
            var session = CreateTestSession();
            var context = new ElementPickerContext {
                ElementType = "martial_maneuvers",
                Filter = "maneuver"
            };

            var elements = new List<SubsystemElement> {
                new SubsystemElement { Name = "Steel Wind", Type = "Strike" },
                new SubsystemElement { Name = "Wind Stride", Type = "Boost" },
                new SubsystemElement { Name = "Fire Riposte", Type = "Counter" },
                new SubsystemElement { Name = "White Raven Tactics", Type = "Other" },
                new SubsystemElement { Name = "Punishing Stance", Type = "Stance" }
            };

            using (var picker = new DefaultElementPicker(session, context, elements)) {
                var filtered = new List<string>(picker.GetFilteredElementNames());
                CollectionAssert.Contains(filtered, "Steel Wind");
                CollectionAssert.Contains(filtered, "Wind Stride");
                CollectionAssert.Contains(filtered, "Fire Riposte");
                CollectionAssert.Contains(filtered, "White Raven Tactics");
                CollectionAssert.DoesNotContain(filtered, "Punishing Stance");
            }
        }

        [TestMethod]
        public void DefaultElementPicker_FilterStances_IncludesOnlyStances() {
            var session = CreateTestSession();
            var context = new ElementPickerContext {
                ElementType = "martial_maneuvers",
                Filter = "stance"
            };

            var elements = new List<SubsystemElement> {
                new SubsystemElement { Name = "Steel Wind", Type = "Strike" },
                new SubsystemElement { Name = "Wind Stride", Type = "Boost" },
                new SubsystemElement { Name = "Punishing Stance", Type = "Stance" },
                new SubsystemElement { Name = "Bolstering Voice", Type = "Stance" }
            };

            using (var picker = new DefaultElementPicker(session, context, elements)) {
                var filtered = new List<string>(picker.GetFilteredElementNames());
                CollectionAssert.DoesNotContain(filtered, "Steel Wind");
                CollectionAssert.DoesNotContain(filtered, "Wind Stride");
                CollectionAssert.Contains(filtered, "Punishing Stance");
                CollectionAssert.Contains(filtered, "Bolstering Voice");
            }
        }

        private static ICharacterSession CreateTestSession() {
            var catalog = new GameRulesCatalog();
            var character = new Character {
                race = "Human"
            };
            return new CharacterSession(character, catalog);
        }

        private class MockSubsystemUI : ISubsystemUI {
            public string SubsystemId { get; }
            public MockSubsystemUI(string id) => SubsystemId = id;
            public bool IsActive(ICharacterSession session) => true;
            public Control? CreateDedicatedTab(ISubsystemUIHost host) => new TabPage("Mock");
            public ISubsystemElementPicker CreateElementPicker(ICharacterSession session, ElementPickerContext context) {
                return new DefaultElementPicker(session, context, new List<SubsystemElement>());
            }
        }

        private class MockSubsystemUIHost : ISubsystemUIHost {
            public ICharacterSession Session { get; }
            public MockSubsystemUIHost(ICharacterSession session) => Session = session;
            public void RequestRecalculate() { }
            public void RequestEffectsRefresh() { }
            public void RequestTimelineRefresh() { }
        }
    }
}
