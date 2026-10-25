#nullable enable
using Newtonsoft.Json.Linq;

namespace PlayMore35.Effects {
    public class ControllableAmount : IAmount {
        public readonly string Name;

        public JToken ToJson() {
            return new JObject(new JProperty("controllable", Name));
        }

        public ControllableAmount(string name) {
            Name = name;
        }

        public int Get(EffectPool pool, Items.Equipped items) {
            return pool.controllableParameters[Name].Value;
        }
    }
}
