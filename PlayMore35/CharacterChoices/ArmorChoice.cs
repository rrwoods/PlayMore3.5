using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace PlayMore35.CharacterChoices {
    public class ArmorChoice : IJsonable {
        public string Name;
        public int SizeCategory;
        public bool Spiked;
        public List<string> ArmorProperties;
        public List<string> MeleeProperties;

        public bool IsShield = false;

        public JToken ToJson() {
            return new JObject(
                new JProperty("name", Name),
                new JProperty("size", SizeCategory),
                new JProperty("spiked", Spiked),
                new JProperty("properties", new JArray(ArmorProperties)),
                new JProperty("melee properties", new JArray(MeleeProperties))
            );
        }

        public ArmorChoice(JObject json) {
            Name = (string)json["name"];
            SizeCategory = (int)json["size"];
            Spiked = (bool)json["spiked"];
            ArmorProperties = ((JArray)json["properties"]).Select(x => (string)x).ToList();
            MeleeProperties = ((JArray)json["melee properties"]).Select(x => (string)x).ToList();
        }

        public ArmorChoice() {
            Name = "";
            SizeCategory = 0;
            Spiked = false;
            ArmorProperties = new List<string>();
            MeleeProperties = new List<string>();
        }

        public string FullName() {
            // if it's a shield without spikes, its properties are ambiguous:
            // is a "mwk light wooden shield" actually a mwk shield, or is its *attack* mwk?
            // for now i'm just introducing the word "bash" after the melee properties, if
            // it has any (so the above would be an actual mwk shield, and if it were instead
            // a mwk weapon, it would be "mwk bash light wooden shield")
            string attackName =
                Spiked
                    ? "spiked "
                    : (MeleeProperties.Any() ? "bash " : ""); 

            return
                string.Join("", MeleeProperties.Select(x => x + " ")) +
                attackName +
                string.Join("", ArmorProperties.Select(x => x + " ")) +
                Name;
        }

        public override string ToString() {
            return FullName();
        }
    }
}
