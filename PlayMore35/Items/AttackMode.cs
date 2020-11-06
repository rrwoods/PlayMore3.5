using System.Collections.Generic;
using System.Linq;
using PlayMore35.Effects;

namespace PlayMore35.Items {
    public class AttackMode {
        public Weapon.Proficiency ProficencyCategory;
        public (int qty, int size) DamageDieMedium;
        public string DamageType;
        public int ThreatRange;
        public int CriticalMultiplier;
        public int RangeIncrement = 0;

        public List<WeaponProperty> Properties = new List<WeaponProperty>();

        public IEnumerable<Effect> GetGlobalEffects() {
            return Properties.Select(x => x.GlobalEffect).Where(x => x != null);
        }
    }
}
