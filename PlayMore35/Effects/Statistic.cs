using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static System.Math;
using static PlayMore35.GameRules.Constants;

namespace PlayMore35.Effects {
    public class Statistic {
        public string Name { get; private set; }
        private readonly List<string> tags = new List<string>();
        private int? minValue = null;
        private int? maxValue = null;
        private int precision = 1;
        private readonly Dictionary<string, IToggle> ignoredModifierTypes = new Dictionary<string, IToggle>();

        public Statistic(string name) {
            Name = name;
            AddTag(name);
        }

        // For things like "an Initiative check is a Dexterity check",
        // or setting up Dexterity checks themselves to be the Dexterity modifier plus some stuff.
        public Statistic(string name, Statistic basedOn) 
            : this(name) {
            tags.AddRange(basedOn.tags);
            foreach (var ignore in basedOn.ignoredModifierTypes) {
                ignoredModifierTypes.Add(ignore.Key, ignore.Value);
            }
        }

        public Statistic AddTag(string tag) {
            tags.Add(tag);
            return this;
        }

        public bool HasTag(string tag) {
            return tags.Contains(tag);
        }

        public Statistic Minimum(int minimum) {
            minValue = minimum;
            return this;
        }

        public Statistic Maximum(int maximum) {
            maxValue = maximum;
            return this;
        }

        public Statistic RoundTo(int precision) {
            this.precision = precision;
            return this;
        }

        public Statistic IgnoreModifierType(string type) {
            ignoredModifierTypes.Add(type, AlwaysOn.Instance);
            return this;
        }

        public Statistic IgnoreModiferType(string type, IToggle when) {
            ignoredModifierTypes.Add(type, when);
            return this;
        }

        public (int value, List<string> explanation) GetValue(EffectPool pool) {
            IEnumerable<Modifier> mods = pool.GetModifiers(this)
                .Where(x => !ignoredModifierTypes.ContainsKey(x.Type) || !ignoredModifierTypes[x.Type].On(pool));

            (decimal total, List<string> explanation) = StackingRules.StackModifiers(mods, pool);

            List<string> multiplierExpl = new List<string>();
            foreach (Multiplier multiplier in pool.GetMultipliers(this)) {
                total *= multiplier.Factor;
                multiplierExpl.Add("×" + multiplier.Factor + " " + multiplier.Name);
            }

            // As of this writing the only thing we use this precision value on is speed, which always rounds *up* to
            // the nearest 5 feet.
            int roundedTotal = (int) (Ceiling(decimal.Divide(total, precision)) * precision);

            if (minValue != null && roundedTotal < minValue) {
                roundedTotal = (int)minValue;
            }
            if (maxValue != null && roundedTotal > maxValue) {
                roundedTotal = (int)maxValue;
            }

            
            explanation.AddRange(multiplierExpl);

            return (roundedTotal, explanation);
        }
    }
}
