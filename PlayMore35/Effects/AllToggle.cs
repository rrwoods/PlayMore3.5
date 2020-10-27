using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Effects {
    // A toggle that is active if all its child toggles are active.
    class AllToggle : IToggle {
        private readonly IToggle[] toggles;

        public AllToggle(params IToggle[] toggles) {
            this.toggles = toggles;
        }

        public bool On(EffectPool pool) {
            return toggles.All(x => x.On(pool));
        }
    }
}
