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

        private static T GetField<T>(object target, string fieldName) {
            var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null) {
                throw new InvalidOperationException($"Field '{fieldName}' not found on type '{target.GetType().FullName}'.");
            }
            return (T)field.GetValue(target)!;
        }

        public static Panel GetDetailsPane(this DefaultElementPicker picker) =>
            GetField<Panel>(picker, "_detailsPanel");

        public static Button GetDetailsToggleButton(this DefaultElementPicker picker) =>
            GetField<Button>(picker, "_btnToggleDetails");

        public static bool GetIsDetailsExpanded(this DefaultElementPicker picker) =>
            GetField<bool>(picker, "_isDetailsExpanded");

        public static string GetDetailsText(this DefaultElementPicker picker) =>
            GetField<Label>(picker, "_lblDetails").Text;

        public static void SetSearchFilterText(this DefaultElementPicker picker, string text) {
            GetField<TextBox>(picker, "_searchBox").Text = text;
        }

        public static void SetHideInvalidChoices(this DefaultElementPicker picker, bool hide) {
            GetField<CheckBox>(picker, "_hideInvalidCheckBox").Checked = hide;
        }

        public static void ToggleDetailsPane(this DefaultElementPicker picker) {
            picker.GetDetailsToggleButton().PerformClick();
        }

        public static ReceptorState GetState(this ElementReceptorControl receptor) {
            if (receptor.IsActiveSelecting) return ReceptorState.ActiveSelecting;
            return receptor.SelectedElement != null ? ReceptorState.Made : ReceptorState.Unmade;
        }

        public static bool IsClearButtonVisible(this ElementReceptorControl receptor) =>
            GetField<Button>(receptor, "_clearButton").Visible;

        public static bool IsWarningBadgeVisible(this ElementReceptorControl receptor) =>
            receptor.GetWarningBadge().Visible;

        public static string GetDisplayText(this ElementReceptorControl receptor) =>
            GetField<Label>(receptor, "_label").Text;

        public static IReadOnlyList<VestigeCardControl> GetCards(this PactMagicElementPicker picker) =>
            GetField<List<VestigeCardControl>>(picker, "_cards");

        public static int GetVisibleCardsCount(this PactMagicElementPicker picker) =>
            picker.GetCards().Count(c => c.Visible && !c.IsBound);

        public static FlowLayoutPanel GetPinnedContainer(this PactMagicElementPicker picker) =>
            GetField<FlowLayoutPanel>(picker, "_pinnedContainer");

        public static FlowLayoutPanel GetCardsContainer(this PactMagicElementPicker picker) =>
            GetField<FlowLayoutPanel>(picker, "_cardsContainer");

        public static string GetSearchQuery(this PactMagicElementPicker picker) =>
            GetField<TextBox>(picker, "_searchBox").Text;

        public static void SetSearchQuery(this PactMagicElementPicker picker, string query) {
            GetField<TextBox>(picker, "_searchBox").Text = query;
        }

        public static bool GetHideUnavailable(this PactMagicElementPicker picker) =>
            GetField<CheckBox>(picker, "_hideUnavailableCheckBox").Checked;

        public static void SetHideUnavailable(this PactMagicElementPicker picker, bool hide) {
            GetField<CheckBox>(picker, "_hideUnavailableCheckBox").Checked = hide;
        }

        public static int GetSlotsColumnWidth(this PactMagicTabPage page) =>
            GetField<Panel>(page, "_leftColumn").Width;

        public static PactMagicElementPicker GetPicker(this PactMagicTabPage page) =>
            GetField<PactMagicElementPicker>(page, "_picker");

        public static string GetDetailsHeader(this PactMagicTabPage page) =>
            GetField<Label>(page, "_lblDetailsTitle").Text;

        public static string GetDetailsSignText(this PactMagicTabPage page) =>
            GetField<Label>(page, "_lblDetailsSign").Text;

        public static string GetDetailsInfluenceText(this PactMagicTabPage page) =>
            GetField<Label>(page, "_lblDetailsInfluence").Text;

        public static string GetDetailsAbilitiesText(this PactMagicTabPage page) =>
            GetField<Label>(page, "_lblDetailsAbilities").Text;

        public static bool GetIsEmptyPromptVisible(this PactMagicTabPage page) =>
            GetField<Label>(page, "_lblEmptyPrompt").Visible;

        public static Button GetGoodPactButton(this VestigeCardControl card) =>
            GetField<Button>(card, "_btnGoodPact");

        public static Button GetPoorPactButton(this VestigeCardControl card) =>
            GetField<Button>(card, "_btnPoorPact");

        public static Button GetUnbindButton(this VestigeCardControl card) =>
            GetField<Button>(card, "_btnUnbind");

        public static Label GetStatusLabel(this VestigeCardControl card) =>
            GetField<Label>(card, "_lblStatus");

        public static Label GetCapacityNoticeLabel(this VestigeCardControl card) =>
            GetField<Label>(card, "_lblCapacityNotice");

        public static string GetStatusToolTip(this VestigeCardControl card) =>
            card.GetStatusLabel().GetTooltip();
    }
}
