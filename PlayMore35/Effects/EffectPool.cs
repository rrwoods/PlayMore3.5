using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using PlayMore35.Items;

using static PlayMore35.Effects.Constants;

namespace PlayMore35.Effects {
    public class EffectPool : IJsonable {
        private readonly Dictionary<string, Statistic> stats = new Dictionary<string, Statistic>();
        private readonly List<Effect> effects = new List<Effect>();

        public JToken ToJson() {
            return new JObject(
                new JProperty("stats", new JArray(stats.Values.Select(x => x.ToJson()))),
                new JProperty("effects", new JArray(effects.Select(x => x.ToJson())))
            );
        }

        public void ReadJson(JToken json) {
            foreach(JObject token in json["stats"]) {
                Statistic s = new Statistic(token);
                stats[s.Name] = s;
            }
            foreach(JObject token in json["effects"]) {
                effects.Add(new Effect(token));
            }
        }

        public void AddStat(Statistic stat) {
            stats[stat.Name] = stat;
        }

        public Statistic GetStat(string name) {
            return stats[name];
        }

        public void AddEffect(Effect effect) {
            effects.Add(effect);
        }

        public IEnumerable<Modifier> GetModifiers(Equipped items, Statistic affecting) {
            IEnumerable<Modifier> mods = effects.SelectMany(x => x.GetModifiers(this, items, affecting));
            if (items.PrimaryHandWeapon != null) {
                mods = mods.Concat(items.PrimaryHandWeapon.GetGlobalModifiers(this, items, affecting));
            }
            if (items.OffHandWeapon != null && items.OffHandWeapon != items.PrimaryHandWeapon) {
                mods = mods.Concat(items.OffHandWeapon.GetGlobalModifiers(this, items, affecting));
            }
            return mods;
        }

        public IEnumerable<Modifier> GetAttackModifiers(Equipped items, Attack affecting) {
            return effects.SelectMany(x => x.GetAttackModifiers(this, items, affecting));
        }

        public IEnumerable<Modifier> GetDamageModifiers(Equipped items, Attack affecting) {
            return effects.SelectMany(x => x.GetDamageModifiers(this, items, affecting));
        }

        public IEnumerable<Multiplier> GetMultipliers(Equipped items, Statistic affecting) {
            return effects.SelectMany(x => x.GetMultipliers(this, items, affecting));
        }

        public IEnumerable<DiceGroup> GetExtraDice(Equipped items, Attack affecting) {
            return effects.SelectMany(x => x.GetExtraDice(this, items, affecting));
        }

        public Dictionary<string, List<string>> GetTextEffects(Equipped items) {
            var allText = TEXT_CATEGORIES.ToDictionary(x => x, x => new List<string>());

            foreach (Effect e in effects.Where(x => x.Toggle.On(this, items))) {
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

        public bool HasFlag(string flag, Equipped items) {
            return effects
                .Any(x => x.HasFlag(flag) && x.Toggle.On(this, items));
        }
    }
}
