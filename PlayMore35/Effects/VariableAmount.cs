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
        private float factor = 1f;

        public int Get(EffectPool pool) {
            return (int) (stat.GetValue(pool) * factor);
        }

        public VariableAmount Halve() {
            factor = 0.5f;
            return this; // method chaining
        }

        public VariableAmount Double() {
            factor = 2f;
            return this;
        }

        public VariableAmount Negate() {
            factor = -1f;
            return this;
        }
    }
}
