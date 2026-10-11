using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Global;
using PlayMore35.Rules;

namespace PlayMore35.Tests {
    [TestClass]
    public class LevelDetailsDialogTests {
        private static string FindRepoRoot() {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Odric.json"))) {
                dir = dir.Parent;
            }
            Assert.IsNotNull(dir, "Could not locate Odric.json in repository root.");
            return dir.FullName;
        }

        private static ICharacterSession LoadOdricSession() {
            var catalog = new GameRulesCatalog();
            FormUtils.availableFeats = catalog.Feats.Keys.OrderBy(x => x).ToList();
            var odricPath = Path.Combine(FindRepoRoot(), "Odric.json");
            var character = new Character(JObject.Parse(File.ReadAllText(odricPath)));
            return new CharacterSession(character, catalog);
        }

        private static void SimulateLevelDetailsClickAndClose(LevelDetailsDialog dialog, int levelIndex) {
            dialog.PrepareLevel(levelIndex);
            dialog.SaveChanges();
        }

        [TestMethod]
        public void Odric_ClickLevel4ThenOtherLevels_DoesNotShowWeaponFocusChoiceOnOtherLevels() {
            var session = LoadOdricSession();
            using (var dialog = new LevelDetailsDialog(session)) {
                // Click level 4 (index 3) first
                SimulateLevelDetailsClickAndClose(dialog, 3);

                // Now check levels 1, 2, and 3 (indices 0, 1, 2)
                for (int otherLevelIndex = 0; otherLevelIndex <= 2; otherLevelIndex++) {
                    dialog.PrepareLevel(otherLevelIndex);

                    var choiceLabels = dialog.Controls.OfType<Label>()
                        .Where(lbl => lbl.Text.StartsWith("Choose "))
                        .ToList();

                    Assert.AreEqual(
                        0,
                        choiceLabels.Count,
                        $"Level {otherLevelIndex + 1} should have no feat choice prompts in Controls, but found {choiceLabels.Count}: {string.Join(", ", choiceLabels.Select(l => l.Text))}"
                    );
                    Assert.AreEqual(
                        0,
                        dialog.GetFeatChoices().Count,
                        $"Level {otherLevelIndex + 1} should report 0 feat choices."
                    );
                }
            }
        }

        [TestMethod]
        public void Odric_ClickLevels1To3ThenLevel4_PreservesWeaponFocusChoiceForLevel4() {
            for (int firstLevelIndex = 0; firstLevelIndex <= 2; firstLevelIndex++) {
                var session = LoadOdricSession();
                using (var dialog = new LevelDetailsDialog(session)) {
                    // Click level 1, 2, or 3 first
                    SimulateLevelDetailsClickAndClose(dialog, firstLevelIndex);

                    // Now open level 4 (index 3)
                    dialog.PrepareLevel(3);

                    var featChoices = dialog.GetFeatChoices();
                    Assert.IsTrue(
                        featChoices.TryGetValue("Fighter 2", out var weaponChoice),
                        $"Expected 'Fighter 2' feat choice to be present when opening level 4 after level {firstLevelIndex + 1}."
                    );
                    Assert.AreEqual(
                        "dagger",
                        weaponChoice,
                        $"Expected Weapon Focus choice 'dagger' to be preserved on level 4 after opening level {firstLevelIndex + 1}."
                    );
                }
            }
        }

        private static ICharacterSession CreateMinimalTwoLevelSession() {
            var catalog = new GameRulesCatalog();
            FormUtils.availableFeats = catalog.Feats.Keys.OrderBy(x => x).ToList();
            var character = new Character();
            character.classLevels = new System.Collections.Generic.List<string> { "Fighter", "Fighter" };
            character.feats["Fighter 1"] = "Endurance";
            character.feats["Fighter 2"] = "Weapon Focus";
            character.featChoices["Fighter 2"] = "dagger";
            return new CharacterSession(character, catalog);
        }

        [TestMethod]
        public void Minimal_PrepareLevelWithChoiceThenPrepareLevelWithoutChoice_ClearsChoiceControls() {
            var session = CreateMinimalTwoLevelSession();
            using (var dialog = new LevelDetailsDialog(session)) {
                dialog.PrepareLevel(1);
                dialog.PrepareLevel(0);

                var choiceLabels = dialog.Controls.OfType<Label>()
                    .Where(lbl => lbl.Text.StartsWith("Choose "))
                    .ToList();

                Assert.AreEqual(
                    0,
                    choiceLabels.Count,
                    $"Level 1 has no feat with a choice, but dialog still contains: {string.Join(", ", choiceLabels.Select(l => l.Text))}"
                );
                Assert.AreEqual(0, dialog.GetFeatChoices().Count);
            }
        }

        [TestMethod]
        public void Minimal_ClickLevelWithoutChoiceThenLevelWithChoice_PreservesOtherLevelFeatChoices() {
            var session = CreateMinimalTwoLevelSession();
            using (var dialog = new LevelDetailsDialog(session)) {
                SimulateLevelDetailsClickAndClose(dialog, 0);
                dialog.PrepareLevel(1);

                Assert.IsTrue(
                    session.Character.featChoices.TryGetValue("Fighter 2", out var choiceInSession),
                    "Expected 'Fighter 2' feat choice to remain in Character.featChoices after closing Level 1 dialog."
                );
                Assert.AreEqual("dagger", choiceInSession);

                var dialogChoices = dialog.GetFeatChoices();
                Assert.AreEqual("dagger", dialogChoices["Fighter 2"]);
            }
        }

        [TestMethod]
        public void Minimal_ReplaceFeatWithChoiceWithFeatWithoutChoice_RemovesStaleFeatChoiceForThatSlot() {
            var session = CreateMinimalTwoLevelSession();
            using (var dialog = new LevelDetailsDialog(session)) {
                dialog.PrepareLevel(1);

                var fighter2Combo = dialog.Controls.OfType<ComboBox>()
                    .First(cbo => Equals(cbo.Tag, "Fighter 2") && cbo.Items.Contains("Endurance"));
                fighter2Combo.SelectedItem = "Endurance";

                dialog.SaveChanges();

                Assert.AreEqual("Endurance", session.Character.feats["Fighter 2"]);
                Assert.IsFalse(
                    session.Character.featChoices.ContainsKey("Fighter 2"),
                    "Changing 'Fighter 2' from Weapon Focus to Endurance should remove its stale feat choice."
                );
            }
        }
    }
}
