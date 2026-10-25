#nullable enable
using PlayMore35.Global;
using static PlayMore35.Global.FormUtils;
using static PlayMore35.Effects.Constants;

using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System;
using System.ComponentModel;
using PlayMore35.Effects;

namespace PlayMore35 {
    public class TextEffectPanel : DynamicPanel {
        private readonly Dictionary<string, LabeledField> fields = new Dictionary<string, LabeledField>();
        private readonly Dictionary<string, CheckBox> flags = new Dictionary<string, CheckBox>();
        private readonly Dictionary<string, Label> effectLabels = new Dictionary<string, Label>();

        private readonly Dictionary<string, Label> usesLabels = new Dictionary<string, Label>();
        private readonly Dictionary<string, LabeledField> usedFields = new Dictionary<string, LabeledField>();
        private readonly Dictionary<string, LabeledField> paramFields = new Dictionary<string, LabeledField>();

        private Label header;

        public enum DisplayMode { SINGLE_LINE, MULTI_LINE_WITH_HEADER, MULTI_LINE_WITHOUT_HEADER }

        [Category("Appearance"), Description("Whether to display this section's header at runtime.")]
        public DisplayMode DisplayStyle { get; set; } = DisplayMode.MULTI_LINE_WITH_HEADER;

        public override void Initialize(MainForm mainForm) {
            base.Initialize(mainForm);
            FlowDirection = FlowDirection.LeftToRight;
            
            switch (DisplayStyle) {
                case DisplayMode.SINGLE_LINE:
                case DisplayMode.MULTI_LINE_WITH_HEADER:
                    header = new Label {
                        Text = (string)Tag,
                        AutoSize = true,
                    };
                    Controls.Add(header);
                    SetFlowBreak(header, true);
                    break;
            }
        }

        private void Toggle_Click(object sender, EventArgs e) {
            CheckBox toggle = (CheckBox)sender;
            string flag = toggle.Text;
            mainForm.Session.SetFlag(flag, toggle.Checked);
            mainForm.RefreshEffects();
        }

        private void UsedUpColor(Control control, bool anyRemaining) {
            if (anyRemaining) {
                control.ForeColor = SystemColors.ControlText;
            } else {
                control.ForeColor = SystemColors.ControlLight;
            }
        }

        private ToolStripMenuItem CreateActionMenuItem(string text, SheetAction action) {
            ToolStripMenuItem menuItem = new ToolStripMenuItem {
                Text = text
            };
            menuItem.Click += (s, e) => {
                mainForm.Session.PerformAction(action);
                mainForm.RefreshEffects();
            };
            return menuItem;
        }

        private void UpdateEffectContextMenu(Label effectLabel, Text text, string usedName) {
            List<ToolStripMenuItem> items = new List<ToolStripMenuItem>();
            if (text.Resource != null && text.Resource.RechargeTime.unit != AT_WILL) {
                SheetAction use = new SheetAction().AdjustParam(usedName, 1);
                if (!text.Resource.RechargeTime.quantity.IsSingleOne()) {
                    use.AndThen(new SheetAction()
                        .Duration(text.Resource.RechargeTime.ToString())
                        .AndThen(new SheetAction().AdjustParam(usedName, -1))
                    );
                }
                items.Add(CreateActionMenuItem($"Use {text.Name}", use));
            }

            if (text.associatedAction != null && text.associatedAction.Any()) {
                foreach (SheetAction action in text.associatedAction) {
                    string actionText = !string.IsNullOrEmpty(action.Name) ? action.Name : $"Use {text.Name}";
                    items.Add(CreateActionMenuItem(actionText, action));
                }
            }

            if (items.Any()) {
                if (effectLabel.ContextMenuStrip == null) {
                    effectLabel.ContextMenuStrip = new ContextMenuStrip();
                }
                effectLabel.ContextMenuStrip.Items.Clear();
                effectLabel.ContextMenuStrip.Items.AddRange(items.ToArray());
            } else if (effectLabel.ContextMenuStrip != null) {
                effectLabel.ContextMenuStrip.Items.Clear();
            }
        }

        private void UpdateFlagContextMenu(CheckBox flagBox, ToggleView flag, string usedName) {
            if (flag.HasResource) {
                SheetAction useCharge = new SheetAction().AdjustParam(usedName, 1);
                if (!flag.IsAtWill && !string.IsNullOrEmpty(flag.RechargeDuration)) {
                    useCharge.Duration(flag.RechargeDuration);
                }

                SheetAction getEffect = new SheetAction().SetFlag(flag.Name).Duration(flag.EffectDuration);
                if (flag.ActionAfter != null) {
                    getEffect.AndThen(flag.ActionAfter);
                }

                ToolStripMenuItem menuItem = new ToolStripMenuItem {
                    Text = $"Use {flag.Name}"
                };
                menuItem.Click += (s, e) => {
                    mainForm.Session.PerformAction(useCharge);
                    mainForm.Session.PerformAction(getEffect);
                    mainForm.RefreshEffects();
                };

                if (flagBox.ContextMenuStrip == null) {
                    flagBox.ContextMenuStrip = new ContextMenuStrip();
                }
                flagBox.ContextMenuStrip.Items.Clear();
                flagBox.ContextMenuStrip.Items.Add(menuItem);
            } else if (flagBox.ContextMenuStrip != null) {
                flagBox.ContextMenuStrip.Items.Clear();
            }
        }

        public override void Update(Dictionary<string, List<Text>> textEffects) {
            string category = (string)Tag;
            var effects = textEffects[category];
            bool any = false;
            ICharacterSession session = mainForm.Session;

            HashSet<string> inactiveFields = new HashSet<string>(fields.Keys);
            foreach (var parameter in session.GetControllableParameters()) {
                if (parameter.Category != category) {
                    continue;
                }

                string paramName = parameter.Name;
                LabeledField field;
                if (fields.ContainsKey(paramName)) {
                    inactiveFields.Remove(paramName);
                    field = fields[paramName];
                    field.Visible = true;
                } else {
                    field = new LabeledField(paramName, mainForm);
                    fields[paramName] = field;
                    Controls.Add(field);
                    SetFlowBreak(field, true);
                }
                field.Value = parameter.Value;
                ToolTipTextLabel(field.lblName, parameter.Description);
                ToolTipTextLabel(field.txtValue, parameter.Description);
                any = true;
            }
            foreach (string inactive in inactiveFields) {
                fields[inactive].Visible = false;
                fields[inactive].Value = 0;
            }

            HashSet<string> inactiveFlags = new HashSet<string>(flags.Keys);
            HashSet<string> inactiveUses = new HashSet<string>(usesLabels.Keys);
            HashSet<string> inactiveUsed = new HashSet<string>(usedFields.Keys);
            HashSet<string> inactiveParams = new HashSet<string>(paramFields.Keys);
            var parameters = session.GetControllableParameters().GroupBy(p => p.Name).ToDictionary(g => g.Key, g => g.First().Value);
            foreach (var flag in session.GetControllableFlags()) {
                if (flag.Category != category || !flag.IsVisible) {
                    continue;
                }

                string toggleName = flag.Name;
                CheckBox flagBox;
                Label usesLabel = null;
                LabeledField usedField = null;
                string usedName = $"{flag.Name} used";
                if (flags.ContainsKey(toggleName)) {
                    inactiveFlags.Remove(toggleName);
                    flagBox = flags[toggleName];
                    flagBox.Visible = true;
                    flagBox.Checked = flag.IsActive;
                    
                    if (flag.HasResource) {
                        inactiveUses.Remove(flag.Name);
                        usesLabel = usesLabels[flag.Name];
                        usesLabel.Visible = true;
                        if (!flag.IsAtWill) {
                            inactiveUsed.Remove(flag.Name);
                            usedField = usedFields[flag.Name];
                            usedField.Visible = true;
                        }
                    }
                } else {
                    flagBox = new CheckBox {
                        AutoSize = true,
                        Text = toggleName,
                        Margin = new Padding(3, 0, 0, 0)
                    }.Initialize();
                    
                    flagBox.Checked = flag.IsActive;

                    flagBox.Click += Toggle_Click;
                    flags[toggleName] = flagBox;
                    Controls.Add(flagBox);

                    if (flag.HasResource) {
                        usesLabel = new Label {
                            Text = flag.ResourceDescription,
                            Margin = new Padding(3, 1, 3, 0)
                        }.Initialize();
                        usesLabels[flag.Name] = usesLabel;
                        Controls.Add(usesLabel);

                        if (!flag.IsAtWill) {
                            usedField = new LabeledField(usedName, mainForm, "used") {
                                Margin = new Padding(0, 1, 0, 0)
                            };
                            usedFields[flag.Name] = usedField;
                            Controls.Add(usedField);
                            SetFlowBreak(usedField, true);
                        } else {
                            SetFlowBreak(usesLabel, true);
                        }
                    } else {
                        SetFlowBreak(flagBox, true);
                    }
                }
                UpdateFlagContextMenu(flagBox, flag, usedName);
                ToolTipTextLabel(flagBox, flag.Description);
                if (usesLabel != null && !flag.IsAtWill) {
                    ToolTipTextLabel(usesLabel, string.Join("\n", flag.UsesExplanation));
                    if (parameters.TryGetValue(usedName, out int used)) {
                        usedField.Value = used;
                        UsedUpColor(usesLabel, used < flag.MaxUses);
                        UsedUpColor(flagBox, used < flag.MaxUses);
                    }
                }
                any = true;
            }
            foreach (string inactive in inactiveFlags) {
                flags[inactive].Visible = false;
                flags[inactive].Checked = false;
            }

            switch (DisplayStyle) {
                case DisplayMode.SINGLE_LINE:
                    if (effects.Any()) {
                        string effectText = string.Join(", ", effects.Select(x => x.Description));
                        header.Text = $"{category} {effectText}";
                        header.Visible = true;
                        any = true;
                    } else {
                        header.Visible = any;
                    }
                    break;

                case DisplayMode.MULTI_LINE_WITH_HEADER:
                case DisplayMode.MULTI_LINE_WITHOUT_HEADER:
                    HashSet<string> inactiveLabels = new HashSet<string>(effectLabels.Keys);
                    foreach (Text text in effects) {
                        string name = text.Name;
                        string desc = text.Description;

                        Label effectLabel;
                        Label usesLabel = null;
                        LabeledField usedField = null;
                        LabeledField paramField = null;
                        string usedName = $"{name} used";
                        if (effectLabels.ContainsKey(name)) {
                            inactiveLabels.Remove(name);
                            effectLabel = effectLabels[name];
                            effectLabel.Visible = true;

                            if (text.Resource != null) {
                                inactiveUses.Remove(name);
                                usesLabel = usesLabels[name];
                                usesLabel.Visible = true;
                                if (text.Resource.RechargeTime.unit != AT_WILL) {
                                    inactiveUsed.Remove(name);
                                    usedField = usedFields[name];
                                    usedField.Visible = true;
                                }
                            }
                        } else {
                            effectLabel = new Label() {
                                Text = INDENT + name
                            }.Initialize();
                            effectLabels[name] = effectLabel;
                            Controls.Add(effectLabel);

                            if (text.Resource != null) {
                                usesLabel = new Label() {
                                    Text = text.Resource.Description()
                                }.Initialize();
                                usesLabels[name] = usesLabel;
                                Controls.Add(usesLabel);

                                if (text.Resource.RechargeTime.unit != AT_WILL) {
                                    usedField = new LabeledField(usedName, mainForm, "used");
                                    usedFields[name] = usedField;
                                    Controls.Add(usedField);
                                }
                            }
                        }

                        UpdateEffectContextMenu(effectLabel, text, usedName);

                        if (!string.IsNullOrEmpty(text.ParameterName)) {
                            if (paramFields.ContainsKey(name)) {
                                inactiveParams.Remove(name);
                                paramField = paramFields[name];
                                paramField.Visible = true;
                            } else {
                                paramField = new LabeledField(text.ParameterName, mainForm, text.ParameterLabel);
                                paramFields[name] = paramField;
                                Controls.Add(paramField);
                            }
                            paramField.Value = parameters.TryGetValue(text.ParameterName, out int paramVal) ? paramVal : 0;
                        }

                        if (paramField != null) {
                            SetFlowBreak(effectLabel, false);
                            if (usesLabel != null) SetFlowBreak(usesLabel, false);
                            if (usedField != null) SetFlowBreak(usedField, false);
                            SetFlowBreak(paramField, true);
                        } else if (usedField != null) {
                            SetFlowBreak(effectLabel, false);
                            SetFlowBreak(usesLabel, false);
                            SetFlowBreak(usedField, true);
                        } else if (usesLabel != null) {
                            SetFlowBreak(effectLabel, false);
                            SetFlowBreak(usesLabel, true);
                        } else {
                            SetFlowBreak(effectLabel, true);
                        }

                        ToolTipTextLabel(effectLabel, desc);
                        if (usesLabel != null && text.Resource.RechargeTime.unit != AT_WILL) {
                            ToolTipTextLabel(usesLabel, string.Join("\n", text.Resource.DynamicUsesExplanation));
                            if (parameters.TryGetValue(usedName, out int used)) {
                                usedField.Value = used;
                                UsedUpColor(usesLabel, used < text.Resource.DynamicUses);
                                UsedUpColor(effectLabel, used < text.Resource.DynamicUses);
                            }
                        }
                        any = true;
                    }
                    foreach (string name in inactiveLabels) {
                        effectLabels[name].Visible = false;
                    }
                    break;
            }

            foreach (string name in inactiveUses) {
                usesLabels[name].Visible = false;
            }
            foreach (string name in inactiveUsed) {
                usedFields[name].Visible = false;
            }
            foreach (string name in inactiveParams) {
                paramFields[name].Visible = false;
            }

            Visible = any;
        }
    }
}
