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

        public EffectPart(IAmount amount, string type, string affects)
            : this(type, affects) {
            this.AddAmount(amount);
        }

        public EffectPart(string type, string affects) {
            this.Type = type;
            this.Affects = affects;
        }

        private readonly List<IAmount> Amounts = new List<IAmount>();

        public EffectPart AddAmount(int amount) {
            return this.AddAmount(new FixedAmount(amount));
        }

        public EffectPart AddAmount(IAmount amount) {
            this.Amounts.Add(amount);
            return this; // method chaining
        }

        public string Type { get; private set; }

        public string Affects { get; private set; }

        public int ComputeAmount(EffectPool pool) {
            return this.Amounts.Select(x => x.Get(pool)).Sum();
        }
    }
}
