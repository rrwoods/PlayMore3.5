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

        private int MaxDexFromItems(EffectPool pool, Equipped items) {
            decimal totalWeight = items.TotalWeight();
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
                if (items.DonnedArmor == null) {
                    return 0;
                }
                return items.DonnedArmor.ArmorBonus + StackingRules.StackModifiers(
                    items.DonnedArmor.Properties.Select(x => x.AcModifier),
                    pool,
                    items
                ).mod;
            }
            if (Key == SHIELD_BONUS) {
                if (items.OffHandShield == null) {
                    return 0;
                }
                return items.OffHandShield.ShieldBonus + StackingRules.StackModifiers(
                    items.OffHandShield.Properties.Select(x => x.AcModifier),
                    pool,
                    items
                ).mod;
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
                return (int)Math.Ceiling(items.TotalWeight());
            }
            return 0;
        }

        public JToken ToJson() {
            return Key;
        }
    }
}
