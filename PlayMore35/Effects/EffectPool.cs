using System;
using System.Collections.Generic;
using System.Linq;

using static PlayMore35.GameRules.Constants;

namespace PlayMore35.Effects {
    public class EffectPool {
        public EffectPool() {

        }

        private readonly Dictionary<string, Effect> Effects = new Dictionary<string, Effect>();

        public void AddEffect(Effect effect) {
            Effects[effect.Name] = effect;
        }

        public void SetValue(int value, string affects) {
            AddEffect(new Effect("Test set value of " + affects).AddModifier(affects, new Modifier(value, BASE)));
        }

        public Effect GetEffect(string name) {
            return Effects[name];
        }

        public IEnumerable<Modifier> GetModifiers(Statistic affecting) {
            return Effects.Values
                .SelectMany(x => x.GetModifiers(this, affecting));
        }

        public IEnumerable<Modifier> GetAttackModifiers(Items.Attack affecting) {
            return Effects.Values
                .SelectMany(x => x.GetAttackModifiers(this, affecting));
        }

        public IEnumerable<Modifier> GetDamageModifiers(Items.Attack affecting) {
            return Effects.Values
                .SelectMany(x => x.GetDamageModifiers(this, affecting));
        }

        public IEnumerable<Multiplier> GetMultipliers(Statistic affecting) {
            return Effects.Values
                .Select(x => x.GetMultiplier(this, affecting))
                .Where(x => x != null);
        }

        public IEnumerable<DiceGroup> GetExtraDice(Items.Attack affecting) {
            return Effects.Values
                    .Select(x => x.GetExtraDice(this, affecting))
                    .Where(x => x != null);
        }

        public Dictionary<string, List<string>> GetTextEffects() {
            var allText = TEXT_CATEGORIES.ToDictionary(x => x, x => new List<string>());

            foreach (Effect e in Effects.Values.Where(x => x.Toggle.On(this))) {
                foreach ((string category, string text) in e.Text) {
                    if (allText.ContainsKey(category)) {
                        allText[category].Add(text);
                    } else {
                        allText[UNCATEGORIZED].Add("[" + category + "] " + text);
                    }
                }
            }

            return allText;
        }

        public bool HasFlag(string flag) {
            return Effects.Values
                .Any(x => (x.Flag == flag) && x.Toggle.On(this));
        }
    }
}
