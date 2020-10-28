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

        public VariableAmount(Statistic stat, decimal factor)
            : this(stat) {
            this.factor = factor;
        }

        private readonly Statistic stat;
        private readonly decimal factor = 1;

        public int Get(EffectPool pool) {
            return (int) Math.Floor(stat.GetValue(pool) * factor);
        }
    }
}
