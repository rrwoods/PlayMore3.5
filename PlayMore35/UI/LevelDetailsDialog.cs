using PlayMore35.CharacterChoices;
using PlayMore35.Global;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static PlayMore35.Global.FormUtils;

namespace PlayMore35 {
    public partial class LevelDetailsDialog : Form {
        private ICharacterSession session;

        private int levelIndex;
        private List<Label> slotNames = new List<Label>();
        private List<ComboBox> feats = new List<ComboBox>();
        private Dictionary<string, Label> choiceLabels = new Dictionary<string, Label>();
        private Dictionary<string, Control> featChoices = new Dictionary<string, Control>();

        public LevelDetailsDialog(ICharacterSession session) {
            InitializeComponent();
            this.session = session;

            foreach (string className in session.Catalog.Classes.Keys.OrderBy(x => x)) {
                cboClass.Items.Add(className);
            }
        }

        public void PrepareLevel(int levelIndex) {
            // this MUST come first, because the SelectedItem change below triggers the SelectedItemChanged event.
            this.levelIndex = levelIndex;

            Text = $"Level {levelIndex + 1}";
            cboClass.SelectedItem = session.Character.classLevels[levelIndex];
            PrepareFeats();
        }

        private void PrepareFeats() {
            ClearDynamicControls(this, feats);
            ClearDynamicControls(this, slotNames);

            SortedDictionary<int, List<string>> featSlots = session.GetFeatSlots();
            if (!featSlots.ContainsKey(levelIndex + 1)) {
                return;
            }

            int featLeft = cboFeat1.Left;
            foreach (string featSlot in featSlots[levelIndex + 1]) {
                ComboBox featBox = new ComboBox {
                    Top = cboFeat1.Top,
                    Left = featLeft,
                    Size = cboFeat1.Size,
                    Tag = featSlot
                };
                foreach (string availableFeat in availableFeats) {
                    featBox.Items.Add(availableFeat);
                }
                if (session.Character.feats.ContainsKey(featSlot)) {
                    string featName = session.Character.feats[featSlot];
                    featBox.SelectedItem = featName;

                    PrepareFeatChoice(session.Catalog.GetFeat(featName)?.ChoiceDescription ?? "", featSlot, featLeft);
                }
                featBox.SelectedValueChanged += SelectFeat(featSlot, featLeft);
                feats.Add(featBox);
                Controls.Add(featBox);

                Label slotName = new Label {
                    Top = lblFeatSlot1.Top,
                    Left = featLeft,
                    Text = $"Feat: {featSlot}"
                };
                slotNames.Add(slotName);
                Controls.Add(slotName);

                featLeft += cboFeat2.Left - cboFeat1.Left;
            }
        }

        private void PrepareFeatChoice(string choiceDescription, string featSlot, int left) {
            if (featChoices.ContainsKey(featSlot)) {
                Controls.Remove(choiceLabels[featSlot]);
                choiceLabels.Remove(featSlot);
                Controls.Remove(featChoices[featSlot]);
                featChoices.Remove(featSlot);
            }
            if (!choiceDescription.Any()) {
                return;
            }

            Label header = new Label {
                Top = lblFeatChoice.Top,
                Left = left,
                AutoSize = true,
                Text = $"Choose {choiceDescription}",
            };
            choiceLabels[featSlot] = header;
            Controls.Add(header);

            IEnumerable<string> choices = session.Catalog.ChoicesForKey(choiceDescription);
            Control choiceBox;
            if (choices.Any()) {
                choiceBox = new ComboBox {
                    Top = cboFeatChoice.Top,
                    Left = left,
                    Size = cboFeatChoice.Size,
                    Tag = featSlot,
                };
                foreach (string choice in choices) {
                    ((ComboBox)choiceBox).Items.Add(choice);
                }
            } else {
                choiceBox = new TextBox {
                    Top = cboFeatChoice.Top,
                    Left = left,
                    Size = cboFeatChoice.Size,
                    Tag = featSlot,
                };
            }
            if (session.Character.featChoices.ContainsKey(featSlot)) {
                choiceBox.Text = session.Character.featChoices[featSlot];
            }
            featChoices[featSlot] = choiceBox;
            Controls.Add(choiceBox);
        }

        public void SelectClass(object sender, EventArgs e) {
            session.Character.classLevels[levelIndex] = cboClass.Text;
            PrepareFeats();
        }

        public EventHandler SelectFeat(string featSlot, int left) {
            return (object sender, EventArgs e) => {
                PrepareFeatChoice(
                    session.Catalog.GetFeat(((Control)sender).Text)?.ChoiceDescription ?? "",
                    featSlot,
                    left
                );
            };
        }

        public string GetClass() {
            return cboClass.Text;
        }

        public Dictionary<string, string> GetFeats() {
            return Enumerable.Range(0, feats.Count)
                .Where(i => feats[i].Text.Any())
                .ToDictionary(i => (string) feats[i].Tag, i => feats[i].Text);
        }

        public Dictionary<string, string> GetFeatChoices() {
            return featChoices.ToDictionary(x => x.Key, x => x.Value.Text);
        }
    }
}
