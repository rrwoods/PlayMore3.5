using Newtonsoft.Json.Linq;

namespace PlayMore35.Effects {
    public class ControllableToggle : IToggle {
        public string Name { get; }
        public bool on = false;

        public JToken ToJson() {
            return new JObject(new JProperty("controllable", Name));
        }

        public ControllableToggle(string name) {
            Name = name;
        }

        public bool On(EffectPool pool) {
            return on;
        }
    }
}
