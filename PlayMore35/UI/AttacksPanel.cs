using PlayMore35.Global;
using static PlayMore35.Global.FormUtils;

using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using PlayMore35.Items;
using System.Text;
using PlayMore35.Effects;

namespace PlayMore35 {
    public class AttacksPanel : DynamicPanel {
        private readonly List<Label> labels = new List<Label>();

        public override void Initialize(MainForm mainForm) {
            base.Initialize(mainForm);
            FlowDirection = FlowDirection.LeftToRight;
        }

        private ToolStripMenuItem AttackRoll(string weapon, int bonus) {
            string modifier = bonus.ToString("+#;-#;\"\"");
            ToolStripMenuItem roll = new ToolStripMenuItem {
                Text = $"Roll {weapon} attack (1d20{modifier})"
            };
            roll.Click += RollStat($"1d20{modifier}", $"{weapon} attack");
            return roll;
        }

        private ContextMenuStrip DamageRoll(string weapon, string damage) {
            // TODO : this re-generates the menu each time.  is there a way to re-use the menu item instead?
            // the wonky thing here is the click event -- if we can re-use the click event but have
            // it somehow take into account what the current damage should be, we're good

            ContextMenuStrip menu = new ContextMenuStrip();
            ToolStripMenuItem roll = new ToolStripMenuItem {
                Text = $"Roll {weapon} damage ({damage})"
            };
            roll.Click += RollStat(damage, $"{weapon} damage");
            menu.Items.Add(roll);
            return menu;
        }

        private void AttackLabels(Attack attack, bool full, ref int i) {
            Label attackLabel;
            if (i < labels.Count) {
                attackLabel = labels[i];
            } else {
                attackLabel = new Label().Initialize();
                attackLabel.ContextMenuStrip = new ContextMenuStrip();
                labels.Add(attackLabel);
                Controls.Add(attackLabel);
            }

            StringBuilder text = new StringBuilder();
            if (full) {
                text.Append(INDENT);
            }
            text.Append(attack.RangeIncrement > 0 ? "Ranged " : "Melee ");
            text.Append(attack.WeaponName + " " + ValueText(attack.AttackBonus.bonus, true));
            attackLabel.ContextMenuStrip = new ContextMenuStrip();
            attackLabel.ContextMenuStrip.Items.Add(AttackRoll(attack.WeaponName, attack.AttackBonus.bonus));
            attackLabel.Text = text.ToString();
            SetFlowBreak(attackLabel, false);
            attackLabel.Visible = true;
            ToolTipStatLabel(attackLabel, attack.AttackBonus.explanation);
            i++;

            Label damageLabel;
            if (i < labels.Count) {
                damageLabel = labels[i];
            } else {
                damageLabel = new Label().Initialize();
                labels.Add(damageLabel);
                Controls.Add(damageLabel);
            }
            string damage = attack.Damage.dice.ToString();
            damageLabel.Text = $"({damage})";
            ToolTipStatLabel(damageLabel, attack.Damage.explanation);
            damageLabel.ContextMenuStrip = DamageRoll(attack.WeaponName, damage);
            SetFlowBreak(damageLabel, false);
            damageLabel.Visible = true;
            i++;

            Label critLabel;
            if (i < labels.Count) {
                critLabel = labels[i];
            } else {
                critLabel = new Label().Initialize();
                labels.Add(critLabel);
                Controls.Add(critLabel);
            }
            string crit = attack.CriticalDamage.dice.ToString();
            critLabel.Text = $"({attack.ThreatRange}{(attack.ThreatRange == 20 ? "" : "–20")}/{crit})";
            ToolTipStatLabel(critLabel, attack.CriticalDamage.explanation);
            critLabel.ContextMenuStrip = DamageRoll(attack.WeaponName + " critical", crit);
            SetFlowBreak(critLabel, true);
            critLabel.Visible = true;
            i++;
        }

        public override void Update(Dictionary<string, List<Text>> textEffects) {
            Controller controller = mainForm.controller;
            var visibleRoutines = controller.GetVisibleRoutines();

            var standardRoutines = visibleRoutines
                .Where(r => r.ActionType == RoutineActionType.Standard)
                .ToList();

            var fullRoutines = visibleRoutines
                .Where(r => r.ActionType == RoutineActionType.FullAttack)
                .ToList();

            int i = 0;
            foreach (AttackRoutine routine in standardRoutines) {
                foreach (Attack attack in routine.Attacks) {
                    AttackLabels(attack, false, ref i);
                }
            }

            foreach (AttackRoutine routine in fullRoutines) {
                if (routine.Attacks.Count <= 1) {
                    continue;
                }

                Label fullHeader;
                if (i < labels.Count) {
                    fullHeader = labels[i];
                } else {
                    fullHeader = new Label().Initialize();
                    labels.Add(fullHeader);
                    Controls.Add(fullHeader);
                }
                fullHeader.Text = string.IsNullOrEmpty(routine.DisplayName) ? "Full Attack" : routine.DisplayName;
                fullHeader.ContextMenuStrip = null;
                SetFlowBreak(fullHeader, true);
                fullHeader.Visible = true;
                i++;

                foreach (Attack attack in routine.Attacks) {
                    AttackLabels(attack, true, ref i);
                }
            }

            for (; i < labels.Count; i++) {
                labels[i].Visible = false;
            }
        }
    }
}
