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
        public int RangeIncrement;
    }
}
