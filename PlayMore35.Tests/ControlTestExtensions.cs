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

namespace PlayMore35.Tests {
    public static class ControlTestExtensions {
        public static string GetTooltip(this Control control) =>
            FormUtils.TOOLTIP.GetToolTip(control);

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

        public static int GetHeaderTop(this TabularLedgerPanel panel) =>
            GetField<Panel>(panel, "headerPanel").Top;

        public static int GetBodyTop(this TabularLedgerPanel panel) =>
            GetField<Panel>(panel, "bodyPanel").Top;

        public static int GetVerticalScrollValue(this TabularLedgerPanel panel) =>
            GetField<Panel>(panel, "bodyPanel").VerticalScroll.Value;

        public static int GetRowCount(this TabularLedgerPanel panel) =>
            GetField<List<LedgerRowControl>>(panel, "rows").Count;

        public static string GetGameTimeText(this LedgerRowControl row) =>
            GetField<Label>(row, "lblGameTime").Text;

        public static string GetRealDateText(this LedgerRowControl row) =>
            GetField<Label>(row, "lblRealDate").Text;

        public static string GetDescriptionText(this LedgerRowControl row) =>
            GetField<Label>(row, "lblDescription").Text;

        public static string GetAmountText(this LedgerRowControl row) =>
            GetField<Label>(row, "lblAmount").Text;

        public static Panel GetPickerHost(this LevelDetailsDialog dialog) =>
            dialog.Controls.OfType<Panel>().First(p => p.Name == "pnlPickerHost");

        public static IReadOnlyList<Label> GetSkillLabels(this SkillTextSectionPanel panel) =>
            GetField<List<Label>>(panel, "skillLabels").AsReadOnly();

        public static Label GetHeaderLabel(this SkillTextSectionPanel panel) =>
            GetField<Label>(panel, "headerLabel");

        public static TextEffectPanel GetEffectsPanel(this SkillTextSectionPanel panel) =>
            GetField<TextEffectPanel>(panel, "effectsPanel");

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
    }
}
