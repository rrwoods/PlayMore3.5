using System.Linq;
using System.Windows.Forms;
using PlayMore35.Global;
using PlayMore35.UI;
using PlayMore35.UI.Subsystems;

namespace PlayMore35.Tests {
    public static class ControlTestExtensions {
        public static WarningBadge GetWarningBadge(this ElementReceptorControl receptor) =>
            receptor.Controls.OfType<WarningBadge>().First();

        public static string GetTooltip(this Control control) =>
            FormUtils.TOOLTIP.GetToolTip(control);
    }
}
