using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Items;

namespace PlayMore35.Tests {
    public static class TestFactory {
        public static Item CreateItem(
            Equipped.Slot slot,
            Effect? effect = null,
            decimal weight = 0,
            IEnumerable<FlagDefinition>? flags = null,
            IEnumerable<Statistic>? stats = null,
            string? name = null) {
            var itemEffect = effect ?? new Effect(name ?? "Test Item");
            var json = new JObject {
                ["slot"] = slot.ToString(),
                ["effect"] = itemEffect.ToJson(),
                ["weight"] = weight
            };
            if (flags != null) {
                json["flags"] = new JArray(flags.Select(f => f.ToJson()));
            }
            if (stats != null) {
                json["stats"] = new JArray(stats.Select(s => s.ToJson()));
            }
            return new Item(json);
        }

        public static CharacterClass CreateClass(
            string name,
            string prerequisites = "",
            int hitDie = 8,
            int skillPoints = 2,
            CharacterClass.BonusProgression bab = CharacterClass.BonusProgression.SLOW,
            CharacterClass.BonusProgression fort = CharacterClass.BonusProgression.SLOW,
            CharacterClass.BonusProgression refSave = CharacterClass.BonusProgression.SLOW,
            CharacterClass.BonusProgression will = CharacterClass.BonusProgression.SLOW,
            Dictionary<string, SubsystemFeature>? subsystems = null) {
            var json = new JObject {
                ["name"] = name,
                ["prerequisites"] = prerequisites,
                ["hit die"] = hitDie,
                ["class skills"] = new JArray(),
                ["skill points"] = skillPoints,
                ["base attack bonus"] = bab.ToString(),
                ["base fort save"] = fort.ToString(),
                ["base ref save"] = refSave.ToString(),
                ["base will save"] = will.ToString(),
                ["weapon proficiencies"] = new JArray(),
                ["armor proficiencies"] = new JArray(),
                ["shield proficiencies"] = new JArray(),
                ["exotic shield proficiencies"] = new JArray(),
                ["class features"] = new JArray()
            };
            if (subsystems != null && subsystems.Count > 0) {
                var subsObj = new JObject();
                foreach (var kvp in subsystems) {
                    var subJson = new JObject(
                        new JProperty("stats", new JArray(kvp.Value.Stats)),
                        new JProperty("effects", new JArray(kvp.Value.Effects.Select(x => x.ToJson())))
                    );
                    if (kvp.Value.Verbs != null && kvp.Value.Verbs.Count > 0) {
                        var verbsObj = new JObject();
                        foreach (var vKvp in kvp.Value.Verbs) {
                            verbsObj[vKvp.Key] = new JArray(vKvp.Value.Select(s => s.ToJson()));
                        }
                        subJson["verbs"] = verbsObj;
                    }
                    subsObj[kvp.Key] = subJson;
                }
                json["subsystems"] = subsObj;
            }
            return new CharacterClass(json);
        }

        public static Attack CreateAttack(
            string name,
            int bonus,
            DiceGroup baseDamage,
            IEnumerable<DiceGroup>? bonusDice = null,
            IEnumerable<DiceGroup>? criticalBonusDice = null,
            int criticalMultiplier = 2,
            int threatRange = 20,
            int rangeIncrement = 0) {
            var bDice = (bonusDice ?? Enumerable.Empty<DiceGroup>()).ToList();
            var cbDice = criticalBonusDice != null
                ? criticalBonusDice.ToList()
                : new List<DiceGroup>(bDice);

            var attack = (Attack)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Attack));
            var type = typeof(Attack);
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;

            type.GetField("WeaponName", flags)!.SetValue(attack, name);
            type.GetField("WeaponTags", flags)!.SetValue(attack, new List<string>());
            attack.AttackBonus = (bonus, new List<string> { $"+{bonus} base attack" });
            type.GetProperty("BaseDamage", flags)!.SetValue(attack, baseDamage);
            type.GetProperty("BonusDice", flags)!.SetValue(attack, bDice.AsReadOnly());
            type.GetProperty("CriticalBonusDice", flags)!.SetValue(attack, cbDice.AsReadOnly());
            type.GetProperty("CriticalMultiplier", flags)!.SetValue(attack, criticalMultiplier);
            type.GetProperty("ThreatRange", flags)!.SetValue(attack, threatRange);
            type.GetProperty("RangeIncrement", flags)!.SetValue(attack, rangeIncrement);
            type.GetProperty("DamageExplanation", flags)!.SetValue(attack, new List<string> { baseDamage?.ToString() ?? "" });
            type.GetProperty("CriticalDamageExplanation", flags)!.SetValue(attack, new List<string> { $"{baseDamage?.ToString()} ×{criticalMultiplier}" });

            return attack;
        }
    }
}
