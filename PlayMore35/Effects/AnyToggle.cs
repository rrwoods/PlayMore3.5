using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace PlayMore35.Effects {
    // A toggle that is active if one of its child toggles are active.
    class AnyToggle : IToggle {
        private readonly List<IToggle> toggles = new List<IToggle>();

        public JToken ToJson() {
            return new JObject(new JProperty("any", new JArray(toggles.Select(x => x.ToJson()))));
        }

        public AnyToggle(JArray array) {
            toggles.AddRange(array.Select(x => Toggle.FromJson(x)));
        }

        public AnyToggle(params IToggle[] toggles) {
            this.toggles.AddRange(toggles);
        }

        public bool On(EffectPool pool, Items.Equipped items) {
            return toggles.Any(x => x.On(pool, items));
        }
    }
}
