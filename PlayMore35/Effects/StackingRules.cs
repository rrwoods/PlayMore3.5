using System;
using System.Collections.Generic;
using System.Linq;

using static PlayMore35.GameRules.Constants;
using static System.Math;

namespace PlayMore35.Effects {
    public static class StackingRules {
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

        private static string ExplainBase(int mod, string name) {
            return ((mod < 0) ? ("" + mod) : ("+" + mod)) + " " + name;
        }

        private static string Explain(int mod, string name, string type, IEnumerable<string> overlaps) {
            string baseText = ExplainBase(mod, name);
            if (overlaps.Any()) {
                return baseText + " (" + type + "; overlaps with " + String.Join(", ", overlaps) + ")";
            }
            return baseText + " (" + type + ")";
        }

        public static (int mod, List<string> explanation) StackModifiers(IEnumerable<Modifier> mods, EffectPool pool) {
            int total = 0;

            Dictionary<string, int> penalties = new Dictionary<string, int>();
            Dictionary<string, int> bonuses = new Dictionary<string, int>();

            var stackingExpl = new Dictionary<string, List<(int mod, string name)>>();
            var bonusExpl = new Dictionary<string, (int mod, string name, List<string> overlaps)>();
            var penaltyExpl = new Dictionary<string, (int mod, string name, List<string> overlaps)>();

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

            return (total, explanation);
        }
    }
}
