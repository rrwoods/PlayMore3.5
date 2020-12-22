using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

using static System.Math;

namespace PlayMore35.Effects {
    public class Statistic {
        public string Name { get; private set; }
        private readonly List<string> tags = new List<string>();
        private int? minValue = null;
        private int? maxValue = null;
        private int precision = 1;
        private readonly bool roundUp = true;
        private readonly Dictionary<string, IToggle> ignoredModifierTypes = new Dictionary<string, IToggle>();

        public JObject ToJson() {
            JObject json = new JObject(new JProperty("name", Name));
            json.AddList("tags", tags);
            if (minValue != null) {
                json.Add("min", minValue);
            }
            if (maxValue != null) {
                json.Add("max", maxValue);
            }
            if (precision != 1) {
                json.Add("precision", precision);
            }
            if (!roundUp) {
                json.Add("round down", true);
            }
            if (ignoredModifierTypes.Any()) {
                json.Add("ignore", new JObject(
                    ignoredModifierTypes.Select(x => new JProperty(x.Key, x.Value.ToJson()))
                ));
            }
            return json;
        }

        public Statistic(JObject token) {
            Name = (string)token["name"];
            tags.ReadJson("tags", token);
            if (token.ContainsKey("min")) {
                minValue = (int)token["min"];
            }
            if (token.ContainsKey("max")) {
                maxValue = (int)token["max"];
            }
            if (token.ContainsKey("precision")) {
                precision = (int)token["precision"];
            }
            roundUp = (token["round down"] == null);
            if (token.ContainsKey("ignore")) {
                foreach (JProperty item in token["ignore"]) {
                    ignoredModifierTypes[item.Name] = Toggle.FromJson(item.Value);
                }
            }
        }

        public Statistic(string name) {
            Name = name;
        }

        // For things like "an Initiative check is a Dexterity check",
        // or setting up Dexterity checks themselves to be the Dexterity modifier plus some stuff.
        public Statistic(string name, Statistic basedOn) 
            : this(name) {
            tags.Add(basedOn.Name);
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
            return Name.Equals(tag) || tags.Contains(tag);
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

        // Used to check whether a skill has any non-ability-modifier effects on it,
        // to determine whether to display it.
        public bool Relevant(EffectPool pool, Items.Equipped items) {
            return pool.GetModifiers(items, this)
                .Where(x => !ignoredModifierTypes.ContainsKey(x.Type) || !ignoredModifierTypes[x.Type].On(pool, items))
                .Where(x => x.ComputeAmount(pool, items) != 0)
                .Where(x => x.Type != Constants.ABILITY_MODIFIER)
                .Any();
        }

        public (int value, List<string> explanation) GetValue(EffectPool pool, Items.Equipped items) {
            IEnumerable<Modifier> mods = pool.GetModifiers(items, this)
                .Where(x => !ignoredModifierTypes.ContainsKey(x.Type) || !ignoredModifierTypes[x.Type].On(pool, items));

            (decimal total, List<string> explanation) = StackingRules.StackModifiers(mods, pool, items);

            List<string> multiplierExpl = new List<string>();
            foreach (Multiplier multiplier in pool.GetMultipliers(items, this)) {
                total *= multiplier.Factor;
                multiplierExpl.Add($"×{multiplier.Factor} {multiplier.Name}");
            }

            int roundedTotal = roundUp
                ? (int) (Ceiling(decimal.Divide(total, precision)) * precision)
                : (int)   (Floor(decimal.Divide(total, precision)) * precision);

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
