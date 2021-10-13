using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PlayMore35.Global {
    public class Rounds : IJsonable {
        private readonly MainForm mainForm;

        public DateTime RoundZero { get; private set; }
        public DateTime EncounterStartTime { get; private set; }
        public long EncounterStartRound { get; private set; }
        public long CurrentRound { get; private set; } = 0;
        public bool Combat { get; private set; } = false;

        private Dictionary<long, List<SheetAction>> scheduledActions = new Dictionary<long, List<SheetAction>>();

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
                ))))
            );
        }

        public Rounds(MainForm mainForm, Rounds previous, JObject json) {
            this.mainForm = mainForm;
            if (json == null) {
                // this happens when loading a character that has not yet saved any time data.  should go away soon tm.
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
                            ((JArray)sched.Value).Select(x => new SheetAction(mainForm, (JObject)x)).ToList();
                    }
                }
            }
        }

        public Rounds(MainForm mainForm) {
            this.mainForm = mainForm;
            DefaultInitialize();
        }

        private void DefaultInitialize() {
            DateTime now = DateTime.Now; // TODO : this should probably be the earliest pickable date instead
            EncounterStartTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, (now.Second / 6) * 6);
            RoundZero = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, (now.Second / 6) * 6);
        }

        public DateTime CurrentTime => RoundZero.AddSeconds(CurrentRound * 6);

        public void Advance(long rounds = 1) {
            DateTime lastTime = CurrentTime;
            CurrentRound += rounds;
            DateTime time = CurrentTime;

            var resettableParams = mainForm.controller.Effects.resettableParameters;

            if (
                (rounds >= LengthOfTime.UNIT_ROUNDS["week"]) ||
                (time.DayOfWeek < lastTime.DayOfWeek) ||
                (time.DayOfWeek == lastTime.DayOfWeek && time.TimeOfDay < lastTime.TimeOfDay)
            ) {
                resettableParams["week"].ForEach(x => x.Value = 0);
            }

            if (
                (rounds >= LengthOfTime.UNIT_ROUNDS["day"]) ||
                (time.TimeOfDay < lastTime.TimeOfDay)
            ) {
                resettableParams["day"].ForEach(x => x.Value = 0);
            }

            if (
                (rounds >= LengthOfTime.UNIT_ROUNDS["hour"]) ||
                (time.Minute < lastTime.Minute)
            ) {
                resettableParams["hour"].ForEach(x => x.Value = 0);
            }

            if (
                (rounds >= LengthOfTime.UNIT_ROUNDS["minute"]) ||
                (time.Second < lastTime.Second)
            ) {
                resettableParams["minute"].ForEach(x => x.Value = 0);
            }

            if (rounds > 0) {
                resettableParams["round"].ForEach(x => x.Value = 0);
            }

            foreach (long round in scheduledActions.Keys.ToList()) {
                if (round <= CurrentRound) {
                    scheduledActions[round].ForEach(x => x.Perform());
                    scheduledActions.Remove(round);
                }
            }

            mainForm.RefreshEffects();
        }

        public void SetTime(DateTime time) {
            Advance( (( (long)(time - RoundZero).TotalSeconds )/6) - CurrentRound );
        }

        public void InCombat(bool combat) {
            if (this.Combat == combat) {
                return;
            }
            this.Combat = combat;
            EncounterStartTime = CurrentTime;
            EncounterStartRound = CurrentRound;

            mainForm.controller.Effects.resettableParameters["encounter"].ForEach(x => x.Value = 0);
            mainForm.RefreshEffects();
        }

        public void Schedule(SheetAction action, long roundsFromNow) {
            long absoluteRound = CurrentRound + roundsFromNow;
            if (!scheduledActions.ContainsKey(absoluteRound)) {
                scheduledActions[absoluteRound] = new List<SheetAction>();
            }
            scheduledActions[absoluteRound].Add(action);
        }
    }
}
