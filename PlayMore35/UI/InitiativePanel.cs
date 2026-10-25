#nullable enable
using PlayMore35.Global;
using static PlayMore35.Global.FormUtils;
using static PlayMore35.Effects.Constants;

using System.Windows.Forms;
using System.Collections.Generic;
using PlayMore35.Effects;

namespace PlayMore35 {
    public class InitiativePanel : DynamicPanel {
        private readonly Label initiative;

        public InitiativePanel() {
            initiative = new Label().Initialize();
            Controls.Add(initiative);
        }

        public override void Update(Dictionary<string, List<Text>> textEffects) {
            LabelStat(initiative, mainForm.Session, INITIATIVE, INITIATIVE);
        }
    }
}
