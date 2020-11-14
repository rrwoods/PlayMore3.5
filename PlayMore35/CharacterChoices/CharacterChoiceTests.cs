using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace PlayMore35.CharacterChoices {
    [TestClass]
    public class CharacterChoiceTests {
        [TestMethod]
        public void TestSerialization() {
            CharacterChoices choices = new CharacterChoices {
                assignedAbilityScores = new Dictionary<string, int> {
                    ["STR"] = 14, ["DEX"] = 8, ["CON"] = 17, ["INT"] = 14, ["WIS"] = 13, ["CHA"] = 10
                },
                classLevels = new List<string> {
                    "Binder", "Barbarian", "Fighter", "Fighter", "Crusader", "Crusader", "Pious Templar"
                },
                abilityScoreIncrements = new List<string> { "CON" },
                rolledHitDice = new List<int> { 8, 6, 6, 5, 6, 5, 6 },
                skillRanks = new Dictionary<string, List<int>> {
                    ["Knowledge (religion)"] = new List<int> { 4, 0, 0, 0, 1, 0, 0 },
                    ["Knowledge (dungeoneering)"] = new List<int> { 2, 0, 1, 0, 1, 0, 1 },
                    ["Jump"] = new List<int> { 0, 1, 1, 0, 3, 0, 0 },
                    ["Climb"] = new List<int> { 0, 0, 1, 4, 0, 0, 0 },
                    ["Intimidate"] = new List<int> { 0, 0, 0, 0, 0, 4, 0 },
                    ["Survival"] = new List<int> { 0, 5, 0, 0, 0, 0, 0 },
                    ["Heal"] = new List<int> { 2, 0, 0, 0, 0, 1, 2 },
                    ["Balance"] = new List<int> { 2, 0, 0, 0, 0, 0, 0 },
                },
                feats = new List<List<string>> {
                    new List<string> { "Improved Binding" },
                    new List<string>(),
                    new List<string> { "Constant Guardian", "Endurance" },
                    new List<string> { "Weapon Focus (dagger)" },
                    new List<string>(),
                    new List<string> { "True Believer" },
                    new List<string>()
                },
                weapons = new List<WeaponChoice> {
                    new WeaponChoice {
                        Name = "dwarven waraxe"
                    }
                },
                armor = new List<ArmorChoice> {
                    new ArmorChoice {
                        Name = "full plate",
                        ArmorProperties = new List<string> { "mwk" }
                    }
                },
                shields = new List<ArmorChoice> {
                    new ArmorChoice {
                        Name = "extreme heavy shield"
                    }
                }
            };

            CharacterChoices fromJson = new CharacterChoices((JObject)choices.ToJson());

            Assert.AreEqual(choices.assignedAbilityScores["STR"], fromJson.assignedAbilityScores["STR"]);
            Assert.AreEqual(choices.assignedAbilityScores["DEX"], fromJson.assignedAbilityScores["DEX"]);
            Assert.AreEqual(choices.assignedAbilityScores["CON"], fromJson.assignedAbilityScores["CON"]);
            Assert.AreEqual(choices.assignedAbilityScores["INT"], fromJson.assignedAbilityScores["INT"]);
            Assert.AreEqual(choices.assignedAbilityScores["WIS"], fromJson.assignedAbilityScores["WIS"]);
            Assert.AreEqual(choices.assignedAbilityScores["CHA"], fromJson.assignedAbilityScores["CHA"]);
            CollectionAssert.AreEqual(choices.classLevels, fromJson.classLevels);
            CollectionAssert.AreEqual(choices.abilityScoreIncrements, fromJson.abilityScoreIncrements);
            CollectionAssert.AreEqual(choices.rolledHitDice, fromJson.rolledHitDice);
            CollectionAssert.AreEqual(choices.skillRanks["Jump"], fromJson.skillRanks["Jump"]);
            for (int i = 0; i < 7; i++) {
                CollectionAssert.AreEqual(choices.feats[i], fromJson.feats[i]);
            }

            Assert.AreEqual(choices.weapons[0].Name, fromJson.weapons[0].Name);
            Assert.AreEqual(choices.armor[0].Name, fromJson.armor[0].Name);
            Assert.AreEqual(choices.armor[0].ArmorProperties[0], fromJson.armor[0].ArmorProperties[0]);
            Assert.AreEqual(choices.shields[0].Name, fromJson.shields[0].Name);
        }
    }
}
