using PlayMore35.Global;
using static PlayMore35.Effects.Constants;

using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;

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

        public override void Update(Dictionary<string, List<(string name, string desc)>> textEffects) {
            Controller controller = mainForm.controller;
            name.Text = controller.Character.name.ToUpper();

            Dictionary<string, int> classLevels = controller.Character.SummarizeClassLevels();
            desc1.Text =
                controller.Character.gender + " " +
                controller.Character.race + " " +
                string.Join(
                    ", ",
                    controller.Character.classLevels.Distinct().Select(x => x.Replace(" ", "\u00a0").ToLower() + "\u00a0" + classLevels[x])
                );

            string sizeCategory = SIZE_CATEGORIES[controller.GetStatistic(SIZE_CATEGORY).value + 4];
            desc2.Text = controller.Character.alignment + " " + sizeCategory + " " +
                controller.Effects.CreatureType +
                (controller.Effects.SubTypes.Any() ? " (" + string.Join(", ", controller.Effects.SubTypes) + ")" : "");

            string languages = string.Join(", ", textEffects[LANGUAGES].Select(x => x.desc));
            langs.Text = $"Languages {languages}";
        }
    }
}
