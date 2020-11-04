using Newtonsoft.Json.Linq;

namespace PlayMore35.Effects {
    public class VariableAmount : IAmount {
        private readonly string Stat;

        public VariableAmount(string stat) {
            Stat = stat;
        }

        public JToken ToJson() {
            return new JObject(new JProperty("statValue", Stat));
        }

        public int Get(EffectPool pool) {
            return pool.GetStat(Stat).GetValue(pool).value;
        }
    }
}
