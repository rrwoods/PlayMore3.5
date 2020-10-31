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

        // this is probably only needed by tests?  might get rid of it once i don't need to programmatically toggle
        // controllable-toggles anymore.
        public Effect GetEffect(string name) {
            return Effects[name];
        }

        public IEnumerable<Modifier> GetModifiers(Statistic affecting) {
            return Effects.Values
                .SelectMany(x => x.GetModifiers(this, affecting));
        }

        public IEnumerable<Multiplier> GetMultipliers(Statistic affecting) {
            return Effects.Values
                .Select(x => x.GetMultiplier(this, affecting))
                .Where(x => x != null);
        }

        public IEnumerable<DiceGroup> GetExtraDice(DiceStatistic affecting) {
            return Effects.Values
                    .Select(x => x.GetExtraDice(this, affecting))
                    .Where(x => x != null);
        }

        public bool HasFlag(string flag) {
            return Effects.Values
                .Any(x => (x.Flag == flag) && (x.Toggle.On(this)));
        }
    }
}
