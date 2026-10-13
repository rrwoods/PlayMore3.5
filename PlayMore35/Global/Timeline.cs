using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PlayMore35.Global {
    public class Timeline : IJsonable {
        public DateTime RoundZero { get; private set; }
        public DateTime EncounterStartTime { get; private set; }
        public long EncounterStartRound { get; private set; }
        public long CurrentRound { get; private set; } = 0;
        public bool Combat { get; private set; } = false;

        public ICharacterContext Context { get; set; }

        private Dictionary<long, List<SheetAction>> scheduledActions = new Dictionary<long, List<SheetAction>>();
        private List<SheetAction> encounterActions = new List<SheetAction>();

        public IReadOnlyDictionary<long, IReadOnlyList<SheetAction>> ScheduledActions =>
            scheduledActions.ToDictionary(k => k.Key, v => (IReadOnlyList<SheetAction>)v.Value.AsReadOnly());

        public IReadOnlyList<SheetAction> EncounterActions => encounterActions.AsReadOnly();

        public JToken ToJson() {
            return new JObject(
                new JProperty("round zero", RoundZero.ToString("O")),
                new JProperty("encounter start time", EncounterStartTime.ToString("O")),
                new JProperty("encounter start round", EncounterStartRound),
                new JProperty("current round", CurrentRound),
                new JProperty("combat", Combat),
                new JProperty("scheduled actions", new JObject(scheduledActions.Select(sched => new JProperty(
                    sched.Key.ToString(),
                    new JArray(sched.Value.Select(action => action.ToJson()))
                )))),
                new JProperty("encounter actions", new JArray(encounterActions.Select(action => action.ToJson())))
            );
        }

        public Timeline(JObject json) {
            if (json == null) {
                DefaultInitialize();
            } else {
                RoundZero = DateTime.Parse((string)json["round zero"], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                EncounterStartTime = DateTime.Parse((string)json["encounter start time"], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                EncounterStartRound = (long)json["encounter start round"];
                CurrentRound = (long)json["current round"];
                Combat = (bool)json["combat"];
                if (json["scheduled actions"] != null) {
                    foreach (JProperty sched in json["scheduled actions"]) {
                        scheduledActions[long.Parse(sched.Name)] =
                            ((JArray)sched.Value).Select(x => SheetAction.FromJson((JObject)x)).ToList();
                    }
                }
                if (json["encounter actions"] != null) {
                    encounterActions.AddRange(json["encounter actions"].Select(x => SheetAction.FromJson((JObject)x)));
                }
            }
        }

        public Timeline() {
            DefaultInitialize();
        }

        private void DefaultInitialize() {
            DateTime now = DateTime.Now;
            EncounterStartTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, (now.Second / 6) * 6);
            RoundZero = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, (now.Second / 6) * 6);
        }

        public DateTime CurrentTime => RoundZero.AddSeconds(CurrentRound * 6);

        public TimelineAdvanceResult Advance(long rounds = 1) {
            return Advance(rounds, Context);
        }

        public TimelineAdvanceResult Advance(long rounds, ICharacterContext context) {
            DateTime lastTime = CurrentTime;
            CurrentRound += rounds;
            DateTime time = CurrentTime;

            var result = new TimelineAdvanceResult {
                RoundsAdvanced = rounds,
                PreviousTime = lastTime,
                CurrentTime = time,
                RefreshRequirement = TimelineRefreshRequirement.None
            };

            var ctx = context ?? Context;
            var resettableParams = ctx?.Effects?.resettableParameters;

            if (resettableParams != null) {
                if (
                    (rounds >= LengthOfTime.UNIT_ROUNDS["week"]) ||
                    (time.DayOfWeek < lastTime.DayOfWeek) ||
                    (time.DayOfWeek == lastTime.DayOfWeek && time.TimeOfDay < lastTime.TimeOfDay)
                ) {
                    if (resettableParams.ContainsKey("week")) {
                        var list = resettableParams["week"];
                        if (list.Any(x => x.Value != 0)) {
                            list.ForEach(x => x.Value = 0);
                            result.RefreshRequirement = TimelineRefreshRequirement.RecomputeDerived;
                        } else {
                            list.ForEach(x => x.Value = 0);
                        }
                    }
                }

                if (
                    (rounds >= LengthOfTime.UNIT_ROUNDS["day"]) ||
                    (time.TimeOfDay < lastTime.TimeOfDay)
                ) {
                    if (resettableParams.ContainsKey("day")) {
                        var list = resettableParams["day"];
                        if (list.Any(x => x.Value != 0)) {
                            list.ForEach(x => x.Value = 0);
                            result.RefreshRequirement = TimelineRefreshRequirement.RecomputeDerived;
                        } else {
                            list.ForEach(x => x.Value = 0);
                        }
                    }
                }

                if (
                    (rounds >= LengthOfTime.UNIT_ROUNDS["hour"]) ||
                    (time.Minute < lastTime.Minute)
                ) {
                    if (resettableParams.ContainsKey("hour")) {
                        var list = resettableParams["hour"];
                        if (list.Any(x => x.Value != 0)) {
                            list.ForEach(x => x.Value = 0);
                            result.RefreshRequirement = TimelineRefreshRequirement.RecomputeDerived;
                        } else {
                            list.ForEach(x => x.Value = 0);
                        }
                    }
                }

                if (
                    (rounds >= LengthOfTime.UNIT_ROUNDS["minute"]) ||
                    (time.Second < lastTime.Second)
                ) {
                    if (resettableParams.ContainsKey("minute")) {
                        var list = resettableParams["minute"];
                        if (list.Any(x => x.Value != 0)) {
                            list.ForEach(x => x.Value = 0);
                            result.RefreshRequirement = TimelineRefreshRequirement.RecomputeDerived;
                        } else {
                            list.ForEach(x => x.Value = 0);
                        }
                    }
                }

                if (rounds > 0 && resettableParams.ContainsKey("round")) {
                    var list = resettableParams["round"];
                    if (list.Any(x => x.Value != 0)) {
                        list.ForEach(x => x.Value = 0);
                        result.RefreshRequirement = TimelineRefreshRequirement.RecomputeDerived;
                    } else {
                        list.ForEach(x => x.Value = 0);
                    }
                }
            }

            foreach (long round in scheduledActions.Keys.ToList()) {
                if (round <= CurrentRound) {
                    var actions = scheduledActions[round];
                    scheduledActions.Remove(round);
                    foreach (var action in actions) {
                        result.ExpiredActions.Add(action);
                        if (ctx != null) {
                            var req = action.Perform(ctx);
                            if (req == TimelineRefreshRequirement.FullReload) {
                                result.RefreshRequirement = TimelineRefreshRequirement.FullReload;
                            } else if (req == TimelineRefreshRequirement.RecomputeDerived &&
                                       result.RefreshRequirement != TimelineRefreshRequirement.FullReload) {
                                result.RefreshRequirement = TimelineRefreshRequirement.RecomputeDerived;
                            }
                        }
                    }
                }
            }

            return result;
        }

        public TimelineAdvanceResult SetTime(DateTime time, ICharacterContext context = null) {
            long targetRound = ((long)(time - RoundZero).TotalSeconds) / 6;
            return Advance(targetRound - CurrentRound, context ?? Context);
        }

        public TimelineAdvanceResult StartCombat(ICharacterContext context = null) {
            if (this.Combat) {
                return new TimelineAdvanceResult {
                    RoundsAdvanced = 0,
                    PreviousTime = CurrentTime,
                    CurrentTime = CurrentTime,
                    RefreshRequirement = TimelineRefreshRequirement.None
                };
            }

            this.Combat = true;
            EncounterStartTime = CurrentTime;
            EncounterStartRound = CurrentRound;

            return new TimelineAdvanceResult {
                RoundsAdvanced = 0,
                PreviousTime = CurrentTime,
                CurrentTime = CurrentTime,
                RefreshRequirement = TimelineRefreshRequirement.RecomputeDerived
            };
        }

        public TimelineAdvanceResult EndCombat(ICharacterContext context = null) {
            if (!this.Combat) {
                return new TimelineAdvanceResult {
                    RoundsAdvanced = 0,
                    PreviousTime = CurrentTime,
                    CurrentTime = CurrentTime,
                    RefreshRequirement = TimelineRefreshRequirement.None
                };
            }

            this.Combat = false;
            var ctx = context ?? Context;
            var result = new TimelineAdvanceResult {
                RoundsAdvanced = 0,
                PreviousTime = CurrentTime,
                CurrentTime = CurrentTime,
                RefreshRequirement = TimelineRefreshRequirement.RecomputeDerived
            };

            if (ctx?.Effects?.resettableParameters != null && ctx.Effects.resettableParameters.ContainsKey("encounter")) {
                var list = ctx.Effects.resettableParameters["encounter"];
                list.ForEach(x => x.Value = 0);
            }

            if (ctx != null) {
                foreach (var action in encounterActions) {
                    result.ExpiredActions.Add(action);
                    var req = action.Perform(ctx);
                    if (req == TimelineRefreshRequirement.FullReload) {
                        result.RefreshRequirement = TimelineRefreshRequirement.FullReload;
                    }
                }
            }
            encounterActions.Clear();

            return result;
        }

        public TimelineAdvanceResult InCombat(bool combat, ICharacterContext context = null) {
            return combat ? StartCombat(context) : EndCombat(context);
        }

        public void Schedule(SheetAction action, string durationString, ICharacterContext context = null) {
            var ctx = context ?? Context;
            var effects = ctx?.Effects;
            var items = ctx?.EquippedItems;
            LengthOfTime duration = new LengthOfTime(
                effects != null && items != null
                    ? durationString.FillStatValues(effects, items)
                    : durationString
            );
            if (duration.unit == "encounter") {
                encounterActions.Add(action);
                return;
            }

            long absoluteRound = CurrentRound + duration.InRounds();
            if (!scheduledActions.ContainsKey(absoluteRound)) {
                scheduledActions[absoluteRound] = new List<SheetAction>();
            }
            scheduledActions[absoluteRound].Add(action);
        }

        public void Schedule(SheetAction action, long durationRounds) {
            long absoluteRound = CurrentRound + durationRounds;
            if (!scheduledActions.ContainsKey(absoluteRound)) {
                scheduledActions[absoluteRound] = new List<SheetAction>();
            }
            scheduledActions[absoluteRound].Add(action);
        }

        public void Unschedule(SheetAction action) {
            foreach (var sched in scheduledActions) {
                sched.Value.RemoveAll(x => x.Matches(action));
            }
        }
    }
}

