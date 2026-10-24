using PlayMore35.CharacterChoices;
using PlayMore35.Global;
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
            public Feat FeatDef;
            public Control SingleChoiceControl;
            public string AutoSelectedClass;
            public ComboBox ClassComboBox;
            public WarningBadge WarningBadge;
            public ChoiceDescriptor WarningDescriptor;
            public Dictionary<string, Control> ChoiceControls = new Dictionary<string, Control>();
            public Dictionary<string, string> AutoResolvedChoices = new Dictionary<string, string>();
            public bool IsMultiChoice => FeatDef != null && FeatDef.Choices.Any();
        }

        private class ChoiceItem {
            public string DisplayText { get; }
            public string Value { get; }

            public ChoiceItem(string displayText, string value) {
                DisplayText = displayText;
                Value = value;
            }

            public override string ToString() => DisplayText;

            public override bool Equals(object obj) {
                if (obj is ChoiceItem other) {
                    return string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(DisplayText, other.DisplayText, StringComparison.OrdinalIgnoreCase);
                }
                if (obj is string str) {
                    return string.Equals(Value, str, StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(DisplayText, str, StringComparison.OrdinalIgnoreCase);
                }
                return false;
            }

            public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
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
            UpdateWarnings();
        }

        private static string FormatChoiceHeader(ChoiceDescriptor desc) {
            if (!string.IsNullOrWhiteSpace(desc?.Label)) {
                return desc.Label.StartsWith("Choose ", StringComparison.OrdinalIgnoreCase)
                    ? desc.Label
                    : $"Choose {desc.Label.ToLower()}";
            }
            return desc != null ? $"Choose {desc.Id}" : "";
        }

        private string ResolveTargetSource(FeatSlotChoiceState state, ChoiceDescriptor desc) {
            string target = null;
            if (desc != null && !string.IsNullOrEmpty(desc.DependsOn)) {
                if (state.ChoiceControls.TryGetValue(desc.DependsOn, out var parentCtrl)) {
                    target = parentCtrl.Text;
                } else if (state.AutoResolvedChoices.TryGetValue(desc.DependsOn, out var autoVal)) {
                    target = autoVal;
                }
            }
            if (string.IsNullOrWhiteSpace(target)) {
                target = state.ClassComboBox != null ? state.ClassComboBox.Text : state.AutoSelectedClass;
            }
            if (string.IsNullOrWhiteSpace(target)) {
                target = session.ResolveFeatTargetSource(state.FeatSlot);
            }
            return target;
        }

        private static string ExtractBracketedName(string formattedText) {
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
                UpdateWarnings();
                return;
            }

            if (!session.Catalog.TryGetFeat(featName, out var featDef)) {
                UpdateWarnings();
                return;
            }
            if (featDef.Choices.Any()) {
                PrepareDeclarativeChoices(featDef, featSlot, left);
                return;
            }

            string choiceDescription = featDef.ChoiceDescription ?? "";
            if (!choiceDescription.Any()) {
                UpdateWarnings();
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
            UpdateWarnings();
        }

        private void PrepareDeclarativeChoices(Feat featDef, string featSlot, int left) {
            var slotControls = new List<Control>();
            var choiceState = new FeatSlotChoiceState {
                FeatSlot = featSlot,
                FeatName = featDef.Benefit?.Name ?? "",
                FeatDef = featDef
            };

            int slotLevel = session.GetFeatSlotLevel(featSlot);
            if (slotLevel <= 0) slotLevel = levelIndex + 1;

            int currentTop = lblFeatChoice.Top;

            foreach (var desc in featDef.Choices) {
                if (string.Equals(desc.Domain, "CharacterClasses", StringComparison.OrdinalIgnoreCase)) {
                    var eligibleClasses = session.GetEligibleClassesAtLevel(slotLevel, desc.Filter);
                    if (eligibleClasses.Count >= 2) {
                        string headerText = FormatChoiceHeader(desc);
                        Label classHeader = new Label {
                            Top = currentTop,
                            Left = left,
                            AutoSize = true,
                            Text = headerText
                        };
                        slotControls.Add(classHeader);
                        Controls.Add(classHeader);

                        ComboBox classBox = new ComboBox {
                            Top = currentTop + (cboFeatChoice.Top - lblFeatChoice.Top),
                            Left = left,
                            Size = cboFeatChoice.Size,
                            Tag = $"{featSlot}:{desc.Id}"
                        };
                        foreach (string eligibleClass in eligibleClasses.OrderBy(x => x)) {
                            classBox.Items.Add(eligibleClass);
                        }

                        string savedClass = session.GetFeatChoice(featSlot, desc.Id);
                        if (!string.IsNullOrWhiteSpace(savedClass) && classBox.Items.Contains(savedClass)) {
                            classBox.SelectedItem = savedClass;
                        } else {
                            classBox.SelectedIndex = 0;
                        }

                        classBox.SelectedValueChanged += (s, e) => UpdateWarnings();

                        slotControls.Add(classBox);
                        Controls.Add(classBox);
                        choiceState.ClassComboBox = classBox;
                        choiceState.ChoiceControls[desc.Id] = classBox;

                        currentTop = classBox.Bottom + 7;
                    } else if (eligibleClasses.Count == 1) {
                        choiceState.AutoSelectedClass = eligibleClasses[0];
                        choiceState.AutoResolvedChoices[desc.Id] = eligibleClasses[0];
                    } else {
                        string fallback = !string.IsNullOrEmpty(desc.Fallback) ? desc.Fallback : "Feats";
                        choiceState.AutoSelectedClass = fallback;
                        choiceState.AutoResolvedChoices[desc.Id] = fallback;
                    }
                } else if (string.Equals(desc.Domain, "SubsystemElements", StringComparison.OrdinalIgnoreCase)) {
                    string headerText = FormatChoiceHeader(desc);
                    Label header = new Label {
                        Top = currentTop,
                        Left = left,
                        AutoSize = true,
                        Text = headerText
                    };
                    slotControls.Add(header);
                    Controls.Add(header);

                    WarningBadge badge = new WarningBadge {
                        Top = header.Top,
                        Left = left + header.PreferredWidth + 6,
                        Tag = featSlot,
                        Name = $"badge_{featSlot}"
                    };
                    slotControls.Add(badge);
                    Controls.Add(badge);
                    choiceState.WarningBadge = badge;
                    choiceState.WarningDescriptor = desc;

                    ComboBox elementBox = new ComboBox {
                        Top = currentTop + (cboFeatChoice.Top - lblFeatChoice.Top),
                        Left = left,
                        Size = cboFeatChoice.Size,
                        DropDownWidth = 280,
                        Tag = $"{featSlot}:{desc.Id}"
                    };

                    string targetClass = ResolveTargetSource(choiceState, desc);

                    var elements = session.GetAccessibleSubsystemElements(desc.Domain, desc.Filter, targetClass);
                    foreach (var el in elements) {
                        elementBox.Items.Add(new ChoiceItem(el, ExtractBracketedName(el)));
                    }

                    string saved = session.GetFeatChoice(featSlot, desc.Id);
                    if (!string.IsNullOrWhiteSpace(saved)) {
                        var matchedItem = elementBox.Items.OfType<ChoiceItem>()
                            .FirstOrDefault(item => item.Value.Equals(saved, StringComparison.OrdinalIgnoreCase) || item.DisplayText.Equals(saved, StringComparison.OrdinalIgnoreCase));
                        if (matchedItem != null) {
                            elementBox.SelectedItem = matchedItem;
                        } else {
                            var elem = session.Subsystems?.All.Select(sub => sub.GetElement(saved)).FirstOrDefault(e => e != null);
                            string disc = elem?.GetProperty<string>("discipline") ?? "Unknown";
                            var extraItem = new ChoiceItem($"[{disc}] {saved}", saved);
                            elementBox.Items.Add(extraItem);
                            elementBox.SelectedItem = extraItem;
                        }
                    }

                    elementBox.SelectedValueChanged += (s, e) => {
                        if (elementBox.SelectedItem == null && !string.IsNullOrWhiteSpace(elementBox.Text)) {
                            string raw = elementBox.Text;
                            var matched = elementBox.Items.OfType<ChoiceItem>()
                                .FirstOrDefault(item => item.Value.Equals(raw, StringComparison.OrdinalIgnoreCase) || item.DisplayText.Equals(raw, StringComparison.OrdinalIgnoreCase));
                            if (matched != null) {
                                elementBox.SelectedItem = matched;
                            }
                        }
                        UpdateWarnings();
                    };
                    bool updatingText = false;
                    elementBox.TextChanged += (s, e) => {
                        if (updatingText) return;
                        if (elementBox.SelectedIndex != -1) {
                            var item = elementBox.SelectedItem as ChoiceItem;
                            if (item == null || !item.DisplayText.Equals(elementBox.Text, StringComparison.OrdinalIgnoreCase)) {
                                updatingText = true;
                                string currentText = elementBox.Text;
                                elementBox.SelectedIndex = -1;
                                elementBox.Text = currentText;
                                updatingText = false;
                            }
                        }
                        UpdateWarnings();
                    };

                    slotControls.Add(elementBox);
                    Controls.Add(elementBox);
                    choiceState.ChoiceControls[desc.Id] = elementBox;

                    currentTop = elementBox.Bottom + 7;
                }
            }

            dynamicSlotControls[featSlot] = slotControls;
            slotChoiceStates[featSlot] = choiceState;
            UpdateWarnings();
        }

        private void UpdateWarnings() {
            var currentChoices = GetFeatChoices();

            foreach (var state in slotChoiceStates.Values) {
                if (state.WarningBadge == null) {
                    continue;
                }

                var desc = state.WarningDescriptor;
                if (desc == null && state.FeatDef != null) {
                    desc = state.FeatDef.Choices.FirstOrDefault(c => string.Equals(c.Domain, "SubsystemElements", StringComparison.OrdinalIgnoreCase));
                }
                if (desc == null) {
                    state.WarningBadge.ClearWarning();
                    continue;
                }

                string selectedValue = "";
                if (state.ChoiceControls.TryGetValue(desc.Id, out var ctrl)) {
                    if (ctrl is ComboBox cbo) {
                        if (cbo.SelectedItem is ChoiceItem ci && (string.IsNullOrWhiteSpace(cbo.Text) || ci.DisplayText.Equals(cbo.Text, StringComparison.OrdinalIgnoreCase))) {
                            selectedValue = ci.Value;
                        } else {
                            selectedValue = ExtractBracketedName(cbo.Text);
                        }
                    } else {
                        selectedValue = ctrl.Text;
                    }
                }

                if (string.IsNullOrWhiteSpace(selectedValue)) {
                    state.WarningBadge.ClearWarning();
                    continue;
                }

                string target = ResolveTargetSource(state, desc);

                var warnings = session.ValidateFeatChoice(state.FeatSlot, desc, selectedValue, target, currentChoices);
                if (warnings.Count > 0) {
                    state.WarningBadge.SetWarning(string.Join("\n", warnings));
                } else {
                    state.WarningBadge.ClearWarning();
                }
            }
        }

        public void SelectClass(object sender, EventArgs e) {
            string selected = cboClass.Text;
            if (!string.IsNullOrWhiteSpace(selected)) {
                session.Mutate(c => c.classLevels[levelIndex] = selected);
            }
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
                if (state.FeatDef != null && state.FeatDef.Choices.Any()) {
                    foreach (var desc in state.FeatDef.Choices) {
                        string val = "";
                        if (state.ChoiceControls.TryGetValue(desc.Id, out var ctrl)) {
                            if (ctrl is ComboBox cbo) {
                                if (cbo.SelectedItem is ChoiceItem ci && (string.IsNullOrWhiteSpace(cbo.Text) || ci.DisplayText.Equals(cbo.Text, StringComparison.OrdinalIgnoreCase))) {
                                    val = ci.Value;
                                } else if (string.Equals(desc.Domain, "SubsystemElements", StringComparison.OrdinalIgnoreCase)) {
                                    val = ExtractBracketedName(cbo.Text);
                                } else {
                                    val = cbo.Text;
                                }
                            } else {
                                val = ctrl.Text;
                            }
                        } else if (state.AutoResolvedChoices.TryGetValue(desc.Id, out var autoVal)) {
                            val = autoVal;
                        }
                        if (!string.IsNullOrWhiteSpace(val)) {
                            result[$"{state.FeatSlot}:{desc.Id}"] = val;
                        }
                    }
                } else if (!state.IsMultiChoice) {
                    if (state.SingleChoiceControl != null && !string.IsNullOrWhiteSpace(state.SingleChoiceControl.Text)) {
                        result[state.FeatSlot] = state.SingleChoiceControl.Text;
                    }
                }
            }
            return result;
        }

        public void SaveChanges() {
            session.Mutate(c => {
                string selectedClass = GetClass();
                if (!string.IsNullOrWhiteSpace(selectedClass)) {
                    c.classLevels[levelIndex] = selectedClass;
                }
                foreach (var feat in GetFeats()) {
                    c.feats[feat.Key] = feat.Value;
                }
                foreach (ComboBox featBox in feats) {
                    string slot = (string)featBox.Tag;
                    if (string.IsNullOrWhiteSpace(featBox.Text)) {
                        c.feats.Remove(slot);
                    }
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
