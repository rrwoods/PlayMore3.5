using Newtonsoft.Json.Linq;

namespace PlayMore35.Effects {
    public class VariableAmount : IAmount {
        private readonly string Stat;

        public VariableAmount(string stat) {
            Stat = stat;
        }

        public JToken ToJson() {
            return new JObject(new JProperty("stat value", Stat));
        }

        public int Get(EffectPool pool, Items.Equipped items) {
            return pool.GetStatValue(Stat, items, 0);
        }
    }
}
