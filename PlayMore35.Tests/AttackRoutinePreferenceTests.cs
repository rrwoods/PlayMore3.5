using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Global;
using PlayMore35.Items;
using PlayMore35.Rules;

namespace PlayMore35.Tests {
    [TestClass]
    public class AttackRoutinePreferenceTests {
        private CharacterSession CreateSessionWithFighter() {
            var character = new Character();
            character.classLevels.Clear();
            character.classLevels.Add("Fighter");
            character.assignedAbilityScores["STR"] = 14;
            character.assignedAbilityScores["DEX"] = 14;
            character.assignedAbilityScores["CON"] = 10;
            character.assignedAbilityScores["INT"] = 10;
            character.assignedAbilityScores["WIS"] = 10;
            character.assignedAbilityScores["CHA"] = 10;
            return new CharacterSession(character, new GameRulesCatalog());
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
            var session = CreateSessionWithFighter();
            session.Mutate(c => {
                c.equipped.PrimaryHandWeapon = new WeaponChoice { Name = "longsword" };
                c.equipped.OffHandShield = new ArmorChoice { Name = "heavy steel shield", IsShield = true };
            });

            // Initially OffHandShieldBash TWF is hidden
            var visibleBefore = session.GetVisibleRoutines();
            Assert.IsFalse(visibleBefore.Any(r => r.Category == RoutineCategory.OffHandShieldBash));

            // Player enables OffHandShieldBash
            session.Character.RoutinePreferences.SetCategoryVisible(RoutineCategory.OffHandShieldBash, true);
            var visibleAfter = session.GetVisibleRoutines();
            Assert.IsTrue(visibleAfter.Any(r => r.Category == RoutineCategory.OffHandShieldBash));

            // Player swaps weapon to short sword
            session.Mutate(c => c.equipped.PrimaryHandWeapon = new WeaponChoice { Name = "short sword" });

            // Preference survives weapon swap
            Assert.IsTrue(session.Character.RoutinePreferences.IsCategoryVisible(RoutineCategory.OffHandShieldBash));
            var visibleAfterSwap = session.GetVisibleRoutines();
            Assert.IsTrue(visibleAfterSwap.Any(r => r.Category == RoutineCategory.OffHandShieldBash));
        }

        // Source: Player's Handbook 3.5, p. 160
        [TestMethod]
        public void CharacterSession_GetVisibleRoutines_FiltersPossibilityEngineByPreferences() {
            var session = CreateSessionWithFighter();
            session.Mutate(c => {
                c.equipped.PrimaryHandWeapon = new WeaponChoice { Name = "longsword" };
                c.equipped.DonnedArmor = new ArmorChoice { Name = "full plate", Spiked = true };
                c.equipped.OffHandShield = new ArmorChoice { Name = "heavy steel shield", IsShield = true };
            });

            var allRoutines = session.GetAllRoutines();
            Assert.IsTrue(allRoutines.Any(r => r.Category == RoutineCategory.ShieldBashSolo));
            Assert.IsTrue(allRoutines.Any(r => r.Category == RoutineCategory.OffHandShieldBash));
            Assert.IsTrue(allRoutines.Any(r => r.Category == RoutineCategory.ArmorSpikesTWF));

            var visibleRoutines = session.GetVisibleRoutines();
            Assert.IsTrue(visibleRoutines.Any(r => r.Category == RoutineCategory.ShieldBashSolo));
            Assert.IsFalse(visibleRoutines.Any(r => r.Category == RoutineCategory.OffHandShieldBash));
            Assert.IsFalse(visibleRoutines.Any(r => r.Category == RoutineCategory.ArmorSpikesTWF));
            Assert.IsTrue(visibleRoutines.Any(r => r.Category == RoutineCategory.PrimarySolo));
            Assert.IsTrue(visibleRoutines.Any(r => r.Category == RoutineCategory.ArmorSpikesSolo));

            // Enable ArmorSpikesTWF
            session.Character.RoutinePreferences.SetCategoryVisible(RoutineCategory.ArmorSpikesTWF, true);
            var updatedVisible = session.GetVisibleRoutines();
            Assert.IsTrue(updatedVisible.Any(r => r.Category == RoutineCategory.ArmorSpikesTWF));
            Assert.IsFalse(updatedVisible.Any(r => r.Category == RoutineCategory.OffHandShieldBash));

            // Disable ShieldBashSolo independently
            session.Character.RoutinePreferences.SetCategoryVisible(RoutineCategory.ShieldBashSolo, false);
            var withoutShieldSolo = session.GetVisibleRoutines();
            Assert.IsFalse(withoutShieldSolo.Any(r => r.Category == RoutineCategory.ShieldBashSolo));
            Assert.IsTrue(withoutShieldSolo.Any(r => r.Category == RoutineCategory.PrimarySolo));
        }
    }
}
