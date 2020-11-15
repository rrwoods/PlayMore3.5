using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace PlayMore35.Character {
    public class ShieldChoice : IJsonable {
        public string Name;
        public int SizeCategory;
        public List<string> ArmorProperties;
        public List<string> MeleeProperties;

        public JToken ToJson() {
            return new JObject(
                new JProperty("name", Name),
                new JProperty("size", SizeCategory),
                new JProperty("properties", new JArray(ArmorProperties)),
                new JProperty("spikes properties", new JArray(MeleeProperties))
            );
        }

        public ShieldChoice(JObject json) {
            Name = (string)json["name"];
            SizeCategory = (int)json["size"];
            ArmorProperties = ((JArray)json["properties"]).Select(x => (string)x).ToList();
            MeleeProperties = ((JArray)json["spikes properties"]).Select(x => (string)x).ToList();
        }

        public ShieldChoice() {
            Name = "";
            SizeCategory = 0;
            ArmorProperties = new List<string>();
            MeleeProperties = new List<string>();
        }
    }
}
