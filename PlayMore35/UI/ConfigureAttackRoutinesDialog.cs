using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using PlayMore35.Items;

namespace PlayMore35 {
    public class ConfigureAttackRoutinesDialog : Form {
        private readonly AttackRoutinePreferences preferences;
        private readonly Dictionary<RoutineCategory, CheckBox> checkBoxes = new Dictionary<RoutineCategory, CheckBox>();

        public ConfigureAttackRoutinesDialog() : this(new AttackRoutinePreferences()) { }

        public ConfigureAttackRoutinesDialog(AttackRoutinePreferences preferences) {
            this.preferences = preferences ?? new AttackRoutinePreferences();
            InitializeControls();
        }

        public bool GetCategoryChecked(RoutineCategory category) {
            return checkBoxes.TryGetValue(category, out var chk) && chk.Checked;
        }

        public void SetCategoryChecked(RoutineCategory category, bool isChecked) {
            if (checkBoxes.TryGetValue(category, out var chk)) {
                chk.Checked = isChecked;
            }
        }

        public void ResetToDefaults() {
            foreach (var kvp in checkBoxes) {
                kvp.Value.Checked = AttackRoutinePreferences.GetDefaultVisibility(kvp.Key);
            }
        }

        public void SavePreferences() {
            foreach (var kvp in checkBoxes) {
                preferences.SetCategoryVisible(kvp.Key, kvp.Value.Checked);
            }
        }

        public static string GetCategoryDisplayName(RoutineCategory category) {
            switch (category) {
                case RoutineCategory.PrimarySolo:
                    return "Primary / Single Weapon";
                case RoutineCategory.TwoWeaponFighting:
                    return "Two-Weapon Fighting";
                case RoutineCategory.DoubleWeapon:
                    return "Double Weapon";
                case RoutineCategory.ShieldBashSolo:
                    return "Single Shield Bash";
                case RoutineCategory.OffHandShieldBash:
                    return "Full Attack with Shield Bash";
                case RoutineCategory.ArmorSpikesSolo:
                    return "Armor Spikes (Solo)";
                case RoutineCategory.ArmorSpikesTWF:
                    return "Armor Spikes (Two-Weapon Fighting)";
                default:
                    return category.ToString();
            }
        }

        private void InitializeControls() {
            Text = "Configure Attack Routines";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;

            Label lblInstructions = new Label {
                Text = "Select routine categories to display:",
                Location = new Point(16, 12),
                AutoSize = true
            };
            Controls.Add(lblInstructions);

            int y = 38;
            foreach (RoutineCategory cat in (RoutineCategory[])Enum.GetValues(typeof(RoutineCategory))) {
                CheckBox chk = new CheckBox {
                    Text = GetCategoryDisplayName(cat),
                    Location = new Point(20, y),
                    AutoSize = true,
                    Checked = preferences.IsCategoryVisible(cat)
                };
                checkBoxes[cat] = chk;
                Controls.Add(chk);
                y += 26;
            }

            Button btnDefaults = new Button {
                Text = "Defaults",
                Location = new Point(16, y + 12),
                Size = new Size(75, 26)
            };
            btnDefaults.Click += (s, e) => ResetToDefaults();
            Controls.Add(btnDefaults);

            Button btnOk = new Button {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(155, y + 12),
                Size = new Size(75, 26)
            };
            btnOk.Click += (s, e) => {
                SavePreferences();
                Close();
            };
            Controls.Add(btnOk);

            Button btnCancel = new Button {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(236, y + 12),
                Size = new Size(75, 26)
            };
            btnCancel.Click += (s, e) => Close();
            Controls.Add(btnCancel);

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            ClientSize = new Size(330, y + 50);
        }
    }
}
