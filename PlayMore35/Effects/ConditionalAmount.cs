using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Effects {
    class ConditionalAmount : IAmount {
        public enum Relation { LESS_THAN, LESS_THAN_OR_EQUAL, EQUAL, GREATER_THAN_OR_EQUAL, GREATER_THAN }

        private IAmount Left = null;
        private IAmount Right = null;
        private Relation Relationship = Relation.EQUAL;

        private IAmount Amount = null;

        public ConditionalAmount(IAmount left, Relation relationship, int right, int amount) {
            Left = left;
            Relationship = relationship;
            Right = new FixedAmount(right);
            Amount = new FixedAmount(amount);
        }

        public ConditionalAmount(IAmount left, Relation relationship, IAmount right, IAmount amount) {
            Left = left;
            Relationship = relationship;
            Right = right;
            Amount = amount;
        }

        public int Get(EffectPool pool) {
            int left = Left.Get(pool);
            int right = Right.Get(pool);

            bool match = false;
            switch (Relationship) {
                case Relation.LESS_THAN:
                    match = left < right;
                    break;
                case Relation.LESS_THAN_OR_EQUAL:
                    match = left <= right;
                    break;
                case Relation.EQUAL:
                    match = left == right;
                    break;
                case Relation.GREATER_THAN_OR_EQUAL:
                    match = left >= right;
                    break;
                case Relation.GREATER_THAN:
                    match = left > right;
                    break;
            }

            return match ? Amount.Get(pool) : 0;
        }
    }
}
