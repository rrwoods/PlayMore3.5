using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static PlayMore35.GameRules.Constants;

namespace PlayMore35.Items {
    public class Weapon {
        public class Attack {
            private readonly Weapon W;

            public Attack(Weapon w) {
                W = w;
            }

            public enum Type { MANUFACTURED, PRIMARY_NATURAL, SECONDARY_NATURAL };
            public enum Proficiency { SIMPLE, MARTIAL, EXOTIC };
            public enum Effort { LIGHT, ONE_HANDED, TWO_HANDED };
            public enum Range { MELEE, REACH_MELEE, RANGED };

            public Type AttackType = Type.MANUFACTURED;
            public Proficiency ProficencyCategory = Proficiency.SIMPLE;
            public Effort EffortCategory = Effort.LIGHT;
            public Range RangeCategory = Range.MELEE;
            public bool Finesse = false; // true if the Weapon Finesse feat applies to this attack

            public string DamageDieMedium = "1d4";
            public int ThreatRange = 19;
            public int CriticalMultiplier = 2;
            public int RangeIncrement = 0;
            public int StrengthRating = -1; // only relevant for ranged weapons (composite bows); -1 = noncomposite
            public string DamageType = "Piercing or slashing";

            public int GetAttackBonus(GameRules.GameRules rules) {
                string attackStat = (RangeCategory == Range.RANGED ? RANGED : MELEE) + ATTACK_ROLLS + WITH + W.Name;
                return rules.Stats[attackStat].GetValue(rules.Rules);
            }
        }

        public string Name = "Dagger";

        public List<Attack> Attacks = new List<Attack>();

        public List<string> Tags = new List<string>();

        // Call this function *after* setting up all the attacks and tags for the weapon!
        // Set finesse to true if the character has the Weapon Finesse feat
        public void Register(GameRules.GameRules rules, bool finesse) {
            foreach (Attack attack in Attacks) {
                string range = (attack.RangeCategory == Attack.Range.RANGED ? RANGED : MELEE);
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

                // TODO : Extra attacks (and iteratives)
                rules.Rules.AddEffect(new Effects.Effect("Base attack bonus for " + Name)
                    .AddModifier(new Effects.Modifier(rules.Stats[BASE_ATTACK_BONUS], UNTYPED, attackStat.Name))
                );

                if (attack.RangeCategory == Attack.Range.RANGED) {
                    rules.Rules.AddEffect(new Effects.Effect("DEX modifier for " + Name + " attacks")
                        .AddModifier(new Effects.Modifier(rules.Stats[DEX_MODIFIER], UNTYPED, attackStat.Name))
                    );
                    if (attack.StrengthRating >= 0) {
                        rules.Rules.AddEffect(new Effects.Effect("Insufficient strength for " + Name)
                            .AddModifier(new Effects.Modifier(-2, UNTYPED, attackStat.Name))
                            .SetToggle(new Effects.ConditionalToggle(rules.Stats[STR_MODIFIER], Effects.Relation.LESS_THAN, attack.StrengthRating))
                        );
                    }
                } else if (finesse) {
                    // When Weapon Finesse applies, you use DEX or STR whichever is better at the time.
                    rules.Rules.AddEffect(new Effects.Effect("Finesse modifier for " + Name + " attacks")
                        .AddModifier(new Effects.Modifier(
                            new Effects.GreaterAmount(rules.Stats[DEX_MODIFIER], rules.Stats[STR_MODIFIER]),
                            UNTYPED,
                            attackStat.Name
                        ))
                    );
                } else {
                    rules.Rules.AddEffect(new Effects.Effect("STR modifier for " + Name + " attacks")
                        .AddModifier(new Effects.Modifier(rules.Stats[STR_MODIFIER], UNTYPED, attackStat.Name))
                    );
                }


            }
        }
    }
}
