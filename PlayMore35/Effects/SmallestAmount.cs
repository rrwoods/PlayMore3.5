#nullable enable
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace PlayMore35.Effects {
    public class SmallestAmount : IAmount {
        private List<IAmount> Amounts = new List<IAmount>();

        public JToken ToJson() {
            return new JObject(new JProperty("smallest", new JArray(Amounts.Select(x => x.ToJson()))));
        }

        public SmallestAmount(JArray array) {
            Amounts.AddRange(array.Select(x => Amount.FromJson(x)!));
        }

        public SmallestAmount(params IAmount[] amounts) {
            Amounts.AddRange(amounts);
        }

        public SmallestAmount(params string[] stats) {
            Amounts.AddRange(stats.Select(x => new VariableAmount(x)));
        }

        public int Get(EffectPool pool, Items.Equipped items) {
            return Amounts.Select(x => x.Get(pool, items)).Min();
        }
    }
}
