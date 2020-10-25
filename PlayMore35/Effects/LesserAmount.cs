using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Effects {
    public class LesserAmount : IAmount {
        private List<IAmount> Amounts = new List<IAmount>();

        public LesserAmount(params IAmount[] amounts) {
            Amounts.AddRange(amounts);
        }

        public LesserAmount(params Statistic[] stats) {
            Amounts.AddRange(stats.Select(x => new VariableAmount(x)));
        }

        public int Get(EffectPool pool) {
            return Amounts.Select(x => x.Get(pool)).Min();
        }
    }
}
