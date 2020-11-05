using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace PlayMore35.Effects {
    class LookupAmount : IAmount {
        private readonly IAmount Value;
        private readonly Dictionary<int, int> Table;

        public JToken ToJson() {
            return new JObject(new JProperty("lookup", new JObject(
                new JProperty("value", Value.ToJson()),
                new JProperty("table", new JObject(Table.Select(x => new JProperty(x.Key.ToString(), x.Value))))
            )));
        }

        public LookupAmount(JObject json) {
            Value = Amount.FromJson(json["value"]);

            Table = new Dictionary<int, int>();
            foreach (JProperty p in json["table"]) {
                Table[int.Parse(p.Name)] = (int)p.Value;
            }
        }

        public LookupAmount(string basis, Dictionary<int, int> lookup) {
            Value = new VariableAmount(basis);
            Table = lookup;
        }

        public int Get(EffectPool pool, Items.Equipped items) {
            return Table[Value.Get(pool, items)];
        }
    }
}
