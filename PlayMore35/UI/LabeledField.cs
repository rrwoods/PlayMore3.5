using PlayMore35.Global;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PlayMore35 {
    class LabeledField : FlowLayoutPanel {
        public Label lblName;
        public TextBox txtValue;

        public LabeledField(string name, MainForm mainForm, string displayName = null) : base() {
            AutoSize = true;

            if (displayName == null) {
                displayName = name;
            }
            txtValue = new TextBox {
                Size = new Size(32, 25),
                TextAlign = HorizontalAlignment.Center,
                BorderStyle = BorderStyle.None,
                BackColor = SystemColors.ActiveCaption,
                Margin = new Padding(3, 0, 0, 0),
            }.Initialize();
            txtValue.Leave += CheckValue(name, mainForm);
            Controls.Add(txtValue);

            lblName = new Label {
                AutoSize = true,
                Text = displayName,
                Anchor = AnchorStyles.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0),
            }.Initialize();
            Controls.Add(lblName);

            Margin = new Padding(0);
        }

        public int Value {
            get {
                return txtValue.Text.Any()
                    ? int.Parse(txtValue.Text)
                    : 0;
            }
            set {
                txtValue.Text = value == 0 ? "" : ("" + value);
            }
        }

        private EventHandler CheckValue(string name, MainForm mainForm) {
            return (object sender, EventArgs e) => {
                Controller controller = mainForm.controller;
                TextBox txt = (TextBox)sender;
                if (!txt.Text.Any()) {
                    controller.Effects.controllableParameters[name].Value = 0;
                } else if (int.TryParse(txt.Text, out int value)) {
                    controller.Effects.controllableParameters[name].Value = value;
                } else {
                    controller.Effects.controllableParameters[name].Value = 0;
                    txt.Text = "";
                }
                mainForm.RefreshEffects();
            };
        }
    }
}
