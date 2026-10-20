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
            public Feat FeatDef;
            public Control SingleChoiceControl;
            public string AutoSelectedClass;
            public ComboBox ClassComboBox;
            public ComboBox ManeuverComboBox;
            public WarningBadge WarningBadge;
            public ChoiceDescriptor WarningDescriptor;
            public Dictionary<string, Control> ChoiceControls = new Dictionary<string, Control>();
            public Dictionary<string, string> AutoResolvedChoices = new Dictionary<string, string>();
            public bool IsMultiChoice => FeatDef != null && FeatDef.Choices != null && FeatDef.Choices.Any();
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
                UpdateWarnings();
                return;
            }

            var featDef = session.Catalog.GetFeat(featName);
            if (featDef?.Choices != null && featDef.Choices.Any()) {
                PrepareDeclarativeChoices(featDef, featSlot, left);
                return;
            }

            string choiceDescription = featDef?.ChoiceDescription ?? "";
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
                        string headerText = !string.IsNullOrWhiteSpace(desc.Label)
                            ? (desc.Label.StartsWith("Choose ", StringComparison.OrdinalIgnoreCase) ? desc.Label : $"Choose {desc.Label.ToLower()}")
                            : "Choose class";
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
                        if (string.IsNullOrWhiteSpace(savedClass)) {
                            savedClass = session.GetFeatChoice(featSlot);
                        }
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
                    string headerText = !string.IsNullOrWhiteSpace(desc.Label)
                        ? (desc.Label.StartsWith("Choose ", StringComparison.OrdinalIgnoreCase) ? desc.Label : $"Choose {desc.Label.ToLower()}")
                        : $"Choose {desc.Id}";
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

                    string targetClass = null;
                    if (!string.IsNullOrEmpty(desc.DependsOn)) {
                        if (choiceState.ChoiceControls.TryGetValue(desc.DependsOn, out var parentCtrl)) {
                            targetClass = parentCtrl.Text;
                            if (parentCtrl is ComboBox parentCombo) {
                                parentCombo.SelectedValueChanged += (s, e) => UpdateWarnings();
                            }
                        } else if (choiceState.AutoResolvedChoices.TryGetValue(desc.DependsOn, out var autoVal)) {
                            targetClass = autoVal;
                        }
                    }
                    if (string.IsNullOrWhiteSpace(targetClass)) {
                        targetClass = choiceState.AutoSelectedClass ?? session.ResolveFeatTargetSource(featSlot);
                    }

                    var elements = session.GetAccessibleSubsystemElements(desc.Domain, desc.Filter, targetClass, slotLevel);
                    foreach (var el in elements) {
                        elementBox.Items.Add(el);
                    }

                    string saved = session.GetFeatChoice(featSlot, desc.Id);
                    if (string.IsNullOrWhiteSpace(saved)) {
                        saved = session.GetFeatChoice(featSlot);
                    }
                    if (!string.IsNullOrWhiteSpace(saved)) {
                        string matchedItem = elementBox.Items.Cast<string>()
                            .FirstOrDefault(item => ExtractManeuverName(item).Equals(saved, StringComparison.OrdinalIgnoreCase) || item.Equals(saved, StringComparison.OrdinalIgnoreCase));
                        if (matchedItem != null) {
                            elementBox.SelectedItem = matchedItem;
                        }
                    }

                    elementBox.SelectedValueChanged += (s, e) => {
                        if (elementBox.SelectedItem == null && !string.IsNullOrWhiteSpace(elementBox.Text)) {
                            string raw = elementBox.Text;
                            string matched = elementBox.Items.Cast<string>()
                                .FirstOrDefault(item => ExtractManeuverName(item).Equals(raw, StringComparison.OrdinalIgnoreCase) || item.Equals(raw, StringComparison.OrdinalIgnoreCase));
                            if (matched != null) {
                                elementBox.SelectedItem = matched;
                            }
                        }
                        UpdateWarnings();
                    };

                    slotControls.Add(elementBox);
                    Controls.Add(elementBox);
                    choiceState.ChoiceControls[desc.Id] = elementBox;
                    choiceState.ManeuverComboBox = elementBox;

                    currentTop = elementBox.Bottom + 7;
                } else {
                    string headerText = !string.IsNullOrWhiteSpace(desc.Label)
                        ? (desc.Label.StartsWith("Choose ", StringComparison.OrdinalIgnoreCase) ? desc.Label : $"Choose {desc.Label.ToLower()}")
                        : $"Choose {desc.Id}";
                    Label header = new Label {
                        Top = currentTop,
                        Left = left,
                        AutoSize = true,
                        Text = headerText
                    };
                    slotControls.Add(header);
                    Controls.Add(header);

                    IEnumerable<string> domainChoices = session.Catalog.ChoicesForKey(desc.Domain);
                    Control choiceBox;
                    if (domainChoices.Any()) {
                        var cbo = new ComboBox {
                            Top = currentTop + (cboFeatChoice.Top - lblFeatChoice.Top),
                            Left = left,
                            Size = cboFeatChoice.Size,
                            Tag = $"{featSlot}:{desc.Id}"
                        };
                        foreach (string ch in domainChoices) {
                            cbo.Items.Add(ch);
                        }
                        string saved = session.GetFeatChoice(featSlot, desc.Id);
                        if (!string.IsNullOrEmpty(saved) && cbo.Items.Contains(saved)) {
                            cbo.SelectedItem = saved;
                        } else if (cbo.Items.Count > 0) {
                            cbo.SelectedIndex = 0;
                        }
                        choiceBox = cbo;
                    } else {
                        choiceBox = new TextBox {
                            Top = currentTop + (cboFeatChoice.Top - lblFeatChoice.Top),
                            Left = left,
                            Size = cboFeatChoice.Size,
                            Tag = $"{featSlot}:{desc.Id}"
                        };
                        string saved = session.GetFeatChoice(featSlot, desc.Id);
                        if (!string.IsNullOrEmpty(saved)) {
                            choiceBox.Text = saved;
                        }
                    }
                    slotControls.Add(choiceBox);
                    Controls.Add(choiceBox);
                    choiceState.ChoiceControls[desc.Id] = choiceBox;
                    currentTop = choiceBox.Bottom + 7;
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
                if (desc == null && state.FeatDef?.Choices != null) {
                    desc = state.FeatDef.Choices.FirstOrDefault(c => string.Equals(c.Domain, "SubsystemElements", StringComparison.OrdinalIgnoreCase));
                }
                if (desc == null) {
                    desc = new ChoiceDescriptor("maneuver", "Choose maneuver", "SubsystemElements", "maneuver", "", "class");
                }

                string selectedText = "";
                if (state.ChoiceControls.TryGetValue(desc.Id, out var ctrl)) {
                    selectedText = ctrl is ComboBox cbo && cbo.SelectedItem != null 
                        ? cbo.SelectedItem.ToString() 
                        : ctrl.Text;
                } else if (state.ManeuverComboBox != null) {
                    selectedText = state.ManeuverComboBox.SelectedItem != null 
                        ? state.ManeuverComboBox.SelectedItem.ToString() 
                        : state.ManeuverComboBox.Text;
                }

                string selectedValue = ExtractManeuverName(selectedText);
                if (string.IsNullOrWhiteSpace(selectedValue)) {
                    state.WarningBadge.ClearWarning();
                    continue;
                }

                string target = null;
                if (!string.IsNullOrEmpty(desc.DependsOn)) {
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

                var warnings = session.ValidateFeatChoice(state.FeatSlot, desc, selectedValue, target, currentChoices);
                if (warnings.Count > 0) {
                    state.WarningBadge.SetWarning(string.Join("\n", warnings));
                } else {
                    state.WarningBadge.ClearWarning();
                }
            }
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
                if (state.FeatDef != null && state.FeatDef.Choices != null && state.FeatDef.Choices.Any()) {
                    foreach (var desc in state.FeatDef.Choices) {
                        string val = "";
                        if (state.ChoiceControls.TryGetValue(desc.Id, out var ctrl)) {
                            string text = ctrl is ComboBox cbo && cbo.SelectedItem != null 
                                ? cbo.SelectedItem.ToString() 
                                : ctrl.Text;
                            val = ExtractManeuverName(text);
                        } else if (state.AutoResolvedChoices.TryGetValue(desc.Id, out var autoVal)) {
                            val = autoVal;
                        }
                        if (!string.IsNullOrWhiteSpace(val)) {
                            result[$"{state.FeatSlot}:{desc.Id}"] = val;
                            if (state.FeatDef.Choices.Count == 1) {
                                result[state.FeatSlot] = val;
                            }
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
