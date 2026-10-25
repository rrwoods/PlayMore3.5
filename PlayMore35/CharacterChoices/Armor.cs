using System;
using System.Collections.Generic;
using System.Linq;
using PlayMore35.Effects;

using Newtonsoft.Json.Linq;

namespace PlayMore35.Items {
    public class Armor : IJsonable {
        public enum WeightCategory { LIGHT, MEDIUM, HEAVY };

        // Serialization of these fields is based on whether it's needed for the definition of the armor.
        // E.g. SizeCategory is not serialized because each armor can come in whatever size the player wants.

        public string Name;
        public int SizeCategory;
        public WeightCategory Type;
        public bool Exotic = false;
        public decimal Weight;

        // All of these fields should EXCLUDE modifiers from properties.  E.g., the ACP
        // reduction from masterwork and the armor bonus increase from enhancement
        // should not be included in these numbers.
        public int ArmorBonus;
        public int MaxDexBonus;
        public int ArmorCheckPenalty; // negative number
        public int ArcaneSpellFailure; // out of 100

        public AttackMode ArmorSpikes = null;

        public List<ArmorProperty> Properties = new List<ArmorProperty>();

        public JToken ToJson() {
            return new JObject(
                new JProperty("name", Name),
                new JProperty("type", Type),
                new JProperty("exotic", Exotic),
                new JProperty("weight", Weight),
                new JProperty("armor bonus", ArmorBonus),
                new JProperty("max dex", MaxDexBonus),
                new JProperty("armor check penalty", ArmorCheckPenalty),
                new JProperty("spell failure", ArcaneSpellFailure)
            );
        }

        public Armor(JObject json) {
            Name = (string)json["name"];
            SizeCategory = 0;
            Type = (WeightCategory) Enum.Parse(typeof(WeightCategory), (string)json["type"], false);
            if (json["exotic"] != null) {
                Exotic = (bool)json["exotic"];
            }
            Weight = (decimal)json["weight"];
            ArmorBonus = (int)json["armor bonus"];
            MaxDexBonus = (int)json["max dex"];
            ArmorCheckPenalty = (int)json["armor check penalty"];
            ArcaneSpellFailure = (int)json["spell failure"];
        }

        public Armor(Armor definition) {
            Name = definition.Name;
            SizeCategory = definition.SizeCategory;
            Type = definition.Type;
            Weight = definition.Weight;
            Exotic = definition.Exotic;
            ArmorBonus = definition.ArmorBonus;
            MaxDexBonus = definition.MaxDexBonus;
            ArmorCheckPenalty = definition.ArmorCheckPenalty;
            ArcaneSpellFailure = definition.ArcaneSpellFailure;
        }

        public Armor() { }

        public IEnumerable<Effect> GetGlobalEffects() {
            return Properties.Select(x => x.GlobalEffect).Where(x => x != null);
        }
    }
}
