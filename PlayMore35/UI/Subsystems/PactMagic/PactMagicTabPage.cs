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
        private readonly PactMagicElementPicker _picker;

        private readonly Label _lblEmptyPrompt;
        private readonly Panel _pnlDetailsContent;
        private readonly Label _lblDetailsTitle;
        private readonly Label _lblDetailsSign;
        private readonly Label _lblDetailsInfluence;
        private readonly Label _lblDetailsAbilities;

        public int SlotsColumnWidth => _leftColumn.Width;
        public PactMagicElementPicker Picker => _picker;
        public string DetailsHeader => _lblDetailsTitle.Text;
        public string DetailsSignText => _lblDetailsSign.Text;
        public string DetailsInfluenceText => _lblDetailsInfluence.Text;
        public string DetailsAbilitiesText => _lblDetailsAbilities.Text;
        public bool IsEmptyPromptVisible => _lblEmptyPrompt.Visible;

        public PactMagicTabPage(ISubsystemUIHost host) {
            _host = host ?? throw new ArgumentNullException(nameof(host));

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

            _picker = new PactMagicElementPicker(_host.Session, new ElementPickerContext { ElementType = "vestige" });
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

            if (_picker.SelectedCard != null) {
                ShowVestigeDetails(_picker.SelectedCard.Vestige);
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
            _lblEmptyPrompt.Visible = false;
            _pnlDetailsContent.Visible = true;

            int level = vestige.GetProperty<int>("level");
            int bindingDc = vestige.GetProperty<int>("binding DC");
            string sign = vestige.GetProperty<string>("sign");

            var influenceEffects = vestige.GetEffects("influence");
            var influenceTexts = influenceEffects
                .SelectMany(eff => eff.Text)
                .Select(t => t.Description)
                .Where(desc => !string.IsNullOrWhiteSpace(desc));
            string influence = string.Join("\n\n", influenceTexts);

            UpdateDetailsLabelsWidth();
            _lblDetailsTitle.Text = $"{vestige.Name} (Level {level}, Binding DC {bindingDc})";
            _lblDetailsSign.Text = $"Sign:\n{sign}";
            _lblDetailsInfluence.Text = $"Influence:\n{influence}";

            var abilities = vestige.GetEffects("abilities");
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
            _lblDetailsAbilities.Text = "Granted Abilities:\n" + string.Join("\n\n", abilitiesFormatted);
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
