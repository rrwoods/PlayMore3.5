using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Items;
using PlayMore35.Rules;
using PlayMore35.Subsystems;
using PlayMore35.UI;
using PlayMore35.UI.Subsystems;

namespace PlayMore35.Tests {
    [TestClass]
    public class DualPassPrerequisiteTests {
        private IRulesCatalog catalog;

        [TestInitialize]
        public void Setup() {
            catalog = new GameRulesCatalog();
        }

        [TestMethod]
        public void ValidationWarning_Properties_AndStringConversions() {
            var unmet = new ValidationWarning("Missing prerequisite", WarningKind.Unmet);
            Assert.AreEqual("Missing prerequisite", unmet.Message);
            Assert.AreEqual(WarningKind.Unmet, unmet.Kind);
            Assert.AreEqual(ValidationCategory.General, unmet.Category);
            Assert.AreEqual("Missing prerequisite", unmet.ToString());

            var itemDep = new ValidationWarning("Item reliant", WarningKind.ItemDependent);
            Assert.AreEqual(WarningKind.ItemDependent, itemDep.Kind);
            Assert.AreEqual(ValidationCategory.General, itemDep.Category);

            var levelGate = new ValidationWarning("Max level exceeded", ValidationCategory.LevelGate);
            Assert.AreEqual(WarningKind.Unmet, levelGate.Kind);
            Assert.AreEqual(ValidationCategory.LevelGate, levelGate.Category);

            var prereqDep = new ValidationWarning("Requires feat", WarningKind.ItemDependent, ValidationCategory.Prerequisite);
            Assert.AreEqual(WarningKind.ItemDependent, prereqDep.Kind);
            Assert.AreEqual(ValidationCategory.Prerequisite, prereqDep.Category);

            // Equality & HashCode
            var unmetCopy = new ValidationWarning("Missing prerequisite", WarningKind.Unmet);
            Assert.AreEqual(unmet, unmetCopy);
            Assert.AreEqual(unmet.GetHashCode(), unmetCopy.GetHashCode());
            Assert.AreNotEqual(unmet, itemDep);

            var generalWarning = new ValidationWarning("Same message", WarningKind.Unmet, ValidationCategory.General);
            var prereqWarning = new ValidationWarning("Same message", WarningKind.Unmet, ValidationCategory.Prerequisite);
            Assert.AreNotEqual(generalWarning, prereqWarning);
        }

        [TestMethod]
        public void WarningBadge_VisualModes_UnmetAndItemDependent() {
            using (var badge = new WarningBadge()) {
                // Initial state
                Assert.IsFalse(badge.Visible);
                Assert.AreEqual("⚠", badge.Text);
                Assert.AreEqual(Color.DarkGoldenrod, badge.ForeColor);
                Assert.AreEqual(WarningKind.Unmet, badge.Kind);

                // Unmet mode
                badge.SetWarning("Prerequisite unmet", WarningKind.Unmet);
                Assert.IsTrue(badge.Visible);
                Assert.AreEqual("⚠", badge.Text);
                Assert.AreEqual(Color.DarkGoldenrod, badge.ForeColor);
                Assert.AreEqual(WarningKind.Unmet, badge.Kind);
                Assert.AreEqual("Prerequisite unmet", badge.WarningMessage);

                // ItemDependent mode
                badge.SetWarning("Item reliant prerequisite", WarningKind.ItemDependent);
                Assert.IsTrue(badge.Visible);
                Assert.AreEqual("🛈", badge.Text);
                Assert.AreEqual(Color.DodgerBlue, badge.ForeColor);
                Assert.AreEqual(WarningKind.ItemDependent, badge.Kind);
                Assert.AreEqual("Item reliant prerequisite", badge.WarningMessage);

                // Clear
                badge.ClearWarning();
                Assert.IsFalse(badge.Visible);
                Assert.AreEqual("⚠", badge.Text);
                Assert.AreEqual(Color.DarkGoldenrod, badge.ForeColor);
                Assert.AreEqual(WarningKind.Unmet, badge.Kind);
                Assert.IsNull(badge.WarningMessage);
            }
        }

        [TestMethod]
        public void WarningBadge_SetWarnings_PrioritizesUnmetWhenMixed() {
            using (var badge = new WarningBadge()) {
                var warnings = new List<ValidationWarning> {
                    new ValidationWarning("Item reliant A", WarningKind.ItemDependent),
                    new ValidationWarning("Missing B", WarningKind.Unmet),
                    new ValidationWarning("Item reliant C", WarningKind.ItemDependent)
                };

                badge.SetWarnings(warnings);
                Assert.IsTrue(badge.Visible);
                Assert.AreEqual(WarningKind.Unmet, badge.Kind, "When any warning is Unmet, overall kind must be Unmet.");
                Assert.AreEqual("⚠", badge.Text);
                Assert.AreEqual(Color.DarkGoldenrod, badge.ForeColor);
                StringAssert.Contains(badge.WarningMessage, "Item reliant A");
                StringAssert.Contains(badge.WarningMessage, "Missing B");

                // When all are ItemDependent
                var allItemDep = new List<ValidationWarning> {
                    new ValidationWarning("Item reliant A", WarningKind.ItemDependent),
                    new ValidationWarning("Item reliant C", WarningKind.ItemDependent)
                };

                badge.SetWarnings(allItemDep);
                Assert.IsTrue(badge.Visible);
                Assert.AreEqual(WarningKind.ItemDependent, badge.Kind);
                Assert.AreEqual("🛈", badge.Text);
                Assert.AreEqual(Color.DodgerBlue, badge.ForeColor);

                // Empty list clears
                badge.SetWarnings(new List<ValidationWarning>());
                Assert.IsFalse(badge.Visible);
            }
        }

        [TestMethod]
        public void ElementReceptorControl_VisualModes_UnmetAndItemDependent() {
            using (var receptor = new ElementReceptorControl()) {
                Assert.IsFalse(receptor.WarningBadgeVisible);

                // Set Unmet
                receptor.SetWarning("Missing", WarningKind.Unmet);
                Assert.IsTrue(receptor.WarningBadgeVisible);
                Assert.AreEqual(WarningKind.Unmet, receptor.WarningKind);
                Assert.AreEqual("Missing", receptor.WarningMessage);

                // Set ItemDependent
                receptor.SetWarning("Item reliant", WarningKind.ItemDependent);
                Assert.IsTrue(receptor.WarningBadgeVisible);
                Assert.AreEqual(WarningKind.ItemDependent, receptor.WarningKind);
                Assert.AreEqual("Item reliant", receptor.WarningMessage);

                // SetWarnings helper
                receptor.SetWarnings(new[] {
                    new ValidationWarning("Item reliant 1", WarningKind.ItemDependent),
                    new ValidationWarning("Item reliant 2", WarningKind.ItemDependent)
                });
                Assert.IsTrue(receptor.WarningBadgeVisible);
                Assert.AreEqual(WarningKind.ItemDependent, receptor.WarningKind);

                // Clear
                receptor.ClearWarning();
                Assert.IsFalse(receptor.WarningBadgeVisible);
            }
        }

        // Source: Tome of Battle pp. 39, 44; ADR-0017
        [TestMethod]
        public void DualPass_PrerequisiteUnmetIntrinsicallyAndWithGear_ReturnsWarningKindUnmet() {
            var martialStanceDef = catalog.GetFeat("Martial Stance");
            var stanceDesc = martialStanceDef.Choices.First(c => c.Id == "stance");

            // Level 10 Fighter with 0 Devoted Spirit maneuvers
            var character = new Character { race = "human" };
            character.classLevels.Clear();
            for (int i = 0; i < 10; i++) character.classLevels.Add("Fighter");

            var session = new CharacterSession(character, catalog);

            // Martial Spirit requires 1 Devoted Spirit maneuver
            var warnings = session.ValidateProgressionChoice(new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Martial Spirit",
                slotKey: "Level 1",
                targetSource: "Feats",
                descriptor: stanceDesc
            ));

            Assert.AreEqual(1, warnings.Count);
            Assert.AreEqual(WarningKind.Unmet, warnings[0].Kind);
            StringAssert.Contains(warnings[0].Message, "Requires 1 Devoted Spirit maneuver, but only 0 known");
        }

        // Source: Tome of Battle pp. 39, 44, 149; ADR-0017
        [TestMethod]
        public void DualPass_PrerequisiteSatisfiedByEquippedItem_ReturnsWarningKindItemDependent() {
            // Create a custom item that grants Crusader's Strike (Devoted Spirit maneuver)
            var itemEffect = new Effect("Crown of the White Raven")
                .AddSlot(new SubsystemSlotGrant("martial_maneuvers", "maneuvers", "Crusader's Strike", "Item"));
            var crownItem = TestFactory.CreateItem(Equipped.Slot.HEAD, itemEffect, weight: 1);

            var fakeCatalog = new FakeRulesCatalog(inner: catalog);
            fakeCatalog.ItemsMap["Crown of the White Raven"] = crownItem;

            var martialStanceDef = fakeCatalog.GetFeat("Martial Stance");
            var stanceDesc = martialStanceDef.Choices.First(c => c.Id == "stance");

            var character = new Character { race = "human" };
            character.classLevels.Clear();
            for (int i = 0; i < 10; i++) character.classLevels.Add("Fighter");

            // Equip the item on the character
            character.equipped.SlottedItems[Equipped.Slot.HEAD] = "Crown of the White Raven";

            var session = new CharacterSession(character, fakeCatalog);

            // Level 1 slot: Martial Spirit requires 1 Devoted Spirit maneuver.
            // Intrinsically (without gear), Devoted Spirit maneuvers known = 0 -> fails Pass 1.
            // With equipped gear applied to Level 1 snapshot, Crown grants Crusader's Strike -> passes Pass 2.
            var warnings = session.ValidateProgressionChoice(new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Martial Spirit",
                slotKey: "Level 1",
                targetSource: "Feats",
                descriptor: stanceDesc
            ));

            Assert.AreEqual(1, warnings.Count, "Must emit 1 warning.");
            Assert.AreEqual(WarningKind.ItemDependent, warnings[0].Kind, "Prerequisite satisfied by gear must be categorized as ItemDependent.");
            Assert.AreEqual("Requires 1 Devoted Spirit maneuver, but only 0 known.", warnings[0].Message);
        }

        // Source: Tome of Battle pp. 39, 52; ADR-0017
        [TestMethod]
        public void DualPass_LevelGating_RemainsUnmetEvenIfEquippedGearProvidesStatBoosts() {
            // Create an item that boosts initiator level (or stats)
            var beltItem = TestFactory.CreateItem(
                Equipped.Slot.WAIST,
                new Effect("Belt of Initiator Mastery")
                    .AddModifier("initiator level:feats", new Modifier(4, ModifierTypes.ENHANCEMENT))
                    .AddModifier("initiator level:warblade", new Modifier(4, ModifierTypes.ENHANCEMENT)),
                weight: 1);

            var fakeCatalog = new FakeRulesCatalog(inner: catalog);
            fakeCatalog.ItemsMap["Belt of Initiator Mastery"] = beltItem;

            var martialStudyDef = fakeCatalog.GetFeat("Martial Study");
            var desc = martialStudyDef.Choices.First(c => c.Id == "maneuver");

            var character = new Character { race = "human" };
            character.classLevels.Clear();
            for (int i = 0; i < 10; i++) character.classLevels.Add("Fighter");
            character.equipped.SlottedItems[Equipped.Slot.WAIST] = "Belt of Initiator Mastery";

            var session = new CharacterSession(character, fakeCatalog);

            // Level 1 slot choosing Mountain Hammer (Level 2 maneuver)
            var warnings = session.ValidateProgressionChoice(new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Mountain Hammer",
                slotKey: "Level 1",
                targetSource: "Feats",
                descriptor: desc
            ));

            Assert.AreEqual(1, warnings.Count);
            Assert.AreEqual(WarningKind.Unmet, warnings[0].Kind, "Subsystem element level gating is strictly intrinsic and must remain WarningKind.Unmet.");
            StringAssert.Contains(warnings[0].Message, "exceeds maximum maneuver level 1 for Feats");
        }

        // Source: Tome of Battle pp. 39, 44; ADR-0017
        [TestMethod]
        public void DualPass_NoLevelLeakage_SubsequentClassLevelsDoNotSatisfyEarlierPrerequisites() {
            var martialStanceDef = catalog.GetFeat("Martial Stance");
            var stanceDesc = martialStanceDef.Choices.First(c => c.Id == "stance");

            // Multiclass: Level 1 Fighter, Levels 2..5 Crusader (learned Devoted Spirit at level 2), Levels 6..10 Fighter
            var character = new Character { race = "human" };
            character.classLevels.Clear();
            character.classLevels.Add("Fighter");
            for (int i = 0; i < 4; i++) character.classLevels.Add("Crusader");
            for (int i = 0; i < 5; i++) character.classLevels.Add("Fighter");

            var crusaderState = new SubsystemState();
            crusaderState.Slots["maneuvers"] = new List<SubsystemSlot> {
                new SubsystemSlot("Crusader's Strike", "unreadied", "Crusader")
            };
            character.Subsystems["martial_maneuvers"] = crusaderState;

            var session = new CharacterSession(character, catalog);

            // Level 1 slot: Crusader class level not yet acquired at character level 1.
            // Live character has Crusader 4 at level 10, but level 1 snapshot must NOT leak later levels.
            var lvl1Warnings = session.ValidateProgressionChoice(new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Martial Spirit",
                slotKey: "Level 1",
                targetSource: "Feats",
                descriptor: stanceDesc
            ));
            Assert.AreEqual(1, lvl1Warnings.Count);
            Assert.AreEqual(WarningKind.Unmet, lvl1Warnings[0].Kind, "Subsequent class levels must not leak into Level 1 snapshot.");
            StringAssert.Contains(lvl1Warnings[0].Message, "Requires 1 Devoted Spirit maneuver, but only 0 known");

            // Level 3 slot: Crusader WAS acquired at level 2 -> satisfied intrinsically!
            var lvl3Warnings = session.ValidateProgressionChoice(new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Martial Spirit",
                slotKey: "Level 3",
                targetSource: "Feats",
                descriptor: stanceDesc
            ));
            Assert.AreEqual(0, lvl3Warnings.Count, "Level 3 slot intrinsically satisfies Devoted Spirit requirement.");
        }

        [TestMethod]
        public void LevelDetailsDialog_WithItemDependentChoice_DisplaysItemDependentBadge() {
            var itemEffect = new Effect("Crown of the White Raven")
                .AddSlot(new SubsystemSlotGrant("martial_maneuvers", "maneuvers", "Crusader's Strike", "Item"));
            var crownItem = TestFactory.CreateItem(Equipped.Slot.HEAD, itemEffect, weight: 1);

            var fakeCatalog = new FakeRulesCatalog(inner: catalog);
            fakeCatalog.ItemsMap["Crown of the White Raven"] = crownItem;
            FormUtils.availableFeats = fakeCatalog.Feats.Keys.OrderBy(x => x).ToList();

            var character = new Character {
                race = "human",
                classLevels = new List<string> { "Fighter", "Fighter", "Fighter" }
            };
            character.equipped.SlottedItems[Equipped.Slot.HEAD] = "Crown of the White Raven";

            var session = new CharacterSession(character, fakeCatalog);

            using (var dialog = new LevelDetailsDialog(session)) {
                dialog.PrepareLevel(0); // Level 1

                var featCombo = dialog.Controls.OfType<ComboBox>()
                    .First(cbo => Equals(cbo.Tag, "Level 1"));
                featCombo.SelectedItem = "Martial Stance";

                var receptor = dialog.Controls.OfType<ElementReceptorControl>()
                    .First(r => Equals(r.Tag, "Level 1:stance"));
                receptor.PerformClick();
                var pickerHost = dialog.Controls.OfType<Panel>().First(p => p.Name == "pnlPickerHost");
                var picker = pickerHost.Controls.OfType<DefaultElementPicker>().First();

                // Select Martial Spirit (requires 1 Devoted Spirit maneuver, satisfied by Crown of White Raven)
                picker.SelectElementByName("Martial Spirit");

                Assert.IsTrue(receptor.WarningBadgeVisible);
                Assert.AreEqual(WarningKind.ItemDependent, receptor.WarningKind, "Badge on receptor must be ItemDependent when requirement is met through equipped gear.");
                StringAssert.Contains(receptor.WarningMessage, "Requires 1 Devoted Spirit maneuver");
            }
        }

        // Source: Player's Handbook 3.5, pp. 22, 58; Complete Warrior, p. 16; ADR-0017, ADR-0018
        [TestMethod]
        public void ValidateProgressionChoice_FirstLevelClass_EvaluatesAgainstLevel0Snapshot_WithoutThrowing() {
            var character = new Character {
                race = "human",
                classLevels = new List<string> { "Fighter" }
            };
            var session = new CharacterSession(character, catalog);

            var context = new ProgressionChoiceContext(
                ProgressionChoiceDomain.Class,
                "Fighter",
                acquisitionLevel: 1
            );

            var warnings = session.ValidateProgressionChoice(context);
            Assert.AreEqual(0, warnings.Count, "First level standard class choice without prerequisites must evaluate cleanly against level 0 state.");
        }

        // Source: Player's Handbook 3.5, pp. 22, 58; Complete Warrior, p. 16; ADR-0017, ADR-0018
        [TestMethod]
        public void ValidateProgressionChoice_FirstLevelClass_PrerequisitesEvaluatedAgainstLevel0State() {
            var fakeCatalog = new FakeRulesCatalog(inner: catalog);
            var reqClass = TestFactory.CreateClass("Berserker", prerequisites: "STR 14");
            fakeCatalog.ClassesMap["Berserker"] = reqClass;

            // Character with base STR 12 at level 0 (human)
            var character = new Character {
                race = "human",
                classLevels = new List<string> { "Fighter" }
            };
            character.assignedAbilityScores["STR"] = 12;

            var session = new CharacterSession(character, fakeCatalog);

            var context = new ProgressionChoiceContext(
                ProgressionChoiceDomain.Class,
                "Berserker",
                acquisitionLevel: 1
            );

            var warnings = session.ValidateProgressionChoice(context);
            Assert.AreEqual(1, warnings.Count);
            Assert.AreEqual(WarningKind.Unmet, warnings[0].Kind);
            Assert.AreEqual(ValidationCategory.Prerequisite, warnings[0].Category);
            StringAssert.Contains(warnings[0].Message, "Requires STR 14");

            // Now with base STR 14 at level 0: should pass
            var char14 = new Character {
                race = "human",
                classLevels = new List<string> { "Fighter" }
            };
            char14.assignedAbilityScores["STR"] = 14;
            var session14 = new CharacterSession(char14, fakeCatalog);
            var warnings14 = session14.ValidateProgressionChoice(context);
            Assert.AreEqual(0, warnings14.Count, "Meeting STR 14 at level 0 must produce 0 warnings.");
        }

        // Source: Player's Handbook 3.5, pp. 22, 58; Complete Warrior, p. 16; ADR-0017, ADR-0018
        [TestMethod]
        public void ValidateProgressionChoice_ClassChoiceAtLevelL_EvaluatesAgainstLevelLMinus1_NoLeakageOfLevelLBenefits() {
            var fakeCatalog = new FakeRulesCatalog(inner: catalog);
            var prcClass = TestFactory.CreateClass("Prestige Champion", prerequisites: "Base attack bonus +4, STR 14, Power Attack, Jump 4 ranks");
            fakeCatalog.ClassesMap["Prestige Champion"] = prcClass;

            // Character has 4 levels: 3 levels Fighter, 4th level Fighter.
            // At level 3 (L - 1):
            // - BAB = +3 (Fighter 3)
            // - STR = 13
            // - Jump ranks = 3
            // - Feats: only "Weapon Focus" at level 1
            // At level 4 (L):
            // - BAB becomes +4
            // - STR increment at level 4 adds +1 STR (to 14)
            // - Jump ranks gets +1 (to 4)
            // - Feats: "Power Attack" taken at level 4 slot
            var character = new Character {
                race = "human",
                classLevels = new List<string> { "Fighter", "Fighter", "Fighter", "Fighter" }
            };
            character.assignedAbilityScores["STR"] = 13;
            character.abilityScoreIncrements[4] = "STR";
            character.rolledHitDice = new List<int> { 10, 8, 8, 8 };
            character.skillRanks["Jump"] = new List<int> { 1, 1, 1, 1 };
            character.feats["Level 1"] = "Weapon Focus";
            character.feats["Level 4"] = "Power Attack";

            var session = new CharacterSession(character, fakeCatalog);

            // Validating Prestige Champion for Level 4 must evaluate against Level 3 (L - 1).
            // At Level 3: BAB is +3 (needs +4), STR is 13 (needs 14), Jump is 3 (needs 4), lacks Power Attack (acquired at Lvl 4).
            var context = new ProgressionChoiceContext(
                ProgressionChoiceDomain.Class,
                "Prestige Champion",
                acquisitionLevel: 4
            );

            var warnings = session.ValidateProgressionChoice(context);
            Assert.AreEqual(4, warnings.Count, "All 4 prerequisites must fail because Level 4 benefits must not leak into Level 4 class selection.");
            foreach (var w in warnings) {
                Assert.AreEqual(WarningKind.Unmet, w.Kind);
                Assert.AreEqual(ValidationCategory.Prerequisite, w.Category);
            }
            Assert.IsTrue(warnings.Any(w => w.Message.Contains("Base attack bonus +4")), "Must warn for BAB +4.");
            Assert.IsTrue(warnings.Any(w => w.Message.Contains("STR 14")), "Must warn for STR 14.");
            Assert.IsTrue(warnings.Any(w => w.Message.Contains("Jump 4 ranks")), "Must warn for Jump 4 ranks.");
            Assert.IsTrue(warnings.Any(w => w.Message.Contains("Power Attack")), "Must warn for Power Attack.");
        }

        // Source: Complete Warrior, p. 16 ("Meeting Class Prerequisites"); ADR-0017, ADR-0018
        [TestMethod]
        public void ValidateProgressionChoice_ClassChoice_DualPassItemQualification_CategorizesAsItemDependent() {
            var fakeCatalog = new FakeRulesCatalog(inner: catalog);
            var prcClass = TestFactory.CreateClass("Iron Mind", prerequisites: "STR 14");
            fakeCatalog.ClassesMap["Iron Mind"] = prcClass;

            var gauntlets = TestFactory.CreateItem(
                Equipped.Slot.HANDS,
                new Effect("Gauntlets of Ogre Power")
                    .AddModifier(Constants.STR_SCORE, new Modifier(2, ModifierTypes.ENHANCEMENT)),
                weight: 1);
            fakeCatalog.ItemsMap["Gauntlets of Ogre Power"] = gauntlets;

            // Character at level 2 with base STR 12, equipping Gauntlets of Ogre Power
            var character = new Character {
                race = "human",
                classLevels = new List<string> { "Fighter", "Fighter" }
            };
            character.assignedAbilityScores["STR"] = 12;
            character.equipped.SlottedItems[Equipped.Slot.HANDS] = "Gauntlets of Ogre Power";

            var session = new CharacterSession(character, fakeCatalog);

            // Validating for level 2 (evaluates against level 1 state):
            // Intrinsically: STR = 12 < 14 -> fails Pass 1
            // Equipped: Gauntlets boost STR to 14 -> satisfies Pass 2
            var context = new ProgressionChoiceContext(
                ProgressionChoiceDomain.Class,
                "Iron Mind",
                acquisitionLevel: 2
            );

            var warnings = session.ValidateProgressionChoice(context);
            Assert.AreEqual(1, warnings.Count);
            Assert.AreEqual(WarningKind.ItemDependent, warnings[0].Kind, "Prerequisite satisfied by gear in pre-advancement slice must be ItemDependent.");
            Assert.AreEqual(ValidationCategory.Prerequisite, warnings[0].Category);
            StringAssert.Contains(warnings[0].Message, "Requires STR 14");
        }

        // Source: Complete Warrior, p. 16 ("Meeting Class Prerequisites"); ADR-0017, ADR-0018
        [TestMethod]
        public void ValidateProgressionChoice_ClassChoice_PartialItemImprovement_RemainsUnmet() {
            var fakeCatalog = new FakeRulesCatalog(inner: catalog);
            var prcClass = TestFactory.CreateClass("Iron Mind", prerequisites: "STR 16");
            fakeCatalog.ClassesMap["Iron Mind"] = prcClass;

            var gauntlets = TestFactory.CreateItem(
                Equipped.Slot.HANDS,
                new Effect("Gauntlets of Ogre Power")
                    .AddModifier(Constants.STR_SCORE, new Modifier(2, ModifierTypes.ENHANCEMENT)),
                weight: 1);
            fakeCatalog.ItemsMap["Gauntlets of Ogre Power"] = gauntlets;

            // Character at level 2 with base STR 12, equipping Gauntlets of Ogre Power (+2 STR -> 14 < 16)
            var character = new Character {
                race = "human",
                classLevels = new List<string> { "Fighter", "Fighter" }
            };
            character.assignedAbilityScores["STR"] = 12;
            character.equipped.SlottedItems[Equipped.Slot.HANDS] = "Gauntlets of Ogre Power";

            var session = new CharacterSession(character, fakeCatalog);

            // Validating for level 2:
            // Intrinsically at level 1: STR 12 < 16 -> unmet
            // Equipped at level 1: STR 14 < 16 -> still unmet!
            // Must remain WarningKind.Unmet (not spuriously marked ItemDependent).
            var context = new ProgressionChoiceContext(
                ProgressionChoiceDomain.Class,
                "Iron Mind",
                acquisitionLevel: 2
            );

            var warnings = session.ValidateProgressionChoice(context);
            Assert.AreEqual(1, warnings.Count);
            Assert.AreEqual(WarningKind.Unmet, warnings[0].Kind, "Prerequisite only partially improved by gear must remain WarningKind.Unmet.");
            Assert.AreEqual(ValidationCategory.Prerequisite, warnings[0].Category);
            StringAssert.Contains(warnings[0].Message, "Requires STR 16");
        }

        // Source: Tome of Battle pp. 31, 39; ADR-0017, ADR-0018
        [TestMethod]
        public void ValidateProgressionChoice_FeatSourcedSubsystem_DualPassPrerequisiteSatisfiedByGear_CategorizesAsItemDependent() {
            var itemEffect = new Effect("Crown of the White Raven")
                .AddSlot(new SubsystemSlotGrant("martial_maneuvers", "maneuvers", "Crusader's Strike", "Item"));
            var crownItem = TestFactory.CreateItem(Equipped.Slot.HEAD, itemEffect, weight: 1);

            var fakeCatalog = new FakeRulesCatalog(inner: catalog);
            fakeCatalog.ItemsMap["Crown of the White Raven"] = crownItem;

            var martialStanceDef = fakeCatalog.GetFeat("Martial Stance");
            var stanceDesc = martialStanceDef.Choices.First(c => c.Id == "stance");

            var character = new Character { race = "human" };
            character.classLevels.Clear();
            character.classLevels.Add("Fighter");
            character.equipped.SlottedItems[Equipped.Slot.HEAD] = "Crown of the White Raven";

            var session = new CharacterSession(character, fakeCatalog);

            var context = new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Martial Spirit",
                slotKey: "Level 1",
                targetSource: "Feats",
                descriptor: stanceDesc
            );

            var warnings = session.ValidateProgressionChoice(context);

            Assert.AreEqual(1, warnings.Count);
            Assert.AreEqual(WarningKind.ItemDependent, warnings[0].Kind, "Prerequisite satisfied by gear must transition to ItemDependent.");
            Assert.AreEqual(ValidationCategory.Prerequisite, warnings[0].Category);
            StringAssert.Contains(warnings[0].Message, "Requires 1 Devoted Spirit maneuver, but only 0 known.");
        }

        // Source: Tome of Battle pp. 39, 52; ADR-0017, ADR-0018
        [TestMethod]
        public void ValidateProgressionChoice_FeatSourcedSubsystem_LevelGateExceeded_RemainsUnmetEvenWithStatBoostItem() {
            var beltItem = TestFactory.CreateItem(
                Equipped.Slot.WAIST,
                new Effect("Belt of Initiator Mastery")
                    .AddModifier("initiator level:feats", new Modifier(4, ModifierTypes.ENHANCEMENT))
                    .AddModifier("initiator level:warblade", new Modifier(4, ModifierTypes.ENHANCEMENT)),
                weight: 1);

            var fakeCatalog = new FakeRulesCatalog(inner: catalog);
            fakeCatalog.ItemsMap["Belt of Initiator Mastery"] = beltItem;

            var martialStudyDef = fakeCatalog.GetFeat("Martial Study");
            var desc = martialStudyDef.Choices.First(c => c.Id == "maneuver");

            var character = new Character { race = "human" };
            character.classLevels.Clear();
            for (int i = 0; i < 10; i++) character.classLevels.Add("Fighter");
            character.equipped.SlottedItems[Equipped.Slot.WAIST] = "Belt of Initiator Mastery";

            var session = new CharacterSession(character, fakeCatalog);

            var context = new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Mountain Hammer",
                slotKey: "Level 1",
                targetSource: "Feats",
                descriptor: desc
            );

            var warnings = session.ValidateProgressionChoice(context);

            Assert.AreEqual(1, warnings.Count);
            Assert.AreEqual(WarningKind.Unmet, warnings[0].Kind, "Level gate must remain Unmet even if gear boosts initiator stats.");
            Assert.AreEqual(ValidationCategory.LevelGate, warnings[0].Category);
            StringAssert.Contains(warnings[0].Message, "exceeds maximum maneuver level 1 for Feats");
        }

        // Source: Tome of Battle pp. 39, 44; ADR-0017, ADR-0018
        [TestMethod]
        public void ValidateProgressionChoice_ClassGrantedSubsystem_DualPassPrerequisiteSatisfiedByGear_CategorizesAsItemDependent() {
            var itemEffect = new Effect("Iron Heart Ring")
                .AddSlot(new SubsystemSlotGrant("martial_maneuvers", "maneuvers", "Steely Strike", "Item"));
            var ringItem = TestFactory.CreateItem(Equipped.Slot.RING1, itemEffect, weight: 1);

            var fakeCatalog = new FakeRulesCatalog(inner: catalog);
            fakeCatalog.ItemsMap["Iron Heart Ring"] = ringItem;

            var character = new Character { race = "human" };
            character.classLevels.Clear();
            for (int i = 0; i < 5; i++) character.classLevels.Add("Warblade");
            character.equipped.SlottedItems[Equipped.Slot.RING1] = "Iron Heart Ring";

            var session = new CharacterSession(character, fakeCatalog);

            // Level 5 Warblade choosing Iron Heart Surge (requires 1 Iron Heart maneuver).
            // Intrinsically 0 Iron Heart maneuvers known -> fails Pass 1.
            // With equipped ring granting Steely Strike -> satisfied in Pass 2.
            var context = new ProgressionChoiceContext(
                ProgressionChoiceDomain.Subsystem,
                "Iron Heart Surge",
                acquisitionLevel: 5,
                targetSource: "Warblade"
            );

            var warnings = session.ValidateProgressionChoice(context);

            Assert.AreEqual(1, warnings.Count);
            Assert.AreEqual(WarningKind.ItemDependent, warnings[0].Kind, "Prerequisite satisfied by gear must transition to ItemDependent.");
            Assert.AreEqual(ValidationCategory.Prerequisite, warnings[0].Category);
            StringAssert.Contains(warnings[0].Message, "Requires 1 Iron Heart maneuver, but only 0 known.");
        }

        // Source: Tome of Battle pp. 39, 44; ADR-0017, ADR-0018
        [TestMethod]
        public void ValidateProgressionChoice_ClassGrantedSubsystem_LevelGateExceeded_RemainsUnmetEvenWithStatBoostItem() {
            var beltItem = TestFactory.CreateItem(
                Equipped.Slot.WAIST,
                new Effect("Belt of Initiator Mastery")
                    .AddModifier("initiator level:warblade", new Modifier(4, ModifierTypes.ENHANCEMENT)),
                weight: 1);

            var fakeCatalog = new FakeRulesCatalog(inner: catalog);
            fakeCatalog.ItemsMap["Belt of Initiator Mastery"] = beltItem;

            var character = new Character { race = "human" };
            character.classLevels.Clear();
            character.classLevels.Add("Warblade");
            character.equipped.SlottedItems[Equipped.Slot.WAIST] = "Belt of Initiator Mastery";

            var session = new CharacterSession(character, fakeCatalog);

            // Warblade 1 choosing Mountain Hammer (Level 2 maneuver > max 1 for Warblade 1; 0 prereqs)
            var context = new ProgressionChoiceContext(
                ProgressionChoiceDomain.Subsystem,
                "Mountain Hammer",
                acquisitionLevel: 1,
                targetSource: "Warblade"
            );

            var warnings = session.ValidateProgressionChoice(context);

            Assert.AreEqual(1, warnings.Count);
            Assert.AreEqual(WarningKind.Unmet, warnings[0].Kind, "Level gate must remain Unmet even if gear boosts initiator stats.");
            Assert.AreEqual(ValidationCategory.LevelGate, warnings[0].Category);
            StringAssert.Contains(warnings[0].Message, "exceeds maximum maneuver level 1 for Warblade");
        }

        // Source: Tome of Battle pp. 22, 43–44; ADR-0018
        [TestMethod]
        public void ValidateProgressionChoice_ClassGrantedSubsystem_ValidChoice_EmitsZeroWarnings() {
            var character = new Character { race = "human" };
            character.classLevels.Clear();
            character.classLevels.Add("Warblade");

            var session = new CharacterSession(character, catalog);

            var context = new ProgressionChoiceContext(
                ProgressionChoiceDomain.Subsystem,
                "Steely Strike",
                acquisitionLevel: 1,
                targetSource: "Warblade"
            );

            var warnings = session.ValidateProgressionChoice(context);

            Assert.AreEqual(0, warnings.Count, "Valid maneuver choice must not emit warnings or false invalid warnings from other subsystems.");
        }

        // Source: Tome of Magic p. 19; ADR-0018
        [TestMethod]
        public void ValidateProgressionChoice_ClassGrantedSubsystem_VestigeChoice_ValidatesPactMagicWithoutMartialConflict() {
            var character = new Character { race = "human" };
            character.classLevels.Clear();
            character.classLevels.Add("Binder");

            var session = new CharacterSession(character, catalog);

            // Valid vestige at Level 1 (Amon is level 1)
            var contextAmon = new ProgressionChoiceContext(
                ProgressionChoiceDomain.Subsystem,
                "Amon",
                acquisitionLevel: 1,
                targetSource: "Binder"
            );

            var warningsAmon = session.ValidateProgressionChoice(contextAmon);
            Assert.AreEqual(0, warningsAmon.Count, "Valid vestige choice must emit 0 warnings.");

            // Level gate exceeded (Savnok is level 2 > max 1 for Binder 1)
            var contextSavnok = new ProgressionChoiceContext(
                ProgressionChoiceDomain.Subsystem,
                "Savnok",
                acquisitionLevel: 1,
                targetSource: "Binder"
            );

            var warningsSavnok = session.ValidateProgressionChoice(contextSavnok);
            Assert.AreEqual(1, warningsSavnok.Count);
            Assert.AreEqual(WarningKind.Unmet, warningsSavnok[0].Kind);
            Assert.AreEqual(ValidationCategory.LevelGate, warningsSavnok[0].Category);
            StringAssert.Contains(warningsSavnok[0].Message, "Level 2 exceeds maximum 1 for Binder.");
        }

        // Source: Player's Handbook p. 90; ADR-0018
        [TestMethod]
        public void ValidateProgressionChoice_StandardFeatChoice_DoesNotFallThroughToSubsystems() {
            var character = new Character { race = "human" };
            character.classLevels.Clear();
            character.classLevels.Add("Fighter");

            var session = new CharacterSession(character, catalog);

            var context = new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Power Attack",
                slotKey: "Level 1"
            );

            var warnings = session.ValidateProgressionChoice(context);
            Assert.AreEqual(0, warnings.Count, "Standard non-subsystem feat must not fall through to subsystem engines.");
        }

        // Source: Tome of Battle pp. 22, 43–44; ADR-0018
        [TestMethod]
        public void ValidateProgressionChoice_ClassGrantedSubsystem_MisspelledChoice_ScopesToClassSubsystem() {
            var character = new Character { race = "human" };
            character.classLevels.Clear();
            character.classLevels.Add("Warblade");

            var session = new CharacterSession(character, catalog);

            var context = new ProgressionChoiceContext(
                ProgressionChoiceDomain.Subsystem,
                "NonexistentManeuver",
                acquisitionLevel: 1,
                targetSource: "Warblade"
            );

            var warnings = session.ValidateProgressionChoice(context);
            Assert.AreEqual(1, warnings.Count, "Unrecognized element for Warblade must only emit a warning from Martial Maneuvers, not Pact Magic.");
            Assert.AreEqual(ValidationCategory.Invalid, warnings[0].Category);
            StringAssert.Contains(warnings[0].Message, "'NonexistentManeuver' is not a valid element in subsystem 'Martial Maneuvers'.");
        }

        [TestMethod]
        public void ValidateProgressionChoice_ContextNullOrChoiceValueEmpty_ReturnsEmptyWarnings() {
            var character = new Character { race = "human" };
            character.classLevels.Clear();
            character.classLevels.Add("Fighter");

            var session = new CharacterSession(character, catalog);

            var nullResult = session.ValidateProgressionChoice(null!);
            Assert.AreEqual(0, nullResult.Count, "Null context must return empty warnings without throwing.");

            var emptyFeatResult = session.ValidateProgressionChoice(new ProgressionChoiceContext(ProgressionChoiceDomain.Feat, ""));
            Assert.AreEqual(0, emptyFeatResult.Count, "Empty feat choice value must return empty warnings.");

            var wsSubsystemResult = session.ValidateProgressionChoice(new ProgressionChoiceContext(ProgressionChoiceDomain.Subsystem, "   "));
            Assert.AreEqual(0, wsSubsystemResult.Count, "Whitespace subsystem choice value must return empty warnings.");
        }

        // Source: Tome of Battle pp. 22, 43–44, 68; ADR-0018
        [TestMethod]
        public void ValidateProgressionChoice_ClassGrantedManeuver_DisciplinePrerequisiteUnmet_EmitsPrerequisiteWarning() {
            var character = new Character { race = "human" };
            character.classLevels.Clear();
            for (int i = 0; i < 5; i++) character.classLevels.Add("Warblade"); // Level 5 Warblade (IL 5, max maneuver level 3)

            var session = new CharacterSession(character, catalog);

            // Iron Heart Surge is level 3, requires 1 Iron Heart maneuver
            var contextWithoutPrereq = new ProgressionChoiceContext(
                ProgressionChoiceDomain.Subsystem,
                "Iron Heart Surge",
                acquisitionLevel: 5,
                targetSource: "Warblade"
            );

            var warningsWithout = session.ValidateProgressionChoice(contextWithoutPrereq);
            Assert.AreEqual(1, warningsWithout.Count, "Lacking 1 Iron Heart maneuver prerequisite must emit 1 warning.");
            Assert.AreEqual(ValidationCategory.Prerequisite, warningsWithout[0].Category);
            Assert.AreEqual(WarningKind.Unmet, warningsWithout[0].Kind);
            StringAssert.Contains(warningsWithout[0].Message, "Requires 1 Iron Heart maneuver, but only 0 known");

            // With an Iron Heart maneuver known in current choices, prerequisite is satisfied
            var choices = new Dictionary<string, string> {
                ["maneuver_1"] = "Steel Wind"
            };
            var contextWithPrereq = new ProgressionChoiceContext(
                ProgressionChoiceDomain.Subsystem,
                "Iron Heart Surge",
                acquisitionLevel: 5,
                targetSource: "Warblade",
                currentChoices: choices
            );

            var warningsWith = session.ValidateProgressionChoice(contextWithPrereq);
            Assert.IsFalse(warningsWith.Any(w => w.Category == ValidationCategory.Prerequisite), "When prerequisite is satisfied via current choices, 0 prerequisite warnings must be emitted.");
        }

        // Source: Tome of Magic pp. 19, 21, 48; ADR-0018
        [TestMethod]
        public void ValidateProgressionChoice_VestigeChoice_HigherLevelBinder_AllowsHigherTierVestiges() {
            var character = new Character { race = "human" };
            character.classLevels.Clear();
            character.classLevels.Add("Binder");
            character.classLevels.Add("Binder");
            character.classLevels.Add("Binder"); // Level 3 Binder (max vestige level 2)

            var session = new CharacterSession(character, catalog);

            // Savnok is a 2nd-level vestige: valid for Binder 3
            var contextSavnok = new ProgressionChoiceContext(
                ProgressionChoiceDomain.Subsystem,
                "Savnok",
                acquisitionLevel: 3,
                targetSource: "Binder"
            );

            var warningsSavnok = session.ValidateProgressionChoice(contextSavnok);
            Assert.AreEqual(0, warningsSavnok.Count, "2nd-level vestige Savnok must be valid for Level 3 Binder.");

            // Dahlver-Nar is also a 2nd-level vestige: valid for Binder 3
            var contextDahlver = new ProgressionChoiceContext(
                ProgressionChoiceDomain.Subsystem,
                "Dahlver-Nar",
                acquisitionLevel: 3,
                targetSource: "Binder"
            );

            var warningsDahlver = session.ValidateProgressionChoice(contextDahlver);
            Assert.AreEqual(0, warningsDahlver.Count, "2nd-level vestige Dahlver-Nar must be valid for Level 3 Binder.");
        }

        // Source: Tome of Magic p. 19; ADR-0018
        [TestMethod]
        public void ValidateProgressionChoice_VestigeChoice_MisspelledVestige_ScopesToPactMagic() {
            var character = new Character { race = "human" };
            character.classLevels.Clear();
            character.classLevels.Add("Binder");

            var session = new CharacterSession(character, catalog);

            var context = new ProgressionChoiceContext(
                ProgressionChoiceDomain.Subsystem,
                "NonexistentVestige",
                acquisitionLevel: 1,
                targetSource: "Binder"
            );

            var warnings = session.ValidateProgressionChoice(context);
            Assert.AreEqual(1, warnings.Count, "Unrecognized vestige for Binder must emit Invalid warning from Pact Magic.");
            Assert.AreEqual(ValidationCategory.Invalid, warnings[0].Category);
            StringAssert.Contains(warnings[0].Message, "'NonexistentVestige' is not a valid element in subsystem 'Pact Magic'.");
        }

        // Source: Tome of Magic pp. 19, 21; Complete Warrior p. 16; ADR-0017, ADR-0018
        [TestMethod]
        public void ValidateProgressionChoice_VestigeChoice_LevelGateRemainsUnmetEvenWithEquippedItem() {
            var beltItem = TestFactory.CreateItem(
                Equipped.Slot.WAIST,
                new Effect("Belt of Binder Mastery")
                    .AddModifier("effective binder level", new Modifier(4, ModifierTypes.ENHANCEMENT)),
                weight: 1);

            var fakeCatalog = new FakeRulesCatalog(inner: catalog);
            fakeCatalog.ItemsMap["Belt of Binder Mastery"] = beltItem;

            var character = new Character { race = "human" };
            character.classLevels.Clear();
            character.classLevels.Add("Binder"); // Level 1 Binder (max vestige level 1)
            character.equipped.SlottedItems[Equipped.Slot.WAIST] = "Belt of Binder Mastery";

            var session = new CharacterSession(character, fakeCatalog);

            // Savnok is level 2 > max 1 for Binder 1
            var context = new ProgressionChoiceContext(
                ProgressionChoiceDomain.Subsystem,
                "Savnok",
                acquisitionLevel: 1,
                targetSource: "Binder"
            );

            var warnings = session.ValidateProgressionChoice(context);
            Assert.AreEqual(1, warnings.Count);
            Assert.AreEqual(WarningKind.Unmet, warnings[0].Kind, "Vestige level gating is intrinsic and must remain WarningKind.Unmet.");
            Assert.AreEqual(ValidationCategory.LevelGate, warnings[0].Category);
            StringAssert.Contains(warnings[0].Message, "Level 2 exceeds maximum 1 for Binder.");
        }

        // Source: Tome of Battle, p. 52
        [TestMethod]
        public void CreateSessionAtLevel_InFlightFeatChoice_DoesNotCreateDuplicateSlots() {
            var character = new Character { race = "human" };
            character.classLevels.Clear();
            character.classLevels.Add("Fighter");

            var session = new CharacterSession(character, catalog);

            var inFlightChoices = new Dictionary<string, string> {
                ["Level 1"] = "Martial Study",
                ["Level 1:maneuver"] = "Steel Wind"
            };

            var snapshot = session.CreateSessionAtLevel(1, includeEquipped: false, inFlightChoices);
            var engine = snapshot.GetSubsystem("martial_maneuvers");
            Assert.IsNotNull(engine);

            var maneuverSlots = engine.GetSlotGroup("maneuvers")?.RawSlots
                .Where(s => string.Equals(s.Element, "Steel Wind", StringComparison.OrdinalIgnoreCase))
                .ToList();

            Assert.IsNotNull(maneuverSlots);
            Assert.AreEqual(1, maneuverSlots.Count, "Martial Study feat choice must create exactly 1 granted slot, not duplicate slots.");
        }

        // Source: Tome of Battle, p. 52
        [TestMethod]
        public void ValidateProgressionChoice_OnSnapshotSessionWithOverlay_EvaluatesDualPassItemDependency() {
            var item = TestFactory.CreateItem(
                Equipped.Slot.HEAD,
                new Effect("Crown of the White Raven")
                    .AddSlot(new SubsystemSlotGrant("martial_maneuvers", "maneuvers", "Crusader's Strike", "Item")),
                weight: 1);

            var fakeCatalog = new FakeRulesCatalog(inner: catalog);
            fakeCatalog.ItemsMap["Crown of the White Raven"] = item;

            var martialStanceDef = fakeCatalog.GetFeat("Martial Stance");
            var stanceDesc = martialStanceDef.Choices.First(c => c.Id == "stance");

            var character = new Character { race = "human" };
            character.classLevels.Clear();
            for (int i = 0; i < 10; i++) character.classLevels.Add("Fighter");
            character.equipped.SlottedItems[Equipped.Slot.HEAD] = "Crown of the White Raven";

            var liveSession = new CharacterSession(character, fakeCatalog);
            var snapshotSession = liveSession.GetSessionAtLevel(10, includeEquipped: true);

            var inFlight = new Dictionary<string, string> {
                ["Level 1"] = "Martial Stance"
            };

            var progContext = new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Martial Spirit",
                slotKey: "Level 1",
                targetSource: "Feats",
                descriptor: stanceDesc,
                currentChoices: inFlight
            );

            var warnings = snapshotSession.ValidateProgressionChoice(progContext);
            Assert.AreEqual(1, warnings.Count, "Must emit 1 warning.");
            Assert.AreEqual(WarningKind.ItemDependent, warnings[0].Kind, "Prerequisite satisfied by gear must be categorized as ItemDependent on snapshot session.");
            Assert.AreEqual(ValidationCategory.Prerequisite, warnings[0].Category);
            StringAssert.Contains(warnings[0].Message, "Requires 1 Devoted Spirit maneuver, but only 0 known");
        }
    }
}
