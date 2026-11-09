using System;
using System.Drawing;
using System.Windows.Forms;
using PlayMore35.Global;

namespace PlayMore35.UI.Subsystems.PactMagic {
    public class VestigeCardControl : UserControl {
        public VestigeCardViewModel ViewModel { get; }

        private readonly Label _lblTitle;
        private readonly Label _lblSign;
        private readonly Label _lblPowers;
        private readonly Panel _pnlActions;
        private readonly Label _lblStatus;
        private readonly Button _btnGoodPact;
        private readonly Button _btnPoorPact;
        private readonly Button _btnUnbind;
        private readonly Label _lblCapacityNotice;

        public VestigeCardControl(VestigeCardViewModel viewModel) {
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

            Size = new Size(212, 230);
            Margin = new Padding(4);
            Padding = new Padding(8);
            Cursor = Cursors.Hand;
            BackColor = Color.White;

            _lblTitle = new Label {
                Dock = DockStyle.Top,
                Height = 22,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 69, 120),
                Text = ViewModel.CardTitleText,
                Cursor = Cursors.Hand
            };
            _lblTitle.Click += (s, e) => OnClick(e);

            _lblSign = new Label {
                Dock = DockStyle.Top,
                Height = 36,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(85, 85, 85),
                Text = ViewModel.CardSignText,
                Cursor = Cursors.Hand
            };
            _lblSign.Click += (s, e) => OnClick(e);

            _pnlActions = new Panel {
                Dock = DockStyle.Bottom,
                Height = 30,
                BackColor = Color.Transparent
            };

            _lblStatus = new Label {
                Location = new Point(0, 6),
                AutoSize = true,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(46, 125, 50),
                Cursor = Cursors.Hand
            };
            _lblStatus.Click += (s, e) => OnClick(e);

            _btnGoodPact = new Button {
                Text = "Good",
                Size = new Size(52, 24),
                Location = new Point(88, 3),
                Font = new Font("Segoe UI", 7.5f, FontStyle.Regular),
                UseVisualStyleBackColor = true,
                Visible = false
            };
            _btnGoodPact.Click += (s, e) => ViewModel.BindGoodPact();

            _btnPoorPact = new Button {
                Text = "Poor",
                Size = new Size(52, 24),
                Location = new Point(144, 3),
                Font = new Font("Segoe UI", 7.5f, FontStyle.Regular),
                UseVisualStyleBackColor = true,
                Visible = false
            };
            _btnPoorPact.Click += (s, e) => ViewModel.BindPoorPact();

            _btnUnbind = new Button {
                Text = "Unbind",
                Size = new Size(65, 24),
                Location = new Point(131, 3),
                Font = new Font("Segoe UI", 7.5f, FontStyle.Regular),
                UseVisualStyleBackColor = true,
                Visible = false
            };
            _btnUnbind.Click += (s, e) => ViewModel.Unbind();

            _lblCapacityNotice = new Label {
                Location = new Point(60, 6),
                Size = new Size(136, 20),
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 7f, FontStyle.Italic),
                ForeColor = Color.FromArgb(180, 100, 0),
                Text = ViewModel.CapacityNoticeText,
                Visible = false
            };

            _pnlActions.Controls.Add(_lblStatus);
            _pnlActions.Controls.Add(_btnGoodPact);
            _pnlActions.Controls.Add(_btnPoorPact);
            _pnlActions.Controls.Add(_btnUnbind);
            _pnlActions.Controls.Add(_lblCapacityNotice);

            _lblPowers = new Label {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(51, 51, 51),
                Text = ViewModel.CardAbilitiesExcerpt,
                Cursor = Cursors.Hand
            };
            _lblPowers.Click += (s, e) => OnClick(e);

            Controls.Add(_lblPowers);
            Controls.Add(_pnlActions);
            Controls.Add(_lblSign);
            Controls.Add(_lblTitle);

            ViewModel.StateChanged += (s, e) => UpdateCardVisuals();
            UpdateCardVisuals();
        }

        public void UpdateCardVisuals() {
            if (ViewModel.IsSelected) {
                BackColor = Color.FromArgb(229, 241, 251);
            } else {
                BackColor = Color.White;
            }

            _lblStatus.Text = ViewModel.StatusText;
            _lblStatus.ForeColor = GetStatusColor(ViewModel.StatusKind);
            FormUtils.ToolTipTextLabel(_lblStatus, ViewModel.StatusTooltip);

            _btnUnbind.Visible = ViewModel.IsUnbindVisible;
            _btnUnbind.Enabled = ViewModel.IsUnbindEnabled;

            _btnGoodPact.Visible = ViewModel.IsGoodPactVisible;
            _btnGoodPact.Enabled = ViewModel.IsGoodPactEnabled;

            _btnPoorPact.Visible = ViewModel.IsPoorPactVisible;
            _btnPoorPact.Enabled = ViewModel.IsPoorPactEnabled;

            _lblCapacityNotice.Visible = ViewModel.IsCapacityNoticeVisible;
            _lblCapacityNotice.Text = ViewModel.CapacityNoticeText;

            Visible = ViewModel.IsVisible;
            Invalidate();
        }

        private static Color GetStatusColor(VestigeCardStatusKind statusKind) {
            switch (statusKind) {
                case VestigeCardStatusKind.PoorPact:
                    return Color.FromArgb(192, 57, 43);
                case VestigeCardStatusKind.GoodPact:
                    return Color.FromArgb(0, 120, 215);
                case VestigeCardStatusKind.Exceeds:
                case VestigeCardStatusKind.Ineligible:
                    return Color.DarkGoldenrod;
                case VestigeCardStatusKind.ItemDependent:
                    return Color.DodgerBlue;
                case VestigeCardStatusKind.Available:
                default:
                    return Color.FromArgb(46, 125, 50);
            }
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            if (ViewModel.IsSelected) {
                using (var pen = new Pen(Color.FromArgb(0, 120, 215), 2f)) {
                    e.Graphics.DrawRectangle(pen, 1, 1, Width - 3, Height - 3);
                }
            } else {
                using (var pen = new Pen(Color.FromArgb(204, 204, 204), 1f)) {
                    e.Graphics.DrawRectangle(pen, rect);
                }
            }
        }

        protected override void OnClick(EventArgs e) {
            base.OnClick(e);
            ViewModel.Select();
        }
    }
}
