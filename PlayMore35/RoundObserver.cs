using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35 {
    public interface IRoundObserver {
        // returns whether this observer should be removed; this is useful for observers whose job
        // it is to turn off a temporary effect at a later point.  if your observer is needed
        // forever, just always return false.
        bool SetCurrentRound(int currentRound, bool combat);
    }
}
