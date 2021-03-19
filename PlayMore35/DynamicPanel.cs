using PlayMore35.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PlayMore35 {
    public abstract class DynamicPanel : FlowLayoutPanel {
        public MainForm mainForm;

        public abstract void Update(Dictionary<string, List<(string name, string desc)>> textEffects);

        public virtual void Initialize(MainForm mainForm) {
            this.mainForm = mainForm;
            FlowDirection = FlowDirection.TopDown;
            AutoSize = true;
            Margin = FormUtils.SECTION_MARGIN;
        }
    }
}
