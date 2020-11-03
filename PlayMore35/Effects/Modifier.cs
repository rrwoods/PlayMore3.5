using System.Collections.Generic;
using System.Linq;

namespace PlayMore35.Effects {
    public class Modifier {
        public string Name = "";
        private readonly List<IAmount> Amounts = new List<IAmount>();
        public string Type { get; private set; }

        public Modifier(string type) {
            Type = type;
        }

        public Modifier(int amount, string type)
            : this(new FixedAmount(amount), type) {
            
        }
        
        public Modifier(string stat, string type)
            : this(new VariableAmount(stat), type) {

        }

        public Modifier(decimal multiplier, string stat, string type)
            : this(new MultiplyAmount(multiplier, new VariableAmount(stat)), type) {

        }

        public Modifier(IAmount amount, string type)
            : this(type) {
            AddAmount(amount);
        }

        public Modifier AddAmount(int amount) {
            return AddAmount(new FixedAmount(amount));
        }

        public Modifier AddAmount(decimal multiplier, string stat) {
            return AddAmount(new MultiplyAmount(multiplier, new VariableAmount(stat)));
        }

        public Modifier AddAmount(IAmount amount) {
            Amounts.Add(amount);
            return this;
        }

        public int ComputeAmount(EffectPool pool) {
            return Amounts.Select(x => x.Get(pool)).Sum();
        }
    }
}
