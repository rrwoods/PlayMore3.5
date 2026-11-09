using PlayMore35.Effects;

using Newtonsoft.Json.Linq;
using static PlayMore35.Effects.Constants;

namespace PlayMore35.Items {
    public class ArmorProperty : IJsonable {
        public readonly string Name;

        public Effect GlobalEffect = null;
        public Modifier AcModifier = null;
        public Modifier MaxDexModifier = null;
        public Modifier AcpModifier = null; // ACP is negative, so a "reduction" here should be positive
        public Modifier AsfModifier = null;

        public JToken ToJson() {
            JObject json = new JObject(new JProperty("name", Name));
            if (GlobalEffect != null) {
                json["effect"] = GlobalEffect.ToJson();
            }
            if (AcModifier != null) {
                json["AC modifier"] = AcModifier.ToJson();
            }
            if (MaxDexModifier != null) {
                json["max dex modifier"] = MaxDexModifier.ToJson();
            }
            if (AcpModifier != null) {
                json["armor check penalty modifier"] = AcpModifier.ToJson();
            }
            if (AsfModifier != null) {
                json["spell failure modifier"] = AsfModifier.ToJson();
            }
            return json;
        }

        public ArmorProperty(
            string name,
            Effect? globalEffect = null,
            Modifier? acModifier = null,
            Modifier? maxDexModifier = null,
            Modifier? acpModifier = null,
            Modifier? asfModifier = null) {
            Name = name ?? "";
            GlobalEffect = globalEffect;
            AcModifier = acModifier;
            MaxDexModifier = maxDexModifier;
            AcpModifier = acpModifier;
            AsfModifier = asfModifier;
        }

        public ArmorProperty(JObject json)
            : this(
                (string)json["name"],
                json["effect"] != null ? new Effect((JObject)json["effect"]) : null,
                json["AC modifier"] != null ? new Modifier((JObject)json["AC modifier"]) : null,
                json["max dex modifier"] != null ? new Modifier((JObject)json["max dex modifier"]) : null,
                json["armor check penalty modifier"] != null ? new Modifier((JObject)json["armor check penalty modifier"]) : null,
                json["spell failure modifier"] != null ? new Modifier((JObject)json["spell failure modifier"]) : null) {
        }
    }
}
