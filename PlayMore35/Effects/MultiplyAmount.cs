using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Effects {
    public class MultiplyAmount : IAmount {
        private readonly IAmount Amount;
        private readonly decimal Multiplier;

        public MultiplyAmount(decimal multiplier, IAmount amount) {
            Multiplier = multiplier;
            Amount = amount;
        }

        public MultiplyAmount(decimal multiplier, Statistic amount)
            : this(multiplier, new VariableAmount(amount)) {

        }

        public int Get(EffectPool pool) {
            return (int) Math.Floor(Multiplier * Amount.Get(pool));
        }
    }
}
