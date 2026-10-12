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
    public class TextEffectPanelTests {
        private IRulesCatalog catalog;

        [TestInitialize]
        public void Setup() {
            catalog = new GameRulesCatalog();
        }

        [TestMethod]
        public void TextEffectPanel_ControllableFlagWithResource_CompensatesTopMarginForSubsequentControls() {
            var panel = new TextEffectPanel {
                Tag = ATK_OPTIONS,
                DisplayStyle = TextEffectPanel.DisplayMode.MULTI_LINE_WITHOUT_HEADER
            };

            var character = new Character();
            character.classLevels[0] = "Barbarian";
            var session = new CharacterSession(character, catalog);

            using (var form = new MainForm(session)) {
                panel.Initialize(form);
                var textEffects = session.GetTextEffects();
                panel.Update(textEffects);

                // Find the whirling frenzy controls in the panel
                var flagBox = panel.Controls.OfType<CheckBox>().FirstOrDefault(c => c.Text == "whirling frenzy");
                Assert.IsNotNull(flagBox, "whirling frenzy CheckBox should exist.");
                Assert.AreEqual(0, flagBox.Margin.Top, "CheckBox Margin.Top should be 0.");

                var usesLabel = panel.Controls.OfType<Label>().FirstOrDefault(c => c.Text.Contains("day"));
                Assert.IsNotNull(usesLabel, "usesLabel should exist for whirling frenzy.");
                Assert.AreEqual(1, usesLabel.Margin.Top, "usesLabel should have 1px top margin to compensate for CheckBox vertical centering.");

                var usedField = panel.Controls.Cast<Control>().FirstOrDefault(c => c.GetType().Name == "LabeledField");
                Assert.IsNotNull(usedField, "usedField should exist for whirling frenzy.");
                Assert.AreEqual(1, usedField.Margin.Top, "usedField should have 1px top margin to compensate for CheckBox vertical centering.");
            }
        }

        [TestMethod]
        public void TextEffectPanel_EffectWithInlineParameterAndAssociatedActions_RendersInlineLabeledFieldAndContextMenu() {
            var panel = new TextEffectPanel {
                Tag = SOCIAL,
                DisplayStyle = TextEffectPanel.DisplayMode.MULTI_LINE_WITHOUT_HEADER
            };

            var character = new Character();
            var session = new CharacterSession(character, catalog);
            var context = (ICharacterContext)session;

            var namedAction = new SheetAction { Name = "Add Stack" }
                .AdjustParam("Test Aura stacks", 1);
            var unnamedAction = new SheetAction()
                .AdjustParam("Test Aura stacks", 2);

            var effect = new Effect("Test Aura")
                .SetParameter("stacks")
                .AddAction(namedAction)
                .AddAction(unnamedAction)
                .AddText(SOCIAL, "Aura social description.");

            context.Effects.AddEffect(effect);

            using (var form = new MainForm(session)) {
                panel.Initialize(form);
                panel.Update(session.GetTextEffects());

                var effectLabel = panel.Controls.OfType<Label>().FirstOrDefault(l => l.Text == FormUtils.INDENT + "Test Aura");
                Assert.IsNotNull(effectLabel, "Effect label should be rendered.");
                Assert.IsTrue(effectLabel.Visible);
                Assert.IsFalse(panel.GetFlowBreak(effectLabel), "Effect label should not break flow before its inline LabeledField.");

                var paramField = panel.Controls.OfType<LabeledField>().FirstOrDefault(f => f.lblName.Text == "stacks");
                Assert.IsNotNull(paramField, "Inline LabeledField with ParameterLabel should be rendered.");
                Assert.IsTrue(paramField.Visible);
                Assert.AreEqual(0, paramField.Value);
                Assert.IsTrue(panel.GetFlowBreak(paramField), "Inline LabeledField should end the flow row.");

                Assert.IsNotNull(effectLabel.ContextMenuStrip, "Effect label should have ContextMenuStrip for associatedAction entries.");
                Assert.AreEqual(2, effectLabel.ContextMenuStrip.Items.Count);
                Assert.AreEqual("Add Stack", effectLabel.ContextMenuStrip.Items[0].Text);
                Assert.AreEqual("Use Test Aura", effectLabel.ContextMenuStrip.Items[1].Text);

                effectLabel.ContextMenuStrip.Items[0].PerformClick();
                panel.Update(session.GetTextEffects());
                Assert.AreEqual(1, session.Character.paramValues["Test Aura stacks"]);
                Assert.AreEqual(1, paramField.Value);

                effectLabel.ContextMenuStrip.Items[1].PerformClick();
                panel.Update(session.GetTextEffects());
                Assert.AreEqual(3, session.Character.paramValues["Test Aura stacks"]);
                Assert.AreEqual(3, paramField.Value);

                // Removing the effect hides both the label and its inline parameter field
                context.Effects.RemoveEffect("Test Aura");
                panel.Update(session.GetTextEffects());
                Assert.IsFalse(effectLabel.Visible);
                Assert.IsFalse(paramField.Visible);
            }
        }

        [TestMethod]
        public void TextEffectPanel_MultiCategoryEffectWithInlineParameter_SynchronizesInlineLabeledFieldsAcrossPanels() {
            var socialPanel = new TextEffectPanel {
                Tag = SOCIAL,
                DisplayStyle = TextEffectPanel.DisplayMode.MULTI_LINE_WITHOUT_HEADER
            };
            var actionsPanel = new TextEffectPanel {
                Tag = ACTIONS,
                DisplayStyle = TextEffectPanel.DisplayMode.MULTI_LINE_WITH_HEADER
            };

            var character = new Character();
            var session = new CharacterSession(character, catalog);
            var context = (ICharacterContext)session;

            var effect = new Effect("Multi Category Aura")
                .SetParameter("counter")
                .AddText(SOCIAL, "Social part.")
                .AddText(ACTIONS, "Actions part.");

            context.Effects.AddEffect(effect);

            using (var form = new MainForm(session)) {
                socialPanel.Initialize(form);
                actionsPanel.Initialize(form);

                var textEffects = session.GetTextEffects();
                socialPanel.Update(textEffects);
                actionsPanel.Update(textEffects);

                var socialField = socialPanel.Controls.OfType<LabeledField>().FirstOrDefault(f => f.lblName.Text == "counter");
                var actionsField = actionsPanel.Controls.OfType<LabeledField>().FirstOrDefault(f => f.lblName.Text == "counter");

                Assert.IsNotNull(socialField, "Social panel should render inline LabeledField.");
                Assert.IsNotNull(actionsField, "Actions panel should render inline LabeledField.");
                Assert.AreEqual(0, socialField.Value);
                Assert.AreEqual(0, actionsField.Value);

                session.SetParameter("Multi Category Aura counter", 4);
                textEffects = session.GetTextEffects();
                socialPanel.Update(textEffects);
                actionsPanel.Update(textEffects);

                Assert.AreEqual(4, socialField.Value, "Social panel inline LabeledField should synchronize with updated parameter.");
                Assert.AreEqual(4, actionsField.Value, "Actions panel inline LabeledField should synchronize with updated parameter.");
                Assert.AreSame(socialField, socialPanel.Controls.OfType<LabeledField>().Single(f => f.lblName.Text == "counter"), "Social panel should recycle its LabeledField.");
                Assert.AreSame(actionsField, actionsPanel.Controls.OfType<LabeledField>().Single(f => f.lblName.Text == "counter"), "Actions panel should recycle its LabeledField.");
            }
        }

        [TestMethod]
        public void TextEffectPanel_RecycledEffectLabel_UpdatesContextMenuStripWhenAssociatedActionsChange() {
            var panel = new TextEffectPanel {
                Tag = SOCIAL,
                DisplayStyle = TextEffectPanel.DisplayMode.MULTI_LINE_WITHOUT_HEADER
            };

            var character = new Character();
            var session = new CharacterSession(character, catalog);
            var context = (ICharacterContext)session;

            var initialAction = new SheetAction { Name = "Initial Action" }
                .AdjustParam("Recycled Aura stacks", 1);

            var initialEffect = new Effect("Recycled Aura")
                .SetParameter("stacks")
                .AddAction(initialAction)
                .AddText(SOCIAL, "Initial social description.");

            context.Effects.AddEffect(initialEffect);

            using (var form = new MainForm(session)) {
                panel.Initialize(form);
                panel.Update(session.GetTextEffects());

                var effectLabel = panel.Controls.OfType<Label>().Single(l => l.Text == FormUtils.INDENT + "Recycled Aura");
                Assert.IsNotNull(effectLabel.ContextMenuStrip);
                Assert.AreEqual(1, effectLabel.ContextMenuStrip.Items.Count);
                Assert.AreEqual("Initial Action", effectLabel.ContextMenuStrip.Items[0].Text);

                var updatedEffect = new Effect("Recycled Aura")
                    .SetParameter("stacks")
                    .AddAction(new SheetAction { Name = "Updated Action 1" }.AdjustParam("Recycled Aura stacks", 2))
                    .AddAction(new SheetAction { Name = "Updated Action 2" }.AdjustParam("Recycled Aura stacks", 5))
                    .AddText(SOCIAL, "Updated social description.");

                context.Effects.ReplaceEffect(updatedEffect);
                panel.Update(session.GetTextEffects());

                var recycledLabel = panel.Controls.OfType<Label>().Single(l => l.Text == FormUtils.INDENT + "Recycled Aura");
                Assert.AreSame(effectLabel, recycledLabel, "Effect label should be recycled.");
                Assert.IsNotNull(recycledLabel.ContextMenuStrip);
                Assert.AreEqual(2, recycledLabel.ContextMenuStrip.Items.Count);
                Assert.AreEqual("Updated Action 1", recycledLabel.ContextMenuStrip.Items[0].Text);
                Assert.AreEqual("Updated Action 2", recycledLabel.ContextMenuStrip.Items[1].Text);

                recycledLabel.ContextMenuStrip.Items[1].PerformClick();
                panel.Update(session.GetTextEffects());
                Assert.AreEqual(5, session.Character.paramValues["Recycled Aura stacks"]);
            }
        }
    }
}

