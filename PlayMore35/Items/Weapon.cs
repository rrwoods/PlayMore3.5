using System.Collections.Generic;
using System.Linq;

namespace PlayMore35.Items {
    public class Weapon {
        public enum Proficiency { SIMPLE, MARTIAL, EXOTIC };
        public enum Effort { LIGHT, ONE_HANDED, TWO_HANDED };
        public enum Hand { ONE_HANDED, PRIMARY_HAND_TWF, OFF_HAND_TWF, TWO_HANDED };
        public enum Range { MELEE, REACH_MELEE, THROWN, BOW, OTHER_PROJECTILE };

        public string Name;
        public List<string> Tags = new List<string>();
        public int SizeCategory;

        public (AttackMode atk, bool bow, int strengthRating)? Projectile = null;
        public AttackMode Thrown = null;
        public (AttackMode atk, bool reach)? Melee = null;
        public bool SecondIdenticalHead = false; // for double weapons like the quarterstaff
        public (AttackMode atk, bool reach)? SecondUniqueHead = null; // for double weapons like the hooked hammer

        public Weapon(string name, int sizeCategory) {
            Name = name;
            SizeCategory = sizeCategory;
        }

        public Effort? ThrownEffort() {
            if (Thrown != null) {
                return Thrown?.EffortCategory;
            }
            if (Melee == null) {
                return null;
            }
            if (Melee?.atk.RangeIncrement > 0) {
                return Melee?.atk.EffortCategory;
            }
            return null;
        }

        public IEnumerable<Effects.Modifier> GetGlobalModifiers(Effects.EffectPool pool, Effects.Statistic affecting) {
            IEnumerable<Effects.Modifier> mods = Enumerable.Empty<Effects.Modifier>();
            if (Projectile != null) {
                mods = mods.Concat(Projectile?.atk.GetGlobalModifiers(pool, affecting));
            }
            if (Thrown != null) {
                mods = mods.Concat(Thrown.GetGlobalModifiers(pool, affecting));
            }
            if (Melee != null) {
                mods = mods.Concat(Melee?.atk.GetGlobalModifiers(pool, affecting));
            }
            if (SecondUniqueHead != null) {
                mods = mods.Concat(SecondUniqueHead?.atk.GetGlobalModifiers(pool, affecting));
            }
            return mods;
        }
    }
}
