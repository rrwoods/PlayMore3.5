#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PlayMore35.UI {
    public partial class RollPrompt : Form {
        public RollPrompt() {
            InitializeComponent();
        }

        public void SetRollDescription(string description) {
            Text = description;
        }

        public int RollResult() {
            return int.Parse(mskResult.Text);
        }

        private void Result_TextChanged(object sender, EventArgs e) {
            btnOK.Enabled = (mskResult.Text != "");
        }
    }
}
