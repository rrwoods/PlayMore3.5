using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PlayMore35.Global {
    public class Rounds : IJsonable {
        private List<IRoundObserver> observers = new List<IRoundObserver>();
        private readonly MainForm mainForm;

        private List<ISaveableRoundObserver> savedObservers = new List<ISaveableRoundObserver>();
        private DateTime roundZero;
        private DateTime encounterStartTime;
        private long encounterStartRound;
        private long currentRound = 0;
        private bool combat = false;

        public JToken ToJson() {
            return new JObject(
                new JProperty("observers", new JArray(savedObservers.Select(x => x.ToJson()))),
                new JProperty("round zero", roundZero.ToString("O")),
                new JProperty("encounter start time", encounterStartTime.ToString("O")),
                new JProperty("encounter start round", encounterStartRound),
                new JProperty("current round", currentRound),
                new JProperty("combat", combat)
            );
        }

        public Rounds(MainForm mainForm, Rounds previous, JObject json) {
            this.mainForm = mainForm;
            if (json == null) {
                // this happens when loading a character that has not yet saved any time data.  should go away soon tm.
                DefaultInitialize();
            } else {
                savedObservers.AddRange(json["observers"].Select(x => RoundObserver.FromJson(mainForm, x)));
                roundZero = DateTime.Parse((string)json["round zero"], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                encounterStartTime = DateTime.Parse((string)json["encounter start time"], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                encounterStartRound = (long)json["encounter start round"];
                currentRound = (long)json["current round"];
                combat = (bool)json["combat"];
            }
            previous.observers.ForEach(x => Register(x));
        }

        public Rounds(MainForm mainForm) {
            this.mainForm = mainForm;
            DefaultInitialize();
            DefaultObservers();
        }

        private void DefaultInitialize() {
            DateTime now = DateTime.Now;
            encounterStartTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, (now.Second / 6) * 6);
            roundZero = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, (now.Second / 6) * 6);
        }

        private void DefaultObservers() {
            Register(new ActionResourceResetter(mainForm));
        }

        public void Register(IRoundObserver observer) {
            Notify(observer, 0);
            observers.Add(observer);
        }

        public void RegisterAndSave(ISaveableRoundObserver observer) {
            if (!Notify(observer, 0)) {
                savedObservers.Add(observer);
            }
        }

        public DateTime CurrentTime => roundZero.AddSeconds(currentRound * 6);

        private bool Notify(IRoundObserver observer, long advanced) {
            return observer.SetState(CurrentTime, currentRound, currentRound - encounterStartRound, advanced, combat);
        }

        private void NotifyRounds(long advanced) {
            observers.RemoveAll(x => Notify(x, advanced));
            savedObservers.RemoveAll(x => Notify(x, advanced));
        }

        public void Advance(long rounds = 1) {
            currentRound += rounds;
            NotifyRounds(rounds);
        }

        public void SetTime(DateTime time) {
            long previousRound = currentRound;
            currentRound = (long)((time - roundZero).TotalSeconds) / 6;
            NotifyRounds(currentRound - previousRound);
        }

        public void InCombat(bool combat) {
            if (this.combat == combat) {
                return;
            }
            this.combat = combat;
            encounterStartTime = CurrentTime;
            encounterStartRound = currentRound;
            NotifyRounds(0);
        }
    }
}
