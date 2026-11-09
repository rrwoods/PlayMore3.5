using System;
using System.Collections.Generic;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlayMore35.Global;
using PlayMore35.Subsystems;
using PlayMore35.UI.Subsystems;

namespace PlayMore35.Tests {
    [TestClass]
    public class ElementReceptorControlTests {
        [TestMethod]
        public void ElementReceptorViewModel_InitialState_Unmade() {
            var vm = new ElementReceptorViewModel();
            Assert.AreEqual(ReceptorState.Unmade, vm.State);
            Assert.IsNull(vm.SelectedElement);
            Assert.IsFalse(vm.IsActiveSelecting);
            Assert.IsFalse(vm.IsClearButtonVisible);
            Assert.IsFalse(vm.IsWarningBadgeVisible);
            Assert.AreEqual("(Choose element...)", vm.DisplayText);
            Assert.AreEqual("", vm.WarningBadgeText);
            Assert.AreEqual("", vm.WarningTooltip);
            Assert.AreEqual(0, vm.Warnings.Count);
        }

        [TestMethod]
        public void ElementReceptorViewModel_SetSelectedElement_TransitionsToMadeState() {
            var vm = new ElementReceptorViewModel();
            var elem = new SubsystemElement { Name = "Aym", Type = "vestige" };
            bool stateChangedFired = false;
            vm.StateChanged += (s, e) => stateChangedFired = true;

            vm.SelectedElement = elem;

            Assert.IsTrue(stateChangedFired);
            Assert.AreEqual(ReceptorState.Made, vm.State);
            Assert.AreSame(elem, vm.SelectedElement);
            Assert.IsTrue(vm.IsClearButtonVisible);
            Assert.AreEqual("Aym", vm.DisplayText);
        }

        [TestMethod]
        public void ElementReceptorViewModel_IsActiveSelecting_TransitionsState() {
            var vm = new ElementReceptorViewModel();
            vm.IsActiveSelecting = true;
            Assert.AreEqual(ReceptorState.ActiveSelecting, vm.State);

            vm.IsActiveSelecting = false;
            Assert.AreEqual(ReceptorState.Unmade, vm.State);

            vm.SelectedElement = new SubsystemElement { Name = "Aym", Type = "vestige" };
            Assert.AreEqual(ReceptorState.Made, vm.State);

            vm.IsActiveSelecting = true;
            Assert.AreEqual(ReceptorState.ActiveSelecting, vm.State);

            vm.IsActiveSelecting = false;
            Assert.AreEqual(ReceptorState.Made, vm.State);
        }

        [TestMethod]
        public void ElementReceptorViewModel_Activate_FiresActivatedEvent() {
            var vm = new ElementReceptorViewModel();
            bool activatedFired = false;
            vm.Activated += (s, e) => activatedFired = true;

            vm.Activate();
            Assert.IsTrue(activatedFired);
        }

        [TestMethod]
        public void ElementReceptorViewModel_Clear_ClearsElementAndFiresEvents() {
            var vm = new ElementReceptorViewModel();
            var elem = new SubsystemElement { Name = "Aym", Type = "vestige" };
            vm.SelectedElement = elem;

            bool clearedFired = false;
            SubsystemElement? deliveredAfterClear = new SubsystemElement();
            vm.ElementCleared += (s, e) => clearedFired = true;
            vm.ElementDelivered += delivered => deliveredAfterClear = delivered;

            vm.Clear();

            Assert.IsNull(vm.SelectedElement);
            Assert.AreEqual(ReceptorState.Unmade, vm.State);
            Assert.IsTrue(clearedFired);
            Assert.IsNull(deliveredAfterClear);
            Assert.IsFalse(vm.IsClearButtonVisible);
        }

        [TestMethod]
        public void ElementReceptorViewModel_WarningHandling_UnmetAndItemDependentPrecedence() {
            var vm = new ElementReceptorViewModel();
            Assert.IsFalse(vm.IsWarningBadgeVisible);
            Assert.IsNull(vm.WorstWarningKind);

            // Set Unmet warning
            vm.SetWarning("Prerequisites not met", WarningKind.Unmet);
            Assert.IsTrue(vm.IsWarningBadgeVisible);
            Assert.AreEqual(WarningKind.Unmet, vm.WorstWarningKind);
            Assert.AreEqual("⚠", vm.WarningBadgeText);
            Assert.AreEqual("Prerequisites not met", vm.WarningTooltip);

            // Set ItemDependent warning
            vm.SetWarning("Item reliant requirement", WarningKind.ItemDependent);
            Assert.IsTrue(vm.IsWarningBadgeVisible);
            Assert.AreEqual(WarningKind.ItemDependent, vm.WorstWarningKind);
            Assert.AreEqual("🛈", vm.WarningBadgeText);
            StringAssert.Contains(vm.WarningTooltip, "Item reliant requirement");
            StringAssert.Contains(vm.WarningTooltip, "(Satisfied by currently equipped items)");

            // Set Mixed warnings: Unmet takes worst-kind precedence
            vm.SetWarnings(new[] {
                new ValidationWarning("Gear requirement", WarningKind.ItemDependent),
                new ValidationWarning("Missing feat", WarningKind.Unmet)
            });
            Assert.IsTrue(vm.IsWarningBadgeVisible);
            Assert.AreEqual(WarningKind.Unmet, vm.WorstWarningKind);
            Assert.AreEqual("⚠", vm.WarningBadgeText);
            StringAssert.Contains(vm.WarningTooltip, "Missing feat");

            // Clear warnings
            vm.ClearWarning();
            Assert.IsFalse(vm.IsWarningBadgeVisible);
            Assert.IsNull(vm.WorstWarningKind);
            Assert.AreEqual("", vm.WarningBadgeText);
            Assert.AreEqual("", vm.WarningTooltip);
        }

        [TestMethod]
        public void ElementReceptorControl_VisualAdapter_BindsToViewModel() {
            var vm = new ElementReceptorViewModel();
            using (var receptor = new ElementReceptorControl(vm)) {
                Assert.AreSame(vm, receptor.ViewModel);
                Assert.AreEqual(ReceptorState.Unmade, receptor.ViewModel.State);

                var elem = new SubsystemElement { Name = "Ronove", Type = "vestige" };
                receptor.SelectedElement = elem;
                Assert.AreSame(elem, vm.SelectedElement);
                Assert.AreEqual(ReceptorState.Made, vm.State);
                Assert.IsTrue(receptor.ViewModel.IsClearButtonVisible);

                receptor.IsActiveSelecting = true;
                Assert.AreEqual(ReceptorState.ActiveSelecting, vm.State);

                receptor.SetWarnings(new[] { new ValidationWarning("Warning test", WarningKind.Unmet) });
                Assert.IsTrue(vm.IsWarningBadgeVisible);
                Assert.AreEqual("Warning test", vm.WarningTooltip);

                receptor.PerformClear();
                Assert.IsNull(vm.SelectedElement);
                Assert.AreEqual(ReceptorState.ActiveSelecting, vm.State);

                receptor.IsActiveSelecting = false;
                Assert.AreEqual(ReceptorState.Unmade, vm.State);
            }
        }
    }
}
