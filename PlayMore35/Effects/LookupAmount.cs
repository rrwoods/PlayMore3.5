using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace PlayMore35.Effects {
    public class LookupAmount : IAmount {
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

        public LookupAmount(
            IAmount value,
            IDictionary<int, int>? table = null,
            int missLookupAdjust = 0,
            int missValueFactor = 0) {
            Value = value ?? throw new System.ArgumentNullException(nameof(value));
            Table = table != null ? new Dictionary<int, int>(table) : new Dictionary<int, int>();
            MissLookupAdjust = missLookupAdjust;
            MissValueFactor = missValueFactor;
        }

        public LookupAmount(JObject json)
            : this(
                Amount.FromJson(json["value"]!)!,
                ParseTable(json["table"]),
                json["miss adjust"] != null ? (int)json["miss adjust"]! : 0,
                json["miss factor"] != null ? (int)json["miss factor"]! : 0) {
        }

        private static Dictionary<int, int>? ParseTable(JToken? token) {
            if (token == null) return null;
            var table = new Dictionary<int, int>();
            foreach (JProperty p in token) {
                table[int.Parse(p.Name)] = (int)p.Value;
            }
            return table;
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
