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
        public Multiplier Factor = null;
        public ExtraDice Dice = null;
        public string Flag = null;
        public readonly List<(string category, string text)> Text = new List<(string, string)>();

        public Effect SetToggle(IToggle toggle) {
            Toggle = toggle;
            return this;
        }

        public Effect AddModifier(Modifier mod) {
            Modifiers.Add(mod);
            mod.Name = Name;
            return this; // method chaining
        }

        // TODO : All of the "set" methods below this point really should be "add".
        // There's no reason an effect couldn't multiply multiple stats, or set multiple flags, etc.
        public Effect SetMultiplier(Multiplier multiplier) {
            Factor = multiplier;
            Factor.Name = Name;
            return this;
        }

        public Effect SetExtraDice(ExtraDice extraDice) {
            Dice = extraDice;
            return this;
        }

        public Effect SetFlag(string flag) {
            Flag = flag;
            return this;
        }

        public Effect AddText(string category, string text) {
            Text.Add((category, text));
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

        public Multiplier GetMultiplier(EffectPool pool, Statistic affecting) {
            if (Factor == null) {
                return null;
            }
            if (affecting.HasTag(Factor.Affects) && Toggle.On(pool)) {
                return Factor;
            }
            return null;
        }

        public DiceGroup GetExtraDice(EffectPool pool, DiceStatistic affecting) {
            if (Dice == null) {
                return null;
            }
            if (affecting.HasTag(Dice.Affects) && Toggle.On(pool)) {
                Dice.Dice.Name = Name;
                return Dice.Dice;
            }
            return null;
        }
    }
}
