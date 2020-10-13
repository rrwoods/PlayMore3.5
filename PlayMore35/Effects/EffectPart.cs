using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Effects {
    public class EffectPart {
        public EffectPart(int amount, string type, string affects)
            : this(new FixedAmount(amount), type, affects) {
            
        }

        public EffectPart(Statistic amount, string type, string affects)
            : this(new VariableAmount(amount), type, affects) {

        }

        public EffectPart(IAmount amount, string type, string affects) {
            this.Amount = amount;
            this.Type = type;
            this.Affects = affects;
        }

        public IAmount Amount { get; private set; }

        public string Type { get; private set; }

        public string Affects { get; private set; }
    }
}
