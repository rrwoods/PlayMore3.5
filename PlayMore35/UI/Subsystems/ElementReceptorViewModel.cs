using System;
using System.Collections.Generic;
using System.Linq;
using PlayMore35.Global;
using PlayMore35.Subsystems;

namespace PlayMore35.UI.Subsystems {
    public class ElementReceptorViewModel {
        private SubsystemElement? _selectedElement;
        private bool _isActiveSelecting;
        private IReadOnlyList<ValidationWarning> _warnings = Array.Empty<ValidationWarning>();

        public event EventHandler? StateChanged;
        public event EventHandler? Activated;
        public event EventHandler? ElementCleared;
        public event Action<SubsystemElement?>? ElementDelivered;

        public SubsystemElement? SelectedElement {
            get => _selectedElement;
            set {
                if (_selectedElement != value) {
                    _selectedElement = value;
                    OnStateChanged();
                }
            }
        }

        public bool IsActiveSelecting {
            get => _isActiveSelecting;
            set {
                if (_isActiveSelecting != value) {
                    _isActiveSelecting = value;
                    OnStateChanged();
                }
            }
        }

        public string PlaceholderText => "(Choose element...)";

        public string DisplayText => _selectedElement != null ? _selectedElement.Name : PlaceholderText;

        public ReceptorState State {
            get {
                if (_isActiveSelecting) return ReceptorState.ActiveSelecting;
                return _selectedElement != null ? ReceptorState.Made : ReceptorState.Unmade;
            }
        }

        public bool IsClearButtonVisible => _selectedElement != null;

        public IReadOnlyList<ValidationWarning> Warnings => _warnings;

        public bool IsWarningBadgeVisible => Warnings.Count > 0;

        public WarningKind? WorstWarningKind {
            get {
                if (Warnings.Count == 0) return null;
                return Warnings.Any(w => w.Kind == WarningKind.Unmet)
                    ? WarningKind.Unmet
                    : WarningKind.ItemDependent;
            }
        }

        public string WarningBadgeText {
            get {
                if (!IsWarningBadgeVisible) return "";
                return WorstWarningKind == WarningKind.ItemDependent ? "🛈" : "⚠";
            }
        }

        public string WarningTooltip {
            get {
                if (!IsWarningBadgeVisible) return "";
                string message = string.Join("\n", Warnings.Select(w => w.Message));
                if (WorstWarningKind == WarningKind.ItemDependent) {
                    return !string.IsNullOrEmpty(message) ? $"{message}\n(Satisfied by currently equipped items)" : "";
                }
                return message;
            }
        }

        public void SetWarnings(IEnumerable<ValidationWarning>? warnings) {
            var list = warnings != null ? warnings.ToList() : new List<ValidationWarning>();
            _warnings = list.AsReadOnly();
            OnStateChanged();
        }

        public void ClearWarning() {
            SetWarnings(null);
        }

        public void Activate() {
            Activated?.Invoke(this, EventArgs.Empty);
        }

        public void Clear() {
            _selectedElement = null;
            OnStateChanged();
            ElementCleared?.Invoke(this, EventArgs.Empty);
            ElementDelivered?.Invoke(null);
        }

        protected virtual void OnStateChanged() {
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
