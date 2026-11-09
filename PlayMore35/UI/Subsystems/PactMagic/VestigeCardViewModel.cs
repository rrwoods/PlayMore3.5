using System;
using System.Collections.Generic;
using System.Linq;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Subsystems;

namespace PlayMore35.UI.Subsystems.PactMagic {
    public enum VestigeCardStatusKind {
        Available,
        Exceeds,
        Ineligible,
        ItemDependent,
        GoodPact,
        PoorPact
    }

    public class VestigeCardViewModel {
        private readonly SubsystemElement _vestige;
        private bool _isSelected;
        private IReadOnlyList<ValidationWarning> _warnings = Array.Empty<ValidationWarning>();
        private bool _isProgressionMode;
        private bool _isBound;
        private string? _pactState;
        private bool _isAtMaxCapacity;
        private bool _isVisible = true;

        public event EventHandler? StateChanged;
        public event EventHandler? CardClicked;
        public event EventHandler? GoodPactClicked;
        public event EventHandler? PoorPactClicked;
        public event EventHandler? UnbindClicked;

        public event Action<VestigeCardViewModel, string>? BindRequested;
        public event Action<VestigeCardViewModel>? UnbindRequested;
        public event Action<VestigeCardViewModel>? Selected;

        public SubsystemElement Vestige => _vestige;

        public VestigeCardViewModel(SubsystemElement vestige) {
            _vestige = vestige ?? throw new ArgumentNullException(nameof(vestige));
        }

        public bool IsSelected {
            get => _isSelected;
            set {
                if (_isSelected != value) {
                    _isSelected = value;
                    OnStateChanged();
                }
            }
        }

        public IReadOnlyList<ValidationWarning> Warnings {
            get => _warnings;
            set {
                _warnings = value ?? Array.Empty<ValidationWarning>();
                OnStateChanged();
            }
        }

        public bool IsProgressionMode {
            get => _isProgressionMode;
            set {
                if (_isProgressionMode != value) {
                    _isProgressionMode = value;
                    OnStateChanged();
                }
            }
        }

        public bool IsInvalid => _warnings.Any(w => w.Kind == WarningKind.Unmet);

        public bool IsBound {
            get => _isBound;
            set {
                if (_isBound != value) {
                    _isBound = value;
                    OnStateChanged();
                }
            }
        }

        public string? PactState {
            get => _pactState;
            set {
                if (_pactState != value) {
                    _pactState = value;
                    OnStateChanged();
                }
            }
        }

        public bool IsAtMaxCapacity {
            get => _isAtMaxCapacity;
            set {
                if (_isAtMaxCapacity != value) {
                    _isAtMaxCapacity = value;
                    OnStateChanged();
                }
            }
        }

        public bool IsVisible {
            get => _isVisible;
            set {
                if (_isVisible != value) {
                    _isVisible = value;
                    OnStateChanged();
                }
            }
        }

        public VestigeCardStatusKind StatusKind {
            get {
                if (_isBound && !_isProgressionMode) {
                    return string.Equals(_pactState, "poor_pact", StringComparison.OrdinalIgnoreCase)
                        ? VestigeCardStatusKind.PoorPact
                        : VestigeCardStatusKind.GoodPact;
                }

                var unmet = _warnings.Where(w => w.Kind == WarningKind.Unmet).ToList();
                if (unmet.Count > 0) {
                    var worst = unmet.FirstOrDefault(w => w.Category == ValidationCategory.LevelGate) ?? unmet[0];
                    return worst.Category == ValidationCategory.LevelGate
                        ? VestigeCardStatusKind.Exceeds
                        : VestigeCardStatusKind.Ineligible;
                }

                if (_warnings.Any(w => w.Kind == WarningKind.ItemDependent)) {
                    return VestigeCardStatusKind.ItemDependent;
                }

                return VestigeCardStatusKind.Available;
            }
        }

        public string StatusText {
            get {
                switch (StatusKind) {
                    case VestigeCardStatusKind.PoorPact:
                        return "Poor Pact";
                    case VestigeCardStatusKind.GoodPact:
                        return "Good Pact";
                    case VestigeCardStatusKind.Exceeds:
                        return "⚠ Exceeds";
                    case VestigeCardStatusKind.Ineligible:
                        return "⚠ Ineligible";
                    case VestigeCardStatusKind.ItemDependent:
                        return "🛈 Item-Dep";
                    case VestigeCardStatusKind.Available:
                    default:
                        return "Available";
                }
            }
        }

        public string StatusTooltip {
            get {
                if (_isBound && !_isProgressionMode) {
                    return "";
                }

                var unmet = _warnings.Where(w => w.Kind == WarningKind.Unmet).ToList();
                var itemDep = _warnings.Where(w => w.Kind == WarningKind.ItemDependent).ToList();

                if (unmet.Count > 0) {
                    return string.Join("\n", _warnings.Select(FormatWarningTooltip));
                }

                if (itemDep.Count > 0) {
                    string message = string.Join("\n", itemDep.Select(w => w.Message));
                    return $"{message}\n(Satisfied by currently equipped items)";
                }

                return "";
            }
        }

        private static string FormatWarningTooltip(ValidationWarning warning) {
            return warning.Kind == WarningKind.ItemDependent
                ? $"{warning.Message} (Satisfied by currently equipped items)"
                : warning.Message;
        }

        public bool IsUnbindVisible => _isBound && !_isProgressionMode;
        public bool IsUnbindEnabled => IsUnbindVisible;

        public bool IsGoodPactVisible => !_isBound && !_isProgressionMode && _isSelected && !IsInvalid && !_isAtMaxCapacity;
        public bool IsGoodPactEnabled => IsGoodPactVisible;

        public bool IsPoorPactVisible => !_isBound && !_isProgressionMode && _isSelected && !IsInvalid && !_isAtMaxCapacity;
        public bool IsPoorPactEnabled => IsPoorPactVisible;

        public bool IsCapacityNoticeVisible => !_isBound && !_isProgressionMode && _isSelected && !IsInvalid && _isAtMaxCapacity;
        public string CapacityNoticeText => "At max vestige capacity";

        public string CardTitleText {
            get {
                int level = _vestige.GetProperty<int>("level");
                int bindingDc = _vestige.GetProperty<int>("binding DC");
                return $"{_vestige.Name} ({level}{GetOrdinal(level)} • DC {bindingDc})";
            }
        }

        public string CardSignText => $"\"{_vestige.GetProperty<string>("sign")}\"";

        public string CardAbilitiesExcerpt => FormatAbilitiesExcerpt(_vestige.GetEffects("abilities"));

        public string DetailsTitle {
            get {
                int level = _vestige.GetProperty<int>("level");
                int bindingDc = _vestige.GetProperty<int>("binding DC");
                return $"{_vestige.Name} (Level {level}, Binding DC {bindingDc})";
            }
        }

        public string DetailsSignText => $"Sign:\n{_vestige.GetProperty<string>("sign")}";

        public string DetailsInfluenceText {
            get {
                var influenceEffects = _vestige.GetEffects("influence");
                var influenceTexts = influenceEffects
                    .SelectMany(eff => eff.Text)
                    .Select(t => t.Description)
                    .Where(desc => !string.IsNullOrWhiteSpace(desc));
                string influence = string.Join("\n\n", influenceTexts);
                return $"Influence:\n{influence}";
            }
        }

        public string DetailsAbilitiesText {
            get {
                var abilities = _vestige.GetEffects("abilities");
                var distinctAbilities = abilities
                    .GroupBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First());
                var abilitiesFormatted = distinctAbilities.Select(a => {
                    string desc = a.Text.Count > 0 ? a.Text[0].Description : "";
                    string type = a.Text.Count > 0 && !string.IsNullOrEmpty(a.Text[0].Category)
                        ? a.Text[0].Category
                        : "Passive";
                    return string.IsNullOrEmpty(desc) ? $"• {a.Name} ({type})" : $"• {a.Name} ({type}): {desc}";
                });
                return "Granted Abilities:\n" + string.Join("\n\n", abilitiesFormatted);
            }
        }

        public void BindGoodPact() {
            GoodPactClicked?.Invoke(this, EventArgs.Empty);
            BindRequested?.Invoke(this, "good_pact");
        }

        public void BindPoorPact() {
            PoorPactClicked?.Invoke(this, EventArgs.Empty);
            BindRequested?.Invoke(this, "poor_pact");
        }

        public void Unbind() {
            UnbindClicked?.Invoke(this, EventArgs.Empty);
            UnbindRequested?.Invoke(this);
        }

        public void Select() {
            CardClicked?.Invoke(this, EventArgs.Empty);
            Selected?.Invoke(this);
        }

        protected virtual void OnStateChanged() {
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        private static string FormatAbilitiesExcerpt(IReadOnlyList<Effect> abilities) {
            if (abilities == null || abilities.Count == 0) return "";
            var distinctNames = abilities.Select(a => a.Name).Distinct(StringComparer.OrdinalIgnoreCase);
            return string.Join("\n", distinctNames.Select(name => $"• {name}"));
        }

        private static string GetOrdinal(int n) {
            if (n == 1) return "st";
            if (n == 2) return "nd";
            if (n == 3) return "rd";
            return "th";
        }
    }
}
