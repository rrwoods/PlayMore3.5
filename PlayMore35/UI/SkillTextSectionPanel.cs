using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using PlayMore35.Effects;
using PlayMore35.Global;
using static PlayMore35.Global.FormUtils;

namespace PlayMore35 {
    public class SkillDeclaration {
        public string Skill { get; }
        public string DisplayName { get; }
        public string DisableCondition { get; }
        public string DisableReason { get; }

        public SkillDeclaration(string skill, string displayName = null, string disableCondition = null, string disableReason = null) {
            Skill = skill ?? throw new ArgumentNullException(nameof(skill));
            DisplayName = displayName ?? skill;
            DisableCondition = disableCondition;
            DisableReason = disableReason ?? (disableCondition != null ? char.ToUpper(disableCondition[0]) + disableCondition.Substring(1) : null);
        }
    }

    public class SkillTextSectionPanel : DynamicPanel {
        public enum HeaderStyle {
            INTEGRATED,         // "Senses: Listen +5, Spot +5" on one row
            DEDICATED_ROW,      // "Senses" on row 1, "   Listen +5, Spot +5" on row 2
            SKILLS_ONLY         // "Listen +5, Spot +5" directly without category title
        }

        public string Category { get; set; }
        public HeaderStyle Style { get; set; } = HeaderStyle.INTEGRATED;
        public List<SkillDeclaration> SkillDeclarations { get; } = new List<SkillDeclaration>();

        private readonly List<Label> skillLabels = new List<Label>();
        private Label headerLabel;
        private readonly TextEffectPanel effectsPanel = new TextEffectPanel();

        public IReadOnlyList<Label> SkillLabels => skillLabels.AsReadOnly();
        public Label HeaderLabel => headerLabel;
        public TextEffectPanel EffectsPanel => effectsPanel;

        public SkillTextSectionPanel() {
        }

        public SkillTextSectionPanel(string category, params SkillDeclaration[] skills) {
            Category = category;
            SkillDeclarations.AddRange(skills);
        }

        public SkillTextSectionPanel(string category, HeaderStyle style, params SkillDeclaration[] skills) {
            Category = category;
            Style = style;
            SkillDeclarations.AddRange(skills);
        }

        public override void Initialize(MainForm mainForm) {
            base.Initialize(mainForm);
            FlowDirection = FlowDirection.LeftToRight;
            WrapContents = true;

            Controls.Clear();
            skillLabels.Clear();

            // 1. Header (if applicable)
            if (Style == HeaderStyle.INTEGRATED) {
                headerLabel = new Label {
                    Text = $"{Category}:",
                    AutoSize = true,
                    Margin = new Padding(3, 3, 0, 3)
                }.Initialize();
                Controls.Add(headerLabel);
            } else if (Style == HeaderStyle.DEDICATED_ROW) {
                headerLabel = new Label {
                    Text = Category,
                    AutoSize = true,
                }.Initialize();
                Controls.Add(headerLabel);
                SetFlowBreak(headerLabel, true);
            }

            // 2. Skill labels on single line
            for (int i = 0; i < SkillDeclarations.Count; i++) {
                var decl = SkillDeclarations[i];
                var label = new Label().Initialize();
                if (Style == HeaderStyle.DEDICATED_ROW && i == 0) {
                    label.Margin = new Padding(12, 3, 3, 3);
                }
                skillLabels.Add(label);
                Controls.Add(label);
            }
            if (skillLabels.Any()) {
                SetFlowBreak(skillLabels.Last(), true);
            }

            // 3. Child TextEffectPanel for category effects, toggles, parameters
            effectsPanel.DisplayStyle = TextEffectPanel.DisplayMode.MULTI_LINE_WITHOUT_HEADER;
            effectsPanel.Tag = Category;
            effectsPanel.Margin = new Padding(0);
            effectsPanel.Initialize(mainForm);
            Controls.Add(effectsPanel);
        }

        public override void Update(Dictionary<string, List<Text>> textEffects) {
            ICharacterSession session = mainForm.Session;

            // Update skills
            for (int i = 0; i < SkillDeclarations.Count; i++) {
                var decl = SkillDeclarations[i];
                var label = skillLabels[i];
                bool isLast = (i == SkillDeclarations.Count - 1);
                string separator = isLast ? "" : ",";

                if (!string.IsNullOrEmpty(decl.DisableCondition) && session.HasFlag(decl.DisableCondition)) {
                    label.Text = $"{decl.DisplayName} \u2014{separator}";
                    ToolTipTextLabel(label, decl.DisableReason ?? "Disabled");
                    label.ContextMenuStrip = null;
                } else {
                    LabelStat(label, session, decl.DisplayName, decl.Skill, separator);
                }
            }

            // Update underlying category text effects if present in dictionary
            if (textEffects != null && Category != null && textEffects.ContainsKey(Category)) {
                effectsPanel.Update(textEffects);
            }

            // Section visibility
            bool hasSkills = SkillDeclarations.Any();
            Visible = hasSkills || effectsPanel.Visible;
        }
    }
}
