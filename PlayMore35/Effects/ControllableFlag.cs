using Newtonsoft.Json.Linq;

namespace PlayMore35.Effects {
    public class ControllableFlag : IJsonable {
        public string Name;
        public string Category;
        public string Description;
        public IToggle Show = AlwaysOn.Instance;

        public ActionResource Resource = null;

        public bool On; // not serialized, because it's tied to the character.

        public JToken ToJson() {
            JObject json = new JObject(
                new JProperty("name", Name),
                new JProperty("category", Category),
                new JProperty("description", Description)
            );
            if (Show != AlwaysOn.Instance) {
                json.Add(new JProperty("show", Show.ToJson()));
            }
            if (Resource != null) {
                json.Add(new JProperty("resource", Resource.ToJson()));
            }
            return json;
        }

        public ControllableFlag(JObject json) {
            Name = (string)json["name"];
            Category = (string)json["category"];
            Description = (string)json["description"];
            if (json["show"] != null) {
                Show = Toggle.FromJson(json["show"]);
            }
            if (json["resource"] != null) {
                Resource = new ActionResource((JObject)json["resource"]);
            }
            On = false;
        }
    }
}
