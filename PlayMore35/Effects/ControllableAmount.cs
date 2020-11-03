using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Effects {
    public class ControllableAmount : IAmount {
        public readonly string Name;
        private IAmount minimum = null;
        private IAmount maximum = null;
        public int Amount;

        public ControllableAmount(string name) {
            Name = name;
        }

        public ControllableAmount Minimum(int min) {
            minimum = new FixedAmount(min);
            return this;
        }

        public ControllableAmount Maximum(string maxStat) {
            maximum = new VariableAmount(maxStat);
            return this;
        }

        // This is a getter with a side effect of changing the property.
        // XXX : This might be a bad idea.
        public int Get(EffectPool pool) {
            if (minimum != null) {
                int min = minimum.Get(pool);
                if (Amount < min) {
                    Amount = min;
                }
            }
            if (maximum != null) {
                int max = maximum.Get(pool);
                if (Amount > max) {
                    Amount = max;
                }
            }
            return Amount;
        }
    }
}
