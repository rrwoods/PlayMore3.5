using PlayMore35.Global;
using static PlayMore35.Global.FormUtils;
using static PlayMore35.Effects.Constants;

using System.Windows.Forms;
using System.Collections.Generic;

namespace PlayMore35 {
    public class AcPanel : DynamicPanel {
        private readonly Label ac, touch;

        public AcPanel() {
            ac = new Label().Initialize();
            Controls.Add(ac);

            touch = new Label().Initialize();
            Controls.Add(touch);
        }

        public override void Initialize(MainForm mainForm) {
            base.Initialize(mainForm);
            FlowDirection = FlowDirection.LeftToRight;
            Margin = new Padding(0);
        }

        public override void Update(Dictionary<string, List<(string name, string desc)>> textEffects) {
            Controller controller = mainForm.controller;
            LabelStat(ac, controller, AC, AC, ",", false);
            LabelStat(touch, controller, "touch", TOUCH_AC, "", false);
        }
    }
}
