using System.Collections.Generic;

using static PlayMore35.GameRules.Constants;

namespace PlayMore35.Items {
    public class Attack {
        private readonly Weapon W;

        public Attack(Weapon w) {
            W = w;
        }

        public enum Type { MANUFACTURED, PRIMARY_NATURAL, SECONDARY_NATURAL };
        public enum Proficiency { SIMPLE, MARTIAL, EXOTIC };
        public enum Effort { LIGHT, ONE_HANDED, TWO_HANDED };
        public enum Range { MELEE, REACH_MELEE, THROWN, PROJECTILE };

        public Type AttackType = Type.MANUFACTURED;
        public Proficiency ProficencyCategory = Proficiency.SIMPLE;
        public Effort EffortCategory = Effort.LIGHT;
        public Range RangeCategory = Range.MELEE;
        public bool OffHand = false;

        public (int qty, int size) DamageDieMedium = (1, 4);

        public int ThreatRange = 19;
        public int CriticalMultiplier = 2;
        public int RangeIncrement = 0;
        public int StrengthRating = -1; // only relevant for ranged weapons (composite bows); -1 = noncomposite
        public string DamageType = PIERCING + OR + SLASHING;

        public bool IsRanged() {
            switch (RangeCategory) {
                case Range.MELEE:
                case Range.REACH_MELEE:
                    return false;
                case Range.PROJECTILE:
                case Range.THROWN:
                    return true;
            }
            return false;
        }

        // keys are the Medium damage dice
        // values are lists of damage dice ordered from Fine to Colossal
        private static readonly Dictionary<(int, int), List<(int, int)>> damageSteps =
            new Dictionary<(int, int), List<(int, int)>> {
                [(1,2)]  = new List<(int, int)> { (0,1), (0,1), (0,1), (1,1),  (1,2),  (1,3), (1,4), (1,6), (1,8) },
                [(1,3)]  = new List<(int, int)> { (0,1), (0,1), (1,1), (1,2),  (1,3),  (1,4), (1,6), (1,8), (2,6) },
                [(1,4)]  = new List<(int, int)> { (0,1), (1,1), (1,2), (1,3),  (1,4),  (1,6), (1,8), (2,6), (3,6) },
                [(1,6)]  = new List<(int, int)> { (1,1), (1,2), (1,3), (1,4),  (1,6),  (1,8), (2,6), (3,6), (4,6) },
                [(2,4)]  = new List<(int, int)> { (1,2), (1,3), (1,4), (1,6),  (2,4),  (2,6), (3,6), (4,6), (6,6) },
                [(1,8)]  = new List<(int, int)> { (1,2), (1,3), (1,4), (1,6),  (1,8),  (2,6), (3,6), (4,6), (6,6) },
                [(1,10)] = new List<(int, int)> { (1,3), (1,4), (1,6), (1,8),  (1,10), (2,8), (3,8), (4,8), (6,8) },
                [(1,12)] = new List<(int, int)> { (1,4), (1,6), (1,8), (1,10), (1,12), (3,6), (4,6), (6,6), (8,6) },
                [(2,6)]  = new List<(int, int)> { (1,4), (1,6), (1,8), (1,10), (2,6),  (3,6), (4,6), (6,6), (8,6) },
            };

        // sizeChange = how many size categories up or down this weapon is from its "normal" state.
        // use this for things like Enlarge Person.  it will clamp to the -4 to +4 range automatically.
        // this function always creates a new dice group for the return value, so modifying it won't affect future results.
        // it will only ever have one key in it, but this format facilitates adding other dice of different sizes later (like
        // modifying the damage via sneak attack for example).
        public Effects.DiceGroup BaseDamageDice(int sizeChange = 0) {
            int sizeCategory = W.SizeCategory + sizeChange;
            if (sizeCategory == 0) {
                return new Effects.DiceGroup(DamageDieMedium.qty, DamageDieMedium.size);
            }

            if (sizeCategory < -4) {
                sizeCategory = -4;
            }
            if (sizeCategory > +4) {
                sizeCategory = +4;
            }

            return new Effects.DiceGroup(damageSteps[DamageDieMedium][sizeCategory + 4]);
        }
    }

    public static class AttackNames {
        public static string Name(this Attack.Type x) {
            switch (x) {
                case Attack.Type.MANUFACTURED:
                    return MANUFACTURED;
                case Attack.Type.PRIMARY_NATURAL:
                case Attack.Type.SECONDARY_NATURAL:
                    return NATURAL;
            }
            return null;
        }

        public static string Name(this Attack.Proficiency x) {
            switch (x) {
                case Attack.Proficiency.SIMPLE:
                    return SIMPLE;
                case Attack.Proficiency.MARTIAL:
                    return MARTIAL;
                case Attack.Proficiency.EXOTIC:
                    return EXOTIC;
            }
            return null;
        }

        public static string Name(this Attack.Effort x) {
            switch (x) {
                case Attack.Effort.LIGHT:
                    return LIGHT;
                case Attack.Effort.ONE_HANDED:
                    return ONE_HANDED;
                case Attack.Effort.TWO_HANDED:
                    return TWO_HANDED;
            }
            return null;
        }

        public static string Name(this Attack.Range x) {
            switch (x) {
                case Attack.Range.MELEE:
                case Attack.Range.REACH_MELEE:
                    return MELEE;
                case Attack.Range.THROWN:
                case Attack.Range.PROJECTILE:
                    return RANGED;
            }
            return null;
        }

        public static string FullName(this Attack.Range x) {
            switch (x) {
                case Attack.Range.MELEE:
                    return MELEE_NAME;
                case Attack.Range.REACH_MELEE:
                    return REACH;
                case Attack.Range.THROWN:
                    return THROWN;
                case Attack.Range.PROJECTILE:
                    return PROJECTILE;
            }
            return null;
        }
    }


}
