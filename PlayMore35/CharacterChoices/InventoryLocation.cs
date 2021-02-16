using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json.Linq;

namespace PlayMore35.CharacterChoices {
    public class InventoryLocation : IJsonable {
        public bool weighs = true; // whether items in this inventory location count toward total weight

        public List<WeaponChoice> weapons;
        public List<ArmorChoice> armor;
        public List<ArmorChoice> shields;

        public Dictionary<string, int> items;

        public JToken ToJson() {
            return new JObject(
                new JProperty("weighs", weighs),
                new JProperty("weapons", new JArray(weapons.Select(x => x.ToJson()))),
                new JProperty("armor", new JArray(armor.Select(x => x.ToJson()))),
                new JProperty("shields", new JArray(shields.Select(x => x.ToJson()))),
                new JProperty("items", new JObject(items.Select(x => new JProperty(x.Key, x.Value))))
            );
        }

        public InventoryLocation(JObject json) {
            weighs = (bool)json["weighs"];
            weapons = ((JArray)json["weapons"]).Select(x => new WeaponChoice((JObject)x)).ToList();
            armor = ((JArray)json["armor"]).Select(x => new ArmorChoice((JObject)x)).ToList();
            shields = ((JArray)json["shields"]).Select(x => new ArmorChoice((JObject)x) { IsShield = true }).ToList();
            items = ((JObject)json["items"]).Properties().ToDictionary(x => x.Name, x => (int)x.Value);
        }

        public InventoryLocation() {
            weapons = new List<WeaponChoice>();
            armor = new List<ArmorChoice>();
            shields = new List<ArmorChoice>();
            items = new Dictionary<string, int>();
        }
    }
}
