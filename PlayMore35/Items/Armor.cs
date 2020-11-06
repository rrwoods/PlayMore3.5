using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayMore35.Effects;

namespace PlayMore35.Items {
    public class Armor {
        public string Name;
        public int sizeCategory;

        // All of these fields should EXCLUDE modifiers from properties.  E.g., the ACP
        // reduction from masterwork and the armor bonus increase from enhancement
        // should not be included in these numbers.
        public int ArmorBonus;
        public int MaxDexBonus;
        public int ArmorCheckPenalty; // negative number
        public int ArcaneSpellFailure; // out of 100

        public AttackMode ArmorSpikes = null;

        public List<ArmorProperty> Properties = new List<ArmorProperty>();

        public IEnumerable<Effect> GetGlobalEffects() {
            return Properties.Select(x => x.GlobalEffect).Where(x => x != null);
        }
    }
}
