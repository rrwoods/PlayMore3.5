using System;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlayMore35.Global;
using PlayMore35.Items;

namespace PlayMore35.Tests {
    [TestClass]
    public class ConfigureAttackRoutinesDialogTests {
        [TestMethod]
        public void Dialog_InitializesWithCurrentPreferences() {
            var prefs = new AttackRoutinePreferences();
            prefs.SetCategoryVisible(RoutineCategory.OffHandShieldBash, true);
            prefs.SetCategoryVisible(RoutineCategory.PrimarySolo, false);

            using (var dialog = new ConfigureAttackRoutinesDialog(prefs)) {
                Assert.IsTrue(dialog.GetCategoryChecked(RoutineCategory.OffHandShieldBash));
                Assert.IsFalse(dialog.GetCategoryChecked(RoutineCategory.PrimarySolo));
                Assert.IsTrue(dialog.GetCategoryChecked(RoutineCategory.TwoWeaponFighting));
            }
        }

        [TestMethod]
        public void Dialog_SavePreferences_UpdatesModel() {
            var prefs = new AttackRoutinePreferences();
            using (var dialog = new ConfigureAttackRoutinesDialog(prefs)) {
                dialog.SetCategoryChecked(RoutineCategory.OffHandShieldBash, true);
                dialog.SetCategoryChecked(RoutineCategory.TwoWeaponFighting, false);
                dialog.SavePreferences();
            }

            Assert.IsTrue(prefs.IsCategoryVisible(RoutineCategory.OffHandShieldBash));
            Assert.IsFalse(prefs.IsCategoryVisible(RoutineCategory.TwoWeaponFighting));
        }

        [TestMethod]
        public void Dialog_ResetToDefaults_RestoresTabletopDefaults() {
            var prefs = new AttackRoutinePreferences();
            prefs.SetCategoryVisible(RoutineCategory.OffHandShieldBash, true);
            prefs.SetCategoryVisible(RoutineCategory.PrimarySolo, false);

            using (var dialog = new ConfigureAttackRoutinesDialog(prefs)) {
                dialog.ResetToDefaults();
                Assert.IsTrue(dialog.GetCategoryChecked(RoutineCategory.PrimarySolo));
                Assert.IsTrue(dialog.GetCategoryChecked(RoutineCategory.TwoWeaponFighting));
                Assert.IsTrue(dialog.GetCategoryChecked(RoutineCategory.DoubleWeapon));
                Assert.IsTrue(dialog.GetCategoryChecked(RoutineCategory.ShieldBashSolo));
                Assert.IsTrue(dialog.GetCategoryChecked(RoutineCategory.ArmorSpikesSolo));
                Assert.IsFalse(dialog.GetCategoryChecked(RoutineCategory.OffHandShieldBash));
                Assert.IsFalse(dialog.GetCategoryChecked(RoutineCategory.ArmorSpikesTWF));
            }
        }

        [TestMethod]
        public void Dialog_DisplayNames_AreHumanReadable() {
            Assert.AreEqual("Full Attack with Shield Bash", ConfigureAttackRoutinesDialog.GetCategoryDisplayName(RoutineCategory.OffHandShieldBash));
            Assert.AreEqual("Single Shield Bash", ConfigureAttackRoutinesDialog.GetCategoryDisplayName(RoutineCategory.ShieldBashSolo));

            foreach (RoutineCategory cat in (RoutineCategory[])Enum.GetValues(typeof(RoutineCategory))) {
                string name = ConfigureAttackRoutinesDialog.GetCategoryDisplayName(cat);
                Assert.IsFalse(string.IsNullOrWhiteSpace(name));
                Assert.AreNotEqual(cat.ToString(), name, $"Category {cat} should have a friendly display name");
            }
        }

        [TestMethod]
        public void LegacyMethods_PurgedFromCodebase() {
            // Acceptance criteria: Legacy methods Attack.Generate, Attack.GenerateMultiweaponFullAttacks,
            // and legacy Controller.GetAttacks() / GetFullAttacks() are purged.
            var attackType = typeof(Attack);
            Assert.IsNull(attackType.GetMethod("Generate", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
            Assert.IsNull(attackType.GetMethod("GenerateMultiweaponFullAttacks", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));

            var controllerType = Type.GetType("PlayMore35.Global.Controller, PlayMore35");
            Assert.IsNull(controllerType, "Controller type must be completely purged from assembly.");
        }

        [TestMethod]
        public void MainForm_HasOptionsConfigureAttackRoutinesMenu() {
            using (var form = new MainForm()) {
                Assert.IsNotNull(form.MainMenuStrip, "MainForm must have a MainMenuStrip");
                var optionsMenu = form.MainMenuStrip.Items.OfType<ToolStripMenuItem>().FirstOrDefault(i => i.Text == "Options");
                Assert.IsNotNull(optionsMenu, "MainMenuStrip must contain 'Options' menu item");
                var configItem = optionsMenu.DropDownItems.OfType<ToolStripMenuItem>().FirstOrDefault(i => i.Text == "Configure Attack Routines");
                Assert.IsNotNull(configItem, "'Options' menu must contain 'Configure Attack Routines' item");
            }
        }
    }
}
