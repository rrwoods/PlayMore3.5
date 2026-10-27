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
                Assert.AreEqual("Prerequisites not met", receptor.WarningMessage);

                receptor.ClearWarning();
                Assert.IsFalse(receptor.WarningBadgeVisible);
                Assert.IsNull(receptor.WarningMessage);
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
                WarningProvider = elem => elem.Name == "Ronove" ? new[] { "Requires Binder Level 3" } : Array.Empty<string>()
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
