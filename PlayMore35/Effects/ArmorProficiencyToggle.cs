using Newtonsoft.Json.Linq;
using PlayMore35.Items;

namespace PlayMore35.Effects {
    public class ArmorProficiencyToggle : IToggle {
        private readonly string Key;

        public ArmorProficiencyToggle(string armorOrShield) {
            Key = armorOrShield;
        }

        public bool On(EffectPool pool, Equipped items) {
            return Key.Equals("armor")
                ? pool.ProficientWithArmor(items)
                : pool.ProficientWithShield(items);
        }

        public JToken ToJson() {
            return new JObject("proficient", Key);
        }
    }
}
