using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Effects {
    public class VariableAmount : IAmount {
        public VariableAmount(Statistic stat) {
            this.stat = stat;
        }

        private readonly Statistic stat;
        private bool halve = false;

        public int Get(EffectPool pool) {
            int value = stat.GetValue(pool);
            if (halve) {
                value /= 2;
            }
            return value;
        }

        public VariableAmount Halve() {
            this.halve = true;
            return this; // method chaining
        }
    }
}
