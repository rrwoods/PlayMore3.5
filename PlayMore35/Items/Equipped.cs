using System.Collections.Generic;
using System.Linq;
using PlayMore35.Effects;

namespace PlayMore35.Items {
    public class Equipped {
        public Weapon PrimaryHandWeapon = null;
        public Weapon OffHandWeapon = null;

        public IEnumerable<Effect> GetGlobalEffects() {
            IEnumerable<Effect> all = Enumerable.Empty<Effect>();
            if (PrimaryHandWeapon != null) {
                all = all.Concat(PrimaryHandWeapon.GetGlobalEffects());
            }
            if (OffHandWeapon != null) {
                all = all.Concat(OffHandWeapon.GetGlobalEffects());
            }
            return all;
        }
    }
}
