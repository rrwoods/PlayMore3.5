using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Global {
    public class Rounds {
        private List<IRoundObserver> observers = new List<IRoundObserver>();

        private readonly DateTime roundZero;
        private DateTime encounterStartTime;
        private long encounterStartRound;
        private long currentRound = 0;
        private bool combat = false;

        public Rounds() {
            DateTime now = DateTime.Now;
            encounterStartTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, (now.Second / 6) * 6);
            roundZero = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, (now.Second / 6) * 6);
        }

        public void Register(IRoundObserver observer) {
            observers.Add(observer);
            observer.SetState(CurrentTime, currentRound, currentRound - encounterStartRound, 0, combat);
        }

        public void RegisterAndSave<T>(T observer, Controller controller) where T : IRoundObserver, IJsonable {
            Register(observer);
        }

        public DateTime CurrentTime => roundZero.AddSeconds(currentRound * 6);

        private void NotifyRounds(long advanced) {
            DateTime time = CurrentTime;
            observers.RemoveAll(x => x.SetState(time, currentRound, currentRound - encounterStartRound, advanced, combat));
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
