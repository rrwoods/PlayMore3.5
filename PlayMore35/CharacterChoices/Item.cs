using System;

using Newtonsoft.Json.Linq;
using PlayMore35.Effects;

namespace PlayMore35.Items {
    public class Item : IJsonable {
        public Equipped.Slot ItemSlot;
        public Effect ItemEffect;

        public JToken ToJson() {
            return new JObject(
                new JProperty("slot", ItemSlot),
                new JProperty("effect", ItemEffect.ToJson())
            );
        }

        public Item(JObject json) {
            ItemSlot = (Equipped.Slot) Enum.Parse(typeof(Equipped.Slot), (string)json["slot"], false);
            ItemEffect = new Effect((JObject)json["effect"]);
        }

        public Item() { }
    }
}
