using Newtonsoft.Json.Linq;

namespace PlayMore35.Effects {
    public class FlagToggle : IToggle {
        private readonly string flag;

        public JToken ToJson() {
            return new JObject(new JProperty("flag", flag));
        }

        public FlagToggle(string flag) {
            this.flag = flag;
        }

        public bool On(EffectPool pool, Items.Equipped items) {
            return pool.HasFlag(flag, items);
        }

        public IToggle ApplyChoice(string choice) {
            return this;
        }
    }
}
