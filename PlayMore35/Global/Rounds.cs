using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Global {
    public class Rounds {
        private List<IRoundObserver> observers = new List<IRoundObserver>();

        private DateTime roundZero;
        private long currentRound = 0;
        private bool combat = false;

        public Rounds() {
            DateTime now = DateTime.Now;
            roundZero = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, (now.Second / 6) * 6);
        }

        public void Register(IRoundObserver observer) {
            observers.Add(observer);
            observer.InCombat(combat);
            if (combat) {
                observer.SetCurrentCombatRound(currentRound, 0);
            } else {
                observer.SetCurrentTime(CurrentTime);
            }
        }

        public DateTime CurrentTime => roundZero.AddSeconds(currentRound * 6);

        private void NotifyRounds(long advanced) {
            if (combat) {
                observers.ForEach(x => x.SetCurrentCombatRound(currentRound, advanced));
            } else {
                DateTime cur = CurrentTime;
                observers.ForEach(x => x.SetCurrentTime(cur));
            }
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
            observers.ForEach(x => x.InCombat(combat));

            roundZero = CurrentTime;
            currentRound = 0;
            NotifyRounds(0);
        }
    }
}
