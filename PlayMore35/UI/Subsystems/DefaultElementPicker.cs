using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using PlayMore35.Global;
using PlayMore35.Subsystems;

namespace PlayMore35.UI.Subsystems {
    public class DefaultElementPicker : UserControl, ISubsystemElementPicker {
        public DefaultElementPickerViewModel ViewModel { get; }

        private TextBox _searchBox;
        private CheckBox _hideInvalidCheckBox;
        private ListBox _listBox;
        private Panel _detailsPanel;
        private Label _lblDetails;
        private Button _btnToggleDetails;
        private bool _isSyncing;

        public event Action<SubsystemElement?>? ElementDelivered {
            add => ViewModel.ElementDelivered += value;
            remove => ViewModel.ElementDelivered -= value;
        }

        public event Action<IReadOnlyList<SubsystemElement>>? ElementsDelivered {
            add => ViewModel.ElementsDelivered += value;
            remove => ViewModel.ElementsDelivered -= value;
        }

        public Control Control => this;

        public DefaultElementPicker(ICharacterSession session, ElementPickerContext context, IEnumerable<SubsystemElement>? elements = null)
            : this(new DefaultElementPickerViewModel(session, context, elements)) {
        }

        public DefaultElementPicker(DefaultElementPickerViewModel viewModel) {
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

            InitializeComponent();

            ViewModel.StateChanged += (s, e) => SyncFromViewModel();
            SyncFromViewModel();
        }

        private void InitializeComponent() {
            SuspendLayout();

            var topPanel = new Panel {
                Dock = DockStyle.Top,
                Height = 55
            };

            _searchBox = new TextBox {
                Location = new Point(4, 4),
                Width = 200,
                Font = new Font(Font.FontFamily, 9f)
            };
            _searchBox.TextChanged += (s, e) => {
                if (_isSyncing) return;
                ViewModel.SearchText = _searchBox.Text;
            };

            _hideInvalidCheckBox = new CheckBox {
                Text = "Hide invalid choices",
                Location = new Point(4, 30),
                AutoSize = true
            };
            _hideInvalidCheckBox.CheckedChanged += (s, e) => {
                if (_isSyncing) return;
                ViewModel.HideInvalidChoices = _hideInvalidCheckBox.Checked;
            };

            _btnToggleDetails = new Button {
                Text = "▼ Hide Details",
                Location = new Point(160, 27),
                Size = new Size(100, 24),
                UseVisualStyleBackColor = true
            };
            _btnToggleDetails.Click += (s, e) => ViewModel.ToggleDetailsPane();

            topPanel.Controls.Add(_searchBox);
            topPanel.Controls.Add(_hideInvalidCheckBox);
            topPanel.Controls.Add(_btnToggleDetails);

            _detailsPanel = new Panel {
                Dock = DockStyle.Bottom,
                Height = 80,
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = true
            };

            _lblDetails = new Label {
                Dock = DockStyle.Fill,
                AutoSize = false,
                Text = "Select an element to view details.",
                Padding = new Padding(4)
            };
            _detailsPanel.Controls.Add(_lblDetails);

            _listBox = new ListBox {
                Dock = DockStyle.Fill,
                IntegralHeight = false,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 20
            };
            _listBox.DrawItem += ListBox_DrawItem;
            _listBox.SelectedIndexChanged += (s, e) => {
                if (_isSyncing) return;
                ViewModel.SelectedIndex = _listBox.SelectedIndex;
            };

            Controls.Add(_listBox);
            Controls.Add(_detailsPanel);
            Controls.Add(topPanel);

            ResumeLayout(false);
        }

        public (string BadgeText, Color BadgeColor)? GetItemBadge(int index) {
            if (index < 0 || index >= ViewModel.FilteredItems.Count) return null;
            var item = ViewModel.FilteredItems[index];
            if (item.BadgeText == null) return null;
            return (item.BadgeText, item.WorstWarningKind == WarningKind.Unmet
                ? Color.DarkGoldenrod
                : Color.DodgerBlue);
        }

        private void SyncFromViewModel() {
            if (_isSyncing) return;
            _isSyncing = true;
            try {
                if (_searchBox.Text != ViewModel.SearchText) {
                    _searchBox.Text = ViewModel.SearchText;
                }
                if (_hideInvalidCheckBox.Checked != ViewModel.HideInvalidChoices) {
                    _hideInvalidCheckBox.Checked = ViewModel.HideInvalidChoices;
                }
                _detailsPanel.Visible = ViewModel.IsDetailsExpanded;
                _btnToggleDetails.Text = ViewModel.DetailsToggleText;
                _lblDetails.Text = ViewModel.DetailsText;

                _listBox.BeginUpdate();
                _listBox.Items.Clear();
                foreach (var item in ViewModel.FilteredItems) {
                    _listBox.Items.Add(item);
                }
                _listBox.SelectedIndex = ViewModel.SelectedIndex;
                _listBox.EndUpdate();
            } finally {
                _isSyncing = false;
            }
        }

        private void ListBox_DrawItem(object sender, DrawItemEventArgs e) {
            if (e.Index < 0 || e.Index >= _listBox.Items.Count) return;

            e.DrawBackground();

            var item = (DefaultElementPickerItem)_listBox.Items[e.Index];
            var g = e.Graphics;
            var bounds = e.Bounds;

            string text = item.DisplayName;
            Font font = item.Element == null ? new Font(e.Font, FontStyle.Italic) : e.Font;
            Brush brush = (e.State & DrawItemState.Selected) != 0 ? SystemBrushes.HighlightText : SystemBrushes.ControlText;

            int textX = bounds.Left + 4;
            int textY = bounds.Top + (bounds.Height - (int)g.MeasureString(text, font).Height) / 2;

            try {
                g.DrawString(text, font, brush, textX, textY);
            } finally {
                if (item.Element == null) {
                    font.Dispose();
                }
            }

            var badge = GetItemBadge(e.Index);
            if (badge != null) {
                using (var badgeFont = new Font("Segoe UI", 9f, FontStyle.Bold))
                using (var badgeBrush = new SolidBrush(badge.Value.BadgeColor)) {
                    var badgeSize = g.MeasureString(badge.Value.BadgeText, badgeFont);
                    float badgeX = bounds.Right - badgeSize.Width - 4;
                    float badgeY = bounds.Top + (bounds.Height - badgeSize.Height) / 2;
                    g.DrawString(badge.Value.BadgeText, badgeFont, badgeBrush, badgeX, badgeY);
                }
            }

            e.DrawFocusRectangle();
        }
    }
}
