using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Rules;

namespace PlayMore35.Tests {
    [TestClass]
    public class TimelinePanelTests {
        private static readonly GameRulesCatalog SharedCatalog = new GameRulesCatalog();

        private static CharacterSession CreateSessionAtTime(DateTime roundZero, long currentRound = 0, bool combat = false, long encounterStartRound = 0) {
            var timeline = new Timeline();
            timeline.SetCampaignTime(roundZero);
            if (combat) {
                if (encounterStartRound > 0) {
                    timeline.Advance(encounterStartRound);
                }
                timeline.StartCombat();
                if (currentRound > encounterStartRound) {
                    timeline.Advance(currentRound - encounterStartRound);
                }
            } else if (currentRound > 0) {
                timeline.Advance(currentRound);
            }
            var character = new Character { Timeline = timeline };
            return new CharacterSession(character, SharedCatalog);
        }

        private static Form CreateOffscreenHost(Control child) {
            var host = new Form {
                StartPosition = FormStartPosition.Manual,
                Location = new Point(-2000, -2000),
                ShowInTaskbar = false,
                Size = new Size(600, 120)
            };
            host.Controls.Add(child);
            host.Show();
            Application.DoEvents();
            return host;
        }

        [TestMethod]
        public void TimelinePanel_OutOfCombatInPre1000Year_FormatsUnpaddedYearWithWeekdayAndClickableLink() {
            // 0612-05-14 is Thursday in proleptic Gregorian
            var session = CreateSessionAtTime(new DateTime(612, 5, 14, 20, 15, 0));
            var panel = new TimelinePanel((_, _, _) => null);
            panel.Initialize(session);

            using (var host = CreateOffscreenHost(panel)) {
                var link = panel.Controls.OfType<LinkLabel>().Single();
                var btnAdvance = panel.Controls.OfType<Button>().Single(b => b.Text == "Advance to...");
                var btnNextRound = panel.Controls.OfType<Button>().Single(b => b.Text == "Next Round");
                var btnCombat = panel.Controls.OfType<Button>().Single(b => b.Text == "Enter combat");

                Assert.AreEqual("Thursday, May 14, 612, 8:15:00 PM", link.Text);
                Assert.AreEqual(LinkBehavior.HoverUnderline, link.LinkBehavior);
                Assert.AreEqual(link.Text.Length, link.LinkArea.Length);
                Assert.IsTrue(btnAdvance.Visible);
                Assert.IsFalse(btnNextRound.Visible);
                Assert.IsNotNull(btnCombat);
            }
        }

        [TestMethod]
        public void TimelinePanel_InCombat_DisplaysRoundCountAsPlainUnclickableText() {
            bool promptCalled = false;
            var session = CreateSessionAtTime(new DateTime(612, 5, 14, 20, 15, 0), currentRound: 12, combat: true, encounterStartRound: 5);
            var panel = new TimelinePanel((_, _, _) => {
                promptCalled = true;
                return new DateTime(612, 5, 15, 0, 0, 0);
            });
            panel.Initialize(session);

            using (var host = CreateOffscreenHost(panel)) {
                var link = panel.Controls.OfType<LinkLabel>().Single();
                var btnAdvance = panel.Controls.OfType<Button>().Single(b => b.Text == "Advance to...");
                var btnNextRound = panel.Controls.OfType<Button>().Single(b => b.Text == "Next Round");

                Assert.AreEqual("Round 7 of combat", link.Text);
                Assert.AreEqual(0, link.LinkArea.Length, "Combat round readout must have no clickable link region.");
                Assert.AreEqual(LinkBehavior.NeverUnderline, link.LinkBehavior);
                Assert.IsFalse(btnAdvance.Visible);
                Assert.IsTrue(btnNextRound.Visible);

                ((IButtonControl)link).PerformClick();
                Assert.IsFalse(promptCalled, "Clicking combat readout must not open PickTime.");
            }
        }

        [TestMethod]
        public void TimelinePanel_AdvanceButton_InvokesAdvanceModeAndAdvancesElapsedRounds() {
            var initialTime = new DateTime(612, 5, 14, 20, 15, 0);
            var targetTime = new DateTime(612, 5, 15, 8, 0, 0); // Next morning -> triggers daily reset
            var timeline = new Timeline();
            timeline.SetCampaignTime(initialTime);
            var character = new Character { Timeline = timeline };
            var session = new CharacterSession(character, SharedCatalog);

            // Attach a daily resettable parameter so advancing across midnight requires derived refresh
            var dayParam = new ControllableParameter("daily_test", character) { Value = 1 };
            ((ICharacterContext)session).Effects.resettableParameters["day"].Add(dayParam);

            PickTimeMode? capturedMode = null;
            DateTime capturedInitial = default;
            DateTime capturedMin = default;
            int effectsRefreshCount = 0;
            int campaignChangedCount = 0;

            var panel = new TimelinePanel((mode, cur, min) => {
                capturedMode = mode;
                capturedInitial = cur;
                capturedMin = min;
                return targetTime;
            });
            panel.Initialize(session, () => effectsRefreshCount++, () => campaignChangedCount++);

            using (var host = CreateOffscreenHost(panel)) {
                var btnAdvance = panel.Controls.OfType<Button>().Single(b => b.Text == "Advance to...");
                btnAdvance.PerformClick();

                Assert.AreEqual(PickTimeMode.AdvanceTimeline, capturedMode);
                Assert.AreEqual(initialTime, capturedInitial);
                Assert.AreEqual(initialTime, capturedMin);
                Assert.AreEqual(targetTime, session.Timeline.CurrentTime);
                Assert.IsTrue(session.Timeline.CurrentRound > 0, "AdvanceTo must advance CurrentRound.");
                Assert.AreEqual(1, effectsRefreshCount);
                Assert.AreEqual(0, campaignChangedCount);

                var link = panel.Controls.OfType<LinkLabel>().Single();
                Assert.AreEqual("Friday, May 15, 612, 8:00:00 AM", link.Text);
            }
        }

        [TestMethod]
        public void TimelinePanel_LinkClickOutOfCombat_InvokesCampaignAdjustmentModeAndRebasesRoundZero() {
            // Character is already 10 rounds (60 seconds) into the campaign
            var initialRoundZero = new DateTime(2026, 10, 6, 20, 11, 0);
            var session = CreateSessionAtTime(initialRoundZero, currentRound: 10);
            var chosenCampaignTime = new DateTime(612, 5, 14, 20, 15, 0);

            PickTimeMode? capturedMode = null;
            DateTime capturedInitial = default;
            DateTime capturedMin = default;
            int effectsRefreshCount = 0;
            int campaignChangedCount = 0;

            var panel = new TimelinePanel((mode, cur, min) => {
                capturedMode = mode;
                capturedInitial = cur;
                capturedMin = min;
                return chosenCampaignTime;
            });
            panel.Initialize(session, () => effectsRefreshCount++, () => campaignChangedCount++);

            using (var host = CreateOffscreenHost(panel)) {
                var link = panel.Controls.OfType<LinkLabel>().Single();
                ((IButtonControl)link).PerformClick();

                Assert.AreEqual(PickTimeMode.AdjustCampaignSetting, capturedMode);
                Assert.AreEqual(new DateTime(2026, 10, 6, 20, 12, 0), capturedInitial);
                Assert.AreEqual(new DateTime(10 * Timeline.TicksPerRound), capturedMin);

                Assert.AreEqual(10L, session.Timeline.CurrentRound, "Campaign adjustment must not change CurrentRound.");
                Assert.AreEqual(chosenCampaignTime, session.Timeline.CurrentTime);
                Assert.AreEqual(new DateTime(612, 5, 14, 20, 14, 0), session.Timeline.RoundZero);
                Assert.AreEqual("Thursday, May 14, 612, 8:15:00 PM", link.Text);
                Assert.AreEqual(0, effectsRefreshCount);
                Assert.AreEqual(1, campaignChangedCount);
            }
        }

        [TestMethod]
        public void MainForm_CampaignSettingAdjustmentViaTimelineLink_RefreshesBothXpAndGpLedgerTimestamps() {
            var initialRoundZero = new DateTime(2026, 10, 6, 12, 0, 0);
            var session = CreateSessionAtTime(initialRoundZero, currentRound: 100); // +10 minutes -> 12:10:00
            var targetCampaignTime = new DateTime(612, 5, 14, 20, 15, 0);

            using (var form = new MainForm(session)) {
                form.StartPosition = FormStartPosition.Manual;
                form.Location = new Point(-2000, -2000);
                form.ShowInTaskbar = false;

                var statsParent = form.timelinePanel.Parent;
                var panelLocation = form.timelinePanel.Location;
                var panelAnchor = form.timelinePanel.Anchor;
                var panelSize = form.timelinePanel.Size;
                statsParent.Controls.Remove(form.timelinePanel);
                form.timelinePanel.Dispose();
                form.timelinePanel = new TimelinePanel((mode, cur, min) => targetCampaignTime) {
                    Location = panelLocation,
                    Anchor = panelAnchor,
                    Size = panelSize,
                    Name = "timelinePanel"
                };
                statsParent.Controls.Add(form.timelinePanel);

                form.Show();
                Application.DoEvents();

                var tabMain = (TabControl)form.Controls.Find("tabMain", true).Single();
                var pagCharacter = tabMain.TabPages.Cast<TabPage>().Single(t => t.Name == "pagCharacter");
                var pagInventory = tabMain.TabPages.Cast<TabPage>().Single(t => t.Name == "pagInventory");
                var pagStats = tabMain.TabPages.Cast<TabPage>().Single(t => t.Name == "pagStats");

                var ledgerXp = (PlayMore35.UI.TabularLedgerPanel)form.Controls.Find("ledgerXp", true).Single();
                var ledgerGold = (PlayMore35.UI.TabularLedgerPanel)form.Controls.Find("ledgerGold", true).Single();

                // 1. Log an XP session at CurrentRound = 100 on the Character tab
                tabMain.SelectedTab = pagCharacter;
                Application.DoEvents();
                var txtSessionDesc = (TextBox)form.Controls.Find("txtSessionDesc", true).Single();
                var txtSessionXp = (TextBox)form.Controls.Find("txtSessionXp", true).Single();
                var btnAddSession = (Button)form.Controls.Find("btnAddSession", true).Single();
                txtSessionDesc.Text = "Prologue ambush";
                txtSessionXp.Text = "500";
                btnAddSession.PerformClick();

                // 2. Advance 10 rounds (+1 minute -> 12:11:00) and log a GP transaction on the Equipment tab
                session.AdvanceTimeline(10);
                tabMain.SelectedTab = pagInventory;
                Application.DoEvents();
                var txtTransactionDesc = (TextBox)form.Controls.Find("txtTransactionDesc", true).Single();
                var txtTransactionGp = (TextBox)form.Controls.Find("txtTransactionGp", true).Single();
                var btnAddTransaction = (Button)form.Controls.Find("btnAddTransaction", true).Single();
                txtTransactionDesc.Text = "Starting coin";
                txtTransactionGp.Text = "150";
                btnAddTransaction.PerformClick();

                Assert.AreEqual(100L, session.Character.sessions[0].GameRound);
                Assert.AreEqual(110L, session.Character.transactions[0].GameRound);
                Assert.AreEqual("2026-10-06 12:10", ledgerXp.ViewModel.Rows[0].GameTimeText);
                Assert.AreEqual("2026-10-06 12:11", ledgerGold.ViewModel.Rows[0].GameTimeText);

                // 3. Switch to Stats tab and click the timeline readout link to adjust campaign setting to May 14, 612, 8:15:00 PM
                // CurrentRound = 110 (11 minutes) -> RoundZero becomes 0612-05-14 20:04:00
                // Round 100 (XP) projects to 0612-05-14 20:14, Round 110 (GP) projects to 0612-05-14 20:15
                tabMain.SelectedTab = pagStats;
                Application.DoEvents();

                var link = form.timelinePanel.Controls.OfType<LinkLabel>().Single();
                ((IButtonControl)link).PerformClick();
                Application.DoEvents();

                Assert.AreEqual("Thursday, May 14, 612, 8:15:00 PM", link.Text);
                Assert.AreEqual(100L, session.Character.sessions[0].GameRound, "Ledger GameRound must remain unadjusted.");
                Assert.AreEqual(110L, session.Character.transactions[0].GameRound, "Ledger GameRound must remain unadjusted.");
                Assert.AreEqual("0612-05-14 20:14", ledgerXp.ViewModel.Rows[0].GameTimeText);
                Assert.AreEqual("0612-05-14 20:15", ledgerGold.ViewModel.Rows[0].GameTimeText);
            }
        }
    }
}
