using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using PlayMore35.Global;
using PlayMore35.Subsystems;

namespace PlayMore35.UI.Subsystems.PactMagic {
    public class PactMagicElementPicker : UserControl, ISubsystemElementPicker {
        private readonly ICharacterSession _session;
        private readonly ElementPickerContext _context;
        private readonly List<SubsystemElement> _allVestiges;
        private readonly List<VestigeCardControl> _cards = new List<VestigeCardControl>();

        private TextBox _searchBox = null!;
        private CheckBox _hideUnavailableCheckBox = null!;
        private ComboBox _cboLevelFilter = null!;
        private FlowLayoutPanel _cardsContainer = null!;
        private Panel _detailsPanel = null!;
        private Label _lblDetailsTitle = null!;
        private Label _lblDetailsSign = null!;
        private Label _lblDetailsInfluence = null!;
        private Label _lblDetailsAbilities = null!;

        private VestigeCardControl? _selectedCard;
        private int _maxVestigeLevel = 8;
        private int _targetSlotIndex = 0;

        public event Action<SubsystemElement?>? ElementDelivered;
        public event Action<IReadOnlyList<SubsystemElement>>? ElementsDelivered;

        public Control Control => this;
        public IReadOnlyList<VestigeCardControl> Cards => _cards;
        public int VisibleCardsCount => _cards.Count(c => c.Visible);

        public string SearchQuery {
            get => _searchBox.Text;
            set {
                _searchBox.Text = value;
                ApplyFilter();
            }
        }

        public bool HideUnavailable {
            get => _hideUnavailableCheckBox.Checked;
            set {
                _hideUnavailableCheckBox.Checked = value;
                ApplyFilter();
            }
        }

        public string DetailsHeader => _lblDetailsTitle.Text;
        public string DetailsSignText => _lblDetailsSign.Text;
        public string DetailsInfluenceText => _lblDetailsInfluence.Text;
        public string DetailsAbilitiesText => _lblDetailsAbilities.Text;

        public PactMagicElementPicker(ICharacterSession session, ElementPickerContext context) {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _context = context ?? throw new ArgumentNullException(nameof(context));

            if (session.Catalog.TryGetSubsystem("pact_magic", out var pactDef)) {
                _allVestiges = pactDef.Elements.Values.OrderBy(v => v.Name).ToList();
            } else {
                _allVestiges = new List<SubsystemElement>();
            }

            InitializeComponent();
            RefreshMaxVestigeLevel();
            PopulateCards();
            ApplyFilter();
        }

        public void SetTargetSlot(int slotIndex) {
            _targetSlotIndex = slotIndex;
            UpdateCardBoundIndicators();
        }

        public void RefreshMaxVestigeLevel() {
            var stat = _session.GetStatistic("maximum vestige level");
            _maxVestigeLevel = stat != null && stat.Value > 0 ? stat.Value : 8;
            foreach (var card in _cards) {
                card.IsInvalid = card.RulesData.Level > _maxVestigeLevel;
            }
            ApplyFilter();
        }

        public void UpdateCardBoundIndicators() {
            var engine = _session.Subsystems.Get("pact_magic") as SubsystemEngine;
            var slots = engine?.GetSlots("vestiges") ?? Array.Empty<SubsystemSlot>();

            foreach (var card in _cards) {
                string? status = null;
                for (int i = 0; i < slots.Count; i++) {
                    if (string.Equals(slots[i].Element, card.Vestige.Name, StringComparison.OrdinalIgnoreCase) && slots[i].State != "empty") {
                        if (i == _targetSlotIndex) {
                            status = $"Bound (Slot {i + 1})";
                        } else {
                            status = $"(Slot {i + 1})";
                        }
                        break;
                    }
                }
                card.BindingStatusText = status;
            }
        }

        public void DeliverVestigeByName(string name) {
            var card = _cards.FirstOrDefault(c => string.Equals(c.Vestige.Name, name, StringComparison.OrdinalIgnoreCase));
            if (card != null) {
                SelectCard(card);
            }
        }

        public void SelectVestigeDetail(string name) {
            var card = _cards.FirstOrDefault(c => string.Equals(c.Vestige.Name, name, StringComparison.OrdinalIgnoreCase));
            if (card != null) {
                UpdateDetailsPanel(card.RulesData);
            }
        }

        private void InitializeComponent() {
            SuspendLayout();
            Dock = DockStyle.Fill;
            BackColor = SystemColors.Control;

            var topToolbar = new Panel {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Color.FromArgb(247, 247, 247),
                Padding = new Padding(6)
            };

            _searchBox = new TextBox {
                Location = new Point(6, 7),
                Width = 200,
                Font = new Font("Segoe UI", 9f)
            };
            _searchBox.TextChanged += (s, e) => ApplyFilter();

            _hideUnavailableCheckBox = new CheckBox {
                Location = new Point(216, 8),
                Text = "Hide unavailable",
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f)
            };
            _hideUnavailableCheckBox.CheckedChanged += (s, e) => ApplyFilter();

            _cboLevelFilter = new ComboBox {
                Location = new Point(340, 7),
                Width = 110,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 8.5f)
            };
            _cboLevelFilter.Items.Add("All Levels");
            for (int i = 1; i <= 8; i++) {
                _cboLevelFilter.Items.Add($"Level {i}");
            }
            _cboLevelFilter.SelectedIndex = 0;
            _cboLevelFilter.SelectedIndexChanged += (s, e) => ApplyFilter();

            topToolbar.Controls.Add(_cboLevelFilter);
            topToolbar.Controls.Add(_hideUnavailableCheckBox);
            topToolbar.Controls.Add(_searchBox);

            _detailsPanel = new Panel {
                Dock = DockStyle.Bottom,
                Height = 160,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = true,
                Padding = new Padding(8)
            };

            _lblDetailsTitle = new Label {
                Dock = DockStyle.Top,
                Height = 22,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 69, 120)
            };

            _lblDetailsSign = new Label {
                Dock = DockStyle.Top,
                Height = 32,
                Font = new Font("Segoe UI", 8f, FontStyle.Regular),
                ForeColor = Color.FromArgb(68, 68, 68)
            };

            _lblDetailsInfluence = new Label {
                Dock = DockStyle.Top,
                Height = 32,
                Font = new Font("Segoe UI", 8f, FontStyle.Regular),
                ForeColor = Color.FromArgb(68, 68, 68)
            };

            _lblDetailsAbilities = new Label {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 8f, FontStyle.Regular),
                ForeColor = Color.FromArgb(51, 51, 51)
            };

            _detailsPanel.Controls.Add(_lblDetailsAbilities);
            _detailsPanel.Controls.Add(_lblDetailsInfluence);
            _detailsPanel.Controls.Add(_lblDetailsSign);
            _detailsPanel.Controls.Add(_lblDetailsTitle);

            _cardsContainer = new FlowLayoutPanel {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(250, 250, 250),
                Padding = new Padding(8),
                WrapContents = true
            };

            Controls.Add(_cardsContainer);
            Controls.Add(_detailsPanel);
            Controls.Add(topToolbar);

            ResumeLayout(false);
        }

        private void PopulateCards() {
            _cardsContainer.SuspendLayout();
            _cardsContainer.Controls.Clear();
            _cards.Clear();

            foreach (var elem in _allVestiges) {
                var rules = VestigeRulesRepository.GetRulesData(elem);
                var card = new VestigeCardControl(elem, rules);
                card.IsInvalid = rules.Level > _maxVestigeLevel;
                card.CardClicked += (s, e) => SelectCard(card);
                _cards.Add(card);
                _cardsContainer.Controls.Add(card);
            }

            if (_cards.Count > 0) {
                UpdateDetailsPanel(_cards[0].RulesData);
            }

            _cardsContainer.ResumeLayout(true);
        }

        private void SelectCard(VestigeCardControl card) {
            foreach (var c in _cards) {
                c.IsSelected = (c == card);
            }
            _selectedCard = card;
            UpdateDetailsPanel(card.RulesData);

            ElementDelivered?.Invoke(card.Vestige);
            ElementsDelivered?.Invoke(new[] { card.Vestige });

            UpdateCardBoundIndicators();
        }

        private void UpdateDetailsPanel(VestigeRulesData rules) {
            _lblDetailsTitle.Text = $"{rules.Name} (Level {rules.Level}, Binding DC {rules.BindingDC})";
            _lblDetailsSign.Text = $"Physical Sign: {rules.Sign}\n(Good pact: suppress at will; Poor pact: cannot be hidden)";
            _lblDetailsInfluence.Text = $"Psychological Influence: {rules.Influence}\n(Poor pact: -1 penalty on attack/saves/checks if violated)";

            var abilitiesFormatted = rules.Abilities.Select(a => $"• {a.Name} ({a.Type}): {a.Text}");
            _lblDetailsAbilities.Text = "Granted Abilities:\n" + string.Join("\n", abilitiesFormatted);
        }

        private void ApplyFilter() {
            _cardsContainer.SuspendLayout();

            string q = _searchBox.Text.Trim().ToLowerInvariant();
            int selectedLevel = _cboLevelFilter.SelectedIndex; // 0 = all, 1 = lvl 1, etc.

            foreach (var card in _cards) {
                bool visible = true;

                if (_hideUnavailableCheckBox.Checked && card.RulesData.Level > _maxVestigeLevel) {
                    visible = false;
                }

                if (selectedLevel > 0 && card.RulesData.Level != selectedLevel) {
                    visible = false;
                }

                if (visible && !string.IsNullOrEmpty(q)) {
                    bool nameMatch = card.Vestige.Name.ToLowerInvariant().Contains(q);
                    bool abilityMatch = card.RulesData.Abilities.Any(a => a.Name.ToLowerInvariant().Contains(q) || a.Text.ToLowerInvariant().Contains(q));
                    if (!nameMatch && !abilityMatch) {
                        visible = false;
                    }
                }

                card.Visible = visible;
            }

            _cardsContainer.ResumeLayout(true);
        }
    }
}
