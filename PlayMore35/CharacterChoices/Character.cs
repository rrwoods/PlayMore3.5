using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Items;
using PlayMore35.Subsystems;

namespace PlayMore35.CharacterChoices {
    public class Character : IJsonable {
        public static readonly string DEFAULT_CLASS = "Barbarian";

        public string name = "";
        public string gender = "";
        public string alignment = "";

        public Dictionary<string, int> assignedAbilityScores = new Dictionary<string, int> {
            ["STR"] = 8,
            ["DEX"] = 8,
            ["CON"] = 8,
            ["INT"] = 8,
            ["WIS"] = 8,
            ["CHA"] = 8,
        };
        public List<string> bonusLanguages = new List<string>();
        public string race = ""; // templates later
        public List<string> classLevels = new List<string> { DEFAULT_CLASS };
        public Dictionary<int, string> abilityScoreIncrements = new Dictionary<int, string>();
        public List<int> rolledHitDice = new List<int>();
        public Dictionary<string, List<int>> skillRanks = new Dictionary<string, List<int>>(); // {skill name -> ranks at each level}
        public List<FeatAssignment> feats = new List<FeatAssignment>();

        public List<GpTransaction> transactions = new List<GpTransaction>();
        // All items available to the character that are not currently equipped
        public Dictionary<string, InventoryLocation> inventory = new Dictionary<string, InventoryLocation>();

        public EquippedChoices equipped = new EquippedChoices();

        public List<XpSessionEntry> sessions = new List<XpSessionEntry>();

        public List<string> externalEffects = new List<string>();
        public Dictionary<string, SubsystemState> Subsystems { get; set; } = new Dictionary<string, SubsystemState>(StringComparer.OrdinalIgnoreCase);

        // THESE ARE NOT the main source of truth for flag and parameter values -- that's EffectPool's responsibility.
        // These are for saving and loading ONLY!
        public Dictionary<string, bool> flagValues = new Dictionary<string, bool>();
        public Dictionary<string, int> paramValues = new Dictionary<string, int>();

        public Timeline Timeline { get; set; } = new Timeline();
        public AttackRoutinePreferences RoutinePreferences { get; set; } = new AttackRoutinePreferences();
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
                new JProperty("skill ranks", new JObject(skillRanks
                    .Where(x => x.Value.Sum() > 0)
                    .Select(x => new JProperty(x.Key, new JArray(x.Value)))
                )),
                new JProperty("feats", new JArray(feats.Select(f => f.ToJson()))),
                new JProperty("transactions", new JArray(transactions.Select(x => x.ToJson()))),
                new JProperty("inventory", new JObject(
                    inventory.Select(x => new JProperty(x.Key, x.Value.ToJson()))
                )),
                new JProperty("equipped", equipped.ToJson()),
                new JProperty("sessions", new JArray(sessions.Select(x => x.ToJson()))),
                new JProperty("effects", new JArray(externalEffects)),
                new JProperty("subsystems", new JObject(Subsystems.Select(x => new JProperty(x.Key, x.Value.ToJson())))),
                new JProperty("routine preferences", RoutinePreferences.ToJson()),
                new JProperty("flag values", new JObject(flagValues.Select(x => new JProperty(x.Key, x.Value)))),
                new JProperty("parameter values", new JObject(paramValues.Select(x => new JProperty(x.Key, x.Value)))),
                new JProperty("time", Timeline.ToJson())
            );
        }

        public Character(JObject json) {
            name = (string?)json["name"] ?? "";
            gender = (string?)json["gender"] ?? "";
            alignment = (string?)json["alignment"] ?? "";

            assignedAbilityScores = json["assigned"] is JObject assignedObj
                ? assignedObj.Properties().ToDictionary(x => x.Name, x => (int)x.Value)
                : new Dictionary<string, int> {
                    ["STR"] = 8,
                    ["DEX"] = 8,
                    ["CON"] = 8,
                    ["INT"] = 8,
                    ["WIS"] = 8,
                    ["CHA"] = 8,
                };

            bonusLanguages = json["bonus languages"] is JArray bonusLangArr
                ? bonusLangArr.Select(x => (string)x!).ToList()
                : new List<string>();

            race = (string?)json["race"] ?? "";

            classLevels = json["classes"] is JArray classesArr
                ? classesArr.Select(x => (string)x!).ToList()
                : new List<string>();

            abilityScoreIncrements = json["incremented"] is JObject incObj
                ? incObj.Properties().ToDictionary(x => int.Parse(x.Name), x => (string)x.Value!)
                : new Dictionary<int, string>();

            rolledHitDice = json["hit die rolls"] is JArray hitDiceArr
                ? hitDiceArr.Select(x => (int)x).ToList()
                : new List<int>();

            skillRanks = json["skill ranks"] is JObject skillsObj
                ? skillsObj.Properties().ToDictionary(
                    skill => skill.Name,
                    skill => ((JArray)skill.Value).Select(ranks => (int)ranks).ToList()
                )
                : new Dictionary<string, List<int>>();

            feats = json["feats"] is JArray featsArr
                ? featsArr.OfType<JObject>().Select(o => new FeatAssignment(o)).ToList()
                : new List<FeatAssignment>();

            transactions = json["transactions"] is JArray transArr
                ? transArr.Select(x => new GpTransaction((JObject)x)).OrderBy(t => t.GameTime).ThenBy(t => t.RealDate).ToList()
                : new List<GpTransaction>();

            inventory = json["inventory"] is JObject invObj
                ? invObj.Properties().ToDictionary(x => x.Name, x => new InventoryLocation((JObject)x.Value))
                : new Dictionary<string, InventoryLocation>();

            equipped = json["equipped"] is JObject eqObj
                ? new EquippedChoices(eqObj)
                : new EquippedChoices();

            sessions = json["sessions"] is JArray sessArr
                ? sessArr.Select(x => new XpSessionEntry((JObject)x)).OrderBy(s => s.GameTime).ThenBy(s => s.RealDate).ToList()
                : new List<XpSessionEntry>();

            externalEffects = json["effects"] is JArray effArr
                ? effArr.Select(x => (string)x!).ToList()
                : new List<string>();

            Subsystems = new Dictionary<string, SubsystemState>(StringComparer.OrdinalIgnoreCase);
            if (json["subsystems"] is JObject subObj) {
                foreach (var prop in subObj.Properties()) {
                    if (prop.Value is JObject stateJson) {
                        Subsystems[prop.Name] = new SubsystemState(stateJson);
                    }
                }
            }

            flagValues = json["flag values"] is JObject flagsObj
                ? flagsObj.Properties().ToDictionary(x => x.Name, x => (bool)x.Value)
                : new Dictionary<string, bool>();

            paramValues = json["parameter values"] is JObject paramsObj
                ? paramsObj.Properties().ToDictionary(x => x.Name, x => (int)x.Value)
                : new Dictionary<string, int>();

            if (json["routine preferences"] is JObject prefObj) {
                RoutinePreferences = new AttackRoutinePreferences(prefObj);
            } else {
                RoutinePreferences = new AttackRoutinePreferences();
            }

            Timeline = json["time"] is JObject timeObj
                ? new Timeline(timeObj)
                : new Timeline();
        }

        public Character() { }


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

        public decimal TotalGold() {
            return transactions.Sum(x => x.Amount);
        }

        public int TotalExperience() {
            return sessions.Sum(s => s.Xp);
        }

        // if this number is negative, the player is due to level up
        public int ExperienceToNextLevel() {
            int level = classLevels.Count;
            int threshold = 500 * level * (level + 1);
            return threshold - TotalExperience();
        }
    }
}
