using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using PlayMore35.Effects;
using PlayMore35.Global;

namespace PlayMore35.CharacterChoices {
    public class Vestige : IJsonable {
        public string Name;
        public int Level;
        public int BindingDc;
        public Effect Influence;
        public List<Effect> Abilities;

        public JToken ToJson() {
            var effects = new JObject();
            if (Abilities != null && Abilities.Count > 0) {
                effects["abilities"] = new JArray(Abilities.Select(x => x.ToJson()));
            }
            if (Influence != null) {
                effects["influence"] = new JArray(Influence.ToJson());
            }
            return new JObject(
                new JProperty("name", Name),
                new JProperty("level", Level),
                new JProperty("binding DC", BindingDc),
                new JProperty("effects", effects)
            );
        }

        public Vestige(JObject json) {
            Name = (string)json["name"];
            Level = (int)json["level"];
            BindingDc = (int)(json["binding DC"] ?? 0);
            if (json["effects"]?["influence"] is JArray kInfArr && kInfArr.Count > 0) {
                Influence = new Effect((JObject)kInfArr[0]);
            }
            if (Influence != null) {
                if (string.IsNullOrEmpty(Influence.Parameter)) {
                    Influence.Parameter = "defied";
                }
                if (!Influence.associatedAction.Any()) {
                    Influence.associatedAction.Add(
                        new SheetAction { Name = $"Defy {Influence.Name}" }
                            .AdjustParam(Influence.ParameterName, 1)
                    );
                }
            }
            if (json["effects"]?["abilities"] is JArray kAbArr) {
                Abilities = kAbArr.Select(x => new Effect((JObject)x)).ToList();
            } else {
                Abilities = new List<Effect>();
            }
        }
    }
}
