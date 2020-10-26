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

        public VariableAmount(Statistic stat, float factor)
            : this(stat) {
            this.factor = factor;
        }

        private readonly Statistic stat;
        private float factor = 1f;

        public int Get(EffectPool pool) {
            return (int) (stat.GetValue(pool) * factor);
        }
    }
}
