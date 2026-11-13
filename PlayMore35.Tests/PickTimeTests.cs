using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PlayMore35.Tests {
    [TestClass]
    public class PickTimeTests {
        private static T FindControl<T>(Control root, string name) where T : Control {
            var matches = root.Controls.Find(name, searchAllChildren: true);
            Assert.AreEqual(1, matches.Length, $"Expected control '{name}' in PickTime.");
            Assert.IsInstanceOfType(matches[0], typeof(T));
            return (T)matches[0];
        }

        private static IEnumerable<Control> EnumerateAllControls(Control root) {
            foreach (Control child in root.Controls) {
                yield return child;
                foreach (Control descendant in EnumerateAllControls(child)) {
                    yield return descendant;
                }
            }
        }

        [TestMethod]
        public void PickTime_InitializesPre1753AndYear1Dates_WithoutDateTimePickerOrWeekdayReadout() {
            var pre1000 = new DateTime(612, 5, 14, 20, 15, 6); // Thursday, May 14, 612, 8:15:06 PM
            using (var dialog = new PickTime(PickTimeMode.AdjustCampaignSetting, pre1000, DateTime.MinValue)) {
                Assert.IsFalse(
                    EnumerateAllControls(dialog).Any(c => c is DateTimePicker),
                    "PickTime must not use WinForms DateTimePicker (1753 floor)."
                );

                Assert.AreEqual(pre1000, dialog.SelectedTime);
                Assert.IsTrue(dialog.IsSelectionValid);

                var cmbMonth = FindControl<ComboBox>(dialog, "cmbMonth");
                var nudDay = FindControl<NumericUpDown>(dialog, "nudDay");
                var nudYear = FindControl<NumericUpDown>(dialog, "nudYear");
                var nudHour = FindControl<NumericUpDown>(dialog, "nudHour");
                var nudMinute = FindControl<NumericUpDown>(dialog, "nudMinute");
                var nudSecond = FindControl<NumericUpDown>(dialog, "nudSecond");
                var cmbAmPm = FindControl<ComboBox>(dialog, "cmbAmPm");

                Assert.AreEqual("May", cmbMonth.SelectedItem);
                Assert.AreEqual(14m, nudDay.Value);
                Assert.AreEqual(612m, nudYear.Value);
                Assert.AreEqual(8m, nudHour.Value);
                Assert.AreEqual(15m, nudMinute.Value);
                Assert.AreEqual(6m, nudSecond.Value);
                Assert.AreEqual("PM", cmbAmPm.SelectedItem);

                // Verify no weekday readout is displayed anywhere in PickTime labels
                foreach (var label in EnumerateAllControls(dialog).OfType<Label>()) {
                    Assert.IsFalse(label.Text.Contains("Thursday"), "PickTime must not render a weekday readout.");
                }
            }

            // Construct at Year 1 boundary (12:00:00 AM)
            var yearOne = new DateTime(1, 1, 1, 0, 0, 0);
            using (var yearOneDialog = new PickTime(PickTimeMode.AdjustCampaignSetting, yearOne, DateTime.MinValue)) {
                Assert.AreEqual(yearOne, yearOneDialog.SelectedTime);
                Assert.AreEqual("January", FindControl<ComboBox>(yearOneDialog, "cmbMonth").SelectedItem);
                Assert.AreEqual(1m, FindControl<NumericUpDown>(yearOneDialog, "nudDay").Value);
                Assert.AreEqual(1m, FindControl<NumericUpDown>(yearOneDialog, "nudYear").Value);
                Assert.AreEqual(12m, FindControl<NumericUpDown>(yearOneDialog, "nudHour").Value);
                Assert.AreEqual(0m, FindControl<NumericUpDown>(yearOneDialog, "nudMinute").Value);
                Assert.AreEqual(0m, FindControl<NumericUpDown>(yearOneDialog, "nudSecond").Value);
                Assert.AreEqual("AM", FindControl<ComboBox>(yearOneDialog, "cmbAmPm").SelectedItem);
            }
        }

        [TestMethod]
        public void PickTime_ClampsDayDynamically_AcrossMonthsAndPre1900LeapYears() {
            var jan31Leap = new DateTime(612, 1, 31, 12, 0, 0); // 612 is a proleptic Gregorian leap year
            using (var dialog = new PickTime(PickTimeMode.AdjustCampaignSetting, jan31Leap, DateTime.MinValue)) {
                var cmbMonth = FindControl<ComboBox>(dialog, "cmbMonth");
                var nudDay = FindControl<NumericUpDown>(dialog, "nudDay");
                var nudYear = FindControl<NumericUpDown>(dialog, "nudYear");

                Assert.AreEqual(31m, nudDay.Maximum);
                Assert.AreEqual(31m, nudDay.Value);

                // Switch January -> February in leap year 612: clamps to 29
                cmbMonth.SelectedIndex = 1;
                Assert.AreEqual(29m, nudDay.Maximum);
                Assert.AreEqual(29m, nudDay.Value);
                Assert.AreEqual(new DateTime(612, 2, 29, 12, 0, 0), dialog.SelectedTime);

                // Switch year 612 -> common year 613: clamps to 28
                nudYear.Value = 613m;
                Assert.AreEqual(28m, nudDay.Maximum);
                Assert.AreEqual(28m, nudDay.Value);
                Assert.AreEqual(new DateTime(613, 2, 28, 12, 0, 0), dialog.SelectedTime);

                // Switch February -> April: Maximum expands to 30 while preserving Day = 28
                cmbMonth.SelectedIndex = 3;
                Assert.AreEqual(30m, nudDay.Maximum);
                Assert.AreEqual(28m, nudDay.Value);
                Assert.AreEqual(new DateTime(613, 4, 28, 12, 0, 0), dialog.SelectedTime);
            }
        }

        [TestMethod]
        public void PickTime_Handles12HourClockTransitions_AndQuantizesSecondsTo6SecondRounds() {
            var unquantizedNoon = new DateTime(1492, 8, 3, 12, 5, 11); // quantizes to 12:05:06 PM
            using (var dialog = new PickTime(PickTimeMode.AdjustCampaignSetting, unquantizedNoon, DateTime.MinValue)) {
                var nudHour = FindControl<NumericUpDown>(dialog, "nudHour");
                var nudMinute = FindControl<NumericUpDown>(dialog, "nudMinute");
                var nudSecond = FindControl<NumericUpDown>(dialog, "nudSecond");
                var cmbAmPm = FindControl<ComboBox>(dialog, "cmbAmPm");

                Assert.AreEqual(12m, nudHour.Value);
                Assert.AreEqual("PM", cmbAmPm.SelectedItem);
                Assert.AreEqual(6m, nudSecond.Value);
                Assert.AreEqual(6m, nudSecond.Increment);
                Assert.AreEqual(54m, nudSecond.Maximum);
                Assert.AreEqual(new DateTime(1492, 8, 3, 12, 5, 6), dialog.SelectedTime);

                // Manual non-multiple-of-6 second snaps to 6-second round floor
                nudSecond.Value = 17m;
                Assert.AreEqual(12m, nudSecond.Value);
                Assert.AreEqual(new DateTime(1492, 8, 3, 12, 5, 12), dialog.SelectedTime);

                // Toggle 12 PM -> 12 AM (hour 0)
                cmbAmPm.SelectedIndex = 0; // AM
                Assert.AreEqual(new DateTime(1492, 8, 3, 0, 5, 12), dialog.SelectedTime);

                // 1 AM -> 1 PM (hour 13)
                nudHour.Value = 1m;
                Assert.AreEqual(new DateTime(1492, 8, 3, 1, 5, 12), dialog.SelectedTime);
                cmbAmPm.SelectedIndex = 1; // PM
                nudMinute.Value = 0m;
                nudSecond.Value = 0m;
                Assert.AreEqual(new DateTime(1492, 8, 3, 13, 0, 0), dialog.SelectedTime);
            }
        }

        [TestMethod]
        public void PickTime_DistinguishesCampaignSettingAdjustmentFromTimelineAdvance() {
            var now = new DateTime(612, 5, 14, 20, 15, 0);
            string campaignTitle;
            string campaignButton;
            using (var campaignDialog = new PickTime(PickTimeMode.AdjustCampaignSetting, now, DateTime.MinValue)) {
                var lblMode = FindControl<Label>(campaignDialog, "lblModeDescription");
                var btnOk = FindControl<Button>(campaignDialog, "btnOK");

                StringAssert.Contains(lblMode.Text.ToLowerInvariant(), "campaign");
                StringAssert.Contains(lblMode.Text.ToLowerInvariant(), "setting");
                campaignTitle = campaignDialog.Text;
                campaignButton = btnOk.Text;
            }

            using (var advanceDialog = new PickTime(PickTimeMode.AdvanceTimeline, now, now)) {
                var lblMode = FindControl<Label>(advanceDialog, "lblModeDescription");
                var btnOk = FindControl<Button>(advanceDialog, "btnOK");

                Assert.AreNotEqual(campaignTitle, advanceDialog.Text);
                Assert.AreNotEqual(campaignButton, btnOk.Text);
                StringAssert.Contains(lblMode.Text.ToLowerInvariant(), "advance");
            }
        }

        [TestMethod]
        public void PickTime_UsesNonFightingValidation_WithoutSnappingSpinnersMidEdit() {
            var currentTime = new DateTime(612, 5, 14, 20, 15, 0);
            using (var dialog = new PickTime(PickTimeMode.AdvanceTimeline, currentTime, currentTime)) {
                var nudYear = FindControl<NumericUpDown>(dialog, "nudYear");
                var cmbMonth = FindControl<ComboBox>(dialog, "cmbMonth");
                var btnOk = FindControl<Button>(dialog, "btnOK");
                var lblValidation = FindControl<Label>(dialog, "lblValidation");

                Assert.IsTrue(dialog.IsSelectionValid);
                Assert.IsTrue(btnOk.Enabled);
                Assert.AreEqual(string.Empty, lblValidation.Text);

                // User lowers month to April (May 14, 612 -> April 14, 612 < minAllowedTime)
                // Spinner/dropdown must NOT fight the edit; confirmation disables until valid
                cmbMonth.SelectedIndex = 3; // April
                Assert.AreEqual("April", cmbMonth.SelectedItem, "Dropdown must not snap back mid-edit.");
                Assert.AreEqual(new DateTime(612, 4, 14, 20, 15, 0), dialog.SelectedTime);
                Assert.IsFalse(dialog.IsSelectionValid);
                Assert.IsFalse(btnOk.Enabled);
                Assert.IsFalse(string.IsNullOrWhiteSpace(lblValidation.Text));

                // User then advances year to 613 (April 14, 613 >= May 14, 612) -> becomes valid!
                nudYear.Value = 613m;
                Assert.AreEqual(new DateTime(613, 4, 14, 20, 15, 0), dialog.SelectedTime);
                Assert.IsTrue(dialog.IsSelectionValid);
                Assert.IsTrue(btnOk.Enabled);
                Assert.AreEqual(string.Empty, lblValidation.Text);
            }

            // AdjustCampaignSetting mode also blocks RoundZero underflow below Year 1 without snapping spinners
            var minCampaignTime = new DateTime(1, 1, 1, 0, 1, 0); // CurrentRound = 10 rounds (60s)
            using (var campaignDialog = new PickTime(PickTimeMode.AdjustCampaignSetting, minCampaignTime, minCampaignTime)) {
                var btnOk = FindControl<Button>(campaignDialog, "btnOK");
                var lblValidation = FindControl<Label>(campaignDialog, "lblValidation");
                var nudMinute = FindControl<NumericUpDown>(campaignDialog, "nudMinute");

                Assert.IsTrue(campaignDialog.IsSelectionValid);
                Assert.IsTrue(btnOk.Enabled);
                Assert.AreEqual(string.Empty, lblValidation.Text);

                nudMinute.Value = 0m; // 0001-01-01 00:00:00 < 0001-01-01 00:01:00
                Assert.AreEqual(0m, nudMinute.Value, "Minute spinner must not snap back mid-edit.");
                Assert.IsFalse(campaignDialog.IsSelectionValid);
                Assert.IsFalse(btnOk.Enabled);
                Assert.IsFalse(string.IsNullOrWhiteSpace(lblValidation.Text));
            }
        }

        [TestMethod]
        public void PickTime_TypedNumericUpDownEdits_UpdateSelectionValidateLiveAndHonorUnalignedMinimum() {
            // 1. Non-6s-aligned minAllowedTime must not floor-quantize and allow earlier selections
            var unalignedMin = new DateTime(612, 5, 14, 20, 15, 3);
            using (var unalignedDialog = new PickTime(PickTimeMode.AdvanceTimeline, unalignedMin, unalignedMin)) {
                Assert.AreEqual(new DateTime(612, 5, 14, 20, 15, 0), unalignedDialog.SelectedTime);
                Assert.IsFalse(unalignedDialog.IsSelectionValid, "20:15:00 must be rejected when minAllowedTime is 20:15:03.");
            }

            // 2. Typing into NumericUpDown inner edit box without losing focus updates SelectedTime & validation live
            var initial = new DateTime(2026, 10, 6, 20, 15, 0);
            var minCampaign = new DateTime(600, 1, 1, 0, 0, 0);
            using (var dialog = new PickTime(PickTimeMode.AdjustCampaignSetting, initial, minCampaign)) {
                dialog.StartPosition = FormStartPosition.Manual;
                dialog.Location = new System.Drawing.Point(-2000, -2000);
                dialog.ShowInTaskbar = false;
                dialog.Show();
                Application.DoEvents();

                var nudYear = FindControl<NumericUpDown>(dialog, "nudYear");
                var nudMinute = FindControl<NumericUpDown>(dialog, "nudMinute");
                var nudSecond = FindControl<NumericUpDown>(dialog, "nudSecond");
                var btnOk = FindControl<Button>(dialog, "btnOK");
                var yearEditBox = nudYear.Controls[1]; // UpDownEdit inner TextBox
                var minuteEditBox = nudMinute.Controls[1];
                var secondEditBox = nudSecond.Controls[1];

                // Multi-digit typing in nudSecond ("1" -> "12") must not snap "1" back to "00" mid-keystroke
                secondEditBox.Text = "1";
                Assert.AreEqual("1", secondEditBox.Text, "First digit in nudSecond must not snap to '00' mid-typing.");
                secondEditBox.Text = "12";
                Assert.AreEqual("12", secondEditBox.Text);
                Assert.AreEqual(12, dialog.SelectedTime.Second);

                // Multi-digit typing in nudMinute ("1" -> "15") must not zero-pad "1" to "01" mid-keystroke
                minuteEditBox.Text = "1";
                Assert.AreEqual("1", minuteEditBox.Text, "First digit in nudMinute must not reformat to '01' mid-typing.");
                minuteEditBox.Text = "15";
                Assert.AreEqual("15", minuteEditBox.Text);
                Assert.AreEqual(15, dialog.SelectedTime.Minute);

                // Leading '0' in nudYear ("0" -> "06" -> "0612") must not clamp "0" to "1" mid-keystroke
                yearEditBox.Text = "0";
                Assert.AreEqual("0", yearEditBox.Text, "Leading '0' in nudYear must not clamp to '1' mid-typing.");
                Assert.IsFalse(dialog.IsSelectionValid);
                Assert.IsFalse(btnOk.Enabled);

                yearEditBox.Text = "06";
                Assert.AreEqual("06", yearEditBox.Text);

                // Type invalid year "500" (< 600) without moving focus
                yearEditBox.Text = "500";
                Assert.AreEqual("500", yearEditBox.Text, "Must not overwrite typed digits mid-edit.");
                Assert.AreEqual(new DateTime(500, 10, 6, 20, 15, 12), dialog.SelectedTime);
                Assert.IsFalse(dialog.IsSelectionValid);
                Assert.IsFalse(btnOk.Enabled);
                dialog.AcceptButton.PerformClick();
                Assert.AreNotEqual(DialogResult.OK, dialog.DialogResult, "Enter must not confirm an invalid typed year.");

                // Clear year edit box mid-typing ("")
                yearEditBox.Text = string.Empty;
                Assert.AreEqual(string.Empty, yearEditBox.Text, "Clearing edit box mid-typing must not snap back.");
                Assert.IsFalse(dialog.IsSelectionValid);
                Assert.IsFalse(btnOk.Enabled);

                // Type valid year "0612" and confirm via Enter (AcceptButton) without moving focus first
                yearEditBox.Text = "0612";
                Assert.AreEqual("0612", yearEditBox.Text);
                Assert.IsTrue(dialog.IsSelectionValid);
                Assert.IsTrue(btnOk.Enabled);
                Assert.AreEqual(new DateTime(612, 10, 6, 20, 15, 12), dialog.SelectedTime);

                dialog.AcceptButton.PerformClick();
                Assert.AreEqual(DialogResult.OK, dialog.DialogResult);
                Assert.AreEqual(612m, nudYear.Value);
                Assert.AreEqual(12m, nudSecond.Value);
                Assert.AreEqual(new DateTime(612, 10, 6, 20, 15, 12), dialog.SelectedTime);
            }
        }
    }
}
