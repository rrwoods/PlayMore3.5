using PlayMore35.Global;
using static PlayMore35.Global.FormUtils;
using static PlayMore35.Effects.Constants;

using System.Windows.Forms;
using System.Collections.Generic;
using PlayMore35.Effects;

namespace PlayMore35 {
    public class SpeedPanel : DynamicPanel {
        private readonly Label speed = new Label().Initialize();
        private readonly TextEffectPanel effects = new TextEffectPanel();

        public override void Initialize(MainForm mainForm) {
            base.Initialize(mainForm);

            Controls.Add(speed);

            effects.DisplayStyle = TextEffectPanel.DisplayMode.MULTI_LINE_WITHOUT_HEADER;
            effects.Tag = MOVEMENT;
            effects.Initialize(mainForm);
            Controls.Add(effects);
            effects.Margin = new Padding(0);
        }

        public override void Update(Dictionary<string, List<Text>> textEffects) {
            LabelStat(speed, mainForm.Session, "Speed", LAND_SPEED, " ft.", null, false);
            effects.Update(textEffects);
        }
    }
}
