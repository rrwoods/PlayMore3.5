using Newtonsoft.Json.Linq;

namespace PlayMore35.Effects {
    public class ControllableToggle : IToggle {
        public string Name { get; }
        public bool on = false;

        public ControllableToggle(string name) {
            Name = name;
        }

        public bool On(EffectPool pool, Items.Equipped items) {
            return on;
        }

        // Controllable toggles should only be added by the interface, in order to set flags.
        public JToken ToJson() {
            throw new System.NotImplementedException();
        }
    }
}
