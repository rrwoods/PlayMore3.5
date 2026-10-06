using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PlayMore35.Effects;
using PlayMore35.Items;

namespace PlayMore35.Items {
    public class AttackRoutinePreferences : IJsonable {
        public Dictionary<RoutineCategory, bool> CategoryVisibility { get; } = new Dictionary<RoutineCategory, bool>();

        public AttackRoutinePreferences() { }

        public AttackRoutinePreferences(JObject json) {
            if (json != null) {
                foreach (var prop in json.Properties()) {
                    if (Enum.TryParse<RoutineCategory>(prop.Name, out var cat)) {
                        CategoryVisibility[cat] = (bool)prop.Value;
                    }
                }
            }
        }

        public bool IsCategoryVisible(RoutineCategory category) {
            if (CategoryVisibility.TryGetValue(category, out bool visible)) {
                return visible;
            }
            return GetDefaultVisibility(category);
        }

        public void SetCategoryVisible(RoutineCategory category, bool visible) {
            CategoryVisibility[category] = visible;
        }

        public static bool GetDefaultVisibility(RoutineCategory category) {
            switch (category) {
                case RoutineCategory.PrimarySolo:
                case RoutineCategory.TwoWeaponFighting:
                case RoutineCategory.DoubleWeapon:
                case RoutineCategory.ArmorSpikesSolo:
                    return true;
                case RoutineCategory.OffHandShieldBash:
                case RoutineCategory.ArmorSpikesTWF:
                    return false;
                default:
                    return true;
            }
        }

        public JToken ToJson() {
            var obj = new JObject();
            foreach (var kvp in CategoryVisibility) {
                obj[kvp.Key.ToString()] = kvp.Value;
            }
            return obj;
        }
    }
}
