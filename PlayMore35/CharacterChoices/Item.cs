#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;

namespace PlayMore35.Items {
    public class Item : IJsonable {
        public Equipped.Slot ItemSlot;
        public Effect ItemEffect;
        public decimal Weight;

        public List<Statistic> NewStats = new List<Statistic>();
        public List<FlagDefinition> NewFlags = new List<FlagDefinition>();

        public JToken ToJson() {
            JObject json = new JObject(
                new JProperty("slot", ItemSlot),
                new JProperty("effect", ItemEffect.ToJson()),
                new JProperty("weight", Weight)
            );
            json.AddList("stats", NewStats);
            json.AddList("flags", NewFlags);
            return json;
        }

        public Item(JObject json) {
            ItemSlot = (Equipped.Slot) Enum.Parse(typeof(Equipped.Slot), (string)json["slot"], false);
            ItemEffect = new Effect((JObject)json["effect"]);
            Weight = (decimal)json["weight"];
            if (json["stats"] != null) {
                NewStats.AddRange(json["stats"].Select(x => new Statistic((JObject)x)));
            }
            if (json["flags"] != null) {
                NewFlags.AddRange(json["flags"].Select(x => new FlagDefinition((JObject)x)));
            }
        }

        public Item() { }
    }
}
