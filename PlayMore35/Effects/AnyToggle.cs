using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Effects {
    // A toggle that is active if one of its child toggles are active.
    class AnyToggle : IToggle {
        private readonly IToggle[] toggles;

        public AnyToggle(params IToggle[] toggles) {
            this.toggles = toggles;
        }

        public bool On(EffectPool pool) {
            return toggles.Any(x => x.On(pool));
        }
    }
}
