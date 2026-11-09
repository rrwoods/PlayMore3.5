using System;
using System.Collections.Generic;
using System.Linq;
using PlayMore35.Global;
using PlayMore35.Subsystems;

namespace PlayMore35.UI.Subsystems.PactMagic {
    [AuthorizedDomainApi("Headless presentation model for pact magic element picker")]
    public class PactMagicPickerViewModel {
        private readonly ICharacterSession _session;
        private readonly ElementPickerContext _context;
        private readonly List<SubsystemElement> _allVestiges;
        private readonly List<VestigeCardViewModel> _cards = new List<VestigeCardViewModel>();

        private string _searchText = "";
        private bool _hideUnavailable = true;
        private int _selectedLevelFilterIndex = 0;
        private VestigeCardViewModel? _selectedCard;
        private int _capacity = 1;

        public event Action<SubsystemElement?>? ElementDelivered;
        public event Action<IReadOnlyList<SubsystemElement>>? ElementsDelivered;
        public event Action<SubsystemElement, string>? VestigeBindRequested;
        public event Action<SubsystemElement>? VestigeUnbindRequested;
        public event Action<SubsystemElement?>? VestigeSelected;
        public event EventHandler? StateChanged;

        public ICharacterSession Session => _session;
        public ElementPickerContext Context => _context;
        public IReadOnlyList<VestigeCardViewModel> Cards => _cards;
        public VestigeCardViewModel? SelectedCard => _selectedCard;
        public int Capacity => _capacity;

        public PactMagicPickerViewModel(ICharacterSession session, ElementPickerContext context, IEnumerable<SubsystemElement>? vestiges = null) {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _context = context ?? throw new ArgumentNullException(nameof(context));

            if (vestiges != null) {
                _allVestiges = vestiges.OrderBy(v => v.Name).ToList();
            } else if (session.Catalog != null && session.Catalog.TryGetSubsystem("pact_magic", out var pactDef)) {
                _allVestiges = pactDef.Elements.Values.OrderBy(v => v.Name).ToList();
            } else {
                _allVestiges = new List<SubsystemElement>();
            }

            PopulateCards();
            RefreshMaxVestigeLevel();
            RefreshBindingState();
        }

        public string SearchText {
            get => _searchText;
            set {
                var val = value ?? "";
                if (_searchText != val) {
                    _searchText = val;
                    ApplyFilter();
                }
            }
        }

        public bool HideUnavailable {
            get => _hideUnavailable;
            set {
                if (_hideUnavailable != value) {
                    _hideUnavailable = value;
                    ApplyFilter();
                }
            }
        }

        public int SelectedLevelFilterIndex {
            get => _selectedLevelFilterIndex;
            set {
                if (_selectedLevelFilterIndex != value) {
                    _selectedLevelFilterIndex = value;
                    ApplyFilter();
                }
            }
        }

        public IReadOnlyList<VestigeCardViewModel> PinnedCards =>
            _cards.Where(c => c.IsBound && _context.AcquisitionLevel <= 0).ToList();

        public bool IsPinnedContainerVisible => PinnedCards.Count > 0;

        public int VisibleCardsCount =>
            _cards.Count(c => c.IsVisible && !c.IsBound);

        private void PopulateCards() {
            _cards.Clear();
            bool isProgression = _context.AcquisitionLevel > 0;

            foreach (var elem in _allVestiges) {
                var card = new VestigeCardViewModel(elem) {
                    IsProgressionMode = isProgression
                };

                card.BindRequested += (c, pactState) => RequestBind(c, pactState);
                card.UnbindRequested += c => RequestUnbind(c);
                card.Selected += c => SelectCard(c);

                _cards.Add(card);
            }
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

            if (isProgression) {
                foreach (var card in _cards) {
                    card.IsProgressionMode = true;
                    card.IsBound = false;
                    card.PactState = null;
                    card.IsAtMaxCapacity = false;
                }
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
                }
            }

            ApplyFilter();
        }

        public void SelectCard(VestigeCardViewModel? card) {
            foreach (var c in _cards) {
                c.IsSelected = (c == card);
            }
            _selectedCard = card;

            if (card != null) {
                ElementDelivered?.Invoke(card.Vestige);
                ElementsDelivered?.Invoke(new[] { card.Vestige });
                VestigeSelected?.Invoke(card.Vestige);
            } else {
                ElementDelivered?.Invoke(null);
                ElementsDelivered?.Invoke(Array.Empty<SubsystemElement>());
                VestigeSelected?.Invoke(null);
            }

            OnStateChanged();
        }

        public void SelectVestige(string name) {
            var card = _cards.FirstOrDefault(c => string.Equals(c.Vestige.Name, name, StringComparison.OrdinalIgnoreCase));
            if (card != null) {
                SelectCard(card);
            }
        }

        public VestigeCardViewModel? FindCard(string name) =>
            _cards.FirstOrDefault(c => string.Equals(c.Vestige.Name, name, StringComparison.OrdinalIgnoreCase));

        public void RequestBind(VestigeCardViewModel card, string pactState) {
            VestigeBindRequested?.Invoke(card.Vestige, pactState);
        }

        public void RequestUnbind(VestigeCardViewModel card) {
            VestigeUnbindRequested?.Invoke(card.Vestige);
        }

        private void ApplyFilter() {
            string q = _searchText.Trim().ToLowerInvariant();
            int selectedLevel = _selectedLevelFilterIndex;

            foreach (var card in _cards) {
                if (card.IsBound && _context.AcquisitionLevel <= 0) {
                    card.IsVisible = true;
                    continue;
                }

                bool visible = true;
                int vestigeLevel = card.Vestige.GetProperty<int>("level");

                if (_hideUnavailable && card.IsInvalid) {
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

                card.IsVisible = visible;
            }

            OnStateChanged();
        }

        protected virtual void OnStateChanged() {
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
