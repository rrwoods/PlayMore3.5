using Newtonsoft.Json.Linq;

namespace PlayMore35.Effects {
    class UnToggle : IToggle {
        private readonly IToggle basis;

        public JToken ToJson() {
            return new JObject(new JProperty("not", basis.ToJson()));
        }

        public UnToggle(IToggle basis) {
            this.basis = basis;
        }

        public bool On(EffectPool pool) {
            return !basis.On(pool);
        }
    }
}
