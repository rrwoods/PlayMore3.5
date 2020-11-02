using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Effects {
    public class FixedAmount : IAmount {
        public int Amount { get; private set; }

        public FixedAmount(int amount) {
            Amount = amount;
        }

        public int Get(EffectPool pool) {
            return Amount;
        }
    }
}
