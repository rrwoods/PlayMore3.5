using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using PlayMore35.Items;

using static PlayMore35.Effects.Constants;

namespace PlayMore35.Effects {
    public class ItemAmount : IAmount {
        private readonly string Key;

        public ItemAmount(string key) {
            Key = key;
        }

        private int TotalWeight(Equipped items) {
            return (int)Math.Ceiling(items.UnequippedWeight +
                (items.PrimaryHandWeapon?.Weight ?? 0) +
                (items.PrimaryHandWeapon == items.OffHandWeapon ? 0 : items.OffHandWeapon?.Weight ?? 0) +
                (items.DonnedArmor?.Weight ?? 0) +
                (items.OffHandShield?.Weight ?? 0)
            );
        }

        private int MaxDexFromItems(EffectPool pool, Equipped items) {
            int totalWeight = TotalWeight(items);
            if (totalWeight > pool.stats[MEDIUM_LOAD].GetValue(pool, items).value) {
                return +1;
            }
            if (totalWeight > pool.stats[LIGHT_LOAD].GetValue(pool, items).value) {
                return +3;
            }
            return int.MaxValue;
        }

        public int Get(EffectPool pool, Equipped items) {
            if (Key == ARMOR_BONUS) {
                return items.DonnedArmor?.ArmorBonus ?? +0;
            }
            if (Key == SHIELD_BONUS) {
                return items.OffHandShield?.ShieldBonus ?? +0;
            }
            if (Key == MAX_DEX_BONUS) {
                // c#'s min doesn't have a 3-number overload.  i'll write one myself if this happens a lot.
                return Math.Min(
                    Math.Min(
                        items.DonnedArmor?.MaxDexBonus ?? int.MaxValue,
                        items.OffHandShield?.MaxDexBonus ?? int.MaxValue
                    ),
                    MaxDexFromItems(pool, items)
                );
            }
            if (Key == ARMOR_CHECK_PENALTY) {
                if (items.DonnedArmor == null) {
                    return 0;
                }
                return items.DonnedArmor.ArmorCheckPenalty + StackingRules.StackModifiers(
                    items.DonnedArmor.Properties.Select(x => x.AcpModifier).Where(x => x != null),
                    pool,
                    items
                ).mod;
            }
            if (Key == SHIELD_ARMOR_CHECK_PENALTY) {
                if (items.OffHandShield == null) {
                    return 0;
                }
                return items.OffHandShield.ArmorCheckPenalty + StackingRules.StackModifiers(
                    items.OffHandShield.Properties.Select(x => x.AcpModifier).Where(x => x != null),
                    pool,
                    items
                ).mod;
            }
            if (Key == ARMOR_WEIGHT) {
                return (int?)items.DonnedArmor?.Type ?? 0;
            }
            if (Key == TOTAL_WEIGHT) {
                return TotalWeight(items);
            }
            return 0;
        }

        public JToken ToJson() {
            return Key;
        }
    }
}
