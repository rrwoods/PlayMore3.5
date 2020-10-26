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

        // {die size => quantity}
        public Dictionary<int, int> DamageDiceMedium = new Dictionary<int, int> { [4] = 1 }; // 1d4

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
