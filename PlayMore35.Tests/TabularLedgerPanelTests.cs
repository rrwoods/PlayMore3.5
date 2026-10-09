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
