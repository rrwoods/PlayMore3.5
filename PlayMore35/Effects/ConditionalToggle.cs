using System;
using Newtonsoft.Json.Linq;

namespace PlayMore35.Effects {
    public class ConditionalToggle : IToggle {
        private IAmount Left;
        private readonly Relation Relationship;
        private IAmount Right;

        public JToken ToJson() {
            return new JObject(new JProperty("conditional", new JObject(
                new JProperty("left", Left.ToJson()),
                new JProperty("relation", Relationship.ToString()),
                new JProperty("right", Right.ToJson())
            )));
        }

        public ConditionalToggle(JObject obj) {
            Left = Amount.FromJson(obj["left"]);
            Relationship = (Relation) Enum.Parse(typeof(Relation), (string)obj["relation"], false);
            Right = Amount.FromJson(obj["right"]);
        }

        public ConditionalToggle(IAmount left, Relation relationship, IAmount right) {
            Left = left;
            Relationship = relationship;
            Right = right;
        }

        public ConditionalToggle(IAmount left, Relation relationship, int right)
            : this(left, relationship, new FixedAmount(right)) {

        }

        public ConditionalToggle(string left, Relation relationship, int right)
            : this(new VariableAmount(left), relationship, new FixedAmount(right)) {
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
