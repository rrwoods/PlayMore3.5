using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using PlayMore35.Global;

namespace PlayMore35.UI {
    public class WarningBadge : Label {
        private bool isWarningActive = false;

        public new bool Visible {
            get => isWarningActive;
            set {
                isWarningActive = value;
                base.Visible = value;
            }
        }

        [AuthorizedDomainApi("Inspection API for warning badge tooltip text")]
        public string ToolTipText => FormUtils.TOOLTIP.GetToolTip(this);

        [AuthorizedDomainApi("Inspection API for warning badge tooltip text")]
        public string GetTooltip() => ToolTipText;

        public WarningBadge() {
            Text = "⚠";
            AutoSize = true;
            ForeColor = Color.DarkGoldenrod;
            Cursor = Cursors.Help;
            Visible = false;
        }

        public void SetWarning(string message, WarningKind kind = WarningKind.Unmet) {
            if (kind == WarningKind.ItemDependent) {
                Text = "🛈";
                ForeColor = Color.DodgerBlue;
                string tooltip = !string.IsNullOrEmpty(message) ? $"{message}\n(Satisfied by currently equipped items)" : "";
                FormUtils.ToolTipTextLabel(this, tooltip);
            } else {
                Text = "⚠";
                ForeColor = Color.DarkGoldenrod;
                FormUtils.ToolTipTextLabel(this, message ?? "");
            }
            Visible = !string.IsNullOrEmpty(message);
        }

        public void SetWarnings(IEnumerable<ValidationWarning>? warnings) {
            if (warnings == null) {
                ClearWarning();
                return;
            }
            var list = warnings.ToList();
            if (list.Count == 0) {
                ClearWarning();
                return;
            }
            var worstKind = list.Any(w => w.Kind == WarningKind.Unmet)
                ? WarningKind.Unmet
                : WarningKind.ItemDependent;
            string message = string.Join("\n", list.Select(w => w.Message));
            SetWarning(message, worstKind);
        }

        public void ClearWarning() {
            Text = "⚠";
            ForeColor = Color.DarkGoldenrod;
            FormUtils.ToolTipTextLabel(this, "");
            Visible = false;
        }
    }
}
