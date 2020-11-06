using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            return 0;
        }

        public JToken ToJson() {
            return Key;
        }
    }
}
