using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace PlayMore35.CharacterChoices {
    public class Character : IJsonable {
        public static readonly string DEFAULT_CLASS = "Barbarian";

        public string name;
        public string gender;
        public string alignment;

        public Dictionary<string, int> assignedAbilityScores;
        public List<string> bonusLanguages;
        public string race; // templates later
        public List<string> classLevels;
        public Dictionary<int, string> abilityScoreIncrements;
        public List<int> rolledHitDice;
        public Dictionary<string, List<int>> skillRanks; // {skill name -> ranks at each level}
        public Dictionary<string, string> feats; // keys are feat slots, values are feat names

        // All items available to the character that are not currently equipped
        public Dictionary<string, InventoryLocation> inventory;

        public EquippedChoices equipped;

        public JToken ToJson() {
            return new JObject(
                new JProperty("name", name),
                new JProperty("gender", gender),
                new JProperty("alignment", alignment),
                new JProperty("assigned", new JObject(assignedAbilityScores.Select(x => new JProperty(x.Key, x.Value)))),
                new JProperty("bonus languages", new JArray(bonusLanguages)),
                new JProperty("race", race),
                new JProperty("classes", new JArray(classLevels)),
                new JProperty("incremented", new JObject(abilityScoreIncrements.Select(x => new JProperty("" + x.Key, x.Value)))),
                new JProperty("hit die rolls", new JArray(rolledHitDice)),
                new JProperty("skill ranks", new JObject(skillRanks.Select(x => new JProperty(x.Key, new JArray(x.Value))))),
                new JProperty("feats", new JObject(feats.Select(x => new JProperty(x.Key, x.Value)))),
                new JProperty("inventory", new JObject(
                    inventory.Select(x => new JProperty(x.Key, x.Value.ToJson()))
                )),
                new JProperty("equipped", equipped.ToJson())
            );
        }

        public Character(JObject json) {
            name = (string)json["name"];
            gender = (string)json["gender"];
            alignment = (string)json["alignment"];
            assignedAbilityScores = ((JObject)json["assigned"]).Properties().ToDictionary(x => x.Name, x => (int)x.Value);
            bonusLanguages = ((JArray)json["bonus languages"]).Select(x => (string)x).ToList();
            race = (string)json["race"];
            classLevels = ((JArray)json["classes"]).Select(x => (string)x).ToList();
            abilityScoreIncrements = ((JObject)json["incremented"]).Properties()
                .ToDictionary(x => int.Parse(x.Name), x => (string)x.Value);
            rolledHitDice = ((JArray)json["hit die rolls"]).Select(x => (int)x).ToList();
            skillRanks = ((JObject)json["skill ranks"]).Properties().ToDictionary(
                skill => skill.Name,
                skill => ((JArray)skill.Value).Select(ranks => (int)ranks).ToList()
            );
            feats = ((JObject)json["feats"]).Properties().ToDictionary(x => x.Name, x => (string)x.Value);
            inventory = ((JObject)json["inventory"]).Properties()
                .ToDictionary(x => x.Name, x => new InventoryLocation((JObject)x.Value));

            equipped = new EquippedChoices((JObject)json["equipped"]);
        }

        public Character() {
            name = "";
            gender = "";
            alignment = "";
            assignedAbilityScores = new Dictionary<string, int> {
                ["STR"] = 8,
                ["DEX"] = 8,
                ["CON"] = 8,
                ["INT"] = 8,
                ["WIS"] = 8,
                ["CHA"] = 8,
            };
            race = "";

            classLevels = new List<string> { DEFAULT_CLASS };

            abilityScoreIncrements = new Dictionary<int, string>();
            bonusLanguages = new List<string>();
            rolledHitDice = new List<int>();
            skillRanks = new Dictionary<string, List<int>>();
            feats = new Dictionary<string, string>();

            inventory = new Dictionary<string, InventoryLocation>();
            equipped = new EquippedChoices();
        }

        public IEnumerable<WeaponChoice> Weapons {
            get {
                return inventory.SelectMany(x => x.Value.weapons);
            }
        }

        public IEnumerable<ArmorChoice> Armor {
            get {
                return inventory.SelectMany(x => x.Value.armor);
            }
        }

        public IEnumerable<ArmorChoice> Shields {
            get {
                return inventory.SelectMany(x => x.Value.shields);
            }
        }

        public Dictionary<string, int> SummarizeClassLevels() {
            Dictionary<string, int> summary = new Dictionary<string, int>();
            foreach (var @class in classLevels) {
                if (!summary.ContainsKey(@class)) {
                    summary[@class] = 1;
                } else {
                    summary[@class]++;
                }
            }
            return summary;
        }

        public List<(string, int)> DetailClassLevels() {
            List<(string, int)> detail = new List<(string, int)>();
            Dictionary<string, int> summary = new Dictionary<string, int>();
            foreach (var @class in classLevels) {
                if (!summary.ContainsKey(@class)) {
                    summary[@class] = 1;
                    detail.Add((@class, 1));
                } else {
                    summary[@class]++;
                    detail.Add((@class, summary[@class]));
                }
            }
            return detail;
        }
    }
}
