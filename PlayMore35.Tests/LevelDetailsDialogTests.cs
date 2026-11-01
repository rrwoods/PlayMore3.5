using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Global;
using PlayMore35.Rules;
using PlayMore35.Subsystems;
using PlayMore35.UI.Subsystems;

namespace PlayMore35.Tests {
    [TestClass]
    public class LevelDetailsDialogTests {
        private static ICharacterSession LoadOdricSession() {
            var catalog = new GameRulesCatalog();
            FormUtils.availableFeats = catalog.Feats.Keys.OrderBy(x => x).ToList();
            var odricPath = TestEnvironment.FindOdricJsonPath();
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

        private static ICharacterSession CreateAdeptSession(params string[] classes) {
            var catalog = new GameRulesCatalog();
            FormUtils.availableFeats = catalog.Feats.Keys.OrderBy(x => x).ToList();
            var character = new Character {
                race = "human",
                classLevels = classes.ToList()
            };
            return new CharacterSession(character, catalog);
        }

        [TestMethod]
        public void MartialStudy_ZeroAdeptClasses_AutoSelectsFeats_NoClassDropdown() {
            // Level 1 Fighter (0 adept classes possessed)
            var session = CreateAdeptSession("Fighter");
            using (var dialog = new LevelDetailsDialog(session)) {
                dialog.PrepareLevel(0);

                var featCombo = dialog.Controls.OfType<ComboBox>()
                    .First(cbo => Equals(cbo.Tag, "Level 1"));
                featCombo.SelectedItem = "Martial Study";

                // Class dropdown should not be rendered
                var classCombos = dialog.Controls.OfType<ComboBox>()
                    .Where(cbo => Equals(cbo.Tag, "Level 1:class"))
                    .ToList();
                Assert.AreEqual(0, classCombos.Count, "No class dropdown should be rendered when 0 adept classes exist.");

                // Maneuver receptor should be rendered
                var receptor = dialog.Controls.OfType<ElementReceptorControl>()
                    .First(r => Equals(r.Tag, "Level 1:maneuver"));
                Assert.AreEqual(ReceptorState.Unmade, receptor.State);

                // Clicking receptor activates picker
                receptor.PerformClick();
                var pickerHost = dialog.PickerHost;
                Assert.IsTrue(pickerHost.Height > 0);
                var picker = pickerHost.Controls.OfType<DefaultElementPicker>().First();
                var names = picker.GetFilteredElementNames();
                Assert.IsTrue(names.Contains("Steel Wind"));
                Assert.IsFalse(names.Contains("Flame's Blessing"));

                picker.SelectElementByName("Steel Wind");
                dialog.SaveChanges();

                Assert.AreEqual("Feats", session.Character.featChoices["Level 1:class"]);
                Assert.AreEqual("Steel Wind", session.Character.featChoices["Level 1:maneuver"]);
            }
        }

        [TestMethod]
        public void MartialStudy_SingleAdeptClass_AutoSelectsAdeptClass_NoClassDropdown() {
            // Level 3 character with Fighter 1, Warblade 2 (1 adept class possessed)
            var session = CreateAdeptSession("Fighter", "Warblade", "Warblade");
            using (var dialog = new LevelDetailsDialog(session)) {
                dialog.PrepareLevel(2);

                var featCombo = dialog.Controls.OfType<ComboBox>()
                    .First(cbo => Equals(cbo.Tag, "Level 3"));
                featCombo.SelectedItem = "Martial Study";

                var classCombos = dialog.Controls.OfType<ComboBox>()
                    .Where(cbo => Equals(cbo.Tag, "Level 3:class"))
                    .ToList();
                Assert.AreEqual(0, classCombos.Count, "No class dropdown should be rendered when 1 adept class exists.");

                var receptor = dialog.Controls.OfType<ElementReceptorControl>()
                    .First(r => Equals(r.Tag, "Level 3:maneuver"));
                receptor.PerformClick();
                var pickerHost = dialog.Controls.OfType<Panel>().First(p => p.Name == "pnlPickerHost");
                var picker = pickerHost.Controls.OfType<DefaultElementPicker>().First();
                picker.SelectElementByName("Steel Wind");
                dialog.SaveChanges();

                Assert.AreEqual("Warblade", session.Character.featChoices["Level 3:class"]);
                Assert.AreEqual("Steel Wind", session.Character.featChoices["Level 3:maneuver"]);
            }
        }

        [TestMethod]
        public void MartialStudy_TwoOrMoreAdeptClasses_RendersClassDropdown_ContainsOnlyEligibleAdeptClasses() {
            // Level 3 character with Warblade 1, Crusader 2 (2 adept classes possessed)
            var session = CreateAdeptSession("Warblade", "Crusader", "Crusader");
            using (var dialog = new LevelDetailsDialog(session)) {
                dialog.PrepareLevel(2);

                var featCombo = dialog.Controls.OfType<ComboBox>()
                    .First(cbo => Equals(cbo.Tag, "Level 3"));
                featCombo.SelectedItem = "Martial Study";

                var classCombo = dialog.Controls.OfType<ComboBox>()
                    .FirstOrDefault(cbo => Equals(cbo.Tag, "Level 3:class"));
                Assert.IsNotNull(classCombo, "Class dropdown should be rendered when 2+ adept classes exist.");
                CollectionAssert.AreEquivalent(
                    new[] { "Crusader", "Warblade" },
                    classCombo.Items.Cast<string>().ToList(),
                    "Class dropdown should contain only the eligible adept classes (and not 'Feats')."
                );

                classCombo.SelectedItem = "Crusader";
                var receptor = dialog.Controls.OfType<ElementReceptorControl>()
                    .First(r => Equals(r.Tag, "Level 3:maneuver"));
                receptor.PerformClick();
                var pickerHost = dialog.Controls.OfType<Panel>().First(p => p.Name == "pnlPickerHost");
                var picker = pickerHost.Controls.OfType<DefaultElementPicker>().First();
                picker.SelectElementByName("Crusader's Strike");

                dialog.SaveChanges();

                Assert.AreEqual("Crusader", session.Character.featChoices["Level 3:class"]);
                Assert.AreEqual("Crusader's Strike", session.Character.featChoices["Level 3:maneuver"]);
            }
        }

        [TestMethod]
        public void MartialStudy_ExceedsMaxManeuverLevel_DisplaysAdvisoryWarningBadge() {
            // Level 1 Fighter: IL = 1 for Feats, max maneuver level = 1
            var session = CreateAdeptSession("Fighter");
            using (var dialog = new LevelDetailsDialog(session)) {
                dialog.PrepareLevel(0);

                var featCombo = dialog.Controls.OfType<ComboBox>()
                    .First(cbo => Equals(cbo.Tag, "Level 1"));
                featCombo.SelectedItem = "Martial Study";

                var receptor = dialog.Controls.OfType<ElementReceptorControl>()
                    .First(r => Equals(r.Tag, "Level 1:maneuver"));
                receptor.PerformClick();
                var pickerHost = dialog.Controls.OfType<Panel>().First(p => p.Name == "pnlPickerHost");
                var picker = pickerHost.Controls.OfType<DefaultElementPicker>().First();

                // Disarming Strike is level 2
                var elem = session.Catalog.GetSubsystem("martial_maneuvers").Elements["Disarming Strike"];
                receptor.SelectedElement = elem;
                // Deliver via element delivered or simulate choice delivery
                receptor.PerformClick(); // active
                picker.SelectElementByName("Disarming Strike");

                Assert.IsTrue(receptor.WarningBadgeVisible, "Warning badge should be visible when maneuver level exceeds max level.");
                StringAssert.Contains(receptor.WarningMessage, "exceeds maximum maneuver level");

                // Switch to level 1 maneuver (Steel Wind)
                picker.SelectElementByName("Steel Wind");
                Assert.IsFalse(receptor.WarningBadgeVisible, "Warning badge should not be visible when selection is valid.");
            }
        }

        [TestMethod]
        public void MartialStudy_UnmetDisciplinePrerequisite_DisplaysAdvisoryWarningBadge() {
            // Level 3 Swordsage (IL = 3, max maneuver level = 2, 0 Desert Wind maneuvers known)
            var session = CreateAdeptSession("Swordsage", "Swordsage", "Swordsage");
            using (var dialog = new LevelDetailsDialog(session)) {
                dialog.PrepareLevel(2);

                var featCombo = dialog.Controls.OfType<ComboBox>()
                    .First(cbo => Equals(cbo.Tag, "Level 3"));
                featCombo.SelectedItem = "Martial Study";

                var receptor = dialog.Controls.OfType<ElementReceptorControl>()
                    .First(r => Equals(r.Tag, "Level 3:maneuver"));
                receptor.PerformClick();
                var pickerHost = dialog.Controls.OfType<Panel>().First(p => p.Name == "pnlPickerHost");
                var picker = pickerHost.Controls.OfType<DefaultElementPicker>().First();
                // Flashing Sun is level 2, requires 1 Desert Wind maneuver
                picker.SelectElementByName("Flashing Sun");

                Assert.IsTrue(receptor.WarningBadgeVisible);
                StringAssert.Contains(receptor.WarningMessage, "Requires 1 Desert Wind maneuver, but only 0 known.");
            }
        }

        [TestMethod]
        public void MartialStudy_PrerequisiteMetByEarlierManeuver_NoWarningBadge() {
            // Level 3 Swordsage who already took Burning Blade at Level 1
            var session = CreateAdeptSession("Swordsage", "Swordsage", "Swordsage");
            session.Mutate(c => {
                c.feats["Level 1"] = "Martial Study";
                c.featChoices["Level 1:class"] = "Swordsage";
                c.featChoices["Level 1:maneuver"] = "Burning Blade";
            });

            using (var dialog = new LevelDetailsDialog(session)) {
                dialog.PrepareLevel(2);

                var featCombo = dialog.Controls.OfType<ComboBox>()
                    .First(cbo => Equals(cbo.Tag, "Level 3"));
                featCombo.SelectedItem = "Martial Study";

                var receptor = dialog.Controls.OfType<ElementReceptorControl>()
                    .First(r => Equals(r.Tag, "Level 3:maneuver"));
                receptor.PerformClick();
                var pickerHost = dialog.Controls.OfType<Panel>().First(p => p.Name == "pnlPickerHost");
                var picker = pickerHost.Controls.OfType<DefaultElementPicker>().First();
                picker.SelectElementByName("Flashing Sun");

                Assert.IsFalse(receptor.WarningBadgeVisible, "Warning badge should be hidden when prerequisite is met by earlier maneuver.");
            }
        }

        [TestMethod]
        public void MartialStudy_ReplaceFeatWithOtherFeat_RemovesStaleCompositeChoices() {
            var session = CreateAdeptSession("Fighter");
            session.Mutate(c => {
                c.feats["Level 1"] = "Martial Study";
                c.featChoices["Level 1:class"] = "Feats";
                c.featChoices["Level 1:maneuver"] = "Steel Wind";
            });

            using (var dialog = new LevelDetailsDialog(session)) {
                dialog.PrepareLevel(0);

                var featCombo = dialog.Controls.OfType<ComboBox>()
                    .First(cbo => Equals(cbo.Tag, "Level 1"));
                featCombo.SelectedItem = "Endurance";

                dialog.SaveChanges();

                Assert.AreEqual("Endurance", session.Character.feats["Level 1"]);
                Assert.IsFalse(session.Character.featChoices.ContainsKey("Level 1:class"));
                Assert.IsFalse(session.Character.featChoices.ContainsKey("Level 1:maneuver"));
            }
        }

        [TestMethod]
        public void MartialStance_ZeroAdeptClasses_AutoSelectsFeats_NoClassDropdown() {
            // Level 1 Fighter (0 adept classes possessed)
            var session = CreateAdeptSession("Fighter");
            using (var dialog = new LevelDetailsDialog(session)) {
                dialog.PrepareLevel(0);

                var featCombo = dialog.Controls.OfType<ComboBox>()
                    .First(cbo => Equals(cbo.Tag, "Level 1"));
                featCombo.SelectedItem = "Martial Stance";

                // Class dropdown should not be rendered
                var classCombos = dialog.Controls.OfType<ComboBox>()
                    .Where(cbo => Equals(cbo.Tag, "Level 1:class"))
                    .ToList();
                Assert.AreEqual(0, classCombos.Count, "No class dropdown should be rendered when 0 adept classes exist.");

                // Stance receptor should be rendered
                var receptor = dialog.Controls.OfType<ElementReceptorControl>()
                    .First(r => Equals(r.Tag, "Level 1:stance"));
                receptor.PerformClick();
                var pickerHost = dialog.Controls.OfType<Panel>().First(p => p.Name == "pnlPickerHost");
                var picker = pickerHost.Controls.OfType<DefaultElementPicker>().First();
                var names = picker.GetFilteredElementNames();
                Assert.IsTrue(names.Contains("Punishing Stance"));
                Assert.IsFalse(names.Contains("Steel Wind"));

                picker.SelectElementByName("Punishing Stance");
                dialog.SaveChanges();

                Assert.AreEqual("Feats", session.Character.featChoices["Level 1:class"]);
                Assert.AreEqual("Punishing Stance", session.Character.featChoices["Level 1:stance"]);
            }
        }

        [TestMethod]
        public void MartialStance_SingleAdeptClass_AutoSelectsAdeptClass_NoClassDropdown() {
            // Level 3 character with Fighter 1, Warblade 2 (1 adept class possessed)
            var session = CreateAdeptSession("Fighter", "Warblade", "Warblade");
            using (var dialog = new LevelDetailsDialog(session)) {
                dialog.PrepareLevel(2);

                var featCombo = dialog.Controls.OfType<ComboBox>()
                    .First(cbo => Equals(cbo.Tag, "Level 3"));
                featCombo.SelectedItem = "Martial Stance";

                var classCombos = dialog.Controls.OfType<ComboBox>()
                    .Where(cbo => Equals(cbo.Tag, "Level 3:class"))
                    .ToList();
                Assert.AreEqual(0, classCombos.Count, "No class dropdown should be rendered when 1 adept class exists.");

                var receptor = dialog.Controls.OfType<ElementReceptorControl>()
                    .First(r => Equals(r.Tag, "Level 3:stance"));
                receptor.PerformClick();
                var pickerHost = dialog.Controls.OfType<Panel>().First(p => p.Name == "pnlPickerHost");
                var picker = pickerHost.Controls.OfType<DefaultElementPicker>().First();
                picker.SelectElementByName("Punishing Stance");
                dialog.SaveChanges();

                Assert.AreEqual("Warblade", session.Character.featChoices["Level 3:class"]);
                Assert.AreEqual("Punishing Stance", session.Character.featChoices["Level 3:stance"]);
            }
        }

        [TestMethod]
        public void MartialStance_TwoOrMoreAdeptClasses_RendersClassDropdown_ContainsOnlyEligibleAdeptClasses() {
            // Level 3 character with Warblade 1, Crusader 2 (2 adept classes possessed)
            var session = CreateAdeptSession("Warblade", "Crusader", "Crusader");
            using (var dialog = new LevelDetailsDialog(session)) {
                dialog.PrepareLevel(2);

                var featCombo = dialog.Controls.OfType<ComboBox>()
                    .First(cbo => Equals(cbo.Tag, "Level 3"));
                featCombo.SelectedItem = "Martial Stance";

                var classCombo = dialog.Controls.OfType<ComboBox>()
                    .FirstOrDefault(cbo => Equals(cbo.Tag, "Level 3:class"));
                Assert.IsNotNull(classCombo, "Class dropdown should be rendered when 2+ adept classes exist.");
                CollectionAssert.AreEquivalent(
                    new[] { "Crusader", "Warblade" },
                    classCombo.Items.Cast<string>().ToList(),
                    "Class dropdown should contain only the eligible adept classes (and not 'Feats')."
                );

                classCombo.SelectedItem = "Crusader";
                var receptor = dialog.Controls.OfType<ElementReceptorControl>()
                    .First(r => Equals(r.Tag, "Level 3:stance"));
                receptor.PerformClick();
                var pickerHost = dialog.Controls.OfType<Panel>().First(p => p.Name == "pnlPickerHost");
                var picker = pickerHost.Controls.OfType<DefaultElementPicker>().First();
                picker.SelectElementByName("Iron Guard's Glare");

                dialog.SaveChanges();

                Assert.AreEqual("Crusader", session.Character.featChoices["Level 3:class"]);
                Assert.AreEqual("Iron Guard's Glare", session.Character.featChoices["Level 3:stance"]);
            }
        }

        [TestMethod]
        public void MartialStance_ExceedsMaxManeuverLevel_DisplaysAdvisoryWarningBadge() {
            // Level 1 Fighter: IL = 1 for Feats, max maneuver level = 1
            var session = CreateAdeptSession("Fighter");
            using (var dialog = new LevelDetailsDialog(session)) {
                dialog.PrepareLevel(0);

                var featCombo = dialog.Controls.OfType<ComboBox>()
                    .First(cbo => Equals(cbo.Tag, "Level 1"));
                featCombo.SelectedItem = "Martial Stance";

                var receptor = dialog.Controls.OfType<ElementReceptorControl>()
                    .First(r => Equals(r.Tag, "Level 1:stance"));
                receptor.PerformClick();
                var pickerHost = dialog.Controls.OfType<Panel>().First(p => p.Name == "pnlPickerHost");
                var picker = pickerHost.Controls.OfType<DefaultElementPicker>().First();

                // Giant's Stance is level 5
                picker.SelectElementByName("Giant's Stance");

                Assert.IsTrue(receptor.WarningBadgeVisible, "Warning badge should be visible when stance level exceeds max level.");
                StringAssert.Contains(receptor.WarningMessage, "exceeds maximum maneuver level");
            }
        }

        [TestMethod]
        public void MartialStance_NoKnownDisciplineManeuvers_DisplaysAdvisoryWarningBadge() {
            // Level 3 Swordsage with 0 Devoted Spirit maneuvers known
            var session = CreateAdeptSession("Swordsage", "Swordsage", "Swordsage");
            using (var dialog = new LevelDetailsDialog(session)) {
                dialog.PrepareLevel(2);

                var featCombo = dialog.Controls.OfType<ComboBox>()
                    .First(cbo => Equals(cbo.Tag, "Level 3"));
                featCombo.SelectedItem = "Martial Stance";

                var receptor = dialog.Controls.OfType<ElementReceptorControl>()
                    .First(r => Equals(r.Tag, "Level 3:stance"));
                receptor.PerformClick();
                var pickerHost = dialog.Controls.OfType<Panel>().First(p => p.Name == "pnlPickerHost");
                var picker = pickerHost.Controls.OfType<DefaultElementPicker>().First();
                // Iron Guard's Glare is Devoted Spirit level 1 (0 prereqs in discipline, but Martial Stance requires knowing a maneuver in the discipline)
                picker.SelectElementByName("Iron Guard's Glare");

                Assert.IsTrue(receptor.WarningBadgeVisible);
                StringAssert.Contains(receptor.WarningMessage, "Requires 1 Devoted Spirit maneuver, but only 0 known.");
            }
        }

        [TestMethod]
        public void MartialStance_PrerequisiteMetByEarlierManeuver_NoWarningBadge() {
            // Level 3 Crusader who already has Crusader's Strike (Devoted Spirit)
            var session = CreateAdeptSession("Crusader", "Crusader", "Crusader");
            var engine = (SubsystemEngine)session.GetSubsystem("martial_maneuvers");
            engine.AssignSlot("maneuvers", 0, "Crusader's Strike", source: "Crusader");

            using (var dialog = new LevelDetailsDialog(session)) {
                dialog.PrepareLevel(2);

                var featCombo = dialog.Controls.OfType<ComboBox>()
                    .First(cbo => Equals(cbo.Tag, "Level 3"));
                featCombo.SelectedItem = "Martial Stance";

                var receptor = dialog.Controls.OfType<ElementReceptorControl>()
                    .First(r => Equals(r.Tag, "Level 3:stance"));
                receptor.PerformClick();
                var pickerHost = dialog.Controls.OfType<Panel>().First(p => p.Name == "pnlPickerHost");
                var picker = pickerHost.Controls.OfType<DefaultElementPicker>().First();
                picker.SelectElementByName("Iron Guard's Glare");

                Assert.IsFalse(receptor.WarningBadgeVisible, "Warning badge should be hidden when discipline maneuver is known.");
            }
        }

        [TestMethod]
        public void MartialStance_ReplaceFeatWithOtherFeat_RemovesStaleCompositeChoices() {
            var session = CreateAdeptSession("Fighter");
            session.Mutate(c => {
                c.feats["Level 1"] = "Martial Stance";
                c.featChoices["Level 1:class"] = "Feats";
                c.featChoices["Level 1:stance"] = "Bolstering Voice";
            });

            using (var dialog = new LevelDetailsDialog(session)) {
                dialog.PrepareLevel(0);

                var featCombo = dialog.Controls.OfType<ComboBox>()
                    .First(cbo => Equals(cbo.Tag, "Level 1"));
                featCombo.SelectedItem = "Endurance";

                dialog.SaveChanges();

                Assert.AreEqual("Endurance", session.Character.feats["Level 1"]);
                Assert.IsFalse(session.Character.featChoices.ContainsKey("Level 1:class"));
                Assert.IsFalse(session.Character.featChoices.ContainsKey("Level 1:stance"));
            }
        }

        [TestMethod]
        public void DialogValidation_PendingChoicesAcrossSlotsAtSameLevel_SatisfiesPrerequisitesWithoutSpuriousWarning() {
            var session = CreateAdeptSession("Fighter");

            using (var dialog = new LevelDetailsDialog(session)) {
                dialog.PrepareLevel(0);

                // Slot "Human": select Martial Study -> Burning Blade (Desert Wind, 0 prereq)
                var humanFeatCombo = dialog.Controls.OfType<ComboBox>()
                    .First(cbo => Equals(cbo.Tag, "Human"));
                humanFeatCombo.SelectedItem = "Martial Study";

                var humanReceptor = dialog.Controls.OfType<ElementReceptorControl>()
                    .First(r => Equals(r.Tag, "Human:maneuver"));
                humanReceptor.PerformClick();
                var pickerHost = dialog.Controls.OfType<Panel>().First(p => p.Name == "pnlPickerHost");
                var humanPicker = pickerHost.Controls.OfType<DefaultElementPicker>().First();
                humanPicker.SelectElementByName("Burning Blade");

                // Slot "Level 1": select Martial Stance -> Flame's Blessing (Desert Wind, requires 1 Desert Wind maneuver)
                var level1FeatCombo = dialog.Controls.OfType<ComboBox>()
                    .First(cbo => Equals(cbo.Tag, "Level 1"));
                level1FeatCombo.SelectedItem = "Martial Stance";

                var level1Receptor = dialog.Controls.OfType<ElementReceptorControl>()
                    .First(r => Equals(r.Tag, "Level 1:stance"));
                level1Receptor.PerformClick();
                var level1Picker = pickerHost.Controls.OfType<DefaultElementPicker>().First();
                level1Picker.SelectElementByName("Flame's Blessing");

                // Prerequisite of 1 Desert Wind maneuver for Flame's Blessing is met by pending choice in Human slot
                Assert.IsFalse(level1Receptor.WarningBadgeVisible, "Warning badge on Level 1 should be hidden because Human slot selected Burning Blade.");

                // If Human slot changes to non-Desert Wind maneuver (e.g. Steel Wind from Iron Heart)
                humanReceptor.PerformClick();
                var switchHumanPicker = pickerHost.Controls.OfType<DefaultElementPicker>().First();
                switchHumanPicker.SelectElementByName("Steel Wind");
                Assert.IsTrue(level1Receptor.WarningBadgeVisible, "Warning badge on Level 1 should appear because Desert Wind prerequisite is no longer met.");
                StringAssert.Contains(level1Receptor.WarningMessage, "Requires 1 Desert Wind maneuver, but only 0 known.");

                // If Human slot switches back to Burning Blade, warning badge clears immediately
                switchHumanPicker.SelectElementByName("Burning Blade");
                Assert.IsFalse(level1Receptor.WarningBadgeVisible, "Warning badge on Level 1 should clear when prerequisite is restored.");
            }
        }

        private static (ICharacterSession session, Feat syntheticFeat) CreateSyntheticFeatSession(Feat syntheticFeat, params string[] classes) {
            var innerCatalog = new GameRulesCatalog();
            var fakeCatalog = new FakeRulesCatalog(inner: innerCatalog);
            string featName = syntheticFeat.Benefit.Name;
            fakeCatalog.FeatsMap[featName] = syntheticFeat;
            foreach (var kvp in innerCatalog.Feats) {
                if (!fakeCatalog.FeatsMap.ContainsKey(kvp.Key)) {
                    fakeCatalog.FeatsMap[kvp.Key] = kvp.Value;
                }
            }
            FormUtils.availableFeats = fakeCatalog.Feats.Keys.OrderBy(x => x).ToList();
            var character = new Character {
                race = "human",
                classLevels = classes.ToList()
            };
            var session = new CharacterSession(character, fakeCatalog);
            return (session, syntheticFeat);
        }

        private static Feat CreateSyntheticClassTargetedFeat(string featName = "Synthetic Adept Feat", string filter = "Adept", string fallback = "Feats") {
            return new Feat(new JObject(
                new JProperty("benefit", new JObject(new JProperty("name", featName))),
                new JProperty("choices", new JArray(
                    new JObject(
                        new JProperty("id", "class"),
                        new JProperty("label", "Target Class"),
                        new JProperty("domain", "CharacterClasses"),
                        new JProperty("filter", filter),
                        new JProperty("fallback", fallback)
                    )
                ))
            ));
        }

        [TestMethod]
        public void SyntheticClassTargetedFeat_ZeroEligibleClasses_AutoSelectsFallback_NoClassDropdown() {
            var syntheticFeat = CreateSyntheticClassTargetedFeat();
            var (session, _) = CreateSyntheticFeatSession(syntheticFeat, "Fighter");

            using (var dialog = new LevelDetailsDialog(session)) {
                dialog.PrepareLevel(0);

                var featCombo = dialog.Controls.OfType<ComboBox>()
                    .First(cbo => Equals(cbo.Tag, "Level 1"));
                featCombo.SelectedItem = syntheticFeat.Benefit.Name;

                // Class dropdown should not be rendered
                var classCombos = dialog.Controls.OfType<ComboBox>()
                    .Where(cbo => Equals(cbo.Tag, "Level 1:class"))
                    .ToList();
                Assert.AreEqual(0, classCombos.Count, "No class dropdown should be rendered when 0 eligible classes exist.");

                dialog.SaveChanges();
                Assert.AreEqual("Feats", session.Character.featChoices["Level 1:class"]);
            }
        }

        [TestMethod]
        public void SyntheticClassTargetedFeat_SingleEligibleClass_AutoSelectsClass_NoClassDropdown() {
            var syntheticFeat = CreateSyntheticClassTargetedFeat();
            var (session, _) = CreateSyntheticFeatSession(syntheticFeat, "Fighter", "Warblade", "Warblade");

            using (var dialog = new LevelDetailsDialog(session)) {
                dialog.PrepareLevel(2);

                var featCombo = dialog.Controls.OfType<ComboBox>()
                    .First(cbo => Equals(cbo.Tag, "Level 3"));
                featCombo.SelectedItem = syntheticFeat.Benefit.Name;

                var classCombos = dialog.Controls.OfType<ComboBox>()
                    .Where(cbo => Equals(cbo.Tag, "Level 3:class"))
                    .ToList();
                Assert.AreEqual(0, classCombos.Count, "No class dropdown should be rendered when 1 eligible class exists.");

                dialog.SaveChanges();
                Assert.AreEqual("Warblade", session.Character.featChoices["Level 3:class"]);
            }
        }

        [TestMethod]
        public void SyntheticClassTargetedFeat_TwoOrMoreEligibleClasses_RendersClassDropdown_ContainsOnlyEligibleClasses() {
            var syntheticFeat = CreateSyntheticClassTargetedFeat();
            var (session, _) = CreateSyntheticFeatSession(syntheticFeat, "Warblade", "Crusader", "Crusader");

            using (var dialog = new LevelDetailsDialog(session)) {
                dialog.PrepareLevel(2);

                var featCombo = dialog.Controls.OfType<ComboBox>()
                    .First(cbo => Equals(cbo.Tag, "Level 3"));
                featCombo.SelectedItem = syntheticFeat.Benefit.Name;

                var classCombo = dialog.Controls.OfType<ComboBox>()
                    .FirstOrDefault(cbo => Equals(cbo.Tag, "Level 3:class"));
                Assert.IsNotNull(classCombo, "Class dropdown should be rendered when 2+ eligible classes exist.");
                CollectionAssert.AreEquivalent(
                    new[] { "Crusader", "Warblade" },
                    classCombo.Items.Cast<string>().ToList(),
                    "Class dropdown should contain only the eligible classes."
                );

                classCombo.SelectedItem = "Crusader";
                dialog.SaveChanges();

                Assert.AreEqual("Crusader", session.Character.featChoices["Level 3:class"]);
            }
        }

        [TestMethod]
        public void SyntheticClassTargetedFeat_ExistingChoicePreserved_InitializesSelection() {
            var syntheticFeat = CreateSyntheticClassTargetedFeat();
            var (session, _) = CreateSyntheticFeatSession(syntheticFeat, "Warblade", "Crusader", "Crusader");
            session.Mutate(c => {
                c.feats["Level 3"] = syntheticFeat.Benefit.Name;
                c.featChoices["Level 3:class"] = "Crusader";
            });

            using (var dialog = new LevelDetailsDialog(session)) {
                dialog.PrepareLevel(2);

                var classCombo = dialog.Controls.OfType<ComboBox>()
                    .FirstOrDefault(cbo => Equals(cbo.Tag, "Level 3:class"));
                Assert.IsNotNull(classCombo, "Class dropdown should be rendered when 2+ eligible classes exist.");
                Assert.AreEqual("Crusader", classCombo.SelectedItem, "Saved class choice should be selected on load.");
            }
        }

        private static Feat CreateSyntheticSubsystemFeat(
            string featName = "Synthetic Subsystem Feat", 
            string elementFilter = "maneuver", 
            string elementId = "maneuver", 
            string elementLabel = "Choose maneuver") 
        {
            return new Feat(new JObject(
                new JProperty("benefit", new JObject(new JProperty("name", featName))),
                new JProperty("choices", new JArray(
                    new JObject(
                        new JProperty("id", "class"),
                        new JProperty("label", "Target Class"),
                        new JProperty("domain", "CharacterClasses"),
                        new JProperty("filter", "Adept"),
                        new JProperty("fallback", "Feats")
                    ),
                    new JObject(
                        new JProperty("id", elementId),
                        new JProperty("label", elementLabel),
                        new JProperty("domain", "SubsystemElements"),
                        new JProperty("filter", elementFilter),
                        new JProperty("dependsOn", "class")
                    )
                ))
            ));
        }

        [TestMethod]
        public void SyntheticSubsystemFeat_DialogRendersDependentMultiChoiceControls_AndSavesChoices() {
            var syntheticFeat = CreateSyntheticSubsystemFeat();
            var (session, _) = CreateSyntheticFeatSession(syntheticFeat, "Fighter");

            using (var dialog = new LevelDetailsDialog(session)) {
                dialog.PrepareLevel(0);

                var featCombo = dialog.Controls.OfType<ComboBox>()
                    .First(cbo => Equals(cbo.Tag, "Level 1"));
                featCombo.SelectedItem = syntheticFeat.Benefit.Name;

                // 1. Class dropdown should not be rendered (0 adept classes -> auto-resolves to Feats)
                var classCombos = dialog.Controls.OfType<ComboBox>()
                    .Where(cbo => Equals(cbo.Tag, "Level 1:class"))
                    .ToList();
                Assert.AreEqual(0, classCombos.Count, "No class dropdown should be rendered when 0 adept classes exist.");

                // 2. Maneuver receptor should be rendered
                var receptor = dialog.Controls.OfType<ElementReceptorControl>()
                    .FirstOrDefault(r => Equals(r.Tag, "Level 1:maneuver"));
                Assert.IsNotNull(receptor, "Maneuver receptor should be rendered for SubsystemElements choice.");

                receptor.PerformClick();
                var pickerHost = dialog.Controls.OfType<Panel>().First(p => p.Name == "pnlPickerHost");
                var picker = pickerHost.Controls.OfType<DefaultElementPicker>().First();
                Assert.IsTrue(picker.GetFilteredElementNames().Contains("Steel Wind"));

                // Valid choice: Level 1 maneuver -> badge hidden
                picker.SelectElementByName("Steel Wind");
                Assert.IsFalse(receptor.WarningBadgeVisible, "Warning badge should be hidden for valid Level 1 maneuver.");

                // Invalid choice: Level 2 maneuver exceeds level 1 max level for Feats -> badge visible
                picker.SelectElementByName("Mountain Hammer");
                Assert.IsTrue(receptor.WarningBadgeVisible, "Warning badge should appear when maneuver exceeds maximum maneuver level.");

                // Select valid maneuver back and save
                picker.SelectElementByName("Steel Wind");
                dialog.SaveChanges();

                Assert.AreEqual("Feats", session.Character.featChoices["Level 1:class"]);
                Assert.AreEqual("Steel Wind", session.Character.featChoices["Level 1:maneuver"]);
            }
        }

        [TestMethod]
        public void SyntheticSubsystemFeat_DialogWithTwoAdeptClasses_RendersBothDropdowns_AndParameterizesLevelGating() {
            var syntheticFeat = CreateSyntheticSubsystemFeat();
            var (session, _) = CreateSyntheticFeatSession(syntheticFeat, "Warblade", "Crusader", "Crusader");

            using (var dialog = new LevelDetailsDialog(session)) {
                dialog.PrepareLevel(2);

                var featCombo = dialog.Controls.OfType<ComboBox>()
                    .First(cbo => Equals(cbo.Tag, "Level 3"));
                featCombo.SelectedItem = syntheticFeat.Benefit.Name;

                var classCombo = dialog.Controls.OfType<ComboBox>()
                    .FirstOrDefault(cbo => Equals(cbo.Tag, "Level 3:class"));
                Assert.IsNotNull(classCombo, "Class dropdown should be rendered for 2+ adept classes.");

                var receptor = dialog.Controls.OfType<ElementReceptorControl>()
                    .FirstOrDefault(r => Equals(r.Tag, "Level 3:maneuver"));
                Assert.IsNotNull(receptor, "Maneuver receptor should be rendered.");

                classCombo.SelectedItem = "Crusader";
                receptor.PerformClick();
                var pickerHost = dialog.Controls.OfType<Panel>().First(p => p.Name == "pnlPickerHost");
                var picker = pickerHost.Controls.OfType<DefaultElementPicker>().First();
                picker.SelectElementByName("Crusader's Strike");

                dialog.SaveChanges();

                Assert.AreEqual("Crusader", session.Character.featChoices["Level 3:class"]);
                Assert.AreEqual("Crusader's Strike", session.Character.featChoices["Level 3:maneuver"]);
            }
        }

        [TestMethod]
        public void ComposableElementSelection_FullLifecycle_ActivationDeliverySwitchingClearAndCollapse() {
            var session = CreateAdeptSession("Fighter");
            using (var dialog = new LevelDetailsDialog(session)) {
                dialog.PrepareLevel(0);

                // 1. Initially pnlPickerHost is collapsed
                var pickerHost = dialog.PickerHost;
                Assert.AreEqual(0, pickerHost.Height);

                // Select Martial Study in Level 1 slot
                var featCombo = dialog.Controls.OfType<ComboBox>().First(cbo => Equals(cbo.Tag, "Level 1"));
                featCombo.SelectedItem = "Martial Study";

                var receptor = dialog.Controls.OfType<ElementReceptorControl>().First(r => Equals(r.Tag, "Level 1:maneuver"));
                Assert.AreEqual(ReceptorState.Unmade, receptor.State);
                Assert.IsFalse(receptor.IsActiveSelecting);

                // 2. Click receptor: activates styling, expands picker host, embeds DefaultElementPicker
                receptor.PerformClick();
                Assert.IsTrue(receptor.IsActiveSelecting);
                Assert.AreEqual(ReceptorState.ActiveSelecting, receptor.State);
                Assert.AreEqual(220, pickerHost.Height);
                var picker = pickerHost.Controls.OfType<DefaultElementPicker>().FirstOrDefault();
                Assert.IsNotNull(picker);

                // 3. Choice delivery: updates receptor text, evaluates warnings in real-time, keeps picker open
                picker.SelectElementByName("Steel Wind");
                Assert.AreEqual("Steel Wind", receptor.SelectedElement?.Name);
                Assert.AreEqual(ReceptorState.ActiveSelecting, receptor.State);
                Assert.IsFalse(receptor.WarningBadgeVisible);
                Assert.AreEqual(1, pickerHost.Controls.Count);

                // 4. Test Human slot to verify switching active receptors
                var humanFeatCombo = dialog.Controls.OfType<ComboBox>().First(cbo => Equals(cbo.Tag, "Human"));
                humanFeatCombo.SelectedItem = "Martial Stance";
                var humanReceptor = dialog.Controls.OfType<ElementReceptorControl>().First(r => Equals(r.Tag, "Human:stance"));

                humanReceptor.PerformClick();
                Assert.IsTrue(humanReceptor.IsActiveSelecting);
                Assert.IsFalse(receptor.IsActiveSelecting);

                // Human picker hosts stance choices
                var stancePicker = pickerHost.Controls.OfType<DefaultElementPicker>().First();
                stancePicker.SelectElementByName("Punishing Stance");
                Assert.AreEqual("Punishing Stance", humanReceptor.SelectedElement?.Name);

                // 5. Clear button on Level 1 receptor clears choice
                receptor.PerformClear();
                Assert.IsNull(receptor.SelectedElement);
                Assert.AreEqual(ReceptorState.Unmade, receptor.State);

                // 6. Changing parent feat dropdown collapses picker host and disposes picker
                humanFeatCombo.SelectedItem = "Endurance";
                Assert.AreEqual(0, pickerHost.Height);
                Assert.AreEqual(0, pickerHost.Controls.Count);
            }
        }
    }
}
