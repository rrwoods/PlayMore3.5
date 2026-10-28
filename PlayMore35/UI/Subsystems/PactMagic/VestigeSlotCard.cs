using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using PlayMore35.Subsystems;

namespace PlayMore35.UI.Subsystems.PactMagic {
    public class VestigeSlotCard : UserControl {
        private readonly int _slotIndex;
        private readonly ElementReceptorControl _receptor;
        private readonly RadioButton _rbGoodPact;
        private readonly RadioButton _rbPoorPact;
        private readonly Panel _pnlSymptoms;
        private readonly Label _lblSymptomsSign;
        private readonly Label _lblSymptomsInfluence;
        private readonly Label _lblSymptomsPowers;
        private readonly Label _lblStatus;
        private readonly WarningBadge _warningBadge;
        private bool _isUpdating;

        public event EventHandler? SlotActivated;
        public event EventHandler? UnbindClicked;
        public event Action<string>? PactStateChanged;

        public int SlotIndex => _slotIndex;
        public ElementReceptorControl Receptor => _receptor;
        public RadioButton GoodPactRadio => _rbGoodPact;
        public RadioButton PoorPactRadio => _rbPoorPact;
        public Panel SymptomsPanel => _pnlSymptoms;
        public string SymptomsSignText => _lblSymptomsSign.Text;
        public string SymptomsInfluenceText => _lblSymptomsInfluence.Text;
        public string SymptomsPowersText => _lblSymptomsPowers.Text;
        public bool WarningBadgeVisible => _warningBadge.Visible;
        public string? WarningMessage => _warningBadge.WarningMessage;

        public VestigeSlotCard(int slotIndex) {
            _slotIndex = slotIndex;

            Size = new Size(330, 240);
            Margin = new Padding(0, 0, 0, 10);
            Padding = new Padding(8);
            BackColor = Color.White;

            var pnlHeader = new Panel {
                Dock = DockStyle.Top,
                Height = 22
            };

            _lblStatus = new Label {
                Dock = DockStyle.Left,
                Text = "Empty",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(102, 102, 102),
                AutoSize = true
            };

            _warningBadge = new WarningBadge {
                Dock = DockStyle.Left,
                Margin = new Padding(4, 0, 0, 0),
                Visible = false
            };

            pnlHeader.Controls.Add(_warningBadge);
            pnlHeader.Controls.Add(_lblStatus);

            _receptor = new ElementReceptorControl {
                Dock = DockStyle.Top,
                Height = 28,
                PlaceholderText = "(Choose vestige...)"
            };
            _receptor.Activated += (s, e) => SlotActivated?.Invoke(this, EventArgs.Empty);
            _receptor.ElementCleared += (s, e) => UnbindClicked?.Invoke(this, EventArgs.Empty);

            var pnlPact = new Panel {
                Dock = DockStyle.Top,
                Height = 26,
                Padding = new Padding(0, 4, 0, 0)
            };

            var lblPact = new Label {
                Location = new Point(2, 4),
                Text = "Pact:",
                Font = new Font("Segoe UI", 8f, FontStyle.Regular),
                ForeColor = Color.FromArgb(102, 102, 102),
                AutoSize = true
            };

            _rbGoodPact = new RadioButton {
                Location = new Point(45, 2),
                Text = "Good",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                AutoSize = true,
                Enabled = false,
                Checked = true
            };
            _rbGoodPact.CheckedChanged += (s, e) => {
                if (_isUpdating) return;
                if (_rbGoodPact.Checked) {
                    PactStateChanged?.Invoke("good_pact");
                    UpdateSymptomsVisuals();
                }
            };

            _rbPoorPact = new RadioButton {
                Location = new Point(110, 2),
                Text = "Poor",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                AutoSize = true,
                Enabled = false
            };
            _rbPoorPact.CheckedChanged += (s, e) => {
                if (_isUpdating) return;
                if (_rbPoorPact.Checked) {
                    PactStateChanged?.Invoke("poor_pact");
                    UpdateSymptomsVisuals();
                }
            };

            pnlPact.Controls.Add(_rbPoorPact);
            pnlPact.Controls.Add(_rbGoodPact);
            pnlPact.Controls.Add(lblPact);

            _pnlSymptoms = new Panel {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 249, 250),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(6),
                Visible = false
            };

            _lblSymptomsSign = new Label {
                Dock = DockStyle.Top,
                Height = 36,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(68, 68, 68),
                Padding = new Padding(0, 0, 0, 4)
            };

            _lblSymptomsInfluence = new Label {
                Dock = DockStyle.Top,
                Height = 44,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(68, 68, 68),
                Padding = new Padding(0, 0, 0, 4)
            };

            _lblSymptomsPowers = new Label {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(68, 68, 68)
            };

            _pnlSymptoms.Controls.Add(_lblSymptomsPowers);
            _pnlSymptoms.Controls.Add(_lblSymptomsInfluence);
            _pnlSymptoms.Controls.Add(_lblSymptomsSign);

            Controls.Add(_pnlSymptoms);
            Controls.Add(pnlPact);
            Controls.Add(_receptor);
            Controls.Add(pnlHeader);

            UpdateVisuals();
        }

        public void SetBoundVestige(SubsystemElement? vestige, string pactState, int maxVestigeLevel = 8) {
            _isUpdating = true;
            try {
                _receptor.SelectedElement = vestige;
                bool isBound = vestige != null;

                _rbGoodPact.Enabled = isBound;
                _rbPoorPact.Enabled = isBound;

                if (isBound) {
                    if (string.Equals(pactState, "poor_pact", StringComparison.OrdinalIgnoreCase)) {
                        _rbPoorPact.Checked = true;
                    } else {
                        _rbGoodPact.Checked = true;
                    }
                    _lblStatus.Text = "Bound";
                    _lblStatus.ForeColor = Color.FromArgb(21, 87, 36);
                    _pnlSymptoms.Visible = true;

                    var rules = VestigeRulesRepository.GetRulesData(vestige!);
                    if (rules.Level > maxVestigeLevel) {
                        _warningBadge.SetWarning($"Vestige level ({rules.Level}) exceeds maximum vestige level ({maxVestigeLevel}).");
                    } else {
                        _warningBadge.ClearWarning();
                    }

                    _lblSymptomsSign.Text = $"Sign: {rules.Sign}";
                    _lblSymptomsInfluence.Text = $"Influence: {rules.Influence}";
                    _lblSymptomsPowers.Text = "Powers: " + string.Join(", ", rules.Abilities.Select(a => a.Name));
                    UpdateSymptomsVisuals();
                } else {
                    _rbGoodPact.Checked = true;
                    _lblStatus.Text = "Empty";
                    _lblStatus.ForeColor = Color.FromArgb(102, 102, 102);
                    _warningBadge.ClearWarning();
                    _pnlSymptoms.Visible = false;
                }

                Invalidate();
            } finally {
                _isUpdating = false;
            }
        }

        public void SetActiveSelecting(bool active) {
            _receptor.IsActiveSelecting = active;
            BackColor = active ? Color.FromArgb(252, 253, 255) : Color.White;
            Invalidate();
        }

        private void UpdateSymptomsVisuals() {
            if (_receptor.SelectedElement != null) {
                var rules = VestigeRulesRepository.GetRulesData(_receptor.SelectedElement);
                _lblSymptomsSign.Text = $"Sign: {rules.Sign}";
            }
        }

        private void UpdateVisuals() {
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            if (_receptor.IsActiveSelecting) {
                using (var pen = new Pen(Color.FromArgb(0, 120, 215), 1.5f)) {
                    e.Graphics.DrawRectangle(pen, rect);
                }
            } else {
                using (var pen = new Pen(Color.FromArgb(204, 204, 204), 1f)) {
                    e.Graphics.DrawRectangle(pen, rect);
                }
            }
        }

        protected override void OnClick(EventArgs e) {
            base.OnClick(e);
            SlotActivated?.Invoke(this, EventArgs.Empty);
        }
    }
}
