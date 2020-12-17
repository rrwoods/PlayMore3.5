using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json.Linq;
using PlayMore35.Effects;

namespace PlayMore35.CharacterChoices {
    public class Feat : IJsonable {
        public List<string> Descriptors = new List<string>(); // don't bother with [General].  nothing cares.
        public string Prerequisites = ""; // not formalizing this for now
        public string ChoiceDescription = "";
        public Statistic Parameter = null;
        public Effect Benefit; // name of the feat is the name of the effect

        public JToken ToJson() {
            JObject json = new JObject(
                new JProperty("benefit", Benefit)
            );
            if (Descriptors.Any()) {
                json["descriptors"] = new JArray(Descriptors);
            }
            if (Prerequisites.Any()) {
                json["prerequisites"] = Prerequisites;
            }
            if (ChoiceDescription.Any()) {
                json["choice"] = ChoiceDescription;
            }
            if (Parameter != null) {
                json["parameter"] = Parameter.ToJson();
            }
            return json;
        }

        public Feat(JObject json) {
            Benefit = new Effect((JObject)json["benefit"]);
            if (json["descriptors"] != null) {
                Descriptors.AddRange(json["descriptors"].Select(x => (string)x));
            }
            if (json["prerequisites"] != null) {
                Prerequisites = (string)json["prerequisites"];
            }
            if (json["choice"] != null) {
                ChoiceDescription = (string)json["choice"];
            }
            if (json["parameter"] != null) {
                Parameter = new Statistic((JObject)json["parameter"]);
            }
        }
    }
}
