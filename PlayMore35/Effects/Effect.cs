using System.Collections.Generic;
using System.Linq;

namespace PlayMore35.Effects {
    public class Effect {
        public Effect(string name) {
            Name = name;
        }

        public string Name { get; private set; }

        public IToggle Toggle = AlwaysOn.Instance;
        private readonly List<(Modifier mod, string affects)> Modifiers = new List<(Modifier, string)>();
        private readonly List<(Modifier mod, WeaponClass affects)> AttackModifiers = new List<(Modifier, WeaponClass)>();
        private readonly List<(Modifier mod, WeaponClass affects)> DamageModifiers = new List<(Modifier, WeaponClass)>();
        public Multiplier Factor = null;
        public ExtraDice Dice = null;
        public string Flag = null;
        public readonly List<(string category, string text)> Text = new List<(string, string)>();

        public Effect SetToggle(IToggle toggle) {
            Toggle = toggle;
            return this;
        }

        public Effect AddModifier(string affects, Modifier mod) {
            Modifiers.Add((mod, affects));
            mod.Name = Name;
            return this;
        }

        public Effect AddAttackModifier(WeaponClass affects, Modifier mod) {
            AttackModifiers.Add((mod, affects));
            mod.Name = Name;
            return this;
        }

        public Effect AddDamageModifier(WeaponClass affects, Modifier mod) {
            DamageModifiers.Add((mod, affects));
            mod.Name = Name;
            return this;
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

        // TODO : these three functions are identical, except that they pull from different collections
        // and use a different matching check.  Can they be combined, with some <T> cleverness maybe?

        public IEnumerable<Modifier> GetModifiers(EffectPool pool, Statistic affecting) {
            // In order to prevent infinite recursion, we have to check whether we care about what this effect
            // does before checking whether it applies.
            IEnumerable<Modifier> potential = Modifiers
                .Where(x => affecting.HasTag(x.affects))
                .Select(x => x.mod);
            if (!potential.Any()) {
                return potential;
            }

            return Toggle.On(pool)
                ? potential
                : Enumerable.Empty<Modifier>();
        }

        public IEnumerable<Modifier> GetAttackModifiers(EffectPool pool, Items.Attack affecting) {
            IEnumerable<Modifier> potential = AttackModifiers
                .Where(x => x.affects.Matches(affecting))
                .Select(x => x.mod);
            if (!potential.Any()) {
                return potential;
            }

            return Toggle.On(pool)
                ? potential
                : Enumerable.Empty<Modifier>();
        }

        public IEnumerable<Modifier> GetDamageModifiers(EffectPool pool, Items.Attack affecting) {
            IEnumerable<Modifier> potential = DamageModifiers
                .Where(x => x.affects.Matches(affecting))
                .Select(x => x.mod);
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

        public DiceGroup GetExtraDice(EffectPool pool, Items.Attack affecting) {
            if (Dice == null) {
                return null;
            }
            if (Dice.Affects.Matches(affecting) && Toggle.On(pool)) {
                Dice.Dice.Name = Name;
                return Dice.Dice;
            }
            return null;
        }
    }
}
