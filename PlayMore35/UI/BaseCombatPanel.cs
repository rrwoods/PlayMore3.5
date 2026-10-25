#nullable enable
using PlayMore35.Global;
using static PlayMore35.Global.FormUtils;
using static PlayMore35.Effects.Constants;

using System.Windows.Forms;
using System.Collections.Generic;
using PlayMore35.Effects;

namespace PlayMore35 {
    public class BaseCombatPanel : DynamicPanel {
        private readonly Label space = new Label().Initialize();
        private readonly Label reach = new Label().Initialize();
        private readonly Label bab = new Label().Initialize();
        private readonly Label grp = new Label().Initialize();

        public override void Initialize(MainForm mainForm) {
            base.Initialize(mainForm);
            FlowDirection = FlowDirection.LeftToRight;

            Controls.Add(space);
            Controls.Add(reach);
            SetFlowBreak(reach, true);

            Controls.Add(bab);
            Controls.Add(grp);
        }

        public override void Update(Dictionary<string, List<Text>> textEffects) {
            LabelStat(space, mainForm.Session, SPACE, SPACE, " ft.;", false);
            LabelStat(reach, mainForm.Session, REACH, REACH, " ft.", false);
            LabelStat(bab, mainForm.Session, "Base Atk", BASE_ATTACK_BONUS, ";");
            LabelStat(grp, mainForm.Session, "Grp", GRAPPLE);
        }
    }
}
