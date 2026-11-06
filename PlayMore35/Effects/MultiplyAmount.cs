using System;
using Newtonsoft.Json.Linq;

namespace PlayMore35.Effects {
    public class MultiplyAmount : IAmount {
        private readonly IAmount Variable;
        private readonly decimal Scalar;

        public JToken ToJson() {
            return new JObject(new JProperty("multiply", new JObject(
                new JProperty("variable", Variable.ToJson()),
                new JProperty("scalar", Scalar)
            )));
        }

        public MultiplyAmount(JObject json) {
            Variable = Amount.FromJson(json["variable"]!)!;
            Scalar = (decimal)json["scalar"]!;
        }


        public int Get(EffectPool pool, Items.Equipped items) {
            return (int) Math.Floor(Scalar * Variable.Get(pool, items));
        }
    }
}
