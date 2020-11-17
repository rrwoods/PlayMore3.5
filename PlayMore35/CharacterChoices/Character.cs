using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace PlayMore35.CharacterChoices {
    public class Character : IJsonable {
        public Dictionary<string, int> assignedAbilityScores;
        public List<string> classLevels;
        public List<string> abilityScoreIncrements;
        public List<int> rolledHitDice;
        public Dictionary<string, List<int>> skillRanks; // {skill name -> ranks at each level}
        public List<List<string>> feats; // each element of the outer list is a level, inner is the feats taken at that level

        // Slots go here.  Probably like:
        // public WondrousItem headSlot = null;
        // public WondrousItem ring1 = null; // I know rings aren't "Wondrous". it doesn't affect this in any meaningful way.

        // All other items available to the character (not necessarily weilded)
        public Dictionary<string, InventoryLocation> inventory;

        public JToken ToJson() {
            return new JObject(
                new JProperty("assigned", new JObject(assignedAbilityScores.Select(x => new JProperty(x.Key, x.Value)))),
                new JProperty("classes", new JArray(classLevels)),
                new JProperty("incremented", new JArray(abilityScoreIncrements)),
                new JProperty("hit die rolls", new JArray(rolledHitDice)),
                new JProperty("skill ranks", new JObject(skillRanks.Select(x => new JProperty(x.Key, new JArray(x.Value))))),
                new JProperty("feats", new JArray(feats.Select(x => new JArray(x)))),
                new JProperty("inventory", new JObject(
                    inventory.Select(x => new JProperty(x.Key, x.Value.ToJson()))
                ))
            );
        }

        public Character(JObject json) {
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

            inventory = ((JObject)json["inventory"]).Properties()
                .ToDictionary(x => x.Name, x => new InventoryLocation((JObject)x.Value));
        }

        public Character() {
            assignedAbilityScores = new Dictionary<string, int>();
            classLevels = new List<string>();
            abilityScoreIncrements = new List<string>();
            rolledHitDice = new List<int>();
            skillRanks = new Dictionary<string, List<int>>();
            feats = new List<List<string>>();

            inventory = new Dictionary<string, InventoryLocation>();
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

        public IEnumerable<ShieldChoice> Shields {
            get {
                return inventory.SelectMany(x => x.Value.shields);
            }
        }
    }
}
