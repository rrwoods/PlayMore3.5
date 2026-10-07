using PlayMore35.Global;
using static PlayMore35.Global.FormUtils;
using static PlayMore35.Effects.Constants;

using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using PlayMore35.Effects;

namespace PlayMore35 {
    public class HpPanel : DynamicPanel {
        private readonly Label hp, maxHp, dr;

        public HpPanel() {
            hp = new Label().Initialize();
            Controls.Add(hp);

            maxHp = new Label().Initialize();
            Controls.Add(maxHp);

            dr = new Label().Initialize();
            Controls.Add(dr);
        }

        public override void Initialize(MainForm mainForm) {
            base.Initialize(mainForm);
            FlowDirection = FlowDirection.LeftToRight;
            Margin = new Padding(0);
        }

        public override void Update(Dictionary<string, List<Text>> textEffects) {
            ICharacterSession session = mainForm.Session;
            LabelStat(hp, session, HP, CURRENT_HP, ";", null, false);
            LabelStat(maxHp, session, "max", MAX_HP, " (" + session.Character.classLevels.Count + " HD)", null, false);

            if (textEffects[DR].Any()) {
                maxHp.Text += ";";
                string drText = string.Join(", ", textEffects[DR].Select(x => x.Description));
                dr.Text = $"DR {drText}";
                dr.Visible = true;
            } else {
                dr.Visible = false;
            }
        }
    }
}
