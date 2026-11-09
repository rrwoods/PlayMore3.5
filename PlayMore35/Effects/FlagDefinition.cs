using Newtonsoft.Json.Linq;
using PlayMore35.Global;

namespace PlayMore35.Effects {
    public class FlagDefinition : IJsonable {
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public string Description { get; set; } = "";
        public IToggle Show { get; set; } = AlwaysOn.Instance;
        public ActionResource? Resource { get; set; } = null;

        public FlagDefinition(
            string name = "",
            string category = "",
            string description = "",
            IToggle? show = null,
            ActionResource? resource = null) {
            Name = name ?? "";
            Category = category ?? "";
            Description = description ?? "";
            Show = show ?? AlwaysOn.Instance;
            Resource = resource;
        }

        public FlagDefinition(JObject json)
            : this(
                (string?)json?["name"] ?? "",
                (string?)json?["category"] ?? "",
                (string?)json?["description"] ?? "",
                json?["show"] != null ? Toggle.FromJson(json["show"]!) : AlwaysOn.Instance,
                json?["resource"] != null ? new ActionResource((JObject)json["resource"]!) : null) {
        }

        public virtual JToken ToJson() {
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
    }
}
