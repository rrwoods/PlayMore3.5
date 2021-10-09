using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json.Linq;
using PlayMore35.Effects;

namespace PlayMore35.CharacterChoices {
    public class Feat : IJsonable {
        public List<string> Descriptors = new List<string>(); // don't bother with [General].  nothing cares.
        public string Prerequisites = ""; // not formalizing this for now
        public string ChoiceDescription = "";
        public List<ControllableFlag> Flags = new List<ControllableFlag>();
        public List<ControllableParameter> Parameters = new List<ControllableParameter>();
        public Effect Benefit; // name of the feat is the name of the effect

        public List<Statistic> NewStats = new List<Statistic>();

        public JToken ToJson() {
            JObject json = new JObject(
                new JProperty("benefit", Benefit)
            );
            json.AddList("descriptors", Descriptors);
            if (Prerequisites.Any()) {
                json["prerequisites"] = Prerequisites;
            }
            if (ChoiceDescription.Any()) {
                json["choice"] = ChoiceDescription;
            }
            json.AddList("flags", Flags);
            json.AddList("parameters", Parameters);
            json.AddList("stats", NewStats);
            return json;
        }

        public Feat(JObject json, Character character) {
            Benefit = new Effect((JObject)json["benefit"]);
            Descriptors.ReadJson("descriptors", json);
            if (json["prerequisites"] != null) {
                Prerequisites = (string)json["prerequisites"];
            }
            if (json["choice"] != null) {
                ChoiceDescription = (string)json["choice"];
            }
            if (json["flags"] != null) {
                Flags.AddRange(json["flags"].Select(x => new ControllableFlag((JObject)x, character)));
            }
            if (json["parameters"] != null) {
                Parameters.AddRange(json["parameters"].Select(x => new ControllableParameter((JObject)x, character)));
            }
            if (json["stats"] != null) {
                NewStats.AddRange(json["stats"].Select(x => new Statistic((JObject)x)));
            }
        }
    }
}
