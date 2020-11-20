using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json.Linq;

namespace PlayMore35.CharacterChoices {
    public class InventoryLocation : IJsonable {
        public bool weighs = true; // whether items in this inventory location count toward total weight

        public List<WeaponChoice> weapons;
        public List<ArmorChoice> armor;
        public List<ShieldChoice> shields;

        public List<string> items;

        public JToken ToJson() {
            return new JObject(
                new JProperty("weighs", weighs),
                new JProperty("weapons", new JArray(weapons.Select(x => x.ToJson()))),
                new JProperty("armor", new JArray(armor.Select(x => x.ToJson()))),
                new JProperty("shields", new JArray(shields.Select(x => x.ToJson()))),
                new JProperty("items", new JArray(items))
            );
        }

        public InventoryLocation(JObject json) {
            weighs = (bool)json["weighs"];
            weapons = ((JArray)json["weapons"]).Select(x => new WeaponChoice((JObject)x)).ToList();
            armor = ((JArray)json["armor"]).Select(x => new ArmorChoice((JObject)x)).ToList();
            shields = ((JArray)json["shields"]).Select(x => new ShieldChoice((JObject)x)).ToList();
            items = ((JArray)json["items"]).Select(x => (string)x).ToList();
        }

        public InventoryLocation() { }
    }
}
