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

        public override string ToString() => Name;

        public bool MatchesFilter(string? filter, SubsystemDefinition? definition = null) {
            if (string.IsNullOrEmpty(filter)) return true;

            if (definition != null) {
                if (string.Equals(definition.Id, filter, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(definition.Name, filter, StringComparison.OrdinalIgnoreCase)) {
                    return true;
                }
            }

            if (string.Equals(Type, filter, StringComparison.OrdinalIgnoreCase)) return true;

            bool MatchesTaxonomyEntry(TaxonomyEntry entry) {
                if (entry == null) return false;
                if (string.Equals(entry.Name, filter, StringComparison.OrdinalIgnoreCase)) return true;
                foreach (var kvp in entry.Properties) {
                    if (kvp.Value == null) continue;
                    if (kvp.Value is System.Collections.IEnumerable enumerable && !(kvp.Value is string)) {
                        foreach (var item in enumerable) {
                            if (item != null && string.Equals(item.ToString(), filter, StringComparison.OrdinalIgnoreCase)) {
                                return true;
                            }
                        }
                    } else if (string.Equals(kvp.Value.ToString(), filter, StringComparison.OrdinalIgnoreCase)) {
                        return true;
                    }
                }
                return false;
            }

            foreach (var entry in _properties.Values.OfType<TaxonomyEntry>()) {
                if (MatchesTaxonomyEntry(entry)) {
                    return true;
                }
            }

            if (definition?.Taxonomies != null) {
                foreach (var taxKvp in definition.Taxonomies) {
                    if (_properties.TryGetValue(taxKvp.Key, out var propVal) && propVal != null) {
                        var entryName = propVal.ToString();
                        if (!string.IsNullOrEmpty(entryName) && taxKvp.Value.TryGetValue(entryName, out var entryDef)) {
                            if (MatchesTaxonomyEntry(entryDef)) {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        public T GetProperty<T>(string key, T defaultValue = default) {
            if (string.IsNullOrEmpty(key)) return defaultValue;
            if (_properties.TryGetValue(key, out var val) && val != null) {
                if (val is T typedVal) return typedVal;
                if (typeof(T) == typeof(string)) return (T)(object)val.ToString();
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
            if (context != null && (context.Effects == null || context.Effects.stats.ContainsKey(statName))) {
                var stat = context.GetStatistic(statName);
                if (stat != null && (stat.Value != 0 || (stat.Explanation != null && stat.Explanation.Count > 0))) {
                    return stat;
                }
            }
            return new StatView(statName, GetProperty<int>(propertyName));
        }

        [AuthorizedDomainApi("Subsystem element raw statistic resolution")]
        public StatView GetStatistic(string propertyName) =>
            new StatView(GetStatisticName(propertyName), GetProperty<int>(propertyName));

        public Dictionary<string, List<SubsystemStep>> Verbs { get; } = new Dictionary<string, List<SubsystemStep>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<Effect>> Effects { get; } = new Dictionary<string, List<Effect>>(StringComparer.OrdinalIgnoreCase);

        public SubsystemElement() { }

        public SubsystemElement(JObject json) {
            if (json == null) return;
            foreach (var prop in json.Properties()) {
                if (string.Equals(prop.Name, "verbs", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(prop.Name, "effects", StringComparison.OrdinalIgnoreCase)) {
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

            if (json["effects"] is JObject effectsObj) {
                foreach (var prop in effectsObj.Properties()) {
                    if (prop.Value is JArray arr) {
                        Effects[prop.Name] = arr.Select(x => new Effect((JObject)x)).ToList();
                    }
                }
            }
        }

        [AuthorizedDomainApi("Programmatic effect registration on subsystem elements")]
        public void AddEffect(string category, Effect effect) {
            if (string.IsNullOrEmpty(category) || effect == null) return;
            if (!Effects.TryGetValue(category, out var list)) {
                list = new List<Effect>();
                Effects[category] = list;
            }
            list.Add(effect);
        }

        public IReadOnlyList<Effect> GetEffects(string category) {
            if (string.IsNullOrEmpty(category)) return new List<Effect>();
            return Effects.TryGetValue(category, out var list) ? list : (IReadOnlyList<Effect>)new List<Effect>();
        }

        public JToken ToJson() {
            var json = new JObject();
            foreach (var kvp in _properties) {
                if (string.Equals(kvp.Key, "verbs", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kvp.Key, "effects", StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }
                if (kvp.Value != null) {
                    if (kvp.Value is IPropertyProvider) {
                        json[kvp.Key] = kvp.Value.ToString();
                    } else {
                        json[kvp.Key] = JToken.FromObject(kvp.Value);
                    }
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

            if (Effects.Count > 0) {
                var effectsObj = new JObject();
                foreach (var kvp in Effects) {
                    effectsObj[kvp.Key] = new JArray(kvp.Value.Select(x => x.ToJson()));
                }
                json["effects"] = effectsObj;
            }

            return json;
        }
    }
}
