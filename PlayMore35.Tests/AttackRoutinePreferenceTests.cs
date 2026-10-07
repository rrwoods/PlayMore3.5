using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Global;
using PlayMore35.Items;

namespace PlayMore35.Tests {
    [TestClass]
    public class AttackRoutinePreferenceTests {
        private Controller CreateControllerWithFighter() {
            var character = new Character();
            character.classLevels.Clear();
            character.classLevels.Add("Fighter");
            character.assignedAbilityScores["STR"] = 14;
            character.assignedAbilityScores["DEX"] = 14;
            character.assignedAbilityScores["CON"] = 10;
            character.assignedAbilityScores["INT"] = 10;
            character.assignedAbilityScores["WIS"] = 10;
            character.assignedAbilityScores["CHA"] = 10;
            return new Controller(character);
        }

        // Source: Player's Handbook 3.5, p. 143, p. 160
        [TestMethod]
        public void AttackRoutinePreferences_DefaultVisibility_FollowsTabletopDefaults() {
            var prefs = new AttackRoutinePreferences();

            Assert.IsTrue(prefs.IsCategoryVisible(RoutineCategory.PrimarySolo));
            Assert.IsTrue(prefs.IsCategoryVisible(RoutineCategory.TwoWeaponFighting));
            Assert.IsTrue(prefs.IsCategoryVisible(RoutineCategory.DoubleWeapon));
            Assert.IsTrue(prefs.IsCategoryVisible(RoutineCategory.ShieldBashSolo));
            Assert.IsTrue(prefs.IsCategoryVisible(RoutineCategory.ArmorSpikesSolo));

            Assert.IsFalse(prefs.IsCategoryVisible(RoutineCategory.OffHandShieldBash));
            Assert.IsFalse(prefs.IsCategoryVisible(RoutineCategory.ArmorSpikesTWF));
        }

        // Source: Player's Handbook 3.5, p. 143
        [TestMethod]
        public void AttackRoutinePreferences_PersistsCustomVisibility_AcrossSerialization() {
            var character = new Character();
            character.RoutinePreferences.SetCategoryVisible(RoutineCategory.OffHandShieldBash, true);
            character.RoutinePreferences.SetCategoryVisible(RoutineCategory.ArmorSpikesSolo, false);

            var json = (JObject)character.ToJson();
            var reloaded = new Character(json);

            Assert.IsTrue(reloaded.RoutinePreferences.IsCategoryVisible(RoutineCategory.OffHandShieldBash));
            Assert.IsFalse(reloaded.RoutinePreferences.IsCategoryVisible(RoutineCategory.ArmorSpikesSolo));
            // Unset still returns defaults
            Assert.IsTrue(reloaded.RoutinePreferences.IsCategoryVisible(RoutineCategory.PrimarySolo));
            Assert.IsFalse(reloaded.RoutinePreferences.IsCategoryVisible(RoutineCategory.ArmorSpikesTWF));
        }

        // Source: Player's Handbook 3.5, p. 160
        [TestMethod]
        public void AttackRoutinePreferences_CategoryPreferences_SurviveEquippedWeaponSwaps() {
            var controller = CreateControllerWithFighter();
            controller.EquippedItems.PrimaryHandWeapon = controller.WeaponDefinitions["longsword"];
            controller.EquippedItems.OffHandShield = controller.ShieldDefinitions["heavy steel shield"];

            // Initially OffHandShieldBash TWF is hidden
            var visibleBefore = controller.GetVisibleRoutines();
            Assert.IsFalse(visibleBefore.Any(r => r.Category == RoutineCategory.OffHandShieldBash));

            // Player enables OffHandShieldBash
            controller.Character.RoutinePreferences.SetCategoryVisible(RoutineCategory.OffHandShieldBash, true);
            var visibleAfter = controller.GetVisibleRoutines();
            Assert.IsTrue(visibleAfter.Any(r => r.Category == RoutineCategory.OffHandShieldBash));

            // Player swaps weapon to Greatsword (or short sword)
            controller.EquippedItems.PrimaryHandWeapon = controller.WeaponDefinitions["short sword"];

            // Preference survives weapon swap
            Assert.IsTrue(controller.Character.RoutinePreferences.IsCategoryVisible(RoutineCategory.OffHandShieldBash));
            var visibleAfterSwap = controller.GetVisibleRoutines();
            Assert.IsTrue(visibleAfterSwap.Any(r => r.Category == RoutineCategory.OffHandShieldBash));
        }

        // Source: Player's Handbook 3.5, p. 160
        [TestMethod]
        public void Controller_GetVisibleRoutines_FiltersPossibilityEngineByPreferences() {
            var controller = CreateControllerWithFighter();
            controller.EquippedItems.PrimaryHandWeapon = controller.WeaponDefinitions["longsword"];
            var armor = new Armor(controller.ArmorDefinitions["full plate"].ToJson() as JObject);
            armor.ArmorSpikes = new AttackMode(controller.WeaponDefinitions["armor spikes"].Melee?.atk);
            controller.EquippedItems.DonnedArmor = armor;
            controller.EquippedItems.OffHandShield = controller.ShieldDefinitions["heavy steel shield"];

            var allRoutines = controller.GetAllRoutines();
            Assert.IsTrue(allRoutines.Any(r => r.Category == RoutineCategory.ShieldBashSolo));
            Assert.IsTrue(allRoutines.Any(r => r.Category == RoutineCategory.OffHandShieldBash));
            Assert.IsTrue(allRoutines.Any(r => r.Category == RoutineCategory.ArmorSpikesTWF));

            var visibleRoutines = controller.GetVisibleRoutines();
            Assert.IsTrue(visibleRoutines.Any(r => r.Category == RoutineCategory.ShieldBashSolo));
            Assert.IsFalse(visibleRoutines.Any(r => r.Category == RoutineCategory.OffHandShieldBash));
            Assert.IsFalse(visibleRoutines.Any(r => r.Category == RoutineCategory.ArmorSpikesTWF));
            Assert.IsTrue(visibleRoutines.Any(r => r.Category == RoutineCategory.PrimarySolo));
            Assert.IsTrue(visibleRoutines.Any(r => r.Category == RoutineCategory.ArmorSpikesSolo));

            // Enable ArmorSpikesTWF
            controller.Character.RoutinePreferences.SetCategoryVisible(RoutineCategory.ArmorSpikesTWF, true);
            var updatedVisible = controller.GetVisibleRoutines();
            Assert.IsTrue(updatedVisible.Any(r => r.Category == RoutineCategory.ArmorSpikesTWF));
            Assert.IsFalse(updatedVisible.Any(r => r.Category == RoutineCategory.OffHandShieldBash));

            // Disable ShieldBashSolo independently
            controller.Character.RoutinePreferences.SetCategoryVisible(RoutineCategory.ShieldBashSolo, false);
            var withoutShieldSolo = controller.GetVisibleRoutines();
            Assert.IsFalse(withoutShieldSolo.Any(r => r.Category == RoutineCategory.ShieldBashSolo));
            Assert.IsTrue(withoutShieldSolo.Any(r => r.Category == RoutineCategory.PrimarySolo));
        }
    }
}
