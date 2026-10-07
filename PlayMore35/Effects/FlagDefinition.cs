using Newtonsoft.Json.Linq;
using PlayMore35.Global;

namespace PlayMore35.Effects {
    public class FlagDefinition : IJsonable {
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public string Description { get; set; } = "";
        public IToggle Show { get; set; } = AlwaysOn.Instance;
        public ActionResource Resource { get; set; } = null;

        public FlagDefinition() { }

        public FlagDefinition(JObject json) {
            Name = (string)json["name"] ?? "";
            Category = (string)json["category"] ?? "";
            Description = (string)json["description"] ?? "";
            if (json["show"] != null) {
                Show = Toggle.FromJson(json["show"]);
            }
            if (json["resource"] != null) {
                Resource = new ActionResource((JObject)json["resource"]);
            }
        }

        public FlagDefinition(string name, string category = "", string description = "", IToggle show = null, ActionResource resource = null) {
            Name = name ?? "";
            Category = category ?? "";
            Description = description ?? "";
            Show = show ?? AlwaysOn.Instance;
            Resource = resource;
        }

        public virtual JToken ToJson() {
            JObject json = new JObject(
                new JProperty("name", Name),
                new JProperty("category", Category),
                new JProperty("description", Description)
            );
            if (Show != null && Show != AlwaysOn.Instance) {
                json.Add(new JProperty("show", Show.ToJson()));
            }
            if (Resource != null) {
                json.Add(new JProperty("resource", Resource.ToJson()));
            }
            return json;
        }
    }
}
