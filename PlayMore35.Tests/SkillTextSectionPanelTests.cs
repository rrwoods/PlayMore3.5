using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Rules;
using PlayMore35.Subsystems.PactMagic;
using static PlayMore35.Effects.Constants;

namespace PlayMore35.Tests {
    [TestClass]
    public class SkillTextSectionPanelTests {
        private IRulesCatalog catalog;

        [TestInitialize]
        public void Setup() {
            catalog = new GameRulesCatalog();
        }

        [TestMethod]
        public void SkillDeclaration_InitializesCleanlyWithDefaults() {
            var decl1 = new SkillDeclaration("Survival");
            Assert.AreEqual("Survival", decl1.Skill);
            Assert.IsNull(decl1.DisableCondition);
            Assert.IsNull(decl1.DisableReason);

            var decl2 = new SkillDeclaration(LISTEN, "deafened");
            Assert.AreEqual(LISTEN, decl2.Skill);
            Assert.AreEqual("deafened", decl2.DisableCondition);
            Assert.AreEqual("Deafened", decl2.DisableReason);

            var decl3 = new SkillDeclaration(SPOT, "blinded", "Sightless");
            Assert.AreEqual(SPOT, decl3.Skill);
            Assert.AreEqual("blinded", decl3.DisableCondition);
            Assert.AreEqual("Sightless", decl3.DisableReason);
        }

        [TestMethod]
        public void SkillTextSectionPanel_InitializesControlsCorrectly() {
            var panel = new SkillTextSectionPanel("Senses",
                new SkillDeclaration(LISTEN, disableCondition: "deafened"),
                new SkillDeclaration(SPOT, disableCondition: "blinded")
            );
            panel.Initialize(null);

            Assert.IsNotNull(panel.HeaderLabel);
            Assert.AreEqual("Senses:", panel.HeaderLabel.Text);
            Assert.AreEqual(2, panel.SkillLabels.Count);
            Assert.IsNotNull(panel.EffectsPanel);
        }

        [TestMethod]
        public void SkillTextSectionPanel_Update_HandlesActiveAndDisabledConditions() {
            var panel = new SkillTextSectionPanel("Senses",
                new SkillDeclaration(LISTEN, disableCondition: "deafened"),
                new SkillDeclaration(SPOT, disableCondition: "blinded")
            );

            var character = new Character();
            var session = new CharacterSession(character, catalog);

            using (var form = new MainForm(session)) {
                panel.Initialize(form);

                var textEffects = session.GetTextEffects();

                // When neither condition is active:
                panel.Update(textEffects);
                Assert.IsTrue(panel.SkillLabels[0].Text.StartsWith("Listen"));
                Assert.IsTrue(panel.SkillLabels[0].Text.EndsWith(","));
                Assert.IsFalse(panel.SkillLabels[0].Text.Contains("\u2014"));
                Assert.IsTrue(panel.SkillLabels[1].Text.StartsWith("Spot"));
                Assert.IsFalse(panel.SkillLabels[1].Text.EndsWith(","));
                Assert.IsFalse(panel.SkillLabels[1].Text.Contains("\u2014"));

                var context = (ICharacterContext)session;
                context.Effects.controllableFlags["deafened"] = new ControllableFlag("deafened", character);
                context.Effects.controllableFlags["blinded"] = new ControllableFlag("blinded", character);

                // When deafened:
                session.SetFlag("deafened", true);
                panel.Update(textEffects);
                Assert.AreEqual("Listen \u2014,", panel.SkillLabels[0].Text);
                Assert.IsFalse(panel.SkillLabels[1].Text.Contains("\u2014"));

                // When blinded as well:
                session.SetFlag("blinded", true);
                panel.Update(textEffects);
                Assert.AreEqual("Listen \u2014,", panel.SkillLabels[0].Text);
                Assert.AreEqual("Spot \u2014", panel.SkillLabels[1].Text);
            }
        }

        [TestMethod]
        public void SkillTextSectionPanel_ExplorationPanel_UpdatesWithSurvivalSearchAndExplorationTextEffects() {
            var panel = new SkillTextSectionPanel();
            panel.Category = EXPLORATION;
            panel.SkillDeclarations.Add(new SkillDeclaration(SURVIVAL));
            panel.SkillDeclarations.Add(new SkillDeclaration(SEARCH));

            var character = new Character();
            var session = new CharacterSession(character, catalog);

            using (var form = new MainForm(session)) {
                panel.Initialize(form);

                Assert.AreEqual("Exploration:", panel.HeaderLabel.Text);
                Assert.AreEqual(2, panel.SkillLabels.Count);
                Assert.AreEqual(EXPLORATION, panel.EffectsPanel.Tag);

                // Give character Track and Endurance feats
                session.Mutate(c => {
                    c.feats["General 1"] = "Track";
                    c.feats["General 3"] = "Endurance";
                });

                var textEffects = session.GetTextEffects();
                Assert.IsTrue(textEffects.ContainsKey(EXPLORATION), "Exploration category must be present in text effects.");
                Assert.IsTrue(textEffects[EXPLORATION].Any(t => t.Name == "Track"), "Track effect should be in Exploration.");
                Assert.IsTrue(textEffects[EXPLORATION].Any(t => t.Name == "Endurance"), "Endurance effect should be in Exploration.");

                panel.Update(textEffects);

                // Verify single-row skill labels with commas
                Assert.IsTrue(panel.SkillLabels[0].Text.StartsWith("Survival"));
                Assert.IsTrue(panel.SkillLabels[0].Text.EndsWith(","));
                Assert.IsTrue(panel.SkillLabels[1].Text.StartsWith("Search"));
                Assert.IsFalse(panel.SkillLabels[1].Text.EndsWith(","));

                // Verify effects panel is visible and contains the exploration text effects
                Assert.IsTrue(panel.EffectsPanel.Visible, "Effects panel should be visible when character has exploration text effects.");
                Assert.IsTrue(panel.Visible, "Exploration section panel should be visible.");
            }
        }

        [TestMethod]
        public void SkillTextSectionPanel_SocialPanel_UpdatesWithDiplomacyBluffAndVestigeInfluenceTextEffects() {
            var panel = new SkillTextSectionPanel();
            panel.Category = SOCIAL;
            panel.SkillDeclarations.Add(new SkillDeclaration(DIPLOMACY));
            panel.SkillDeclarations.Add(new SkillDeclaration(BLUFF));

            var character = new Character();
            character.classLevels[0] = "Binder";
            character.assignedAbilityScores[CHA] = 14;
            character.skillRanks[DIPLOMACY] = new List<int> { 4 };
            character.skillRanks[BLUFF] = new List<int> { 2 };
            var session = new CharacterSession(character, catalog);

            using (var form = new MainForm(session)) {
                panel.Initialize(form);

                Assert.AreEqual("Social:", panel.HeaderLabel.Text);
                Assert.AreEqual(2, panel.SkillLabels.Count);
                Assert.AreEqual(SOCIAL, panel.EffectsPanel.Tag);

                // Bind a vestige with Social influence (Aym) with poor pact so influence is contributed
                var pactEngine = session.Subsystems.Get<PactMagicEngine>();
                Assert.IsNotNull(pactEngine, "PactMagicEngine should be registered.");
                Assert.IsTrue(pactEngine.IsActive, "PactMagicEngine should be active for Binder.");
                pactEngine.BindVestige(0, "Aym", PactState.POOR_PACT);
                session.Recalculate();

                var textEffects = session.GetTextEffects();
                Assert.IsTrue(textEffects.ContainsKey(SOCIAL), "Social category must be present in text effects.");
                Assert.IsTrue(textEffects[SOCIAL].Any(t => t.Name == "Aym's influence"), "Aym's influence should be in Social.");

                panel.Update(textEffects);

                // Verify single-row skill labels with modifiers and commas
                Assert.AreEqual("Diplomacy +6,", panel.SkillLabels[0].Text);
                Assert.AreEqual("Bluff +4", panel.SkillLabels[1].Text);

                // Verify effects panel is visible and contains vestige influence text effects indented
                Assert.IsTrue(panel.EffectsPanel.Visible, "Effects panel should be visible when vestige influence is active.");
                Assert.IsTrue(panel.Visible, "Social section panel should be visible.");
            }
        }

        [TestMethod]
        public void MainForm_Column0_StackingOrderMatchesSpec() {
            using (var form = new MainForm()) {
                var field = typeof(MainForm).GetField("floBeforeCombat", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.IsNotNull(field, "floBeforeCombat field should exist.");
                var floBeforeCombat = field.GetValue(form) as FlowLayoutPanel;
                Assert.IsNotNull(floBeforeCombat, "floBeforeCombat should be a FlowLayoutPanel.");

                var controlNames = floBeforeCombat.Controls.Cast<Control>().Select(c => c.Name).ToList();
                var expectedOrder = new List<string> {
                    "descriptionPanel",
                    "explorationPanel",
                    "socialPanel",
                    "initiativePanel",
                    "sensesPanel",
                    "evadePanel",
                    "conditionPanel"
                };
                CollectionAssert.AreEqual(expectedOrder, controlNames);

                foreach (Control ctrl in floBeforeCombat.Controls) {
                    Assert.AreEqual(307, ctrl.Width, $"Control {ctrl.Name} should have width 307px");
                }
            }
        }

        [TestMethod]
        public void SkillTextSectionPanel_HeaderAndSkillLabels_TopMarginsAligned() {
            var panel = new SkillTextSectionPanel("Exploration",
                new SkillDeclaration(SURVIVAL),
                new SkillDeclaration(SEARCH)
            );
            panel.Initialize(null);

            Assert.AreEqual(new Padding(3, 0, 0, 0), panel.HeaderLabel.Margin);
            Assert.AreEqual(0, panel.HeaderLabel.Margin.Top);
            foreach (var label in panel.SkillLabels) {
                Assert.AreEqual(panel.HeaderLabel.Margin.Top, label.Margin.Top);
            }
        }
    }
}
