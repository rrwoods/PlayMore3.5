using Newtonsoft.Json.Linq;
using PlayMore35.Global;

namespace PlayMore35.Effects {
    public class ParameterDefinition : IJsonable {
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public string Description { get; set; } = "";

        public ParameterDefinition() { }

        public ParameterDefinition(JObject json) {
            Name = (string?)json["name"] ?? "";
            Category = (string?)json["category"] ?? "";
            Description = (string?)json["description"] ?? "";
        }

        public ParameterDefinition(string name, string category = "", string description = "") {
            Name = name ?? "";
            Category = category ?? "";
            Description = description ?? "";
        }

        public virtual JToken ToJson() {
            return new JObject(
                new JProperty("name", Name),
                new JProperty("category", Category),
                new JProperty("description", Description)
            );
        }
    }
}
