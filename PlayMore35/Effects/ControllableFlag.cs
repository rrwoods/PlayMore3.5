using Newtonsoft.Json.Linq;

namespace PlayMore35.Effects {
    public class ControllableFlag : IJsonable {
        public string Name;
        public string Category;
        public string Description;
        public bool On; // not serialized, because it's tied to the character.

        public JToken ToJson() {
            return new JObject(
                new JProperty("name", Name),
                new JProperty("category", Category),
                new JProperty("description", Description)
            );
        }

        public ControllableFlag(JObject json) {
            Name = (string)json["name"];
            Category = (string)json["category"];
            Description = (string)json["description"];
            On = false;
        }
    }
}
