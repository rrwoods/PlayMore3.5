using Newtonsoft.Json.Linq;

namespace PlayMore35.Effects {
    public class FixedAmount : IAmount {
        public int Amt { get; private set; }

        public JToken ToJson() {
            return new JObject(new JProperty("fixed", Amt));
        }

        public FixedAmount(int amount) {
            Amt = amount;
        }

        public int Get(EffectPool pool) {
            return Amt;
        }
    }
}
