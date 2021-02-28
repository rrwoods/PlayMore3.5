using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using PlayMore35.Effects;
using PlayMore35.Items;

namespace PlayMore35.CharacterChoices {
    public class CharacterClass : IJsonable {
        public enum BonusProgression { NONE, SLOW, MEDIUM, FAST };

        public string Name = "";
        public string Prerequisites = "";
        public int HitDie = 4;
        public List<string> ClassSkills = new List<string>();
        public int SkillPoints = 2;

        public BonusProgression BaseAttackBonus = BonusProgression.SLOW;
        public BonusProgression BaseFortSave = BonusProgression.SLOW;
        public BonusProgression BaseRefSave = BonusProgression.SLOW;
        public BonusProgression BaseWillSave = BonusProgression.SLOW;

        public List<AttackMatcher> WeaponProficiencies = new List<AttackMatcher>();
        public List<Armor.WeightCategory> ArmorProficiencies = new List<Armor.WeightCategory>();
        public List<bool> ShieldProficiencies = new List<bool>();

        // level 1 at position 0
        // within each level, a list of named class features, each represented as one effect
        public List<List<Effect>> ClassFeatures = new List<List<Effect>>();

        // within each level, a list of toggles required by that level's features
        public List<List<ControllableFlag>> Flags = new List<List<ControllableFlag>>();

        // { description => level }
        // examples:
        // "Fighter 1" => 1
        // "Endurance" => 3
        public Dictionary<string, int> BonusFeats = new Dictionary<string, int>();

        // S U B S Y S T E M S .
        public (List<string> stats, List<Effect> effects)? Vestiges;

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
                new JProperty("class features", new JArray(ClassFeatures.Select(level =>
                    new JArray(level.Select(feature => new JObject(feature)))
                )))
            );
            if (Flags.Any()) {
                json["flags"] = new JArray(Flags.Select(level => new JArray(level.Select(flag => flag.ToJson()))));
            }
            if (BonusFeats.Any()) {
                json["bonus feats"] = new JObject(BonusFeats.Select(x => new JProperty(x.Key, x.Value)));
            }

            if (Vestiges != null) {
                json["vestiges"] = new JObject(
                    new JProperty("stats", new JArray(Vestiges?.stats)),
                    new JProperty("effects", new JArray(Vestiges?.effects.Select(x => x.ToJson())))
                );
            }

            return json;
        }

        public CharacterClass(JObject json) {
            Name = (string)json["name"];
            Prerequisites = (string)json["prerequisites"];
            HitDie = (int)json["hit die"];
            ClassSkills.AddRange(json["class skills"].Select(x => (string)x));
            SkillPoints = (int)json["skill points"];
            BaseAttackBonus = (BonusProgression)Enum.Parse(typeof(BonusProgression), (string)json["base attack bonus"], false);
            BaseFortSave = (BonusProgression)Enum.Parse(typeof(BonusProgression), (string)json["base fort save"], false);
            BaseRefSave = (BonusProgression)Enum.Parse(typeof(BonusProgression), (string)json["base ref save"], false);
            BaseWillSave = (BonusProgression)Enum.Parse(typeof(BonusProgression), (string)json["base will save"], false);
            WeaponProficiencies.AddRange(json["weapon proficiencies"].Select(x => new AttackMatcher((JObject)x)));
            ArmorProficiencies.AddRange(json["armor proficiencies"].Select(x =>
                (Armor.WeightCategory) Enum.Parse(typeof(Armor.WeightCategory), (string)x, false)
            ));
            ShieldProficiencies.AddRange(json["shield proficiencies"].Select(x => x.Equals("tower")));
            ClassFeatures.AddRange(json["class features"].Select(level =>
                level.Select(feature => new Effect((JObject)feature)).ToList()
            ));
            if (json["flags"] != null) {
                foreach (JArray level in json["flags"]) {
                    Flags.Add(level.Select(x => new ControllableFlag((JObject)x)).ToList());
                }
            }
            if (json["bonus feats"] != null) {
                foreach (JProperty feat in json["bonus feats"]) {
                    BonusFeats[feat.Name] = (int)feat.Value;
                }
            }

            if (json["vestiges"] != null) {
                Vestiges = (
                    json["vestiges"]["stats"].Select(x => (string)x).ToList(),
                    json["vestiges"]["effects"].Select(x => new Effect((JObject)x)).ToList()
                );
            }
        }
    }
}
