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
        public List<int> ExtraAttacks { get; private set; }
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
                // the first row is for zero-damage weapons, like the net.
                [(0, 1)] = new List<(int, int)> { (0, 1), (0, 1), (0, 1), (0, 1), (0, 1), (0, 1), (0, 1), (0, 1), (0, 1) },
                [(1, 2)] = new List<(int, int)> { (0, 1), (0, 1), (0, 1), (1, 1), (1, 2), (1, 3), (1, 4), (1, 6), (1, 8) },
                [(1, 3)] = new List<(int, int)> { (0, 1), (0, 1), (1, 1), (1, 2), (1, 3), (1, 4), (1, 6), (1, 8), (2, 6) },
                [(1, 4)] = new List<(int, int)> { (0, 1), (1, 1), (1, 2), (1, 3), (1, 4), (1, 6), (1, 8), (2, 6), (3, 6) },
                [(1, 6)] = new List<(int, int)> { (1, 1), (1, 2), (1, 3), (1, 4), (1, 6), (1, 8), (2, 6), (3, 6), (4, 6) },
                [(2, 4)] = new List<(int, int)> { (1, 2), (1, 3), (1, 4), (1, 6), (2, 4), (2, 6), (3, 6), (4, 6), (6, 6) },
                [(1, 8)] = new List<(int, int)> { (1, 2), (1, 3), (1, 4), (1, 6), (1, 8), (2, 6), (3, 6), (4, 6), (6, 6) },
                [(1, 10)] = new List<(int, int)> { (1, 3), (1, 4), (1, 6), (1, 8), (1, 10), (2, 8), (3, 8), (4, 8), (6, 8) },
                [(1, 12)] = new List<(int, int)> { (1, 4), (1, 6), (1, 8), (1, 10), (1, 12), (3, 6), (4, 6), (6, 6), (8, 6) },
                [(2, 6)] = new List<(int, int)> { (1, 4), (1, 6), (1, 8), (1, 10), (2, 6), (3, 6), (4, 6), (6, 6), (8, 6) },
            };

        private Attack(Weapon weapon, int wpnSizeCategory, int sizeDiscrepancy, AttackMode atk, Weapon.Hand hand, bool familiar) {
            WeaponName = weapon.Name;
            WeaponTags = weapon.Tags;
            WeaponSizeCategory = wpnSizeCategory;

            Proficiency = familiar ? Weapon.Proficiency.MARTIAL : atk.ProficencyCategory;
            if (Proficiency == Weapon.Proficiency.ONE_HANDED_EXOTIC) {
                Proficiency = (hand == Weapon.Hand.TWO_HANDED || hand == Weapon.Hand.TWO_HANDED_TWF)
                    ? Weapon.Proficiency.MARTIAL
                    : Weapon.Proficiency.EXOTIC;
            }

            Effort = weapon.EffortCategory + sizeDiscrepancy;
            Hand = hand;
            DamageType = atk.DamageType;
            ThreatRange = atk.ThreatRange;
        }

        private Attack(Armor armor, int wpnSizeCategory, Weapon.Hand hand, bool familiar) {
            WeaponName = "armor spikes";
            WeaponTags = new List<string>(); // can armor have weapon tags?
            WeaponSizeCategory = wpnSizeCategory;

            Proficiency = familiar ? Weapon.Proficiency.MARTIAL : armor.ArmorSpikes.ProficencyCategory;
            Effort = Weapon.Effort.LIGHT;
            Hand = hand;
            DamageType = armor.ArmorSpikes.DamageType;
            ThreatRange = armor.ArmorSpikes.ThreatRange;

            Range = Weapon.Range.MELEE;
            RangeIncrement = 0;
        }

        private Attack(Shield shield, int wpnSizeCategory, int sizeDiscrepancy, Weapon.Hand hand, bool familiar) {
            WeaponName = "shield bash";
            WeaponTags = new List<string>(); // can shields have weapon tags?
            WeaponSizeCategory = wpnSizeCategory;

            Proficiency = familiar ? Weapon.Proficiency.MARTIAL : shield.ShieldBash.ProficencyCategory;
            Effort = (Weapon.Effort)shield.ShieldBashEffort + sizeDiscrepancy;
            Hand = hand;
            DamageType = shield.ShieldBash.DamageType;
            ThreatRange = shield.ShieldBash.ThreatRange;

            Range = Weapon.Range.MELEE;
            RangeIncrement = 0;
        }

        private DiceGroup BaseDamageDice(int sizeCategory, (int, int) damageDieMedium) {
            DiceGroup dice = new DiceGroup(damageSteps[damageDieMedium][sizeCategory + 4]) {
                Name = "Base damage"
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
                AttackBonus.explanation.Add("–4 not proficient");
            }

            IEnumerable<ExtraAttack> extrasFromProps = properties
                .Select(x => x.ExtraAttack)
                .Where(x => x != null);
            IEnumerable<ExtraAttack> extras = rules.GetExtraAttacks(items, this)
                .Concat(extrasFromProps);
            ExtraAttacks = StackExtraAttacks(extras);
            ExtraAttacks.Sort((a, b) => b.CompareTo(a));

            // XXX : This is crappy but composite bows are crappy.  Oh well.
            int compositeStrToDamage = 0;
            if (Range == Weapon.Range.BOW && StrengthRating >= 0) {
                int strModifier = rules.GetStat(STR_MODIFIER).GetValue(rules, items).value;
                if (strModifier >= StrengthRating) {
                    compositeStrToDamage = StrengthRating;
                } else {
                    AttackBonus.bonus -= 2;
                    AttackBonus.explanation.Add($"–2 insufficient STR for +{StrengthRating} composite bow");
                    compositeStrToDamage = Math.Max(strModifier, 0); // penalty would already be applied by game rules.
                }
            }
            if (sizeDiscrepancy != 0) {
                int attackPenalty = -2 * Math.Abs(sizeDiscrepancy);
                AttackBonus.bonus += attackPenalty;
                AttackBonus.explanation.Add($"{attackPenalty.ToString("+#;–#;–0")} inappropriately-sized weapon");
            }

            if (twfPenalty != 0) {
                AttackBonus.bonus += twfPenalty;
                AttackBonus.explanation.Add($"{twfPenalty.ToString("+#;–#;0")} two-weapon fighting penalty");
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
                explanation.Add($"+{compositeStrToDamage} STR to composite bow damage");
            }

            DiceGroup critDamage = new DiceGroup(WeaponName + " critical hit damage").AddDice(damage);
            List<string> critExplanation = new List<string>();
            critExplanation.AddRange(explanation);

            critDamage.Multiply(criticalMultiplier);
            critExplanation.Add($"Critical hit ×{criticalMultiplier}");

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

            IEnumerable<Modifier> threatModsFromProps = properties
                .Select(x => x.ThreatModifier)
                .Where(x => x != null);
            IEnumerable<Modifier> threatModifiers = rules.GetThreatModifiers(items, this)
                .Concat(threatModsFromProps);
            (int threatIncrease, List<string> _) = StackModifiers(threatModifiers, rules, items);
            if (threatIncrease > 0) {
                int threatMultiplier = threatIncrease + 1;
                int threatCount = 21 - ThreatRange;
                int newThreatCount = threatCount * threatMultiplier;
                ThreatRange = 21 - newThreatCount;
            }

            IEnumerable<Multiplier> rangeMultipliers = rules.GetRangeMultipliers(items, this);
            if (rangeMultipliers.Any()) {
                decimal rangeIncrement = RangeIncrement;
                foreach (Multiplier m in rangeMultipliers) {
                    rangeIncrement *= m.Factor;
                }
                RangeIncrement = (int)(Math.Floor(rangeIncrement / 5) * 5);
            }
        }

        public static Attack Melee(
            EffectPool rules,
            Equipped items,
            Weapon weapon,
            Weapon.Hand hand,
            int wpnSizeCategory,
            int sizeDiscrepancy,
            int twfPenalty = 0,
            bool firstHead = true,
            Weapon.Effort? effort = null // override the weapon's effort when using a double weapon to twf
        ) {
            var melee = firstHead ? weapon.Melee : weapon.SecondHead;
            if (melee == null) {
                return null;
            }

            (AttackMode atk, bool reach) = ((AttackMode, bool))melee;
            Attack attack = new Attack(weapon, wpnSizeCategory, sizeDiscrepancy, atk, hand, rules.Familiar(items, weapon.Name)) {
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

            (AttackMode atk, bool bow, int strengthRating) = ((AttackMode, bool, int))weapon.Projectile;
            Attack attack = new Attack(weapon, wpnSizeCategory, sizeDiscrepancy, atk, hand, rules.Familiar(items, weapon.Name)) {
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
            if (!weapon.CanBeThrown()) {
                return null;
            }

            AttackMode atk;
            if (weapon.Thrown != null) {
                atk = weapon.Thrown;
            } else if (weapon.Melee != null) {
                atk = (((AttackMode atk, bool reach))weapon.Melee).atk;
            } else {
                return null; // not dealing with throwing the second head of a double weapon.  for now.
            }

            Attack attack = new Attack(weapon, wpnSizeCategory, sizeDiscrepancy, atk, hand, rules.Familiar(items, weapon.Name)) {
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
            Attack spikes = new Attack(armor, armSizeCategory, hand, false);
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
            Attack shieldBash = new Attack(shield, shSizeCategory, sizeDiscrepancy, hand, false);
            shieldBash.ComputeAttackBonusAndDamage(
                rules,
                items,
                shield.ShieldBash.DamageDieMedium,
                shield.SizeCategory + (shield.Spiked ? 1 : 0),
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
            bool primary = true
        ) {
            Weapon.Effort effort = weapon.EffortCategory + sizeDiscrepancy;
            if (effort == Weapon.Effort.LIGHT || effort == Weapon.Effort.ONE_HANDED) {
                if (weapon.Melee != null) {
                    attacks.Add(Melee(rules, items, weapon, Weapon.Hand.ONE_HANDED, wpnSizeCategory, sizeDiscrepancy, 0, true));
                }

                if (weapon.Projectile != null) {
                    attacks.Add(Projectile(rules, items, weapon, Weapon.Hand.ONE_HANDED, wpnSizeCategory, sizeDiscrepancy));
                }

                if (weapon.CanBeThrown()) {
                    attacks.Add(Thrown(rules, items, weapon, Weapon.Hand.ONE_HANDED, wpnSizeCategory, sizeDiscrepancy));
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

        public static List<Attack> Generate(EffectPool rules, int sizeChange, Equipped items) {
            Armor armor = items.DonnedArmor;
            Shield shield = items.OffHandShield;
            Weapon primaryHandWeapon = items.PrimaryHandWeapon;
            Weapon offHandWeapon = items.OffHandWeapon;
            bool shieldPresent = shield != null;
            if (shieldPresent && shield.AttackRollPenalty == null) {
                // Not allowed to use off-hand weapon.
                offHandWeapon = null;
            }

            int playerSizeCategory = rules.GetStat(SIZE_CATEGORY).GetValue(rules, items).value;

            List<Attack> attacks = new List<Attack>();
            int shSizeCategory = shieldPresent ? AdjustSize(shield.SizeCategory, sizeChange) : 0;
            int shSizeDiscrepancy = shieldPresent ? shSizeCategory - playerSizeCategory : 0;
            bool shieldBashAvailable = shield?.ShieldBash != null;
            bool noWeapons = primaryHandWeapon == null && offHandWeapon == null;
            if (shieldBashAvailable) {
                attacks.Add(
                    ShieldBash(rules, items, Weapon.Hand.ONE_HANDED, shSizeCategory, shSizeDiscrepancy)
                );
            }

            bool spikesAvailable = armor?.ArmorSpikes != null;
            int armSizeCategory = spikesAvailable ? AdjustSize(armor.SizeCategory, sizeChange) : 0;
            if (spikesAvailable) {
                attacks.Add(ArmorSpikes(rules, items, Weapon.Hand.ONE_HANDED, armSizeCategory));
            }

            if (noWeapons) {
                return attacks;
            }

            if (primaryHandWeapon != null) {
                int priSizeCategory = AdjustSize(primaryHandWeapon.SizeCategory, sizeChange);
                int priSizeDiscrepancy = priSizeCategory - playerSizeCategory;
                Weapon.Effort priEffort = primaryHandWeapon.EffortCategory + priSizeDiscrepancy;

                if ((priEffort >= Weapon.Effort.LIGHT) && (priEffort <= Weapon.Effort.TWO_HANDED)) {
                    if (!shieldPresent && (offHandWeapon == null)) {
                        // Single weapon, held in the primary hand: we intend to two-hand
                        if (primaryHandWeapon.Melee != null) {
                            // You can't get a mechanical benefit for two-handing a light weapon.
                            Weapon.Hand hand = priEffort == Weapon.Effort.LIGHT
                                ? Weapon.Hand.ONE_HANDED
                                : Weapon.Hand.TWO_HANDED;
                            attacks.Add(
                                Melee(rules, items, primaryHandWeapon, hand, priSizeCategory, priSizeDiscrepancy)
                            );
                            if (primaryHandWeapon.SecondHead != null) {
                                attacks.Add(
                                    Melee(rules, items, primaryHandWeapon, hand, priSizeCategory, priSizeDiscrepancy, 0, false)
                                );
                            }
                        }

                        if (primaryHandWeapon.Projectile != null) {
                            attacks.Add(
                                Projectile(rules, items, primaryHandWeapon, Weapon.Hand.TWO_HANDED, priSizeCategory, priSizeDiscrepancy)
                            );
                        }

                        if (primaryHandWeapon.CanBeThrown()) {
                            // I'm assuming all weapons are thrown one-handed.  Either way, it doesn't
                            // affect anything mechanically (I think); STR to damage isn't multiplied or anything like that.
                            attacks.Add(
                                Thrown(rules, items, primaryHandWeapon, Weapon.Hand.ONE_HANDED, priSizeCategory, priSizeDiscrepancy)
                            );
                        }
                    } else if (priEffort < Weapon.Effort.TWO_HANDED) {
                        GenerateOneHandAttack(attacks, rules, items, primaryHandWeapon, priSizeCategory, priSizeDiscrepancy);
                    }
                }
            }

            if (offHandWeapon != null) {
                int offSizeCategory = AdjustSize(offHandWeapon.SizeCategory, sizeChange);
                int offSizeDiscrepancy = offSizeCategory - playerSizeCategory;
                GenerateOneHandAttack(attacks, rules, items, offHandWeapon, offSizeCategory, offSizeDiscrepancy, false);
            }

            return attacks;
        }

        private static List<List<Attack>> Twf(List<Attack> pris, List<Attack> offs) {
            List<List<Attack>> ret = new List<List<Attack>>();
            foreach (Attack pri in pris) {
                if (pri == null) {
                    continue;
                }
                foreach (Attack off in offs) {
                    if (off == null) {
                        continue;
                    }
                    ret.Add(new List<Attack> { pri, off });
                }
            }
            return ret;
        }

        public static List<List<Attack>> GenerateMultiweaponFullAttacks(EffectPool rules, int sizeChange, Equipped items) {
            Weapon primaryHandWeapon = items.PrimaryHandWeapon;
            Weapon offHandWeapon = items.OffHandWeapon;
            if (primaryHandWeapon == null) {
                return new List<List<Attack>>();
            }

            Armor armor = items.DonnedArmor;
            Shield shield = items.OffHandShield;
            bool shieldPresent = shield != null;
            if (shieldPresent && shield.AttackRollPenalty == null) {
                // Not allowed to use off-hand weapon.
                offHandWeapon = null;
            }
            int playerSizeCategory = rules.GetStat(SIZE_CATEGORY).GetValue(rules, items).value;

            List<List<Attack>> fullAttacks = new List<List<Attack>>();

            int shSizeCategory = shieldPresent ? AdjustSize(shield.SizeCategory, sizeChange) : 0;
            int shSizeDiscrepancy = shieldPresent ? shSizeCategory - playerSizeCategory : 0;
            bool shieldBashAvailable = shield?.ShieldBash != null;
            bool spikesAvailable = armor?.ArmorSpikes != null;
            int armSizeCategory = spikesAvailable ? AdjustSize(armor.SizeCategory, sizeChange) : 0;

            int priSizeCategory = AdjustSize(primaryHandWeapon.SizeCategory, sizeChange);
            int priSizeDiscrepancy = priSizeCategory - playerSizeCategory;
            Weapon.Effort priEffort = primaryHandWeapon.EffortCategory + priSizeDiscrepancy;

            if ((priEffort >= Weapon.Effort.LIGHT) && (priEffort <= Weapon.Effort.TWO_HANDED)) {
                if (!shieldPresent && (offHandWeapon == null)) {
                    // Single weapon, held in the primary hand: we intend to two-hand
                    if (primaryHandWeapon.Melee != null) {
                        if (primaryHandWeapon.SecondHead != null) {
                            fullAttacks.Add(new List<Attack> {
                                Melee(rules, items, primaryHandWeapon, Weapon.Hand.PRIMARY_HAND_TWF, priSizeCategory, priSizeDiscrepancy, -4, true, Weapon.Effort.ONE_HANDED),
                                Melee(rules, items, primaryHandWeapon, Weapon.Hand.OFF_HAND_TWF, priSizeCategory, priSizeDiscrepancy, -8, false, Weapon.Effort.LIGHT)
                            });
                            // TODO : don't do this second one if the heads are identical (including properties)
                            fullAttacks.Add(new List<Attack> {
                                Melee(rules, items, primaryHandWeapon, Weapon.Hand.PRIMARY_HAND_TWF, priSizeCategory, priSizeDiscrepancy, -4, false, Weapon.Effort.ONE_HANDED),
                                Melee(rules, items, primaryHandWeapon, Weapon.Hand.OFF_HAND_TWF, priSizeCategory, priSizeDiscrepancy, -8, true, Weapon.Effort.LIGHT)
                            });
                        }

                        if (spikesAvailable) {
                            fullAttacks.Add(new List<Attack> {
                                Melee(rules, items, primaryHandWeapon, Weapon.Hand.TWO_HANDED_TWF, priSizeCategory, priSizeDiscrepancy, -4),
                                ArmorSpikes(rules, items, Weapon.Hand.OFF_HAND_TWF, armSizeCategory, -8)
                            });
                        }
                    }

                    if (primaryHandWeapon.Projectile != null && spikesAvailable) {
                        fullAttacks.Add(new List<Attack> {
                            Projectile(rules, items, primaryHandWeapon, Weapon.Hand.TWO_HANDED_TWF, priSizeCategory, priSizeDiscrepancy, -4),
                            ArmorSpikes(rules, items, Weapon.Hand.OFF_HAND_TWF, armSizeCategory, -8)
                        });
                    }

                    if (primaryHandWeapon.CanBeThrown() && spikesAvailable) {
                        fullAttacks.Add(new List<Attack> {
                            Thrown(rules, items, primaryHandWeapon, Weapon.Hand.PRIMARY_HAND_TWF, priSizeCategory, priSizeDiscrepancy, -4),
                            ArmorSpikes(rules, items, Weapon.Hand.OFF_HAND_TWF, armSizeCategory, -8)
                        });
                    }
                } else if (priEffort < Weapon.Effort.TWO_HANDED) {
                    if (offHandWeapon != null) {
                        int offSizeCategory = AdjustSize(offHandWeapon.SizeCategory, sizeChange);
                        int offsizeDiscrepancy = offSizeCategory - playerSizeCategory;
                        Weapon.Effort offEffort = offHandWeapon.EffortCategory + priSizeDiscrepancy;
                        if (offEffort == Weapon.Effort.LIGHT || offEffort == Weapon.Effort.ONE_HANDED) {
                            int twfPri = offEffort == Weapon.Effort.LIGHT ? -4 : -6;
                            int twfOff = offEffort == Weapon.Effort.LIGHT ? -8 : -10;
                            fullAttacks.AddRange(Twf(
                                new List<Attack> {
                                    Melee(rules, items, primaryHandWeapon, Weapon.Hand.PRIMARY_HAND_TWF, priSizeCategory, priSizeDiscrepancy, twfPri),
                                    Projectile(rules, items, primaryHandWeapon, Weapon.Hand.PRIMARY_HAND_TWF, priSizeCategory, priSizeDiscrepancy, twfPri),
                                    Thrown(rules, items, primaryHandWeapon, Weapon.Hand.PRIMARY_HAND_TWF, priSizeCategory, priSizeDiscrepancy, twfPri)
                                },
                                new List<Attack> {
                                    Melee(rules, items, offHandWeapon, Weapon.Hand.OFF_HAND_TWF, offSizeCategory, offsizeDiscrepancy, twfOff),
                                    Projectile(rules, items, offHandWeapon, Weapon.Hand.OFF_HAND_TWF, offSizeCategory, offsizeDiscrepancy, twfOff),
                                    Thrown(rules, items, offHandWeapon, Weapon.Hand.OFF_HAND_TWF, offSizeCategory, offsizeDiscrepancy, twfOff)
                                }
                            ));
                        }
                    }
                    if (spikesAvailable) {
                        fullAttacks.AddRange(Twf(
                            new List<Attack> {
                                Melee(rules, items, primaryHandWeapon, Weapon.Hand.PRIMARY_HAND_TWF, priSizeCategory, priSizeDiscrepancy, -4),
                                Projectile(rules, items, primaryHandWeapon, Weapon.Hand.PRIMARY_HAND_TWF, priSizeCategory, priSizeDiscrepancy, -4),
                                Thrown(rules, items, primaryHandWeapon, Weapon.Hand.PRIMARY_HAND_TWF, priSizeCategory, priSizeDiscrepancy, -4)
                            },
                            new List<Attack> {
                                ArmorSpikes(rules, items, Weapon.Hand.OFF_HAND_TWF, armSizeCategory, -8)
                            }
                        ));
                    }
                    if (shieldBashAvailable) {
                        Weapon.Effort offEffort = (Weapon.Effort)items.OffHandShield.ShieldBashEffort;
                        int twfPri = offEffort == Weapon.Effort.LIGHT ? -4 : -6;
                        int twfOff = offEffort == Weapon.Effort.LIGHT ? -8 : -10;
                        fullAttacks.AddRange(Twf(
                            new List<Attack> {
                                Melee(rules, items, primaryHandWeapon, Weapon.Hand.PRIMARY_HAND_TWF, priSizeCategory, priSizeDiscrepancy, -4),
                                Projectile(rules, items, primaryHandWeapon, Weapon.Hand.PRIMARY_HAND_TWF, priSizeCategory, priSizeDiscrepancy, -4),
                                Thrown(rules, items, primaryHandWeapon, Weapon.Hand.PRIMARY_HAND_TWF, priSizeCategory, priSizeDiscrepancy, -4)
                            },
                            new List<Attack> {
                                ShieldBash(rules, items, Weapon.Hand.OFF_HAND_TWF, shSizeCategory, shSizeDiscrepancy, twfOff)
                            }
                        ));
                    }
                }
            } 

            return fullAttacks;
        }
    }
}
