using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace PlayMore35.Effects {
    class LookupAmount : IAmount {
        private readonly IAmount Value;
        private readonly Dictionary<int, int> Table;

        private readonly int MissValueFactor = 0;
        private readonly int MissLookupAdjust = 0;

        public JToken ToJson() {
            JObject json = new JObject(new JProperty("lookup", new JObject(
                new JProperty("value", Value.ToJson()),
                new JProperty("table", new JObject(Table.Select(x => new JProperty(x.Key.ToString(), x.Value))))
            )));
            if (MissLookupAdjust != 0) {
                json["miss adjust"] = MissLookupAdjust;
                json["miss factor"] = MissValueFactor;
            }
            return json;
        }

        public LookupAmount(JObject json) {
            Value = Amount.FromJson(json["value"]);
            Table = new Dictionary<int, int>();
            foreach (JProperty p in json["table"]) {
                Table[int.Parse(p.Name)] = (int)p.Value;
            }

            if (json["miss adjust"] != null) {
                MissLookupAdjust = (int)json["miss adjust"];
                MissValueFactor = (int)json["miss factor"];
            }
        }

        public LookupAmount(string basis, Dictionary<int, int> lookup) {
            Value = new VariableAmount(basis);
            Table = lookup;
        }

        public int Get(EffectPool pool, Items.Equipped items) {
            int lookup = Value.Get(pool, items);
            int factor = 1;
            while (!Table.ContainsKey(lookup)) {
                lookup += MissLookupAdjust;
                factor *= MissValueFactor;
            }
            return Table[lookup] * factor;
        }
    }
}
