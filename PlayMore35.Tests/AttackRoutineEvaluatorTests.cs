using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlayMore35.CharacterChoices;
using PlayMore35.Global;
using PlayMore35.Items;

namespace PlayMore35.Tests {
    [TestClass]
    public class AttackRoutineEvaluatorTests {
        private Controller CreateControllerWithFighter(int fighterLevel, int strScore = 14, IEnumerable<string> feats = null) {
            var character = new Character();
            character.classLevels.Clear();
            for (int i = 0; i < fighterLevel; i++) {
                character.classLevels.Add("Fighter");
            }
            character.assignedAbilityScores["STR"] = strScore;
            character.assignedAbilityScores["DEX"] = 14;
            character.assignedAbilityScores["CON"] = 10;
            character.assignedAbilityScores["INT"] = 10;
            character.assignedAbilityScores["WIS"] = 10;
            character.assignedAbilityScores["CHA"] = 10;

            if (feats != null) {
                int index = 1;
                foreach (var feat in feats) {
                    character.feats[index.ToString()] = feat;
                    index++;
                }
            }

            var controller = new Controller(character);
            return controller;
        }

        // Source: Player's Handbook 3.5, p. 143
        [TestMethod]
        public void EvaluateRoutines_SoloPrimaryWeapon_GeneratesStandardAndIterativeFullAttacks() {
            var controller = CreateControllerWithFighter(11, strScore: 16); // BAB +11/+6/+1, STR +3
            controller.EquippedItems.PrimaryHandWeapon = controller.WeaponDefinitions["greatsword"];

            var routines = AttackRoutineEvaluator.EvaluateRoutines(controller.Effects, controller.EquippedItems, 0);

            // Verify Standard attack routine
            var standard = routines.FirstOrDefault(r => r.ActionType == RoutineActionType.Standard && r.Category == RoutineCategory.PrimarySolo);
            Assert.IsNotNull(standard, "Standard primary solo routine must exist");
            Assert.AreEqual(1, standard.Attacks.Count);
            // +11 BAB + 3 STR = +14
            Assert.AreEqual(14, standard.Attacks[0].AttackBonus.bonus);
            // 2d6 + 4 (1.5x STR = 4)
            Assert.AreEqual("2d6+4", standard.Attacks[0].BaseDamage.ToString());

            // Verify Full Attack routine
            var full = routines.FirstOrDefault(r => r.ActionType == RoutineActionType.FullAttack && r.Category == RoutineCategory.PrimarySolo);
            Assert.IsNotNull(full, "Full attack primary solo routine must exist");
            Assert.AreEqual(3, full.Attacks.Count, "Fighter 11 must have 3 iterative attacks");
            Assert.AreEqual(14, full.Attacks[0].AttackBonus.bonus); // +14
            Assert.AreEqual(9, full.Attacks[1].AttackBonus.bonus);  // +9 (+14 - 5)
            Assert.AreEqual(4, full.Attacks[2].AttackBonus.bonus);  // +4 (+14 - 10)

            // All attacks in full attack deal 1.5x STR
            Assert.AreEqual("2d6+4", full.Attacks[0].BaseDamage.ToString());
            Assert.AreEqual("2d6+4", full.Attacks[1].BaseDamage.ToString());
            Assert.AreEqual("2d6+4", full.Attacks[2].BaseDamage.ToString());
        }

        // Source: Player's Handbook 3.5, p. 143, p. 160
        [TestMethod]
        public void EvaluateRoutines_TwoWeaponFighting_SequencesProgressionTiers_WithTwfPenalties() {
            var controller = CreateControllerWithFighter(6, strScore: 14, feats: new[] { "Two-Weapon Fighting", "Improved Two-Weapon Fighting" });
            controller.EquippedItems.PrimaryHandWeapon = controller.WeaponDefinitions["longsword"];
            controller.EquippedItems.OffHandWeapon = controller.WeaponDefinitions["short sword"];

            var routines = AttackRoutineEvaluator.EvaluateRoutines(controller.Effects, controller.EquippedItems, 0);

            var twfRoutine = routines.FirstOrDefault(r => r.ActionType == RoutineActionType.FullAttack && r.Category == RoutineCategory.TwoWeaponFighting);
            Assert.IsNotNull(twfRoutine, "TWF full attack routine must exist");
            Assert.AreEqual(4, twfRoutine.Attacks.Count, "Fighter 6 with ITWF and light offhand must have 4 attacks (2 primary, 2 offhand)");

            // BAB +6, STR +2, TWF penalty -2 (light offhand + TWF feat) -> Net bonus: +6 + 2 - 2 = +6
            // Tier 1: Primary 1st (+6), Off-Hand 1st (+6)
            // Tier 2: Primary 2nd (+1), Off-Hand 2nd (+1)
            Assert.AreEqual("longsword", twfRoutine.Attacks[0].WeaponName);
            Assert.AreEqual(6, twfRoutine.Attacks[0].AttackBonus.bonus, string.Join("; ", twfRoutine.Attacks[0].AttackBonus.explanation));

            Assert.AreEqual("short sword", twfRoutine.Attacks[1].WeaponName);
            Assert.AreEqual(6, twfRoutine.Attacks[1].AttackBonus.bonus);

            Assert.AreEqual("longsword", twfRoutine.Attacks[2].WeaponName);
            Assert.AreEqual(1, twfRoutine.Attacks[2].AttackBonus.bonus);

            Assert.AreEqual("short sword", twfRoutine.Attacks[3].WeaponName);
            Assert.AreEqual(1, twfRoutine.Attacks[3].AttackBonus.bonus);

            // Primary gets full STR (+2), Off-hand gets half STR (+1)
            Assert.AreEqual("1d8+2", twfRoutine.Attacks[0].BaseDamage.ToString());
            Assert.AreEqual("1d6+1", twfRoutine.Attacks[1].BaseDamage.ToString());
        }

        // Source: Player's Handbook 3.5, p. 118, p. 160
        [TestMethod]
        public void EvaluateRoutines_DoubleWeapon_GeneratesTwoHandedSingleAndTwfRoutines() {
            var controller = CreateControllerWithFighter(1, strScore: 14);
            controller.EquippedItems.PrimaryHandWeapon = controller.WeaponDefinitions["orc double axe"];

            var routines = AttackRoutineEvaluator.EvaluateRoutines(controller.Effects, controller.EquippedItems, 0);

            // Double weapon single attack routine (two-handed)
            var standard = routines.FirstOrDefault(r => r.ActionType == RoutineActionType.Standard && r.Category == RoutineCategory.DoubleWeapon);
            Assert.IsNotNull(standard, "Double weapon standard routine must exist");
            Assert.AreEqual(1, standard.Attacks.Count);
            Assert.AreEqual("1d8+3", standard.Attacks[0].BaseDamage.ToString(), "Two-handed gets 1.5x STR (+3)");

            // Double weapon TWF routine
            var twf = routines.FirstOrDefault(r => r.ActionType == RoutineActionType.FullAttack && r.Category == RoutineCategory.DoubleWeapon);
            Assert.IsNotNull(twf, "Double weapon TWF full attack routine must exist");
            Assert.AreEqual(2, twf.Attacks.Count, "Level 1 fighter double weapon TWF has 2 attacks");
            // Head 1 primary, Head 2 off-hand
            Assert.AreEqual("orc double axe", twf.Attacks[0].WeaponName);
            Assert.AreEqual("orc double axe", twf.Attacks[1].WeaponName);
            Assert.AreEqual("1d8+2", twf.Attacks[0].BaseDamage.ToString(), "Primary head gets full STR");
            Assert.AreEqual("1d8+1", twf.Attacks[1].BaseDamage.ToString(), "Second head treated as light gets half STR");
        }

        // Source: Player's Handbook 3.5, p. 124, p. 125
        [TestMethod]
        public void EvaluateRoutines_SoloArmorSpikesAndSoloShieldBash_GenerateValidStandaloneRoutines() {
            var controller = CreateControllerWithFighter(1, strScore: 14);
            var armor = new Armor(controller.ArmorDefinitions["full plate"].ToJson() as Newtonsoft.Json.Linq.JObject);
            armor.ArmorSpikes = new AttackMode(controller.WeaponDefinitions["armor spikes"].Melee?.atk);
            controller.EquippedItems.DonnedArmor = armor;

            var shield = controller.ShieldDefinitions["heavy steel shield"];
            controller.EquippedItems.OffHandShield = shield;

            var routines = AttackRoutineEvaluator.EvaluateRoutines(controller.Effects, controller.EquippedItems, 0);

            var spikesSolo = routines.FirstOrDefault(r => r.Category == RoutineCategory.ArmorSpikesSolo);
            Assert.IsNotNull(spikesSolo, "Solo armor spikes routine must exist");
            Assert.AreEqual("armor spikes", spikesSolo.Attacks[0].WeaponName);

            var shieldSolo = routines.FirstOrDefault(r => r.Attacks.Any(a => a.WeaponName == "shield bash") && r.Attacks.Count == 1);
            Assert.IsNotNull(shieldSolo, "Solo shield bash routine must exist");
        }

        // Source: Player's Handbook 3.5, p. 160
        [TestMethod]
        public void EvaluateRoutines_MultiWeaponSecondaryCombinations_CategorizedCorrectly() {
            var controller = CreateControllerWithFighter(1, strScore: 14);
            controller.EquippedItems.PrimaryHandWeapon = controller.WeaponDefinitions["longsword"];
            var armor = new Armor(controller.ArmorDefinitions["full plate"].ToJson() as Newtonsoft.Json.Linq.JObject);
            armor.ArmorSpikes = new AttackMode(controller.WeaponDefinitions["armor spikes"].Melee?.atk);
            controller.EquippedItems.DonnedArmor = armor;

            var shield = controller.ShieldDefinitions["heavy steel shield"];
            controller.EquippedItems.OffHandShield = shield;

            var routines = AttackRoutineEvaluator.EvaluateRoutines(controller.Effects, controller.EquippedItems, 0);

            var shieldTwf = routines.FirstOrDefault(r => r.Category == RoutineCategory.OffHandShieldBash);
            Assert.IsNotNull(shieldTwf, "Off-hand shield bash TWF routine must exist");

            var spikesTwf = routines.FirstOrDefault(r => r.Category == RoutineCategory.ArmorSpikesTWF);
            Assert.IsNotNull(spikesTwf, "Armor spikes TWF routine must exist");
        }
    }
}
