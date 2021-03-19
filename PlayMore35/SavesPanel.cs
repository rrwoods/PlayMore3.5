using static PlayMore35.Effects.Constants;

using System.Collections.Generic;

namespace PlayMore35 {
    public class SavesPanel : DynamicPanel {
        private readonly DynamicStatPanel fortSaves = new DynamicStatPanel();
        private readonly DynamicStatPanel refSaves = new DynamicStatPanel();
        private readonly DynamicStatPanel willSaves = new DynamicStatPanel();

        public override void Initialize(MainForm mainForm) {
            base.Initialize(mainForm);

            fortSaves.BaseStatistic = FORT_SAVES;
            fortSaves.Initialize(mainForm);
            Controls.Add(fortSaves);

            refSaves.BaseStatistic = REF_SAVES;
            refSaves.Initialize(mainForm);
            Controls.Add(refSaves);

            willSaves.BaseStatistic = WILL_SAVES;
            willSaves.Initialize(mainForm);
            Controls.Add(willSaves);
        }

        public override void Update(Dictionary<string, List<(string name, string desc)>> textEffects) {
            fortSaves.Update(textEffects);
            refSaves.Update(textEffects);
            willSaves.Update(textEffects);
        }
    }
}
