using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Global {
    public class Rounds {
        private List<IRoundObserver> observers = new List<IRoundObserver>();

        private DateTime roundZero = DateTime.MinValue;
        private int currentRound = 0;
        private bool combat = false;

        public void Register(IRoundObserver observer) {
            observers.Add(observer);
            observer.SetCurrentRound(currentRound, combat);
        }

        private void Notify() {
            observers.ForEach(x => x.SetCurrentRound(currentRound, combat));
        }

        public void Advance(int rounds = 1) {
            currentRound += rounds;
            Notify();
        }
    }
}
