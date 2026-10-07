using PlayMore35.Global;
using static PlayMore35.Global.FormUtils;
using static PlayMore35.Effects.Constants;

using System.Windows.Forms;
using System.Collections.Generic;
using PlayMore35.Effects;

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

        public override void Update(Dictionary<string, List<Text>> textEffects) {
            ICharacterSession session = mainForm.Session;
            if (session.HasFlag("deafened")) {
                listen.Text = "Listen \u2014,";
                ToolTipTextLabel(listen, "Defeaned");
            } else {
                LabelStat(listen, session, LISTEN, LISTEN, ",");
            }
            if (session.HasFlag("blinded")) {
                spot.Text = "Spot \u2014";
                ToolTipTextLabel(spot, "Blinded");
            } else {
                LabelStat(spot, session, SPOT, SPOT);
            }
        }
    }
}
