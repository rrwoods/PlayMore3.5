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
        public DiceGroup BaseDamage { get; private set; }
        public IReadOnlyList<DiceGroup> BonusDice { get; private set; } = new List<DiceGroup>();
        public IReadOnlyList<DiceGroup> CriticalBonusDice { get; private set; } = new List<DiceGroup>();
        public int CriticalMultiplier { get; private set; } = 2;
        public List<string> DamageExplanation { get; private set; } = new List<string>();
        public List<string> CriticalDamageExplanation { get; private set; } = new List<string>();

        public DiceGroup TotalDamage {
            get {
                DiceGroup total = new DiceGroup(WeaponName + " damage");
                if (BaseDamage != null) {
                    total.AddDice(BaseDamage);
                }
                if (BonusDice != null) {
                    foreach (var die in BonusDice) {
                        total.AddDice(die);
                    }
                }
                return total;
            }
        }

        public DiceGroup CriticalBaseDamage {
            get {
                DiceGroup critBase = new DiceGroup(WeaponName + " critical base damage");
                if (BaseDamage != null) {
                    critBase.AddDice(BaseDamage);
                }
                critBase.Multiply(CriticalMultiplier);
                return critBase;
            }
        }

        public DiceGroup CriticalTotalDamage {
            get {
                DiceGroup critTotal = new DiceGroup(WeaponName + " critical total damage");
                critTotal.AddDice(CriticalBaseDamage);
                if (CriticalBonusDice != null) {
                    foreach (var die in CriticalBonusDice) {
                        critTotal.AddDice(die);
                    }
                }
                return critTotal;
            }
        }

        public (DiceGroup dice, List<string> explanation) Damage => (TotalDamage, DamageExplanation);
        public (DiceGroup dice, List<string> explanation) CriticalDamage => (CriticalTotalDamage, CriticalDamageExplanation);
        public int ThreatRange { get; private set; }
        public int RangeIncrement { get; private set; }
        public int StrengthRating { get; private set; } = -1; // only relevant for ranged weapons (composite bows); -1 = noncomposite

        public static Attack CreateForTesting(
            string name,
            int bonus,
            DiceGroup baseDamage,
            IEnumerable<DiceGroup> bonusDice = null,
            IEnumerable<DiceGroup> criticalBonusDice = null,
            int criticalMultiplier = 2,
            int threatRange = 20,
            int rangeIncrement = 0
        ) {
            var bDice = (bonusDice ?? Enumerable.Empty<DiceGroup>()).ToList();
            var cbDice = criticalBonusDice != null
                ? criticalBonusDice.ToList()
                : new List<DiceGroup>(bDice);

            return new Attack(name, bonus, baseDamage, bDice, cbDice, criticalMultiplier, threatRange, rangeIncrement);
        }

        private Attack(
            string name,
            int bonus,
            DiceGroup baseDamage,
            List<DiceGroup> bonusDice,
            List<DiceGroup> criticalBonusDice,
            int criticalMultiplier,
            int threatRange,
            int rangeIncrement
        ) {
            WeaponName = name;
            WeaponTags = new List<string>();
            AttackBonus = (bonus, new List<string> { $"+{bonus} base attack" });
            BaseDamage = baseDamage;
            BonusDice = bonusDice.AsReadOnly();
            CriticalBonusDice = criticalBonusDice.AsReadOnly();
            CriticalMultiplier = criticalMultiplier;
            ThreatRange = threatRange;
            RangeIncrement = rangeIncrement;
            DamageExplanation = new List<string> { baseDamage?.ToString() ?? "" };
            CriticalDamageExplanation = new List<string> { $"{baseDamage?.ToString()} ×{criticalMultiplier}" };
        }

        public Attack WithIterativePenalty(int penalty, string explanation = null) {
            var copy = (Attack)this.MemberwiseClone();
            var expl = new List<string>(this.AttackBonus.explanation);
            if (!string.IsNullOrEmpty(explanation)) {
                expl.Add(explanation);
            } else if (penalty != 0) {
                expl.Add($"{penalty.ToString("+#;–#;0")} iterative attack");
            }
            copy.AttackBonus = (this.AttackBonus.bonus + penalty, expl);
            return copy;
        }

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
            ThreatRange = atk.ThreatRange;
        }

        private Attack(Armor armor, int wpnSizeCategory, Weapon.Hand hand, bool familiar) {
            WeaponName = "armor spikes";
            WeaponTags = new List<string>(); // can armor have weapon tags?
            WeaponSizeCategory = wpnSizeCategory;

            Proficiency = familiar ? Weapon.Proficiency.MARTIAL : armor.ArmorSpikes.ProficencyCategory;
            Effort = Weapon.Effort.LIGHT;
            Hand = hand;
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

            DiceGroup baseDamage = BaseDamageDice(sizeCategory, damageDieMedium);
            string baseExpl = baseDamage.ToString() + " " + baseDamage.Name;
            List<string> explanation = new List<string>() {
                baseExpl
            };

            IEnumerable<Modifier> dmgModsFromProps = properties
                .Select(x => x.DamageModifier)
                .Where(x => x != null);
            IEnumerable<Modifier> damageModifiers = rules.GetDamageModifiers(items, this)
                .Concat(dmgModsFromProps);

            (int mod, List<string> modExpl) = StackModifiers(damageModifiers, rules, items);
            baseDamage.AddDice(mod + compositeStrToDamage, 1);
            explanation.AddRange(modExpl);
            if (compositeStrToDamage > 0) {
                explanation.Add($"+{compositeStrToDamage} STR to composite bow damage");
            }

            BaseDamage = baseDamage;
            CriticalMultiplier = criticalMultiplier;

            List<string> critExplanation = new List<string>();
            critExplanation.AddRange(explanation);
            critExplanation.Add($"Critical hit ×{criticalMultiplier}");

            List<DiceGroup> bonusDiceList = new List<DiceGroup>();
            List<DiceGroup> critBonusDiceList = new List<DiceGroup>();

            IEnumerable<DiceGroup> extraDiceFromProps = properties
                .Where(x => x.ExtraDamage.qty != 0)
                .Select(x => new DiceGroup(x.Name).AddDice(x.ExtraDamage.qty, x.ExtraDamage.size));
            IEnumerable<DiceGroup> extraDice = rules.GetExtraDice(items, this)
                .Concat(extraDiceFromProps);
            foreach (DiceGroup extra in extraDice) {
                bonusDiceList.Add(extra);
                critBonusDiceList.Add(extra);
                explanation.Add(extra.ToString() + " " + extra.Name);
                critExplanation.Add(extra.ToString() + " " + extra.Name);
            }

            BonusDice = bonusDiceList.AsReadOnly();
            CriticalBonusDice = critBonusDiceList.AsReadOnly();
            DamageExplanation = explanation;
            CriticalDamageExplanation = critExplanation;

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
    }
}
