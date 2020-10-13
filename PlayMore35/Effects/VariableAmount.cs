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

        public int Get(EffectPool pool) {
            return stat.GetValue(pool);
        }
    }
}
