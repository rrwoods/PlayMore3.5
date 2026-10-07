using PlayMore35.Global;
using PlayMore35.Effects;
using static PlayMore35.Global.FormUtils;
using static PlayMore35.Effects.Constants;

using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;

namespace PlayMore35 {
    public class DynamicStatPanel : DynamicPanel {
        private readonly Label baseLabel = new Label().Initialize();
        private readonly Dictionary<string, Label> specialLabels = new Dictionary<string, Label>();

        public string BaseStatistic;
        public bool IndentBase = false;
        public string HideBaseIfEquals = null;

        public override void Initialize(MainForm mainForm) {
            base.Initialize(mainForm);
            Margin = new Padding(0);
            Controls.Add(baseLabel);
        }

        public override void Update(Dictionary<string, List<Text>> textEffects) {
            ICharacterSession session = mainForm.Session;
            StatView baseStat = session.GetStatistic(BaseStatistic);
            int baseValue = baseStat.Value;
            var baseExpl = baseStat.Explanation;
            var specials = session.GetRelevantStats(BaseStatistic);
            bool any = false;
            bool relevant = specials.Any() || session.IsStatRelevant(BaseStatistic);
            bool shouldHideBase = HideBaseIfEquals == null
                ? false
                : baseValue == session.GetStatistic(HideBaseIfEquals).Value;
            if (relevant && !shouldHideBase) {
                LabelStat(baseLabel, baseValue, baseExpl, (IndentBase ? INDENT : "") + BaseStatistic, BaseStatistic);
                baseLabel.Visible = true;
                any = true;
            } else {
                baseLabel.Visible = false;
            }

            HashSet<string> inactiveSpecials = new HashSet<string>(specialLabels.Keys);
            foreach (StatView special in specials) {
                if (special.Name == INITIATIVE) {
                    continue; // don't show initiative under dexterity.
                }
                int value = special.Value;
                var expl = special.Explanation;
                if (value == baseValue) {
                    continue;
                }

                Label specialLabel;
                if (specialLabels.ContainsKey(special.Name)) {
                    specialLabel = specialLabels[special.Name];
                    inactiveSpecials.Remove(special.Name);
                    specialLabel.Visible = true;
                } else {
                    specialLabel = new Label().Initialize();
                    specialLabels[special.Name] = specialLabel;
                    Controls.Add(specialLabel);
                }
                LabelStat(specialLabel, value, expl, INDENT + special.Name, special.Name);
                any = true;
            }
            foreach (string inactive in inactiveSpecials) {
                specialLabels[inactive].Visible = false;
            }

            Visible = any;
        }
    }
}
