using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35 {
    public interface IRoundObserver {
        // all functions should whether this observer should be removed; this is useful for observers whose job
        // it is to turn off a temporary effect at a later point.  if your observer is needed
        // forever, just always return false.

        // guaranteed to only be called during combat
        // currentRound is what round it is right now
        // advanced is how many rounds have passed since the last time we were notified (currently always 1 but you never know), or 0 on wakeup
        bool SetCurrentCombatRound(long currentRound, long advanced);

        // guaranteed to only be called out of combat
        bool SetCurrentTime(DateTime time);

        // always called on register, and the moment combat switches
        // will always be immediately followed by a SetCurrentCombatRound (if in combat) or SetCurrentTime (if out of combat)
        bool InCombat(bool combat);
    }
}
