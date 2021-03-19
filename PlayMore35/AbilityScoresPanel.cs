using PlayMore35.Global;
using static PlayMore35.Global.FormUtils;
using static PlayMore35.Effects.Constants;

using System.Windows.Forms;
using System.Collections.Generic;

namespace PlayMore35 {
    public class AbilityScoresPanel : DynamicPanel {
        private readonly List<(Label score, Label mod, DynamicStatPanel checks)> abilities = new List<(Label score, Label mod, DynamicStatPanel checks)>();
        private static readonly string[] ABILITY_NAMES = new string[] { STR, DEX, CON, INT, WIS, CHA };

        public override void Initialize(MainForm mainForm) {
            base.Initialize(mainForm);
            FlowDirection = FlowDirection.LeftToRight;

            foreach (string ability in ABILITY_NAMES) {
                Label score = new Label().Initialize();
                Label mod = new Label().Initialize();
                DynamicStatPanel checks = new DynamicStatPanel {
                    BaseStatistic = $"{ability} checks",
                    IndentBase = true,
                    HideBaseIfEquals = $"{ability} modifier",
                };
                checks.Initialize(mainForm);

                abilities.Add((score, mod, checks));
                Controls.Add(score);
                Controls.Add(mod);
                SetFlowBreak(mod, true);
                Controls.Add(checks);
                SetFlowBreak(checks, true);
            }
        }

        public override void Update(Dictionary<string, List<(string name, string desc)>> textEffects) {
            Controller controller = mainForm.controller;
            for (int i = 0; i < ABILITY_NAMES.Length; i++) {
                string ability = ABILITY_NAMES[i];
                LabelStat(abilities[i].score, controller, ability, $"{ability} score", "", false);
                LabelStat(abilities[i].mod, controller, "(", $"{ability} modifier", " )");
                abilities[i].checks.Update(textEffects);
            }
        }
    }
}
