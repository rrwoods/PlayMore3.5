using Newtonsoft.Json.Linq;
using PlayMore35.Effects;
using PlayMore35.Subsystems;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlayMore35.Global {
    public class SheetAction : IJsonable {
        private static readonly List<Func<JObject, SheetAction>> parsers = new List<Func<JObject, SheetAction>>();
        private static readonly object parserLock = new object();
        private static bool defaultParsersRegistered = false;

        private static void EnsureDefaultParsers() {
            if (!defaultParsersRegistered) {
                lock (parserLock) {
                    if (!defaultParsersRegistered) {
                        defaultParsersRegistered = true;
                        SubsystemRegistry.RegisterDefaultActionParsers();
                    }
                }
            }
        }

        public static void RegisterParser(Func<JObject, SheetAction> parser) {
            if (parser != null) {
                lock (parserLock) {
                    if (!parsers.Contains(parser)) {
                        parsers.Add(parser);
                    }
                }
            }
        }

        public static void ResetParsersForTesting() {
            lock (parserLock) {
                parsers.Clear();
                defaultParsersRegistered = false;
                EnsureDefaultParsers();
            }
        }

        public static SheetAction FromJson(JObject json) {
            if (json == null) return null;
            EnsureDefaultParsers();
            List<Func<JObject, SheetAction>> parsersSnapshot;
            lock (parserLock) {
                parsersSnapshot = new List<Func<JObject, SheetAction>>(parsers);
            }
            foreach (var parser in parsersSnapshot) {
                var action = parser(json);
                if (action != null) return action;
            }
            return new SheetAction(json);
        }

        public string Name { get; set; } = null;
        string duration = null; // null for instantaneous, effect will not be reversed
        private readonly List<string> flagsToSet = new List<string>();
        private readonly List<string> flagsToClear = new List<string>();
        private readonly Dictionary<string, int> paramsToAdjust = new Dictionary<string, int>();
        List<SheetAction> actionsAfter = new List<SheetAction>(); // actions to fire at the end of the duration

        public virtual JToken ToJson() {
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
            return json;
        }

        public SheetAction(JObject json) {
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
        }

        public SheetAction() { }

        protected virtual SheetAction Revert() {
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

        public SheetAction Duration(string duration) {
            this.duration = duration;
            return this;
        }

        public SheetAction AndThen(SheetAction after) {
            actionsAfter.Add(after);
            return this;
        }

        public virtual TimelineRefreshRequirement Perform(ICharacterContext context) {
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
                    }
                }
            }

            if (duration == null) {
                foreach (var after in actionsAfter) {
                    var req = after.Perform(context);
                    if (req > requirement) requirement = req;
                }
            } else if (context != null) {
                context.Timeline.Schedule(Revert(), duration, context);
            }

            return requirement;
        }

        public virtual bool Matches(SheetAction action) {
            if (action == null) return false;
            return Name == action.Name &&
                   duration == action.duration &&
                   flagsToSet.SequenceEqual(action.flagsToSet) &&
                   flagsToClear.SequenceEqual(action.flagsToClear) &&
                   paramsToAdjust.Count == action.paramsToAdjust.Count && !paramsToAdjust.Except(action.paramsToAdjust).Any() &&
                   actionsAfter.Count == action.actionsAfter.Count && (actionsAfter.Zip(action.actionsAfter, (x, y) => x.Matches(y)).All(x => x));
        }
    }
}

