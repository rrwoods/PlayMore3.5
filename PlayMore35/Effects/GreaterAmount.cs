using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Effects {
    public class GreaterAmount : IAmount {
        private List<IAmount> Amounts = new List<IAmount>();

        public GreaterAmount(params IAmount[] amounts) {
            Amounts.AddRange(amounts);
        }

        public GreaterAmount(params string[] stats) {
            Amounts.AddRange(stats.Select(x => new VariableAmount(x)));
        }

        public int Get(EffectPool pool) {
            return Amounts.Select(x => x.Get(pool)).Max();
        }
    }
}
