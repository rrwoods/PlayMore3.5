
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

        public string DamageDieMedium = "1d4";
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

        public int GetAttackBonus(GameRules.GameRules rules) {
            string attackStat = (IsRanged() ? RANGED : MELEE) + ATTACK_ROLLS + WITH + W.Name;
            return rules.Stats[attackStat].GetValue(rules.Rules);
        }
    }
}
