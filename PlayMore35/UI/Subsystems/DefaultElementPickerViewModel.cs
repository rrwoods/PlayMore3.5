using System;
using System.Collections.Generic;
using System.Linq;
using PlayMore35.Global;
using PlayMore35.Subsystems;

namespace PlayMore35.UI.Subsystems {
    public class DefaultElementPickerItem {
        public SubsystemElement? Element { get; }
        public string DisplayName => Element != null ? Element.Name : "(None)";
        public IReadOnlyList<ValidationWarning> Warnings { get; }
        public bool HasWarning => Warnings.Count > 0;

        public WarningKind? WorstWarningKind {
            get {
                if (Warnings.Count == 0) return null;
                return Warnings.Any(w => w.Kind == WarningKind.Unmet)
                    ? WarningKind.Unmet
                    : WarningKind.ItemDependent;
            }
        }

        public string? BadgeText {
            get {
                if (!HasWarning) return null;
                return WorstWarningKind == WarningKind.Unmet ? "⚠" : "🛈";
            }
        }

        public DefaultElementPickerItem(SubsystemElement? element, IEnumerable<ValidationWarning>? warnings = null) {
            Element = element;
            Warnings = warnings != null ? warnings.ToList().AsReadOnly() : (IReadOnlyList<ValidationWarning>)Array.Empty<ValidationWarning>();
        }

        public override string ToString() => DisplayName;
    }

    public class DefaultElementPickerViewModel {
        private readonly ICharacterSession _session;
        private readonly ElementPickerContext _context;
        private readonly SubsystemDefinition? _definition;
        private readonly List<SubsystemElement> _allElements;

        private string _searchText = "";
        private bool _hideInvalidChoices = false;
        private bool _isDetailsExpanded = true;
        private int _selectedIndex = -1;
        private readonly List<DefaultElementPickerItem> _filteredItems = new List<DefaultElementPickerItem>();

        public event Action<SubsystemElement?>? ElementDelivered;
        public event Action<IReadOnlyList<SubsystemElement>>? ElementsDelivered;
        public event EventHandler? StateChanged;

        public DefaultElementPickerViewModel(ICharacterSession session, ElementPickerContext context, IEnumerable<SubsystemElement>? elements = null) {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _context = context ?? throw new ArgumentNullException(nameof(context));

            if (!string.IsNullOrEmpty(context.ElementType)) {
                if (session.Catalog != null && session.Catalog.TryGetSubsystem(context.ElementType!, out var subDef)) {
                    _definition = subDef;
                } else if (session.Subsystems != null) {
                    var engine = session.GetSubsystem(context.ElementType!) ??
                                 session.Subsystems.FirstOrDefault(e =>
                                     e.Definition.SlotGroups.Values.Any(g => string.Equals(g.Filter, context.ElementType, StringComparison.OrdinalIgnoreCase)) ||
                                     e.Definition.Elements.Values.Any(el => el.MatchesFilter(context.ElementType, e.Definition)));
                    if (engine != null) {
                        _definition = engine.Definition;
                    }
                }
            }

            if (_definition == null && elements != null) {
                var firstElem = elements.FirstOrDefault();
                if (firstElem != null) {
                    if (session.Catalog?.Subsystems != null) {
                        _definition = session.Catalog.Subsystems.Values.FirstOrDefault(d => d.Elements.ContainsKey(firstElem.Name));
                    }
                    if (_definition == null && session.Subsystems != null) {
                        _definition = session.Subsystems.Select(e => e.Definition).FirstOrDefault(d => d.Elements.ContainsKey(firstElem.Name));
                    }
                }
            }

            if (elements != null) {
                _allElements = new List<SubsystemElement>(elements);
            } else if (_definition != null) {
                _allElements = new List<SubsystemElement>(_definition.Elements.Values);
            } else {
                _allElements = new List<SubsystemElement>();
            }

            ApplyFilter();
        }

        public string SearchText {
            get => _searchText;
            set {
                var normalized = value ?? "";
                if (_searchText != normalized) {
                    _searchText = normalized;
                    ApplyFilter();
                    OnStateChanged();
                }
            }
        }

        public bool HideInvalidChoices {
            get => _hideInvalidChoices;
            set {
                if (_hideInvalidChoices != value) {
                    _hideInvalidChoices = value;
                    ApplyFilter();
                    OnStateChanged();
                }
            }
        }

        public bool IsDetailsExpanded {
            get => _isDetailsExpanded;
            set {
                if (_isDetailsExpanded != value) {
                    _isDetailsExpanded = value;
                    OnStateChanged();
                }
            }
        }

        public string DetailsToggleText => _isDetailsExpanded ? "▼ Hide Details" : "▲ Show Details";

        public IReadOnlyList<DefaultElementPickerItem> FilteredItems => _filteredItems;

        public int SelectedIndex {
            get => _selectedIndex;
            set => SetSelectedIndex(value);
        }

        public string DetailsText {
            get {
                if (_selectedIndex < 0) {
                    return "Select an element to view details.";
                }
                var item = _filteredItems[_selectedIndex];
                if (item.Element != null) {
                    var desc = item.Element.GetProperty<string>("description", "");
                    var warningText = item.Warnings.Count > 0
                        ? "\n\n" + string.Join("\n", item.Warnings.Select(FormatWarning))
                        : "";
                    return $"{item.Element.Name}\n{desc}{warningText}";
                }
                return "(None selected)";
            }
        }

        public void ToggleDetailsPane() {
            IsDetailsExpanded = !IsDetailsExpanded;
        }

        private void SetSelectedIndex(int index) {
            if (_selectedIndex == index) {
                return;
            }
            if (index < -1 || index >= _filteredItems.Count) {
                index = -1;
            }
            _selectedIndex = index;
            OnStateChanged();
            if (index >= 0 && index < _filteredItems.Count) {
                var item = _filteredItems[index];
                if (item.Element != null) {
                    ElementDelivered?.Invoke(item.Element);
                    ElementsDelivered?.Invoke(new[] { item.Element });
                } else {
                    ElementDelivered?.Invoke(null);
                    ElementsDelivered?.Invoke(Array.Empty<SubsystemElement>());
                }
            }
        }

        private void ApplyFilter() {
            _filteredItems.Clear();

            // Always add (None) option at the top
            _filteredItems.Add(new DefaultElementPickerItem(null, Array.Empty<ValidationWarning>()));

            var searchText = _searchText.Trim();
            var filter = _context.Filter;
            foreach (var elem in _allElements) {
                if (!elem.MatchesFilter(filter, _definition)) {
                    continue;
                }

                if (!string.IsNullOrEmpty(searchText) && elem.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) < 0) {
                    continue;
                }

                IReadOnlyList<ValidationWarning> warnings = Array.Empty<ValidationWarning>();
                if (_context.WarningProvider != null) {
                    warnings = _context.WarningProvider(elem) ?? Array.Empty<ValidationWarning>();
                }

                if (_hideInvalidChoices && warnings.Any(w => w.Kind == WarningKind.Unmet)) {
                    continue;
                }

                _filteredItems.Add(new DefaultElementPickerItem(elem, warnings));
            }

            _selectedIndex = -1;
        }

        private static string FormatWarning(ValidationWarning warning) {
            return warning.Kind == WarningKind.ItemDependent
                ? $"[Item-Dependent: {warning.Message} (satisfied by equipped items)]"
                : $"[Warning: {warning.Message}]";
        }

        protected virtual void OnStateChanged() {
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
