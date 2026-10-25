#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Linq;
using PlayMore35.UI;

namespace PlayMore35.Global {
    public class FormUtils {
        public const string INDENT = "     ";
        public static readonly Padding SECTION_MARGIN = new Padding(0, 0, 0, 12);
        public static readonly ToolTip TOOLTIP = new ToolTip {
            AutoPopDelay = 32767 // maximum value.
        };
        private static RollPrompt rollPrompt;

        public static List<string> availableFeats;

        public static void ClearDynamicControls<T>(Control container, List<T> controls) where T : Control {
            foreach (T control in controls) {
                container.Controls.Remove(control);
            }
            controls.Clear();
        }

        public static void ClearDynamicControls<K, T>(Control container, Dictionary<K, T> controls) where T : Control {
            foreach (var entry in controls) {
                container.Controls.Remove(entry.Value);
            }
            controls.Clear();
        }

        public static void ClearDynamicControls<K, T>(Control container, Dictionary<K, List<T>> controls) where T : Control {
            foreach (var category in controls) {
                foreach (Control control in category.Value) {
                    container.Controls.Remove(control);
                }
            }
            controls.Clear();
        }

        public static string ValueText(int value, bool plus) {
            return ((plus && value >= 0 ? "+" : "") + value).Replace("-", "–");
        }

        private static readonly Dictionary<string, string> customShortNames = new Dictionary<string, string> {
            ["Control Shape"] = "CS",
            ["Decipher Script"] = "DS",
            ["Disable Device"] = "DD",
            ["Gather Information"] = "Info",
            ["Knowledge (arcana)"] = "K:arc",
            ["Knowledge (architecture and engineering)"] = "K:eng",
            ["Knowledge (dungeoneering)"] = "K:dun",
            ["Knowledge (geography)"] = "K:geo",
            ["Knowledge (history)"] = "K:his",
            ["Knowledge (local)"] = "K:loc",
            ["Knowledge (nature)"] = "K:nat",
            ["Knowledge (nobility and royalty)"] = "K:nob",
            ["Knowledge (psionics)"] = "K:psi",
            ["Knowledge (religion)"] = "K:rel",
            ["Knowledge (the planes)"] = "K:pla",
            ["Martial Lore"] = "ML",
            ["Move Silently"] = "MS",
            ["Open Lock"] = "Open",
            ["Sleight Of Hand"] = "SOH",
            ["Speak Language"] = "Lang",
            ["Use Magic Device"] = "UMD",
            ["Use Psionic Device"] = "UPD",
            ["Use Rope"] = "Rope"
        };

        public static string ShortName(string skill) {
            if (skill == null) return "";
            if (customShortNames.TryGetValue(skill, out string customName)) {
                return customName;
            }
            if (skill.Count() == 4) {
                return skill;
            }
            return skill.Length >= 3 ? skill.Substring(0, 3) : skill;
        }

        public static void ToolTipStatLabel(Control label, IEnumerable<string> explanation) {
            TOOLTIP.SetToolTip(label, explanation != null ? string.Join("\n", explanation) : "");
        }

        public static void ToolTipTextLabel(Control label, string description) {
            TOOLTIP.SetToolTip(label, description);
        }

        public static Label LabelStat(Label label, int value, IEnumerable<string> explanation, string prefix, string stat, string postfix = "", bool plus = true) {
            string statValue = ValueText(value, plus);

            label.Text = $"{prefix} {statValue}{postfix}";
            ToolTipStatLabel(label, explanation);

            return label;
        }

        public static int GetRollResult(string description) {
            if (rollPrompt == null) {
                rollPrompt = new RollPrompt();
            }
            rollPrompt.SetRollDescription(description);
            rollPrompt.ShowDialog();
            return rollPrompt.RollResult();
        }

        public static Label LabelStat(Label label, ICharacterSession session, string prefix, string stat, string postfix = "", bool plus = true) {
            var statView = session.GetStatistic(stat);
            return LabelStat(label, statView.Value, statView.Explanation, prefix, stat, postfix, plus);
        }
    }

    static class LabelExtensions {
        // Setting the max width of a tooltip doesn't have a convenient API, but the tooltip control *can* set its
        // maximum width by receiving a certain WinAPI message.
        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hwnd, uint msg, int wParam, int lParam);
        private const uint SET_MAX_TOOLTIP_WIDTH_MSG = 0x418;

        private static void ResizeTooltip(object sender, EventArgs e) {
            IntPtr hwnd = (IntPtr)typeof(ToolTip).InvokeMember(
                "Handle",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.GetProperty,
                null,
                FormUtils.TOOLTIP,
                null
            );
            SendMessage(hwnd, SET_MAX_TOOLTIP_WIDTH_MSG, 0, 300);
        }

        public static T Initialize<T>(this T label) where T : Control {
            label.AutoSize = true;
            label.MouseEnter += ResizeTooltip;
            return label;
        }
    }
}
