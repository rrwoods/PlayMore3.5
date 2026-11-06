using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        private SubsystemElement? _selectedElement;
        private bool _isActiveSelecting;
        private string _placeholderText = "(Choose element...)";

        public event EventHandler? Activated;
        public event EventHandler? ElementCleared;
        public event Action<SubsystemElement?>? ElementDelivered;

        public ElementReceptorControl() {
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
            UpdateLayout();
            UpdateVisualState();
        }

        public string PlaceholderText {
            get => _placeholderText;
            set {
                _placeholderText = value;
                UpdateVisualState();
            }
        }

        public SubsystemElement? SelectedElement {
            get => _selectedElement;
            set {
                _selectedElement = value;
                UpdateVisualState();
            }
        }

        public bool IsActiveSelecting {
            get => _isActiveSelecting;
            set {
                if (_isActiveSelecting != value) {
                    _isActiveSelecting = value;
                    UpdateVisualState();
                }
            }
        }

        public ReceptorState State {
            get {
                if (_isActiveSelecting) return ReceptorState.ActiveSelecting;
                return _selectedElement != null ? ReceptorState.Made : ReceptorState.Unmade;
            }
        }

        public bool ClearButtonVisible => _clearButton.Visible;
        public bool WarningBadgeVisible => _warningBadge.Visible;
        public string DisplayText => _label.Text;

        public void PerformClick() {
            Activated?.Invoke(this, EventArgs.Empty);
        }

        public void PerformClear() {
            _selectedElement = null;
            UpdateVisualState();
            ElementCleared?.Invoke(this, EventArgs.Empty);
            ElementDelivered?.Invoke(null);
        }

        public void SetWarnings(IEnumerable<ValidationWarning>? warnings) {
            _warningBadge.SetWarnings(warnings);
            UpdateLayout();
        }

        public void ClearWarning() {
            _warningBadge.ClearWarning();
            UpdateLayout();
        }

        private void UpdateVisualState() {
            if (_selectedElement != null) {
                _label.Text = _selectedElement.Name;
                _label.Font = new Font(Font, FontStyle.Regular);
                _label.ForeColor = SystemColors.ControlText;
                _clearButton.Visible = true;
            } else {
                _label.Text = _placeholderText;
                _label.Font = new Font(Font, FontStyle.Italic);
                _label.ForeColor = SystemColors.GrayText;
                _clearButton.Visible = false;
            }

            if (_isActiveSelecting) {
                BackColor = Color.FromArgb(240, 245, 255);
            } else {
                BackColor = SystemColors.Window;
            }

            UpdateLayout();
            Invalidate();
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

            if (_isActiveSelecting) {
                using (var pen = new Pen(Color.FromArgb(0, 120, 215), 2f)) {
                    e.Graphics.DrawRectangle(pen, 1, 1, Width - 3, Height - 3);
                }
            } else if (_selectedElement == null) {
                using (var pen = new Pen(SystemColors.ControlDark, 1f)) {
                    pen.DashStyle = DashStyle.Dash;
                    e.Graphics.DrawRectangle(pen, rect);
                }
            } else {
                using (var pen = new Pen(SystemColors.ControlDark, 1f)) {
                    pen.DashStyle = DashStyle.Solid;
                    e.Graphics.DrawRectangle(pen, rect);
                }
            }
        }

        protected override void OnClick(EventArgs e) {
            base.OnClick(e);
            PerformClick();
        }
    }
}
