#nullable enable
using PlayMore35.Global;
using System;
using System.Windows.Forms;

namespace PlayMore35 {
    public class TimelinePanel : FlowLayoutPanel {
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
            var session = mainForm.Session;
            var result = session.Timeline.Combat ? session.EndCombat() : session.StartCombat();
            if (result.RefreshRequirement != TimelineRefreshRequirement.None) {
                mainForm.RefreshEffects();
            }
            RefreshTimeline();
        }

        private void NextRound_Click(object sender, EventArgs e) {
            var result = mainForm.Session.AdvanceTimeline(1);
            if (result.RefreshRequirement != TimelineRefreshRequirement.None) {
                mainForm.RefreshEffects();
            }
            RefreshTimeline();
        }

        private void SetTime_Click(object sender, EventArgs e) {
            DateTime cur = mainForm.Session.Timeline.CurrentTime;
            pickTimeDialog.dtpPicker.MinDate = cur;
            pickTimeDialog.dtpPicker.Value = cur;
            if (pickTimeDialog.ShowDialog() != DialogResult.OK) {
                return;
            }
            var result = mainForm.Session.SetTime(pickTimeDialog.dtpPicker.Value);
            if (result.RefreshRequirement != TimelineRefreshRequirement.None) {
                mainForm.RefreshEffects();
            }
            RefreshTimeline();
        }

        public void RefreshTimeline() {
            Timeline rounds = mainForm.Session.Timeline;

            btnNextRound.Visible = rounds.Combat;
            btnSetTime.Visible = !rounds.Combat;
            btnCombat.Text = rounds.Combat ? "Leave combat" : "Enter combat";
            lblCurrentRound.Text = rounds.Combat
                ? $"Round {rounds.CurrentRound - rounds.EncounterStartRound} of combat"
                : rounds.CurrentTime.ToString(pickTimeDialog.dtpPicker.CustomFormat);
        }
    }
}
