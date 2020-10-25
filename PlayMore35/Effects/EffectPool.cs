using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace PlayMore35.Effects {
    public class EffectPool {
        public EffectPool() {

        }

        private readonly Dictionary<string, Effect> Effects = new Dictionary<string, Effect>();

        public void AddEffect(Effect effect) {
            Effects[effect.Name] = effect;
        }

        public void SetValue(int value, string affects) {
            AddEffect(new Effect(affects).AddModifier(new Modifier("untyped", affects).AddAmount(value)));
        }

        public IEnumerable<Modifier> GetModifiers(Statistic affecting) {
            return Effects.Values
                .SelectMany(x => x.GetModifiers(this, affecting));
        }

        public bool EffectActive(string name) {
            if (!Effects.ContainsKey(name)) {
                return false;
            }
            return Effects[name].Toggle.On(this);
        }
    }
}
