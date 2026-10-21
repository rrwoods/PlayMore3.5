using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using PlayMore35.Effects;
using PlayMore35.Global;

namespace PlayMore35.Subsystems {
    public class SubsystemElement : IJsonable {
        public string Name { get; set; } = "";
        public int Level { get; set; } = 1;
        public string Type { get; set; } = "";
        public Dictionary<string, List<SubsystemStep>> Verbs { get; } = new Dictionary<string, List<SubsystemStep>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<Effect>> Kinds { get; } = new Dictionary<string, List<Effect>>(StringComparer.OrdinalIgnoreCase);

        public SubsystemElement() { }

        public SubsystemElement(JObject json) {
            if (json == null) return;
            Name = (string)(json["name"] ?? "");
            Level = (int)(json["level"] ?? 1);
            Type = (string)(json["type"] ?? "");

            if (json["verbs"] is JObject verbsObj) {
                foreach (var prop in verbsObj.Properties()) {
                    if (prop.Value is JArray stepsArr) {
                        Verbs[prop.Name] = stepsArr.Select(s => new SubsystemStep((JObject)s)).ToList();
                    }
                }
            }

            if (json["kinds"] is JObject kindsObj) {
                foreach (var prop in kindsObj.Properties()) {
                    if (prop.Value is JArray arr) {
                        Kinds[prop.Name] = arr.Select(x => new Effect((JObject)x)).ToList();
                    }
                }
            }
        }

        public void AddKindEffect(string kind, Effect effect) {
            if (string.IsNullOrEmpty(kind) || effect == null) return;
            if (!Kinds.TryGetValue(kind, out var list)) {
                list = new List<Effect>();
                Kinds[kind] = list;
            }
            list.Add(effect);
        }

        public IReadOnlyList<Effect> GetEffectsByKind(string kind) {
            if (string.IsNullOrEmpty(kind)) return new List<Effect>();
            return Kinds.TryGetValue(kind, out var list) ? list : (IReadOnlyList<Effect>)new List<Effect>();
        }

        public JToken ToJson() {
            var json = new JObject(
                new JProperty("name", Name),
                new JProperty("level", Level),
                new JProperty("type", Type)
            );

            if (Verbs.Count > 0) {
                var verbsObj = new JObject();
                foreach (var kvp in Verbs) {
                    verbsObj[kvp.Key] = new JArray(kvp.Value.Select(x => x.ToJson()));
                }
                json["verbs"] = verbsObj;
            }

            if (Kinds.Count > 0) {
                var kindsObj = new JObject();
                foreach (var kvp in Kinds) {
                    kindsObj[kvp.Key] = new JArray(kvp.Value.Select(x => x.ToJson()));
                }
                json["kinds"] = kindsObj;
            }

            return json;
        }
    }
}
