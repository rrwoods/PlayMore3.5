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
        public string DisableCondition { get; }
        public string DisableReason { get; }

        public SkillDeclaration(string skill, string disableCondition = null, string disableReason = null) {
            Skill = skill ?? throw new ArgumentNullException(nameof(skill));
            DisableCondition = disableCondition;
            DisableReason = disableReason ?? (disableCondition != null ? char.ToUpper(disableCondition[0]) + disableCondition.Substring(1) : null);
        }
    }

    public class SkillTextSectionPanel : DynamicPanel {
        public string Category { get; set; }
        public List<SkillDeclaration> SkillDeclarations { get; } = new List<SkillDeclaration>();

        private readonly List<Label> skillLabels = new List<Label>();
        private Label headerLabel;
        private readonly TextEffectPanel effectsPanel = new TextEffectPanel();

        public IReadOnlyList<Label> SkillLabels => skillLabels.AsReadOnly();
        public Label HeaderLabel => headerLabel;
        public TextEffectPanel EffectsPanel => effectsPanel;

        public SkillTextSectionPanel() {
        }


        public override void Initialize(MainForm mainForm) {
            base.Initialize(mainForm);
            FlowDirection = FlowDirection.LeftToRight;
            WrapContents = true;

            Controls.Clear();
            skillLabels.Clear();

            // 1. Header (integrated style)
            headerLabel = new Label {
                Text = $"{Category}:",
                AutoSize = true,
                Margin = new Padding(3, 0, 0, 0)
            }.Initialize();
            Controls.Add(headerLabel);

            // 2. Skill labels on single line
            for (int i = 0; i < SkillDeclarations.Count; i++) {
                var decl = SkillDeclarations[i];
                var label = new Label().Initialize();
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
                    label.Text = $"{decl.Skill} \u2014{separator}";
                    ToolTipTextLabel(label, decl.DisableReason ?? "Disabled");
                    label.ContextMenuStrip = null;
                } else {
                    LabelStat(label, session, decl.Skill, decl.Skill, separator);
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
