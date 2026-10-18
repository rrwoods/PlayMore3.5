using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace PlayMore35.Effects {
    public class ExtraAttack : IJsonable {
        public readonly int AttackPenalty;
        public readonly string BonusType;

        public JToken ToJson() {
            return new JObject(
                new JProperty("attack penalty", AttackPenalty),
                new JProperty("bonus type", BonusType)
            );
        }

        public ExtraAttack(JObject json) {
            AttackPenalty = (int)json["attack penalty"];
            BonusType = (string)json["bonus type"];
        }

        // attackPenalty is the penalty to *this extra attack* -- for penalties to all attacks
        // as a result of the same effect, add a Modifier to the effect.
        // bonusType should be ModifierTypes.ENHANCEMENT for explicitly non-stacking extra attacks (like haste/speed)
        // ModifierTypes.BASE for iteratives, and ModifierTypes.UNTYPED for others.  Can use any stacking bonus type for others,
        // but ModifierTypes.UNTYPED is probably the only one that makes sense.
        public ExtraAttack(int attackPenalty, string bonusType) {
            AttackPenalty = attackPenalty;
            BonusType = bonusType;
        }
    }
}
