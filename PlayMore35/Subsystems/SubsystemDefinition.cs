using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Subsystems.Maneuvers;

namespace PlayMore35.Subsystems {
    public class SubsystemStep : IJsonable {
        public string Type { get; set; } = "";
        public string SlotGroup { get; set; } = "";
        public string Selector { get; set; } = "invoked";
        public string FromState { get; set; } = null;
        public string ToState { get; set; } = "";
        public string CountExpression { get; set; } = "1";
        public string OnEmptyVerb { get; set; } = "";
        public string Verb { get; set; } = "";
        public SheetAction Action { get; set; } = null;
        public string Pool { get; set; } = "";
        public string Op { get; set; } = "consume";
        public int Amount { get; set; } = 0;

        public SubsystemStep() { }

        public SubsystemStep(JObject json) {
            if (json == null) return;
            Type = (string)(json["type"] ?? "");
            SlotGroup = (string)(json["slot_group"] ?? "");
            Selector = (string)(json["selector"] ?? "invoked");
            FromState = (string)json["from"];
            ToState = (string)(json["to"] ?? "");
            if (json["count"] != null) {
                CountExpression = (string)json["count"];
            }
            OnEmptyVerb = (string)(json["on_empty_verb"] ?? "");
            Verb = (string)(json["verb"] ?? "");
            if (json["action"] is JObject actionObj) {
                Action = SheetAction.FromJson(actionObj);
            }
            Pool = (string)(json["pool"] ?? "");
            Op = (string)(json["op"] ?? "consume");
            Amount = (int)(json["amount"] ?? 0);
        }

        public JToken ToJson() {
            var json = new JObject(new JProperty("type", Type));
            if (!string.IsNullOrEmpty(SlotGroup)) json["slot_group"] = SlotGroup;
            if (!string.IsNullOrEmpty(Selector)) json["selector"] = Selector;
            if (FromState != null) json["from"] = FromState;
            if (!string.IsNullOrEmpty(ToState)) json["to"] = ToState;
            if (!string.IsNullOrEmpty(CountExpression)) json["count"] = CountExpression;
            if (!string.IsNullOrEmpty(OnEmptyVerb)) json["on_empty_verb"] = OnEmptyVerb;
            if (!string.IsNullOrEmpty(Verb)) json["verb"] = Verb;
            if (Action != null) json["action"] = Action.ToJson();
            if (!string.IsNullOrEmpty(Pool)) json["pool"] = Pool;
            if (!string.IsNullOrEmpty(Op)) json["op"] = Op;
            if (Amount != 0) json["amount"] = Amount;
            return json;
        }
    }

    public class MaxLevelDefinition : IJsonable {
        public string Stat { get; set; } = "";
        public int Base { get; set; } = 0;

        public MaxLevelDefinition() { }

        public MaxLevelDefinition(string stat, int baseValue = 0) {
            Stat = stat ?? "";
            Base = baseValue;
        }

        public MaxLevelDefinition(JObject json) {
            if (json == null) return;
            Stat = (string)(json["stat"] ?? "");
            Base = (int)(json["base"] ?? 0);
        }

        public JToken ToJson() {
            var json = new JObject();
            if (!string.IsNullOrEmpty(Stat)) json["stat"] = Stat;
            if (Base != 0) json["base"] = Base;
            return json;
        }
    }

    public class LevelGatingDefinition : IJsonable {
        public string ElementStatistic { get; set; } = "";

        public LevelGatingDefinition() { }

        public LevelGatingDefinition(string elementStatistic) {
            ElementStatistic = elementStatistic ?? "";
        }

        public LevelGatingDefinition(JObject json) {
            if (json == null) return;
            ElementStatistic = (string)(json["element_statistic"] ?? "");
        }

        public JToken ToJson() {
            var json = new JObject();
            if (!string.IsNullOrEmpty(ElementStatistic)) json["element_statistic"] = ElementStatistic;
            return json;
        }
    }

    public class SlotGroupDefinition : IJsonable {
        public string Name { get; set; } = "";
        public string CapacityStat { get; set; } = null;
        public string ReadiedCapacityStat { get; set; } = null;
        public string MaxActiveStat { get; set; } = null;
        public int FixedCapacity { get; set; } = 0;
        public MaxLevelDefinition MaxLevel { get; set; } = null;
        public List<Effect> Effects { get; } = new List<Effect>();
        private readonly List<JObject> _rawEffectJson = new List<JObject>();
        private readonly Dictionary<string, List<Effect>> _effectsBySourceCache = new Dictionary<string, List<Effect>>(StringComparer.OrdinalIgnoreCase);
        public List<string> States { get; } = new List<string>();
        public string DefaultState { get; set; } = "";

        public SlotGroupDefinition() { }

        public SlotGroupDefinition(JObject json) {
            if (json == null) return;
            Name = (string)(json["name"] ?? "");
            CapacityStat = (string)json["capacity_stat"];
            ReadiedCapacityStat = (string)json["readied_capacity_stat"];
            MaxActiveStat = (string)json["max_active_stat"];
            FixedCapacity = (int)(json["fixed_capacity"] ?? 0);
            if (json["max_level"] is JObject mlObj) {
                MaxLevel = new MaxLevelDefinition(mlObj);
            }
            if (json["effects"] is JArray effArr) {
                foreach (var x in effArr.OfType<JObject>()) {
                    _rawEffectJson.Add((JObject)x.DeepClone());
                    Effects.Add(new Effect(x));
                }
            }
            if (json["states"] is JArray arr) {
                States.AddRange(arr.Select(x => (string)x));
            }
            DefaultState = (string)(json["default_state"] ?? (States.Count > 0 ? States[0] : ""));
        }

        public List<Effect> GetEffectsForSource(string source) {
            if (string.IsNullOrEmpty(source)) return new List<Effect>(Effects);
            var norm = source.Trim().ToLowerInvariant();
            if (_effectsBySourceCache.TryGetValue(norm, out var cached) && cached.Count == Effects.Count) {
                return cached;
            }
            List<Effect> resolved;
            if (_rawEffectJson.Count > 0) {
                resolved = _rawEffectJson
                    .Select(j => new Effect(JObject.Parse(j.ToString().Replace("{source}", norm))))
                    .ToList();
            } else {
                resolved = Effects
                    .Select(e => new Effect(JObject.Parse(e.ToJson().ToString().Replace("{source}", norm))))
                    .ToList();
            }
            _effectsBySourceCache[norm] = resolved;
            return resolved;
        }

        public JToken ToJson() {
            var json = new JObject();
            if (!string.IsNullOrEmpty(Name)) json["name"] = Name;
            if (!string.IsNullOrEmpty(CapacityStat)) json["capacity_stat"] = CapacityStat;
            if (!string.IsNullOrEmpty(ReadiedCapacityStat)) json["readied_capacity_stat"] = ReadiedCapacityStat;
            if (!string.IsNullOrEmpty(MaxActiveStat)) json["max_active_stat"] = MaxActiveStat;
            if (FixedCapacity > 0) json["fixed_capacity"] = FixedCapacity;
            if (MaxLevel != null) json["max_level"] = MaxLevel.ToJson();
            if (Effects.Count > 0) json["effects"] = new JArray(Effects.Select(x => x.ToJson()));
            if (States.Count > 0) json["states"] = new JArray(States);
            if (!string.IsNullOrEmpty(DefaultState)) json["default_state"] = DefaultState;
            return json;
        }
    }

    public class PoolDefinition : IJsonable {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string CapacityStat { get; set; } = null;
        public int DefaultCapacity { get; set; } = 0;

        public PoolDefinition() { }

        public PoolDefinition(JObject json) {
            if (json == null) return;
            Id = (string)(json["id"] ?? "");
            Name = (string)(json["name"] ?? Id);
            CapacityStat = (string)json["capacity_stat"];
            DefaultCapacity = (int)(json["default_capacity"] ?? 0);
        }

        public JToken ToJson() {
            var json = new JObject(
                new JProperty("id", Id),
                new JProperty("name", Name)
            );
            if (!string.IsNullOrEmpty(CapacityStat)) json["capacity_stat"] = CapacityStat;
            if (DefaultCapacity > 0) json["default_capacity"] = DefaultCapacity;
            return json;
        }
    }

    public class EffectProjectionRule : IJsonable {
        public string SlotGroup { get; set; } = "";
        public string State { get; set; } = "";
        public List<string> Effects { get; } = new List<string>();

        public EffectProjectionRule() { }

        public EffectProjectionRule(string slotGroup, string state, params string[] effects) {
            SlotGroup = slotGroup ?? "";
            State = state ?? "";
            if (effects != null) {
                Effects.AddRange(effects);
            }
        }

        public EffectProjectionRule(JObject json) {
            if (json == null) return;
            SlotGroup = (string)(json["slot_group"] ?? "");
            State = (string)(json["state"] ?? "");
            if (json["effects"] is JArray arr) {
                Effects.AddRange(arr.Select(x => (string)x));
            }
        }

        public JToken ToJson() {
            return new JObject(
                new JProperty("slot_group", SlotGroup),
                new JProperty("state", State),
                new JProperty("effects", new JArray(Effects))
            );
        }
    }

    public class SubsystemDefinition : IJsonable {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public Dictionary<string, SlotGroupDefinition> SlotGroups { get; } = new Dictionary<string, SlotGroupDefinition>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<SubsystemStep>> Verbs { get; } = new Dictionary<string, List<SubsystemStep>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<SubsystemStep>> DefaultElementVerbs { get; } = new Dictionary<string, List<SubsystemStep>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, PoolDefinition> Pools { get; } = new Dictionary<string, PoolDefinition>(StringComparer.OrdinalIgnoreCase);
        public List<EffectProjectionRule> Projections { get; } = new List<EffectProjectionRule>();
        public List<string> RequiredElementProperties { get; } = new List<string>();
        public Dictionary<string, List<string>> ElementStatistics { get; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        public LevelGatingDefinition LevelGating { get; set; } = null;
        public Dictionary<string, MartialDiscipline> Disciplines { get; } = new Dictionary<string, MartialDiscipline>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, SubsystemElement> Elements { get; } = new Dictionary<string, SubsystemElement>(StringComparer.OrdinalIgnoreCase);
        public List<SheetAction> Actions { get; } = new List<SheetAction>();

        public SubsystemDefinition() { }

        public SubsystemDefinition(JObject json) {
            if (json == null) return;
            var subJson = json["subsystem"] as JObject;
            if (subJson == null) return;
            Id = (string)(subJson["id"] ?? "");
            Name = (string)(subJson["name"] ?? Id);

            if (subJson["level_gating"] is JObject lgObj) {
                LevelGating = new LevelGatingDefinition(lgObj);
            }

            if (subJson["disciplines"] is JObject discObj) {
                foreach (var prop in discObj.Properties()) {
                    if (prop.Value is JObject dObj) {
                        Disciplines[prop.Name] = new MartialDiscipline(prop.Name, dObj);
                    }
                }
            }

            if (subJson["slot_groups"] is JObject slotsObj) {
                foreach (var prop in slotsObj.Properties()) {
                    if (prop.Value is JObject groupObj) {
                        var def = new SlotGroupDefinition(groupObj);
                        if (string.IsNullOrEmpty(def.Name)) def.Name = prop.Name;
                        SlotGroups[prop.Name] = def;
                    }
                }
            }

            if (subJson["verbs"] is JObject verbsObj) {
                foreach (var prop in verbsObj.Properties()) {
                    if (prop.Value is JArray stepsArr) {
                        Verbs[prop.Name] = stepsArr.Select(x => new SubsystemStep((JObject)x)).ToList();
                    }
                }
            }

            if (subJson["default_element_verbs"] is JObject defVerbsObj) {
                foreach (var prop in defVerbsObj.Properties()) {
                    if (prop.Value is JArray stepsArr) {
                        DefaultElementVerbs[prop.Name] = stepsArr.Select(x => new SubsystemStep((JObject)x)).ToList();
                    }
                }
            }

            if (subJson["pools"] is JObject poolsObj) {
                foreach (var prop in poolsObj.Properties()) {
                    if (prop.Value is JObject poolObj) {
                        var poolDef = new PoolDefinition(poolObj);
                        if (string.IsNullOrEmpty(poolDef.Id)) poolDef.Id = prop.Name;
                        Pools[prop.Name] = poolDef;
                    }
                }
            }

            if (subJson["projections"] is JArray projArr) {
                Projections.AddRange(projArr.Select(x => new EffectProjectionRule((JObject)x)));
            }

            if (subJson["required_element_properties"] is JArray reqPropsArr) {
                RequiredElementProperties.AddRange(reqPropsArr.Select(x => x.ToString()));
            }

            if (subJson["element_statistics"] is JObject statObj) {
                foreach (var prop in statObj.Properties()) {
                    var tags = new List<string>();
                    if (prop.Value is JArray tagsArr) {
                        tags.AddRange(tagsArr.Select(x => (string)x));
                    }
                    ElementStatistics[prop.Name] = tags;
                }
            }

            if (LevelGating != null) {
                if (string.IsNullOrEmpty(LevelGating.ElementStatistic) || !ElementStatistics.ContainsKey(LevelGating.ElementStatistic)) {
                    throw new InvalidOperationException($"Subsystem '{Id}' level gating references element statistic '{LevelGating.ElementStatistic}' which is not defined in element_statistics.");
                }
            }

            if (subJson["actions"] is JArray actionsArr) {
                Actions.AddRange(actionsArr.Select(x => SheetAction.FromJson((JObject)x)));
            }

            if (json["elements"] is JArray elemArr) {
                foreach (var elemToken in elemArr) {
                    if (elemToken is JObject elemObj) {
                        var elem = new SubsystemElement(elemObj);
                        if (!string.IsNullOrEmpty(elem.Name)) {
                            foreach (var reqProp in RequiredElementProperties) {
                                if (!elem.Properties.ContainsKey(reqProp) || elem.Properties[reqProp] == null) {
                                    throw new InvalidOperationException($"Subsystem '{Id}' element '{elem.Name}' is missing required property '{reqProp}'.");
                                }
                            }
                            foreach (var statProp in ElementStatistics.Keys) {
                                if (!elem.Properties.TryGetValue(statProp, out var val) || val == null) {
                                    throw new InvalidOperationException($"Subsystem '{Id}' element '{elem.Name}' is missing required element statistic '{statProp}'.");
                                }
                                try {
                                    Convert.ToInt32(val);
                                } catch {
                                    throw new InvalidOperationException($"Subsystem '{Id}' element '{elem.Name}' statistic '{statProp}' must be numeric, but was '{val}'.");
                                }
                            }
                            if (Disciplines.Count > 0 && elem.Properties.TryGetValue("discipline", out var discVal) && discVal != null) {
                                var discName = discVal.ToString();
                                if (Disciplines.TryGetValue(discName, out var discDef)) {
                                    elem.SetProperty("discipline", discDef);
                                }
                            }
                            Elements[elem.Name] = elem;
                        }
                    }
                }
            }
        }

        public JToken ToJson() {
            var subJson = new JObject(
                new JProperty("id", Id),
                new JProperty("name", Name)
            );

            if (Disciplines.Count > 0) {
                var discObj = new JObject();
                foreach (var kvp in Disciplines) discObj[kvp.Key] = kvp.Value.ToJson();
                subJson["disciplines"] = discObj;
            }

            if (SlotGroups.Count > 0) {
                var slotsObj = new JObject();
                foreach (var kvp in SlotGroups) slotsObj[kvp.Key] = kvp.Value.ToJson();
                subJson["slot_groups"] = slotsObj;
            }

            if (Verbs.Count > 0) {
                var verbsObj = new JObject();
                foreach (var kvp in Verbs) verbsObj[kvp.Key] = new JArray(kvp.Value.Select(x => x.ToJson()));
                subJson["verbs"] = verbsObj;
            }

            if (DefaultElementVerbs.Count > 0) {
                var defVerbsObj = new JObject();
                foreach (var kvp in DefaultElementVerbs) defVerbsObj[kvp.Key] = new JArray(kvp.Value.Select(x => x.ToJson()));
                subJson["default_element_verbs"] = defVerbsObj;
            }

            if (Pools.Count > 0) {
                var poolsObj = new JObject();
                foreach (var kvp in Pools) poolsObj[kvp.Key] = kvp.Value.ToJson();
                subJson["pools"] = poolsObj;
            }

            if (Projections.Count > 0) {
                subJson["projections"] = new JArray(Projections.Select(x => x.ToJson()));
            }

            if (RequiredElementProperties.Count > 0) {
                subJson["required_element_properties"] = new JArray(RequiredElementProperties);
            }

            if (LevelGating != null) {
                subJson["level_gating"] = LevelGating.ToJson();
            }

            if (ElementStatistics.Count > 0) {
                var statObj = new JObject();
                foreach (var kvp in ElementStatistics) {
                    statObj[kvp.Key] = new JArray(kvp.Value);
                }
                subJson["element_statistics"] = statObj;
            }

            if (Actions.Count > 0) {
                subJson["actions"] = new JArray(Actions.Select(x => x.ToJson()));
            }

            var root = new JObject(new JProperty("subsystem", subJson));
            if (Elements.Count > 0) {
                root["elements"] = new JArray(Elements.Values.Select(x => x.ToJson()));
            }
            return root;
        }
    }
}
