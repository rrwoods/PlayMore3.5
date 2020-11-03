using System.Collections.Generic;
using System.Linq;

using PlayMore35.Items;

namespace PlayMore35.Effects {
    public class Effect {
        public Effect(string name) {
            Name = name;
        }

        public string Name { get; private set; }

        public IToggle Toggle = AlwaysOn.Instance;

        private readonly List<(Modifier mod, StatisticClass affects)> Modifiers = new List<(Modifier, StatisticClass)>();
        private readonly List<(Modifier mod, AttackClass affects)> AttackModifiers = new List<(Modifier, AttackClass)>();
        private readonly List<(Modifier mod, AttackClass affects)> DamageModifiers = new List<(Modifier, AttackClass)>();

        private readonly List<(Multiplier mod, StatisticClass affects)> Multipliers = new List<(Multiplier, StatisticClass)>();
        private readonly List<((int qty, int size) dice, AttackClass affects)> ExtraDice = new List<((int, int), AttackClass)>();
        private readonly List<string> Flags = new List<string>();
        public readonly List<(string category, string text)> Text = new List<(string, string)>();

        public Effect SetToggle(IToggle toggle) {
            Toggle = toggle;
            return this;
        }

        public Effect AddModifier(string affects, Modifier mod) {
            Modifiers.Add((mod, new StatisticClass(affects)));
            mod.Name = Name;
            return this;
        }

        public Effect AddAttackModifier(AttackClass affects, Modifier mod) {
            AttackModifiers.Add((mod, affects));
            mod.Name = Name;
            return this;
        }

        public Effect AddDamageModifier(AttackClass affects, Modifier mod) {
            DamageModifiers.Add((mod, affects));
            mod.Name = Name;
            return this;
        }

        public Effect AddMultiplier(string affects, Multiplier multiplier) {
            Multipliers.Add((multiplier, new StatisticClass(affects)));
            return this;
        }

        public Effect AddExtraDice(AttackClass affects, int qty, int size) {
            ExtraDice.Add(((qty, size), affects));
            return this;
        }

        public Effect AddFlag(string flag) {
            Flags.Add(flag);
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

        private IEnumerable<ModType> GetAlterers<ModType, SubjectType, SubjectSpecifier> (
            EffectPool pool,
            SubjectType affecting,
            List<(ModType mod, SubjectSpecifier affects)> alterers
        ) where SubjectSpecifier : ISubjectClass<SubjectType> {
            // In order to prevent infinite recursion, we have to check whether we care about what this effect
            // does before checking whether it applies.
            IEnumerable<ModType> potential = alterers
                .Where(x => x.affects.Matches(affecting))
                .Select(x => x.mod);
            if (!potential.Any()) {
                return potential;
            }

            return Toggle.On(pool)
                ? potential
                : Enumerable.Empty<ModType>();
        }


        public IEnumerable<Modifier> GetModifiers(EffectPool pool, Statistic affecting) {
            return GetAlterers(pool, affecting, Modifiers);
        }

        public IEnumerable<Modifier> GetAttackModifiers(EffectPool pool, Attack affecting) {
            return GetAlterers(pool, affecting, AttackModifiers);
        }

        public IEnumerable<Modifier> GetDamageModifiers(EffectPool pool, Attack affecting) {
            return GetAlterers(pool, affecting, DamageModifiers);
        }

        public IEnumerable<Multiplier> GetMultipliers(EffectPool pool, Statistic affecting) {
            return GetAlterers(pool, affecting, Multipliers);
        }

        public IEnumerable<DiceGroup> GetExtraDice(EffectPool pool, Attack affecting) {
            return GetAlterers(pool, affecting, ExtraDice)
                .Select(x => new DiceGroup(Name).AddDice(x.qty, x.size));
        }

        public bool HasFlag(string flag) {
            return Flags.Contains(flag);
        }
    }
}
