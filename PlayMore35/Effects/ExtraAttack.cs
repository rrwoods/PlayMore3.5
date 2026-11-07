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
            AttackPenalty = (int)json["attack penalty"]!;
            BonusType = (string?)json["bonus type"] ?? "";
        }
    }
}
