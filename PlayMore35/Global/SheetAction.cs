using Newtonsoft.Json.Linq;
using PlayMore35.Effects;
using PlayMore35.Subsystems;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlayMore35.Global {
    public class SheetAction : IJsonable {
        public static SheetAction FromJson(JObject json) {
            return json == null ? null : new SheetAction(json);
        }

        public string Name { get; set; } = null;
        public string Subsystem { get; set; } = null;
        public string Verb { get; set; } = null;
        public string SlotGroup { get; set; } = null;
        public int? SlotIndex { get; set; } = null;
        public string Element { get; set; } = null;
        public string Source { get; set; } = null;
        public SubsystemTransition Transition { get; set; } = null;
        public SubsystemPoolOperation PoolOperation { get; set; } = null;
        string duration = null; // null for instantaneous, effect will not be reversed
        private readonly List<string> flagsToSet = new List<string>();
        private readonly List<string> flagsToClear = new List<string>();
        private readonly Dictionary<string, int> paramsToAdjust = new Dictionary<string, int>();
        List<SheetAction> actionsAfter = new List<SheetAction>(); // actions to fire at the end of the duration

        public JToken ToJson() {
            JObject json = new JObject();
            if (!string.IsNullOrEmpty(Name)) {
                json["name"] = Name;
            }
            if (duration != null) {
                json["duration"] = duration;
            }
            json.AddList("set", flagsToSet);
            json.AddList("clear", flagsToClear);
            if (paramsToAdjust.Any()) {
                json["adjust"] = new JObject(paramsToAdjust.Select(x => new JProperty(x.Key, x.Value)));
            }
            json.AddList("after", actionsAfter);

            if (!string.IsNullOrEmpty(Subsystem)) {
                json["subsystem"] = Subsystem;
                if (!string.IsNullOrEmpty(Verb)) json["verb"] = Verb;
                if (!string.IsNullOrEmpty(SlotGroup)) json["slot_group"] = SlotGroup;
                if (SlotIndex.HasValue) json["slot_index"] = SlotIndex.Value;
                if (!string.IsNullOrEmpty(Element)) json["element"] = Element;
                if (!string.IsNullOrEmpty(Source)) json["source"] = Source;
                if (Transition != null) json["transition"] = Transition.ToJson();
                if (PoolOperation != null) json["pool_operation"] = PoolOperation.ToJson();
            }

            return json;
        }

        public SheetAction(JObject json) {
            if (json == null) return;
            if (json["name"] != null) {
                Name = (string)json["name"];
            }
            if (json["duration"] != null) {
                duration = (string)json["duration"];
            }
            flagsToSet.ReadJson("set", json);
            flagsToClear.ReadJson("clear", json);
            if (json["adjust"] != null) {
                paramsToAdjust = ((JObject)json["adjust"]).Properties()
                    .ToDictionary(x => x.Name, x => int.Parse((string)x.Value));
            }
            if (json["after"] != null) {
                actionsAfter.AddRange(json["after"].Select(x => SheetAction.FromJson((JObject)x)));
            }

            if (json["subsystem"] != null) {
                Subsystem = (string)json["subsystem"];
                if (json["verb"] != null) Verb = (string)json["verb"];
                if (json["slot_group"] != null) SlotGroup = (string)json["slot_group"];
                if (json["slot_index"] != null) SlotIndex = (int)json["slot_index"];
                if (json["element"] != null) Element = (string)json["element"];
                if (json["source"] != null) Source = (string)json["source"];
                if (json["transition"] is JObject transObj) Transition = new SubsystemTransition(transObj);
                if (json["pool_operation"] is JObject poolObj) PoolOperation = new SubsystemPoolOperation(poolObj);
            }
        }

        public SheetAction() { }

        private SheetAction Revert() {
            SheetAction revert = new SheetAction {
                Name = Name
            };
            revert.flagsToSet.AddRange(flagsToClear);
            revert.flagsToClear.AddRange(flagsToSet);
            foreach (var adjustment in paramsToAdjust) {
                revert.paramsToAdjust[adjustment.Key] = -adjustment.Value;
            }
            revert.actionsAfter.AddRange(actionsAfter);

            if (!string.IsNullOrEmpty(Subsystem)) {
                revert.Subsystem = Subsystem;
                revert.SlotGroup = SlotGroup;
                revert.SlotIndex = SlotIndex;
                revert.Element = Element;
                revert.Source = Source;

                if (Transition != null) {
                    revert.Transition = new SubsystemTransition {
                        SlotGroup = Transition.SlotGroup,
                        Selector = Transition.Selector,
                        FromState = Transition.ToState,
                        ToState = Transition.FromState,
                        SlotIndex = Transition.SlotIndex ?? SlotIndex,
                        SelectedIndices = Transition.SelectedIndices != null ? new List<int>(Transition.SelectedIndices) : null
                    };
                }

                if (PoolOperation != null) {
                    string invOp = PoolOperation.Op == "consume" ? "recharge"
                                 : PoolOperation.Op == "recharge" ? "consume"
                                 : PoolOperation.Op;
                    revert.PoolOperation = new SubsystemPoolOperation(PoolOperation.Pool, invOp, PoolOperation.Amount);
                }
            }

            return revert;
        }

        public SheetAction SetFlag(string flag) {
            flagsToSet.Add(flag);
            return this;
        }

        public SheetAction ClearFlag(string flag) {
            flagsToClear.Add(flag);
            return this;
        }

        public SheetAction AdjustParam(string param, int adjustment) {
            paramsToAdjust[param] = adjustment;
            return this;
        }

        public SheetAction Duration(string duration) {
            this.duration = duration;
            return this;
        }

        public SheetAction AndThen(SheetAction after) {
            actionsAfter.Add(after);
            return this;
        }

        public TimelineRefreshRequirement Perform(ICharacterContext context) {
            TimelineRefreshRequirement requirement = TimelineRefreshRequirement.None;

            if (duration == "encounter" && context != null && !context.Timeline.Combat) {
                foreach (var after in actionsAfter) {
                    var req = after.Perform(context);
                    if (req > requirement) requirement = req;
                }
                return requirement;
            }

            var effects = context?.Effects;
            if (effects != null) {
                flagsToSet.ForEach(x => {
                    if (effects.controllableFlags.ContainsKey(x)) {
                        effects.controllableFlags[x].On = true;
                        if (requirement < TimelineRefreshRequirement.RecomputeDerived)
                            requirement = TimelineRefreshRequirement.RecomputeDerived;
                    }
                });
                flagsToClear.ForEach(x => {
                    if (effects.controllableFlags.ContainsKey(x)) {
                        effects.controllableFlags[x].On = false;
                        if (requirement < TimelineRefreshRequirement.RecomputeDerived)
                            requirement = TimelineRefreshRequirement.RecomputeDerived;
                    }
                });
                foreach (var item in paramsToAdjust) {
                    if (effects.controllableParameters.ContainsKey(item.Key)) {
                        effects.controllableParameters[item.Key].Value += item.Value;
                        if (requirement < TimelineRefreshRequirement.RecomputeDerived)
                            requirement = TimelineRefreshRequirement.RecomputeDerived;
                    } else if (context?.Character != null) {
                        var cp = new ControllableParameter(item.Key, context.Character);
                        cp.Value += item.Value;
                        effects.controllableParameters[item.Key] = cp;
                        if (requirement < TimelineRefreshRequirement.RecomputeDerived)
                            requirement = TimelineRefreshRequirement.RecomputeDerived;
                    }
                }
            }

            if (!string.IsNullOrEmpty(Subsystem) && context?.Subsystems != null) {
                var engine = context.Subsystems.Get(Subsystem);
                if (engine != null) {
                    var target = new SubsystemTarget(SlotGroup, SlotIndex, Element, Source);
                    if (!string.IsNullOrEmpty(Verb)) {
                        var req = engine.ExecuteVerb(Verb, context, target);
                        if (req > requirement) requirement = req;
                    } else if (Transition != null) {
                        var req = engine.ExecuteTransition(Transition, context, target);
                        if (req > requirement) requirement = req;
                    } else if (PoolOperation != null) {
                        var req = engine.ExecutePoolOperation(PoolOperation, context);
                        if (req > requirement) requirement = req;
                    }
                }
                if (requirement < TimelineRefreshRequirement.RecomputeDerived) {
                    requirement = TimelineRefreshRequirement.RecomputeDerived;
                }
            }

            if (duration == null) {
                foreach (var after in actionsAfter) {
                    var req = after.Perform(context);
                    if (req > requirement) requirement = req;
                }
                if (requirement == TimelineRefreshRequirement.None && !string.IsNullOrEmpty(Name)) {
                    requirement = TimelineRefreshRequirement.FullReload;
                }
            } else if (context != null) {
                context.Timeline.Schedule(Revert(), duration, context);
                if (requirement == TimelineRefreshRequirement.None && !string.IsNullOrEmpty(Name)) {
                    requirement = TimelineRefreshRequirement.FullReload;
                }
            }

            return requirement;
        }

        public bool Matches(SheetAction action) {
            if (action == null) return false;
            bool baseMatches = Name == action.Name &&
                   duration == action.duration &&
                   flagsToSet.SequenceEqual(action.flagsToSet) &&
                   flagsToClear.SequenceEqual(action.flagsToClear) &&
                   paramsToAdjust.Count == action.paramsToAdjust.Count && !paramsToAdjust.Except(action.paramsToAdjust).Any() &&
                   actionsAfter.Count == action.actionsAfter.Count && (actionsAfter.Zip(action.actionsAfter, (x, y) => x.Matches(y)).All(x => x));

            if (!baseMatches) return false;

            if (!string.IsNullOrEmpty(Subsystem) || !string.IsNullOrEmpty(action.Subsystem)) {
                if (!string.Equals(Subsystem, action.Subsystem, StringComparison.OrdinalIgnoreCase)) return false;
                if (!string.Equals(Verb, action.Verb, StringComparison.OrdinalIgnoreCase)) return false;
                if (!string.Equals(SlotGroup, action.SlotGroup, StringComparison.OrdinalIgnoreCase)) return false;
                if (SlotIndex != action.SlotIndex) return false;
                if (!string.Equals(Element, action.Element, StringComparison.OrdinalIgnoreCase)) return false;
                if (!string.Equals(Source, action.Source, StringComparison.OrdinalIgnoreCase)) return false;
            }

            return true;
        }
    }
}
