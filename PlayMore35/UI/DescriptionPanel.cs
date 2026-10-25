using PlayMore35.Global;
using static PlayMore35.Effects.Constants;

using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using PlayMore35.Effects;

namespace PlayMore35 {
    public class DescriptionPanel : DynamicPanel {
        private readonly Label name = new Label { AutoSize = true };
        private readonly Label desc1 = new Label { AutoSize = true };
        private readonly Label desc2 = new Label { AutoSize = true };
        private readonly Label langs = new Label { AutoSize = true };

        public override void Initialize(MainForm mainForm) {
            base.Initialize(mainForm);
            Controls.Add(name);
            Controls.Add(desc1);
            Controls.Add(desc2);
            Controls.Add(langs);
        }

        public override void Update(Dictionary<string, List<Text>> textEffects) {
            ICharacterSession session = mainForm.Session;
            name.Text = session.Character.name.ToUpper();

            Dictionary<string, int> classLevels = session.Character.SummarizeClassLevels();
            desc1.Text =
                session.Character.gender + " " +
                session.Character.race + " " +
                string.Join(
                    ", ",
                    session.Character.classLevels.Distinct().Select(x => x.Replace(" ", "\u00a0").ToLower() + "\u00a0" + classLevels[x])
                );

            string sizeCategory = SIZE_CATEGORIES[session.GetStatistic(SIZE_CATEGORY).Value + 4];
            desc2.Text = session.Character.alignment + " " + sizeCategory + " " +
                session.CreatureType +
                (session.SubTypes.Any() ? " (" + string.Join(", ", session.SubTypes) + ")" : "");

            string languages = string.Join(", ", textEffects[LANGUAGES].Select(x => x.Description));
            langs.Text = $"Languages {languages}";
        }
    }
}
