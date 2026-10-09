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
    }
}
