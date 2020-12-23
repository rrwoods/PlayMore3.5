using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json.Linq;
using PlayMore35.Effects;

namespace PlayMore35.CharacterChoices {
    public class Feat : IJsonable {
        public List<string> Descriptors = new List<string>(); // don't bother with [General].  nothing cares.
        public string Prerequisites = ""; // not formalizing this for now
        public string ChoiceDescription = "";
        public List<string> Parameters = new List<string>();
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
            json.AddList("parameters", Parameters);
            json.AddList("stats", NewStats);
            return json;
        }

        public Feat(JObject json) {
            Benefit = new Effect((JObject)json["benefit"]);
            Descriptors.ReadJson("descriptors", json);
            if (json["prerequisites"] != null) {
                Prerequisites = (string)json["prerequisites"];
            }
            if (json["choice"] != null) {
                ChoiceDescription = (string)json["choice"];
            }
            Parameters.ReadJson("parameters", json);
            if (json["stats"] != null) {
                NewStats.AddRange(json["stats"].Select(x => new Statistic((JObject)x)));
            }
        }
    }
}
