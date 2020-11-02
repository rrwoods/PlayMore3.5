using System;
using System.Collections.Generic;
using System.Linq;

using static PlayMore35.GameRules.Constants;
using static PlayMore35.Effects.StackingRules;

namespace PlayMore35.Items {
    public class Attack {
        public readonly string WeaponName;
        public readonly List<string> WeaponTags;
        public readonly int WeaponSizeCategory;
        public Weapon.Proficiency Proficiency { get; private set; }
        public Weapon.Effort Effort { get; private set; }
        public Weapon.Hand Hand { get; private set; }
        public Weapon.Range Range { get; private set; }
        public (int bonus, List<string> explanation) AttackBonus; // XXX : {get private set} is missing because i modify the bonus directly.
        public (Effects.DiceGroup dice, List<string> explanation) Damage { get; private set; }
        public (Effects.DiceGroup dice, List<string> explanation) CriticalDamage { get; private set; }
        public string DamageType { get; private set; }
        public int ThreatRange { get; private set; }
        public int RangeIncrement { get; private set; }
        public int StrengthRating { get; private set; } = -1; // only relevant for ranged weapons (composite bows); -1 = noncomposite

        // keys are the Medium damage dice
        // values are lists of damage dice ordered from Fine to Colossal
        private static readonly Dictionary<(int, int), List<(int, int)>> damageSteps =
            new Dictionary<(int, int), List<(int, int)>> {
                [(1, 2)] = new List<(int, int)>  { (0, 1), (0, 1), (0, 1), (1, 1),  (1, 2),  (1, 3), (1, 4), (1, 6), (1, 8) },
                [(1, 3)] = new List<(int, int)>  { (0, 1), (0, 1), (1, 1), (1, 2),  (1, 3),  (1, 4), (1, 6), (1, 8), (2, 6) },
                [(1, 4)] = new List<(int, int)>  { (0, 1), (1, 1), (1, 2), (1, 3),  (1, 4),  (1, 6), (1, 8), (2, 6), (3, 6) },
                [(1, 6)] = new List<(int, int)>  { (1, 1), (1, 2), (1, 3), (1, 4),  (1, 6),  (1, 8), (2, 6), (3, 6), (4, 6) },
                [(2, 4)] = new List<(int, int)>  { (1, 2), (1, 3), (1, 4), (1, 6),  (2, 4),  (2, 6), (3, 6), (4, 6), (6, 6) },
                [(1, 8)] = new List<(int, int)>  { (1, 2), (1, 3), (1, 4), (1, 6),  (1, 8),  (2, 6), (3, 6), (4, 6), (6, 6) },
                [(1, 10)] = new List<(int, int)> { (1, 3), (1, 4), (1, 6), (1, 8),  (1, 10), (2, 8), (3, 8), (4, 8), (6, 8) },
                [(1, 12)] = new List<(int, int)> { (1, 4), (1, 6), (1, 8), (1, 10), (1, 12), (3, 6), (4, 6), (6, 6), (8, 6) },
                [(2, 6)] = new List<(int, int)>  { (1, 4), (1, 6), (1, 8), (1, 10), (2, 6),  (3, 6), (4, 6), (6, 6), (8, 6) },
            };

        private Effects.DiceGroup BaseDamageDice(int sizeCategory, (int, int) damageDieMedium) {
            Effects.DiceGroup dice = new Effects.DiceGroup(damageSteps[damageDieMedium][sizeCategory + 4]) {
                Name = "Base " + SIZE_CATEGORIES[sizeCategory + 4] + " " + WeaponName + " damage"
            };
            return dice;
        }

        private void ComputeAttackBonusAndDamage(
            GameRules.GameRules rules,
            (int qty, int type) damageDieMedium,
            int sizeCategory,
            int criticalMultiplier,
            List<WeaponProperty> properties,
            int sizeDiscrepancy
        ) {
            IEnumerable<Effects.Modifier> atkModsFromProps = properties
                .Select(x => x.AttackModifier)
                .Where(x => x != null);
            IEnumerable<Effects.Modifier> attackModifiers = rules.Rules.GetAttackModifiers(this)
                .Concat(atkModsFromProps);
                
            AttackBonus = StackModifiers(attackModifiers, rules.Rules);

            // XXX : This is crappy but composite bows are crappy.  Oh well.
            int compositeStrToDamage = 0;
            if (Range == Weapon.Range.BOW && StrengthRating >= 0) {
                int strModifier = rules.Stats[STR_MODIFIER].GetValue(rules.Rules).value;
                if (strModifier >= StrengthRating) {
                    compositeStrToDamage = StrengthRating;
                } else {
                    AttackBonus.bonus -= 2;
                    AttackBonus.explanation.Add("-2 Insufficient STR for +" + StrengthRating + " composite bow");
                    compositeStrToDamage = Math.Max(strModifier, 0); // penalty would already be applied by game rules.
                }
            }
            if (sizeDiscrepancy != 0) {
                int attackPenalty = -2 * Math.Abs(sizeDiscrepancy);
                AttackBonus.bonus += attackPenalty;
                AttackBonus.explanation.Add("Inappropriately-sized weapon");
            }
            
            Effects.DiceGroup damage = BaseDamageDice(sizeCategory, damageDieMedium);
            string baseExpl = damage.ToString() + " " + damage.Name;
            List<string> explanation = new List<string>() {
                baseExpl
            };

            IEnumerable<Effects.Modifier> dmgModsFromProps = properties
                .Select(x => x.DamageModifier)
                .Where(x => x != null);
            IEnumerable<Effects.Modifier> damageModifiers = rules.Rules.GetDamageModifiers(this)
                .Concat(dmgModsFromProps);
            
            (int mod, List<string> modExpl) = StackModifiers(damageModifiers, rules.Rules);
            damage.AddDice(mod + compositeStrToDamage, 1);
            explanation.AddRange(modExpl);
            if (compositeStrToDamage > 0) {
                explanation.Add("+" + compositeStrToDamage + " STR to composite bow damage");
            }

            Effects.DiceGroup critDamage = new Effects.DiceGroup(WeaponName + " critical hit damage").AddDice(damage);
            List<string> critExplanation = new List<string>();
            critExplanation.AddRange(explanation);

            critDamage.Multiply(criticalMultiplier);
            critExplanation.Add("Critical hit ×" + criticalMultiplier);

            IEnumerable<Effects.DiceGroup> extraDiceFromProps = properties
                .Where(x => x.ExtraDamage.qty != 0)
                .Select(x => new Effects.DiceGroup(x.Name).AddDice(x.ExtraDamage.qty, x.ExtraDamage.size));
            IEnumerable<Effects.DiceGroup> extraDice = rules.Rules.GetExtraDice(this)
                .Concat(extraDiceFromProps);
            foreach (Effects.DiceGroup extra in extraDice) {
                damage.AddDice(extra);
                critDamage.AddDice(extra);
                explanation.Add(extra.ToString() + " " + extra.Name);
                critExplanation.Add(extra.ToString() + " " + extra.Name);
            }

            Damage = (damage, explanation);
            CriticalDamage = (critDamage, critExplanation);
        }

        private Attack(Weapon weapon, int wpnSizeCategory, int sizeDiscrepancy, AttackMode atk, Weapon.Hand hand) {
            WeaponName = weapon.Name;
            WeaponTags = weapon.Tags;
            WeaponSizeCategory = wpnSizeCategory;

            Proficiency = atk.ProficencyCategory;
            Effort = atk.EffortCategory + sizeDiscrepancy;
            Hand = hand;
            DamageType = atk.DamageType;
            ThreatRange = atk.ThreatRange;
        }

        public static Attack Melee(
            GameRules.GameRules rules,
            Weapon weapon,
            Weapon.Hand hand,
            int wpnSizeCategory,
            int sizeDiscrepancy,
            bool firstHead,
            Weapon.Effort? effort = null // when unspecified, use the AttackMode's effort
        ) {
            var melee = firstHead ? weapon.Melee : weapon.SecondUniqueHead;
            if (melee == null) {
                return null;
            }

            (AttackMode atk, bool reach) = ((AttackMode, bool)) melee;
            Attack attack = new Attack(weapon, wpnSizeCategory, sizeDiscrepancy, atk, hand) {
                Range = reach ? Weapon.Range.REACH_MELEE : Weapon.Range.MELEE,
                RangeIncrement = 0,
            };
            attack.Effort = (effort + sizeDiscrepancy) ?? attack.Effort;
            attack.ComputeAttackBonusAndDamage(
                rules,
                atk.DamageDieMedium,
                attack.WeaponSizeCategory,
                atk.CriticalMultiplier,
                atk.Properties,
                sizeDiscrepancy
            );
            return attack;
        }

        public static Attack Projectile(
            GameRules.GameRules rules,
            Weapon weapon,
            Weapon.Hand hand,
            int wpnSizeCategory,
            int sizeDiscrepancy
        ) {
            if (weapon.Projectile == null) {
                return null;
            }

            (AttackMode atk, bool bow, int strengthRating) = ((AttackMode, bool, int)) weapon.Projectile;
            Attack attack = new Attack(weapon, wpnSizeCategory, sizeDiscrepancy, atk, hand) {
                Range = bow ? Weapon.Range.BOW : Weapon.Range.OTHER_PROJECTILE,
                RangeIncrement = atk.RangeIncrement,
                StrengthRating = strengthRating
            };
            attack.ComputeAttackBonusAndDamage(
                rules, 
                atk.DamageDieMedium,
                attack.WeaponSizeCategory,
                atk.CriticalMultiplier,
                atk.Properties,
                sizeDiscrepancy
            );
            return attack;
        }

        public static Attack Thrown(
            GameRules.GameRules rules,
            Weapon weapon,
            Weapon.Hand hand,
            int wpnSizeCategory,
            int sizeDiscrepancy
        ) {
            AttackMode atk;
            if (weapon.Thrown != null) {
                atk = weapon.Thrown;
            } else if (weapon.Melee != null) {
                atk = ( ((AttackMode atk, bool reach)) weapon.Melee).atk;
            } else {
                return null; // not dealing with throwing the second head of a double weapon.  for now.
            }

            Attack attack = new Attack(weapon, wpnSizeCategory, sizeDiscrepancy, atk, hand) {
                Range = Weapon.Range.THROWN,
                RangeIncrement = atk.RangeIncrement
            };
            attack.ComputeAttackBonusAndDamage(
                rules,
                atk.DamageDieMedium, 
                attack.WeaponSizeCategory,
                atk.CriticalMultiplier, 
                atk.Properties,
                sizeDiscrepancy
            );
            return attack;
        }

        private static void GenerateOneHandAttacks(
            List<Attack> attacks,
            GameRules.GameRules rules,
            Weapon weapon,
            int wpnSizeCategory,
            int sizeDiscrepancy,
            Weapon.Hand[] hands
        ) {
            foreach (Weapon.Hand hand in hands) {
                if (weapon.Melee != null) {
                    Weapon.Effort? effort = weapon.Melee?.atk.EffortCategory + sizeDiscrepancy;
                    if (effort == Weapon.Effort.LIGHT || effort == Weapon.Effort.ONE_HANDED) {
                        // Assuming that there are no one-handed double weapons...
                        attacks.Add(Melee(rules, weapon, hand, wpnSizeCategory, sizeDiscrepancy, true));
                    }
                }

                if (weapon.Melee != null) {
                    Weapon.Effort? effort = weapon.Projectile?.atk.EffortCategory + sizeDiscrepancy;
                    if (effort == Weapon.Effort.LIGHT || effort == Weapon.Effort.ONE_HANDED) {
                        attacks.Add(Projectile(rules, weapon, hand, wpnSizeCategory, sizeDiscrepancy));
                    }
                }

                Weapon.Effort? thrownEffort = weapon.ThrownEffort() + sizeDiscrepancy;
                if (thrownEffort == Weapon.Effort.LIGHT || thrownEffort == Weapon.Effort.ONE_HANDED) {
                    attacks.Add(Thrown(rules, weapon, hand, wpnSizeCategory, sizeDiscrepancy));
                }
            }
        }

        private static int AdjustSize(int starting, int change) {
            int adjusted = starting + change;
            if (adjusted < -4) {
                adjusted = -4;
            } else if (adjusted > +4) {
                adjusted = +4;
            }
            return adjusted;
        }

        // TODO : weapon size feats... like Monkey Grip, Oversized TWF, and all that fucking bullshit (maybe)

        // This function uses _reference_ equality to check whether you're two-handing a weapon.
        // If they are reference-equal, you're two-handing it.  If not, you have a different weapon in each hand,
        // even if they look identical.  
        // Either being null means that hand is empty.  If the primary hand is empty, this function will generate
        // an empty list of attacks, assuming that the player has no attacks available.
        public static List<Attack> Generate(GameRules.GameRules rules, int sizeChange) {
            Weapon primaryHandWeapon = rules.Rules.PrimaryHandWeapon;
            Weapon offHandWeapon = rules.Rules.OffHandWeapon;

            List<Attack> attacks = new List<Attack>();
            if (primaryHandWeapon == null) {
                return attacks;
            }

            int playerSizeCategory = rules.Stats[SIZE_CATEGORY].GetValue(rules.Rules).value;
            int priSizeCategory = AdjustSize(primaryHandWeapon.SizeCategory, sizeChange);
            int priSizeDiscrepancy = priSizeCategory - playerSizeCategory;

            bool twfAvailable = (offHandWeapon != null);
            int offSizeCategory = twfAvailable ? AdjustSize(offHandWeapon.SizeCategory, sizeChange) : 0;
            int offSizeDiscrepancy = twfAvailable ? offSizeCategory - playerSizeCategory : 0;

            // Exact same weapon in both hands.  Player is two-handing a single weapon.
            if (primaryHandWeapon == offHandWeapon) {
                if (primaryHandWeapon.Melee != null) {
                    Weapon.Effort effort = (Weapon.Effort)primaryHandWeapon.Melee?.atk.EffortCategory + priSizeDiscrepancy;
                    if (effort >= Weapon.Effort.LIGHT && effort <= Weapon.Effort.TWO_HANDED) {
                        // You can't get a mechanical benefit for two-handing a light weapon.
                        Weapon.Hand hand = effort == Weapon.Effort.LIGHT
                            ? Weapon.Hand.ONE_HANDED
                            : Weapon.Hand.TWO_HANDED;
                        attacks.Add(Melee(rules, primaryHandWeapon, hand, priSizeCategory, priSizeDiscrepancy, true));

                        // For double weapons with identical heads, generate an identical primary- and off-hand attack.
                        if (primaryHandWeapon.SecondIdenticalHead) {
                            attacks.Add(Melee(rules, primaryHandWeapon, Weapon.Hand.PRIMARY_HAND_TWF, priSizeCategory, priSizeDiscrepancy, true, Weapon.Effort.ONE_HANDED));
                            attacks.Add(Melee(rules, primaryHandWeapon, Weapon.Hand.OFF_HAND_TWF, offSizeCategory, offSizeDiscrepancy, true, Weapon.Effort.LIGHT));
                        }

                        // For double weapons with different heads, generate a primary-hand/off-hand pairing for each ordering.
                        else if (primaryHandWeapon.SecondUniqueHead != null) {
                            attacks.Add(Melee(rules, primaryHandWeapon, Weapon.Hand.PRIMARY_HAND_TWF, priSizeCategory, priSizeDiscrepancy, true, Weapon.Effort.ONE_HANDED));
                            attacks.Add(Melee(rules, primaryHandWeapon, Weapon.Hand.OFF_HAND_TWF, offSizeCategory, offSizeDiscrepancy, false, Weapon.Effort.LIGHT));

                            attacks.Add(Melee(rules, primaryHandWeapon, Weapon.Hand.PRIMARY_HAND_TWF, priSizeCategory, priSizeDiscrepancy, false, Weapon.Effort.ONE_HANDED));
                            attacks.Add(Melee(rules, primaryHandWeapon, Weapon.Hand.OFF_HAND_TWF, offSizeCategory, offSizeDiscrepancy, true, Weapon.Effort.LIGHT));
                        }
                    }
                }

                if (primaryHandWeapon.Projectile != null) {
                    Weapon.Effort effort = (Weapon.Effort)primaryHandWeapon.Projectile?.atk.EffortCategory + priSizeDiscrepancy;
                    if (effort >= Weapon.Effort.LIGHT && effort <= Weapon.Effort.TWO_HANDED) {
                        attacks.Add(Projectile(rules, primaryHandWeapon, Weapon.Hand.TWO_HANDED, priSizeCategory, priSizeDiscrepancy));
                    }
                }

                Weapon.Effort? thrownEffort = primaryHandWeapon.ThrownEffort() + priSizeDiscrepancy;
                if (thrownEffort >= Weapon.Effort.LIGHT && thrownEffort <= Weapon.Effort.TWO_HANDED) {
                    // I'm assuming all non-improvised weapons are thrown one-handed.  Either way, it doesn't
                    // affect anything mechanically (I think); STR to damage isn't multiplied or anything like that.
                    attacks.Add(Thrown(rules, primaryHandWeapon, Weapon.Hand.ONE_HANDED, priSizeCategory, priSizeDiscrepancy));
                }
            }
            
            // Contents of hands are different.
            else {
                Weapon.Hand[] hands = twfAvailable
                    ? new Weapon.Hand[] { Weapon.Hand.ONE_HANDED, Weapon.Hand.PRIMARY_HAND_TWF }
                    : new Weapon.Hand[] { Weapon.Hand.ONE_HANDED };
                GenerateOneHandAttacks(attacks, rules, primaryHandWeapon, priSizeCategory, priSizeDiscrepancy, hands);
                
                if (twfAvailable) {
                    GenerateOneHandAttacks(attacks, rules, offHandWeapon, offSizeCategory, offSizeDiscrepancy, new Weapon.Hand[] { Weapon.Hand.OFF_HAND_TWF });
                }
            }

            return attacks;
        }
    }
}
