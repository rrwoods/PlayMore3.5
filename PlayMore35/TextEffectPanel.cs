using PlayMore35.Global;
using static PlayMore35.Global.FormUtils;

using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System;
using System.ComponentModel;

namespace PlayMore35 {
    public class TextEffectPanel : DynamicPanel {
        private readonly Dictionary<string, LabeledField> fields = new Dictionary<string, LabeledField>();
        private readonly Dictionary<string, CheckBox> flags = new Dictionary<string, CheckBox>();
        private readonly Dictionary<string, Label> labels = new Dictionary<string, Label>();
        private Label header;

        public enum DisplayMode { SINGLE_LINE, MULTI_LINE_WITH_HEADER, MULTI_LINE_WITHOUT_HEADER }

        [Category("Appearance"), Description("Whether to display this section's header at runtime.")]
        public DisplayMode DisplayStyle { get; set; } = DisplayMode.MULTI_LINE_WITH_HEADER;

        public override void Initialize(MainForm mainForm) {
            base.Initialize(mainForm);
            
            switch (DisplayStyle) {
                case DisplayMode.SINGLE_LINE:
                case DisplayMode.MULTI_LINE_WITH_HEADER:
                    header = new Label {
                        Text = (string)Tag,
                        AutoSize = true,
                    };
                    Controls.Add(header);
                    break;
            }
        }

        private void Toggle_Click(object sender, EventArgs e) {
            CheckBox toggle = (CheckBox)sender;
            string flag = toggle.Text;
            mainForm.controller.Effects.controllableFlags[flag].On = toggle.Checked;
            mainForm.controller.Character.flagValues[flag] = toggle.Checked;
            mainForm.RefreshEffects();
        }

        public override void Update(Dictionary<string, List<(string name, string desc)>> textEffects) {
            string category = (string)Tag;
            var effects = textEffects[category];
            bool any = false;
            Controller controller = mainForm.controller;

            HashSet<string> inactiveFields = new HashSet<string>(fields.Keys);
            foreach (var parameter in controller.Effects.controllableParameters) {
                if (parameter.Value.Category != category) {
                    continue;
                }

                string paramName = parameter.Key;
                LabeledField field;
                if (fields.ContainsKey(paramName)) {
                    inactiveFields.Remove(paramName);
                    field = fields[paramName];
                    field.Visible = true;
                } else {
                    field = new LabeledField(paramName, mainForm);
                    field.lblName.Initialize();
                    fields[paramName] = field;
                    Controls.Add(field);
                }
                if (controller.Character.paramValues.ContainsKey(paramName)) {
                    field.Value = controller.Character.paramValues[paramName];
                }
                ToolTipTextLabel(field.lblName, parameter.Value.Description);
                any = true;
            }
            foreach (string inactive in inactiveFields) {
                fields[inactive].Visible = false;
                fields[inactive].Value = 0;
            }

            HashSet<string> inactiveFlags = new HashSet<string>(flags.Keys);
            foreach (var toggle in controller.Effects.controllableFlags) {
                if (toggle.Value.Category != category || !toggle.Value.Show.On(controller.Effects, controller.EquippedItems)) {
                    continue;
                }

                string toggleName = toggle.Key;
                CheckBox flag;
                if (flags.ContainsKey(toggleName)) {
                    inactiveFlags.Remove(toggleName);
                    flag = flags[toggleName];
                    flag.Visible = true;
                } else {
                    flag = new CheckBox {
                        AutoSize = true,
                        Text = toggleName,
                        Margin = new Padding(3, 0, 0, 0)
                    }.Initialize();
                    
                    if (controller.Character.flagValues.ContainsKey(toggleName)) {
                        bool value = controller.Character.flagValues[toggleName];
                        flag.Checked = value;
                        toggle.Value.On = value;
                    }

                    flag.Click += Toggle_Click;
                    flags[toggleName] = flag;
                    Controls.Add(flag);
                }
                ToolTipTextLabel(flag, toggle.Value.Description);
                any = true;
            }
            foreach (string inactive in inactiveFlags) {
                flags[inactive].Visible = false;
                flags[inactive].Checked = false;
            }

            switch (DisplayStyle) {
                case DisplayMode.SINGLE_LINE:
                    if (effects.Any()) {
                        string effectText = string.Join(", ", effects.Select(x => x.desc));
                        header.Text = $"{category} {effectText}";
                        header.Visible = true;
                        any = true;
                    } else {
                        header.Visible = any;
                    }
                    break;

                case DisplayMode.MULTI_LINE_WITH_HEADER:
                case DisplayMode.MULTI_LINE_WITHOUT_HEADER:
                    HashSet<string> inactiveLabels = new HashSet<string>(labels.Keys);
                    foreach (var (name, desc) in effects) {
                        Label label;
                        if (labels.ContainsKey(name)) {
                            inactiveLabels.Remove(name);
                            label = labels[name];
                            labels[name].Visible = true;
                        } else {
                            label = new Label() {
                                Text = INDENT + name
                            }.Initialize();
                            labels[name] = label;
                            Controls.Add(label);
                        }
                        ToolTipTextLabel(label, desc);
                        any = true;
                    }
                    foreach (string name in inactiveLabels) {
                        labels[name].Visible = false;
                    }
                    break;
            }

            Visible = any;
        }
    }
}
