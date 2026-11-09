using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace PlayMore35.Effects {
    public class Modifier : IJsonable {
        public string Name = "";
        public string Type { get; private set; } = "";
        private readonly List<IAmount> Amounts = new List<IAmount>();

        public JToken ToJson() {
            return new JObject(
                new JProperty("type", Type),
                new JProperty("amounts", new JArray(Amounts.Select(x => x.ToJson())))
            );
        }

        public Modifier(
            string type,
            IEnumerable<IAmount>? amounts = null,
            string name = "") {
            Type = type ?? "";
            Name = name ?? "";
            if (amounts != null) {
                Amounts.AddRange(amounts);
            }
        }

        public Modifier(JObject json)
            : this(
                (string?)json["type"] ?? "",
                json["amounts"]?.Select(x => Amount.FromJson(x)!),
                (string?)json["name"] ?? "") {
        }

        public Modifier(int amount, string type)
            : this(type, new IAmount[] { new FixedAmount(amount) }) {
        }
        
        public Modifier(string stat, string type)
            : this(type, new IAmount[] { new VariableAmount(stat) }) {
        }

        public int ComputeAmount(EffectPool pool, Items.Equipped items) {
            return Amounts.Select(x => x.Get(pool, items)).Sum();
        }
    }
}
