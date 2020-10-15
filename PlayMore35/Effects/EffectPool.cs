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
            this.Effects[effect.Name] = effect;
        }

        public IEnumerable<EffectPart> GetEffects(Statistic affecting) {
            return this.Effects.Values
                .SelectMany(x => x.EffectParts)
                .Where(x => affecting.HasTag(x.Affects));
        }
    }
}
