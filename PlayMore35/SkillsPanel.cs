using static PlayMore35.Effects.Constants;

using System.Collections.Generic;
using PlayMore35.Effects;

namespace PlayMore35 {
    public class SkillsPanel : DynamicPanel {
        private readonly List<DynamicStatPanel> skills = new List<DynamicStatPanel>();

        public override void Initialize(MainForm mainForm) {
            base.Initialize(mainForm);

            foreach (string skill in SKILLS_LIST) {
                DynamicStatPanel skillPanel = new DynamicStatPanel {
                    BaseStatistic = skill,
                };
                skillPanel.Initialize(mainForm);
                skills.Add(skillPanel);
                Controls.Add(skillPanel);
            }
        }

        public override void Update(Dictionary<string, List<Text>> textEffects) {
            foreach (DynamicStatPanel skill in skills) {
                skill.Update(textEffects);
            }
        }
    }
}
