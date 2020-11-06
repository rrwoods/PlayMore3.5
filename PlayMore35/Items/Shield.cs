using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayMore35.Effects;

namespace PlayMore35.Items {
    public class Shield {
        public string Name;
        public int SizeCategory;

        // All of these fields should EXCLUDE modifiers from properties.  E.g., the ACP
        // reduction from masterwork and the shield bonus increase from enhancement
        // should not be included in these numbers.
        public int ShieldBonus;
        public bool Tower = false; // True = you can use this shield for cover instead of AC
        public int? MaxDexBonus = null; // null means it does not impose a max dex bonus (most shields are this way)
        public int ArmorCheckPenalty; // negative number
        public int ArcaneSpellFailure; // out of 100

        // If this is a number, it represents the penalty imposed on attack rolls if you're opting to use
        // a weapon in held the same hand as this shield.  See https://www.d20srd.org/srd/equipment/armor.htm#buckler .
        // If this is null, it means you can't use a weapon held in the same hand as this shield at all.
        // In core, only a buckler lets you also weild a weapon, and it imposes a -1 penalty on all attack rolls.
        // Light, heavy, and tower shields should all leave this null.
        public int? AttackRollPenalty = null; // negative number

        // It's up to the user of this class to account for shield spikes.
        public Weapon.Effort? ShieldBashEffort = null;
        public AttackMode ShieldBash = null;

        public List<ArmorProperty> Properties = new List<ArmorProperty>();

        public IEnumerable<Effect> GetGlobalEffects() {
            IEnumerable<Effect> all = Properties.Select(x => x.GlobalEffect).Where(x => x != null);
            if (ShieldBash != null) {
                all = all.Concat(ShieldBash.GetGlobalEffects());
            }
            return all;
        }
    }
}