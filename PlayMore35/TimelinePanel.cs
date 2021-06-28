using PlayMore35.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PlayMore35 {
    class TimelinePanel : FlowLayoutPanel, IRoundObserver {
        private Rounds rounds;
        private readonly Button btnNextRound = new Button() {
            Text = "Next Round",
            AutoSize = true
        };
        private Label lblCurrentRound = new Label().Initialize();

        public TimelinePanel() : base() {
            FlowDirection = FlowDirection.LeftToRight;
            AutoSize = true;
            Controls.Add(btnNextRound);
            Controls.Add(lblCurrentRound);
        }

        private void NextRound_Click(object sender, EventArgs e) {
            rounds.Advance();
        }

        public void Initialize(MainForm mainForm) {
            rounds = mainForm.controller.rounds;
            rounds.Register(this);
            btnNextRound.Click += NextRound_Click;
        }

        public bool SetCurrentRound(int currentRound, bool combat) {
            lblCurrentRound.Text = $"Round {currentRound}";
            return false;
        }
    }
}
