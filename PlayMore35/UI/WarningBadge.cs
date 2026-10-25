#nullable enable
using System;
using System.Drawing;
using System.Windows.Forms;
using PlayMore35.Global;

namespace PlayMore35.UI {
    public class WarningBadge : Label {
        public string WarningMessage { get; private set; }
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

        public void SetWarning(string message) {
            WarningMessage = message;
            FormUtils.ToolTipTextLabel(this, message ?? "");
            Visible = !string.IsNullOrEmpty(message);
        }

        public void ClearWarning() {
            WarningMessage = null;
            FormUtils.ToolTipTextLabel(this, "");
            Visible = false;
        }
    }
}
