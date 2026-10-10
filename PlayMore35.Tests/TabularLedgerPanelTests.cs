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
        [TestMethod]
        public void TabularLedgerPanel_SetEntries_CreatesRowsAndHeaders() {
            var panel = new TabularLedgerPanel();
            panel.AmountColumnHeader = "Amount";

            var t1 = new GpTransaction(100m, "starting gold", new DateTime(2026, 10, 6, 20, 11, 0), new DateTime(2026, 10, 9));
            var t2 = new GpTransaction(-25m, "trail rations", new DateTime(2026, 10, 6, 20, 30, 0), new DateTime(2026, 10, 9));

            GpTransaction deletedItem = null;
            panel.SetEntries(
                new[] { t1, t2 },
                t => t.GameTime,
                t => t.RealDate,
                t => t.Description,
                t => LedgerFormatters.FormatGoldAmount(t.Amount),
                t => deletedItem = t
            );

            Assert.AreEqual(2, panel.RowCount);

            var row0 = panel.GetRow(0);
            Assert.AreEqual("2026-10-06 20:11", row0.GameTimeText);
            Assert.AreEqual("2026-10-09", row0.RealDateText);
            Assert.AreEqual("starting gold", row0.DescriptionText);
            Assert.AreEqual("+100", row0.AmountText);

            Assert.IsNotNull(row0.ContextMenuStrip);
            Assert.AreEqual(1, row0.ContextMenuStrip.Items.Count);
            Assert.AreEqual("Delete starting gold", row0.ContextMenuStrip.Items[0].Text);

            row0.ContextMenuStrip.Items[0].PerformClick();
            Assert.AreSame(t1, deletedItem);
        }

        [TestMethod]
        public void TabularLedgerPanel_Selection_HighlightsRow() {
            var panel = new TabularLedgerPanel();
            var t1 = new GpTransaction(10m, "item 1", new DateTime(2026, 10, 6), new DateTime(2026, 10, 9));
            panel.SetEntries(
                new[] { t1 },
                t => t.GameTime,
                t => t.RealDate,
                t => t.Description,
                t => LedgerFormatters.FormatGoldAmount(t.Amount),
                t => { }
            );

            Assert.AreEqual(1, panel.RowCount);
            var row = panel.GetRow(0);
            Assert.IsFalse(row.IsSelected);

            panel.SelectRow(0);
            Assert.IsTrue(row.IsSelected);
        }

        [TestMethod]
        public void TabularLedgerPanel_ScrollToBottom_DoesNotThrowOnEmptyOrPopulated() {
            var panel = new TabularLedgerPanel();
            panel.ScrollToBottom(); // Empty should not throw

            var t1 = new GpTransaction(10m, "item 1", new DateTime(2026, 10, 6), new DateTime(2026, 10, 9));
            panel.SetEntries(
                new[] { t1 },
                t => t.GameTime,
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

            var sessionEntry = new XpSessionEntry(1000, "Session 1 - Dungeon Crawl", new DateTime(2026, 10, 6, 20, 0, 0), new DateTime(2026, 10, 9));

            XpSessionEntry deletedEntry = null;
            panel.SetEntries(
                new[] { sessionEntry },
                s => s.GameTime,
                s => s.RealDate,
                s => s.Description,
                s => LedgerFormatters.FormatXpAmount(s.Xp),
                s => deletedEntry = s
            );

            Assert.AreEqual("XP", panel.AmountColumnHeader);
            Assert.AreEqual(1, panel.RowCount);

            var row = panel.GetRow(0);
            Assert.AreEqual("+1,000", row.AmountText);
            Assert.AreEqual("Session 1 - Dungeon Crawl", row.DescriptionText);
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
                t => t.GameTime,
                t => t.RealDate,
                t => t.Description,
                t => LedgerFormatters.FormatGoldAmount(t.Amount),
                t => { }
            );

            Assert.AreEqual(0, panel.RowCount);
            Assert.IsFalse(panel.IsOverflow);
            Assert.AreEqual(500 - TabularLedgerPanel.HEADER_HEIGHT, panel.HeaderTop);
            Assert.AreEqual(500, panel.BodyTop);
        }

        [TestMethod]
        public void TabularLedgerPanel_FewEntries_AnchorsHeaderImmediatelyAboveFirstLineAtBottom() {
            var panel = new TabularLedgerPanel {
                Size = new System.Drawing.Size(500, 500)
            };
            var t1 = new GpTransaction(100m, "item 1", new DateTime(2026, 10, 6), new DateTime(2026, 10, 9));
            var t2 = new GpTransaction(50m, "item 2", new DateTime(2026, 10, 7), new DateTime(2026, 10, 9));

            panel.SetEntries(
                new[] { t1, t2 },
                t => t.GameTime,
                t => t.RealDate,
                t => t.Description,
                t => LedgerFormatters.FormatGoldAmount(t.Amount),
                t => { }
            );

            Assert.AreEqual(2, panel.RowCount);
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
                new GpTransaction(i * 10m, $"item {i}", new DateTime(2026, 10, 6), new DateTime(2026, 10, 9))
            ).ToList();

            panel.SetEntries(
                items,
                t => t.GameTime,
                t => t.RealDate,
                t => t.Description,
                t => LedgerFormatters.FormatGoldAmount(t.Amount),
                t => { }
            );

            Assert.AreEqual(15, panel.RowCount);
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
                    new GpTransaction(i * 10m, $"item {i}", new DateTime(2026, 10, 6), new DateTime(2026, 10, 9))
                ).ToList();

                panel.SetEntries(
                    items,
                    t => t.GameTime,
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
                    new GpTransaction(i * 10m, $"item {i}", new DateTime(2026, 10, 6), new DateTime(2026, 10, 9))
                ).ToList();

                panel.SetEntries(
                    items,
                    t => t.GameTime,
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
                    new GpTransaction(i * 10m, $"item {i}", new DateTime(2026, 10, 6), new DateTime(2026, 10, 9))
                ).ToList();

                panel.SetEntries(
                    items,
                    t => t.GameTime,
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
                    t => t.GameTime,
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
                character.sessions.Add(new XpSessionEntry(i * 100, $"session {i}", new DateTime(2026, 10, i), new DateTime(2026, 10, i)));
                character.transactions.Add(new GpTransaction(i * 10m, $"tx {i}", new DateTime(2026, 10, i), new DateTime(2026, 10, i)));
            }
            var session = new PlayMore35.Global.CharacterSession(character, catalog);

            using (var form = new MainForm(session)) {
                form.Show();
                Application.DoEvents();

                var ledgerXp = (TabularLedgerPanel)form.Controls.Find("ledgerXp", true).First();
                ledgerXp.SetEntries(
                    character.sessions,
                    s => s.GameTime,
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
                Size = new System.Drawing.Size(500, 500)
            };
            var t1 = new GpTransaction(100m, "starting gold", new DateTime(2026, 10, 6, 20, 11, 0), new DateTime(2026, 10, 9));
            panel.SetEntries(
                new[] { t1 },
                t => t.GameTime,
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

            Assert.AreEqual(0, lblGameTime.Left);
            Assert.AreEqual(115, lblGameTime.Width);

            Assert.AreEqual(120, lblRealDate.Left);
            Assert.AreEqual(110, lblRealDate.Width);

            Assert.AreEqual(235, lblDesc.Left);
            Assert.AreEqual(150, lblDesc.Width);

            Assert.AreEqual(391, lblAmount.Left);
            Assert.AreEqual(78, lblAmount.Width);
        }
    }
}
