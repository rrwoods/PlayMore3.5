using PlayMore35.Global;
using System;
using System.Windows.Forms;

namespace PlayMore35 {
    class TimelinePanel : FlowLayoutPanel {
        private MainForm mainForm;
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

        public TimelinePanel() : base() {
            FlowDirection = FlowDirection.LeftToRight;
            AutoSize = true;
            Controls.Add(btnCombat);
            Controls.Add(btnNextRound);
            Controls.Add(btnSetTime);
            Controls.Add(lblCurrentRound);
        }

        public void Initialize(MainForm mainForm) {
            this.mainForm = mainForm;
            btnCombat.Click += Combat_Click;
            btnNextRound.Click += NextRound_Click;
            btnSetTime.Click += SetTime_Click;
            pickTimeDialog = new PickTime {
                Font = Font
            };
            RefreshTimeline();
        }

        private void Combat_Click(object sender, EventArgs e) {
            // if we're not in combat, we want to enter combat (i.e. pass true to this function), and SetTime is visible
            // if we're in combat, we want to leave combat (i.e. pass false to this function), and SetTime is not visible
            mainForm.controller.Character.rounds.InCombat(btnSetTime.Visible);
            RefreshTimeline();
        }

        private void NextRound_Click(object sender, EventArgs e) {
            mainForm.controller.Character.rounds.Advance();
            RefreshTimeline();
        }

        private void SetTime_Click(object sender, EventArgs e) {
            DateTime cur = mainForm.controller.Character.rounds.CurrentTime;
            pickTimeDialog.dtpPicker.MinDate = cur;
            pickTimeDialog.dtpPicker.Value = cur;
            if (pickTimeDialog.ShowDialog() != DialogResult.OK) {
                return;
            }
            mainForm.controller.Character.rounds.SetTime(pickTimeDialog.dtpPicker.Value);
            RefreshTimeline();
        }

        public void RefreshTimeline() {
            Rounds rounds = mainForm.controller.Character.rounds;

            btnNextRound.Visible = rounds.Combat;
            btnSetTime.Visible = !rounds.Combat;
            btnCombat.Text = rounds.Combat ? "Leave combat" : "Enter combat";
            lblCurrentRound.Text = rounds.Combat
                ? $"Round {rounds.CurrentRound - rounds.EncounterStartRound} of combat"
                : rounds.CurrentTime.ToString(pickTimeDialog.dtpPicker.CustomFormat);
        }
    }
}
