using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Effects {
    class UnToggle : IToggle {
        private readonly IToggle basis;

        public UnToggle(IToggle basis) {
            this.basis = basis;
        }

        public bool On(EffectPool pool) {
            return !basis.On(pool);
        }
    }
}
