using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static PlayMore35.GameRules.Constants;

namespace PlayMore35.Items {
    public class Weapon {
        public string Name = "Dagger";

        public List<Attack> Attacks = new List<Attack>();

        public List<string> Tags = new List<string>();

        // Call this function *after* setting up all the attacks and tags for the weapon!
        // Set finesse to true if the character has the Weapon Finesse feat
        public void Register(GameRules.GameRules rules, bool finesse) {
            foreach (Attack attack in Attacks) {
                string range = (attack.IsRanged() ? RANGED : MELEE);
                Effects.Statistic attackStat = new Effects.Statistic(range + ATTACK_ROLLS + WITH + Name)
                    .AddTag(ATTACK_ROLLS)
                    .AddTag(range + ATTACK_ROLLS)
                    .AddTag(ATTACK_ROLLS + WITH + Name);
                Effects.Statistic damageStat = new Effects.Statistic(range + DAMAGE_ROLLS + WITH + Name)
                    .AddTag(DAMAGE_ROLLS)
                    .AddTag(range + DAMAGE_ROLLS)
                    .AddTag(DAMAGE_ROLLS + WITH + Name);

                foreach (string tag in Tags) {
                    attackStat
                        .AddTag(ATTACK_ROLLS + WITH + tag + WEAPONS)
                        .AddTag(range + ATTACK_ROLLS + WITH + tag + WEAPONS);
                    damageStat
                        .AddTag(DAMAGE_ROLLS + WITH + tag + WEAPONS)
                        .AddTag(range + DAMAGE_ROLLS + WITH + tag + WEAPONS);
                }

                rules.AddStat(attackStat);
                rules.AddStat(damageStat);

                // TODO : Extra attacks (and iteratives, maybe via extra attacks)
                rules.Rules.AddEffect(new Effects.Effect("Base attack bonus for " + Name)
                    .AddModifier(new Effects.Modifier(rules.Stats[BASE_ATTACK_BONUS], UNTYPED, attackStat.Name))
                );

                if (attack.IsRanged()) {
                    rules.Rules.AddEffect(new Effects.Effect("DEX modifier for " + Name + " attacks")
                        .AddModifier(new Effects.Modifier(rules.Stats[DEX_MODIFIER], UNTYPED, attackStat.Name))
                    );
                    if (attack.RangeCategory == Attack.Range.THROWN) {
                        rules.Rules.AddEffect(new Effects.Effect("STR modifier for " + Name + " damage")
                            .AddModifier(new Effects.Modifier(rules.Stats[STR_MODIFIER], UNTYPED, damageStat.Name))
                        );
                    } else if (attack.StrengthRating >= 0) {
                        rules.Rules.AddEffect(new Effects.Effect("Insufficient strength for " + Name)
                            .AddModifier(new Effects.Modifier(-2, UNTYPED, attackStat.Name))
                            .SetToggle(new Effects.ConditionalToggle(rules.Stats[STR_MODIFIER], Effects.Relation.LESS_THAN, attack.StrengthRating))
                        );
                        rules.Rules.AddEffect(new Effects.Effect("STR modifier for " + Name + " damage")
                            .AddModifier(new Effects.Modifier(
                                new Effects.LesserAmount(
                                    new Effects.VariableAmount(rules.Stats[STR_MODIFIER]),
                                    new Effects.FixedAmount(attack.StrengthRating)
                                ),
                                UNTYPED,
                                RANGED + DAMAGE_ROLLS + WITH + Name
                            ))
                        );
                    } else {
                        rules.Rules.AddEffect(new Effects.Effect("STR penalty for " + Name + " damage")
                            .AddModifier(new Effects.Modifier(rules.Stats[STR_PENALTY], UNTYPED, damageStat.Name))
                        );
                    }
                } else {
                    rules.Rules.AddEffect(new Effects.Effect("STR modifier for " + Name + " attacks")
                        .AddModifier(new Effects.Modifier(rules.Stats[STR_MODIFIER], UNTYPED, attackStat.Name))
                    );

                    float damageFactor = 1f;
                    if (attack.EffortCategory == Attack.Effort.TWO_HANDED) {
                        damageFactor = 1.5f;
                    } else if (attack.OffHand) {
                        damageFactor = 0.5f;
                    }
                    rules.Rules.AddEffect(new Effects.Effect("STR bonus for " + Name + " damage")
                        .AddModifier(new Effects.Modifier(
                            new Effects.VariableAmount(rules.Stats[STR_BONUS], damageFactor), UNTYPED, damageStat.Name
                        ))
                    );
                }
            }
        }
    }
}
