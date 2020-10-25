using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Effects {
    class ToggleableAmount : IAmount {
        private readonly IToggle Condition;
        private IAmount Amount = null;

        public ToggleableAmount(IAmount left, Relation relationship, int right, int amount) {
            Condition = new ConditionalToggle(left, relationship, right);
            Amount = new FixedAmount(amount);
        }

        public ToggleableAmount(IAmount left, Relation relationship, IAmount right, IAmount amount) {
            Condition = new ConditionalToggle(left, relationship, right);
            Amount = amount;
        }

        public int Get(EffectPool pool) {
            return Condition.On(pool) ? Amount.Get(pool) : 0;
        }
    }
}
