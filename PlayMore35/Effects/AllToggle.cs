using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace PlayMore35.Effects {
    // A toggle that is active if all its child toggles are active.
    class AllToggle : IToggle {
        private readonly List<IToggle> toggles = new List<IToggle>();

        public JToken ToJson() {
            return new JObject(new JProperty("all", new JArray(toggles.Select(x => x.ToJson()))));
        }

        public AllToggle(JArray array) {
            toggles.AddRange(array.Select(x => Toggle.FromJson(x)!));
        }

        public AllToggle(params IToggle[] toggles) {
            this.toggles.AddRange(toggles);
        }

        public bool On(EffectPool pool, Items.Equipped items) {
            return toggles.All(x => x.On(pool, items));
        }

        public IToggle ApplyChoice(string choice) {
            return new AllToggle(toggles.Select(x => x.ApplyChoice(choice)).ToArray());
        }
    }
}
