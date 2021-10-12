using PlayMore35.CharacterChoices;
using System;
using System.Collections.Generic;

namespace PlayMore35.Global {
    public class Rounds {
        private List<IRoundObserver> observers = new List<IRoundObserver>();
        private Controller controller;

        private readonly DateTime roundZero;
        private DateTime encounterStartTime;
        private long encounterStartRound;
        private long currentRound = 0;
        private bool combat = false;

        public Rounds(Controller controller) {
            this.controller = controller;

            DateTime now = DateTime.Now;
            encounterStartTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, (now.Second / 6) * 6);
            roundZero = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, (now.Second / 6) * 6);
        }

        public void Register(IRoundObserver observer) {
            if (!Notify(observer, 0)) {
                observers.Add(observer);
            }
        }

        public void RegisterAndSave(ISaveableRoundObserver observer) {
            if (!Notify(observer, 0)) {
                controller.Character.roundObservers.Add(observer);
            }
        }

        public DateTime CurrentTime => roundZero.AddSeconds(currentRound * 6);

        private bool Notify(IRoundObserver observer, long advanced) {
            return observer.SetState(CurrentTime, currentRound, currentRound - encounterStartRound, advanced, combat);
        }

        private void NotifyRounds(long advanced) {
            observers.RemoveAll(x => Notify(x, advanced));
            controller.Character.roundObservers.RemoveAll(x => Notify(x, advanced));
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
