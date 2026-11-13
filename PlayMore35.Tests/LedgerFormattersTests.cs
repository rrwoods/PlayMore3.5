using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlayMore35.UI;

namespace PlayMore35.Tests {
    [TestClass]
    public class LedgerFormattersTests {
        [TestMethod]
        public void FormatGameTime_FormatsAsFourDigitPaddedYearFirstMinuteResolution_IncludingPre1000Years() {
            var dtModern = new DateTime(2026, 10, 6, 20, 11, 45);
            Assert.AreEqual("2026-10-06 20:11", LedgerFormatters.FormatGameTime(dtModern));

            var dtPre1000 = new DateTime(612, 5, 14, 20, 15, 0);
            Assert.AreEqual("0612-05-14 20:15", LedgerFormatters.FormatGameTime(dtPre1000));

            var dtTwoDigitYear = new DateTime(45, 1, 9, 8, 5, 0);
            Assert.AreEqual("0045-01-09 08:05", LedgerFormatters.FormatGameTime(dtTwoDigitYear));
        }

        [TestMethod]
        public void FormatRealDate_FormatsAsYearFirstDayResolution() {
            var dt = new DateTime(2026, 10, 9, 15, 30, 0);
            string formatted = LedgerFormatters.FormatRealDate(dt);
            Assert.AreEqual("2026-10-09", formatted);
        }

        [TestMethod]
        public void FormatGoldAmount_FormatsPositiveNegativeAndZeroAccurately() {
            Assert.AreEqual("+100", LedgerFormatters.FormatGoldAmount(100m));
            Assert.AreEqual("+100.5", LedgerFormatters.FormatGoldAmount(100.5m));
            Assert.AreEqual("-45.25", LedgerFormatters.FormatGoldAmount(-45.25m));
            Assert.AreEqual("0", LedgerFormatters.FormatGoldAmount(0m));
        }

        [TestMethod]
        public void FormatTotalGold_FormatsCommaSeparated() {
            Assert.AreEqual("1,250.5", LedgerFormatters.FormatTotalGold(1250.5m));
            Assert.AreEqual("0", LedgerFormatters.FormatTotalGold(0m));
        }
    }
}
