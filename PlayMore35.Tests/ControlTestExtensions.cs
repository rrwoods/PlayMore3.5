using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using PlayMore35.Global;
using PlayMore35.Items;
using PlayMore35.Subsystems;
using PlayMore35.UI;
using PlayMore35.UI.Subsystems;
using PlayMore35.UI.Subsystems.PactMagic;

namespace PlayMore35.Tests {
    public static class ControlTestExtensions {
        public static WarningBadge GetWarningBadge(this ElementReceptorControl receptor) =>
            receptor.Controls.OfType<WarningBadge>().First();

        public static string GetTooltip(this Control control) =>
            FormUtils.TOOLTIP.GetToolTip(control);

        public static void SetWarning(this ElementReceptorControl receptor, string message, WarningKind kind = WarningKind.Unmet) =>
            receptor.SetWarnings(new[] { new ValidationWarning(message, kind) });

        public static IReadOnlyList<string> GetFilteredElementNames(this DefaultElementPicker picker) {
            var listBox = picker.Controls.OfType<ListBox>().First();
            var names = new List<string>();
            foreach (var item in listBox.Items) {
                names.Add(item.ToString());
            }
            return names;
        }

        public static void SelectElementByName(this DefaultElementPicker picker, string name) {
            var listBox = picker.Controls.OfType<ListBox>().First();
            for (int i = 0; i < listBox.Items.Count; i++) {
                var str = listBox.Items[i].ToString();
                if (!string.Equals(str, "(None)", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(str, name, StringComparison.OrdinalIgnoreCase)) {
                    if (listBox.SelectedIndex == i) {
                        listBox.SelectedIndex = -1;
                    }
                    listBox.SelectedIndex = i;
                    return;
                }
            }
        }

        public static void SelectElementByName(this ISubsystemElementPicker picker, string name) {
            if (picker is DefaultElementPicker dep) {
                dep.SelectElementByName(name);
            } else if (picker is PactMagicElementPicker pm) {
                pm.SelectVestige(name);
            }
        }

        public static void SelectNone(this DefaultElementPicker picker) {
            var listBox = picker.Controls.OfType<ListBox>().First();
            for (int i = 0; i < listBox.Items.Count; i++) {
                if (string.Equals(listBox.Items[i].ToString(), "(None)", StringComparison.OrdinalIgnoreCase)) {
                    if (listBox.SelectedIndex == i) {
                        listBox.SelectedIndex = -1;
                    }
                    listBox.SelectedIndex = i;
                    return;
                }
            }
        }

        public static void SelectNone(this ISubsystemElementPicker picker) {
            if (picker is DefaultElementPicker dep) {
                dep.SelectNone();
            }
        }

        public static void DeliverVestigeByName(this PactMagicElementPicker picker, string name) =>
            picker.SelectVestige(name);

        public static void DeliverVestigeByName(this ISubsystemElementPicker picker, string name) {
            if (picker is PactMagicElementPicker pm) {
                pm.SelectVestige(name);
            }
        }

        public static bool GetCategoryChecked(this ConfigureAttackRoutinesDialog dialog, RoutineCategory category) {
            var text = ConfigureAttackRoutinesDialog.GetCategoryDisplayName(category);
            var chk = dialog.Controls.OfType<CheckBox>().FirstOrDefault(c => Equals(c.Tag, category) || c.Text == text);
            return chk?.Checked ?? false;
        }

        public static void SetCategoryChecked(this ConfigureAttackRoutinesDialog dialog, RoutineCategory category, bool isChecked) {
            var text = ConfigureAttackRoutinesDialog.GetCategoryDisplayName(category);
            var chk = dialog.Controls.OfType<CheckBox>().FirstOrDefault(c => Equals(c.Tag, category) || c.Text == text);
            if (chk != null) {
                chk.Checked = isChecked;
            }
        }

        public static LedgerRowControl? GetRow(this TabularLedgerPanel panel, int index) {
            var bodyPanel = panel.Controls.OfType<Panel>().FirstOrDefault(p => p.Controls.OfType<LedgerRowControl>().Any())
                ?? panel.Controls.OfType<Panel>().FirstOrDefault(p => p.BackColor == Color.Transparent);
            if (bodyPanel != null && index >= 0 && index < bodyPanel.Controls.Count) {
                return bodyPanel.Controls[index] as LedgerRowControl;
            }
            return null;
        }

        public static void SelectRow(this TabularLedgerPanel panel, int index) {
            var row = panel.GetRow(index);
            if (row == null) return;
            var onMouseClick = typeof(Control).GetMethod("OnMouseClick", BindingFlags.NonPublic | BindingFlags.Instance);
            if (onMouseClick != null) {
                onMouseClick.Invoke(row, new object[] { new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0) });
            }
            if (!row.IsSelected) {
                typeof(TabularLedgerPanel)
                    .GetMethod("SelectRowInternal", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.Invoke(panel, new object[] { row });
            }
        }
    }
}
