using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace PlayMore35.Character {
    public class CharacterChoices : IJsonable {
        public Dictionary<string, int> assignedAbilityScores;
        public List<string> classLevels;
        public List<string> abilityScoreIncrements;
        public List<int> rolledHitDice;
        public Dictionary<string, List<int>> skillRanks; // {skill name -> ranks at each level}
        public List<List<string>> feats; // each element of the outer list is a level, inner is the feats taken at that level

        // All weapons, armor, and shields available to the character (not necessarily weilded)
        public List<WeaponChoice> weapons;
        public List<ArmorChoice> armor;
        public List<ArmorChoice> shields;
        // other items coming soon(tm)

        public JToken ToJson() {
            return new JObject(
                new JProperty("assigned", new JObject(assignedAbilityScores.Select(x => new JProperty(x.Key, x.Value)))),
                new JProperty("classes", new JArray(classLevels)),
                new JProperty("incremented", new JArray(abilityScoreIncrements)),
                new JProperty("hit die rolls", new JArray(rolledHitDice)),
                new JProperty("skill ranks", new JObject(skillRanks.Select(x => new JProperty(x.Key, new JArray(x.Value))))),
                new JProperty("feats", new JArray(feats.Select(x => new JArray(x)))),

                new JProperty("weapons", new JArray(weapons.Select(x => x.ToJson()))),
                new JProperty("armor", new JArray(armor.Select(x => x.ToJson()))),
                new JProperty("shields", new JArray(shields.Select(x => x.ToJson())))
            );
        }

        public CharacterChoices(JObject json) {
            assignedAbilityScores = ((JObject)json["assigned"]).Properties().ToDictionary(x => x.Name, x => (int)x.Value);
            classLevels = ((JArray)json["classes"]).Select(x => (string)x).ToList();
            abilityScoreIncrements = ((JArray)json["incremented"]).Select(x => (string)x).ToList();
            rolledHitDice = ((JArray)json["hit die rolls"]).Select(x => (int)x).ToList();
            skillRanks = ((JObject)json["skill ranks"]).Properties().ToDictionary(
                skill => skill.Name,
                skill => ((JArray)skill.Value).Select(ranks => (int)ranks).ToList()
            );
            feats = ((JArray)json["feats"])
                .Select(level => ((JArray)level)
                    .Select(feat => (string)feat).ToList())
                .ToList();

            weapons = ((JArray)json["weapons"]).Select(x => new WeaponChoice((JObject)x)).ToList();
            armor = ((JArray)json["armor"]).Select(x => new ArmorChoice((JObject)x)).ToList();
            shields = ((JArray)json["shields"]).Select(x => new ArmorChoice((JObject)x)).ToList();
        }

        public CharacterChoices() {
            assignedAbilityScores = new Dictionary<string, int>();
            classLevels = new List<string>();
            abilityScoreIncrements = new List<string>();
            rolledHitDice = new List<int>();
            skillRanks = new Dictionary<string, List<int>>();
            feats = new List<List<string>>();

            weapons = new List<WeaponChoice>();
            armor = new List<ArmorChoice>();
            shields = new List<ArmorChoice>();
        }
    }
}
