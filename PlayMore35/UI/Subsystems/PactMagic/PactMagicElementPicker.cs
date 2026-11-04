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

        private FlowLayoutPanel _pinnedContainer = null!;
        private Panel _toolbarPanel = null!;
        private TextBox _searchBox = null!;
        private CheckBox _hideUnavailableCheckBox = null!;
        private ComboBox _cboLevelFilter = null!;
        private FlowLayoutPanel _cardsContainer = null!;

        private VestigeCardControl? _selectedCard;
        private int _capacity = 1;

        public event Action<SubsystemElement?>? ElementDelivered;
        public event Action<IReadOnlyList<SubsystemElement>>? ElementsDelivered;
        public event Action<SubsystemElement, string>? VestigeBindRequested;
        public event Action<SubsystemElement>? VestigeUnbindRequested;
        public event Action<SubsystemElement?>? VestigeSelected;

        public Control Control => this;
        public IReadOnlyList<VestigeCardControl> Cards => _cards;
        public int VisibleCardsCount => _cards.Count(c => c.Visible && !c.IsBound);
        public FlowLayoutPanel PinnedContainer => _pinnedContainer;
        public FlowLayoutPanel CardsContainer => _cardsContainer;
        public VestigeCardControl? SelectedCard => _selectedCard;

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

        public PactMagicElementPicker(ICharacterSession session, ElementPickerContext context) {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _context = context ?? throw new ArgumentNullException(nameof(context));

            if (session.Catalog.TryGetSubsystem("pact_magic", out var pactDef)) {
                _allVestiges = pactDef.Elements.Values.OrderBy(v => v.Name).ToList();
            } else {
                _allVestiges = new List<SubsystemElement>();
            }

            InitializeComponent();
            PopulateCards();
            RefreshMaxVestigeLevel();
            RefreshBindingState();
        }

        public void RefreshMaxVestigeLevel() {
            var engine = _session.GetSubsystem("pact_magic");
            var group = engine?.GetSlotGroup("vestiges");
            _capacity = group?.Capacity ?? _session.GetStatistic("number of vestiges")?.Value ?? 0;

            foreach (var card in _cards) {
                var warnings = _context.WarningProvider != null
                    ? _context.WarningProvider(card.Vestige)
                    : Array.Empty<ValidationWarning>();
                card.Warnings = warnings ?? Array.Empty<ValidationWarning>();
            }
            ApplyFilter();
        }

        public void RefreshBindingState() {
            bool isProgression = _context.AcquisitionLevel > 0;

            _pinnedContainer.SuspendLayout();
            _cardsContainer.SuspendLayout();

            _pinnedContainer.AutoScrollPosition = Point.Empty;
            _pinnedContainer.AutoScroll = false;
            _pinnedContainer.Controls.Clear();

            if (isProgression) {
                foreach (var card in _cards) {
                    card.IsProgressionMode = true;
                    card.IsBound = false;
                    card.PactState = null;
                    card.IsAtMaxCapacity = false;

                    if (!_cardsContainer.Controls.Contains(card)) {
                        _cardsContainer.Controls.Add(card);
                    }
                }
                _pinnedContainer.Visible = false;
            } else {
                var engine = _session.GetSubsystem("pact_magic");
                var slots = engine?.GetSlots("vestiges") ?? Array.Empty<SubsystemSlot>();

                int boundCount = slots.Count(s => !string.IsNullOrEmpty(s.Element) && s.State != "empty");
                bool atMaxCapacity = boundCount >= _capacity;

                foreach (var card in _cards) {
                    card.IsProgressionMode = false;
                    var boundSlot = slots.FirstOrDefault(s => string.Equals(s.Element, card.Vestige.Name, StringComparison.OrdinalIgnoreCase) && s.State != "empty");
                    bool isBound = (boundSlot != null);

                    card.IsBound = isBound;
                    card.PactState = boundSlot?.State;
                    card.IsAtMaxCapacity = atMaxCapacity;

                    if (isBound) {
                        if (!_pinnedContainer.Controls.Contains(card)) {
                            _pinnedContainer.Controls.Add(card);
                        }
                    } else {
                        if (!_cardsContainer.Controls.Contains(card)) {
                            _cardsContainer.Controls.Add(card);
                        }
                    }
                }

                _pinnedContainer.Visible = (_pinnedContainer.Controls.Count > 0);
            }

            _pinnedContainer.AutoScroll = true;
            _pinnedContainer.ResumeLayout(true);
            _cardsContainer.ResumeLayout(true);

            ApplyFilter();
        }

        public void DeliverVestigeByName(string name) {
            var card = _cards.FirstOrDefault(c => string.Equals(c.Vestige.Name, name, StringComparison.OrdinalIgnoreCase));
            if (card != null) {
                SelectCard(card);
            }
        }

        public void SelectVestige(string name) {
            var card = _cards.FirstOrDefault(c => string.Equals(c.Vestige.Name, name, StringComparison.OrdinalIgnoreCase));
            if (card != null) {
                SelectCard(card);
            }
        }

        private void InitializeComponent() {
            SuspendLayout();
            Dock = DockStyle.Fill;
            BackColor = SystemColors.Control;

            _pinnedContainer = new FlowLayoutPanel {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.FromArgb(240, 244, 248),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(6),
                WrapContents = false,
                AutoScroll = true,
                Visible = false
            };

            _toolbarPanel = new Panel {
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
                Font = new Font("Segoe UI", 8.5f),
                Checked = true
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

            _toolbarPanel.Controls.Add(_cboLevelFilter);
            _toolbarPanel.Controls.Add(_hideUnavailableCheckBox);
            _toolbarPanel.Controls.Add(_searchBox);

            _cardsContainer = new FlowLayoutPanel {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(250, 250, 250),
                Padding = new Padding(8),
                WrapContents = true
            };

            // Order of controls controls docking hierarchy:
            // Top docking resolves bottom-to-top or top-to-bottom based on add order.
            // DockStyle.Fill must be added first or last depending on layout.
            // Controls added: fill first, then toolbars top.
            Controls.Add(_cardsContainer);
            Controls.Add(_toolbarPanel);
            Controls.Add(_pinnedContainer);

            ResumeLayout(false);
        }

        private void PopulateCards() {
            _cards.Clear();

            foreach (var elem in _allVestiges) {
                var card = new VestigeCardControl(elem);
                card.IsProgressionMode = _context.AcquisitionLevel > 0;

                card.CardClicked += (s, e) => SelectCard(card);
                card.GoodPactClicked += (s, e) => OnBindClicked(card, "good_pact");
                card.PoorPactClicked += (s, e) => OnBindClicked(card, "poor_pact");
                card.UnbindClicked += (s, e) => OnUnbindClicked(card);

                _cards.Add(card);
            }
        }

        private void SelectCard(VestigeCardControl card) {
            foreach (var c in _cards) {
                c.IsSelected = (c == card);
            }
            _selectedCard = card;

            ElementDelivered?.Invoke(card.Vestige);
            ElementsDelivered?.Invoke(new[] { card.Vestige });
            VestigeSelected?.Invoke(card.Vestige);
        }

        private void OnBindClicked(VestigeCardControl card, string pactState) {
            VestigeBindRequested?.Invoke(card.Vestige, pactState);
        }

        private void OnUnbindClicked(VestigeCardControl card) {
            VestigeUnbindRequested?.Invoke(card.Vestige);
        }

        private void ApplyFilter() {
            _cardsContainer.SuspendLayout();

            string q = _searchBox.Text.Trim().ToLowerInvariant();
            int selectedLevel = _cboLevelFilter.SelectedIndex;

            foreach (var card in _cards) {
                if (card.IsBound && _context.AcquisitionLevel <= 0) {
                    // Pinned cards remain visible in pinned container
                    card.Visible = true;
                    continue;
                }

                bool visible = true;
                int vestigeLevel = card.Vestige.GetProperty<int>("level");

                if (_hideUnavailableCheckBox.Checked && card.IsInvalid) {
                    visible = false;
                }

                if (selectedLevel > 0 && vestigeLevel != selectedLevel) {
                    visible = false;
                }

                if (visible && !string.IsNullOrEmpty(q)) {
                    bool nameMatch = card.Vestige.Name.ToLowerInvariant().Contains(q);
                    bool abilityMatch = card.Vestige.GetEffects("abilities").Any(a =>
                        a.Name.ToLowerInvariant().Contains(q) ||
                        a.Text.Any(t => t.Description != null && t.Description.ToLowerInvariant().Contains(q)));
                    string signText = card.Vestige.GetProperty<string>("sign");
                    bool signMatch = signText.ToLowerInvariant().Contains(q);
                    var influenceTexts = card.Vestige.GetEffects("influence").SelectMany(eff => eff.Text).Select(t => t.Description ?? "");
                    bool influenceMatch = influenceTexts.Any(t => t.ToLowerInvariant().Contains(q));
                    if (!nameMatch && !abilityMatch && !signMatch && !influenceMatch) {
                        visible = false;
                    }
                }

                card.Visible = visible;
            }

            _cardsContainer.ResumeLayout(true);
        }
    }
}
