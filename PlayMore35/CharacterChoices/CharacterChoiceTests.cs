using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json.Linq;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Items;
using static PlayMore35.Effects.Constants;

namespace PlayMore35.CharacterChoices {
    [TestClass]
    public class CharacterChoiceTests {
        private Character SampleCharacter() {
            return new Character {
                assignedAbilityScores = new Dictionary<string, int> {
                    ["STR"] = 14,
                    ["DEX"] = 8,
                    ["CON"] = 17,
                    ["INT"] = 14,
                    ["WIS"] = 13,
                    ["CHA"] = 10
                },
                race = "dwarf",
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
                feats = new Dictionary<string, string> {
                    ["Level 1"] = "Improved Binding",
                    ["Level 3"] = "Endurance",
                    ["Fighter 1"] = "Constant Guardian",
                    ["Fighter 2"] = "Weapon Focus (dagger)",
                    ["Level 6"] = "True Believer"
                },
                equipped = new EquippedChoices {
                    PrimaryHandWeapon = new WeaponChoice {
                        Name = "dwarven waraxe"
                    },
                    DonnedArmor = new ArmorChoice {
                        Name = "full plate",
                        ArmorProperties = new List<string> { "mwk" }
                    },
                    OffHandShield = new ArmorChoice {
                        Name = "heavy wooden shield",
                        IsShield = true
                    }
                }
            };
        }

        private void CheckStat(Controller controller, string stat, int expectedValue) {
            Assert.AreEqual(expectedValue, controller.GetStatistic(stat).value);
        }

        public void TestCharacterGeneration() {
            Controller controller = new Controller(SampleCharacter());

            // some shoehorning of effects that would normally come from
            // race/class/feat definitions, that i don't have yet:
            controller.Effects.AddEffect(new Effect("character effects...")
                .AddProficiency(AttackMatcher.ALL)
                .AddProficiency(Armor.WeightCategory.HEAVY)
                .AddShieldProficiency(false)
            );

            Equipped noItems = new Equipped();
            Assert.AreEqual(14, controller.Effects.GetStat(STR_SCORE).GetValue(controller.Effects, noItems).value);
            Assert.AreEqual(77, controller.Effects.GetStat(MAX_HP).GetValue(controller.Effects, noItems).value);

            // Check basic armor/shields:
            CheckStat(controller, AC, 19);

            // Check armor check penalty (total -8 from armor + shield):
            // Jump = 5 ranks + 2 STR - 8 ACP - 6 speed = -7 total
            CheckStat(controller, JUMP, -7);
            // Intimidate = 4 ranks - 1 CHA (no ACP) = +3 total
            CheckStat(controller, INTIMIDATE, +3);

            CheckStat(controller, LAND_SPEED, 20);

            Assert.AreEqual(68, new ItemAmount(TOTAL_WEIGHT).Get(controller.Effects, controller.EquippedItems));
        }

        [TestMethod]
        public void TestSerialization() {
            Character character = SampleCharacter();

            Character fromJson = new Character((JObject)character.ToJson());

            Assert.AreEqual(character.assignedAbilityScores["STR"], fromJson.assignedAbilityScores["STR"]);
            Assert.AreEqual(character.assignedAbilityScores["DEX"], fromJson.assignedAbilityScores["DEX"]);
            Assert.AreEqual(character.assignedAbilityScores["CON"], fromJson.assignedAbilityScores["CON"]);
            Assert.AreEqual(character.assignedAbilityScores["INT"], fromJson.assignedAbilityScores["INT"]);
            Assert.AreEqual(character.assignedAbilityScores["WIS"], fromJson.assignedAbilityScores["WIS"]);
            Assert.AreEqual(character.assignedAbilityScores["CHA"], fromJson.assignedAbilityScores["CHA"]);
            CollectionAssert.AreEqual(character.classLevels, fromJson.classLevels);
            CollectionAssert.AreEqual(character.abilityScoreIncrements, fromJson.abilityScoreIncrements);
            CollectionAssert.AreEqual(character.rolledHitDice, fromJson.rolledHitDice);
            CollectionAssert.AreEqual(character.skillRanks["Jump"], fromJson.skillRanks["Jump"]);
            CollectionAssert.AreEqual(character.feats, fromJson.feats);
            Assert.AreEqual(character.equipped.PrimaryHandWeapon.Name, fromJson.equipped.PrimaryHandWeapon.Name);
            ArmorChoice armor = character.equipped.DonnedArmor;
            ArmorChoice jsonArmor = fromJson.equipped.DonnedArmor;
            Assert.AreEqual(armor.Name, jsonArmor.Name);
            Assert.AreEqual(armor.ArmorProperties[0], jsonArmor.ArmorProperties[0]);
            Assert.AreEqual(character.equipped.OffHandShield.Name, fromJson.equipped.OffHandShield.Name);
        }
    }
}
