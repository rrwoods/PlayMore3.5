using System;
using System.Drawing;
using System.Windows.Forms;

namespace PlayMore35.UI {
    public class DateOverridePicker : LinkLabel {
        public static readonly Color DefaultTextColor = SystemColors.HotTrack;
        public static readonly Color OverriddenTextColor = Color.Chocolate;

        private DateTime selectedDate;

        public event EventHandler DateChanged;

        public DateTime SelectedDate {
            get => selectedDate;
            set {
                DateTime normalized = value.Date;
                if (selectedDate != normalized) {
                    selectedDate = normalized;
                    UpdateText();
                    DateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public string DisplayText => Text;

        public DateOverridePicker() {
            AutoSize = true;
            Cursor = Cursors.Hand;
            LinkBehavior = LinkBehavior.HoverUnderline;
            ActiveLinkColor = SystemColors.Highlight;
            selectedDate = DateTime.Today;
            UpdateText();
        }

        public void Reset() {
            SelectedDate = DateTime.Today;
        }

        protected override void OnLinkClicked(LinkLabelLinkClickedEventArgs e) {
            base.OnLinkClicked(e);
            ShowCalendarPopup();
        }

        protected override void OnClick(EventArgs e) {
            base.OnClick(e);
            if (!SystemInformation.MousePresent) {
                ShowCalendarPopup();
            }
        }

        private void ShowCalendarPopup() {
            var dropDown = new ToolStripDropDown();
            var calendar = new MonthCalendar {
                MaxSelectionCount = 1,
                ShowToday = true,
                ShowTodayCircle = true,
                SelectionStart = selectedDate,
                SelectionEnd = selectedDate
            };

            calendar.DateSelected += (s, e) => {
                SelectedDate = e.Start;
                dropDown.Close();
            };

            var host = new ToolStripControlHost(calendar) {
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                AutoSize = false,
                Size = calendar.Size
            };

            dropDown.Items.Add(host);
            dropDown.Show(this, new Point(0, Height));
        }

        private void UpdateText() {
            Text = $"[Date: {selectedDate:yyyy-MM-dd}]";
            bool isOverridden = selectedDate != DateTime.Today;
            Color targetColor = isOverridden ? OverriddenTextColor : DefaultTextColor;
            LinkColor = targetColor;
            VisitedLinkColor = targetColor;
            ForeColor = targetColor;

            FontStyle targetStyle = isOverridden ? FontStyle.Italic : FontStyle.Regular;
            if (Font.Style != targetStyle) {
                Font = new Font(Font, targetStyle);
            }
        }
    }
}
