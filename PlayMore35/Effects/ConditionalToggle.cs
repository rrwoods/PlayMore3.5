using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Effects {
    public class ConditionalToggle : IToggle {
        private IAmount Left = null;
        private IAmount Right = null;
        private readonly Relation Relationship = Relation.EQUAL;

        public ConditionalToggle(IAmount left, Relation relationship, IAmount right) {
            Left = left;
            Relationship = relationship;
            Right = right;
        }

        public ConditionalToggle(IAmount left, Relation relationship, int right)
            : this(left, relationship, new FixedAmount(right)) {

        }

        public bool On(EffectPool pool) {
            int left = Left.Get(pool);
            int right = Right.Get(pool);

            switch (Relationship) {
                case Relation.LESS_THAN:
                    return left < right;
                case Relation.LESS_THAN_OR_EQUAL:
                    return left <= right;
                case Relation.EQUAL:
                    return left == right;
                case Relation.GREATER_THAN_OR_EQUAL:
                    return left >= right;
                case Relation.GREATER_THAN:
                    return left > right;
            }

            return false; // we should never get here...
        }
    }
}
