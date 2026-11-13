using System;
using System.Globalization;
using System.Windows.Forms;
using PlayMore35.Global;

namespace PlayMore35 {
    public partial class PickTime : Form {
        private static readonly string[] MonthNames = {
            "January", "February", "March", "April", "May", "June",
            "July", "August", "September", "October", "November", "December"
        };

        private PickTimeMode mode = PickTimeMode.AdvanceTimeline;
        private DateTime minAllowedTime = DateTime.MinValue;
        private DateTimeKind selectedKind = DateTimeKind.Unspecified;
        private bool isUpdating;

        public PickTime() {
            InitializeComponent();
            cmbMonth.Items.AddRange(MonthNames);
            cmbAmPm.Items.AddRange(new object[] { "AM", "PM" });

            cmbMonth.SelectedIndexChanged += OnMonthChanged;
            nudYear.ValueChanged += OnYearValueChanged;
            nudDay.ValueChanged += OnTimeComponentChanged;
            nudHour.ValueChanged += OnTimeComponentChanged;
            nudMinute.ValueChanged += OnTimeComponentChanged;
            nudSecond.ValueChanged += OnSecondValueChanged;
            cmbAmPm.SelectedIndexChanged += OnTimeComponentChanged;

            nudYear.TextChanged += OnSpinnerTextChanged;
            nudDay.TextChanged += OnSpinnerTextChanged;
            nudHour.TextChanged += OnSpinnerTextChanged;
            nudMinute.TextChanged += OnSpinnerTextChanged;
            nudSecond.TextChanged += OnSpinnerTextChanged;

            btnOK.Click += BtnOK_Click;

            DateTime nowQuantized = Timeline.QuantizeToRound(DateTime.Now);
            Configure(PickTimeMode.AdvanceTimeline, nowQuantized, nowQuantized);
        }

        public PickTime(PickTimeMode mode, DateTime initialTime, DateTime minAllowedTime) : this() {
            Configure(mode, initialTime, minAllowedTime);
        }

        public DateTime SelectedTime { get; private set; }

        public bool IsSelectionValid { get; private set; } = true;

        private void Configure(PickTimeMode mode, DateTime initialTime, DateTime minAllowedTime) {
            this.mode = mode;
            this.selectedKind = initialTime.Kind;
            DateTime quantizedInitial = ClampMaxQuantized(Timeline.QuantizeToRound(initialTime));
            this.minAllowedTime = minAllowedTime;

            isUpdating = true;
            try {
                ApplyModeFraming(mode);

                nudYear.CommitValue(quantizedInitial.Year);
                cmbMonth.SelectedIndex = quantizedInitial.Month - 1;
                UpdateDayBoundsForMonthAndYear(quantizedInitial.Year, quantizedInitial.Month);
                nudDay.CommitValue(quantizedInitial.Day);

                int hour24 = quantizedInitial.Hour;
                cmbAmPm.SelectedIndex = hour24 >= 12 ? 1 : 0;
                int hour12 = hour24 % 12;
                nudHour.CommitValue(hour12 == 0 ? 12 : hour12);
                nudMinute.CommitValue(quantizedInitial.Minute);
                nudSecond.CommitValue(quantizedInitial.Second);
            } finally {
                isUpdating = false;
            }

            RecomputeSelectionAndValidation();
        }

        private static DateTime ClampMaxQuantized(DateTime time) {
            return time > Timeline.MaxQuantizedDateTime
                ? new DateTime(Timeline.MaxQuantizedDateTime.Ticks, time.Kind)
                : time;
        }

        private static int QuantizeSecond(int second) {
            return (second / Timeline.SecondsPerRound) * Timeline.SecondsPerRound;
        }

        private void ApplyModeFraming(PickTimeMode currentMode) {
            if (currentMode == PickTimeMode.AdjustCampaignSetting) {
                Text = "Set Campaign Date & Time";
                lblModeDescription.Text =
                    "Campaign setting adjustment: sets the calendar date and time of the campaign setting " +
                    "rather than advancing play time. Active durations and resets do not advance.";
                btnOK.Text = "Set Campaign Time";
            } else {
                Text = "Advance In-Game Time";
                lblModeDescription.Text =
                    "Advance elapsed play time forward. Active durations and periodic resets will trigger " +
                    "across the elapsed rounds.";
                btnOK.Text = "Advance Time";
            }
        }

        private void OnMonthChanged(object? sender, EventArgs e) {
            if (isUpdating) {
                return;
            }

            if (nudYear.TryGetEffectiveValue(out int year)) {
                isUpdating = true;
                try {
                    int month = Math.Max(1, cmbMonth.SelectedIndex + 1);
                    UpdateDayBoundsForMonthAndYear(year, month);
                } finally {
                    isUpdating = false;
                }
            }

            RecomputeSelectionAndValidation();
        }

        private void OnYearValueChanged(object? sender, EventArgs e) {
            if (isUpdating) {
                return;
            }

            isUpdating = true;
            try {
                int year = (int)nudYear.Value;
                int month = Math.Max(1, cmbMonth.SelectedIndex + 1);
                UpdateDayBoundsForMonthAndYear(year, month);
            } finally {
                isUpdating = false;
            }

            RecomputeSelectionAndValidation();
        }

        private void OnSecondValueChanged(object? sender, EventArgs e) {
            if (isUpdating) {
                return;
            }

            int rawSecond = (int)nudSecond.Value;
            int snappedSecond = QuantizeSecond(rawSecond);
            if (snappedSecond != rawSecond) {
                isUpdating = true;
                try {
                    nudSecond.CommitValue(snappedSecond);
                } finally {
                    isUpdating = false;
                }
            }

            RecomputeSelectionAndValidation();
        }

        private void OnSpinnerTextChanged(object? sender, EventArgs e) {
            if (isUpdating) {
                return;
            }

            RecomputeSelectionAndValidation();
        }

        private void OnTimeComponentChanged(object? sender, EventArgs e) {
            if (isUpdating) {
                return;
            }

            RecomputeSelectionAndValidation();
        }

        private void BtnOK_Click(object? sender, EventArgs e) {
            if (!CommitPendingTextEdits()) {
                RecomputeSelectionAndValidation();
                return;
            }

            RecomputeSelectionAndValidation();
            if (IsSelectionValid) {
                DialogResult = DialogResult.OK;
            }
        }

        private bool CommitPendingTextEdits() {
            if (!TryReadSpinnerParts(out int year, out int month, out int day, out int hour12, out int minute, out int second)) {
                return false;
            }

            isUpdating = true;
            try {
                nudYear.CommitValue(year);
                UpdateDayBoundsForMonthAndYear(year, month);
                nudDay.CommitValue(day);
                nudHour.CommitValue(hour12);
                nudMinute.CommitValue(minute);
                nudSecond.CommitValue(second);
            } finally {
                isUpdating = false;
            }

            return true;
        }

        private bool TryReadSpinnerParts(out int year, out int month, out int day, out int hour12, out int minute, out int second) {
            year = month = day = hour12 = minute = second = 0;
            if (!nudYear.TryGetEffectiveValue(out year) ||
                !nudDay.TryGetEffectiveValue(out int rawDay) ||
                !nudHour.TryGetEffectiveValue(out hour12) ||
                !nudMinute.TryGetEffectiveValue(out minute) ||
                !nudSecond.TryGetEffectiveValue(out int rawSecond)) {
                return false;
            }

            month = Math.Max(1, cmbMonth.SelectedIndex + 1);
            int daysInMonth = DateTime.DaysInMonth(year, month);
            if (nudDay.IsEditingText && rawDay > daysInMonth) {
                return false;
            }

            day = Math.Min(rawDay, daysInMonth);
            second = QuantizeSecond(rawSecond);
            return true;
        }

        private void UpdateDayBoundsForMonthAndYear(int year, int month) {
            int daysInMonth = DateTime.DaysInMonth(year, month);
            if (daysInMonth > nudDay.Maximum) {
                nudDay.Maximum = daysInMonth;
            } else if (!nudDay.IsEditingText) {
                if (nudDay.Value > daysInMonth) {
                    nudDay.Value = daysInMonth;
                }
                nudDay.Maximum = daysInMonth;
            }
        }

        private void RecomputeSelectionAndValidation() {
            if (!TryReadSpinnerParts(out int year, out int month, out int day, out int hour12, out int minute, out int second)) {
                SetValidationState(false, "Enter a valid date and time within range.");
                return;
            }

            bool isPm = cmbAmPm.SelectedIndex == 1;
            int hour24 = isPm
                ? (hour12 == 12 ? 12 : hour12 + 12)
                : (hour12 == 12 ? 0 : hour12);

            SelectedTime = new DateTime(year, month, day, hour24, minute, second, selectedKind);

            if (SelectedTime < minAllowedTime) {
                SetValidationState(
                    false,
                    mode == PickTimeMode.AdjustCampaignSetting
                        ? "Campaign time would place Round Zero before January 1, Year 1."
                        : "Target time cannot be earlier than the current in-game time."
                );
            } else {
                SetValidationState(true, string.Empty);
            }
        }

        private void SetValidationState(bool isValid, string validationMessage) {
            IsSelectionValid = isValid;
            btnOK.Enabled = isValid;
            lblValidation.Text = validationMessage;
            lblValidation.Visible = !isValid;
        }

        private sealed class SpinnerNumericUpDown : NumericUpDown {
            private readonly string? format;

            public SpinnerNumericUpDown(string? format = null) {
                this.format = format;
                if (format != null) {
                    UpdateEditText();
                }
            }

            public bool IsEditingText => UserEdit;

            public bool TryGetEffectiveValue(out int result) {
                if (!UserEdit) {
                    result = (int)Value;
                    return true;
                }

                if (int.TryParse(Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) &&
                    parsed >= (int)Minimum &&
                    parsed <= (int)Maximum) {
                    result = parsed;
                    return true;
                }

                result = 0;
                return false;
            }

            public void CommitValue(int committedValue) {
                UserEdit = false;
                if ((int)Value != committedValue) {
                    Value = committedValue;
                } else {
                    UpdateEditText();
                }
            }

            protected override void OnValueChanged(EventArgs e) {
                UserEdit = false;
                base.OnValueChanged(e);
            }

            protected override void UpdateEditText() {
                if (UserEdit) {
                    ParseEditText();
                }
                if (format != null) {
                    ChangingText = true;
                    Text = ((int)Value).ToString(format, CultureInfo.InvariantCulture);
                } else {
                    base.UpdateEditText();
                }
            }
        }
    }

    public enum PickTimeMode {
        AdvanceTimeline,
        AdjustCampaignSetting
    }
}

