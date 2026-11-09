using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using PlayMore35.Global;
using PlayMore35.Subsystems;

namespace PlayMore35.UI.Subsystems.PactMagic {
    public class PactMagicElementPicker : UserControl, ISubsystemElementPicker {
        public PactMagicPickerViewModel ViewModel { get; }
        private readonly List<VestigeCardControl> _cards = new List<VestigeCardControl>();

        private FlowLayoutPanel _pinnedContainer = null!;
        private Panel _toolbarPanel = null!;
        private TextBox _searchBox = null!;
        private CheckBox _hideUnavailableCheckBox = null!;
        private ComboBox _cboLevelFilter = null!;
        private FlowLayoutPanel _cardsContainer = null!;
        private bool _isSyncing;

        public event Action<SubsystemElement?>? ElementDelivered {
            add => ViewModel.ElementDelivered += value;
            remove => ViewModel.ElementDelivered -= value;
        }

        public event Action<IReadOnlyList<SubsystemElement>>? ElementsDelivered {
            add => ViewModel.ElementsDelivered += value;
            remove => ViewModel.ElementsDelivered -= value;
        }

        public event Action<SubsystemElement, string>? VestigeBindRequested {
            add => ViewModel.VestigeBindRequested += value;
            remove => ViewModel.VestigeBindRequested -= value;
        }

        public event Action<SubsystemElement>? VestigeUnbindRequested {
            add => ViewModel.VestigeUnbindRequested += value;
            remove => ViewModel.VestigeUnbindRequested -= value;
        }

        public event Action<SubsystemElement?>? VestigeSelected {
            add => ViewModel.VestigeSelected += value;
            remove => ViewModel.VestigeSelected -= value;
        }

        public Control Control => this;

        public PactMagicElementPicker(ICharacterSession session, ElementPickerContext context)
            : this(new PactMagicPickerViewModel(session, context)) {
        }

        public PactMagicElementPicker(PactMagicPickerViewModel viewModel) {
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

            InitializeComponent();
            PopulateCards();

            ViewModel.StateChanged += (s, e) => SyncFromViewModel();
            SyncFromViewModel();
        }

        public void RefreshMaxVestigeLevel() => ViewModel.RefreshMaxVestigeLevel();

        public void RefreshBindingState() => ViewModel.RefreshBindingState();

        public void SelectVestige(string name) => ViewModel.SelectVestige(name);

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
            _searchBox.TextChanged += (s, e) => {
                if (_isSyncing) return;
                ViewModel.SearchText = _searchBox.Text;
            };

            _hideUnavailableCheckBox = new CheckBox {
                Location = new Point(216, 8),
                Text = "Hide unavailable",
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f),
                Checked = ViewModel.HideUnavailable
            };
            _hideUnavailableCheckBox.CheckedChanged += (s, e) => {
                if (_isSyncing) return;
                ViewModel.HideUnavailable = _hideUnavailableCheckBox.Checked;
            };

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
            _cboLevelFilter.SelectedIndex = ViewModel.SelectedLevelFilterIndex;
            _cboLevelFilter.SelectedIndexChanged += (s, e) => {
                if (_isSyncing) return;
                ViewModel.SelectedLevelFilterIndex = _cboLevelFilter.SelectedIndex;
            };

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

            Controls.Add(_cardsContainer);
            Controls.Add(_toolbarPanel);
            Controls.Add(_pinnedContainer);

            ResumeLayout(false);
        }

        private void PopulateCards() {
            _cards.Clear();
            _pinnedContainer.Controls.Clear();
            _cardsContainer.Controls.Clear();

            foreach (var cardVm in ViewModel.Cards) {
                var cardControl = new VestigeCardControl(cardVm);
                _cards.Add(cardControl);
                _cardsContainer.Controls.Add(cardControl);
            }
        }

        private void SyncFromViewModel() {
            _isSyncing = true;
            try {
                if (_searchBox.Text != ViewModel.SearchText) {
                    _searchBox.Text = ViewModel.SearchText;
                }
                if (_hideUnavailableCheckBox.Checked != ViewModel.HideUnavailable) {
                    _hideUnavailableCheckBox.Checked = ViewModel.HideUnavailable;
                }
                if (_cboLevelFilter.SelectedIndex != ViewModel.SelectedLevelFilterIndex) {
                    _cboLevelFilter.SelectedIndex = ViewModel.SelectedLevelFilterIndex;
                }

                _pinnedContainer.SuspendLayout();
                _cardsContainer.SuspendLayout();

                _pinnedContainer.AutoScrollPosition = Point.Empty;
                _pinnedContainer.AutoScroll = false;

                _pinnedContainer.Visible = ViewModel.IsPinnedContainerVisible;

                foreach (var cardControl in _cards) {
                    var cardVm = cardControl.ViewModel;

                    if (cardVm.IsBound && ViewModel.Context.AcquisitionLevel <= 0) {
                        if (!_pinnedContainer.Controls.Contains(cardControl)) {
                            _pinnedContainer.Controls.Add(cardControl);
                        }
                    } else {
                        if (!_cardsContainer.Controls.Contains(cardControl)) {
                            _cardsContainer.Controls.Add(cardControl);
                        }
                    }
                }

                _pinnedContainer.AutoScroll = true;
                _pinnedContainer.ResumeLayout(true);
                _cardsContainer.ResumeLayout(true);
            } finally {
                _isSyncing = false;
            }
        }
    }
}
