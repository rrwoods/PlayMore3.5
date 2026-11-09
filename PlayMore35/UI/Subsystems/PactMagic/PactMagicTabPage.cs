using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using PlayMore35.CharacterChoices;
using PlayMore35.Global;
using PlayMore35.Subsystems;

namespace PlayMore35.UI.Subsystems.PactMagic {
    public class PactMagicTabPage : TabPage {
        private readonly ISubsystemUIHost _host;
        private readonly Panel _leftColumn;
        private readonly Panel _pickerHost;
        private readonly PactMagicElementPicker _picker;

        private readonly Label _lblEmptyPrompt;
        private readonly Panel _pnlDetailsContent;
        private readonly Label _lblDetailsTitle;
        private readonly Label _lblDetailsSign;
        private readonly Label _lblDetailsInfluence;
        private readonly Label _lblDetailsAbilities;

        public PactMagicTabPage(ISubsystemUIHost host)
            : this(host, new PactMagicElementPicker(host.Session, new ElementPickerContext {
                ElementType = "vestige",
                WarningProvider = elem => host.Session.ValidateProgressionChoice(
                    new ProgressionChoiceContext(ProgressionChoiceDomain.Subsystem, elem.Name, targetSource: "pact_magic"))
            })) {
        }

        public PactMagicTabPage(ISubsystemUIHost host, PactMagicElementPicker picker) {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _picker = picker ?? throw new ArgumentNullException(nameof(picker));

            Text = "Vestiges";
            BackColor = SystemColors.Control;
            Size = new Size(1281, 623);

            _leftColumn = new Panel {
                Dock = DockStyle.Left,
                Width = 360,
                BackColor = Color.White,
                Padding = new Padding(12),
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = true
            };
            _leftColumn.Resize += (s, e) => UpdateDetailsLabelsWidth();

            _lblEmptyPrompt = new Label {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(120, 120, 120),
                Text = "Select a vestige to view sign, influence, and granted abilities.",
                Visible = true
            };

            _pnlDetailsContent = new Panel {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                Visible = false
            };

            _lblDetailsTitle = new Label {
                Dock = DockStyle.Top,
                AutoSize = true,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 69, 120),
                Padding = new Padding(0, 0, 0, 8)
            };

            _lblDetailsSign = new Label {
                Dock = DockStyle.Top,
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(68, 68, 68),
                Padding = new Padding(0, 0, 0, 8)
            };

            _lblDetailsInfluence = new Label {
                Dock = DockStyle.Top,
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(68, 68, 68),
                Padding = new Padding(0, 0, 0, 8)
            };

            _lblDetailsAbilities = new Label {
                Dock = DockStyle.Top,
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(51, 51, 51)
            };

            _pnlDetailsContent.Controls.Add(_lblDetailsAbilities);
            _pnlDetailsContent.Controls.Add(_lblDetailsInfluence);
            _pnlDetailsContent.Controls.Add(_lblDetailsSign);
            _pnlDetailsContent.Controls.Add(_lblDetailsTitle);

            _leftColumn.Controls.Add(_lblEmptyPrompt);
            _leftColumn.Controls.Add(_pnlDetailsContent);

            _pickerHost = new Panel {
                Dock = DockStyle.Fill,
                Padding = new Padding(6)
            };

            _picker.VestigeSelected += OnVestigeSelected;
            _picker.VestigeBindRequested += OnVestigeBindRequested;
            _picker.VestigeUnbindRequested += OnVestigeUnbindRequested;
            _pickerHost.Controls.Add(_picker);

            Controls.Add(_pickerHost);
            Controls.Add(_leftColumn);

            Enter += (s, e) => RefreshTab();

            RefreshTab();
        }

        public void RefreshTab() {
            _picker.RefreshMaxVestigeLevel();
            _picker.RefreshBindingState();

            if (_picker.ViewModel.SelectedCard != null) {
                ShowVestigeDetails(_picker.ViewModel.SelectedCard);
            } else {
                _pnlDetailsContent.Visible = false;
                _lblEmptyPrompt.Visible = true;
            }
        }

        private void OnVestigeSelected(SubsystemElement? vestige) {
            if (vestige != null) {
                ShowVestigeDetails(vestige);
            } else {
                _pnlDetailsContent.Visible = false;
                _lblEmptyPrompt.Visible = true;
            }
        }

        public void ShowVestigeDetails(SubsystemElement vestige) {
            var card = _picker.ViewModel.FindCard(vestige.Name);
            if (card != null) {
                ShowVestigeDetails(card);
            }
        }

        public void ShowVestigeDetails(VestigeCardViewModel card) {
            _lblEmptyPrompt.Visible = false;
            _pnlDetailsContent.Visible = true;

            UpdateDetailsLabelsWidth();
            _lblDetailsTitle.Text = card.DetailsTitle;
            _lblDetailsSign.Text = card.DetailsSignText;
            _lblDetailsInfluence.Text = card.DetailsInfluenceText;
            _lblDetailsAbilities.Text = card.DetailsAbilitiesText;
        }

        private void UpdateDetailsLabelsWidth() {
            int targetWidth = Math.Max(200, _leftColumn.ClientSize.Width - _leftColumn.Padding.Horizontal - 8);
            _lblDetailsTitle.MaximumSize = new Size(targetWidth, 0);
            _lblDetailsSign.MaximumSize = new Size(targetWidth, 0);
            _lblDetailsInfluence.MaximumSize = new Size(targetWidth, 0);
            _lblDetailsAbilities.MaximumSize = new Size(targetWidth, 0);
        }

        private void OnVestigeBindRequested(SubsystemElement vestige, string pactState) {
            var session = _host.Session;
            var engine = session.GetSubsystem("pact_magic");
            var group = engine?.GetSlotGroup("vestiges");

            int capacity = group?.Capacity ?? session.GetStatistic("number of vestiges")?.Value ?? 0;

            // Find first available slot index
            int targetSlotIndex = -1;
            for (int i = 0; i < capacity; i++) {
                var slot = engine?.GetSlot("vestiges", i);
                if (slot == null || string.IsNullOrEmpty(slot.Element) || slot.State == "empty") {
                    targetSlotIndex = i;
                    break;
                }
            }

            if (targetSlotIndex < 0) {
                // At capacity, cannot bind
                return;
            }

            string verb = string.Equals(pactState, "poor_pact", StringComparison.OrdinalIgnoreCase) ? "bind_poor_pact" : "bind_good_pact";

            session.PerformAction(new SheetAction {
                Name = $"Bind {vestige.Name} slot {targetSlotIndex}",
                Subsystem = "pact_magic",
                SlotGroup = "vestiges",
                SlotIndex = targetSlotIndex,
                Element = vestige.Name,
                Verb = verb,
                Source = engine?.GetActiveSources().FirstOrDefault()
            });

            _host.RequestRecalculate();
            _host.RequestEffectsRefresh();
            _host.RequestTimelineRefresh();

            _picker.RefreshBindingState();
            _picker.SelectVestige(vestige.Name);
        }

        private void OnVestigeUnbindRequested(SubsystemElement vestige) {
            var session = _host.Session;
            var engine = session.GetSubsystem("pact_magic");
            var slots = engine?.GetSlots("vestiges") ?? Array.Empty<SubsystemSlot>();

            int targetIndex = -1;
            for (int i = 0; i < slots.Count; i++) {
                if (string.Equals(slots[i].Element, vestige.Name, StringComparison.OrdinalIgnoreCase) && slots[i].State != "empty") {
                    targetIndex = i;
                    break;
                }
            }

            if (targetIndex < 0) return;

            session.PerformAction(new SheetAction {
                Name = $"Unbind {vestige.Name} slot {targetIndex}",
                Subsystem = "pact_magic",
                SlotGroup = "vestiges",
                SlotIndex = targetIndex,
                Verb = "unbind"
            });

            _host.RequestRecalculate();
            _host.RequestEffectsRefresh();
            _host.RequestTimelineRefresh();

            _picker.RefreshBindingState();
        }
    }
}
