using PlayMore35.Global;
using System;
using System.Windows.Forms;

namespace PlayMore35 {
    class TimelinePanel : FlowLayoutPanel, IRoundObserver {
        private Rounds rounds;
        private readonly Button btnCombat = new Button() {
            Text = "",
            AutoSize = true
        };
        private readonly Button btnNextRound = new Button() {
            Text = "Next Round",
            AutoSize = true,
            Visible = false
        };
        private readonly Button btnSetTime = new Button() {
            Text = "Advance to...",
            AutoSize = true,
            Visible = false
        };
        private Label lblCurrentRound = new Label().Initialize();

        private PickTime pickTimeDialog = null;
        private void InitializePicker() {
            if (pickTimeDialog == null) {
                pickTimeDialog = new PickTime {
                    Font = Font
                };
            }
        }

        public TimelinePanel() : base() {
            FlowDirection = FlowDirection.LeftToRight;
            AutoSize = true;
            Controls.Add(btnCombat);
            Controls.Add(btnNextRound);
            Controls.Add(btnSetTime);
            Controls.Add(lblCurrentRound);
        }

        public void Initialize(MainForm mainForm) {
            rounds = mainForm.controller.rounds;
            rounds.Register(this);
            btnCombat.Click += Combat_Click;
            btnNextRound.Click += NextRound_Click;
            btnSetTime.Click += SetTime_Click;
        }

        private void Combat_Click(object sender, EventArgs e) {
            // if we're not in combat, we want to enter combat (i.e. pass true to this function), and SetTime is visible
            // if we're in combat, we want to leave combat (i.e. pass false to this function), and SetTime is not visible
            rounds.InCombat(btnSetTime.Visible);
        }

        private void NextRound_Click(object sender, EventArgs e) {
            rounds.Advance();
        }

        private void SetTime_Click(object sender, EventArgs e) {
            DateTime cur = rounds.CurrentTime;
            pickTimeDialog.dtpPicker.MinDate = cur;
            pickTimeDialog.dtpPicker.Value = cur;
            if (pickTimeDialog.ShowDialog() != DialogResult.OK) {
                return;
            }
            rounds.SetTime(pickTimeDialog.dtpPicker.Value);
        }

        // ---- Listener methods implemented as part of IRoundObserver contract

        public bool SetCurrentCombatRound(long currentRound, long advanced) {
            lblCurrentRound.Text = $"Round {currentRound} of combat";
            return false;
        }

        public bool SetCurrentTime(DateTime time) {
            InitializePicker();
            lblCurrentRound.Text = time.ToString(pickTimeDialog.dtpPicker.CustomFormat);
            return false;
        }

        public bool InCombat(bool combat) {
            btnNextRound.Visible = combat;
            btnSetTime.Visible = !combat;
            btnCombat.Text = combat ? "Leave combat" : "Enter combat";
            return false;
        }
    }
}
