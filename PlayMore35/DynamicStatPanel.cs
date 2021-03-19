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

        public override void Update(Dictionary<string, List<(string name, string desc)>> textEffects) {
            Controller controller = mainForm.controller;
            (int baseValue, List<string> baseExpl) = controller.GetStatistic(BaseStatistic);
            IEnumerable<Statistic> specials = controller.Effects.RelevantStatsWithTag(BaseStatistic, controller.EquippedItems);
            bool any = false;
            bool relevant =
                specials.Any() ||
                controller.Effects.GetStat(BaseStatistic).Relevant(controller.Effects, controller.EquippedItems);
            bool shouldHideBase = HideBaseIfEquals == null
                ? false
                : baseValue == controller.GetStatistic(HideBaseIfEquals).value;
            if (relevant && !shouldHideBase) {
                LabelStat(baseLabel, baseValue, baseExpl, (IndentBase ? INDENT : "") + BaseStatistic, BaseStatistic);
                baseLabel.Visible = true;
                any = true;
            } else {
                baseLabel.Visible = false;
            }

            HashSet<string> inactiveSpecials = new HashSet<string>(specialLabels.Keys);
            foreach (Statistic special in specials) {
                if (special.Name == INITIATIVE) {
                    continue; // don't show initiative under dexterity.
                }
                (int value, List<string> expl) = special.GetValue(controller.Effects, controller.EquippedItems);
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
