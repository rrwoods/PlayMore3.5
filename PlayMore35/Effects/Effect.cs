using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Effects {
    public class Effect {
        public Effect(string name) {
            Name = name;
        }

        public string Name { get; private set; }

        public IToggle Toggle = AlwaysOn.Instance;
        private readonly List<Modifier> Modifiers = new List<Modifier>();

        public Effect AddModifier(Modifier mod) {
            Modifiers.Add(mod);
            mod.Name = Name;
            return this; // method chaining
        }

        public Effect SetToggle(IToggle toggle) {
            Toggle = toggle;
            return this;
        }

        public Effect Controllable() {
            Toggle = new ControllableToggle(Name);
            return this;
        }

        public IEnumerable<Modifier> GetModifiers(EffectPool pool, Statistic affecting) {
            // In order to prevent infinite recursion, we have to check whether we care about what this effect
            // does before checking whether it applies.
            IEnumerable<Modifier> potential = Modifiers.Where(x => affecting.HasTag(x.Affects));
            if (!potential.Any()) {
                return potential;
            }

            return Toggle.On(pool)
                ? potential
                : Enumerable.Empty<Modifier>();
        }
    }
}
