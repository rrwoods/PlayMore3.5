using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
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
                Assert.AreEqual(ReceptorState.Unmade, receptor.ViewModel.State);
                Assert.IsNull(receptor.SelectedElement);
                Assert.IsFalse(receptor.ViewModel.IsClearButtonVisible);
                Assert.IsFalse(receptor.ViewModel.IsWarningBadgeVisible);
            }
        }

        [TestMethod]
        public void ElementReceptorControl_SetSelectedElement_TransitionsToMadeState() {
            using (var receptor = new ElementReceptorControl()) {
                var elem = new SubsystemElement { Name = "Aym", Type = "vestige" };
                receptor.SelectedElement = elem;

                Assert.AreEqual(ReceptorState.Made, receptor.ViewModel.State);
                Assert.AreSame(elem, receptor.SelectedElement);
                Assert.IsTrue(receptor.ViewModel.IsClearButtonVisible);
                Assert.IsTrue(receptor.ViewModel.DisplayText.Contains("Aym"));
            }
        }

        [TestMethod]
        public void ElementReceptorControl_IsActiveSelecting_TransitionsBorderAccent() {
            using (var receptor = new ElementReceptorControl()) {
                receptor.IsActiveSelecting = true;
                Assert.AreEqual(ReceptorState.ActiveSelecting, receptor.ViewModel.State);

                receptor.IsActiveSelecting = false;
                Assert.AreEqual(ReceptorState.Unmade, receptor.ViewModel.State);

                receptor.SelectedElement = new SubsystemElement { Name = "Aym", Type = "vestige" };
                Assert.AreEqual(ReceptorState.Made, receptor.ViewModel.State);

                receptor.IsActiveSelecting = true;
                Assert.AreEqual(ReceptorState.ActiveSelecting, receptor.ViewModel.State);

                receptor.IsActiveSelecting = false;
                Assert.AreEqual(ReceptorState.Made, receptor.ViewModel.State);
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
                Assert.AreEqual(ReceptorState.Unmade, receptor.ViewModel.State);
                Assert.IsTrue(clearedFired);
                Assert.IsNull(deliveredAfterClear);
            }
        }

        [TestMethod]
        public void ElementReceptorControl_WarningHandling_SetsAndClearsWarning() {
            using (var receptor = new ElementReceptorControl()) {
                receptor.ViewModel.SetWarning("Prerequisites not met");
                Assert.IsTrue(receptor.ViewModel.IsWarningBadgeVisible);
                Assert.AreEqual("Prerequisites not met", receptor.ViewModel.WarningTooltip);

                receptor.ClearWarning();
                Assert.IsFalse(receptor.ViewModel.IsWarningBadgeVisible);
                Assert.AreEqual("", receptor.ViewModel.WarningTooltip);
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

                picker.ViewModel.SelectElementByName("Aym");

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

                picker.ViewModel.SelectNone();

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
                picker.ViewModel.SearchText = "Aym";
                var visible = picker.ViewModel.FilteredNames();
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
                var visibleInitial = picker.ViewModel.FilteredNames();
                CollectionAssert.Contains(visibleInitial, "Aym");
                CollectionAssert.Contains(visibleInitial, "Ronove");

                picker.ViewModel.HideInvalidChoices = true;
                var visibleAfterHide = picker.ViewModel.FilteredNames();
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
                var initial = picker.ViewModel.FilteredNames();
                CollectionAssert.Contains(initial, "(None)");
                CollectionAssert.Contains(initial, "ValidElem");
                CollectionAssert.Contains(initial, "ItemDepElem");
                CollectionAssert.Contains(initial, "UnmetElem");
                CollectionAssert.Contains(initial, "MixedElem");

                picker.ViewModel.HideInvalidChoices = true;
                var filtered = picker.ViewModel.FilteredNames();
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
                Assert.AreEqual("🛈", picker.ViewModel.FilteredItems[2].BadgeText);
                Assert.AreEqual(WarningKind.ItemDependent, picker.ViewModel.FilteredItems[2].WorstWarningKind);

                // Index 3: UnmetElem (has Unmet -> goldenrod ⚠)
                var unmetBadge = picker.GetItemBadge(3);
                Assert.IsNotNull(unmetBadge);
                Assert.AreEqual("⚠", unmetBadge.Value.BadgeText);
                Assert.AreEqual(Color.DarkGoldenrod, unmetBadge.Value.BadgeColor);
                Assert.AreEqual("⚠", picker.ViewModel.FilteredItems[3].BadgeText);
                Assert.AreEqual(WarningKind.Unmet, picker.ViewModel.FilteredItems[3].WorstWarningKind);

                // Index 4: MixedElem (both Unmet and ItemDependent -> goldenrod ⚠ worst-kind precedence)
                var mixedBadge = picker.GetItemBadge(4);
                Assert.IsNotNull(mixedBadge);
                Assert.AreEqual("⚠", mixedBadge.Value.BadgeText);
                Assert.AreEqual(Color.DarkGoldenrod, mixedBadge.Value.BadgeColor);
                Assert.AreEqual("⚠", picker.ViewModel.FilteredItems[4].BadgeText);
                Assert.AreEqual(WarningKind.Unmet, picker.ViewModel.FilteredItems[4].WorstWarningKind);

                // Exercise owner-draw rendering for all items on a real Graphics surface
                using (var bmp = new Bitmap(200, 100))
                using (var g = Graphics.FromImage(bmp)) {
                    for (int i = 0; i < 5; i++) {
                        DrawPickerItem(picker, g, new Rectangle(0, i * 20, 200, 20), i);
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
                picker.ViewModel.SelectElementByName("ValidElem");
                Assert.AreEqual("ValidElem\nLore text", picker.ViewModel.DetailsText);

                // Select UnmetElem
                picker.ViewModel.SelectElementByName("UnmetElem");
                Assert.IsTrue(picker.ViewModel.DetailsText.Contains("UnmetElem\nUnmet lore\n\n[Warning: Requires Binder Level 3]"));

                // Select ItemDepElem
                picker.ViewModel.SelectElementByName("ItemDepElem");
                Assert.IsTrue(picker.ViewModel.DetailsText.Contains("ItemDepElem\nItemDep lore\n\n[Item-Dependent: Requires BAB +3 (satisfied by equipped items)]"));

                // Select MixedElem
                picker.ViewModel.SelectElementByName("MixedElem");
                Assert.IsTrue(picker.ViewModel.DetailsText.Contains("[Warning: Missing prerequisite feat]"));
                Assert.IsTrue(picker.ViewModel.DetailsText.Contains("[Item-Dependent: Requires Str 13 (satisfied by equipped items)]"));

                // Select None
                picker.ViewModel.SelectNone();
                Assert.AreEqual("(None selected)", picker.ViewModel.DetailsText);
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
                Assert.IsTrue(picker.ViewModel.IsDetailsExpanded);
                Assert.AreEqual("▼ Hide Details", picker.ViewModel.DetailsToggleText);

                picker.ViewModel.ToggleDetailsPane();
                Assert.IsFalse(picker.ViewModel.IsDetailsExpanded);
                Assert.AreEqual("▲ Show Details", picker.ViewModel.DetailsToggleText);

                picker.ViewModel.ToggleDetailsPane();
                Assert.IsTrue(picker.ViewModel.IsDetailsExpanded);
                Assert.AreEqual("▼ Hide Details", picker.ViewModel.DetailsToggleText);
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
                var filtered = picker.ViewModel.FilteredNames();
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
                var filtered = picker.ViewModel.FilteredNames();
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

        private static void DrawPickerItem(DefaultElementPicker picker, Graphics g, Rectangle bounds, int index, DrawItemState state = DrawItemState.Default) {
            var listBox = picker.Controls.OfType<ListBox>().First();
            var method = typeof(DefaultElementPicker).GetMethod("ListBox_DrawItem", BindingFlags.Instance | BindingFlags.NonPublic);
            method!.Invoke(picker, new object[] { listBox, new DrawItemEventArgs(g, listBox.Font, bounds, index, state) });
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
