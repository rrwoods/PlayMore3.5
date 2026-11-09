using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using PlayMore35.Effects;
using PlayMore35.Items;
using PlayMore35.Subsystems;

namespace PlayMore35.CharacterChoices {
    public class CharacterClass : IJsonable {
        public enum BonusProgression { NONE, SLOW, MEDIUM, FAST };

        public string Name = "";
        public string Prerequisites = "";
        public int HitDie = 4;
        public List<string> Tags = new List<string>();
        public List<string> ClassSkills = new List<string>();
        public int SkillPoints = 2;

        public BonusProgression BaseAttackBonus = BonusProgression.SLOW;
        public BonusProgression BaseFortSave = BonusProgression.SLOW;
        public BonusProgression BaseRefSave = BonusProgression.SLOW;
        public BonusProgression BaseWillSave = BonusProgression.SLOW;

        public List<AttackMatcher> WeaponProficiencies = new List<AttackMatcher>();
        public List<Armor.WeightCategory> ArmorProficiencies = new List<Armor.WeightCategory>();
        public List<bool> ShieldProficiencies = new List<bool>();
        // once i have ACFs, this will go away, because the only class that gets an exotic shield proficiency
        // gets it via an ACF.  as far as i know.
        public List<string> ExoticShieldProficiencies = new List<string>();

        // level 1 at position 0
        // within each level, a list of named class features, each represented as one effect
        public List<List<Effect>> ClassFeatures = new List<List<Effect>>();

        // within each level, a list of toggles required by that level's features
        public List<List<FlagDefinition>> Flags = new List<List<FlagDefinition>>();
        public List<Statistic> NewStats = new List<Statistic>();

        // { description => level }
        // examples:
        // "Fighter 1" => 1
        // "Endurance" => 3
        public Dictionary<string, int> BonusFeats = new Dictionary<string, int>();

        // S U B S Y S T E M S .
        public Dictionary<string, SubsystemFeature> Subsystems = new Dictionary<string, SubsystemFeature>();

        public JToken ToJson() {
            JObject json = new JObject(
                new JProperty("name", Name),
                new JProperty("prerequisites", Prerequisites),
                new JProperty("hit die", HitDie),
                new JProperty("class skills", new JArray(ClassSkills)),
                new JProperty("skill points", SkillPoints),
                new JProperty("base attack bonus", BaseAttackBonus),
                new JProperty("base fort save", BaseFortSave),
                new JProperty("base ref save", BaseRefSave),
                new JProperty("base will save", BaseWillSave),
                new JProperty("weapon proficiencies", new JArray(WeaponProficiencies)),
                new JProperty("armor proficiencies", new JArray(ArmorProficiencies)),
                new JProperty("shield proficiencies", new JArray(ShieldProficiencies.Select(x => x ? "tower" : "normal"))),
                new JProperty("exotic shield proficiencies", new JArray(ExoticShieldProficiencies)),
                new JProperty("class features", new JArray(ClassFeatures.Select(level =>
                    new JArray(level.Select(feature => new JObject(feature)))
                )))
            );
            if (Flags.Any()) {
                json["flags"] = new JArray(Flags.Select(level => new JArray(level.Select(flag => flag.ToJson()))));
            }
            if (Tags.Any()) {
                json["tags"] = new JArray(Tags);
            }
            json.AddList("stats", NewStats);
            if (BonusFeats.Any()) {
                json["bonus feats"] = new JObject(BonusFeats.Select(x => new JProperty(x.Key, x.Value)));
            }

            var subsObj = new JObject();
            foreach (var kvp in Subsystems) {
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
            if (subsObj.Count > 0) {
                json["subsystems"] = subsObj;
            }

            return json;
        }

        public CharacterClass(
            string name = "",
            string prerequisites = "",
            int hitDie = 4,
            int skillPoints = 2,
            BonusProgression baseAttackBonus = BonusProgression.SLOW,
            BonusProgression baseFortSave = BonusProgression.SLOW,
            BonusProgression baseRefSave = BonusProgression.SLOW,
            BonusProgression baseWillSave = BonusProgression.SLOW,
            IEnumerable<string>? classSkills = null,
            IEnumerable<AttackMatcher>? weaponProficiencies = null,
            IEnumerable<Armor.WeightCategory>? armorProficiencies = null,
            IEnumerable<bool>? shieldProficiencies = null,
            IEnumerable<string>? exoticShieldProficiencies = null,
            IEnumerable<IEnumerable<Effect>>? classFeatures = null,
            IEnumerable<IEnumerable<FlagDefinition>>? flags = null,
            IEnumerable<Statistic>? newStats = null,
            IEnumerable<string>? tags = null,
            IDictionary<string, int>? bonusFeats = null,
            IDictionary<string, SubsystemFeature>? subsystems = null) {
            Name = name ?? "";
            Prerequisites = prerequisites ?? "";
            HitDie = hitDie;
            SkillPoints = skillPoints;
            BaseAttackBonus = baseAttackBonus;
            BaseFortSave = baseFortSave;
            BaseRefSave = baseRefSave;
            BaseWillSave = baseWillSave;
            if (classSkills != null) ClassSkills.AddRange(classSkills);
            if (weaponProficiencies != null) WeaponProficiencies.AddRange(weaponProficiencies);
            if (armorProficiencies != null) ArmorProficiencies.AddRange(armorProficiencies);
            if (shieldProficiencies != null) ShieldProficiencies.AddRange(shieldProficiencies);
            if (exoticShieldProficiencies != null) ExoticShieldProficiencies.AddRange(exoticShieldProficiencies);
            if (classFeatures != null) {
                foreach (var lvl in classFeatures) {
                    ClassFeatures.Add(lvl != null ? lvl.ToList() : new List<Effect>());
                }
            }
            if (flags != null) {
                foreach (var lvl in flags) {
                    Flags.Add(lvl != null ? lvl.ToList() : new List<FlagDefinition>());
                }
            }
            if (newStats != null) NewStats.AddRange(newStats);
            if (tags != null) Tags.AddRange(tags);
            if (bonusFeats != null) {
                foreach (var kvp in bonusFeats) {
                    BonusFeats[kvp.Key] = kvp.Value;
                }
            }
            if (subsystems != null) {
                foreach (var kvp in subsystems) {
                    Subsystems[kvp.Key] = kvp.Value;
                }
            }
        }

        public CharacterClass(JObject json)
            : this(
                (string)json["name"],
                (string)json["prerequisites"],
                (int)json["hit die"],
                (int)json["skill points"],
                (BonusProgression)Enum.Parse(typeof(BonusProgression), (string)json["base attack bonus"], false),
                (BonusProgression)Enum.Parse(typeof(BonusProgression), (string)json["base fort save"], false),
                (BonusProgression)Enum.Parse(typeof(BonusProgression), (string)json["base ref save"], false),
                (BonusProgression)Enum.Parse(typeof(BonusProgression), (string)json["base will save"], false),
                json["class skills"]?.Select(x => (string)x),
                json["weapon proficiencies"]?.Select(x => new AttackMatcher((JObject)x)),
                json["armor proficiencies"]?.Select(x => (Armor.WeightCategory)Enum.Parse(typeof(Armor.WeightCategory), (string)x, false)),
                json["shield proficiencies"]?.Select(x => x.Equals("tower") || (string)x == "tower"),
                json["exotic shield proficiencies"]?.Select(x => (string)x),
                json["class features"]?.Select(level => level.Select(feature => new Effect((JObject)feature))),
                json["flags"]?.Select(level => level.Select(x => new FlagDefinition((JObject)x))),
                json["stats"]?.Select(x => new Statistic((JObject)x)),
                json["tags"]?.Select(x => (string)x!),
                ParseBonusFeats(json["bonus feats"]),
                ParseSubsystems(json["subsystems"] as JObject)) {
        }

        private static Dictionary<string, int>? ParseBonusFeats(JToken? token) {
            if (token == null) return null;
            var result = new Dictionary<string, int>();
            foreach (JProperty feat in token) {
                result[feat.Name] = (int)feat.Value;
            }
            return result;
        }

        private static Dictionary<string, SubsystemFeature>? ParseSubsystems(JObject? subsJson) {
            if (subsJson == null) return null;
            var result = new Dictionary<string, SubsystemFeature>();
            foreach (var prop in subsJson.Properties()) {
                if (prop.Value is JObject sObj) {
                    var feature = new SubsystemFeature {
                        Stats = sObj["stats"] != null ? sObj["stats"].Select(x => (string)x).ToList() : new List<string>(),
                        Effects = sObj["effects"] != null ? sObj["effects"].Select(x => new Effect((JObject)x)).ToList() : new List<Effect>()
                    };
                    if (sObj["verbs"] is JObject verbsObj) {
                        foreach (var vProp in verbsObj.Properties()) {
                            if (vProp.Value is JArray stepsArr) {
                                feature.Verbs[vProp.Name] = stepsArr.Select(x => new SubsystemStep((JObject)x)).ToList();
                            }
                        }
                    }
                    result[prop.Name] = feature;
                }
            }
            return result;
        }
    }

    public class SubsystemFeature {
        public List<string> Stats { get; set; } = new List<string>();
        public List<Effect> Effects { get; set; } = new List<Effect>();
        public Dictionary<string, List<SubsystemStep>> Verbs { get; } = new Dictionary<string, List<SubsystemStep>>(StringComparer.OrdinalIgnoreCase);
    }
}
