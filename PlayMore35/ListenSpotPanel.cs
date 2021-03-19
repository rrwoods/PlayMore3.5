using PlayMore35.Global;
using static PlayMore35.Global.FormUtils;
using static PlayMore35.Effects.Constants;

using System.Windows.Forms;
using System.Collections.Generic;

namespace PlayMore35 {
    public class ListenSpotPanel : DynamicPanel {
        private readonly Label listen, spot;

        public ListenSpotPanel() {
            listen = new Label().Initialize();
            Controls.Add(listen);

            spot = new Label().Initialize();
            Controls.Add(spot);
        }

        public override void Initialize(MainForm mainForm) {
            base.Initialize(mainForm);
            FlowDirection = FlowDirection.LeftToRight;
        }

        public override void Update(Dictionary<string, List<(string name, string desc)>> textEffects) {
            Controller controller = mainForm.controller;
            if (controller.Effects.HasFlag("deafened", controller.EquippedItems)) {
                listen.Text = "Listen \u2014,";
                ToolTipTextLabel(listen, "Defeaned");
            } else {
                LabelStat(listen, controller, LISTEN, LISTEN, ",");
            }
            if (controller.Effects.HasFlag("blinded", controller.EquippedItems)) {
                spot.Text = "Spot \u2014";
                ToolTipTextLabel(spot, "Blinded");
            } else {
                LabelStat(spot, controller, SPOT, SPOT);
            }
        }
    }
}
