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
            Assert.AreEqual("Missing prerequisite", unmet.ToString());

            var itemDep = new ValidationWarning("Item reliant", WarningKind.ItemDependent);
            Assert.AreEqual(WarningKind.ItemDependent, itemDep.Kind);

            // Equality & HashCode
            var unmetCopy = new ValidationWarning("Missing prerequisite", WarningKind.Unmet);
            Assert.AreEqual(unmet, unmetCopy);
            Assert.AreEqual(unmet.GetHashCode(), unmetCopy.GetHashCode());
            Assert.AreNotEqual(unmet, itemDep);
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
            var warnings = session.ValidateFeatChoice("Level 1", stanceDesc, "Martial Spirit", "Feats");

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
            var crownItem = new Item {
                ItemSlot = Equipped.Slot.HEAD,
                ItemEffect = itemEffect,
                Weight = 1
            };

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
            var warnings = session.ValidateFeatChoice("Level 1", stanceDesc, "Martial Spirit", "Feats");

            Assert.AreEqual(1, warnings.Count, "Must emit 1 warning.");
            Assert.AreEqual(WarningKind.ItemDependent, warnings[0].Kind, "Prerequisite satisfied by gear must be categorized as ItemDependent.");
            Assert.AreEqual("Requires 1 Devoted Spirit maneuver, but only 0 known.", warnings[0].Message);
        }

        // Source: Tome of Battle pp. 39, 52; ADR-0017
        [TestMethod]
        public void DualPass_LevelGating_RemainsUnmetEvenIfEquippedGearProvidesStatBoosts() {
            // Create an item that boosts initiator level (or stats)
            var beltItem = new Item {
                ItemSlot = Equipped.Slot.WAIST,
                ItemEffect = new Effect("Belt of Initiator Mastery")
                    .AddModifier("initiator level:feats", new Modifier(4, ModifierTypes.ENHANCEMENT))
                    .AddModifier("initiator level:warblade", new Modifier(4, ModifierTypes.ENHANCEMENT)),
                Weight = 1
            };

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
            var warnings = session.ValidateFeatChoice("Level 1", desc, "Mountain Hammer", "Feats");

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
            var lvl1Warnings = session.ValidateFeatChoice("Level 1", stanceDesc, "Martial Spirit", "Feats");
            Assert.AreEqual(1, lvl1Warnings.Count);
            Assert.AreEqual(WarningKind.Unmet, lvl1Warnings[0].Kind, "Subsequent class levels must not leak into Level 1 snapshot.");
            StringAssert.Contains(lvl1Warnings[0].Message, "Requires 1 Devoted Spirit maneuver, but only 0 known");

            // Level 3 slot: Crusader WAS acquired at level 2 -> satisfied intrinsically!
            var lvl3Warnings = session.ValidateFeatChoice("Level 3", stanceDesc, "Martial Spirit", "Feats");
            Assert.AreEqual(0, lvl3Warnings.Count, "Level 3 slot intrinsically satisfies Devoted Spirit requirement.");
        }

        [TestMethod]
        public void LevelDetailsDialog_WithItemDependentChoice_DisplaysItemDependentBadge() {
            var itemEffect = new Effect("Crown of the White Raven")
                .AddSlot(new SubsystemSlotGrant("martial_maneuvers", "maneuvers", "Crusader's Strike", "Item"));
            var crownItem = new Item {
                ItemSlot = Equipped.Slot.HEAD,
                ItemEffect = itemEffect,
                Weight = 1
            };

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
    }
}
