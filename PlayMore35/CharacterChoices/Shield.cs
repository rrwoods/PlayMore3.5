using System;
using System.Collections.Generic;
using System.Linq;
using PlayMore35.Effects;

using Newtonsoft.Json.Linq;

namespace PlayMore35.Items {
    public class Shield : IJsonable {
        // Serialization of these fields is based on whether it's needed for the definition of the shield.
        // E.g. SizeCategory is not serialized because each shield can come in whatever size the player wants.

        public string Name;
        public int SizeCategory;
        public bool Exotic = false;
        public decimal Weight;

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

        public bool Spiked = false;
        public Weapon.Effort? ShieldBashEffort = null;
        public AttackMode ShieldBash = null;

        public List<ArmorProperty> Properties = new List<ArmorProperty>();

        public JToken ToJson() {
            JObject json = new JObject(
                new JProperty("name", Name),
                new JProperty("exotic", Exotic),
                new JProperty("weight", Weight),
                new JProperty("shield bonus", ShieldBonus),
                new JProperty("tower", Tower),
                new JProperty("armor check penalty", ArmorCheckPenalty),
                new JProperty("spell failure", ArcaneSpellFailure)
            );
            if (MaxDexBonus != null) {
                json.Add("max dex", MaxDexBonus);
            }
            if (AttackRollPenalty != null) {
                json.Add("attack", AttackRollPenalty);
            }
            if (ShieldBashEffort != null) {
                json.Add("effort", ShieldBashEffort.ToString());
            }
            return json;
        }

        public Shield(JObject json) {
            Name = (string)json["name"];
            if (json["exotic"] != null) {
                Exotic = (bool)json["exotic"];
            }
            Weight = (decimal)json["weight"];
            ShieldBonus = (int)json["shield bonus"];
            Tower = (bool)json["tower"];
            ArmorCheckPenalty = (int)json["armor check penalty"];
            ArcaneSpellFailure = (int)json["spell failure"];
            if (json["max dex"] != null) {
                MaxDexBonus = (int)json["max dex"];
            }
            if (json["attack"] != null) {
                AttackRollPenalty = (int)json["attack"];
            }
            if (json["effort"] != null) {
                ShieldBashEffort = (Weapon.Effort) Enum.Parse(typeof(Weapon.Effort), (string)json["effort"], false);
            }
        }

        public Shield(Shield definition) {
            Name = definition.Name;
            Exotic = definition.Exotic;
            Weight = definition.Weight;
            ShieldBonus = definition.ShieldBonus;
            Tower = definition.Tower;
            ArmorCheckPenalty = definition.ArmorCheckPenalty;
            ArcaneSpellFailure = definition.ArcaneSpellFailure;
            MaxDexBonus = definition.MaxDexBonus;
            AttackRollPenalty = definition.AttackRollPenalty;
            ShieldBashEffort = definition.ShieldBashEffort;
            AddBash();
        }

        public Shield() { }

        public IEnumerable<Effect> GetGlobalEffects() {
            IEnumerable<Effect> all = Properties.Select(x => x.GlobalEffect).Where(x => x != null);
            if (ShieldBash != null) {
                all = all.Concat(ShieldBash.GetGlobalEffects());
            }
            return all;
        }

        private void AddBash() {
            if (ShieldBashEffort == null) {
                return;
            }
            ShieldBash = new AttackMode {
                ProficencyCategory = Weapon.Proficiency.MARTIAL,
                DamageDieMedium = (ShieldBashEffort == Weapon.Effort.LIGHT) ? (1,3) : (1,4),
                DamageType = Constants.BLUDGEONING,
                ThreatRange = 20,
                CriticalMultiplier = 2
            };
        }
    }
}