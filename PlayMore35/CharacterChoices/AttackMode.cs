using System;
using System.Collections.Generic;
using System.Linq;
using PlayMore35.Effects;
using Newtonsoft.Json.Linq;

namespace PlayMore35.Items {
    public class AttackMode : IJsonable {
        public Weapon.Proficiency ProficencyCategory;
        public (int qty, int size) DamageDieMedium;
        public string DamageType;
        public int ThreatRange = 20;
        public int CriticalMultiplier = 2;
        public int RangeIncrement = 0;

        // not serialized -- added after the fact
        public List<WeaponProperty> Properties = new List<WeaponProperty>();

        public JToken ToJson() {
            JObject json = new JObject(
                new JProperty("proficiency", ProficencyCategory),
                new JProperty("damage", DiceGroup.ToDNotation(DamageDieMedium)),
                new JProperty("type", DamageType),
                new JProperty("threat", ThreatRange),
                new JProperty("crit", CriticalMultiplier)
            );
            if (RangeIncrement > 0) {
                json["range increment"] = RangeIncrement;
            }
            return json;
        }

        public AttackMode(JObject json) {
            ProficencyCategory = (Weapon.Proficiency) Enum.Parse(typeof(Weapon.Proficiency), (string)json["proficiency"], false);
            DamageDieMedium = DiceGroup.FromDNotation((string)json["damage"]);
            DamageType = (string)json["type"];
            ThreatRange = (int)json["threat"];
            CriticalMultiplier = (int)json["crit"];

            if (json["range increment"] != null) {
                RangeIncrement = (int)json["range increment"];
            }
        }

        // Purposefully does not copy properties.
        public AttackMode(AttackMode copy) {
            ProficencyCategory = copy.ProficencyCategory;
            DamageDieMedium = copy.DamageDieMedium;
            DamageType = copy.DamageType;
            ThreatRange = copy.ThreatRange;
            CriticalMultiplier = copy.CriticalMultiplier;
            RangeIncrement = copy.RangeIncrement;
        }

        public AttackMode() { }

        public IEnumerable<Effect> GetGlobalEffects() {
            return Properties.Select(x => x.GlobalEffect).Where(x => x != null);
        }
    }
}
