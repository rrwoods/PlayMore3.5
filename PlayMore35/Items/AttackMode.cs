using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Items {
    public class AttackMode {
        public Weapon.Proficiency ProficencyCategory;
        public Weapon.Effort EffortCategory;
        public (int qty, int size) DamageDieMedium;
        public string DamageType;
        public int ThreatRange;
        public int CriticalMultiplier;
        public int RangeIncrement = 0;

        public List<WeaponProperty> Properties = new List<WeaponProperty>();

        public IEnumerable<Effects.Modifier> GetGlobalModifiers(Effects.EffectPool pool, Effects.Statistic affecting) {
            List<Effects.Modifier> ret = new List<Effects.Modifier>();
            foreach (WeaponProperty prop in Properties) {
                if (prop.GlobalEffect != null) {
                    ret.AddRange(prop.GlobalEffect.GetModifiers(pool, affecting));
                }
            }
            return ret;

            //return Properties.SelectMany(x => x.GlobalEffect?.GetModifiers(pool, affecting) ?? Enumerable.Empty<Effects.Modifier>());
        }
    }
}
