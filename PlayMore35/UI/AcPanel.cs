using PlayMore35.Global;
using static PlayMore35.Global.FormUtils;
using static PlayMore35.Effects.Constants;

using System.Windows.Forms;
using System.Collections.Generic;
using PlayMore35.Effects;

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

        public override void Update(Dictionary<string, List<Text>> textEffects) {
            ICharacterSession session = mainForm.Session;
            LabelStat(ac, session, AC, AC, ",", false);
            LabelStat(touch, session, "touch", TOUCH_AC, "", false);
        }
    }
}
