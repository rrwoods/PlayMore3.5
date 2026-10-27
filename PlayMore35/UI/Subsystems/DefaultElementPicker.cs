using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using PlayMore35.Global;
using PlayMore35.Subsystems;

namespace PlayMore35.UI.Subsystems {
    public class DefaultElementPicker : UserControl, ISubsystemElementPicker {
        private readonly ICharacterSession _session;
        private readonly ElementPickerContext _context;
        private readonly List<SubsystemElement> _allElements;

        private TextBox _searchBox;
        private CheckBox _hideInvalidCheckBox;
        private ListBox _listBox;
        private Panel _detailsPanel;
        private Label _lblDetails;

        public event Action<SubsystemElement?>? ElementDelivered;
        public event Action<IReadOnlyList<SubsystemElement>>? ElementsDelivered;

        public Control Control => this;

        public DefaultElementPicker(ICharacterSession session, ElementPickerContext context, IEnumerable<SubsystemElement>? elements = null) {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _context = context ?? throw new ArgumentNullException(nameof(context));

            if (elements != null) {
                _allElements = new List<SubsystemElement>(elements);
            } else if (!string.IsNullOrEmpty(context.ElementType)) {
                if (session.Catalog.TryGetSubsystem(context.ElementType!, out var subDef)) {
                    _allElements = new List<SubsystemElement>(subDef.Elements.Values);
                } else {
                    var engine = session.Subsystems.Get(context.ElementType!) ??
                                 session.Subsystems.All.FirstOrDefault(e => e.Definition.Elements.Values.Any(el => string.Equals(el.Type, context.ElementType, StringComparison.OrdinalIgnoreCase)));
                    if (engine != null) {
                        _allElements = new List<SubsystemElement>(engine.Definition.Elements.Values);
                    } else {
                        _allElements = new List<SubsystemElement>();
                    }
                }
            } else {
                _allElements = new List<SubsystemElement>();
            }

            InitializeComponent();
            ApplyFilter();
        }

        public string SearchFilterText {
            get => _searchBox.Text;
            set {
                _searchBox.Text = value;
                ApplyFilter();
            }
        }

        public bool HideInvalidChoices {
            get => _hideInvalidCheckBox.Checked;
            set {
                _hideInvalidCheckBox.Checked = value;
                ApplyFilter();
            }
        }

        public IReadOnlyList<string> GetFilteredElementNames() {
            var names = new List<string>();
            foreach (var item in _listBox.Items) {
                if (item is ListItem listItem) {
                    names.Add(listItem.Element != null ? listItem.Element.Name : "(None)");
                }
            }
            return names;
        }

        public void SelectElementByName(string name) {
            for (int i = 0; i < _listBox.Items.Count; i++) {
                if (_listBox.Items[i] is ListItem item && item.Element != null && string.Equals(item.Element.Name, name, StringComparison.OrdinalIgnoreCase)) {
                    _listBox.SelectedIndex = i;
                    return;
                }
            }
        }

        public void SelectNone() {
            for (int i = 0; i < _listBox.Items.Count; i++) {
                if (_listBox.Items[i] is ListItem item && item.Element == null) {
                    _listBox.SelectedIndex = i;
                    return;
                }
            }
            // If not found in list, deliver null directly
            ElementDelivered?.Invoke(null);
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
            _searchBox.TextChanged += (s, e) => ApplyFilter();

            _hideInvalidCheckBox = new CheckBox {
                Text = "Hide invalid choices",
                Location = new Point(4, 30),
                AutoSize = true
            };
            _hideInvalidCheckBox.CheckedChanged += (s, e) => ApplyFilter();

            topPanel.Controls.Add(_searchBox);
            topPanel.Controls.Add(_hideInvalidCheckBox);

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
            _listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;

            Controls.Add(_listBox);
            Controls.Add(_detailsPanel);
            Controls.Add(topPanel);

            ResumeLayout(false);
        }

        private void ApplyFilter() {
            _listBox.BeginUpdate();
            _listBox.Items.Clear();

            // Always add (None) option at the top
            _listBox.Items.Add(new ListItem(null, Array.Empty<string>()));

            var searchText = _searchBox.Text.Trim();
            var filter = _context.Filter;
            foreach (var elem in _allElements) {
                if (!string.IsNullOrEmpty(filter)) {
                    if (string.Equals(filter, "maneuver", StringComparison.OrdinalIgnoreCase)) {
                        if (string.Equals(elem.Type, "Stance", StringComparison.OrdinalIgnoreCase)) {
                            continue;
                        }
                    } else if (!string.Equals(elem.Type, filter, StringComparison.OrdinalIgnoreCase)) {
                        continue;
                    }
                }

                if (!string.IsNullOrEmpty(searchText) && elem.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) < 0) {
                    continue;
                }

                IReadOnlyList<string> warnings = Array.Empty<string>();
                if (_context.WarningProvider != null) {
                    warnings = _context.WarningProvider(elem) ?? Array.Empty<string>();
                }

                if (_hideInvalidCheckBox.Checked && warnings.Count > 0) {
                    continue;
                }

                _listBox.Items.Add(new ListItem(elem, warnings));
            }

            _listBox.EndUpdate();
        }

        private void ListBox_DrawItem(object sender, DrawItemEventArgs e) {
            if (e.Index < 0 || e.Index >= _listBox.Items.Count) return;

            e.DrawBackground();

            var item = (ListItem)_listBox.Items[e.Index];
            var g = e.Graphics;
            var bounds = e.Bounds;

            string text = item.Element != null ? item.Element.Name : "(None)";
            Font font = item.Element == null ? new Font(e.Font, FontStyle.Italic) : e.Font;
            Brush brush = (e.State & DrawItemState.Selected) != 0 ? SystemBrushes.HighlightText : SystemBrushes.ControlText;

            int textX = bounds.Left + 4;
            int textY = bounds.Top + (bounds.Height - (int)g.MeasureString(text, font).Height) / 2;

            g.DrawString(text, font, brush, textX, textY);

            if (item.Warnings.Count > 0) {
                string badge = "⚠";
                using (var badgeFont = new Font("Segoe UI", 9f, FontStyle.Bold))
                using (var badgeBrush = new SolidBrush(Color.DarkGoldenrod)) {
                    var badgeSize = g.MeasureString(badge, badgeFont);
                    float badgeX = bounds.Right - badgeSize.Width - 4;
                    float badgeY = bounds.Top + (bounds.Height - badgeSize.Height) / 2;
                    g.DrawString(badge, badgeFont, badgeBrush, badgeX, badgeY);
                }
            }

            e.DrawFocusRectangle();
        }

        private void ListBox_SelectedIndexChanged(object sender, EventArgs e) {
            if (_listBox.SelectedItem is ListItem selectedItem) {
                if (selectedItem.Element != null) {
                    var desc = selectedItem.Element.GetProperty<string>("description", "");
                    var warningText = selectedItem.Warnings.Count > 0 ? $"\n\n[Warning: {string.Join("; ", selectedItem.Warnings)}]" : "";
                    _lblDetails.Text = $"{selectedItem.Element.Name}\n{desc}{warningText}";

                    ElementDelivered?.Invoke(selectedItem.Element);
                    ElementsDelivered?.Invoke(new[] { selectedItem.Element });
                } else {
                    _lblDetails.Text = "(None selected)";
                    ElementDelivered?.Invoke(null);
                    ElementsDelivered?.Invoke(Array.Empty<SubsystemElement>());
                }
            }
        }

        private class ListItem {
            public SubsystemElement? Element { get; }
            public IReadOnlyList<string> Warnings { get; }

            public ListItem(SubsystemElement? element, IReadOnlyList<string> warnings) {
                Element = element;
                Warnings = warnings ?? Array.Empty<string>();
            }

            public override string ToString() => Element != null ? Element.Name : "(None)";
        }
    }
}
