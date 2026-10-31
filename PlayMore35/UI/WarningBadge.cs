using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using PlayMore35.Global;

namespace PlayMore35.UI {
    public class WarningBadge : Label {
        public string? WarningMessage { get; private set; }
        public WarningKind Kind { get; private set; } = WarningKind.Unmet;
        private bool isWarningActive = false;

        public new bool Visible {
            get => isWarningActive;
            set {
                isWarningActive = value;
                base.Visible = value;
            }
        }

        public WarningBadge() {
            Text = "⚠";
            AutoSize = true;
            ForeColor = Color.DarkGoldenrod;
            Cursor = Cursors.Help;
            Visible = false;
        }

        public void SetWarning(string message, WarningKind kind = WarningKind.Unmet) {
            WarningMessage = message;
            Kind = kind;
            if (kind == WarningKind.ItemDependent) {
                Text = "🛈";
                ForeColor = Color.DodgerBlue;
            } else {
                Text = "⚠";
                ForeColor = Color.DarkGoldenrod;
            }
            FormUtils.ToolTipTextLabel(this, message ?? "");
            Visible = !string.IsNullOrEmpty(message);
        }

        public void SetWarning(ValidationWarning warning) {
            if (warning == null) {
                ClearWarning();
                return;
            }
            SetWarning(warning.Message, warning.Kind);
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
            WarningMessage = null;
            Kind = WarningKind.Unmet;
            Text = "⚠";
            ForeColor = Color.DarkGoldenrod;
            FormUtils.ToolTipTextLabel(this, "");
            Visible = false;
        }
    }
}
