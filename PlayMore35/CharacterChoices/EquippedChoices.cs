using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json.Linq;

namespace PlayMore35.CharacterChoices {
    public class EquippedChoices : IJsonable {
        public WeaponChoice PrimaryHandWeapon = null;
        public WeaponChoice OffHandWeapon = null;
        public WeaponChoice TwoHandWeapon = null;

        public ArmorChoice DonnedArmor = null;
        public ShieldChoice OffHandShield = null;

        public Dictionary<Items.Equipped.Slot, string> SlottedItems = new Dictionary<Items.Equipped.Slot, string>();
        public List<string> SlotlessItems = new List<string>();

        private void JsonProperty(JObject json, string key, IJsonable value) {
            if (value != null) {
                json[key] = value.ToJson();
            }
        }

        public JToken ToJson() {
            JObject json = new JObject();
            JsonProperty(json, "primary", PrimaryHandWeapon);
            JsonProperty(json, "offhand", OffHandWeapon);
            JsonProperty(json, "twohand", TwoHandWeapon);
            JsonProperty(json, "armor", DonnedArmor);
            JsonProperty(json, "shield", OffHandShield);
            json["slots"] = new JObject(SlottedItems.Select(x => new JProperty(x.Key.ToString(), x.Value)));
            json["slotless"] = new JArray(SlotlessItems);
            return json;
        }

        public EquippedChoices(JObject json) {
            if (json["primary"] != null) {
                PrimaryHandWeapon = new WeaponChoice((JObject)json["primary"]);
            }
            if (json["offhand"] != null) {
                OffHandWeapon = new WeaponChoice((JObject)json["offhand"]);
            }
            if (json["twohand"] != null) {
                TwoHandWeapon = new WeaponChoice((JObject)json["twohand"]);
            }
            if (json["armor"] != null) {
                DonnedArmor = new ArmorChoice((JObject)json["armor"]);
            }
            if (json["shield"] != null) {
                OffHandShield = new ShieldChoice((JObject)json["shield"]);
            }

            SlottedItems = ((JObject)json["slots"]).Properties().ToDictionary(
                x => (Items.Equipped.Slot)Enum.Parse(typeof(Items.Equipped.Slot), (string)x.Name, false),
                x => (string)x.Value
            );
            SlotlessItems = ((JArray)json["slotless"]).Select(x => (string)x).ToList();
        }

        public EquippedChoices() { }
    }
}
