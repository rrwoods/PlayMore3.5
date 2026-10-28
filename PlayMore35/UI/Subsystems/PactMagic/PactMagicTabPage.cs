using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using PlayMore35.Global;
using PlayMore35.Subsystems;

namespace PlayMore35.UI.Subsystems.PactMagic {
    public class PactMagicTabPage : TabPage {
        private readonly ISubsystemUIHost _host;
        private readonly Panel _leftColumn;
        private readonly Panel _pickerHost;
        private readonly FlowLayoutPanel _slotsContainer;
        private readonly List<VestigeSlotCard> _slotCards = new List<VestigeSlotCard>();
        private readonly PactMagicElementPicker _picker;

        private int _activeSlotIndex = 0;

        public IReadOnlyList<VestigeSlotCard> SlotCards => _slotCards;
        public int SlotsColumnWidth => _leftColumn.Width;
        public int ActiveSlotIndex => _activeSlotIndex;
        public PactMagicElementPicker Picker => _picker;

        public PactMagicTabPage(ISubsystemUIHost host) {
            _host = host ?? throw new ArgumentNullException(nameof(host));

            Text = "Vestiges";
            BackColor = SystemColors.Control;
            Size = new Size(1281, 623);

            _leftColumn = new Panel {
                Dock = DockStyle.Left,
                Width = 360,
                BackColor = Color.FromArgb(250, 250, 250),
                Padding = new Padding(10),
                BorderStyle = BorderStyle.FixedSingle
            };

            _slotsContainer = new FlowLayoutPanel {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(0, 4, 0, 0)
            };

            _leftColumn.Controls.Add(_slotsContainer);

            _pickerHost = new Panel {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            _picker = new PactMagicElementPicker(_host.Session, new ElementPickerContext { ElementType = "vestige" });
            _picker.ElementDelivered += OnElementDelivered;
            _pickerHost.Controls.Add(_picker);

            Controls.Add(_pickerHost);
            Controls.Add(_leftColumn);

            Enter += (s, e) => RefreshTab();

            RefreshTab();
        }

        public void RefreshTab() {
            var session = _host.Session;
            var engine = session.Subsystems.Get("pact_magic") as SubsystemEngine;

            int capacity = engine?.GetSlotGroup("vestiges")?.Capacity ?? session.GetStatistic("number of vestiges")?.Value ?? 0;
            if (capacity <= 0) capacity = 1;
            if (capacity > 4) capacity = 4;

            int maxLevel = session.GetStatistic("maximum vestige level")?.Value ?? 8;
            if (maxLevel <= 0) maxLevel = 8;

            _slotsContainer.SuspendLayout();
            _slotsContainer.Controls.Clear();
            _slotCards.Clear();

            for (int i = 0; i < capacity; i++) {
                int slotIndex = i;
                var card = new VestigeSlotCard(slotIndex);
                card.SlotActivated += (s, e) => SelectSlot(slotIndex);
                card.UnbindClicked += (s, e) => UnbindSlot(slotIndex);
                card.PactStateChanged += pactState => SetSlotPactState(slotIndex, pactState);

                var slot = engine?.GetSlot("vestiges", slotIndex);
                if (slot != null && !string.IsNullOrEmpty(slot.Element) && slot.State != "empty") {
                    var elem = engine?.GetElement(slot.Element);
                    card.SetBoundVestige(elem, slot.State, maxLevel);
                } else {
                    card.SetBoundVestige(null, "good_pact", maxLevel);
                }

                _slotCards.Add(card);
                _slotsContainer.Controls.Add(card);
            }

            if (_activeSlotIndex >= capacity) {
                _activeSlotIndex = 0;
            }

            UpdateActiveSlotVisuals();
            _slotsContainer.ResumeLayout(true);

            _picker.SetTargetSlot(_activeSlotIndex);
            _picker.RefreshMaxVestigeLevel();
        }

        private void SelectSlot(int index) {
            _activeSlotIndex = index;
            UpdateActiveSlotVisuals();
            _picker.SetTargetSlot(_activeSlotIndex);

            if (index >= 0 && index < _slotCards.Count) {
                var selectedVestige = _slotCards[index].Receptor.SelectedElement;
                if (selectedVestige != null) {
                    _picker.SelectVestigeDetail(selectedVestige.Name);
                }
            }
        }

        private void UpdateActiveSlotVisuals() {
            for (int i = 0; i < _slotCards.Count; i++) {
                _slotCards[i].SetActiveSelecting(i == _activeSlotIndex);
            }
        }

        private void OnElementDelivered(SubsystemElement? vestige) {
            if (_activeSlotIndex < 0 || _activeSlotIndex >= _slotCards.Count) return;

            if (vestige != null) {
                BindSlot(_activeSlotIndex, vestige);
            } else {
                UnbindSlot(_activeSlotIndex);
            }
        }

        private void BindSlot(int slotIndex, SubsystemElement vestige) {
            var session = _host.Session;
            var engine = session.Subsystems.Get("pact_magic") as SubsystemEngine;

            string verb = (_slotCards[slotIndex].PoorPactRadio.Checked) ? "bind_poor_pact" : "bind_good_pact";
            string state = (_slotCards[slotIndex].PoorPactRadio.Checked) ? "poor_pact" : "good_pact";

            session.PerformAction(new SheetAction {
                Name = $"Bind {vestige.Name} slot {slotIndex}",
                Subsystem = "pact_magic",
                SlotGroup = "vestiges",
                SlotIndex = slotIndex,
                Element = vestige.Name,
                Verb = verb,
                Source = engine?.GetActiveSources().FirstOrDefault()
            });

            int maxLevel = session.GetStatistic("maximum vestige level")?.Value ?? 8;
            if (maxLevel <= 0) maxLevel = 8;
            _slotCards[slotIndex].SetBoundVestige(vestige, state, maxLevel);

            _host.RequestRecalculate();
            _host.RequestEffectsRefresh();
            _host.RequestTimelineRefresh();
            _picker.UpdateCardBoundIndicators();
        }

        private void UnbindSlot(int slotIndex) {
            var session = _host.Session;
            var engine = session.Subsystems.Get("pact_magic") as SubsystemEngine;

            session.PerformAction(new SheetAction {
                Name = $"Unbind vestige slot {slotIndex}",
                Subsystem = "pact_magic",
                SlotGroup = "vestiges",
                SlotIndex = slotIndex,
                Verb = "unbind"
            });

            _slotCards[slotIndex].SetBoundVestige(null, "good_pact");

            _host.RequestRecalculate();
            _host.RequestEffectsRefresh();
            _host.RequestTimelineRefresh();
            _picker.UpdateCardBoundIndicators();
        }

        private void SetSlotPactState(int slotIndex, string newState) {
            var session = _host.Session;
            var engine = session.Subsystems.Get("pact_magic") as SubsystemEngine;
            var slot = engine?.GetSlot("vestiges", slotIndex);

            if (slot != null && !string.IsNullOrEmpty(slot.Element) && slot.State != "empty") {
                engine?.SetSlotState("vestiges", slotIndex, newState);
                _host.RequestRecalculate();
                _host.RequestEffectsRefresh();
                _host.RequestTimelineRefresh();
            }
        }
    }
}
