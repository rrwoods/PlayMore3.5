using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using PlayMore35.Subsystems;

namespace PlayMore35.UI.Subsystems.PactMagic {
    public class VestigeCardControl : UserControl {
        private readonly SubsystemElement _vestige;
        private readonly VestigeRulesData _rulesData;
        private bool _isSelected;
        private bool _isInvalid;
        private string? _bindingStatusText;

        public event EventHandler? CardClicked;

        public SubsystemElement Vestige => _vestige;
        public VestigeRulesData RulesData => _rulesData;

        public bool IsSelected {
            get => _isSelected;
            set {
                if (_isSelected != value) {
                    _isSelected = value;
                    UpdateCardVisuals();
                }
            }
        }

        public bool IsInvalid {
            get => _isInvalid;
            set {
                if (_isInvalid != value) {
                    _isInvalid = value;
                    UpdateCardVisuals();
                }
            }
        }

        public string? BindingStatusText {
            get => _bindingStatusText;
            set {
                if (_bindingStatusText != value) {
                    _bindingStatusText = value;
                    UpdateCardVisuals();
                }
            }
        }

        private readonly Label _lblTitle;
        private readonly Label _lblSign;
        private readonly Label _lblPowers;
        private readonly Label _lblFooter;

        public VestigeCardControl(SubsystemElement vestige, VestigeRulesData rulesData) {
            _vestige = vestige ?? throw new ArgumentNullException(nameof(vestige));
            _rulesData = rulesData ?? throw new ArgumentNullException(nameof(rulesData));

            Size = new Size(212, 220);
            Margin = new Padding(4);
            Padding = new Padding(8);
            Cursor = Cursors.Hand;
            BackColor = Color.White;

            _lblTitle = new Label {
                Dock = DockStyle.Top,
                Height = 22,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 69, 120),
                Text = $"{_vestige.Name} ({_rulesData.Level}{GetOrdinal(_rulesData.Level)} • DC {_rulesData.BindingDC})",
                Cursor = Cursors.Hand
            };
            _lblTitle.Click += (s, e) => OnClick(e);

            _lblSign = new Label {
                Dock = DockStyle.Top,
                Height = 36,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(85, 85, 85),
                Text = $"\"{_rulesData.Sign}\"",
                Cursor = Cursors.Hand
            };
            _lblSign.Click += (s, e) => OnClick(e);

            _lblFooter = new Label {
                Dock = DockStyle.Bottom,
                Height = 18,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(102, 102, 102),
                Cursor = Cursors.Hand
            };
            _lblFooter.Click += (s, e) => OnClick(e);

            _lblPowers = new Label {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(51, 51, 51),
                Text = FormatAbilitiesExcerpt(_rulesData.Abilities),
                Cursor = Cursors.Hand
            };
            _lblPowers.Click += (s, e) => OnClick(e);

            Controls.Add(_lblPowers);
            Controls.Add(_lblFooter);
            Controls.Add(_lblSign);
            Controls.Add(_lblTitle);

            UpdateCardVisuals();
        }

        private static string FormatAbilitiesExcerpt(IReadOnlyList<VestigeAbilityData> abilities) {
            if (abilities == null || abilities.Count == 0) return "";
            return string.Join("\n", abilities.Select(a => $"• {a.Name}"));
        }

        private void UpdateCardVisuals() {
            if (_isSelected) {
                BackColor = Color.FromArgb(229, 241, 251);
            } else {
                BackColor = Color.White;
            }

            string status = _isInvalid ? "⚠ Exceeds level" : "✓ Available";
            if (!string.IsNullOrEmpty(_bindingStatusText)) {
                status += $" | {_bindingStatusText}";
            }
            _lblFooter.Text = status;
            if (_isInvalid) {
                _lblFooter.ForeColor = Color.DarkGoldenrod;
            } else if (!string.IsNullOrEmpty(_bindingStatusText)) {
                _lblFooter.ForeColor = Color.FromArgb(0, 120, 215);
            } else {
                _lblFooter.ForeColor = Color.FromArgb(46, 125, 50);
            }

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            if (_isSelected) {
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
            CardClicked?.Invoke(this, EventArgs.Empty);
        }

        private static string GetOrdinal(int n) {
            if (n == 1) return "st";
            if (n == 2) return "nd";
            if (n == 3) return "rd";
            return "th";
        }
    }
}
