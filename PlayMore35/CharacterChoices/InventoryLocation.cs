using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json.Linq;

namespace PlayMore35.CharacterChoices {
    public class InventoryLocation : IJsonable {
        public List<WeaponChoice> weapons;
        public List<ArmorChoice> armor;
        public List<ShieldChoice> shields;

        // other items coming soon(tm)

        public JToken ToJson() {
            return new JObject(
                new JProperty("weapons", new JArray(weapons.Select(x => x.ToJson()))),
                new JProperty("armor", new JArray(armor.Select(x => x.ToJson()))),
                new JProperty("shields", new JArray(shields.Select(x => x.ToJson())))
            );
        }

        public InventoryLocation(JObject json) {
            weapons = ((JArray)json["weapons"]).Select(x => new WeaponChoice((JObject)x)).ToList();
            armor = ((JArray)json["armor"]).Select(x => new ArmorChoice((JObject)x)).ToList();
            shields = ((JArray)json["shields"]).Select(x => new ShieldChoice((JObject)x)).ToList();
        }

        public InventoryLocation() { }
    }
}
