using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

using static PlayMore35.Effects.Constants;

namespace PlayMore35.Effects {
    public class EffectPool : IJsonable {
        private readonly Dictionary<string, Statistic> stats = new Dictionary<string, Statistic>();
        private readonly List<Effect> effects = new List<Effect>();

        // Weapons are not serialized or deserialized.  Serialization of the player's currently-equipped
        // weapons are (or will be) handled separately by the system that handles inventory.
        public Items.Weapon PrimaryHandWeapon { get; private set; } = null;
        public Items.Weapon OffHandWeapon { get; private set; } = null;

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

        public void NoWeapons() {
            PrimaryHandWeapon = null;
            OffHandWeapon = null;
        }

        public void OneHandOneWeapon(Items.Weapon w) {
            PrimaryHandWeapon = w;
            OffHandWeapon = null;
        }

        public void TwoHandOneWeapon(Items.Weapon w) {
            PrimaryHandWeapon = w;
            OffHandWeapon = w;
        }

        public void TwoWeapons(Items.Weapon primary, Items.Weapon off) {
            PrimaryHandWeapon = primary;
            OffHandWeapon = off;
        }

        public IEnumerable<Modifier> GetModifiers(Statistic affecting) {
            IEnumerable<Modifier> mods = effects.SelectMany(x => x.GetModifiers(this, affecting));
            if (PrimaryHandWeapon != null) {
                mods = mods.Concat(PrimaryHandWeapon.GetGlobalModifiers(this, affecting));
            }
            if (OffHandWeapon != null && OffHandWeapon != PrimaryHandWeapon) {
                mods = mods.Concat(OffHandWeapon.GetGlobalModifiers(this, affecting));
            }
            return mods;
        }

        public IEnumerable<Modifier> GetAttackModifiers(Items.Attack affecting) {
            return effects.SelectMany(x => x.GetAttackModifiers(this, affecting));
        }

        public IEnumerable<Modifier> GetDamageModifiers(Items.Attack affecting) {
            return effects.SelectMany(x => x.GetDamageModifiers(this, affecting));
        }

        public IEnumerable<Multiplier> GetMultipliers(Statistic affecting) {
            return effects.SelectMany(x => x.GetMultipliers(this, affecting));
        }

        public IEnumerable<DiceGroup> GetExtraDice(Items.Attack affecting) {
            return effects.SelectMany(x => x.GetExtraDice(this, affecting));
        }

        public Dictionary<string, List<string>> GetTextEffects() {
            var allText = TEXT_CATEGORIES.ToDictionary(x => x, x => new List<string>());

            foreach (Effect e in effects.Where(x => x.Toggle.On(this))) {
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
            return effects
                .Any(x => x.HasFlag(flag) && x.Toggle.On(this));
        }
    }
}
