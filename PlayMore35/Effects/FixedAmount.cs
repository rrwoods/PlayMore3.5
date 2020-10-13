using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Effects {
    public class FixedAmount : IAmount {
        public FixedAmount(int amount) {
            this.Amount = amount;
        }

        public int Amount { get; private set; }

        public int Get(EffectPool pool) {
            return this.Amount;
        }
    }
}
