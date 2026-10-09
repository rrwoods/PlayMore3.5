using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlayMore35.UI;

namespace PlayMore35.Tests {
    [TestClass]
    public class DateOverridePickerTests {
        [TestMethod]
        public void DateOverridePicker_DefaultsToToday() {
            var picker = new DateOverridePicker();
            Assert.AreEqual(DateTime.Today, picker.SelectedDate);
            Assert.AreEqual($"[Date: {DateTime.Today:yyyy-MM-dd}]", picker.DisplayText);
        }

        [TestMethod]
        public void DateOverridePicker_SetSelectedDate_UpdatesDisplayAndNormalizesDate() {
            var picker = new DateOverridePicker();
            var targetDate = new DateTime(2026, 10, 1, 15, 30, 0);

            bool eventFired = false;
            picker.DateChanged += (s, e) => eventFired = true;

            picker.SelectedDate = targetDate;

            Assert.AreEqual(new DateTime(2026, 10, 1), picker.SelectedDate);
            Assert.AreEqual("[Date: 2026-10-01]", picker.DisplayText);
            Assert.IsTrue(eventFired);
        }

        [TestMethod]
        public void DateOverridePicker_Reset_RestoresToday() {
            var picker = new DateOverridePicker();
            picker.SelectedDate = new DateTime(2026, 9, 15);
            Assert.AreEqual(new DateTime(2026, 9, 15), picker.SelectedDate);

            picker.Reset();
            Assert.AreEqual(DateTime.Today, picker.SelectedDate);
            Assert.AreEqual($"[Date: {DateTime.Today:yyyy-MM-dd}]", picker.DisplayText);
        }
    }
}
