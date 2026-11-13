using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlayMore35.CharacterChoices;
using PlayMore35.UI;

namespace PlayMore35.Tests {
    [TestClass]
    public class TabularLedgerPanelTests {
        private static readonly DateTime AnchorTime = new DateTime(2026, 10, 6, 20, 11, 0);
        private static DateTime ProjectRound(long round) => AnchorTime.AddTicks(round * PlayMore35.Global.Timeline.TicksPerRound);

        [TestMethod]
        public void TabularLedgerPanel_SetEntries_CreatesRowsAndHeaders() {
            var panel = new TabularLedgerPanel();
            panel.AmountColumnHeader = "Amount";

            var t1 = new GpTransaction(100m, "starting gold", 0L, new DateTime(2026, 10, 9));
            var t2 = new GpTransaction(-25m, "trail rations", 190L, new DateTime(2026, 10, 9));

            GpTransaction deletedItem = null;
            panel.SetEntries(
                new[] { t1, t2 },
                t => ProjectRound(t.GameRound),
                t => t.RealDate,
                t => t.Description,
                t => LedgerFormatters.FormatGoldAmount(t.Amount),
                t => deletedItem = t
            );

            Assert.AreEqual(2, panel.ViewModel.RowCount);
            Assert.AreEqual(75m, panel.ViewModel.TotalAmount);
            Assert.AreEqual(100m, panel.ViewModel.Rows[0].RunningBalance);
            Assert.AreEqual(75m, panel.ViewModel.Rows[1].RunningBalance);

            var row0 = panel.GetRow(0);
            Assert.AreEqual("2026-10-06 20:11", panel.ViewModel.Rows[0].GameTimeText);
            Assert.AreEqual("2026-10-09", panel.ViewModel.Rows[0].RealDateText);
            Assert.AreEqual("starting gold", panel.ViewModel.Rows[0].DescriptionText);
            Assert.AreEqual("+100", panel.ViewModel.Rows[0].AmountText);

            Assert.IsNotNull(row0.ContextMenuStrip);
            Assert.AreEqual(1, row0.ContextMenuStrip.Items.Count);
            Assert.AreEqual("Delete starting gold", row0.ContextMenuStrip.Items[0].Text);

            row0.ContextMenuStrip.Items[0].PerformClick();
            Assert.AreSame(t1, deletedItem);
        }

        [TestMethod]
        public void TabularLedgerPanel_Selection_HighlightsRow() {
            var panel = new TabularLedgerPanel();
            var t1 = new GpTransaction(10m, "item 1", 0L, new DateTime(2026, 10, 9));
            panel.SetEntries(
                new[] { t1 },
                t => ProjectRound(t.GameRound),
                t => t.RealDate,
                t => t.Description,
                t => LedgerFormatters.FormatGoldAmount(t.Amount),
                t => { }
            );

            Assert.AreEqual(1, panel.ViewModel.RowCount);
            var row = panel.GetRow(0);
            Assert.IsFalse(row.IsSelected);
            Assert.IsFalse(panel.ViewModel.Rows[0].IsSelected);

            panel.ViewModel.SelectRow(0);
            Assert.IsTrue(row.IsSelected);
            Assert.IsTrue(panel.ViewModel.Rows[0].IsSelected);
            Assert.AreEqual(0, panel.ViewModel.SelectedIndex);
        }

        [TestMethod]
        public void TabularLedgerPanel_ScrollToBottom_DoesNotThrowOnEmptyOrPopulated() {
            var panel = new TabularLedgerPanel();
            panel.ScrollToBottom(); // Empty should not throw

            var t1 = new GpTransaction(10m, "item 1", 0L, new DateTime(2026, 10, 9));
            panel.SetEntries(
                new[] { t1 },
                t => ProjectRound(t.GameRound),
                t => t.RealDate,
                t => t.Description,
                t => LedgerFormatters.FormatGoldAmount(t.Amount),
                t => { }
            );

            panel.ScrollToBottom(); // Populated should scroll without throwing
        }

        [TestMethod]
        public void TabularLedgerPanel_GenericReuse_CleanlyAdaptsToArbitraryLedgerEntries() {
            // Demonstrates Ticket #111 reuse for XP session entries
            var panel = new TabularLedgerPanel {
                AmountColumnHeader = "XP"
            };

            var sessionEntry = new XpSessionEntry(1000, "Session 1 - Dungeon Crawl", 0L, new DateTime(2026, 10, 9));

            XpSessionEntry deletedEntry = null;
            panel.SetEntries(
                new[] { sessionEntry },
                s => ProjectRound(s.GameRound),
                s => s.RealDate,
                s => s.Description,
                s => LedgerFormatters.FormatXpAmount(s.Xp),
                s => deletedEntry = s
            );

            Assert.AreEqual("XP", panel.AmountColumnHeader);
            Assert.AreEqual("XP", panel.ViewModel.AmountHeader);
            Assert.AreEqual(1, panel.ViewModel.RowCount);
            Assert.AreEqual(1000m, panel.ViewModel.TotalAmount);

            var row = panel.GetRow(0);
            Assert.AreEqual("+1,000", panel.ViewModel.Rows[0].AmountText);
            Assert.AreEqual("Session 1 - Dungeon Crawl", panel.ViewModel.Rows[0].DescriptionText);
            Assert.AreEqual("Delete Session 1 - Dungeon Crawl", row.ContextMenuStrip.Items[0].Text);

            row.ContextMenuStrip.Items[0].PerformClick();
            Assert.AreSame(sessionEntry, deletedEntry);
        }

        [TestMethod]
        public void TabularLedgerPanel_EmptyLedger_AnchorsHeaderToBottom() {
            var panel = new TabularLedgerPanel {
                Size = new System.Drawing.Size(500, 500)
            };
            panel.SetEntries(
                Array.Empty<GpTransaction>(),
                t => ProjectRound(t.GameRound),
                t => t.RealDate,
                t => t.Description,
                t => LedgerFormatters.FormatGoldAmount(t.Amount),
                t => { }
            );

            Assert.AreEqual(0, panel.ViewModel.RowCount);
            Assert.IsFalse(panel.IsOverflow);
            Assert.AreEqual(500 - TabularLedgerPanel.HEADER_HEIGHT, panel.HeaderTop);
            Assert.AreEqual(500, panel.BodyTop);
        }

        [TestMethod]
        public void TabularLedgerPanel_FewEntries_AnchorsHeaderImmediatelyAboveFirstLineAtBottom() {
            var panel = new TabularLedgerPanel {
                Size = new System.Drawing.Size(500, 500)
            };
            var t1 = new GpTransaction(100m, "item 1", 0L, new DateTime(2026, 10, 9));
            var t2 = new GpTransaction(50m, "item 2", 14400L, new DateTime(2026, 10, 9));

            panel.SetEntries(
                new[] { t1, t2 },
                t => ProjectRound(t.GameRound),
                t => t.RealDate,
                t => t.Description,
                t => LedgerFormatters.FormatGoldAmount(t.Amount),
                t => { }
            );

            Assert.AreEqual(2, panel.ViewModel.RowCount);
            Assert.IsFalse(panel.IsOverflow);
            int expectedContentHeight = TabularLedgerPanel.HEADER_HEIGHT + 2 * TabularLedgerPanel.ROW_HEIGHT;
            int expectedHeaderTop = 500 - expectedContentHeight;
            Assert.AreEqual(expectedHeaderTop, panel.HeaderTop);
            Assert.AreEqual(expectedHeaderTop + TabularLedgerPanel.HEADER_HEIGHT, panel.BodyTop);
        }

        [TestMethod]
        public void TabularLedgerPanel_OverflowingEntries_AnchorsHeaderToTop() {
            var panel = new TabularLedgerPanel {
                Size = new System.Drawing.Size(500, 200)
            };
            var items = Enumerable.Range(1, 15).Select(i =>
                new GpTransaction(i * 10m, $"item {i}", 0L, new DateTime(2026, 10, 9))
            ).ToList();

            panel.SetEntries(
                items,
                t => ProjectRound(t.GameRound),
                t => t.RealDate,
                t => t.Description,
                t => LedgerFormatters.FormatGoldAmount(t.Amount),
                t => { }
            );

            Assert.AreEqual(15, panel.ViewModel.RowCount);
            Assert.IsTrue(panel.IsOverflow);
            Assert.AreEqual(0, panel.HeaderTop);
            Assert.AreEqual(TabularLedgerPanel.HEADER_HEIGHT, panel.BodyTop);
        }

        [TestMethod]
        public void TabularLedgerPanel_OverflowingEntries_DefaultScrollAnchoredToBottom() {
            using (var form = new Form()) {
                var panel = new TabularLedgerPanel {
                    Size = new System.Drawing.Size(500, 200)
                };
                form.Controls.Add(panel);
                form.Show();

                var items = Enumerable.Range(1, 25).Select(i =>
                    new GpTransaction(i * 10m, $"item {i}", 0L, new DateTime(2026, 10, 9))
                ).ToList();

                panel.SetEntries(
                    items,
                    t => ProjectRound(t.GameRound),
                    t => t.RealDate,
                    t => t.Description,
                    t => LedgerFormatters.FormatGoldAmount(t.Amount),
                    t => { }
                );
                Application.DoEvents();

                // Default scroll position after SetEntries should be greater than 0 (anchored to bottom)
                Assert.IsTrue(panel.VerticalScrollValue > 0, $"Default scroll position was {panel.VerticalScrollValue}, expected > 0.");
            }
        }

        [TestMethod]
        public void TabularLedgerPanel_ShrinkToOverflowThenExpand_RestoresRowPositions() {
            using (var form = new Form()) {
                var panel = new TabularLedgerPanel {
                    Size = new System.Drawing.Size(500, 400)
                };
                form.Controls.Add(panel);
                form.Show();

                // 10 items => header (22) + 10 * 18 (180) = 202px total content height
                var items = Enumerable.Range(1, 10).Select(i =>
                    new GpTransaction(i * 10m, $"item {i}", 0L, new DateTime(2026, 10, 9))
                ).ToList();

                panel.SetEntries(
                    items,
                    t => ProjectRound(t.GameRound),
                    t => t.RealDate,
                    t => t.Description,
                    t => LedgerFormatters.FormatGoldAmount(t.Amount),
                    t => { }
                );
                Application.DoEvents();

                Assert.IsFalse(panel.IsOverflow);
                Assert.AreEqual(0, panel.GetRow(0).Top, "Row 0 should start at Top=0 before shrink");

                // Shrink in one jump so it needs to scroll
                panel.Size = new System.Drawing.Size(500, 100);
                Application.DoEvents();
                Assert.IsTrue(panel.IsOverflow);
                Assert.IsTrue(panel.VerticalScrollValue > 0, $"Expected scroll > 0 when shrunk, but got {panel.VerticalScrollValue}, row[0].Top={panel.GetRow(0).Top}");
                Assert.IsTrue(panel.GetRow(0).Top < 0, $"Expected row[0].Top < 0 when scrolled, but got {panel.GetRow(0).Top}");

                // Expand in one jump back to 400 (no longer overflowing)
                panel.Size = new System.Drawing.Size(500, 400);
                Application.DoEvents();
                Assert.IsFalse(panel.IsOverflow);

                // Verify header and body positions, and that all rows are back at their expected positions
                int expectedContentHeight = TabularLedgerPanel.HEADER_HEIGHT + 10 * TabularLedgerPanel.ROW_HEIGHT;
                Assert.AreEqual(400 - expectedContentHeight, panel.HeaderTop);
                Assert.AreEqual(400 - 10 * TabularLedgerPanel.ROW_HEIGHT, panel.BodyTop);
                for (int i = 0; i < 10; i++) {
                    Assert.AreEqual(i * TabularLedgerPanel.ROW_HEIGHT, panel.GetRow(i).Top, $"Row {i} Top was {panel.GetRow(i).Top} after expanding");
                }
            }
        }

        [TestMethod]
        public void TabularLedgerPanel_ShrinkToOverflow_RefreshEntriesWhileScrolled_ThenExpand_RestoresRowPositions() {
            using (var form = new Form { ClientSize = new System.Drawing.Size(1289, 653) }) {
                var panel = new TabularLedgerPanel {
                    Location = new System.Drawing.Point(775, 10),
                    Size = new System.Drawing.Size(500, 545),
                    Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right
                };
                form.Controls.Add(panel);
                form.Show();

                var items = Enumerable.Range(1, 5).Select(i =>
                    new GpTransaction(i * 10m, $"item {i}", 0L, new DateTime(2026, 10, 9))
                ).ToList();

                panel.SetEntries(
                    items,
                    t => ProjectRound(t.GameRound),
                    t => t.RealDate,
                    t => t.Description,
                    t => LedgerFormatters.FormatGoldAmount(t.Amount),
                    t => { }
                );
                Application.DoEvents();

                // Shrink window so the panel overflows and scrolls to bottom
                form.ClientSize = new System.Drawing.Size(1289, 180);
                Application.DoEvents();
                Assert.IsTrue(panel.IsOverflow);
                Assert.IsTrue(panel.VerticalScrollValue > 0);

                // Refresh entries while scrolled (simulating tab enter or adding an entry while shrunk)
                panel.SetEntries(
                    items,
                    t => ProjectRound(t.GameRound),
                    t => t.RealDate,
                    t => t.Description,
                    t => LedgerFormatters.FormatGoldAmount(t.Amount),
                    t => { }
                );
                Application.DoEvents();

                // Expand window back to original size
                form.ClientSize = new System.Drawing.Size(1289, 653);
                Application.DoEvents();

                int expectedContentHeight = TabularLedgerPanel.HEADER_HEIGHT + 5 * TabularLedgerPanel.ROW_HEIGHT;
                Assert.AreEqual(545, panel.Height);
                Assert.IsFalse(panel.IsOverflow);
                Assert.AreEqual(545 - expectedContentHeight, panel.HeaderTop);
                Assert.AreEqual(545 - 5 * TabularLedgerPanel.ROW_HEIGHT, panel.BodyTop);
                for (int i = 0; i < 5; i++) {
                    var row = panel.GetRow(i);
                    Assert.AreEqual(i * TabularLedgerPanel.ROW_HEIGHT, row.Top, $"Row {i} Top was {row.Top} after expanding");
                    Assert.AreEqual(0, row.Left, $"Row {i} Left was {row.Left} after expanding");
                }
            }
        }

        [TestMethod]
        public void MainForm_ShrinkWindowToScrollThenExpand_LedgerRemainsVisible() {
            var catalog = new PlayMore35.Rules.GameRulesCatalog();
            var character = new Character();
            character.race = "dream dwarf";
            character.classLevels.Add("Fighter");
            character.rolledHitDice.Add(10);
            character.assignedAbilityScores["STR"] = 14;
            character.assignedAbilityScores["DEX"] = 10;
            character.assignedAbilityScores["CON"] = 14;
            character.assignedAbilityScores["INT"] = 10;
            character.assignedAbilityScores["WIS"] = 10;
            character.assignedAbilityScores["CHA"] = 10;
            for (int i = 1; i <= 5; i++) {
                character.sessions.Add(new XpSessionEntry(i * 100, $"session {i}", i * 100L, new DateTime(2026, 10, i)));
                character.transactions.Add(new GpTransaction(i * 10m, $"tx {i}", i * 100L, new DateTime(2026, 10, i)));
            }
            var session = new PlayMore35.Global.CharacterSession(character, catalog);

            using (var form = new MainForm(session)) {
                form.Show();
                Application.DoEvents();

                var ledgerXp = (TabularLedgerPanel)form.Controls.Find("ledgerXp", true).First();
                ledgerXp.SetEntries(
                    character.sessions,
                    s => ProjectRound(s.GameRound),
                    s => s.RealDate,
                    s => s.Description,
                    s => LedgerFormatters.FormatXpAmount(s.Xp),
                    s => { }
                );
                Application.DoEvents();

                int initialHeight = form.Height;
                int initialWidth = form.Width;
                int initialLedgerHeight = ledgerXp.Height;
                int initialLedgerTop = ledgerXp.Top;
                int initialLedgerLeft = ledgerXp.Left;

                // Shrink in steps into scrolling region and down to minimum window height
                for (int h = initialHeight; h >= 150; h -= 40) {
                    form.Size = new System.Drawing.Size(initialWidth, h);
                    Application.DoEvents();
                }

                // Expand in steps back to initial height
                for (int h = 150; h <= initialHeight; h += 40) {
                    form.Size = new System.Drawing.Size(initialWidth, h);
                    Application.DoEvents();
                }
                form.Size = new System.Drawing.Size(initialWidth, initialHeight);
                Application.DoEvents();

                Assert.AreEqual(initialLedgerTop, ledgerXp.Top, $"ledgerXp.Top changed from {initialLedgerTop} to {ledgerXp.Top}");
                Assert.AreEqual(initialLedgerLeft, ledgerXp.Left, $"ledgerXp.Left changed from {initialLedgerLeft} to {ledgerXp.Left}");
                Assert.AreEqual(initialLedgerHeight, ledgerXp.Height, $"ledgerXp.Height changed from {initialLedgerHeight} to {ledgerXp.Height}");
                Assert.IsFalse(ledgerXp.IsOverflow);

                int expectedContentHeight = TabularLedgerPanel.HEADER_HEIGHT + 5 * TabularLedgerPanel.ROW_HEIGHT;
                Assert.AreEqual(initialLedgerHeight - expectedContentHeight, ledgerXp.HeaderTop);
                Assert.AreEqual(initialLedgerHeight - 5 * TabularLedgerPanel.ROW_HEIGHT, ledgerXp.BodyTop);

                for (int i = 0; i < 5; i++) {
                    var row = ledgerXp.GetRow(i);
                    Assert.AreEqual(i * TabularLedgerPanel.ROW_HEIGHT, row.Top, $"Row {i} Top={row.Top}");
                    Assert.AreEqual(0, row.Left, $"Row {i} Left={row.Left}");
                }
            }
        }

        [TestMethod]
        public void TabularLedgerPanel_Columns_AlignWithBaselineCoordinates() {
            var panel = new TabularLedgerPanel {
                Size = new System.Drawing.Size(TabularLedgerPanel.BASELINE_WIDTH, 500)
            };
            var t1 = new GpTransaction(100m, "starting gold", 0L, new DateTime(2026, 10, 9));
            panel.SetEntries(
                new[] { t1 },
                t => ProjectRound(t.GameRound),
                t => t.RealDate,
                t => t.Description,
                t => LedgerFormatters.FormatGoldAmount(t.Amount),
                t => { }
            );

            var row = panel.GetRow(0);
            var lblGameTime = row.Controls.Cast<Control>().First(c => c.Text == "2026-10-06 20:11");
            var lblRealDate = row.Controls.Cast<Control>().First(c => c.Text == "2026-10-09");
            var lblDesc = row.Controls.Cast<Control>().First(c => c.Text == "starting gold");
            var lblAmount = row.Controls.Cast<Control>().First(c => c.Text == "+100");

            Assert.AreEqual(TabularLedgerPanel.COL_GAMETIME_LEFT, lblGameTime.Left);
            Assert.AreEqual(TabularLedgerPanel.COL_GAMETIME_WIDTH, lblGameTime.Width);

            Assert.AreEqual(TabularLedgerPanel.COL_REALDATE_LEFT, lblRealDate.Left);
            Assert.AreEqual(TabularLedgerPanel.COL_REALDATE_WIDTH, lblRealDate.Width);

            Assert.AreEqual(TabularLedgerPanel.COL_DESC_LEFT, lblDesc.Left);
            Assert.AreEqual(TabularLedgerPanel.COL_DESC_WIDTH, lblDesc.Width);

            Assert.AreEqual(TabularLedgerPanel.BASELINE_AMOUNT_LEFT, lblAmount.Left);
            Assert.AreEqual(TabularLedgerPanel.COL_AMOUNT_WIDTH, lblAmount.Width);
        }

        [TestMethod]
        public void MainForm_LedgerColumns_AlignWithInputControlsAndDoNotOverlapSiblings() {
            using (var form = new MainForm()) {
                var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                Control GetField(string name) => (Control)typeof(MainForm).GetField(name, flags).GetValue(form);

                var ledgerXp = (TabularLedgerPanel)GetField("ledgerXp");
                var dtpSessionDate = GetField("dtpSessionDate");
                var txtSessionDesc = GetField("txtSessionDesc");
                var txtSessionXp = GetField("txtSessionXp");

                Assert.AreEqual(TabularLedgerPanel.BASELINE_WIDTH, ledgerXp.Width);
                Assert.AreEqual(ledgerXp.Left + TabularLedgerPanel.COL_REALDATE_LEFT, dtpSessionDate.Left);
                Assert.AreEqual(ledgerXp.Left + TabularLedgerPanel.COL_DESC_LEFT, txtSessionDesc.Left);
                Assert.AreEqual(TabularLedgerPanel.COL_DESC_WIDTH, txtSessionDesc.Width);
                Assert.AreEqual(ledgerXp.Left + TabularLedgerPanel.BASELINE_AMOUNT_LEFT, txtSessionXp.Left);
                Assert.AreEqual(TabularLedgerPanel.COL_AMOUNT_WIDTH, txtSessionXp.Width);

                var ledgerGold = (TabularLedgerPanel)GetField("ledgerGold");
                var dtpTransactionDate = GetField("dtpTransactionDate");
                var txtTransactionDesc = GetField("txtTransactionDesc");
                var txtTransactionGp = GetField("txtTransactionGp");
                var txtLocationName = GetField("txtLocationName");
                var btnAddLocation = GetField("btnAddLocation");

                Assert.AreEqual(TabularLedgerPanel.BASELINE_WIDTH, ledgerGold.Width);
                Assert.AreEqual(ledgerGold.Left + TabularLedgerPanel.COL_REALDATE_LEFT, dtpTransactionDate.Left);
                Assert.AreEqual(ledgerGold.Left + TabularLedgerPanel.COL_DESC_LEFT, txtTransactionDesc.Left);
                Assert.AreEqual(TabularLedgerPanel.COL_DESC_WIDTH, txtTransactionDesc.Width);
                Assert.AreEqual(ledgerGold.Left + TabularLedgerPanel.BASELINE_AMOUNT_LEFT, txtTransactionGp.Left);
                Assert.AreEqual(TabularLedgerPanel.COL_AMOUNT_WIDTH, txtTransactionGp.Width);

                Assert.IsTrue(txtLocationName.Right < ledgerGold.Left, "txtLocationName must not overlap ledgerGold.");
                Assert.IsTrue(btnAddLocation.Right < ledgerGold.Left, "btnAddLocation must not overlap ledgerGold.");
            }
        }

        [TestMethod]
        public void TabularLedgerPanel_UpdateVerticalBounds_PositionsBelowMinTopAndClampsToThreeRowMinimum() {
            var panel = new TabularLedgerPanel();

            // Plenty of vertical room (152 to 555 = 403px >= 82px)
            panel.UpdateVerticalBounds(minTop: 152, bottom: 555);
            Assert.AreEqual(152, panel.Top);
            Assert.AreEqual(403, panel.Height);
            Assert.AreEqual(555, panel.Bottom);

            // Constrained vertical room (741 to 555 = -186px < 82px): clamps to 82px (header 22 + 3 * 20 rows)
            panel.UpdateVerticalBounds(minTop: 741, bottom: 555);
            Assert.AreEqual(82, panel.Height);
            Assert.AreEqual(473, panel.Top);
            Assert.AreEqual(555, panel.Bottom);
        }

        [TestMethod]
        public void MainForm_CharacterTab_XpLedgerDoesNotCoverClassLevelRowsAndKeepsThreeRowMinimumHeight() {
            var catalog = new PlayMore35.Rules.GameRulesCatalog();
            var character = new Character { race = "dream dwarf" };
            for (int i = 1; i < 19; i++) {
                character.classLevels.Add("Fighter");
                character.rolledHitDice.Add(10);
            }
            var session = new PlayMore35.Global.CharacterSession(character, catalog);

            using (var form = new MainForm(session)) {
                form.Show();
                Application.DoEvents();

                var ledgerXp = (TabularLedgerPanel)form.Controls.Find("ledgerXp", true).First();
                var btnAddLevel = (Button)form.Controls.Find("btnAddLevel", true).First();
                var txtSessionDesc = (TextBox)form.Controls.Find("txtSessionDesc", true).First();

                // Click Add Level to reach 20 levels and render level rows (btnAddLevel.Top = 694)
                btnAddLevel.PerformClick();
                Application.DoEvents();

                Assert.AreEqual(694, btnAddLevel.Top);

                // At default window height (txtSessionDesc.Top = 561, bottom = 555), 20 levels exceed 555,
                // so ledgerXp clamps to the 3-row minimum height (82px).
                Assert.AreEqual(82, ledgerXp.Height);
                Assert.AreEqual(txtSessionDesc.Top - 6, ledgerXp.Bottom);

                // When the window is tall enough to show all 20 class level rows plus the ledger (e.g. screenshot size),
                // ledgerXp starts at btnAddLevel.Top (741) so no class level row or skill column is covered.
                form.ClientSize = new System.Drawing.Size(1600, 950);
                Application.DoEvents();

                Assert.AreEqual(btnAddLevel.Top, ledgerXp.Top);
                Assert.AreEqual(txtSessionDesc.Top - 6, ledgerXp.Bottom);
                Assert.IsTrue(ledgerXp.Height > 82);
            }
        }

        [TestMethod]
        public void TabularLedgerViewModel_HeadlessRows_ColumnsAndTotals_CalculateCorrectly() {
            var vm = new TabularLedgerViewModel();
            Assert.AreEqual("In-Game Time", vm.GameTimeHeader);
            Assert.AreEqual("Real Date", vm.RealDateHeader);
            Assert.AreEqual("Description", vm.DescriptionHeader);
            Assert.AreEqual("Amount", vm.AmountHeader);
            Assert.AreEqual(0, vm.RowCount);
            Assert.AreEqual(0m, vm.TotalAmount);
            Assert.AreEqual(0m, vm.Total);

            var t1 = new GpTransaction(150.5m, "quest reward", 100L, new DateTime(2026, 10, 5));
            var t2 = new GpTransaction(-50.25m, "supplies", 200L, new DateTime(2026, 10, 5));
            var t3 = new GpTransaction(25m, "found coins", 300L, new DateTime(2026, 10, 6));

            vm.SetEntries(
                new[] { t1, t2, t3 },
                t => ProjectRound(t.GameRound),
                t => t.RealDate,
                t => t.Description,
                t => LedgerFormatters.FormatGoldAmount(t.Amount),
                t => { }
            );

            Assert.AreEqual(3, vm.RowCount);
            Assert.AreEqual(150.5m, vm.Rows[0].NumericAmount);
            Assert.AreEqual(150.5m, vm.Rows[0].RunningBalance);
            Assert.AreEqual(-50.25m, vm.Rows[1].NumericAmount);
            Assert.AreEqual(100.25m, vm.Rows[1].RunningBalance);
            Assert.AreEqual(25m, vm.Rows[2].NumericAmount);
            Assert.AreEqual(125.25m, vm.Rows[2].RunningBalance);
            Assert.AreEqual(125.25m, vm.TotalAmount);
            Assert.AreEqual(125.25m, vm.Total);
            Assert.AreEqual("125.25", vm.FormattedTotal);

            // Switching to XP ledger
            vm.AmountHeader = "XP";
            var s1 = new XpSessionEntry(1200, "session 1", 100L, new DateTime(2026, 10, 5));
            var s2 = new XpSessionEntry(800, "session 2", 200L, new DateTime(2026, 10, 6));
            vm.SetEntries(
                new[] { s1, s2 },
                s => ProjectRound(s.GameRound),
                s => s.RealDate,
                s => s.Description,
                s => LedgerFormatters.FormatXpAmount(s.Xp),
                s => { }
            );

            Assert.AreEqual("XP", vm.AmountHeader);
            Assert.AreEqual(2, vm.RowCount);
            Assert.AreEqual(1200m, vm.Rows[0].RunningBalance);
            Assert.AreEqual(2000m, vm.Rows[1].RunningBalance);
            Assert.AreEqual(2000m, vm.TotalAmount);
            Assert.AreEqual("2,000", vm.FormattedTotal);
        }

        [TestMethod]
        public void TabularLedgerViewModel_SelectionAndDeletion_StateTransitions() {
            var vm = new TabularLedgerViewModel();
            var t1 = new GpTransaction(10m, "item 1", 10L, new DateTime(2026, 10, 2));
            var t2 = new GpTransaction(20m, "item 2", 30L, new DateTime(2026, 10, 4));

            GpTransaction? deleted = null;
            vm.SetEntries(
                new[] { t1, t2 },
                t => ProjectRound(t.GameRound),
                t => t.RealDate,
                t => t.Description,
                t => LedgerFormatters.FormatGoldAmount(t.Amount),
                t => deleted = t
            );

            Assert.AreEqual(-1, vm.SelectedIndex);
            Assert.IsNull(vm.SelectedRow);
            Assert.IsFalse(vm.Rows[0].IsSelected);
            Assert.IsFalse(vm.Rows[1].IsSelected);

            vm.SelectRow(0);
            Assert.AreEqual(0, vm.SelectedIndex);
            Assert.AreSame(vm.Rows[0], vm.SelectedRow);
            Assert.IsTrue(vm.Rows[0].IsSelected);
            Assert.IsFalse(vm.Rows[1].IsSelected);

            vm.SelectRow(1);
            Assert.AreEqual(1, vm.SelectedIndex);
            Assert.AreSame(vm.Rows[1], vm.SelectedRow);
            Assert.IsFalse(vm.Rows[0].IsSelected);
            Assert.IsTrue(vm.Rows[1].IsSelected);

            // Out-of-bounds selection deselects
            vm.SelectRow(-1);
            Assert.AreEqual(-1, vm.SelectedIndex);
            Assert.IsNull(vm.SelectedRow);
            Assert.IsFalse(vm.Rows[1].IsSelected);

            // Deletion
            vm.Rows[0].Delete();
            Assert.AreSame(t1, deleted);
        }

        [TestMethod]
        public void TabularLedgerViewModel_CustomNumericExtractor_AndEvents() {
            var vm = new TabularLedgerViewModel();
            bool entriesFired = false;
            bool selectionFired = false;
            bool headersFired = false;

            vm.EntriesChanged += (s, e) => entriesFired = true;
            vm.SelectionChanged += (s, e) => selectionFired = true;
            vm.ColumnHeadersChanged += (s, e) => headersFired = true;

            vm.AmountHeader = "Credits";
            Assert.IsTrue(headersFired);

            var items = new[] {
                new { Date = DateTime.Today, Desc = "Alpha", AmountStr = "+500" },
                new { Date = DateTime.Today, Desc = "Beta", AmountStr = "-200" }
            };

            vm.SetEntries(
                items,
                x => x.Date,
                x => x.Date,
                x => x.Desc,
                x => x.AmountStr,
                x => { },
                getNumericAmount: x => x.Desc == "Alpha" ? 500m : -200m
            );

            Assert.IsTrue(entriesFired);
            Assert.AreEqual(300m, vm.TotalAmount);
            Assert.AreEqual(500m, vm.Rows[0].RunningBalance);
            Assert.AreEqual(300m, vm.Rows[1].RunningBalance);

            vm.SelectedIndex = 1;
            Assert.IsTrue(selectionFired);

            vm.Clear();
            Assert.AreEqual(0, vm.RowCount);
            Assert.AreEqual(-1, vm.SelectedIndex);
        }
    }
}

