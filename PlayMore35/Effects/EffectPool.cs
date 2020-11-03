using System;
using System.Collections.Generic;
using System.Linq;

using static PlayMore35.GameRules.Constants;

namespace PlayMore35.Effects {
    public class EffectPool {
        private readonly List<Effect> Effects = new List<Effect>();
        public Items.Weapon PrimaryHandWeapon { get; private set; } = null;
        public Items.Weapon OffHandWeapon { get; private set; } = null;

        public EffectPool() {

        }

        public void AddEffect(Effect effect) {
            Effects.Add(effect);
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
            IEnumerable<Modifier> mods = Effects
                .SelectMany(x => x.GetModifiers(this, affecting));
            if (PrimaryHandWeapon != null) {
                mods = mods.Concat(PrimaryHandWeapon.GetGlobalModifiers(this, affecting));
            }
            if (OffHandWeapon != null && OffHandWeapon != PrimaryHandWeapon) {
                mods = mods.Concat(OffHandWeapon.GetGlobalModifiers(this, affecting));
            }
            return mods;
        }

        public IEnumerable<Modifier> GetAttackModifiers(Items.Attack affecting) {
            return Effects
                .SelectMany(x => x.GetAttackModifiers(this, affecting));
        }

        public IEnumerable<Modifier> GetDamageModifiers(Items.Attack affecting) {
            return Effects
                .SelectMany(x => x.GetDamageModifiers(this, affecting));
        }

        public IEnumerable<Multiplier> GetMultipliers(Statistic affecting) {
            return Effects
                .SelectMany(x => x.GetMultipliers(this, affecting));
        }

        public IEnumerable<DiceGroup> GetExtraDice(Items.Attack affecting) {
            return Effects
                    .SelectMany(x => x.GetExtraDice(this, affecting));
        }

        public Dictionary<string, List<string>> GetTextEffects() {
            var allText = TEXT_CATEGORIES.ToDictionary(x => x, x => new List<string>());

            foreach (Effect e in Effects.Where(x => x.Toggle.On(this))) {
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
            return Effects
                .Any(x => x.HasFlag(flag) && x.Toggle.On(this));
        }
    }
}
