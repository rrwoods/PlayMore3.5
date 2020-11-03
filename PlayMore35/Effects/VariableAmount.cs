using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Effects {
    public class VariableAmount : IAmount {
        private readonly string stat;

        public VariableAmount(string stat) {
            this.stat = stat;
        }
        
        public int Get(EffectPool pool) {
            return pool.GetStat(stat).GetValue(pool).value;
        }
    }
}
