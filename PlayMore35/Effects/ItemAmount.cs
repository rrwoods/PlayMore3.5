using System;
using Newtonsoft.Json.Linq;
using PlayMore35.Items;

using static PlayMore35.Effects.Constants;

namespace PlayMore35.Effects {
    public class ItemAmount : IAmount {
        private readonly string Key;

        public ItemAmount(string key) {
            Key = key;
        }

        public int Get(EffectPool pool, Equipped items) {
            if (Key == ARMOR_BONUS) {
                return items.DonnedArmor?.ArmorBonus ?? +0;
            }
            if (Key == SHIELD_BONUS) {
                return items.OffHandShield?.ShieldBonus ?? +0;
            }
            if (Key == MAX_DEX_BONUS) {
                return Math.Min(
                    items.DonnedArmor?.MaxDexBonus ?? int.MaxValue,
                    items.OffHandShield?.MaxDexBonus ?? int.MaxValue
                );
            }
            if (Key == ARMOR_CHECK_PENALTY) {
                return items.DonnedArmor?.ArmorCheckPenalty ?? 0;
            }
            if (Key == SHIELD_ARMOR_CHECK_PENALTY) {
                return items.OffHandShield?.ArmorCheckPenalty ?? 0;
            }
            if (Key == ARMOR_WEIGHT) {
                return (int?)items.DonnedArmor?.Type ?? 0;
            }
            if (Key == TOTAL_WEIGHT) {
                return (int) Math.Ceiling(items.UnequippedWeight +
                    (items.PrimaryHandWeapon?.Weight ?? 0) +
                    (items.PrimaryHandWeapon == items.OffHandWeapon ? 0 : items.OffHandWeapon?.Weight ?? 0) +
                    (items.DonnedArmor?.Weight ?? 0) +
                    (items.OffHandShield?.Weight ?? 0)
                );
            }
            return 0;
        }

        public JToken ToJson() {
            return Key;
        }
    }
}
