using System.Collections.Generic;

namespace PlayMore35.Items {
    public class AttackMode {
        public Weapon.Proficiency ProficencyCategory;
        public (int qty, int size) DamageDieMedium;
        public string DamageType;
        public int ThreatRange;
        public int CriticalMultiplier;
        public int RangeIncrement = 0;

        public List<WeaponProperty> Properties = new List<WeaponProperty>();

        public IEnumerable<Effects.Modifier> GetGlobalModifiers(Effects.EffectPool pool, Equipped items, Effects.Statistic affecting) {
            List<Effects.Modifier> ret = new List<Effects.Modifier>();
            foreach (WeaponProperty prop in Properties) {
                if (prop.GlobalEffect != null) {
                    ret.AddRange(prop.GlobalEffect.GetModifiers(pool, items, affecting));
                }
            }
            return ret;
        }
    }
}
