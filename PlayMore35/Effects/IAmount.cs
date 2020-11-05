using Newtonsoft.Json.Linq;

namespace PlayMore35.Effects {
    public interface IAmount : IJsonable {
        int Get(EffectPool pool, Items.Equipped items);
    }

    public static class Amount {
        public static IAmount FromJson(JToken json) {
            if (json["controllable"] != null) {
                return new ControllableAmount((JObject)json["controllable"]);
            }
            if (json["fixed"] != null) {
                return new FixedAmount((int)json["fixed"]);
            }
            if (json["largest"] != null) {
                return new LargestAmount((JArray)json["largest"]);
            }
            if (json["lookup"] != null) {
                return new LookupAmount((JObject)json["lookup"]);
            }
            if (json["multiply"] != null) {
                return new MultiplyAmount((JObject)json["multiply"]);
            }
            if (json["smallest"] != null) {
                return new SmallestAmount((JArray)json["smallest"]);
            }
            if (json["statValue"] != null) {
                return new VariableAmount((string)json["statValue"]);
            }
            return null;
        }
    }
}
