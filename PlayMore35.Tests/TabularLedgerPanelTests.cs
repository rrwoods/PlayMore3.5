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
    }
}
