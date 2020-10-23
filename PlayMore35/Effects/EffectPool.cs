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
            AddEffect(new Effect(affects).AddPart(new EffectPart("untyped", affects).AddAmount(value)));
        }

        public IEnumerable<EffectPart> GetEffects(Statistic affecting) {
            return Effects.Values
                .SelectMany(x => x.EffectParts)
                .Where(x => affecting.HasTag(x.Affects));
        }
    }
}
