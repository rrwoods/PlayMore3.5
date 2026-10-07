using PlayMore35.Global;
using static PlayMore35.Global.FormUtils;
using static PlayMore35.Effects.Constants;

using System.Windows.Forms;
using System.Collections.Generic;
using PlayMore35.Effects;

using System.Linq;

namespace PlayMore35 {
    internal class AbilityDisplay : TableLayoutPanel {
        public readonly Label score = new Label().Initialize();
        public readonly Label mod = new Label().Initialize();
        public readonly LabeledField damage, drain;

        public AbilityDisplay(MainForm mainForm, string ability) {
            AutoSize = true;
            Margin = new Padding(0);

            RowCount = 1;
            ColumnCount = 4;
            RowStyles.Add(new RowStyle(SizeType.AutoSize));

            string damageName = $"{ability} damage";
            damage = new LabeledField(damageName, mainForm);
            var damageParam = mainForm.Session.GetControllableParameters().FirstOrDefault(p => p.Name == damageName);
            ToolTipTextLabel(damage.txtValue, damageParam?.Description ?? "");
            damage.Controls.Remove(damage.lblName);

            string drainName = $"{ability} drain";
            drain = new LabeledField(drainName, mainForm);
            var drainParam = mainForm.Session.GetControllableParameters().FirstOrDefault(p => p.Name == drainName);
            ToolTipTextLabel(drain.txtValue, drainParam?.Description ?? "");
            drain.Controls.Remove(drain.lblName);

            Controls.Add(score, 0, 0);
            ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));
            Controls.Add(mod, 1, 0);
            ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 55));
            Controls.Add(damage, 2, 0);
            Controls.Add(drain, 3, 0);
        }
    }

    public class AbilityScoresPanel : DynamicPanel {
        private readonly List<AbilityDisplay> abilities = new List<AbilityDisplay>();
        private readonly List<DynamicStatPanel> checks = new List<DynamicStatPanel>();
        private static readonly string[] ABILITY_NAMES = new string[] { STR, DEX, CON, INT, WIS, CHA };

        public override void Initialize(MainForm mainForm) {
            base.Initialize(mainForm);

            foreach (string ability in ABILITY_NAMES) {
                AbilityDisplay display = new AbilityDisplay(mainForm, ability);
                abilities.Add(display);
                Controls.Add(display);

                DynamicStatPanel check = new DynamicStatPanel {
                    BaseStatistic = $"{ability} checks",
                    IndentBase = true,
                    HideBaseIfEquals = $"{ability} modifier",
                };
                check.Initialize(mainForm);
                checks.Add(check);
                Controls.Add(check);
            }
        }

        public override void Update(Dictionary<string, List<Text>> textEffects) {
            ICharacterSession session = mainForm.Session;
            for (int i = 0; i < ABILITY_NAMES.Length; i++) {
                string ability = ABILITY_NAMES[i];
                LabelStat(abilities[i].score, session, ability, $"{ability} score", "", null, false);
                LabelStat(abilities[i].mod, session, "(", $"{ability} modifier", " )", $"{ability} check");

                string damageName = $"{ability} damage";
                if (session.Character.paramValues.ContainsKey(damageName)) {
                    abilities[i].damage.Value = session.Character.paramValues[damageName];
                }
                string drainName = $"{ability} drain";
                if (session.Character.paramValues.ContainsKey(drainName)) {
                    abilities[i].drain.Value = session.Character.paramValues[drainName];
                }

                checks[i].Update(textEffects);
            }
        }
    }
}
