using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Subsystems;

namespace PlayMore35.UI.Subsystems.PactMagic {
    public class VestigeCardControl : UserControl {
        private readonly SubsystemElement _vestige;
        private bool _isSelected;
        private IReadOnlyList<ValidationWarning> _warnings = Array.Empty<ValidationWarning>();
        private bool _isProgressionMode;
        private bool _isBound;
        private string? _pactState;
        private bool _isAtMaxCapacity;

        public event EventHandler? CardClicked;
        public event EventHandler? GoodPactClicked;
        public event EventHandler? PoorPactClicked;
        public event EventHandler? UnbindClicked;

        public SubsystemElement Vestige => _vestige;

        public bool IsSelected {
            get => _isSelected;
            set {
                if (_isSelected != value) {
                    _isSelected = value;
                    UpdateCardVisuals();
                }
            }
        }

        public IReadOnlyList<ValidationWarning> Warnings {
            get => _warnings;
            set {
                _warnings = value ?? Array.Empty<ValidationWarning>();
                UpdateCardVisuals();
            }
        }

        public bool IsProgressionMode {
            get => _isProgressionMode;
            set {
                if (_isProgressionMode != value) {
                    _isProgressionMode = value;
                    UpdateCardVisuals();
                }
            }
        }

        public bool IsInvalid {
            get => _warnings.Any(w => w.Kind == WarningKind.Unmet);
            set {
                if (value) {
                    if (!_warnings.Any(w => w.Kind == WarningKind.Unmet)) {
                        Warnings = new[] { new ValidationWarning("Ineligible", WarningKind.Unmet, ValidationCategory.General) };
                    }
                } else {
                    if (_warnings.Any(w => w.Kind == WarningKind.Unmet)) {
                        Warnings = _warnings.Where(w => w.Kind != WarningKind.Unmet).ToList();
                    }
                }
            }
        }

        public string StatusToolTip => FormUtils.TOOLTIP.GetToolTip(_lblStatus);

        public bool IsBound {
            get => _isBound;
            set {
                if (_isBound != value) {
                    _isBound = value;
                    UpdateCardVisuals();
                }
            }
        }

        public string? PactState {
            get => _pactState;
            set {
                if (_pactState != value) {
                    _pactState = value;
                    UpdateCardVisuals();
                }
            }
        }

        public bool IsAtMaxCapacity {
            get => _isAtMaxCapacity;
            set {
                if (_isAtMaxCapacity != value) {
                    _isAtMaxCapacity = value;
                    UpdateCardVisuals();
                }
            }
        }

        private readonly Label _lblTitle;
        private readonly Label _lblSign;
        private readonly Label _lblPowers;
        private readonly Panel _pnlActions;
        private readonly Label _lblStatus;
        private readonly Button _btnGoodPact;
        private readonly Button _btnPoorPact;
        private readonly Button _btnUnbind;
        private readonly Label _lblCapacityNotice;

        public Button GoodPactButton => _btnGoodPact;
        public Button PoorPactButton => _btnPoorPact;
        public Button UnbindButton => _btnUnbind;
        public Label StatusLabel => _lblStatus;
        public Label CapacityNoticeLabel => _lblCapacityNotice;

        public VestigeCardControl(SubsystemElement vestige) {
            _vestige = vestige ?? throw new ArgumentNullException(nameof(vestige));

            Size = new Size(212, 230);
            Margin = new Padding(4);
            Padding = new Padding(8);
            Cursor = Cursors.Hand;
            BackColor = Color.White;

            int level = _vestige.GetProperty<int>("level");
            int bindingDc = _vestige.GetProperty<int>("binding DC");
            string sign = _vestige.GetProperty<string>("sign");

            _lblTitle = new Label {
                Dock = DockStyle.Top,
                Height = 22,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 69, 120),
                Text = $"{_vestige.Name} ({level}{GetOrdinal(level)} • DC {bindingDc})",
                Cursor = Cursors.Hand
            };
            _lblTitle.Click += (s, e) => OnClick(e);

            _lblSign = new Label {
                Dock = DockStyle.Top,
                Height = 36,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(85, 85, 85),
                Text = $"\"{sign}\"",
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
            _btnGoodPact.Click += (s, e) => GoodPactClicked?.Invoke(this, EventArgs.Empty);

            _btnPoorPact = new Button {
                Text = "Poor",
                Size = new Size(52, 24),
                Location = new Point(144, 3),
                Font = new Font("Segoe UI", 7.5f, FontStyle.Regular),
                UseVisualStyleBackColor = true,
                Visible = false
            };
            _btnPoorPact.Click += (s, e) => PoorPactClicked?.Invoke(this, EventArgs.Empty);

            _btnUnbind = new Button {
                Text = "Unbind",
                Size = new Size(65, 24),
                Location = new Point(131, 3),
                Font = new Font("Segoe UI", 7.5f, FontStyle.Regular),
                UseVisualStyleBackColor = true,
                Visible = false
            };
            _btnUnbind.Click += (s, e) => UnbindClicked?.Invoke(this, EventArgs.Empty);

            _lblCapacityNotice = new Label {
                Location = new Point(60, 6),
                Size = new Size(136, 20),
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 7f, FontStyle.Italic),
                ForeColor = Color.FromArgb(180, 100, 0),
                Text = "At max vestige capacity",
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
                Text = FormatAbilitiesExcerpt(_vestige.GetEffects("abilities")),
                Cursor = Cursors.Hand
            };
            _lblPowers.Click += (s, e) => OnClick(e);

            Controls.Add(_lblPowers);
            Controls.Add(_pnlActions);
            Controls.Add(_lblSign);
            Controls.Add(_lblTitle);

            UpdateCardVisuals();
        }

        private static string FormatAbilitiesExcerpt(IReadOnlyList<Effect> abilities) {
            if (abilities == null || abilities.Count == 0) return "";
            var distinctNames = abilities.Select(a => a.Name).Distinct(StringComparer.OrdinalIgnoreCase);
            return string.Join("\n", distinctNames.Select(name => $"• {name}"));
        }

        public void UpdateCardVisuals() {
            if (_isSelected) {
                BackColor = Color.FromArgb(229, 241, 251);
            } else {
                BackColor = Color.White;
            }

            if (_isBound && !_isProgressionMode) {
                string pactDisplayName = string.Equals(_pactState, "poor_pact", StringComparison.OrdinalIgnoreCase) ? "Poor Pact" : "Good Pact";
                _lblStatus.Text = pactDisplayName;
                _lblStatus.ForeColor = string.Equals(_pactState, "poor_pact", StringComparison.OrdinalIgnoreCase)
                    ? Color.FromArgb(192, 57, 43)
                    : Color.FromArgb(0, 120, 215);

                _btnUnbind.Visible = true;
                _btnUnbind.Enabled = true;
                _btnGoodPact.Visible = false;
                _btnGoodPact.Enabled = false;
                _btnPoorPact.Visible = false;
                _btnPoorPact.Enabled = false;
                _lblCapacityNotice.Visible = false;
                FormUtils.ToolTipTextLabel(_lblStatus, "");
            } else {
                _btnUnbind.Visible = false;
                _btnUnbind.Enabled = false;

                var unmet = _warnings.Where(w => w.Kind == WarningKind.Unmet).ToList();
                var itemDep = _warnings.Where(w => w.Kind == WarningKind.ItemDependent).ToList();

                if (unmet.Count > 0) {
                    var worst = unmet.FirstOrDefault(w => w.Category == ValidationCategory.LevelGate) ?? unmet[0];
                    _lblStatus.Text = worst.Category == ValidationCategory.LevelGate ? "⚠ Exceeds" : "⚠ Ineligible";
                    _lblStatus.ForeColor = Color.DarkGoldenrod;
                    string tooltip = string.Join("\n", _warnings.Select(FormatWarningTooltip));
                    FormUtils.ToolTipTextLabel(_lblStatus, tooltip);
                } else if (itemDep.Count > 0) {
                    _lblStatus.Text = "🛈 Item-Dep";
                    _lblStatus.ForeColor = Color.DodgerBlue;
                    string message = string.Join("\n", itemDep.Select(w => w.Message));
                    string tooltip = $"{message}\n(Satisfied by currently equipped items)";
                    FormUtils.ToolTipTextLabel(_lblStatus, tooltip);
                } else {
                    _lblStatus.Text = "Available";
                    _lblStatus.ForeColor = Color.FromArgb(46, 125, 50);
                    FormUtils.ToolTipTextLabel(_lblStatus, "");
                }

                if (!_isProgressionMode && _isSelected && !IsInvalid) {
                    if (_isAtMaxCapacity) {
                        _lblCapacityNotice.Visible = true;
                        _btnGoodPact.Visible = false;
                        _btnGoodPact.Enabled = false;
                        _btnPoorPact.Visible = false;
                        _btnPoorPact.Enabled = false;
                    } else {
                        _lblCapacityNotice.Visible = false;
                        _btnGoodPact.Visible = true;
                        _btnGoodPact.Enabled = true;
                        _btnPoorPact.Visible = true;
                        _btnPoorPact.Enabled = true;
                    }
                } else {
                    _lblCapacityNotice.Visible = false;
                    _btnGoodPact.Visible = false;
                    _btnGoodPact.Enabled = false;
                    _btnPoorPact.Visible = false;
                    _btnPoorPact.Enabled = false;
                }
            }

            Invalidate();
        }

        private static string FormatWarningTooltip(ValidationWarning warning) {
            return warning.Kind == WarningKind.ItemDependent
                ? $"{warning.Message} (Satisfied by currently equipped items)"
                : warning.Message;
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
