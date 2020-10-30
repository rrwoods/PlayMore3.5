using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static PlayMore35.GameRules.Constants;

namespace PlayMore35.Effects {
    public class Statistic {
        private static string[] stackingTypes = { BASE, CIRCUMSTANCE, DODGE, RACIAL, UNTYPED };

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
        public Statistic(string name, Statistic basedOn) {
            Name = name;
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

        public int GetValue(EffectPool pool) {
            decimal total = 0;
            Dictionary<string, int> penalties = new Dictionary<string, int>();
            Dictionary<string, int> bonuses = new Dictionary<string, int>();
            IEnumerable<Modifier> mods = pool.GetModifiers(this);
            foreach (Modifier mod in mods) {
                int amount = mod.ComputeAmount(pool);
                if (amount == 0) {
                    continue;
                }

                string type = mod.Type;
                if (ignoredModifierTypes.ContainsKey(type) && ignoredModifierTypes[type].On(pool)) {
                    continue;
                }

                if (stackingTypes.Contains(type)) {
                    total += amount;
                    continue;
                }

                Dictionary<string, int> dict = (amount < 0) ? penalties : bonuses;
                amount = Math.Abs(amount);

                dict.TryGetValue(type, out int current);
                dict[type] = Math.Max(current, amount);
            }

            total -= penalties.Values.Sum();
            total += bonuses.Values.Sum();

            total *= pool.GetMultipliers(this);

            // As of this writing the only thing we use this precision value on is speed, which always rounds *up* to
            // the nearest 5 feet.
            int roundedTotal = (int) (Math.Ceiling(decimal.Divide(total, precision)) * precision);

            if (minValue != null && roundedTotal < minValue) {
                roundedTotal = (int)minValue;
            }
            if (maxValue != null && roundedTotal > maxValue) {
                roundedTotal = (int)maxValue;
            }

            return roundedTotal;
        }
    }
}
