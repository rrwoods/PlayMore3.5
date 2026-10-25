#nullable enable
using PlayMore35.Effects;
using PlayMore35.Global;
using System.Collections.Generic;
using System.Windows.Forms;

namespace PlayMore35 {
    public abstract class DynamicPanel : FlowLayoutPanel {
        public MainForm mainForm;

        public abstract void Update(Dictionary<string, List<Text>> textEffects);

        public virtual void Initialize(MainForm mainForm) {
            this.mainForm = mainForm;
            FlowDirection = FlowDirection.TopDown;
            AutoSize = true;
            Margin = FormUtils.SECTION_MARGIN;
        }
    }
}
