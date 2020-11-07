using System;
using System.Collections.Generic;
using System.Linq;

using PlayMore35.Effects;
using static PlayMore35.Effects.Constants;
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
        public (DiceGroup dice, List<string> explanation) Damage { get; private set; }
        public (DiceGroup dice, List<string> explanation) CriticalDamage { get; private set; }
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

        private Attack(Weapon weapon, int wpnSizeCategory, int sizeDiscrepancy, AttackMode atk, Weapon.Hand hand) {
            WeaponName = weapon.Name;
            WeaponTags = weapon.Tags;
            WeaponSizeCategory = wpnSizeCategory;

            Proficiency = atk.ProficencyCategory;
            Effort = weapon.EffortCategory + sizeDiscrepancy;
            Hand = hand;
            DamageType = atk.DamageType;
            ThreatRange = atk.ThreatRange;
        }

        private Attack(Armor armor, int wpnSizeCategory, Weapon.Hand hand) {
            WeaponName = "armor spikes";
            WeaponTags = new List<string>(); // TODO : ... see shield todo
            WeaponSizeCategory = wpnSizeCategory;

            Proficiency = armor.ArmorSpikes.ProficencyCategory;
            Effort = Weapon.Effort.LIGHT;
            Hand = hand;
            DamageType = armor.ArmorSpikes.DamageType;
            ThreatRange = armor.ArmorSpikes.ThreatRange;

            Range = Weapon.Range.MELEE;
            RangeIncrement = 0;
        }

        private Attack(Shield shield, int wpnSizeCategory, int sizeDiscrepancy, Weapon.Hand hand) {
            WeaponName = "shield bash";
            WeaponTags = new List<string>(); // TODO : ... can shields have weapon tags?
            WeaponSizeCategory = wpnSizeCategory;

            Proficiency = shield.ShieldBash.ProficencyCategory;
            Effort = (Weapon.Effort) shield.ShieldBashEffort + sizeDiscrepancy;
            Hand = hand;
            DamageType = shield.ShieldBash.DamageType;
            ThreatRange = shield.ShieldBash.ThreatRange;

            Range = Weapon.Range.MELEE;
            RangeIncrement = 0;
        }

        private DiceGroup BaseDamageDice(int sizeCategory, (int, int) damageDieMedium) {
            DiceGroup dice = new DiceGroup(damageSteps[damageDieMedium][sizeCategory + 4]) {
                Name = "Base " + SIZE_CATEGORIES[sizeCategory + 4] + " " + WeaponName + " damage"
            };
            return dice;
        }

        private void ComputeAttackBonusAndDamage(
            EffectPool rules,
            Equipped items,
            (int qty, int type) damageDieMedium,
            int sizeCategory,
            int criticalMultiplier,
            List<WeaponProperty> properties,
            int sizeDiscrepancy,
            int twfPenalty
        ) {
            IEnumerable<Modifier> atkModsFromProps = properties
                .Select(x => x.AttackModifier)
                .Where(x => x != null);
            IEnumerable<Modifier> attackModifiers = rules.GetAttackModifiers(items, this)
                .Concat(atkModsFromProps);
                
            AttackBonus = StackModifiers(attackModifiers, rules, items);
            if (!rules.Proficient(items, this)) {
                AttackBonus.bonus -= 4;
                AttackBonus.explanation.Add("-4 not proficient");
            }

            // XXX : This is crappy but composite bows are crappy.  Oh well.
            int compositeStrToDamage = 0;
            if (Range == Weapon.Range.BOW && StrengthRating >= 0) {
                int strModifier = rules.GetStat(STR_MODIFIER).GetValue(rules, items).value;
                if (strModifier >= StrengthRating) {
                    compositeStrToDamage = StrengthRating;
                } else {
                    AttackBonus.bonus -= 2;
                    AttackBonus.explanation.Add("-2 insufficient STR for +" + StrengthRating + " composite bow");
                    compositeStrToDamage = Math.Max(strModifier, 0); // penalty would already be applied by game rules.
                }
            }
            if (sizeDiscrepancy != 0) {
                int attackPenalty = -2 * Math.Abs(sizeDiscrepancy);
                AttackBonus.bonus += attackPenalty;
                AttackBonus.explanation.Add("" + attackPenalty + " inappropriately-sized weapon");
            }

            if (twfPenalty != 0) {
                AttackBonus.bonus += twfPenalty;
                AttackBonus.explanation.Add("" + twfPenalty + " two-weapon fighting penalty");
            }
            
            DiceGroup damage = BaseDamageDice(sizeCategory, damageDieMedium);
            string baseExpl = damage.ToString() + " " + damage.Name;
            List<string> explanation = new List<string>() {
                baseExpl
            };

            IEnumerable<Modifier> dmgModsFromProps = properties
                .Select(x => x.DamageModifier)
                .Where(x => x != null);
            IEnumerable<Modifier> damageModifiers = rules.GetDamageModifiers(items, this)
                .Concat(dmgModsFromProps);
            
            (int mod, List<string> modExpl) = StackModifiers(damageModifiers, rules, items);
            damage.AddDice(mod + compositeStrToDamage, 1);
            explanation.AddRange(modExpl);
            if (compositeStrToDamage > 0) {
                explanation.Add("+" + compositeStrToDamage + " STR to composite bow damage");
            }

            DiceGroup critDamage = new DiceGroup(WeaponName + " critical hit damage").AddDice(damage);
            List<string> critExplanation = new List<string>();
            critExplanation.AddRange(explanation);

            critDamage.Multiply(criticalMultiplier);
            critExplanation.Add("Critical hit ×" + criticalMultiplier);

            IEnumerable<DiceGroup> extraDiceFromProps = properties
                .Where(x => x.ExtraDamage.qty != 0)
                .Select(x => new DiceGroup(x.Name).AddDice(x.ExtraDamage.qty, x.ExtraDamage.size));
            IEnumerable<DiceGroup> extraDice = rules.GetExtraDice(items, this)
                .Concat(extraDiceFromProps);
            foreach (DiceGroup extra in extraDice) {
                damage.AddDice(extra);
                critDamage.AddDice(extra);
                explanation.Add(extra.ToString() + " " + extra.Name);
                critExplanation.Add(extra.ToString() + " " + extra.Name);
            }

            Damage = (damage, explanation);
            CriticalDamage = (critDamage, critExplanation);
        }

        public static Attack Melee(
            EffectPool rules,
            Equipped items,
            Weapon weapon,
            Weapon.Hand hand,
            int wpnSizeCategory,
            int sizeDiscrepancy,
            bool firstHead,
            int twfPenalty = 0,
            Weapon.Effort? effort = null // override the weapon's effort when using a double weapon to twf
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
                items,
                atk.DamageDieMedium,
                attack.WeaponSizeCategory,
                atk.CriticalMultiplier,
                atk.Properties,
                sizeDiscrepancy,
                twfPenalty
            );
            return attack;
        }

        public static Attack Projectile(
            EffectPool rules,
            Equipped items,
            Weapon weapon,
            Weapon.Hand hand,
            int wpnSizeCategory,
            int sizeDiscrepancy,
            int twfPenalty = 0
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
                items,
                atk.DamageDieMedium,
                attack.WeaponSizeCategory,
                atk.CriticalMultiplier,
                atk.Properties,
                sizeDiscrepancy,
                twfPenalty
            );
            return attack;
        }

        public static Attack Thrown(
            EffectPool rules,
            Equipped items,
            Weapon weapon,
            Weapon.Hand hand,
            int wpnSizeCategory,
            int sizeDiscrepancy,
            int twfPenalty = 0
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
                items,
                atk.DamageDieMedium, 
                attack.WeaponSizeCategory,
                atk.CriticalMultiplier, 
                atk.Properties,
                sizeDiscrepancy,
                twfPenalty
            );
            return attack;
        }

        public static Attack ArmorSpikes(
            EffectPool rules,
            Equipped items,
            Weapon.Hand hand,
            int armSizeCategory,
            int twfPenalty = 0
        ) {
            Armor armor = items.DonnedArmor;
            Attack spikes = new Attack(armor, armSizeCategory, hand);
            spikes.ComputeAttackBonusAndDamage(
                rules,
                items,
                armor.ArmorSpikes.DamageDieMedium,
                armSizeCategory,
                armor.ArmorSpikes.CriticalMultiplier,
                armor.ArmorSpikes.Properties,
                0,
                twfPenalty
            );
            return spikes;
        }

        public static Attack ShieldBash(
            EffectPool rules,
            Equipped items,
            Weapon.Hand hand,
            int shSizeCategory,
            int sizeDiscrepancy,
            int twfPenalty = 0
        ) {
            Shield shield = items.OffHandShield;
            Attack shieldBash = new Attack(shield, shSizeCategory, sizeDiscrepancy, hand);
            shieldBash.ComputeAttackBonusAndDamage(
                rules,
                items,
                shield.ShieldBash.DamageDieMedium,
                shield.SizeCategory,
                shield.ShieldBash.CriticalMultiplier,
                shield.ShieldBash.Properties,
                sizeDiscrepancy,
                twfPenalty
            );
            return shieldBash;
        }

        private static void GenerateOneHandAttack(
            List<Attack> attacks,
            EffectPool rules,
            Equipped items,
            Weapon weapon,
            int wpnSizeCategory,
            int sizeDiscrepancy,
            Weapon.Hand hand,
            int twfPenalty = 0
        ) {
            Weapon.Effort effort = weapon.EffortCategory + sizeDiscrepancy;
            if (effort == Weapon.Effort.LIGHT || effort == Weapon.Effort.ONE_HANDED) {
                if (weapon.Melee != null) {
                    attacks.Add(Melee(rules, items, weapon, hand, wpnSizeCategory, sizeDiscrepancy, true, twfPenalty));
                }

                if (weapon.Projectile != null) {
                    attacks.Add(Projectile(rules, items, weapon, hand, wpnSizeCategory, sizeDiscrepancy, twfPenalty));
                }

                if (weapon.CanBeThrown()) {
                    attacks.Add(Thrown(rules, items, weapon, hand, wpnSizeCategory, sizeDiscrepancy, twfPenalty));
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

        // This function uses _reference_ equality to check whether you're two-handing a weapon.
        // If they are reference-equal, you're two-handing it.  If not, you have a different weapon in each hand,
        // even if they look identical.  
        // Either being null means that hand is empty.  If the primary hand is empty, this function will generate
        // an empty list of attacks, assuming that the player has no attacks available.
        public static List<Attack> Generate(EffectPool rules, int sizeChange, Equipped items) {
            Armor armor = items.DonnedArmor;
            Shield shield = items.OffHandShield;
            Weapon primaryHandWeapon = items.PrimaryHandWeapon;
            Weapon offHandWeapon = items.OffHandWeapon;
            bool shieldPresent = shield != null;
            if (shieldPresent && shield.AttackRollPenalty == null) {
                // Not allowed to use off-hand weapon.  If we're trying to two-hand a weapon, we're not allowed
                // to use our weapon at all.
                if (primaryHandWeapon == offHandWeapon) {
                    primaryHandWeapon = offHandWeapon = null;
                } else {
                    offHandWeapon = null;
                }
            }

            int playerSizeCategory = rules.GetStat(SIZE_CATEGORY).GetValue(rules, items).value;

            List<Attack> attacks = new List<Attack>();
            int shSizeCategory = shieldPresent ? AdjustSize(shield.SizeCategory, sizeChange) : 0;
            int shSizeDiscrepancy = shieldPresent ? shSizeCategory - playerSizeCategory : 0;
            bool shieldBashAvailable = shield?.ShieldBash != null;
            if (shieldBashAvailable) {
                attacks.Add(ShieldBash(rules, items, Weapon.Hand.ONE_HANDED, shSizeCategory, shSizeDiscrepancy));
            }

            bool spikesAvailable = armor?.ArmorSpikes != null;
            int armSizeCategory = spikesAvailable ? AdjustSize(armor.sizeCategory, sizeChange) : 0;
            if (spikesAvailable) {
                attacks.Add(ArmorSpikes(rules, items, Weapon.Hand.ONE_HANDED, armSizeCategory));
            }

            if (primaryHandWeapon == null) {
                return attacks;
            }

            int priSizeCategory = AdjustSize(primaryHandWeapon.SizeCategory, sizeChange);
            int priSizeDiscrepancy = priSizeCategory - playerSizeCategory;
            Weapon.Effort priEffort = primaryHandWeapon.EffortCategory + priSizeDiscrepancy;

            bool offWpnPresent = offHandWeapon != null;
            int offSizeCategory = offWpnPresent ? AdjustSize(offHandWeapon.SizeCategory, sizeChange) : 0;
            int offSizeDiscrepancy = offWpnPresent ? offSizeCategory - playerSizeCategory : 0;

            // Exact same weapon in both hands.  Player is two-handing a single weapon.
            if (primaryHandWeapon == offHandWeapon) {
                if (primaryHandWeapon.Melee != null) {
                    if (priEffort >= Weapon.Effort.LIGHT && priEffort <= Weapon.Effort.TWO_HANDED) {
                        // You can't get a mechanical benefit for two-handing a light weapon.
                        Weapon.Hand hand = priEffort == Weapon.Effort.LIGHT
                            ? Weapon.Hand.ONE_HANDED
                            : Weapon.Hand.TWO_HANDED;
                        attacks.Add(Melee(rules, items, primaryHandWeapon, hand, priSizeCategory, priSizeDiscrepancy, true));

                        // For double weapons with identical heads, generate an identical primary- and off-hand attack.
                        if (primaryHandWeapon.SecondIdenticalHead) {
                            attacks.Add(Melee(rules, items, primaryHandWeapon, Weapon.Hand.PRIMARY_HAND_TWF, priSizeCategory, priSizeDiscrepancy, true, -4, Weapon.Effort.ONE_HANDED));
                            attacks.Add(Melee(rules, items, primaryHandWeapon, Weapon.Hand.OFF_HAND_TWF, offSizeCategory, offSizeDiscrepancy, true, -8, Weapon.Effort.LIGHT));
                        }

                        // For double weapons with different heads, generate a primary-hand/off-hand pairing for each ordering.
                        else if (primaryHandWeapon.SecondUniqueHead != null) {
                            attacks.Add(Melee(rules, items, primaryHandWeapon, Weapon.Hand.PRIMARY_HAND_TWF, priSizeCategory, priSizeDiscrepancy, true, -4, Weapon.Effort.ONE_HANDED));
                            attacks.Add(Melee(rules, items, primaryHandWeapon, Weapon.Hand.OFF_HAND_TWF, offSizeCategory, offSizeDiscrepancy, false, -8, Weapon.Effort.LIGHT));

                            attacks.Add(Melee(rules, items, primaryHandWeapon, Weapon.Hand.PRIMARY_HAND_TWF, priSizeCategory, priSizeDiscrepancy, false, -4, Weapon.Effort.ONE_HANDED));
                            attacks.Add(Melee(rules, items, primaryHandWeapon, Weapon.Hand.OFF_HAND_TWF, offSizeCategory, offSizeDiscrepancy, true, -8, Weapon.Effort.LIGHT));
                        }
                    }
                }

                if (primaryHandWeapon.Projectile != null) {
                    Weapon.Effort effort = primaryHandWeapon.EffortCategory + priSizeDiscrepancy;
                    if (effort >= Weapon.Effort.LIGHT && effort <= Weapon.Effort.TWO_HANDED) {
                        attacks.Add(Projectile(rules, items, primaryHandWeapon, Weapon.Hand.TWO_HANDED, priSizeCategory, priSizeDiscrepancy));
                    }
                }


                if (primaryHandWeapon.CanBeThrown()) {
                    // I'm assuming all non-improvised weapons are thrown one-handed.  Either way, it doesn't
                    // affect anything mechanically (I think); STR to damage isn't multiplied or anything like that.
                    attacks.Add(Thrown(rules, items, primaryHandWeapon, Weapon.Hand.ONE_HANDED, priSizeCategory, priSizeDiscrepancy));
                }
            }
            
            // Contents of hands are different.
            else {
                GenerateOneHandAttack(attacks, rules, items, primaryHandWeapon, priSizeCategory, priSizeDiscrepancy, Weapon.Hand.ONE_HANDED);
                if (offWpnPresent) {
                    Weapon.Effort offEffort = offHandWeapon.EffortCategory + offSizeDiscrepancy;
                    GenerateOneHandAttack(attacks, rules, items, primaryHandWeapon, priSizeCategory, priSizeDiscrepancy, Weapon.Hand.PRIMARY_HAND_TWF, offEffort == Weapon.Effort.LIGHT ? -4 : -6);
                    GenerateOneHandAttack(attacks, rules, items, offHandWeapon, offSizeCategory, offSizeDiscrepancy, Weapon.Hand.OFF_HAND_TWF, offEffort == Weapon.Effort.LIGHT ? -8 : -10);
                }
                if (spikesAvailable) {
                    GenerateOneHandAttack(attacks, rules, items, primaryHandWeapon, priSizeCategory, priSizeDiscrepancy, Weapon.Hand.PRIMARY_HAND_TWF, -4);
                    attacks.Add(ArmorSpikes(rules, items, Weapon.Hand.OFF_HAND_TWF, armSizeCategory, -8));
                }
                if (shieldBashAvailable) {
                    Weapon.Effort offEffort = (Weapon.Effort)shield.ShieldBashEffort + shSizeDiscrepancy;
                    GenerateOneHandAttack(attacks, rules, items, primaryHandWeapon, priSizeCategory, priSizeDiscrepancy, Weapon.Hand.PRIMARY_HAND_TWF, offEffort == Weapon.Effort.LIGHT ? -4 : -6);
                    attacks.Add(ShieldBash(rules, items, Weapon.Hand.OFF_HAND_TWF, shSizeCategory, shSizeDiscrepancy, offEffort == Weapon.Effort.LIGHT ? -8 : -10));
                }
            }

            return attacks;
        }
    }
}
