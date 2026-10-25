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

        public Modifier(JObject json) {
            Type = (string?)json["type"] ?? "";
            if (json["amounts"] != null) {
                Amounts.AddRange(json["amounts"]!.Select(x => Amount.FromJson(x)!));
            }
        }

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

        public int ComputeAmount(EffectPool pool, Items.Equipped items) {
            return Amounts.Select(x => x.Get(pool, items)).Sum();
        }
    }
}
