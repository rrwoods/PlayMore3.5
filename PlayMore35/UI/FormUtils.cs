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
        private static readonly RollServer rollServer = new RollServer();
        private static RollPrompt rollPrompt = new RollPrompt();

        public static List<string> availableFeats;

        public static void ClearDynamicControls<T>(Control container, List<T> controls) where T : Control {
            foreach (T control in controls) {
                container.Controls.Remove(control);
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

        public static EventHandler RollStat(string dice, string description) {
            return (object sender, EventArgs e) => {
                rollServer.Roll(dice, description);
            };
        }

        public static void Rollable(Control label, string prefix, string diceText) {
            ToolStripMenuItem roll = new ToolStripMenuItem {
                Text = $"Roll {prefix} ({diceText})"
            };
            roll.Click += RollStat(diceText, prefix);
            label.ContextMenuStrip = new ContextMenuStrip();
            label.ContextMenuStrip.Items.Add(roll);
        }

        public static void ToolTipStatLabel(Control label, List<string> explanation) {
            TOOLTIP.SetToolTip(label, string.Join("\n", explanation));
        }

        public static void ToolTipTextLabel(Control label, string description) {
            TOOLTIP.SetToolTip(label, description);
        }

        public static Label LabelStat(Label label, int value, List<string> explanation, string prefix, string stat, string postfix = "", string rollPrefix = "", bool plus = true) {
            string statValue = ValueText(value, plus);

            label.Text = $"{prefix} {statValue}{postfix}";
            ToolTipStatLabel(label, explanation);

            if (rollPrefix != null) {
                if (!rollPrefix.Any()) {
                    rollPrefix = prefix;
                }
                Rollable(label, rollPrefix, $"1d20{value.ToString("+#;-#;\"\"")}");
            }

            return label;
        }

        public static int GetRollResult(string description) {
            rollPrompt.SetRollDescription(description);
            rollPrompt.ShowDialog();
            return rollPrompt.RollResult();
        }

        public static Label LabelStat(Label label, Controller controller, string prefix, string stat, string postfix = "", string rollPrefix = "", bool plus = true) {
            var (value, explanation) = controller.GetStatistic(stat);
            return LabelStat(label, value, explanation, prefix, stat, postfix, rollPrefix, plus);
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
