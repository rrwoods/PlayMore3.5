using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Rules;
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
            Assert.AreEqual("Survival", decl1.DisplayName);
            Assert.IsNull(decl1.DisableCondition);
            Assert.IsNull(decl1.DisableReason);

            var decl2 = new SkillDeclaration(LISTEN, "Listen", "deafened");
            Assert.AreEqual(LISTEN, decl2.Skill);
            Assert.AreEqual("Listen", decl2.DisplayName);
            Assert.AreEqual("deafened", decl2.DisableCondition);
            Assert.AreEqual("Deafened", decl2.DisableReason);

            var decl3 = new SkillDeclaration(SPOT, "Spot", "blinded", "Sightless");
            Assert.AreEqual("blinded", decl3.DisableCondition);
            Assert.AreEqual("Sightless", decl3.DisableReason);
        }

        [TestMethod]
        public void SkillTextSectionPanel_HeaderStyles_ConfigureControlsCorrectly() {
            // Style 1: INTEGRATED
            var panelA = new SkillTextSectionPanel("Senses", SkillTextSectionPanel.HeaderStyle.INTEGRATED,
                new SkillDeclaration(LISTEN, disableCondition: "deafened"),
                new SkillDeclaration(SPOT, disableCondition: "blinded")
            );
            panelA.Initialize(null);

            Assert.IsNotNull(panelA.HeaderLabel);
            Assert.AreEqual("Senses:", panelA.HeaderLabel.Text);
            Assert.AreEqual(2, panelA.SkillLabels.Count);
            Assert.IsNotNull(panelA.EffectsPanel);

            // Style 2: DEDICATED_ROW
            var panelB = new SkillTextSectionPanel("Exploration", SkillTextSectionPanel.HeaderStyle.DEDICATED_ROW,
                new SkillDeclaration(SURVIVAL),
                new SkillDeclaration(SEARCH)
            );
            panelB.Initialize(null);

            Assert.IsNotNull(panelB.HeaderLabel);
            Assert.AreEqual("Exploration", panelB.HeaderLabel.Text);
            Assert.AreEqual(2, panelB.SkillLabels.Count);

            // Style 3: SKILLS_ONLY
            var panelC = new SkillTextSectionPanel("Social", SkillTextSectionPanel.HeaderStyle.SKILLS_ONLY,
                new SkillDeclaration(DIPLOMACY),
                new SkillDeclaration(BLUFF)
            );
            panelC.Initialize(null);

            Assert.IsNull(panelC.HeaderLabel);
            Assert.AreEqual(2, panelC.SkillLabels.Count);
        }

        [TestMethod]
        public void SkillTextSectionPanel_Update_HandlesActiveAndDisabledConditions() {
            var panel = new SkillTextSectionPanel("Senses", SkillTextSectionPanel.HeaderStyle.INTEGRATED,
                new SkillDeclaration(LISTEN, disableCondition: "deafened"),
                new SkillDeclaration(SPOT, disableCondition: "blinded")
            );

            var character = new Character();
            var session = new CharacterSession(character, catalog);

            using (var form = new MainForm()) {
                form.Session = session;
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
    }
}
