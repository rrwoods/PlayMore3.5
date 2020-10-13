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

        private readonly List<EffectPart> Effects = new List<EffectPart>();

        public void AddEffect(Effect effect) {
            this.Effects.AddRange(effect.EffectParts);
        }

        public IEnumerable<EffectPart> GetEffects(Statistic affecting) {
            return this.Effects.Where(x => affecting.HasTag(x.Affects));
        }
    }
}
