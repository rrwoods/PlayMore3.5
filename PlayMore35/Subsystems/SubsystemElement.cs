using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Rules;

namespace PlayMore35.Subsystems {
    public class SubsystemElement : IJsonable, IPropertyProvider {
        private readonly Dictionary<string, object> _properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<string, object> Properties => _properties;

        public string Name {
            get => _properties.TryGetValue("name", out var v) ? v?.ToString() ?? "" : "";
            set => _properties["name"] = value;
        }

        public int Level {
            get => _properties.TryGetValue("level", out var v) ? Convert.ToInt32(v) : 1;
            set => _properties["level"] = value;
        }

        public string Type {
            get => _properties.TryGetValue("type", out var v) ? v?.ToString() ?? "" : "";
            set => _properties["type"] = value;
        }

        public int BindingDc {
            get => _properties.TryGetValue("binding DC", out var v) ? Convert.ToInt32(v) : 0;
            set => _properties["binding DC"] = value;
        }

        public Dictionary<string, List<SubsystemStep>> Verbs { get; } = new Dictionary<string, List<SubsystemStep>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<Effect>> Kinds { get; } = new Dictionary<string, List<Effect>>(StringComparer.OrdinalIgnoreCase);

        public SubsystemElement() { }

        public SubsystemElement(JObject json) {
            if (json == null) return;
            foreach (var prop in json.Properties()) {
                if (prop.Value is JValue jv) {
                    _properties[prop.Name] = jv.Value;
                }
            }

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
            var json = new JObject();
            foreach (var kvp in _properties) {
                if (kvp.Value != null) {
                    json[kvp.Key] = JToken.FromObject(kvp.Value);
                }
            }
            if (!json.ContainsKey("name")) json["name"] = Name;
            if (!json.ContainsKey("level")) json["level"] = Level;
            if (!string.IsNullOrEmpty(Type) && !json.ContainsKey("type")) json["type"] = Type;

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
