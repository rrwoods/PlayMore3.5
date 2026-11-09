using System;
using System.Collections.Generic;
using System.Linq;
using PlayMore35.Global;
using PlayMore35.Subsystems;
using PlayMore35.UI.Subsystems;

namespace PlayMore35.Tests {
    public static class PresentationModelTestExtensions {
        public static void SetWarning(this ElementReceptorViewModel vm, string message, WarningKind kind = WarningKind.Unmet) {
            if (string.IsNullOrEmpty(message)) {
                vm.ClearWarning();
            } else {
                vm.SetWarnings(new[] { new ValidationWarning(message, kind) });
            }
        }

        public static List<string> FilteredNames(this DefaultElementPickerViewModel vm) =>
            vm.FilteredItems.Select(x => x.DisplayName).ToList();

        public static SubsystemElement? GetSelectedElement(this DefaultElementPickerViewModel vm) =>
            vm.SelectedIndex >= 0 && vm.SelectedIndex < vm.FilteredItems.Count
                ? vm.FilteredItems[vm.SelectedIndex].Element
                : null;

        public static void SelectElementByName(this DefaultElementPickerViewModel vm, string name) {
            for (int i = 0; i < vm.FilteredItems.Count; i++) {
                var item = vm.FilteredItems[i];
                if (item.Element != null && string.Equals(item.DisplayName, name, StringComparison.OrdinalIgnoreCase)) {
                    if (vm.SelectedIndex == i) {
                        vm.SelectedIndex = -1;
                    }
                    vm.SelectedIndex = i;
                    return;
                }
            }
        }

        public static void SelectNone(this DefaultElementPickerViewModel vm) {
            for (int i = 0; i < vm.FilteredItems.Count; i++) {
                if (vm.FilteredItems[i].Element == null) {
                    if (vm.SelectedIndex == i) {
                        vm.SelectedIndex = -1;
                    }
                    vm.SelectedIndex = i;
                    return;
                }
            }
        }
    }
}
