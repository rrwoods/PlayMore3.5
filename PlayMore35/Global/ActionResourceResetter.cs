using PlayMore35.Effects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Global {
    class ActionResourceResetter : IRoundObserver {
        private DateTime? lastTime = null;
        private bool lastCombatStatus = false;
        // i tried just storing a reference to the resettableParams dictionary here, but it somehow magically got out of sync.
        // i might be misunderstanding something about how reference assignment works in c#.
        private readonly Controller controller;

        public ActionResourceResetter(Controller controller) {
            this.controller = controller;
        }

        public bool SetState(DateTime time, long currentAbsoluteRound, long currentEncounterRound, long advanced, bool combat) {
            if (this.lastTime == null) {
                this.lastTime = time;
                this.lastCombatStatus = combat;
                return false;
            }
            DateTime lastTime = (DateTime)this.lastTime; // cast away nullable because c# is stupid.
            var resettableParams = controller.Effects.resettableParameters;

            if (
                (advanced >= LengthOfTime.UNIT_ROUNDS["week"]) ||
                (time.DayOfWeek < lastTime.DayOfWeek) ||
                (time.DayOfWeek == lastTime.DayOfWeek && time.TimeOfDay < lastTime.TimeOfDay)
            ) {
                resettableParams["week"].ForEach(x => x.Value = 0);
            }

            if(
                (advanced >= LengthOfTime.UNIT_ROUNDS["day"]) ||
                (time.TimeOfDay < lastTime.TimeOfDay)
            ) {
                resettableParams["day"].ForEach(x => x.Value = 0);
            }

            if (
                (advanced >= LengthOfTime.UNIT_ROUNDS["hour"]) ||
                (time.Minute < lastTime.Minute)
            ) {
                resettableParams["hour"].ForEach(x => x.Value = 0);
            }

            if (
                (advanced >= LengthOfTime.UNIT_ROUNDS["minute"]) ||
                (time.Second < lastTime.Second)
            ) {
                resettableParams["minute"].ForEach(x => x.Value = 0);
            }

            if (advanced > 0) {
                resettableParams["round"].ForEach(x => x.Value = 0);
            }

            if (lastCombatStatus != combat) {
                resettableParams["encounter"].ForEach(x => x.Value = 0);
            }

            lastCombatStatus = combat;
            lastTime = time;
            return false;
        }
    }
}
