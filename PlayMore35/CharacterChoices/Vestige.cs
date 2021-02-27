using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using PlayMore35.Effects;

namespace PlayMore35.CharacterChoices {
    public class Vestige : IJsonable {
        public string Name;
        public int Level;
        public int BindingDc;
        public Effect Influence;
        public List<Effect> Abilities;

        public JToken ToJson() {
            return new JObject(
                new JProperty("name", Name),
                new JProperty("level", Level),
                new JProperty("binding DC", BindingDc),
                new JProperty("influence", Influence.ToJson()),
                new JProperty("abilities", new JArray(Abilities.Select(x => x.ToJson())))
            );
        }

        public Vestige(JObject json) {
            Name = (string)json["name"];
            Level = (int)json["level"];
            BindingDc = (int)json["binding DC"];
            Influence = new Effect((JObject)json["influence"]);
            Abilities = ((JArray)json["abilities"]).Select(x => new Effect((JObject)x)).ToList();
        }
    }
}
