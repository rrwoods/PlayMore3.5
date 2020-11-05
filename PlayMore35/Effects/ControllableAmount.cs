using Newtonsoft.Json.Linq;

namespace PlayMore35.Effects {
    public class ControllableAmount : IAmount {
        public readonly string Name;
        private IAmount minimum = null;
        private IAmount maximum = null;
        public int Amt;

        public JToken ToJson() {
            JObject json = new JObject(new JProperty("name", Name));
            if (minimum != null) {
                json.Add("min", minimum.ToJson());
            }
            if (maximum != null) {
                json.Add("max", maximum.ToJson());
            }
            return new JObject(new JProperty("controllable", json));
        }

        public ControllableAmount(JObject json) {
            Name = (string)json["name"];
            if (json["min"] != null) {
                minimum = Amount.FromJson(json["min"]);
            }
            if (json["max"] != null) {
                maximum = Amount.FromJson(json["max"]);
            }
        }

        public ControllableAmount(string name) {
            Name = name;
        }

        public ControllableAmount Minimum(int min) {
            minimum = new FixedAmount(min);
            return this;
        }

        public ControllableAmount Maximum(string maxStat) {
            maximum = new VariableAmount(maxStat);
            return this;
        }

        // This is a getter with a side effect of changing the property.
        // XXX : This might be a bad idea.
        public int Get(EffectPool pool, Items.Equipped items) {
            if (minimum != null) {
                int min = minimum.Get(pool, items);
                if (Amt < min) {
                    Amt = min;
                }
            }
            if (maximum != null) {
                int max = maximum.Get(pool, items);
                if (Amt > max) {
                    Amt = max;
                }
            }
            return Amt;
        }
    }
}
