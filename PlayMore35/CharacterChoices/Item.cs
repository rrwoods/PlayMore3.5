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

        public Item(
            Equipped.Slot itemSlot,
            Effect itemEffect,
            decimal weight = 0m,
            IEnumerable<Statistic>? newStats = null,
            IEnumerable<FlagDefinition>? newFlags = null) {
            ItemSlot = itemSlot;
            ItemEffect = itemEffect ?? throw new ArgumentNullException(nameof(itemEffect));
            Weight = weight;
            if (newStats != null) {
                NewStats.AddRange(newStats);
            }
            if (newFlags != null) {
                NewFlags.AddRange(newFlags);
            }
        }

        public Item(JObject json)
            : this(
                (Equipped.Slot) Enum.Parse(typeof(Equipped.Slot), (string)json["slot"], false),
                new Effect((JObject)json["effect"]),
                (decimal)json["weight"],
                json["stats"]?.Select(x => new Statistic((JObject)x)),
                json["flags"]?.Select(x => new FlagDefinition((JObject)x))) {
        }
    }
}
