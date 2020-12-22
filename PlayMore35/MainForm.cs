using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

using PlayMore35.CharacterChoices;
using PlayMore35.Global;
using PlayMore35.Items;
using static PlayMore35.Properties.Resources;
using Newtonsoft.Json.Linq;

using static PlayMore35.Effects.Constants;

namespace PlayMore35 {
    public partial class MainForm : Form {
        [DllImport("gdi32.dll")]
        private static extern IntPtr AddFontMemResourceEx(IntPtr pbFont, uint cbFont, IntPtr pdv, [In] ref uint pcFonts);
        private PrivateFontCollection fonts = new PrivateFontCollection();
        Font scala;

        private int leftBuffer;
        private int topBuffer;
        private int verticalBufferLarge;
        private int columnWidth;
        private int locationWidthBuffer;
        private int paramBuffer;

        private bool loaded = false;

        private Label name, desc1, desc2, init, senses, listen, spot, evade, languages, condition;
        private List<List<Label>> beforeCombat; // can't initialize right away apparently.

        private Label ac, touch, acEffects, currentHp, maxHp, dr, react, resist, immune, sr, fort, reflex, will;
        private List<List<Label>> enemyTurn;

        private Label speed, movement;
        private List<Label> attacks = new List<Label>();
        private List<Label> damage = new List<Label>();
        private List<Label> criticals = new List<Label>();
        private Label space, reach, bab, grapple, atkOptions, actions, specialActions;
        private List<List<Label>> myTurn;

        private Label str_score, str_mod, str_check;
        private Label dex_score, dex_mod, dex_check;
        private Label con_score, con_mod, con_check;
        private Label int_score, int_mod, int_check;
        private Label wis_score, wis_mod, wis_check;
        private Label cha_score, cha_mod, cha_check;
        private Label sq;
        private List<Label> skills = new List<Label>();
        private Label miscSkills;
        private List<List<Label>> misc;

        private List<Label> slotBoxes;

        private Control dragging = null;
        private Size draggingFrom;
        public Dictionary<Label, ToolTip> toolTips = new Dictionary<Label, ToolTip>();

        private Dictionary<string, ListBox> locationLists = new Dictionary<string, ListBox>();
        private Dictionary<string, CheckBox> locationWeights = new Dictionary<string, CheckBox>();
        private Control draggingItemFrom = null;
        private int numberDragging = 0;
        
        private Controller controller = null;
        private Character character = null;

        public MainForm() {
            InitializeComponent();

            IntPtr fontPtr = Marshal.AllocCoTaskMem(Scala_SansRegular_LF.Length);
            Marshal.Copy(Scala_SansRegular_LF, 0, fontPtr, Scala_SansRegular_LF.Length);
            fonts.AddMemoryFont(fontPtr, Scala_SansRegular_LF.Length);
            uint dummy = 0;
            AddFontMemResourceEx(fontPtr, (uint)Scala_SansRegular_LF.Length, IntPtr.Zero, ref dummy);
            Marshal.FreeCoTaskMem(fontPtr);
            scala = new Font(fonts.Families[0], 9.75F);
        }

        public TabPage StatsTab() {
            return statsTab;
        }

        private (int value, List<string> explanation) GetStatValue(string stat) {
            return controller.Effects.GetStat(stat).GetValue(controller.Effects, controller.EquippedItems);
        }

        private string ValueText(int value, bool plus) {
            return ((plus && value >= 0 ? "+" : "") + value).Replace("-", "–");
        }

        private void ToolTipLabel(Label label, List<string> explanation) {
            toolTips[label].SetToolTip(label, string.Join("\n", explanation));
        }

        private void LabelStat(Label label, string prefix, string stat, string postfix = "", bool plus = true) {
            (int value, List<string> explanation) = GetStatValue(stat);
            string statValue = ValueText(value, plus);
            label.Text = $"{prefix} {statValue}{postfix}";
            ToolTipLabel(label, explanation);
        }

        private void AbilityScore(Label score, Label mod, Label check, string ability) {
            LabelStat(score, ability, ability + " score", "", false);

            var abilityModifier = GetStatValue(ability + " modifier");
            string value = ValueText(abilityModifier.value, true);
            mod.Text = $"({value})";
            ToolTipLabel(mod, abilityModifier.explanation);

            var abilityCheck = GetStatValue(ability + " checks");
            if (abilityCheck.value == abilityModifier.value) {
                check.Hide();
            } else {
                check.Text = $"(check at {ValueText(abilityCheck.value, true)})";
                ToolTipLabel(check, abilityCheck.explanation);
                check.Show();
            }
        }

        private void OneLineText(Label label, Dictionary<string, List<string>> textEffects, string key, string postfix = "") {
            if (textEffects[key].Any()) {
                label.Text = $"{key} {string.Join(", ", textEffects[key])}{postfix}";
            } else {
                label.Text = "";
            }
        }

        private void MultilineText(Label label, Dictionary<string, List<string>> textEffects, string key, bool header = true) {
            if (textEffects[key].Any()) {
                label.Text = (header ? key + "\n" : "") + string.Join("\n", textEffects[key].Select(x => "     " + x));
            } else {
                label.Text = "";
            }
        }

        private void RefreshEffects() {
            Dictionary<string, List<string>> textEffects = controller.Effects.GetTextEffects(controller.EquippedItems);

            name.Text = character.name.ToUpper();

            Dictionary<string, int> classLevels = new Dictionary<string, int>();
            foreach (var @class in character.classLevels) {
                if (!classLevels.ContainsKey(@class)) {
                    classLevels[@class] = 1;
                } else {
                    classLevels[@class]++;
                }
            }
            desc1.Text =
                character.gender + " " +
                character.race + " " +
                string.Join(", ", classLevels.Select(x => x.Key.Replace(" ", "\u00a0") + "\u00a0" + x.Value));
            // I wanted the separator for classes to be "/" like it is in most stat blocks,
            // but I also wanted wrapping line breaks to only appear between classes.  Unfortunately,
            // the zero-width space isn't respected by the wrapping routine for some reason.  So I
            // decided to use a character that looks okay when I put a space after it instead.
            string sizeCategory = SIZE_CATEGORIES[GetStatValue(SIZE_CATEGORY).value + 4];
            desc2.Text = character.alignment + " " + sizeCategory + " " +
                controller.Effects.CreatureType +
                (controller.Effects.SubTypes.Any() ? " (" + string.Join(", ", controller.Effects.SubTypes) + ")" : "");
            MultilineText(languages, textEffects, LANGUAGES);
            LabelStat(init, "Initiative", INITIATIVE);
            MultilineText(senses, textEffects, SENSES);
            LabelStat(listen, "Listen", LISTEN, ","); // ok navi calm down.
            LabelStat(spot, "Spot", SPOT);
            OneLineText(evade, textEffects, EVADE);
            MultilineText(condition, textEffects, CONDITION);

            LabelStat(ac, "AC", AC, ",", false);
            LabelStat(touch, "touch", TOUCH_AC, "", false);
            MultilineText(acEffects, textEffects, AC, false);
            LabelStat(currentHp, "hp", CURRENT_HP, ";", false);
            LabelStat(maxHp, "max", MAX_HP, " (" + character.classLevels.Count() + " HD)", false);
            OneLineText(dr, textEffects, DR);
            if (dr.Text.Any()) {
                maxHp.Text += ";";
            }
            MultilineText(react, textEffects, REACT);
            MultilineText(resist, textEffects, RESIST);
            MultilineText(immune, textEffects, IMMUNE);
            OneLineText(sr, textEffects, SR);
            LabelStat(fort, "Fort", FORT_SAVES, ",");
            LabelStat(reflex, "Ref", REF_SAVES, ",");
            LabelStat(will, "Will", WILL_SAVES);

            LabelStat(speed, "Speed", LAND_SPEED, " ft.", false);
            MultilineText(movement, textEffects, MOVEMENT, false);
            foreach (Label label in attacks) {
                statsTab.Controls.Remove(label);
            }
            attacks.Clear();
            foreach (Label label in damage) {
                statsTab.Controls.Remove(label);
            }
            damage.Clear();
            foreach (Label label in criticals) {
                statsTab.Controls.Remove(label);
            }
            criticals.Clear();
            foreach (Items.Attack attack in controller.GetAttacks()) {
                StringBuilder text = new StringBuilder();
                text.Append(attack.RangeIncrement > 0 ? "Ranged " : "Melee ");
                text.Append(attack.WeaponName + " " + ValueText(attack.AttackBonus.bonus, true));
                foreach (int extra in attack.ExtraAttacks) {
                    text.Append("/" + (attack.AttackBonus.bonus - extra));
                }
                Label attackLabel = new Label().Initialize(this);
                attackLabel.Text = text.ToString();
                ToolTipLabel(attackLabel, attack.AttackBonus.explanation);
                attacks.Add(attackLabel);
                Label damageLabel = new Label().Initialize(this);
                damageLabel.Text = "(" + attack.Damage.dice.ToString() + ")";
                ToolTipLabel(damageLabel, attack.Damage.explanation);
                damage.Add(damageLabel);
                Label critLabel = new Label().Initialize(this);
                critLabel.Text = "(" + attack.ThreatRange + (attack.ThreatRange == 20 ? "" : "–20") + "/"
                    + attack.CriticalDamage.dice.ToString() + ")";
                ToolTipLabel(critLabel, attack.CriticalDamage.explanation);
                criticals.Add(critLabel);
            }
            LabelStat(space, "Space", SPACE, " ft.;", false);
            LabelStat(reach, "Reach", REACH, " ft.", false);
            LabelStat(bab, "Base Atk", BASE_ATTACK_BONUS, ";");
            LabelStat(grapple, "Grp", GRAPPLE);
            MultilineText(atkOptions, textEffects, ATK_OPTIONS);
            MultilineText(specialActions, textEffects, SPECIAL_ACTIONS);
            MultilineText(actions, textEffects, ACTIONS);

            AbilityScore(str_score, str_mod, str_check, "STR");
            AbilityScore(dex_score, dex_mod, dex_check, "DEX");
            AbilityScore(con_score, con_mod, con_check, "CON");
            AbilityScore(int_score, int_mod, int_check, "INT");
            AbilityScore(wis_score, wis_mod, wis_check, "WIS");
            AbilityScore(cha_score, cha_mod, cha_check, "CHA");
            MultilineText(sq, textEffects, SQ);
            foreach (Label label in skills) {
                statsTab.Controls.Remove(label);
            }
            skills.Clear();
            foreach (string skill in SKILLS_LIST) {
                if (controller.Effects.GetStat(skill).Relevant(controller.Effects, controller.EquippedItems)) {
                    Label label = new Label().Initialize(this);
                    LabelStat(label, skill, skill);
                    skills.Add(label);
                }
            }
            MultilineText(miscSkills, textEffects, SKILLS, false);

            loaded = true;
            RepositionLabels();
            RefreshWeight();
        }

        private void RefreshWeight() {
            foreach (var weight in locationWeights) {
                weight.Value.Text = $"Count weight ({controller.WeightAtLocation(weight.Key)} lbs.)";
            }

            lblEquippedWeight.Text = $"Equipped weight: {controller.EquippedItems.EquippedWeight()} lbs.";
            lblTotalWeight.Text = $"Total load: {controller.EquippedItems.TotalWeight()} lbs.";
        }

        private void RepositionColumn(int columnLeft, List<List<Label>> column) {
            int top = topBuffer;
            foreach (List<Label> labels in column) {
                bool anyText = false;
                foreach (Label label in labels) {
                    if (label.Text.Any()) {
                        anyText = true;
                        label.Left = columnLeft;
                        label.Top = top;
                        label.MaximumSize = new Size(columnWidth - 2 * leftBuffer, 1000);
                        top = label.Bottom;
                    }
                }
                if (anyText) {
                    top += verticalBufferLarge;
                }
            }
        }

        private void RepositionRow(Label first, params Label[] rest) {
            int left = first.Right;
            foreach (Label label in rest) {
                if (label.Text.Any()) {
                    label.Top = first.Top;
                    label.Left = left;
                    left = label.Right;
                }
            }
        }

        private void RepositionLabels() {
            if (!loaded) {
                return;
            }
            columnWidth = ClientSize.Width / 4;

            RepositionColumn(leftBuffer, beforeCombat);
            RepositionRow(listen, spot);

            RepositionColumn(leftBuffer + columnWidth, enemyTurn);
            RepositionRow(ac, touch);
            RepositionRow(currentHp, maxHp, dr);
            RepositionRow(fort, reflex, will);

            RepositionColumn(leftBuffer + columnWidth * 2, myTurn);
            RepositionRow(space, reach);
            for (int i = 0; i < attacks.Count(); i++) {
                RepositionRow(attacks[i], damage[i], criticals[i]);
            }
            RepositionRow(bab, grapple);

            RepositionColumn(leftBuffer + columnWidth * 3, misc);
            RepositionRow(str_score, str_mod, str_check);
            RepositionRow(dex_score, dex_mod, dex_check);
            RepositionRow(con_score, con_mod, con_check);
            RepositionRow(int_score, int_mod, int_check);
            RepositionRow(wis_score, wis_mod, wis_check);
            RepositionRow(cha_score, cha_mod, cha_check);
        }


        private void InventoryTab_Paint(object sender, PaintEventArgs e) {
            InitializeInventory();
            inventoryTab.Paint -= InventoryTab_Paint;
        }

        private EventHandler CountWeight_CheckedChanged(string location) {
            return (object sender, EventArgs e) => {
                controller.CountWeight(location, ((CheckBox)sender).Checked);
                RefreshEffects();
            };
        }

        private bool DraggingFromLocation() {
            if (!(draggingItemFrom is ListBox)) {
                return false;
            }
            return draggingItemFrom != lstSlotless;
        }

        // Begin dragging a location
        private void Location_MouseDown(object sender, MouseEventArgs e) {
            draggingFrom = new Size(e.Location);
            dragging = (Control)sender;
            ((Control)sender).BringToFront();
        }

        // Drag a location
        private void Location_MouseMove(object sender, MouseEventArgs e) {
            if (dragging == sender) {
                Point offset = e.Location - draggingFrom;
                ((Control)sender).Left += offset.X;
                ((Control)sender).Top += offset.Y;
            }
        }

        // Drop a location
        private void Location_MouseUp(object sender, MouseEventArgs e) {
            dragging = null;
        }

        // Begin dragging an item from a location
        private void LocationItem_MouseDown(object sender, MouseEventArgs e) {
            ListBox list = (ListBox)sender;
            int index = list.IndexFromPoint(e.Location);
            if (index < 0) {
                return;
            }

            draggingItemFrom = list;
            numberDragging = (e.Button == MouseButtons.Right) ? 10 : 1;
            list.DoDragDrop(list.Items[index], DragDropEffects.Move);
        }

        // Drag an item into a location
        private void Location_DragEnter(object sender, DragEventArgs e) {
            e.Effect = DragDropEffects.Move;
        }

        // Drop an item into a location
        private void Location_DragDrop(object sender, DragEventArgs e) {
            ListBox targetList = (ListBox)sender;
            ItemDrag item = (ItemDrag) e.Data.GetData(typeof(ItemDrag));
            string to = targetList.Parent.Text;

            if (draggingItemFrom is ListBox fromList) {
                if (fromList == lstSlotless) {
                    controller.UnequipSlotlessItem(targetList.Parent.Text, (string)item.item);
                } else {
                    controller.MoveItem(fromList.Parent.Text, to, item.item, numberDragging);
                }
            } else if (draggingItemFrom is Label fromSlot) {
                if (fromSlot == txtPrimaryWeapon) {
                    controller.UnequipWeapon(to, true);
                } else if (fromSlot == txtOffHandWeapon) {
                    controller.UnequipWeapon(to, false);
                } else if (fromSlot == txtArmor) {
                    controller.UndonArmor(to);
                } else if (fromSlot == txtShield) {
                    controller.UnequipShield(to);
                } else {
                    controller.UnequipItem(item.slot, to);
                }
            }

            draggingItemFrom = null;
            RefreshInventory();
        }

        // Begin dragging an equipped weapon
        private void Weapon_MouseDown(object sender, MouseEventArgs e) {
            draggingItemFrom = (Control)sender;
            draggingItemFrom.DoDragDrop(new ItemDrag {
                item = (sender == txtPrimaryWeapon)
                    ? controller.Character.equipped.PrimaryHandWeapon
                    : controller.Character.equipped.OffHandWeapon
            }, DragDropEffects.Move);
        }

        // Drag a weapon into a hand
        private void Weapon_DragEnter(object sender, DragEventArgs e) {
            ItemDrag item = (ItemDrag) e.Data.GetData(typeof(ItemDrag));
            e.Effect = (item.item is WeaponChoice) ? DragDropEffects.Move : DragDropEffects.None;
        }

        // Drop a weapon into a hand
        private void Weapon_DragDrop(object sender, DragEventArgs e) {
            ItemDrag item = (ItemDrag) e.Data.GetData(typeof(ItemDrag));
            if (!(item.item is WeaponChoice)) { 
                return;
            }

            if (
                ((draggingItemFrom == txtPrimaryWeapon) && (sender == txtOffHandWeapon)) ||
                ((draggingItemFrom == txtOffHandWeapon) && (sender == txtPrimaryWeapon))
            ) {
                controller.SwapWeapons();
            } else {
                bool primary = sender == txtPrimaryWeapon;
                controller.EquipWeapon(draggingItemFrom.Parent.Text, primary, (WeaponChoice)item.item);
            }
            draggingItemFrom = null;
            RefreshInventory();
        }

        // Begin dragging donned armor
        private void Armor_MouseDown(object sender, MouseEventArgs e) {
            draggingItemFrom = (Control)sender;
            draggingItemFrom.DoDragDrop(
                new ItemDrag { item = controller.Character.equipped.DonnedArmor },
                DragDropEffects.Move
            );
        }

        // Drag an armor into the armor box
        private void Armor_DragEnter(object sender, DragEventArgs e) {
            ItemDrag item = (ItemDrag)e.Data.GetData(typeof(ItemDrag));
            e.Effect = (item.item is ArmorChoice) ? DragDropEffects.Move : DragDropEffects.None;
        }

        // Drop an armor into the armor box
        private void Armor_DragDrop(object sender, DragEventArgs e) {
            ItemDrag item = (ItemDrag) e.Data.GetData(typeof(ItemDrag));
            if (!(item.item is ArmorChoice) || !DraggingFromLocation()) {
                return;
            }

            controller.DonArmor(draggingItemFrom.Parent.Text, (ArmorChoice)item.item);
            draggingItemFrom = null;
            RefreshInventory();
        }

        // Begin dragging equipped shield
        private void Shield_MouseDown(object sender, MouseEventArgs e) {
            draggingItemFrom = (Control)sender;
            draggingItemFrom.DoDragDrop(
                new ItemDrag { item = controller.Character.equipped.OffHandShield },
                DragDropEffects.Move
            );
        }

        // Drag a shield into the shield box
        private void Shield_DragEnter(object sender, DragEventArgs e) {
            ItemDrag item = (ItemDrag)e.Data.GetData(typeof(ItemDrag));
            e.Effect = (item.item is ArmorChoice armor && armor.IsShield) ? DragDropEffects.Move : DragDropEffects.None;
        }

        // Drop an shield into the shield box
        private void Shield_DragDrop(object sender, DragEventArgs e) {
            ItemDrag item = (ItemDrag)e.Data.GetData(typeof(ItemDrag));
            if (!(item.item is ArmorChoice armor && armor.IsShield) || !DraggingFromLocation()) {
                return;
            }

            controller.EquipShield(draggingItemFrom.Parent.Text, armor);
            draggingItemFrom = null;
            RefreshInventory();
        }

        // Begin dragging an item from a slot
        private void Slot_MouseDown(object sender, MouseEventArgs e) {
            draggingItemFrom = (Control)sender;
            draggingItemFrom.DoDragDrop(
                new ItemDrag { item = draggingItemFrom.Text }, // is this cheating?
                DragDropEffects.Move
            );
        }

        // Drag an item into a slot
        private void Slot_DragEnter(object sender, DragEventArgs e) {
            Label targetBox = (Label)sender;
            ItemDrag item = (ItemDrag) e.Data.GetData(typeof(ItemDrag));
            e.Effect = (item.slot == (Equipped.Slot)targetBox.Tag) ? DragDropEffects.Move : DragDropEffects.None;
        }

        // Drop an item into a slot
        private void Slot_DragDrop(object sender, DragEventArgs e) {
            Label targetBox = (Label)sender;
            Equipped.Slot slot = (Equipped.Slot)(targetBox.Tag);
            ItemDrag itemDrag = (ItemDrag)e.Data.GetData(typeof(ItemDrag));
            if (slot != itemDrag.slot || !DraggingFromLocation()) {
                return;
            }

            string item = (string) (itemDrag.item);
            controller.EquipItem(draggingItemFrom.Parent.Text, slot, item);
            draggingItemFrom = null;
            RefreshInventory();
        }

        // Begin dragging an item from slotless
        private void Slotless_MouseDown(object sender, MouseEventArgs e) {
            ListBox list = (ListBox)sender;
            int index = list.IndexFromPoint(e.Location);
            if (index < 0) {
                return;
            }

            draggingItemFrom = list;
            numberDragging = 1;
            list.DoDragDrop(list.Items[index], DragDropEffects.Move);
        }

        // Drag an item into slotless
        private void Slotless_DragEnter(object sender, DragEventArgs e) {
            ItemDrag item = (ItemDrag) e.Data.GetData(typeof(ItemDrag));
            e.Effect = ((item.slot == Equipped.Slot.SLOTLESS) && (item.item is string))
                ? DragDropEffects.Move
                : DragDropEffects.None;
        }

        // Drop an item into slotless
        private void Slotless_DragDrop(object sender, DragEventArgs e) {
            if (!DraggingFromLocation()) {
                return;
            }

            ItemDrag item = (ItemDrag)e.Data.GetData(typeof(ItemDrag));
            if (!(item.item is string)) {
                return;
            }

            ListBox targetList = (ListBox)sender;
            controller.EquipItem(draggingItemFrom.Parent.Text, Equipped.Slot.SLOTLESS, (string)item.item);
            draggingItemFrom = null;
            RefreshInventory();
        }

        private void RefreshInventory() {
            foreach (var location in controller.Character.inventory) {
                ListLocation(location.Value, locationLists[location.Key]);
            }
            RefreshEquipment();
        }

        private void ListItems<T>(List<T> items, ListBox locationBox) {
            foreach (object item in items) {
                locationBox.Items.Add(new ItemDrag { item = item });
            }
        }

        private void ListItems(Dictionary<string, int> itemQuantities, ListBox locationBox) {
            foreach (var item in itemQuantities) {
                if (item.Value == 0) {
                    continue;
                }
                locationBox.Items.Add(new ItemDrag {
                    item = item.Key,
                    qty = item.Value,
                    slot = controller.SlotForItem(item.Key)
                });
            }
        }

        private void ListLocation(InventoryLocation location, ListBox locationBox) {
            locationBox.Items.Clear();
            ListItems(location.weapons, locationBox);
            ListItems(location.armor, locationBox);
            ListItems(location.shields, locationBox);
            ListItems(location.items, locationBox);
        }

        private void RefreshEquipment() {
            EquippedChoices equipped = controller.Character.equipped;
            txtPrimaryWeapon.Text = equipped.PrimaryHandWeapon?.FullName() ?? "";
            txtOffHandWeapon.Text = equipped.OffHandWeapon?.FullName() ?? "";
            txtArmor.Text = equipped.DonnedArmor?.FullName() ?? "";
            txtShield.Text = equipped.OffHandShield?.FullName() ?? "";

            foreach (Label slotBox in slotBoxes) {
                if (equipped.SlottedItems.ContainsKey((Equipped.Slot)slotBox.Tag)) {
                    slotBox.Text = equipped.SlottedItems[(Equipped.Slot)slotBox.Tag];
                }
            }
            lstSlotless.Items.Clear();
            foreach (Item item in controller.EquippedItems.SlotlessItems) {
                lstSlotless.Items.Add(new ItemDrag { item = item.ItemEffect.Name });
            }
            // XXX: why in the world does this need to be one higher????
            if (lstSlotless.Items.Count > 2) {
                lstSlotless.Height = lstSlotless.ItemHeight * (lstSlotless.Items.Count + 1);
            }
            RefreshEffects();
            RefreshWeight();
        }

        private void InitializeInventory() {
            int locationTop = grpSampleLocation.Top;
            int locationLeft = grpSampleLocation.Left;
            int locationBuffer = grpSampleLocation2.Left - grpSampleLocation.Right;
            locationWidthBuffer = grpSampleLocation.Width - lstSampleItems.Width;
            foreach (var location in controller.Character.inventory) {
                GroupBox locationBox = new GroupBox {
                    Text = location.Key,
                    Top = locationTop,
                    Left = locationLeft,
                    Width = 100,
                    Height = 0,
                    AutoSize = true
                };
                locationBox.MouseDown += new MouseEventHandler(Location_MouseDown);
                locationBox.MouseUp += new MouseEventHandler(Location_MouseUp);
                locationBox.MouseMove += new MouseEventHandler(Location_MouseMove);

                CheckBox countWeight = new CheckBox() {
                    Text = chkWeightCounts.Text,
                    Left = chkWeightCounts.Left,
                    Top = chkWeightCounts.Top,
                    Checked = location.Value.weighs,
                    AutoSize = true
                };
                countWeight.CheckedChanged += CountWeight_CheckedChanged(location.Key);
                locationWeights[location.Key] = countWeight;
                locationBox.Controls.Add(countWeight);

                ListBox locationList = new ListBox() {
                    AllowDrop = true,
                    AutoSize = true,
                    Top = lstSampleItems.Top,
                    Left = lstSampleItems.Left,
                    Height = 1
                };
                locationList.MouseDown += new MouseEventHandler(LocationItem_MouseDown);
                locationList.DragEnter += new DragEventHandler(Location_DragEnter);
                locationList.DragDrop += new DragEventHandler(Location_DragDrop);
                locationLists[location.Key] = locationList;
                locationBox.Controls.Add(locationList);

                ListLocation(location.Value, locationList);
                inventoryTab.Controls.Add(locationBox);
                locationLeft += locationBox.Width + locationBuffer;
            }
            RefreshEquipment();
        }

        private void MainForm_Resize(object sender, EventArgs e) {
            RepositionLabels();
        }

        private void Toggle_Click(object sender, EventArgs e) {
            CheckBox toggle = (CheckBox)sender;
            string flag = toggle.Text;
            controller.Effects.controllableFlags[flag] = toggle.Checked;

            RefreshEffects();
        }

        private void Param_Leave(object sender, EventArgs e) {
            TextBox box = (TextBox)sender;
            if (int.TryParse(box.Text, out int value)) {
                box.BackColor = SystemColors.Window;
                string parameterName = (string)box.Tag;
                controller.Effects.controllableAmounts[parameterName] = value;
                RefreshEffects();
            } else if (box.Text.Equals("")) {
                box.BackColor = SystemColors.Window;
                string parameterName = (string)box.Tag;
                controller.Effects.controllableAmounts[parameterName] = 0;
                RefreshEffects();
            } else {
                box.BackColor = Color.Red;
            }
        }

        private void MainForm_Load(object sender, EventArgs e) {
            Font = scala;

            leftBuffer = lblTopLeft.Left;
            topBuffer = lblTopLeft.Top + 5; // eh.
            verticalBufferLarge = 8;
            paramBuffer = lblParam2.Top - lblParam.Top;

            using (OpenFileDialog dialog = new OpenFileDialog()) {
                dialog.InitialDirectory = "c:\\";
                if (dialog.ShowDialog() != DialogResult.OK) {
                    Application.Exit();
                    return;
                }
                var fileStream = dialog.OpenFile();
                using (StreamReader reader = new StreamReader(fileStream)) {
                    string content = reader.ReadToEnd();
                    JObject json = JObject.Parse(content);
                    character = new Character(json);
                    controller = new Controller(character);
                }
            }

            name = new Label().Initialize(this);
            desc1 = new Label().Initialize(this);
            desc2 = new Label().Initialize(this);
            init = new Label().Initialize(this);
            senses = new Label().Initialize(this);
            listen = new Label().Initialize(this);
            spot = new Label().Initialize(this);
            evade = new Label().Initialize(this);
            languages = new Label().Initialize(this);
            condition = new Label().Initialize(this);
            beforeCombat = new List<List<Label>> {
                new List<Label> { name },
                new List<Label> { desc1 },
                new List<Label> { desc2 },
                new List<Label> { languages },
                new List<Label> { init },
                new List<Label> { senses },
                new List<Label> { listen },
                new List<Label> { evade },
                new List<Label> { condition },
            };

            ac = new Label().Initialize(this);
            touch = new Label().Initialize(this);
            acEffects = new Label().Initialize(this);
            currentHp = new Label().Initialize(this);
            maxHp = new Label().Initialize(this);
            dr = new Label().Initialize(this);
            react = new Label().Initialize(this);
            resist = new Label().Initialize(this);
            immune = new Label().Initialize(this);
            sr = new Label().Initialize(this);
            fort = new Label().Initialize(this);
            reflex = new Label().Initialize(this);
            will = new Label().Initialize(this);
            enemyTurn = new List<List<Label>> {
                new List<Label> { ac, acEffects, currentHp },
                new List<Label> { react, resist, immune },
                new List<Label> { sr },
                new List<Label> { fort }
            };

            speed = new Label().Initialize(this);
            movement = new Label().Initialize(this);
            space = new Label().Initialize(this);
            reach = new Label().Initialize(this);
            bab = new Label().Initialize(this);
            grapple = new Label().Initialize(this);
            atkOptions = new Label().Initialize(this);
            specialActions = new Label().Initialize(this);
            actions = new Label().Initialize(this);
            myTurn = new List<List<Label>> {
                new List<Label> { speed, movement },
                attacks,
                new List<Label> { space },
                new List<Label> { bab },
                new List<Label> { atkOptions },
                new List<Label> { specialActions },
                new List<Label> { actions },
            };

            str_score = new Label().Initialize(this);
            str_mod = new Label().Initialize(this);
            str_check = new Label().Initialize(this);
            dex_score = new Label().Initialize(this);
            dex_mod = new Label().Initialize(this);
            dex_check = new Label().Initialize(this);
            con_score = new Label().Initialize(this);
            con_mod = new Label().Initialize(this);
            con_check = new Label().Initialize(this);
            int_score = new Label().Initialize(this);
            int_mod = new Label().Initialize(this);
            int_check = new Label().Initialize(this);
            wis_score = new Label().Initialize(this);
            wis_mod = new Label().Initialize(this);
            wis_check = new Label().Initialize(this);
            cha_score = new Label().Initialize(this);
            cha_mod = new Label().Initialize(this);
            cha_check = new Label().Initialize(this);
            sq = new Label().Initialize(this);
            miscSkills = new Label().Initialize(this);
            misc = new List<List<Label>> {
                new List<Label> { str_score, dex_score, con_score, int_score, wis_score, cha_score },
                new List<Label> { sq },
                skills,
                new List<Label> { miscSkills }
            };

            int txtTop = txtParam.Top;
            int lblTop = lblParam.Top;
            foreach (string parameter in controller.Effects.controllableAmounts.Keys) {
                TextBox txtNewParam = new TextBox() {
                    Top = txtTop,
                    Left = txtParam.Left,
                    Width = txtParam.Width,
                    TextAlign = HorizontalAlignment.Center,
                    Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                    Tag = parameter
                };
                txtNewParam.Leave += Param_Leave;
                statsTab.Controls.Add(txtNewParam);

                Label lblNewParam = new Label() {
                    Top = lblTop,
                    Left = lblParam.Left,
                    AutoSize = true,
                    Anchor = AnchorStyles.Bottom | AnchorStyles.Left
                };
                lblNewParam.Text = parameter;
                statsTab.Controls.Add(lblNewParam);

                txtTop += paramBuffer;
                lblTop += paramBuffer;
            }

            foreach (string toggle in controller.Effects.controllableFlags.Keys) {
                CheckBox chkNewToggle = new CheckBox() {
                    Top = txtTop,
                    Left = txtParam.Left,
                    Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                    Text = toggle
                };
                chkNewToggle.Click += Toggle_Click;
                statsTab.Controls.Add(chkNewToggle);

                txtTop += paramBuffer;
            }

            txtHead.Tag = Equipped.Slot.HEAD;
            txtFace.Tag = Equipped.Slot.FACE;
            txtNeck.Tag = Equipped.Slot.NECK;
            txtTorso.Tag = Equipped.Slot.TORSO;
            txtBody.Tag = Equipped.Slot.BODY;
            txtWaist.Tag = Equipped.Slot.WAIST;
            txtShoulders.Tag = Equipped.Slot.SHOULDERS;
            txtArms.Tag = Equipped.Slot.ARMS;
            txtHands.Tag = Equipped.Slot.HANDS;
            txtRing1.Tag = Equipped.Slot.RING1;
            txtRing2.Tag = Equipped.Slot.RING2;
            txtFeet.Tag = Equipped.Slot.FEET;
            slotBoxes = new List<Label> {
                txtHead, txtFace, txtNeck, txtTorso, txtBody, txtWaist, txtShoulders, txtArms, txtHands, txtRing1, txtRing2, txtFeet
            };

            RefreshEffects();
        }

        private void SaveCharacter_Click(object sender, EventArgs e) {
            SaveFileDialog saveCharacter = new SaveFileDialog {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                FilterIndex = 1
            };
            if (saveCharacter.ShowDialog() == DialogResult.OK) {
                File.WriteAllText(saveCharacter.FileName, controller.Character.ToJson().ToString());
            }
        }

        class ItemDrag {
            public Equipped.Slot slot = Equipped.Slot.SLOTLESS;
            public object item;
            public int qty = 1;

            public override string ToString() {
                return qty == 1 ? item.ToString() : $"{item} x {qty}";
            }
        }
    }

    static class LabelExtensions {
        public static Label Initialize(this Label label, MainForm form) {
            form.StatsTab().Controls.Add(label);
            label.AutoSize = true;
            form.toolTips[label] = new ToolTip {
                AutoPopDelay = 32767 // maximum value.
            };
            return label;
        }
    }
}
