using Newtonsoft.Json.Linq;
using PlayMore35.Effects;
using PlayMore35.Subsystems;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlayMore35.Global {
    public class SheetAction : IJsonable {
        string duration = null; // null for instantaneous, effect will not be reversed
        private readonly List<string> flagsToSet = new List<string>();
        private readonly List<string> flagsToClear = new List<string>();
        private readonly Dictionary<string, int> paramsToAdjust = new Dictionary<string, int>();
        private readonly List<(string subsystem, int slotIndex)> slotsToClear = new List<(string, int)>();
        List<SheetAction> actionsAfter = new List<SheetAction>(); // actions to fire at the end of the duration

        public JToken ToJson() {
            JObject json = new JObject();
            if (duration != null) {
                json["duration"] = duration;
            }
            json.AddList("set", flagsToSet);
            json.AddList("clear", flagsToClear);
            if (paramsToAdjust.Any()) {
                json["adjust"] = new JObject(paramsToAdjust.Select(x => new JProperty(x.Key, x.Value)));
            }
            if (slotsToClear.Any()) {
                json["clear_slots"] = new JArray(slotsToClear.Select(s => new JObject(
                    new JProperty("subsystem", s.subsystem),
                    new JProperty("slot", s.slotIndex)
                )));
                var vestigeSlots = slotsToClear.Where(s => s.subsystem == "vestige").Select(s => s.slotIndex).ToList();
                if (vestigeSlots.Any()) {
                    json.AddList("unbind", vestigeSlots);
                }
            }
            json.AddList("after", actionsAfter);
            return json;
        }

        public SheetAction(JObject json) {
            if (json["duration"] != null) {
                duration = (string)json["duration"];
            }
            flagsToSet.ReadJson("set", json);
            flagsToClear.ReadJson("clear", json);
            if (json["adjust"] != null) {
                paramsToAdjust = ((JObject)json["adjust"]).Properties()
                    .ToDictionary(x => x.Name, x => int.Parse((string)x.Value));
            }
            if (json["clear_slots"] != null) {
                foreach (var token in (JArray)json["clear_slots"]) {
                    ClearSlot((string)token["subsystem"], (int)token["slot"]);
                }
            } else if (json["unbind"] != null) {
                foreach (var token in (JArray)json["unbind"]) {
                    ClearSlot("vestige", (int)token);
                }
            }
            if (json["after"] != null) {
                actionsAfter.AddRange(json["after"].Select(x => new SheetAction((JObject)x)));
            }
        }

        public SheetAction() { }

        private SheetAction Revert() {
            SheetAction revert = new SheetAction();
            revert.flagsToSet.AddRange(flagsToClear);
            revert.flagsToClear.AddRange(flagsToSet);
            foreach (var adjustment in paramsToAdjust) {
                revert.paramsToAdjust[adjustment.Key] = -adjustment.Value;
            }
            revert.actionsAfter.AddRange(actionsAfter);
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

        public SheetAction ClearSlot(string subsystem, int slotIndex) {
            slotsToClear.Add((subsystem, slotIndex));
            return this;
        }

        public SheetAction UnbindSlot(int slot) {
            return ClearSlot("vestige", slot);
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

            if (duration == "encounter" && !context.Timeline.Combat) {
                foreach (var after in actionsAfter) {
                    var req = after.Perform(context);
                    if (req > requirement) requirement = req;
                }
                return requirement;
            }

            var effects = context.Effects;
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
                    }
                }
            }

            foreach (var slot in slotsToClear) {
                var slotted = context.Subsystems?.Get(slot.subsystem) as ISlottedSubsystem
                    ?? (slot.subsystem == "vestige" ? context.Subsystems?.Get<ISlottedSubsystem>() : null);
                if (slotted != null) {
                    slotted.ClearSlot(slot.slotIndex);
                } else {
                    context.ClearSlot(slot.subsystem, slot.slotIndex);
                }
                requirement = TimelineRefreshRequirement.FullReload;
            }

            if (duration == null) {
                foreach (var after in actionsAfter) {
                    var req = after.Perform(context);
                    if (req > requirement) requirement = req;
                }
            } else {
                context.Timeline.Schedule(Revert(), duration, context);
            }

            return requirement;
        }

        public bool Matches(SheetAction action) {
            return duration == action.duration &&
                   flagsToSet.SequenceEqual(action.flagsToSet) &&
                   flagsToClear.SequenceEqual(action.flagsToClear) &&
                   paramsToAdjust.Count == action.paramsToAdjust.Count && !paramsToAdjust.Except(action.paramsToAdjust).Any() &&
                   slotsToClear.SequenceEqual(action.slotsToClear) &&
                   actionsAfter.Count == action.actionsAfter.Count && (actionsAfter.Zip(action.actionsAfter, (x, y) => x.Matches(y)).All(x => x));
        }
    }
}

