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

        public JToken ToJson() {
            return new JObject(
                new JProperty("name", Name),
                new JProperty("size", SizeCategory),
                new JProperty("spiked", Spiked),
                new JProperty("properties", new JArray(ArmorProperties)),
                new JProperty("spikes properties", new JArray(MeleeProperties))
            );
        }

        public ArmorChoice(JObject json) {
            Name = (string)json["name"];
            SizeCategory = (int)json["size"];
            Spiked = (bool)json["spiked"];
            ArmorProperties = ((JArray)json["properties"]).Select(x => (string)x).ToList();
            MeleeProperties = ((JArray)json["spikes properties"]).Select(x => (string)x).ToList();
        }

        public ArmorChoice() {
            Name = "";
            SizeCategory = 0;
            Spiked = false;
            ArmorProperties = new List<string>();
            MeleeProperties = new List<string>();
        }

        public string FullName() {
            return
                string.Join("", MeleeProperties.Select(x => x + " ")) +
                (Spiked ? "spiked " : "") +
                string.Join("", ArmorProperties.Select(x => x + " ")) +
                Name;
        }

        public override string ToString() {
            return FullName();
        }
    }
}
