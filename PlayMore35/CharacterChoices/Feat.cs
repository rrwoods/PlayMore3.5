using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PlayMore35.Effects;

namespace PlayMore35.CharacterChoices {
    public class ChoiceDescriptor : IJsonable {
        public string Id { get; }
        public string Label { get; }
        public string Domain { get; }
        public string Filter { get; }
        public string Fallback { get; }
        public string DependsOn { get; }

        [JsonConstructor]
        public ChoiceDescriptor(string id, string label, string domain, string filter = null, string fallback = null, string dependsOn = null) {
            Id = id ?? "";
            Label = label ?? "";
            Domain = domain ?? "";
            Filter = filter ?? "";
            Fallback = fallback ?? "";
            DependsOn = dependsOn ?? "";
        }

        public ChoiceDescriptor(JObject json)
            : this((string)json?["id"], (string)json?["label"], (string)json?["domain"], (string)json?["filter"], (string)json?["fallback"], (string)json?["dependsOn"]) {
        }

        public JToken ToJson() {
            var json = new JObject(
                new JProperty("id", Id),
                new JProperty("label", Label),
                new JProperty("domain", Domain)
            );
            if (!string.IsNullOrEmpty(Filter)) {
                json.Add(new JProperty("filter", Filter));
            }
            if (!string.IsNullOrEmpty(Fallback)) {
                json.Add(new JProperty("fallback", Fallback));
            }
            if (!string.IsNullOrEmpty(DependsOn)) {
                json.Add(new JProperty("dependsOn", DependsOn));
            }
            return json;
        }
    }

    public class Feat : IJsonable {
        public List<string> Descriptors = new List<string>(); // don't bother with [General].  nothing cares.
        public string Prerequisites = ""; // not formalizing this for now
        public string ChoiceDescription = "";
        public List<ChoiceDescriptor> Choices = new List<ChoiceDescriptor>();
        public List<string> ClassSkills = new List<string>();
        public List<FlagDefinition> Flags = new List<FlagDefinition>();
        public List<ParameterDefinition> Parameters = new List<ParameterDefinition>();
        public Effect Benefit; // name of the feat is the name of the effect

        public List<Statistic> NewStats = new List<Statistic>();

        public JToken ToJson() {
            JObject json = new JObject(
                new JProperty("benefit", Benefit?.ToJson() ?? new JObject())
            );
            json.AddList("descriptors", Descriptors);
            if (Prerequisites.Any()) {
                json["prerequisites"] = Prerequisites;
            }
            if (ChoiceDescription.Any()) {
                json["choice"] = ChoiceDescription;
            }
            json.AddList("choices", Choices);
            json.AddList("class skills", ClassSkills);
            json.AddList("flags", Flags);
            json.AddList("parameters", Parameters);
            json.AddList("stats", NewStats);
            return json;
        }

        public Feat(JObject json) {
            Benefit = new Effect((JObject)json["benefit"]);
            Descriptors.ReadJson("descriptors", json);
            ClassSkills.ReadJson("class skills", json);
            if (json["prerequisites"] != null) {
                Prerequisites = (string)json["prerequisites"];
            }
            if (json["choice"] != null) {
                ChoiceDescription = (string)json["choice"];
            }
            if (json["choices"] != null) {
                Choices.AddRange(json["choices"].Select(x => new ChoiceDescriptor((JObject)x)));
            }
            if (json["flags"] != null) {
                Flags.AddRange(json["flags"].Select(x => new FlagDefinition((JObject)x)));
            }
            if (json["parameters"] != null) {
                Parameters.AddRange(json["parameters"].Select(x => new ParameterDefinition((JObject)x)));
            }
            if (json["stats"] != null) {
                NewStats.AddRange(json["stats"].Select(x => new Statistic((JObject)x)));
            }
        }
    }
}
