using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json.Linq;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Items;
using static PlayMore35.Effects.Constants;

namespace PlayMore35.Character {
    [TestClass]
    public class CharacterChoiceTests {
        private CharacterChoices SampleCharacter() {
            return new CharacterChoices {
                assignedAbilityScores = new Dictionary<string, int> {
                    ["STR"] = 14,
                    ["DEX"] = 8,
                    ["CON"] = 17,
                    ["INT"] = 14,
                    ["WIS"] = 13,
                    ["CHA"] = 10
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
                shields = new List<ShieldChoice> {
                    new ShieldChoice {
                        Name = "heavy shield"
                    }
                }
            };
        }

        [TestMethod]
        public void TestCharacterGeneration() {
            Controller controller = new Controller(SampleCharacter());

            // no racial bonuses working right now, so shoehorn it in:
            controller.Effects.AddEffect(new Effect("dream dwarf CON bonus")
                .AddModifier(CON_SCORE, new Modifier(+2, RACIAL))
            );

            Equipped noItems = new Equipped();
            Assert.AreEqual(14, controller.Effects.GetStat(STR_SCORE).GetValue(controller.Effects, noItems).value);
            Assert.AreEqual(77, controller.Effects.GetStat(MAX_HP).GetValue(controller.Effects, noItems).value);

            List<Weapon> weapons = controller.AvailableWeapons();
            Assert.AreEqual(1, weapons.Count);
            Assert.AreEqual("dwarven waraxe", weapons[0].Name);
            Assert.AreEqual((1, 10), weapons[0].Melee?.atk.DamageDieMedium);

            List<Armor> armor = controller.AvailableArmor();
            Assert.AreEqual(1, armor.Count);
            Assert.AreEqual("full plate", armor[0].Name);
            Assert.AreEqual(1, armor[0].Properties.Count);
            Assert.AreEqual("mwk", armor[0].Properties[0].Name);

            List<Shield> shields = controller.AvailableShields();
            Assert.AreEqual(1, shields.Count);
            Assert.AreEqual("heavy shield", shields[0].Name);
            Assert.AreEqual(Weapon.Effort.ONE_HANDED, shields[0].ShieldBashEffort);
            Assert.AreEqual((1, 4), shields[0].ShieldBash.DamageDieMedium);
        }

        [TestMethod]
        public void TestSerialization() {
            CharacterChoices choices = SampleCharacter();

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
