using PlayMore35.Global;
using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace PlayMore35 {
    public class TimelinePanel : FlowLayoutPanel {
        private readonly Func<PickTimeMode, DateTime, DateTime, DateTime?> promptTime;
        private ICharacterSession? session;
        private Action? onEffectsChanged;
        private Action? onCampaignTimeChanged;

        private readonly Button btnCombat = new Button {
            Text = "",
            AutoSize = true
        };
        private readonly Button btnNextRound = new Button {
            Text = "Next Round",
            AutoSize = true,
            Visible = false
        };
        private readonly Button btnAdvanceTo = new Button {
            Text = "Advance to...",
            AutoSize = true,
            Visible = false
        };
        private readonly LinkLabel lblTimelineReadout = new LinkLabel {
            Margin = new Padding(3, 5, 3, 0)
        }.Initialize();

        public TimelinePanel() : this(ShowPickTimeDialog) { }

        public TimelinePanel(Func<PickTimeMode, DateTime, DateTime, DateTime?> promptTime) {
            this.promptTime = promptTime ?? throw new ArgumentNullException(nameof(promptTime));
            FlowDirection = FlowDirection.LeftToRight;
            AutoSize = true;

            btnCombat.Click += Combat_Click;
            btnNextRound.Click += NextRound_Click;
            btnAdvanceTo.Click += AdvanceTo_Click;
            lblTimelineReadout.LinkClicked += TimelineReadout_LinkClicked;

            Controls.Add(btnCombat);
            Controls.Add(btnNextRound);
            Controls.Add(btnAdvanceTo);
            Controls.Add(lblTimelineReadout);
        }

        public void Initialize(ICharacterSession session, Action? onEffectsChanged = null, Action? onCampaignTimeChanged = null) {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.onEffectsChanged = onEffectsChanged;
            this.onCampaignTimeChanged = onCampaignTimeChanged;
            RefreshTimeline();
        }

        private static DateTime? ShowPickTimeDialog(PickTimeMode mode, DateTime initialTime, DateTime minAllowedTime) {
            using (var dialog = new PickTime(mode, initialTime, minAllowedTime)) {
                if (dialog.ShowDialog() == DialogResult.OK && dialog.IsSelectionValid) {
                    return dialog.SelectedTime;
                }
                return null;
            }
        }

        private void Combat_Click(object? sender, EventArgs e) {
            if (session == null) {
                return;
            }
            var result = session.Timeline.Combat ? session.EndCombat() : session.StartCombat();
            if (result.RefreshRequirement != TimelineRefreshRequirement.None) {
                onEffectsChanged?.Invoke();
            }
            RefreshTimeline();
        }

        private void NextRound_Click(object? sender, EventArgs e) {
            if (session == null) {
                return;
            }
            var result = session.AdvanceTimeline(1);
            if (result.RefreshRequirement != TimelineRefreshRequirement.None) {
                onEffectsChanged?.Invoke();
            }
            RefreshTimeline();
        }

        private void AdvanceTo_Click(object? sender, EventArgs e) {
            if (session == null) {
                return;
            }
            DateTime cur = session.Timeline.CurrentTime;
            DateTime? chosen = promptTime(PickTimeMode.AdvanceTimeline, cur, cur);
            if (!chosen.HasValue) {
                return;
            }
            var result = session.AdvanceTo(chosen.Value);
            if (result.RefreshRequirement != TimelineRefreshRequirement.None) {
                onEffectsChanged?.Invoke();
            }
            RefreshTimeline();
        }

        private void TimelineReadout_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e) {
            if (session == null || session.Timeline.Combat) {
                return;
            }
            DateTime cur = session.Timeline.CurrentTime;
            DateTime? chosen = promptTime(PickTimeMode.AdjustCampaignSetting, cur, session.Timeline.MinCampaignTime);
            if (!chosen.HasValue) {
                return;
            }
            session.SetCampaignTime(chosen.Value);
            RefreshTimeline();
            onCampaignTimeChanged?.Invoke();
        }

        public void RefreshTimeline() {
            if (session == null) {
                return;
            }
            Timeline rounds = session.Timeline;

            btnNextRound.Visible = rounds.Combat;
            btnAdvanceTo.Visible = !rounds.Combat;
            btnCombat.Text = rounds.Combat ? "Leave combat" : "Enter combat";

            if (rounds.Combat) {
                lblTimelineReadout.Text = $"Round {rounds.CurrentRound - rounds.EncounterStartRound} of combat";
                lblTimelineReadout.LinkBehavior = LinkBehavior.NeverUnderline;
                lblTimelineReadout.LinkArea = new LinkArea(0, 0);
                lblTimelineReadout.TabStop = false;
                lblTimelineReadout.Cursor = Cursors.Default;
                FormUtils.ToolTipTextLabel(lblTimelineReadout, string.Empty);
            } else {
                string text = FormatTimestamp(rounds.CurrentTime);
                lblTimelineReadout.Text = text;
                lblTimelineReadout.LinkBehavior = LinkBehavior.HoverUnderline;
                lblTimelineReadout.LinkArea = new LinkArea(0, text.Length);
                lblTimelineReadout.LinkColor = SystemColors.HotTrack;
                lblTimelineReadout.VisitedLinkColor = SystemColors.HotTrack;
                lblTimelineReadout.ActiveLinkColor = SystemColors.Highlight;
                lblTimelineReadout.TabStop = true;
                lblTimelineReadout.Cursor = Cursors.Hand;
                FormUtils.ToolTipTextLabel(lblTimelineReadout, "Click to adjust campaign setting date and time");
            }
        }

        private static string FormatTimestamp(DateTime cur) {
            return $"{cur.ToString("dddd, MMMM d", CultureInfo.InvariantCulture)}, {cur.Year}, {cur.ToString("h:mm:ss tt", CultureInfo.InvariantCulture)}";
        }
    }
}
