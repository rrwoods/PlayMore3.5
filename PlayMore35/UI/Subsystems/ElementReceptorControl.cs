using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using PlayMore35.Global;
using PlayMore35.Subsystems;

namespace PlayMore35.UI.Subsystems {
    public class ElementReceptorControl : UserControl {
        private readonly Label _label;
        private readonly Button _clearButton;
        private readonly WarningBadge _warningBadge;
        private bool _isSyncing;

        public ElementReceptorViewModel ViewModel { get; }

        public event EventHandler? Activated {
            add => ViewModel.Activated += value;
            remove => ViewModel.Activated -= value;
        }

        public event EventHandler? ElementCleared {
            add => ViewModel.ElementCleared += value;
            remove => ViewModel.ElementCleared -= value;
        }

        public event Action<SubsystemElement?>? ElementDelivered {
            add => ViewModel.ElementDelivered += value;
            remove => ViewModel.ElementDelivered -= value;
        }

        public ElementReceptorControl() : this(new ElementReceptorViewModel()) {
        }

        public ElementReceptorControl(ElementReceptorViewModel viewModel) {
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

            _label = new Label {
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            _label.Click += (s, e) => PerformClick();

            _clearButton = new Button {
                Text = "✕",
                FlatStyle = FlatStyle.Flat,
                Size = new Size(18, 18),
                Cursor = Cursors.Hand,
                Visible = false
            };
            _clearButton.FlatAppearance.BorderSize = 0;
            _clearButton.Click += (s, e) => PerformClear();

            _warningBadge = new WarningBadge {
                Cursor = Cursors.Help,
                Visible = false
            };

            Controls.Add(_clearButton);
            Controls.Add(_warningBadge);
            Controls.Add(_label);

            Height = 24;
            Width = 180;
            Padding = new Padding(2);

            ViewModel.StateChanged += (s, e) => UpdateVisualState();
            UpdateLayout();
            UpdateVisualState();
        }

        public SubsystemElement? SelectedElement {
            get => ViewModel.SelectedElement;
            set => ViewModel.SelectedElement = value;
        }

        public bool IsActiveSelecting {
            get => ViewModel.IsActiveSelecting;
            set => ViewModel.IsActiveSelecting = value;
        }

        public void PerformClick() {
            ViewModel.Activate();
        }

        public void PerformClear() {
            ViewModel.Clear();
        }

        public void SetWarnings(IEnumerable<ValidationWarning>? warnings) {
            ViewModel.SetWarnings(warnings);
        }

        public void ClearWarning() {
            ViewModel.ClearWarning();
        }

        private void UpdateVisualState() {
            if (_isSyncing) return;
            _isSyncing = true;
            try {
                _label.Text = ViewModel.DisplayText;
                _label.Font = ViewModel.SelectedElement != null ? new Font(Font, FontStyle.Regular) : new Font(Font, FontStyle.Italic);
                _label.ForeColor = ViewModel.SelectedElement != null ? SystemColors.ControlText : SystemColors.GrayText;
                _clearButton.Visible = ViewModel.IsClearButtonVisible;

                if (ViewModel.IsActiveSelecting) {
                    BackColor = Color.FromArgb(240, 245, 255);
                } else {
                    BackColor = SystemColors.Window;
                }

                _warningBadge.Visible = ViewModel.IsWarningBadgeVisible;
                if (ViewModel.IsWarningBadgeVisible) {
                    _warningBadge.Text = ViewModel.WarningBadgeText;
                    _warningBadge.ForeColor = ViewModel.WorstWarningKind == WarningKind.Unmet ? Color.DarkGoldenrod : Color.DodgerBlue;
                    FormUtils.ToolTipTextLabel(_warningBadge, ViewModel.WarningTooltip);
                } else {
                    FormUtils.ToolTipTextLabel(_warningBadge, "");
                }

                UpdateLayout();
                Invalidate();
            } finally {
                _isSyncing = false;
            }
        }

        private void UpdateLayout() {
            int rightOffset = Width - Padding.Right;

            if (_clearButton.Visible) {
                rightOffset -= _clearButton.Width + 2;
                _clearButton.Location = new Point(rightOffset, (Height - _clearButton.Height) / 2);
            }

            if (_warningBadge.Visible) {
                rightOffset -= _warningBadge.Width + 2;
                _warningBadge.Location = new Point(rightOffset, (Height - _warningBadge.Height) / 2);
            }

            int leftOffset = Padding.Left + 4;
            _label.Location = new Point(leftOffset, 2);
            _label.Size = new Size(Math.Max(0, rightOffset - leftOffset), Height - 4);
        }

        protected override void OnResize(EventArgs e) {
            base.OnResize(e);
            UpdateLayout();
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            switch (ViewModel.State) {
                case ReceptorState.ActiveSelecting:
                    using (var pen = new Pen(Color.FromArgb(0, 120, 215), 2f)) {
                        e.Graphics.DrawRectangle(pen, 1, 1, Width - 3, Height - 3);
                    }
                    break;
                case ReceptorState.Unmade:
                    using (var pen = new Pen(SystemColors.ControlDark, 1f)) {
                        pen.DashStyle = DashStyle.Dash;
                        e.Graphics.DrawRectangle(pen, rect);
                    }
                    break;
                case ReceptorState.Made:
                    using (var pen = new Pen(SystemColors.ControlDark, 1f)) {
                        pen.DashStyle = DashStyle.Solid;
                        e.Graphics.DrawRectangle(pen, rect);
                    }
                    break;
            }
        }

        protected override void OnClick(EventArgs e) {
            base.OnClick(e);
            PerformClick();
        }
    }
}
