using Newtonsoft.Json;
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
        [JsonProperty("step")]
        public SubsystemStep? Step { get; set; }
        internal string duration = null; // null for instantaneous, effect will not be reversed
        internal readonly List<string> flagsToSet = new List<string>();
        internal readonly List<string> flagsToClear = new List<string>();
        internal readonly Dictionary<string, int> paramsToAdjust = new Dictionary<string, int>();
        internal List<SheetAction> actionsAfter = new List<SheetAction>(); // actions to fire at the end of the duration

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
                if (Step != null) json["step"] = Step.ToJson();
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
                if (json["step"] is JObject stepObj) Step = new SubsystemStep(stepObj);
            }
        }

        public SheetAction() { }

        internal SheetAction Revert() {
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

                if (Step == null) {
                    revert.Step = null;
                } else if (Step.IsTransition) {
                    if (!string.IsNullOrEmpty(Step.FromState) && !string.IsNullOrEmpty(Step.ToState)) {
                        revert.Step = SubsystemStep.Transition(
                            slotGroup: Step.SlotGroup,
                            toState: Step.FromState,
                            fromState: Step.ToState,
                            selector: Step.Selector,
                            countExpression: Step.CountExpression,
                            onEmptyVerb: Step.OnEmptyVerb);
                    } else {
                        revert.Step = null;
                    }
                } else if (string.Equals(Step.Type, "pool", StringComparison.OrdinalIgnoreCase)) {
                    string invOp = string.Equals(Step.Op, "consume", StringComparison.OrdinalIgnoreCase) ? "recharge"
                                 : string.Equals(Step.Op, "recharge", StringComparison.OrdinalIgnoreCase) ? "consume"
                                 : null;
                    if (invOp != null) {
                        revert.Step = SubsystemStep.Pool(Step.PoolId, invOp, Step.Amount);
                    } else {
                        revert.Step = null;
                    }
                } else {
                    revert.Step = null;
                }
            }

            return revert;
        }

        public SheetAction SetFlag(string flag) {
            flagsToSet.Add(flag);
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

            if (!string.IsNullOrEmpty(Subsystem) && context != null) {
                var engine = context.GetSubsystem(Subsystem);
                if (engine != null) {
                    var target = new SubsystemTarget(SlotGroup, SlotIndex, Element, Source);
                    if (!string.IsNullOrEmpty(Verb)) {
                        var req = engine.ExecuteVerb(Verb, target);
                        if (req > requirement) requirement = req;
                    } else if (Step != null) {
                        var req = engine.ExecuteStep(Step, target);
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
    }
}
