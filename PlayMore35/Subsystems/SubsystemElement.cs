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

        public string Type {
            get => _properties.TryGetValue("type", out var v) ? v?.ToString() ?? "" : "";
            set => _properties["type"] = value;
        }

        public T GetProperty<T>(string key, T defaultValue = default) {
            if (string.IsNullOrEmpty(key)) return defaultValue;
            if (_properties.TryGetValue(key, out var val) && val != null) {
                if (val is T typedVal) return typedVal;
                try {
                    var targetType = typeof(T);
                    var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
                    return (T)Convert.ChangeType(val, underlyingType);
                } catch {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        public void SetProperty(string key, object value) {
            if (string.IsNullOrEmpty(key)) return;
            _properties[key] = value;
        }

        public string GetStatisticName(string propertyName) => $"{propertyName}:{Name}";

        public StatView GetStatistic(string propertyName, ICharacterContext context) {
            var statName = GetStatisticName(propertyName);
            if (context != null) {
                var stat = context.GetStatistic(statName);
                if (stat != null && (stat.Value != 0 || (stat.Explanation != null && stat.Explanation.Count > 0))) {
                    return stat;
                }
            }
            return new StatView(statName, GetProperty<int>(propertyName));
        }

        public StatView GetStatistic(string propertyName, ICharacterSession session) {
            var statName = GetStatisticName(propertyName);
            if (session != null) {
                var stat = session.GetStatistic(statName);
                if (stat != null && (stat.Value != 0 || (stat.Explanation != null && stat.Explanation.Count > 0))) {
                    return stat;
                }
            }
            return new StatView(statName, GetProperty<int>(propertyName));
        }

        public StatView GetStatistic(string propertyName, CharacterSession session) =>
            GetStatistic(propertyName, (ICharacterSession)session);

        public StatView GetStatistic(string propertyName) =>
            new StatView(GetStatisticName(propertyName), GetProperty<int>(propertyName));

        public Dictionary<string, List<SubsystemStep>> Verbs { get; } = new Dictionary<string, List<SubsystemStep>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<Effect>> Kinds { get; } = new Dictionary<string, List<Effect>>(StringComparer.OrdinalIgnoreCase);

        public SubsystemElement() { }

        public SubsystemElement(JObject json) {
            if (json == null) return;
            foreach (var prop in json.Properties()) {
                if (string.Equals(prop.Name, "verbs", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(prop.Name, "kinds", StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }

                if (prop.Value is JValue jv) {
                    _properties[prop.Name] = jv.Value;
                } else if (prop.Value is JArray ja) {
                    _properties[prop.Name] = ja.Select(t => t is JValue v ? v.Value : (object)t).ToList();
                } else if (prop.Value is JObject jo) {
                    _properties[prop.Name] = jo;
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
                if (string.Equals(kvp.Key, "verbs", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kvp.Key, "kinds", StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }
                if (kvp.Value != null) {
                    json[kvp.Key] = JToken.FromObject(kvp.Value);
                }
            }
            if (!json.ContainsKey("name")) json["name"] = Name;
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
