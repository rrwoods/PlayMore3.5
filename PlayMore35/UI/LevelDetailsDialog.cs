using PlayMore35.CharacterChoices;
using PlayMore35.Global;
using PlayMore35.Subsystems;
using PlayMore35.UI;
using PlayMore35.UI.Subsystems;
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
        private readonly SubsystemUIRegistry uiRegistry;

        private int levelIndex;
        private List<Label> slotNames = new List<Label>();
        private List<ComboBox> feats = new List<ComboBox>();
        private Dictionary<ProgressionSlotKey, Label> choiceLabels = new Dictionary<ProgressionSlotKey, Label>();
        private Dictionary<ProgressionSlotKey, Control> featChoices = new Dictionary<ProgressionSlotKey, Control>();

        private ElementReceptorControl activeReceptor;
        private ISubsystemElementPicker activePicker;

        private class FeatSlotChoiceState {
            public ProgressionSlotKey FeatSlot;
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

        public class ElementReceptorBinding {
            public ProgressionSlotKey FeatSlot { get; }
            public ChoiceDescriptor Descriptor { get; }

            public ElementReceptorBinding(ProgressionSlotKey featSlot, ChoiceDescriptor descriptor) {
                FeatSlot = featSlot;
                Descriptor = descriptor;
            }

            public override string ToString() => $"{FeatSlot}:{Descriptor?.Id}";
            public override bool Equals(object obj) {
                if (obj is ElementReceptorBinding other) {
                    return Equals(FeatSlot, other.FeatSlot) &&
                           string.Equals(Descriptor?.Id, other.Descriptor?.Id, StringComparison.OrdinalIgnoreCase);
                }
                if (obj is string str) {
                    return string.Equals(ToString(), str, StringComparison.OrdinalIgnoreCase);
                }
                return false;
            }

            public override int GetHashCode() => (FeatSlot != null ? FeatSlot.GetHashCode() : 0) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(Descriptor?.Id ?? "");
        }

        private readonly Dictionary<ElementReceptorControl, ElementReceptorBinding> receptorBindings = new Dictionary<ElementReceptorControl, ElementReceptorBinding>();

        private Dictionary<ProgressionSlotKey, FeatSlotChoiceState> slotChoiceStates = new Dictionary<ProgressionSlotKey, FeatSlotChoiceState>();
        private Dictionary<ProgressionSlotKey, List<Control>> dynamicSlotControls = new Dictionary<ProgressionSlotKey, List<Control>>();

        public LevelDetailsDialog(ICharacterSession session, SubsystemUIRegistry? uiRegistry = null) {
            InitializeComponent();
            this.session = session;
            this.uiRegistry = uiRegistry ?? SubsystemUIRegistry.CreateDefault();

            foreach (string className in session.Catalog.Classes.Keys.OrderBy(x => x)) {
                cboClass.Items.Add(className);
            }
        }

        public Panel PickerHost => pnlPickerHost;

        public void PrepareLevel(int levelIndex) {
            // this MUST come first, because the SelectedItem change below triggers the SelectedItemChanged event.
            this.levelIndex = levelIndex;

            Text = $"Level {levelIndex + 1}";
            cboClass.SelectedItem = session.Character.classLevels[levelIndex];
            PrepareFeats();
        }

        private void PrepareFeats() {
            CollapsePickerHost();
            ClearDynamicControls(this, feats);
            ClearDynamicControls(this, slotNames);
            ClearDynamicControls(this, choiceLabels);
            ClearDynamicControls(this, featChoices);

            foreach (var list in dynamicSlotControls.Values) {
                foreach (var c in list) {
                    Controls.Remove(c);
                }
            }
            receptorBindings.Clear();
            dynamicSlotControls.Clear();
            slotChoiceStates.Clear();

            SortedDictionary<int, List<ProgressionSlotKey>> featSlots = session.GetFeatSlots();
            if (!featSlots.ContainsKey(levelIndex + 1)) {
                return;
            }

            int featLeft = cboFeat1.Left;
            foreach (ProgressionSlotKey featSlot in featSlots[levelIndex + 1]) {
                ComboBox featBox = new ComboBox {
                    Top = cboFeat1.Top,
                    Left = featLeft,
                    Size = cboFeat1.Size,
                    Tag = featSlot
                };
                foreach (string availableFeat in availableFeats) {
                    featBox.Items.Add(availableFeat);
                }
                string? featName = session.GetFeat(featSlot);
                if (!string.IsNullOrEmpty(featName)) {
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

        private void PrepareFeatChoice(string featName, ProgressionSlotKey featSlot, int left) {
            CollapsePickerHost();
            if (dynamicSlotControls.TryGetValue(featSlot, out var existingControls)) {
                foreach (var c in existingControls) {
                    if (c is ElementReceptorControl r) {
                        receptorBindings.Remove(r);
                    }
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
            string? savedChoice = session.GetFeatChoice(featSlot, "");
            if (!string.IsNullOrEmpty(savedChoice)) {
                choiceBox.Text = savedChoice;
            }
            slotControls.Add(choiceBox);
            Controls.Add(choiceBox);
            choiceState.SingleChoiceControl = choiceBox;
            featChoices[featSlot] = choiceBox;

            dynamicSlotControls[featSlot] = slotControls;
            slotChoiceStates[featSlot] = choiceState;
            UpdateWarnings();
        }

        private void PrepareDeclarativeChoices(Feat featDef, ProgressionSlotKey featSlot, int left) {
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

                    var receptorBinding = new ElementReceptorBinding(featSlot, desc);
                    var receptor = new ElementReceptorControl {
                        Top = currentTop + (cboFeatChoice.Top - lblFeatChoice.Top),
                        Left = left,
                        Width = cboFeatChoice.Width,
                        Tag = receptorBinding
                    };
                    receptorBindings[receptor] = receptorBinding;

                    string saved = session.GetFeatChoice(featSlot, desc.Id);
                    if (!string.IsNullOrWhiteSpace(saved)) {
                        var existingElem = session.Subsystems.Select(sub => sub.GetElement(saved)).FirstOrDefault(e => e != null);
                        if (existingElem != null) {
                            receptor.SelectedElement = existingElem;
                        } else {
                            receptor.SelectedElement = new SubsystemElement { Name = saved, Type = desc.Filter ?? desc.Id };
                        }
                    }

                    receptor.Activated += (s, e) => {
                        ActivateReceptor(receptor, desc, choiceState);
                    };

                    receptor.ElementCleared += (s, e) => {
                        choiceState.WarningBadge?.ClearWarning();
                        UpdateWarnings();
                    };

                    receptor.ElementDelivered += (elem) => {
                        string deliveredName = elem?.Name;
                        if (!string.IsNullOrWhiteSpace(deliveredName)) {
                            string target = ResolveTargetSource(choiceState, desc);
                            var currentChoices = GetCurrentProgressionChoices();
                            currentChoices[$"{featSlot}:{desc.Id}"] = deliveredName;
                            var progContext = new ProgressionChoiceContext(
                                ProgressionChoiceDomain.Feat,
                                deliveredName,
                                acquisitionLevel: slotLevel,
                                slotKey: featSlot.ToString(),
                                targetSource: target,
                                descriptor: desc,
                                currentChoices: currentChoices,
                                slot: featSlot,
                                overlayAssignments: GetCurrentFeatAssignments()
                            );
                            var warnings = session.ValidateProgressionChoice(progContext);
                            if (warnings != null && warnings.Count > 0) {
                                receptor.SetWarnings(warnings);
                            } else {
                                receptor.ClearWarning();
                            }
                        } else {
                            receptor.ClearWarning();
                        }
                        UpdateWarnings();
                    };

                    slotControls.Add(receptor);
                    Controls.Add(receptor);
                    choiceState.ChoiceControls[desc.Id] = receptor;

                    currentTop = receptor.Bottom + 7;
                }
            }

            dynamicSlotControls[featSlot] = slotControls;
            slotChoiceStates[featSlot] = choiceState;
            UpdateWarnings();
        }

        private void UpdateWarnings() {
            var currentChoices = GetCurrentProgressionChoices();

            foreach (var state in slotChoiceStates.Values) {
                var desc = state.WarningDescriptor;
                if (desc == null && state.FeatDef != null) {
                    desc = state.FeatDef.Choices.FirstOrDefault(c => string.Equals(c.Domain, "SubsystemElements", StringComparison.OrdinalIgnoreCase));
                }
                if (desc == null) {
                    state.WarningBadge?.ClearWarning();
                    continue;
                }

                string selectedValue = "";
                ElementReceptorControl receptor = null;
                if (state.ChoiceControls.TryGetValue(desc.Id, out var ctrl)) {
                    if (ctrl is ElementReceptorControl r) {
                        receptor = r;
                        selectedValue = r.SelectedElement?.Name ?? "";
                    } else if (ctrl is ComboBox cbo) {
                        selectedValue = ExtractBracketedName(cbo.Text);
                    } else {
                        selectedValue = ctrl.Text;
                    }
                }

                if (string.IsNullOrWhiteSpace(selectedValue)) {
                    state.WarningBadge?.ClearWarning();
                    receptor?.ClearWarning();
                    continue;
                }

                string target = ResolveTargetSource(state, desc);
                int slotLevel = session.GetFeatSlotLevel(state.FeatSlot);
                if (slotLevel <= 0) slotLevel = levelIndex + 1;

                var progContext = new ProgressionChoiceContext(
                    ProgressionChoiceDomain.Feat,
                    selectedValue,
                    acquisitionLevel: slotLevel,
                    slotKey: state.FeatSlot.ToString(),
                    targetSource: target,
                    descriptor: desc,
                    currentChoices: currentChoices,
                    slot: state.FeatSlot,
                    overlayAssignments: GetCurrentFeatAssignments()
                );
                var warnings = session.ValidateProgressionChoice(progContext);
                if (warnings.Count > 0) {
                    state.WarningBadge?.SetWarnings(warnings);
                    receptor?.SetWarnings(warnings);
                } else {
                    state.WarningBadge?.ClearWarning();
                    receptor?.ClearWarning();
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

        public EventHandler SelectFeat(ProgressionSlotKey featSlot, int left) {
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

        public Dictionary<ProgressionSlotKey, string> GetFeats() {
            return Enumerable.Range(0, feats.Count)
                .Where(i => feats[i].Text.Any())
                .ToDictionary(i => (ProgressionSlotKey) feats[i].Tag, i => feats[i].Text);
        }

        private List<FeatAssignment> GetCurrentFeatAssignments() {
            var assignments = new List<FeatAssignment>();
            foreach (var state in slotChoiceStates.Values) {
                if (string.IsNullOrWhiteSpace(state.FeatName)) continue;
                var choices = new Dictionary<string, string>();
                if (state.FeatDef != null && state.FeatDef.Choices.Any()) {
                    foreach (var desc in state.FeatDef.Choices) {
                        string val = "";
                        if (state.ChoiceControls.TryGetValue(desc.Id, out var ctrl)) {
                            if (ctrl is ElementReceptorControl r) {
                                val = r.SelectedElement?.Name ?? "";
                            } else if (ctrl is ComboBox cbo) {
                                if (string.Equals(desc.Domain, "SubsystemElements", StringComparison.OrdinalIgnoreCase)) {
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
                            choices[desc.Id] = val;
                        }
                    }
                } else if (!state.IsMultiChoice) {
                    if (state.SingleChoiceControl != null && !string.IsNullOrWhiteSpace(state.SingleChoiceControl.Text)) {
                        choices[""] = state.SingleChoiceControl.Text;
                    }
                }
                assignments.Add(new FeatAssignment(state.FeatSlot, state.FeatName, choices));
            }
            return assignments;
        }

        private Dictionary<string, string> GetCurrentProgressionChoices() {
            var choices = GetFeatChoices();
            foreach (var feat in GetFeats()) {
                choices[feat.Key.ToString()] = feat.Value;
            }
            return choices;
        }

        public Dictionary<string, string> GetFeatChoices() {
            var result = new Dictionary<string, string>();
            foreach (var state in slotChoiceStates.Values) {
                if (state.FeatDef != null && state.FeatDef.Choices.Any()) {
                    if (!string.IsNullOrWhiteSpace(state.FeatName)) {
                        result[state.FeatSlot.ToString()] = state.FeatName;
                    }
                    foreach (var desc in state.FeatDef.Choices) {
                        string val = "";
                        if (state.ChoiceControls.TryGetValue(desc.Id, out var ctrl)) {
                            if (ctrl is ElementReceptorControl r) {
                                val = r.SelectedElement?.Name ?? "";
                            } else if (ctrl is ComboBox cbo) {
                                if (string.Equals(desc.Domain, "SubsystemElements", StringComparison.OrdinalIgnoreCase)) {
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
                        result[state.FeatSlot.ToString()] = state.SingleChoiceControl.Text;
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
                foreach (ComboBox featBox in feats) {
                    var slot = (ProgressionSlotKey)featBox.Tag;
                    if (string.IsNullOrWhiteSpace(featBox.Text)) {
                        c.feats.RemoveAll(fa => fa.Slot.Equals(slot));
                    }
                }
                foreach (var kvp in GetFeats()) {
                    var slot = kvp.Key;
                    var featName = kvp.Value;
                    var existing = c.feats.FirstOrDefault(fa => fa.Slot.Equals(slot));
                    if (existing != null) {
                        if (!string.Equals(existing.Feat, featName, StringComparison.OrdinalIgnoreCase)) {
                            existing.Feat = featName;
                            existing.Choices.Clear();
                        }
                    } else {
                        existing = new FeatAssignment(slot, featName);
                        c.feats.Add(existing);
                    }
                }
                foreach (var state in slotChoiceStates.Values) {
                    var existing = c.feats.FirstOrDefault(fa => fa.Slot.Equals(state.FeatSlot));
                    if (existing == null) continue;
                    if (state.FeatDef != null && state.FeatDef.Choices.Any()) {
                        foreach (var desc in state.FeatDef.Choices) {
                            string val = "";
                            if (state.ChoiceControls.TryGetValue(desc.Id, out var ctrl)) {
                                if (ctrl is ElementReceptorControl r) {
                                    val = r.SelectedElement?.Name ?? "";
                                } else if (ctrl is ComboBox cbo) {
                                    if (string.Equals(desc.Domain, "SubsystemElements", StringComparison.OrdinalIgnoreCase)) {
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
                                existing.Choices[desc.Id] = val;
                            }
                        }
                    } else if (!state.IsMultiChoice) {
                        if (state.SingleChoiceControl != null && !string.IsNullOrWhiteSpace(state.SingleChoiceControl.Text)) {
                            existing.Choices[""] = state.SingleChoiceControl.Text;
                        }
                    }
                }
            });
        }

        private void ActivateReceptor(ElementReceptorControl receptor, ChoiceDescriptor desc, FeatSlotChoiceState state) {
            if (activeReceptor == receptor && activePicker != null) {
                return;
            }

            if (activeReceptor != null) {
                activeReceptor.IsActiveSelecting = false;
            }

            activeReceptor = receptor;
            activeReceptor.IsActiveSelecting = true;

            DisposeActivePicker();

            string elementType = desc.Filter ?? desc.Id;
            var provider = uiRegistry.GetByElementType(elementType);

            string targetSource = ResolveTargetSource(state, desc);
            int slotLevel = session.GetFeatSlotLevel(state.FeatSlot);
            if (slotLevel <= 0) slotLevel = levelIndex + 1;

            var context = new ElementPickerContext {
                ElementType = elementType,
                Filter = desc.Filter,
                AcquisitionLevel = slotLevel,
                WarningProvider = (elem) => {
                    var currentChoices = GetCurrentProgressionChoices();
                    currentChoices[$"{state.FeatSlot}:{desc.Id}"] = elem.Name;
                    var progContext = new ProgressionChoiceContext(
                        ProgressionChoiceDomain.Feat,
                        elem.Name,
                        acquisitionLevel: slotLevel,
                        slotKey: state.FeatSlot.ToString(),
                        targetSource: targetSource,
                        descriptor: desc,
                        currentChoices: currentChoices,
                        slot: state.FeatSlot,
                        overlayAssignments: GetCurrentFeatAssignments()
                    );
                    return session.ValidateProgressionChoice(progContext);
                }
            };

            activePicker = provider.CreateElementPicker(session, context);
            activePicker.ElementDelivered += (elem) => {
                receptor.SelectedElement = elem;
                if (elem != null) {
                    var progContext = new ProgressionChoiceContext(
                        ProgressionChoiceDomain.Feat,
                        elem.Name,
                        acquisitionLevel: slotLevel,
                        slotKey: state.FeatSlot.ToString(),
                        targetSource: targetSource,
                        descriptor: desc,
                        currentChoices: GetCurrentProgressionChoices(),
                        slot: state.FeatSlot,
                        overlayAssignments: GetCurrentFeatAssignments()
                    );
                    var warnings = session.ValidateProgressionChoice(progContext);
                    if (warnings != null && warnings.Count > 0) {
                        receptor.SetWarnings(warnings);
                    } else {
                        receptor.ClearWarning();
                    }
                } else {
                    receptor.ClearWarning();
                }
                UpdateWarnings();
            };

            ExpandPickerHost(activePicker.Control);
        }

        private void ExpandPickerHost(Control pickerControl) {
            pnlPickerHost.SuspendLayout();
            pnlPickerHost.Controls.Clear();
            pickerControl.Dock = DockStyle.Fill;
            pnlPickerHost.Controls.Add(pickerControl);

            int bottomMost = 120;
            foreach (var list in dynamicSlotControls.Values) {
                foreach (var c in list) {
                    if (c.Bottom > bottomMost) {
                        bottomMost = c.Bottom;
                    }
                }
            }

            pnlPickerHost.Top = bottomMost + 10;
            pnlPickerHost.Height = 220;
            pnlPickerHost.Visible = true;
            pnlPickerHost.ResumeLayout(true);
        }

        private void CollapsePickerHost() {
            if (activeReceptor != null) {
                activeReceptor.IsActiveSelecting = false;
                activeReceptor = null;
            }

            DisposeActivePicker();

            if (pnlPickerHost != null) {
                pnlPickerHost.Controls.Clear();
                pnlPickerHost.Visible = false;
                pnlPickerHost.Height = 0;
            }
        }

        private void DisposeActivePicker() {
            if (activePicker != null) {
                try {
                    activePicker.Dispose();
                } catch {
                }
                activePicker = null;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e) {
            CollapsePickerHost();
            base.OnFormClosing(e);
        }
    }
}
