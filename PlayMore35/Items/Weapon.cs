using System.Collections.Generic;
using System.Linq;
using PlayMore35.Effects;

namespace PlayMore35.Items {
    public class Weapon {
        public enum Proficiency { SIMPLE, MARTIAL, EXOTIC };
        public enum Effort { LIGHT, ONE_HANDED, TWO_HANDED };
        public enum Hand { ONE_HANDED, PRIMARY_HAND_TWF, OFF_HAND_TWF, TWO_HANDED };
        public enum Range { MELEE, REACH_MELEE, THROWN, BOW, OTHER_PROJECTILE };

        public string Name;
        public List<string> Tags = new List<string>();
        public int SizeCategory;
        public Effort EffortCategory;

        public (AttackMode atk, bool bow, int strengthRating)? Projectile = null;
        public AttackMode Thrown = null;
        public (AttackMode atk, bool reach)? Melee = null;
        public bool SecondIdenticalHead = false; // for double weapons like the quarterstaff
        public (AttackMode atk, bool reach)? SecondUniqueHead = null; // for double weapons like the hooked hammer

        public Weapon(string name, int sizeCategory, Effort effort) {
            Name = name;
            SizeCategory = sizeCategory;
            EffortCategory = effort;
        }

        public bool CanBeThrown() {
            if (Thrown != null) {
                return true;
            }
            if (Melee == null) {
                return false;
            }
            return Melee?.atk.RangeIncrement > 0;
        }

        public IEnumerable<Effect> GetGlobalEffects() {
            IEnumerable<Effect> effects = Enumerable.Empty<Effect>();
            if (Projectile != null) {
                effects = effects.Concat(Projectile?.atk.GetGlobalEffects());
            }
            if (Thrown != null) {
                effects = effects.Concat(Thrown.GetGlobalEffects());
            }
            if (Melee != null) {
                effects = effects.Concat(Melee?.atk.GetGlobalEffects());
            }
            if (SecondUniqueHead != null) {
                effects = effects.Concat(SecondUniqueHead?.atk.GetGlobalEffects());
            }
            return effects;
        }
    }
}
