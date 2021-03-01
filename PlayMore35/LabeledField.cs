using PlayMore35.Global;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PlayMore35 {
    class LabeledField : Panel {
        private const int LABEL_LEFT = 38;

        public Label lblName;
        public TextBox txtValue;

        public LabeledField(string name, Controller controller) : base() {
            AutoSize = true;

            txtValue = new TextBox {
                Location = new Point(0, 0),
                Size = new Size(32, 25),
                TextAlign = HorizontalAlignment.Center,
                BorderStyle = BorderStyle.None,
                BackColor = SystemColors.ActiveCaption,
            };
            txtValue.Leave += CheckValue(name, controller);
            Controls.Add(txtValue);

            lblName = new Label {
                AutoSize = true,
                Left = LABEL_LEFT,
                Text = name,
                Anchor = AnchorStyles.Left,
                TextAlign = ContentAlignment.MiddleLeft
            };
            Controls.Add(lblName);
        }

        public int Value {
            get {
                return txtValue.Text.Any()
                    ? int.Parse(txtValue.Text)
                    : 0;
            }
            set {
                txtValue.Text = "" + value;
            }
        }

        private EventHandler CheckValue(string name, Controller controller) {
            return (object sender, EventArgs e) => {
                TextBox txt = (TextBox)sender;
                if (!txt.Text.Any()) {
                    controller.Effects.controllableParameters[name].Value = 0;
                    controller.Character.paramValues[name] = 0;
                } else if (int.TryParse(txt.Text, out int value)) {
                    controller.Effects.controllableParameters[name].Value = value;
                    controller.Character.paramValues[name] = value;
                } else {
                    controller.Effects.controllableParameters[name].Value = 0;
                    controller.Character.paramValues[name] = 0;
                    txt.Text = "";
                }
                ((MainForm)txt.Parent.Parent.Parent.Parent).RefreshEffects();
            };
        }
    }
}
