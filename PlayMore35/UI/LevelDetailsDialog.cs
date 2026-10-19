using PlayMore35.CharacterChoices;
using PlayMore35.Global;
using PlayMore35.Subsystems.Maneuvers;
using PlayMore35.UI;
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

        private class FeatSlotChoiceState {
            public string FeatSlot;
            public string FeatName;
            public Control SingleChoiceControl;
            public string AutoSelectedClass;
            public ComboBox ClassComboBox;
            public ComboBox ManeuverComboBox;
            public WarningBadge WarningBadge;
            public bool IsMultiChoice => FeatName == "Martial Study" || FeatName == "Martial Stance";
        }

        private Dictionary<string, FeatSlotChoiceState> slotChoiceStates = new Dictionary<string, FeatSlotChoiceState>();
        private Dictionary<string, List<Control>> dynamicSlotControls = new Dictionary<string, List<Control>>();

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
            ClearDynamicControls(this, choiceLabels);
            ClearDynamicControls(this, featChoices);

            foreach (var list in dynamicSlotControls.Values) {
                foreach (var c in list) {
                    Controls.Remove(c);
                }
            }
            dynamicSlotControls.Clear();
            slotChoiceStates.Clear();

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

                    PrepareFeatChoice(featName, featSlot, featLeft);
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

        public static string ExtractManeuverName(string formattedText) {
            if (string.IsNullOrWhiteSpace(formattedText)) return "";
            int closeBracket = formattedText.IndexOf("] ");
            if (formattedText.StartsWith("[") && closeBracket >= 0) {
                return formattedText.Substring(closeBracket + 2).Trim();
            }
            return formattedText.Trim();
        }

        private void PrepareFeatChoice(string featName, string featSlot, int left) {
            if (dynamicSlotControls.TryGetValue(featSlot, out var existingControls)) {
                foreach (var c in existingControls) {
                    Controls.Remove(c);
                }
                dynamicSlotControls.Remove(featSlot);
                slotChoiceStates.Remove(featSlot);
            }
            if (choiceLabels.ContainsKey(featSlot)) {
                Controls.Remove(choiceLabels[featSlot]);
                choiceLabels.Remove(featSlot);
            }
            if (featChoices.ContainsKey(featSlot)) {
                Controls.Remove(featChoices[featSlot]);
                featChoices.Remove(featSlot);
            }

            if (string.IsNullOrEmpty(featName)) {
                return;
            }

            if (featName == "Martial Study" || featName == "Martial Stance") {
                PrepareMartialChoice(featName, featSlot, left);
                return;
            }

            string choiceDescription = session.Catalog.GetFeat(featName)?.ChoiceDescription ?? "";
            if (!choiceDescription.Any()) {
                return;
            }

            var slotControls = new List<Control>();
            var choiceState = new FeatSlotChoiceState {
                FeatSlot = featSlot,
                FeatName = featName
            };

            Label header = new Label {
                Top = lblFeatChoice.Top,
                Left = left,
                AutoSize = true,
                Text = $"Choose {choiceDescription}",
            };
            choiceLabels[featSlot] = header;
            slotControls.Add(header);
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
            slotControls.Add(choiceBox);
            Controls.Add(choiceBox);
            choiceState.SingleChoiceControl = choiceBox;
            featChoices[featSlot] = choiceBox;

            dynamicSlotControls[featSlot] = slotControls;
            slotChoiceStates[featSlot] = choiceState;
        }

        private void PrepareMartialChoice(string featName, string featSlot, int left) {
            var slotControls = new List<Control>();
            var choiceState = new FeatSlotChoiceState {
                FeatSlot = featSlot,
                FeatName = featName
            };

            int slotLevel = session.GetFeatSlotLevel(featSlot);
            if (slotLevel <= 0) slotLevel = levelIndex + 1;

            var eligibleAdeptClasses = session.GetEligibleAdeptClassesAtLevel(slotLevel);

            int currentTop = lblFeatChoice.Top;

            if (eligibleAdeptClasses.Count >= 2) {
                Label classHeader = new Label {
                    Top = currentTop,
                    Left = left,
                    AutoSize = true,
                    Text = "Choose class"
                };
                slotControls.Add(classHeader);
                Controls.Add(classHeader);

                ComboBox classBox = new ComboBox {
                    Top = cboFeatChoice.Top,
                    Left = left,
                    Size = cboFeatChoice.Size,
                    Tag = $"{featSlot}:class"
                };
                foreach (string adeptClass in eligibleAdeptClasses.OrderBy(x => x)) {
                    classBox.Items.Add(adeptClass);
                }

                string savedClass = session.GetFeatChoice(featSlot, "class");
                if (!string.IsNullOrWhiteSpace(savedClass) && classBox.Items.Contains(savedClass)) {
                    classBox.SelectedItem = savedClass;
                } else {
                    classBox.SelectedIndex = 0;
                }

                slotControls.Add(classBox);
                Controls.Add(classBox);
                choiceState.ClassComboBox = classBox;

                currentTop = classBox.Bottom + 7;
            } else if (eligibleAdeptClasses.Count == 1) {
                choiceState.AutoSelectedClass = eligibleAdeptClasses[0];
            } else {
                choiceState.AutoSelectedClass = "Feats";
            }

            bool isStance = featName == "Martial Stance";
            Label maneuverHeader = new Label {
                Top = currentTop,
                Left = left,
                AutoSize = true,
                Text = isStance ? "Choose stance" : "Choose maneuver"
            };
            slotControls.Add(maneuverHeader);
            Controls.Add(maneuverHeader);

            WarningBadge badge = new WarningBadge {
                Top = maneuverHeader.Top,
                Left = left + maneuverHeader.PreferredWidth + 6,
                Tag = featSlot,
                Name = $"badge_{featSlot}"
            };
            slotControls.Add(badge);
            Controls.Add(badge);
            choiceState.WarningBadge = badge;

            ComboBox maneuverBox = new ComboBox {
                Top = currentTop + (cboFeatChoice.Top - lblFeatChoice.Top),
                Left = left,
                Size = cboFeatChoice.Size,
                DropDownWidth = 280,
                Tag = isStance ? $"{featSlot}:stance" : $"{featSlot}:maneuver"
            };

            var candidateManeuvers = session.Catalog.Maneuvers.Values
                .Where(m => isStance ? m.Type == ManeuverType.Stance : m.Type != ManeuverType.Stance)
                .OrderBy(m => m.Discipline)
                .ThenBy(m => m.Name);

            foreach (var m in candidateManeuvers) {
                maneuverBox.Items.Add($"[{m.Discipline}] {m.Name}");
            }

            string choiceKey = isStance ? "stance" : "maneuver";
            string savedManeuver = session.GetFeatChoice(featSlot, choiceKey);
            if (string.IsNullOrWhiteSpace(savedManeuver)) {
                savedManeuver = session.GetFeatChoice(featSlot);
            }

            if (!string.IsNullOrWhiteSpace(savedManeuver)) {
                string matchedItem = maneuverBox.Items.Cast<string>()
                    .FirstOrDefault(item => ExtractManeuverName(item).Equals(savedManeuver, StringComparison.OrdinalIgnoreCase) || item.Equals(savedManeuver, StringComparison.OrdinalIgnoreCase));
                if (matchedItem != null) {
                    maneuverBox.SelectedItem = matchedItem;
                }
            }

            slotControls.Add(maneuverBox);
            Controls.Add(maneuverBox);
            choiceState.ManeuverComboBox = maneuverBox;

            Action updateWarning = () => {
                string selectedText = maneuverBox.SelectedItem != null ? maneuverBox.SelectedItem.ToString() : maneuverBox.Text;
                string selectedManeuver = ExtractManeuverName(selectedText);
                if (string.IsNullOrWhiteSpace(selectedManeuver)) {
                    badge.ClearWarning();
                    return;
                }

                string target = choiceState.ClassComboBox != null ? choiceState.ClassComboBox.Text : choiceState.AutoSelectedClass;
                var engine = session.Subsystems?.Get<MartialManeuverEngine>();
                var warnings = engine != null
                    ? engine.ValidateFeatManeuver(featSlot, selectedManeuver, target)
                    : new List<string>();
                if (warnings.Count > 0) {
                    badge.SetWarning(string.Join("\n", warnings));
                } else {
                    badge.ClearWarning();
                }
            };

            maneuverBox.SelectedValueChanged += (s, e) => {
                if (maneuverBox.SelectedItem == null && !string.IsNullOrWhiteSpace(maneuverBox.Text)) {
                    string raw = maneuverBox.Text;
                    string matched = maneuverBox.Items.Cast<string>()
                        .FirstOrDefault(item => ExtractManeuverName(item).Equals(raw, StringComparison.OrdinalIgnoreCase) || item.Equals(raw, StringComparison.OrdinalIgnoreCase));
                    if (matched != null) {
                        maneuverBox.SelectedItem = matched;
                    }
                }
                updateWarning();
            };

            if (choiceState.ClassComboBox != null) {
                choiceState.ClassComboBox.SelectedValueChanged += (s, e) => updateWarning();
            }

            updateWarning();

            dynamicSlotControls[featSlot] = slotControls;
            slotChoiceStates[featSlot] = choiceState;
        }

        public void SelectClass(object sender, EventArgs e) {
            session.Mutate(c => c.classLevels[levelIndex] = cboClass.Text);
            PrepareFeats();
        }

        public EventHandler SelectFeat(string featSlot, int left) {
            return (object sender, EventArgs e) => {
                PrepareFeatChoice(
                    ((Control)sender).Text,
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
            var result = new Dictionary<string, string>();
            foreach (var state in slotChoiceStates.Values) {
                if (!state.IsMultiChoice) {
                    if (state.SingleChoiceControl != null && !string.IsNullOrWhiteSpace(state.SingleChoiceControl.Text)) {
                        result[state.FeatSlot] = state.SingleChoiceControl.Text;
                    }
                } else {
                    string targetClass = state.ClassComboBox != null ? state.ClassComboBox.Text : state.AutoSelectedClass;
                    if (!string.IsNullOrWhiteSpace(targetClass)) {
                        result[$"{state.FeatSlot}:class"] = targetClass;
                    }
                    string key = state.FeatName == "Martial Stance" ? "stance" : "maneuver";
                    string mName = state.ManeuverComboBox != null ? ExtractManeuverName(state.ManeuverComboBox.SelectedItem?.ToString() ?? state.ManeuverComboBox.Text) : "";
                    if (!string.IsNullOrWhiteSpace(mName)) {
                        result[$"{state.FeatSlot}:{key}"] = mName;
                    }
                }
            }
            return result;
        }

        public void SaveChanges() {
            session.Mutate(c => {
                c.classLevels[levelIndex] = GetClass();
                foreach (var feat in GetFeats()) {
                    c.feats[feat.Key] = feat.Value;
                }
                foreach (ComboBox featBox in feats) {
                    string slot = (string)featBox.Tag;
                    var keysToRemove = c.featChoices.Keys
                        .Where(k => k == slot || k.StartsWith(slot + ":"))
                        .ToList();
                    foreach (var k in keysToRemove) {
                        c.featChoices.Remove(k);
                    }
                }
                foreach (var featChoice in GetFeatChoices()) {
                    if (!string.IsNullOrWhiteSpace(featChoice.Value)) {
                        c.featChoices[featChoice.Key] = featChoice.Value;
                    }
                }
            });
        }
    }
}
