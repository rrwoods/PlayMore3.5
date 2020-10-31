using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static System.Math;
using static PlayMore35.GameRules.Constants;

namespace PlayMore35.Effects {
    public class Statistic {
        private static readonly string[] stackingTypes = { BASE, CIRCUMSTANCE, DODGE, RACIAL, UNTYPED };
        private static readonly string[] typeOrder = {
            //BASE,
            SIZE,
            RACIAL,
            NATURAL_ARMOR,
            ARMOR,
            SHIELD,
            DODGE,
            DEFLECTION,
            ENHANCEMENT,
            RESISTANCE,
            INSIGHT,
            COMPETENCE,
            LUCK,
            MORALE,
            SACRED,
            PROFANE,
            ALCHEMICAL,
            CIRCUMSTANCE,
            //UNTYPED,
        };

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

        private string ExplainBase(int mod, string name) {
            return ((mod < 0) ? ("" + mod) : ("+" + mod)) + " " + name;
        }

        private string Explain(int mod, string name, string type, IEnumerable<string> overlaps) {
            string baseText = ExplainBase(mod, name);
            if (overlaps.Any()) {
                return baseText + " (" + type + "; overlaps with " + String.Join(", ", overlaps) + ")";
            }
            return baseText + " (" + type + ")";
        }

        public (int value, List<string> explanation) GetValue(EffectPool pool) {
            decimal total = 0;
            
            Dictionary<string, int> penalties = new Dictionary<string, int>();
            Dictionary<string, int> bonuses = new Dictionary<string, int>();

            var stackingExpl = new Dictionary<string, List<(int mod, string name)>>();
            var bonusExpl   = new Dictionary<string, (int mod, string name, List<string> overlaps)>();
            var penaltyExpl = new Dictionary<string, (int mod, string name, List<string> overlaps)>();
            
            IEnumerable<Modifier> mods = pool.GetModifiers(this);
            foreach (Modifier mod in mods) {
                int amount = mod.ComputeAmount(pool);
                string type = mod.Type;
                if (amount == 0) {
                    if (mod.Type == BASE) {
                        if (!stackingExpl.ContainsKey(BASE)) {
                            stackingExpl[BASE] = new List<(int mod, string name)>();
                        }
                        stackingExpl[BASE].Add((amount, mod.Name));
                    }
                    continue;
                }

                if (ignoredModifierTypes.ContainsKey(type) && ignoredModifierTypes[type].On(pool)) {
                    continue;
                }

                if (stackingTypes.Contains(type)) {
                    total += amount;
                    if (!stackingExpl.ContainsKey(type)) {
                        stackingExpl[type] = new List<(int mod, string name)>();
                    }
                    stackingExpl[type].Add((amount, mod.Name));
                    continue;
                }

                Dictionary<string, int> modsDict = (amount < 0) ? penalties : bonuses;
                var explDict = (amount < 0) ? penaltyExpl : bonusExpl;
                
                if (modsDict.ContainsKey(type)) {
                    List<string> overlaps = explDict[type].overlaps;
                    int previousAmount = modsDict[type];
                    if (Abs(amount) > Abs(previousAmount)) {
                        // this amount supersedes the previous, so
                        // use the new amount,
                        modsDict[type] = amount;
                        // use the new mod name,
                        overlaps.Add(explDict[type].name);
                        // and mark the previous mod name as being overlapped
                        explDict[type] = (amount, mod.Name, overlaps);
                    } else {
                        // the previous amount supersedes this one, so
                        // keep the old amount and mod name,
                        // and mark this mod name as being overlapped
                        overlaps.Add(mod.Name);
                    }
                } else {
                    modsDict[type] = amount;
                    explDict[type] = (amount, mod.Name, new List<string>());
                }
            }

            total += penalties.Values.Sum();
            total += bonuses.Values.Sum();

            List<string> multiplierExpl = new List<string>();
            foreach (Multiplier multiplier in pool.GetMultipliers(this)) {
                total *= multiplier.Factor;
                multiplierExpl.Add("x" + multiplier.Factor + " " + multiplier.Name);
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

            List<string> explanation = new List<string>();
            if (stackingExpl.ContainsKey(BASE)) {
                foreach (var (mod, name) in stackingExpl[BASE]) {
                    explanation.Add(ExplainBase(mod, name));
                }
            }
            foreach (string type in typeOrder) {
                if (stackingExpl.ContainsKey(type)) {
                    foreach (var (mod, name) in stackingExpl[type]) {
                        explanation.Add(Explain(mod, name, type, Enumerable.Empty<string>()));
                    }
                } else {
                    if (bonusExpl.ContainsKey(type)) {
                        var (mod, name, overlaps) = bonusExpl[type];
                        explanation.Add(Explain(mod, name, type, overlaps));
                    }
                    if (penaltyExpl.ContainsKey(type)) {
                        var (mod, name, overlaps) = penaltyExpl[type];
                        explanation.Add(Explain(mod, name, type, overlaps));
                    }
                }
            }
            // TODO : handle bonus/penalty types that weren't in the ordering list
            if (stackingExpl.ContainsKey(UNTYPED)) {
                foreach (var (mod, name) in stackingExpl[UNTYPED]) {
                    explanation.Add(ExplainBase(mod, name));
                }
            }
            explanation.AddRange(multiplierExpl);

            return (roundedTotal, explanation);
        }
    }
}
