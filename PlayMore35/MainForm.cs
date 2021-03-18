using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Global;
using PlayMore35.Items;
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
using static PlayMore35.Global.FormUtils;
using static PlayMore35.Effects.Constants;
using static PlayMore35.Properties.Resources;

namespace PlayMore35 {
    public partial class MainForm : Form {
        [DllImport("gdi32.dll")]
        private static extern IntPtr AddFontMemResourceEx(IntPtr pbFont, uint cbFont, IntPtr pdv, [In] ref uint pcFonts);
        private PrivateFontCollection fonts = new PrivateFontCollection();
        Font scala;

        private bool loaded = false;

        private int leftBuffer;
        private int topBuffer;
        private int verticalBufferLarge;
        private int columnWidth;
        private int locationWidthBuffer;
        private int paramBuffer;
        private int transactionBuffer;
        // maximum number of gold transactions to display on inventory tab
        // (this doesn't affect the number that are counted, only the number that are displayed)
        private const int MAX_TRANSACTIONS = 100;

        private List<Label> transactionDescriptions = new List<Label>();
        private List<Label> transactionAmounts = new List<Label>();

        private List<Label> sessionDescriptions = new List<Label>();
        private List<Label> sessionXps = new List<Label>();

        private List<Label> bonusLabels = new List<Label>();
        private List<ComboBox> bonusLanguages = new List<ComboBox>();

        private List<ComboBox> feats = new List<ComboBox>();

        private LevelDetailsDialog levelDetailsDialog;

        private List<ComboBox> increments = new List<ComboBox>();
        private List<ComboBox> levels = new List<ComboBox>();
        private List<Button> levelDetails = new List<Button>();
        private List<TextBox> hpRolls = new List<TextBox>();
        private List<Label> skillNames = new List<Label>();
        private Dictionary<string, List<TextBox>> skillRanks = new Dictionary<string, List<TextBox>>();
        private List<Label> skillPoints = new List<Label>();
        private Dictionary<string, Label> totalSkillRanks = new Dictionary<string, Label>();
        private int levelBuffer, levelUpBuffer;
        private int skillBufferLeft, skillLabelOffset, skillPointsBuffer;

        private List<Panel> vestigePanels = new List<Panel>();
        private List<Label> stateLabels = new List<Label>();
        private List<ListBox> pactStates = new List<ListBox>();
        private List<List<RadioButton>> vestigeChoices = new List<List<RadioButton>>();
        private int vestigePanelBuffer, vestigeChoiceBuffer;

        // 1. each of these fields represents one of the four sections of the summary page.
        // 2. the outer list is list of subsections, with visual separation between each subsection.
        // 3. each subsection contains a list of rows
        // 4. each row is a list of labels
        private List<List<List<Control>>> beforeCombat;
        private List<List<List<Control>>> enemyTurn;
        private List<List<List<Control>>> myTurn;
        private List<List<List<Control>>> misc;

        private Label name, desc1, desc2, init;
        private List<List<Control>> senses = new List<List<Control>>();
        private Label listen, spot, evade, languages;
        private List<List<Control>> evadeControls = new List<List<Control>>();
        private List<List<Control>> downtime = new List<List<Control>>();
        private List<List<Control>> condition = new List<List<Control>>();

        private Label ac, touch;
        private Label acEffects;
        private List<List<Control>> acControls = new List<List<Control>>();
        private Label currentHp, maxHp, dr;
        private Label hpEffects;
        private List<List<Control>> hpControls = new List<List<Control>>();
        private List<List<Control>> react = new List<List<Control>>();
        private Label resist, immune;
        private List<List<Control>> resistControls = new List<List<Control>>();
        private List<List<Control>> immuneControls = new List<List<Control>>();
        private List<List<Control>> defend = new List<List<Control>>();
        private Label sr;
        private List<List<Control>> srControls = new List<List<Control>>();
        private List<List<Control>> saves = new List<List<Control>>();

        private Label speed;
        private List<List<Control>> movement = new List<List<Control>>();
        private List<List<Control>> attacks = new List<List<Control>>();
        private Label space, reach, bab, grapple;
        private List<List<Control>> atkOptions = new List<List<Control>>();
        private List<List<Control>> defOptions = new List<List<Control>>();
        private List<List<Control>> actions = new List<List<Control>>();
        private List<List<Control>> specialActions = new List<List<Control>>();

        private List<List<Control>> abilityScores = new List<List<Control>>();
        private List<List<Control>> sq = new List<List<Control>>();
        private List<List<Control>> skills = new List<List<Control>>();
        private List<List<Control>> miscSkills = new List<List<Control>>();

        private const string INDENT = "     ";

        private Dictionary<string, List<LabeledField>> fieldsByCategory = new Dictionary<string, List<LabeledField>>();
        private Dictionary<string, List<CheckBox>> flagsByCategory = new Dictionary<string, List<CheckBox>>();
        private Dictionary<string, List<List<Control>>> whereFieldsGo;

        private List<Label> slotBoxes;

        private Control dragging = null;
        private Size draggingFrom;
        public Dictionary<Control, ToolTip> toolTips = new Dictionary<Control, ToolTip>();

        private Dictionary<string, GroupBox> locationGroups = new Dictionary<string, GroupBox>();
        private Dictionary<string, ListBox> locationLists = new Dictionary<string, ListBox>();
        private Dictionary<string, CheckBox> locationWeights = new Dictionary<string, CheckBox>();
        private Dictionary<string, Button> addItemButtons = new Dictionary<string, Button>();
        private Control draggingItemFrom = null;
        private int numberDragging = 0;

        private AddItemDialog addItemDialog = new AddItemDialog();
        
        private Controller controller = null;

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
            return pagStats;
        }

        private (int value, List<string> explanation) GetStatValue(string stat) {
            return controller.Effects.GetStat(stat).GetValue(controller.Effects, controller.EquippedItems);
        }

        private string ValueText(int value, bool plus) {
            return ((plus && value >= 0 ? "+" : "") + value).Replace("-", "–");
        }

        private void ToolTipStatLabel(Control label, List<string> explanation) {
            toolTips[label].SetToolTip(label, string.Join("\n", explanation));
        }

        private void ToolTipTextLabel(Control label, string description) {
            toolTips[label].SetToolTip(label, description);
        }

        private EventHandler RollStat(string dice, string description) {
            return (object sender, EventArgs e) => {
                controller.RollServer.Roll(dice, description);
            };
        }

        private void Rollable(Control label, string prefix, string diceText) {
            ToolStripMenuItem roll = new ToolStripMenuItem {
                Text = $"Roll {prefix} ({diceText})"
            };
            roll.Click += RollStat(diceText, prefix);
            label.ContextMenuStrip = new ContextMenuStrip();
            label.ContextMenuStrip.Items.Add(roll);
        }

        private void LabelStat(Label label, int value, List<string> explanation, string prefix, string stat, string postfix = "", bool plus = true) {
            string statValue = ValueText(value, plus);
            label.Text = $"{prefix} {statValue}{postfix}";
            ToolTipStatLabel(label, explanation);

            Rollable(label, prefix, $"1d20{value.ToString("+#;-#;\"\"")}");
        }

        private void LabelStat(Label label, string prefix, string stat, string postfix = "", bool plus = true) {
            var (value, explanation) = GetStatValue(stat);
            LabelStat(label, value, explanation, prefix, stat, postfix, plus);
        }

        private void DynamicStat(List<List<Control>> section, string prefix, string stat, string postfix = "", bool plus = true, int hideIfEqual = int.MinValue) {
            (int baseValue, List<string> baseExplanation) = GetStatValue(stat);
            IEnumerable<Effects.Statistic> specials = controller.Effects.RelevantStatsWithTag(stat, controller.EquippedItems);
            bool relevant =
                specials.Any() ||
                controller.Effects.GetStat(stat).Relevant(controller.Effects, controller.EquippedItems);
            if ((baseValue != hideIfEqual) && relevant) {
                Label label = new Label().Initialize(this);
                section.Add(new List<Control> { label });
                LabelStat(label, baseValue, baseExplanation, prefix, stat, postfix, plus);
            }

            foreach (Effects.Statistic special in specials) {
                // this is a hack, i don't much care tbh.
                if (special.Name.Equals("Initiative")) {
                    continue;
                }
                (int value, List<string> explanation) = special.GetValue(controller.Effects, controller.EquippedItems);
                if (value == baseValue) {
                    continue;
                }
                Label lblSpecial = new Label().Initialize(this);
                section.Add(new List<Control> { lblSpecial });
                LabelStat(lblSpecial, value, explanation, special.Name, special.Name, postfix, plus);
            }
        }

        private void AbilityScore(string ability) {
            Label lblScore = new Label().Initialize(this);
            Label lblModifier = new Label().Initialize(this);

            string scoreStat = $"{ability} score";
            var (sValue, sExplanation) = GetStatValue(scoreStat);
            LabelStat(lblScore, sValue, sExplanation, ability, scoreStat, "", false);

            string modifierStat = $"{ability} modifier";
            var (mValue, mExplanation) = GetStatValue(modifierStat);
            LabelStat(lblModifier, mValue, mExplanation, "(", modifierStat, " )");

            abilityScores.Add(new List<Control> { lblScore, lblModifier });

            string checkStat = $"{ability} checks";
            DynamicStat(abilityScores, checkStat, checkStat, "", true, mValue);
        }

        private void RemoveDynamicSection(List<List<Control>> section) {
            foreach (List<Control> row in section) {
                foreach (Control control in row) {
                    if (control is Label label) {
                        pagStats.Controls.Remove(label);
                        toolTips.Remove(label);
                    }
                }
            }
            section.Clear();
        }

        private ToolStripMenuItem AttackRoll(string weapon, int bonus) {
            string modifier = bonus.ToString("+#;-#;\"\"");
            ToolStripMenuItem roll = new ToolStripMenuItem {
                Text = $"Roll {weapon} attack (1d20{modifier})"
            };
            roll.Click += RollStat($"1d20{modifier}", $"{weapon} attack");
            return roll;
        }

        private ContextMenuStrip DamageRoll(string weapon, string damage) {
            ContextMenuStrip menu = new ContextMenuStrip();
            ToolStripMenuItem roll = new ToolStripMenuItem {
                Text = $"Roll {weapon} damage ({damage})"
            };
            roll.Click += RollStat(damage, $"{weapon} damage");
            menu.Items.Add(roll);
            return menu;
        }

        private void AttackLabels(Attack attack, bool full) {
            Label attackLabel = new Label().Initialize(this);
            attackLabel.ContextMenuStrip = new ContextMenuStrip();
            StringBuilder text = new StringBuilder();
            if (full) {
                text.Append("     ");
            }
            text.Append(attack.RangeIncrement > 0 ? "Ranged " : "Melee ");
            text.Append(attack.WeaponName + " " + ValueText(attack.AttackBonus.bonus, true));
            attackLabel.ContextMenuStrip.Items.Add(AttackRoll(attack.WeaponName, attack.AttackBonus.bonus));
            foreach (int extra in attack.ExtraAttacks) {
                text.Append("/" + ValueText(attack.AttackBonus.bonus + extra, true));
                // there's a quirk here where attacks at the same bonus show up separately
                attackLabel.ContextMenuStrip.Items.Add(AttackRoll(attack.WeaponName, attack.AttackBonus.bonus + extra));
            }
            attackLabel.Text = text.ToString();
            ToolTipStatLabel(attackLabel, attack.AttackBonus.explanation);

            Label damageLabel = new Label().Initialize(this);
            string damage = attack.Damage.dice.ToString();
            damageLabel.Text = $"({damage})";
            ToolTipStatLabel(damageLabel, attack.Damage.explanation);
            damageLabel.ContextMenuStrip = DamageRoll(attack.WeaponName, damage);
            
            Label critLabel = new Label().Initialize(this);
            damage = attack.CriticalDamage.dice.ToString();
            critLabel.Text = $"({attack.ThreatRange}{(attack.ThreatRange == 20 ? "" : "–20")}/{damage})";
            ToolTipStatLabel(critLabel, attack.CriticalDamage.explanation);
            critLabel.ContextMenuStrip = DamageRoll(attack.WeaponName + " critical", damage);

            attacks.Add(new List<Control> { attackLabel, damageLabel, critLabel });
        }

        private void OneLineText(Label label, Dictionary<string, List<(string, string desc)>> textEffects, string key, string prefix = null) {
            if (textEffects[key].Any()) {
                label.Text = $"{prefix ?? key} {string.Join(", ", textEffects[key].Select(x => x.desc))}";
            } else {
                label.Text = "";
            }
        }

        private void MultilineText(List<List<Control>> section, Dictionary<string, List<(string name, string desc)>> textEffects, string key, bool header = true) {
            RemoveDynamicSection(section);
            if (textEffects == null) {
                if (!fieldsByCategory.ContainsKey(key) && !flagsByCategory.ContainsKey(key)) {
                    return;
                }
            } else {
                if (!textEffects[key].Any() && !fieldsByCategory.ContainsKey(key) && !flagsByCategory.ContainsKey(key)) {
                    return;
                }
            }

            if (header) {
                Label headerLabel = new Label() {
                    Text = key
                }.Initialize(this);
                section.Add(new List<Control> { headerLabel });
                pagStats.Controls.Add(headerLabel);
            }

            if (fieldsByCategory.ContainsKey(key)) {
                foreach (LabeledField field in fieldsByCategory[key]) {
                    section.Add(new List<Control> { field });
                }
            }
            if (flagsByCategory.ContainsKey(key)) {
                foreach (CheckBox flag in flagsByCategory[key]) {
                    section.Add(new List<Control> { flag });
                }
            }

            if (textEffects == null) {
                return;
            }

            foreach ((string name, string desc) in textEffects[key]) {
                Label effectLabel = new Label() {
                    Text = INDENT + name
                }.Initialize(this);
                ToolTipTextLabel(effectLabel, desc);
                section.Add(new List<Control> { effectLabel });
                pagStats.Controls.Add(effectLabel);
            }
        }

        private void Text_Enter(object sender, EventArgs e) {
            BeginInvoke((Action)delegate {
                ((TextBox)sender).SelectAll();
            });
        }

        private void Name_Leave(object sender, EventArgs e) {
            controller.Character.name = txtName.Text;
        }

        private void Alignment_Leave(object sender, EventArgs e) {
            controller.Character.alignment = txtAlignment.Text;
        }

        private void Gender_Leave(object sender, EventArgs e) {
            controller.Character.gender = txtGender.Text;
        }

        private void Race_SelectedValueChanged(object sender, EventArgs e) {
            controller.Character.race = cboRace.Text;
            DisplayRacialAbilityModifiers();
            DisplayTotalAbilityScores();
            HandleLanguages();
            DisplayFeats();
        }

        private void Assigned_Leave(object sender, EventArgs e) {
            AssignAbilityScore((TextBox)sender);
            HandleLanguages();
        }

        private void AssignAbilityScore(TextBox txt) {
            try {
                controller.Character.assignedAbilityScores[(string)txt.Tag] = int.Parse(txt.Text);
            } catch (FormatException) {
                controller.Character.assignedAbilityScores[(string)txt.Tag] = 0;
                txt.Text = "";
            }
            controller.Effects.AssignAbilityScores(controller.Character.assignedAbilityScores);
            DisplayTotalAbilityScores();
            HandleLanguages();
            DisplayTotalHp();
            for (int i = 0; i < controller.Character.classLevels.Count(); i++) {
                DisplaySkillPoints(i);
            }
        }

        private void DisplayRacialAbilityModifiers() {
            const string MODIFIER_FORMAT = "+#;–#;\"\"";

            Race race = controller.RaceDefinitions[controller.Character.race];
            txtRacialStr.Text = race.Str.ToString(MODIFIER_FORMAT);
            txtRacialDex.Text = race.Dex.ToString(MODIFIER_FORMAT);
            txtRacialCon.Text = race.Con.ToString(MODIFIER_FORMAT);
            txtRacialInt.Text = race.Int.ToString(MODIFIER_FORMAT);
            txtRacialWis.Text = race.Wis.ToString(MODIFIER_FORMAT);
            txtRacialCha.Text = race.Cha.ToString(MODIFIER_FORMAT);
        }

        private void DisplayTotalAbilityScores() {
            Race race = controller.RaceDefinitions[controller.Character.race];
            txtFinalStr.Text = "" + (controller.Character.assignedAbilityScores[STR] + race.Str);
            txtFinalDex.Text = "" + (controller.Character.assignedAbilityScores[DEX] + race.Dex);
            txtFinalCon.Text = "" + (controller.Character.assignedAbilityScores[CON] + race.Con);
            txtFinalInt.Text = "" + (controller.Character.assignedAbilityScores[INT] + race.Int);
            txtFinalWis.Text = "" + (controller.Character.assignedAbilityScores[WIS] + race.Wis);
            txtFinalCha.Text = "" + (controller.Character.assignedAbilityScores[CHA] + race.Cha);
        }

        private EventHandler SelectBonusLanguage(int i) {
            return (object sender, EventArgs e) => {
                controller.Character.bonusLanguages[i] = ((ComboBox)sender).Text;
            };
        }

        private EventHandler SelectFeat(string slot) {
            return (object sender, EventArgs e) => {
                ComboBox featBox = ((ComboBox)sender);
                controller.Character.feats[slot] = featBox.Text;
                featBox.Font = new Font(featBox.Font, FontStyle.Regular);
            };
        }

        private void AddSkillColumn(string skill, int left) {
            Label skillName = new Label {
                AutoSize = false,
                Size = lblSkill1.Size,
                Left = left,
                Top = lblSkill1.Top,
                TextAlign = lblSkill1.TextAlign,
                Text = Controller.ShortName(skill),
                Tag = skill,
            };
            toolTips[skillName] = new ToolTip();
            toolTips[skillName].SetToolTip(skillName, skill);

            skillNames.Add(skillName);
            pagCharacter.Controls.Add(skillName);

            skillRanks[skill] = new List<TextBox>();

            Label totalRanks = new Label {
                AutoSize = false,
                Size = lblTotalRanks.Size,
                Left = left,
                Top = lblTotalRanks.Top,
                TextAlign = lblTotalRanks.TextAlign,
                Text = "0"
            };
            totalSkillRanks[skill] = totalRanks;
            pagCharacter.Controls.Add(totalRanks);

            cboAddSkill.Left = left + skillLabelOffset;
            cboAddSkill.Items.Remove(skill);

            TextBox lastBox = null;
            for (
                int i = 0, levelTop = txtRanksCrossClass.Top;
                i < controller.Character.classLevels.Count();
                i++, levelTop += levelBuffer
            ) {
                TextBox ranksBox = new TextBox {
                    Top = levelTop,
                    Left = left,
                    Size = txtRanksCrossClass.Size,
                    TextAlign = txtRanksCrossClass.TextAlign,
                    BackColor = controller.IsClassSkill(skill, i)
                        ? txtRanksClass.BackColor
                        : txtRanksCrossClass.BackColor,
                };
                if (i < controller.Character.skillRanks[skill].Count) {
                    int ranks = controller.Character.skillRanks[skill][i];
                    ranksBox.Text = ranks.ToString("#;#;\"\"");
                } else {
                    controller.Character.skillRanks[skill].Add(0);
                }
                lastBox = ranksBox;

                ranksBox.Enter += Text_Enter;
                ranksBox.Leave += SetRanks(skill, i);
                skillRanks[skill].Add(ranksBox);
                pagCharacter.Controls.Add(ranksBox);

                skillPoints[i].Left = left + skillLabelOffset;
            }
            CheckRanks(skill);

            lastBox.Focus();
            // this just ... doesn't work.  WinForms refuses to set the text to something that isn't in the list.
            // this is despite the fact that you *can* do this in the properties menu just fine.
            // cboAddSkill.Text = "Add Skill";
        }

        private void AddSkill_SelectedValueChanged(object sender, EventArgs e) {
            string skill = ((ComboBox)sender).Text;
            if (!SKILLS_LIST.Contains(skill)) {
                return;
            }

            controller.Character.skillRanks[skill] = Enumerable.Repeat(0, controller.Character.classLevels.Count()).ToList();
            AddSkillColumn(
                skill,
                skillNames.Any() ? skillNames.Last().Left + skillLabelOffset : cboAddSkill.Left
            );
        }

        private void AddLevel_Click(object sender, EventArgs e) {
            controller.AddLevel();
            DisplayLevels();
            DisplayFeats();
            DisplayXp();
        }

        private EventHandler SelectAbilityIncrement(int levelIndex) {
            return (object sender, EventArgs e) => {
                controller.Character.abilityScoreIncrements[levelIndex + 1] = ((ComboBox)sender).Text;
                DisplayTotalHp();
                for (int i = levelIndex; i < controller.Character.classLevels.Count; i++) {
                    DisplaySkillPoints(i);
                }
            };
        }

        private EventHandler SelectClassLevel(int levelIndex) {
            return (object sender, EventArgs e) => {
                controller.Character.classLevels[levelIndex] = ((ComboBox)sender).Text;

                DisplaySkillPoints(levelIndex);
                CheckSkillColors(levelIndex);
                DisplayFeats();
                TextBox hp = hpRolls[levelIndex];
                if (!int.TryParse(hp.Text, out int _)) {
                    HitDieForLevel(hp, levelIndex);
                }

                UpdateSubsystemTabs();
            };
        }

        private void HitDieForLevel(TextBox box, int levelIndex) {
            box.Text = "d" + controller.ClassDefinitions[controller.Character.classLevels[levelIndex]].HitDie;
            box.Font = new Font(box.Font, FontStyle.Italic);
        }

        private EventHandler SetHp(int levelIndex) {
            return (object sender, EventArgs e) => {
                TextBox box = (TextBox)sender;
                try {
                    controller.Character.rolledHitDice[levelIndex] = int.Parse(box.Text);
                    box.Font = new Font(box.Font, FontStyle.Regular);
                } catch (FormatException) {
                    controller.Character.rolledHitDice[levelIndex] = 0;
                    HitDieForLevel(box, levelIndex);
                }
                DisplayTotalHp();
            };
        }

        private void CheckRanks(string skill, int levelIndex = 0) {
            for (int i = levelIndex; i < controller.Character.classLevels.Count; i++) {
                TextBox ranksBox = skillRanks[skill][i];
                if (controller.TooManyRanks(skill, i)) {
                    // Adding spaces around the text makes the strikethrough more apparent, and doesn't
                    // mess with the int.Parse at all!
                    ranksBox.Text = $"  {controller.Character.skillRanks[skill][i]}  ";
                    ranksBox.ForeColor = Color.Red;
                    ranksBox.Font = new Font(ranksBox.Font, FontStyle.Bold | FontStyle.Strikeout);
                } else {
                    ranksBox.ForeColor = SystemColors.WindowText;
                    ranksBox.Font = new Font(ranksBox.Font, FontStyle.Regular);
                }
            }
        }

        private EventHandler SetRanks(string skill, int levelIndex) {
            return (object sender, EventArgs e) => {
                TextBox ranksBox = ((TextBox)sender);
                try {
                    controller.Character.skillRanks[skill][levelIndex] = int.Parse(ranksBox.Text);
                    CheckRanks(skill, levelIndex);
                } catch (FormatException) {
                    controller.Character.skillRanks[skill][levelIndex] = 0;
                    ranksBox.Text = "";
                }
                DisplaySkillPoints(levelIndex);
                DisplaySkillRanks(skill);
            };
        }

        private int Parse(string s) {
            return string.IsNullOrWhiteSpace(s) ? 0 : int.Parse(s.Replace("–", "-"));
        }

        private void HandleLanguages() {
            Race race = controller.RaceDefinitions[controller.Character.race];
            txtAutomaticLanguages.Text = string.Join(", ", race.AutomaticLanguages);

            int numBonusLanguages = Math.Max(0, controller.GetStartingIntelligence() / 2 - 5);
            int curBonusLanguages = bonusLanguages.Count;
            int langBuffer = cboBonusLanguage.Top - txtAutomaticLanguages.Top;
            int langTop = cboBonusLanguage.Top + langBuffer * curBonusLanguages;
            for (int i = 0; i < curBonusLanguages; i++) {
                bonusLanguages[i].Items.Clear();
                foreach (string language in controller.RaceDefinitions[controller.Character.race].BonusLanguages) {
                    bonusLanguages[i].Items.Add(language);
                }
            }
            for (int i = curBonusLanguages; i < numBonusLanguages; i++) {
                Label bonusLabel = new Label {
                    Size = lblBonusLang.Size,
                    Top = langTop,
                    Left = lblBonusLang.Left,
                    Text = lblBonusLang.Text,
                    TextAlign = lblBonusLang.TextAlign
                };
                pagCharacter.Controls.Add(bonusLabel);
                bonusLabels.Add(bonusLabel);

                ComboBox box = new ComboBox {
                    Size = cboBonusLanguage.Size,
                    Top = langTop,
                    Left = cboBonusLanguage.Left
                };
                foreach (string language in controller.RaceDefinitions[controller.Character.race].BonusLanguages) {
                    box.Items.Add(language);
                }
                if (controller.Character.bonusLanguages.Count() > i) {
                    box.Text = controller.Character.bonusLanguages[i];
                } else {
                    controller.Character.bonusLanguages.Add("");
                }

                box.SelectedValueChanged += SelectBonusLanguage(i);

                pagCharacter.Controls.Add(box);
                bonusLanguages.Add(box);

                langTop += langBuffer;
            }
            int excess = controller.Character.bonusLanguages.Count - numBonusLanguages;
            if (excess > 0) {
                controller.Character.bonusLanguages.RemoveRange(numBonusLanguages, excess);
            }
            for (int i = curBonusLanguages - 1; i >= numBonusLanguages; i--) {
                pagCharacter.Controls.Remove(bonusLanguages[i]);
                bonusLanguages.RemoveAt(i);
                pagCharacter.Controls.Remove(bonusLabels[i]);
                bonusLabels.RemoveAt(i);
            }
        }

        private void DisplayFeats() {
            // For some reason, the race-selected method gets called before the character
            // is actually set up.
            if (!levels.Any()) {
                return;
            }
            ClearDynamicControls(pagCharacter, feats);
            int featRight = 0;
            foreach (var level in controller.GetFeatSlots()) {
                int featTop = levels[level.Key - 1].Top;
                int featLeft = cboFeat.Left;
                foreach (string slot in level.Value) {
                    ComboBox featBox = new ComboBox {
                        AutoCompleteMode = AutoCompleteMode.Append,
                        AutoCompleteSource = AutoCompleteSource.ListItems,
                        Top = featTop,
                        Left = featLeft,
                        Size = cboFeat.Size,
                        Font = scala // why do i need to do this?
                    };
                    if (controller.Character.feats.ContainsKey(slot)) {
                        featBox.Text = controller.Character.feats[slot];
                        featBox.Font = new Font(featBox.Font, FontStyle.Regular);
                    } else {
                        featBox.Text = slot;
                        featBox.Font = new Font(featBox.Font, FontStyle.Italic);
                    }

                    foreach (string feat in availableFeats) {
                        featBox.Items.Add(feat);
                    }
                    featBox.SelectedValueChanged += SelectFeat(slot);
                    feats.Add(featBox);
                    featLeft += featBox.Width + (cboFeat.Left - btnLevelDetails.Right);
                    pagCharacter.Controls.Add(featBox);

                    featRight = Math.Max(featRight, featBox.Right);
                }
            }
            AdjustHpAndSkills(featRight + (txtRolledHp.Left - cboFeat.Right));
        }

        private void DisplayTotalHp() {
            lblTotalHp.Text = "" + controller.BaseHp();
        }
        
        private void AdjustHpAndSkills(int left) {
            lblRolledHp.Left = left;
            lblTotalHp.Left = left;
            foreach (TextBox roll in hpRolls) {
                roll.Left = left;
            }

            left += skillBufferLeft;
            lblSkills.Left = left;
            foreach (Label skillName in skillNames) {
                skillName.Left = left;
                string skill = (string)skillName.Tag;
                totalSkillRanks[skill].Left = left;
                for (int i = 0; i < levels.Count; i++) {
                    skillRanks[skill][i].Left = left;
                }
                left += skillLabelOffset;
            }
            cboAddSkill.Left = left;
            for (int i = 0; i < levels.Count; i++) {
                skillPoints[i].Left = left;
            }
        }

        private void DisplaySkillPoints(int levelIndex) {
            if (!skillPoints.Any()) {
                return;
            }

            int remaining = controller.SkillPointsRemainingAtLevel(levelIndex);
            if (remaining > 1) {
                skillPoints[levelIndex].Text = "" + remaining + " skill points remaining";
            } else if (remaining == 1) {
                skillPoints[levelIndex].Text = "1 skill point remaining";
            } else if (remaining == 0) {
                skillPoints[levelIndex].Text = ""; 
            } else {
                skillPoints[levelIndex].Text = "" + (-remaining) + " too many skill points";
            }
        }

        private void CheckSkillColors(int levelIndex) {
            foreach (var skillRanks in skillRanks) {
                skillRanks.Value[levelIndex].BackColor = controller.IsClassSkill(skillRanks.Key, levelIndex)
                    ? txtRanksClass.BackColor
                    : txtRanksCrossClass.BackColor;
            };
        }

        private void DisplaySkillRanks(string skill) {
            totalSkillRanks[skill].Text = "" + controller.TotalSkillRanks(skill);
        }

        private EventHandler PopupLevelDetails(int levelIndex) {
            return (object sender, EventArgs e) => {
                levelDetailsDialog.PrepareLevel(levelIndex);
                levelDetailsDialog.ShowDialog();

                controller.Character.classLevels[levelIndex] = levelDetailsDialog.GetClass();
                foreach (var feat in levelDetailsDialog.GetFeats()) {
                    controller.Character.feats[feat.Key] = feat.Value;
                }
                controller.Character.featChoices = levelDetailsDialog.GetFeatChoices();

                DisplayLevels();
                DisplayFeats();
            };
        }

        private void DisplayLevels() {
            ClearDynamicControls(pagCharacter, skillNames);
            foreach (List<TextBox> boxes in skillRanks.Values) {
                foreach (TextBox box in boxes) {
                    pagCharacter.Controls.Remove(box);
                }
            }
            skillRanks.Clear();
            foreach (Label totalRanks in totalSkillRanks.Values) {
                pagCharacter.Controls.Remove(totalRanks);
            }
            totalSkillRanks.Clear();

            ClearDynamicControls(pagCharacter, increments);
            ClearDynamicControls(pagCharacter, levels);
            ClearDynamicControls(pagCharacter, levelDetails);
            ClearDynamicControls(pagCharacter, hpRolls);
            ClearDynamicControls(pagCharacter, skillPoints);

            int levelTop = cboClass.Top;
            for (int i = 0; i < controller.Character.classLevels.Count; i++) {
                string @class = controller.Character.classLevels[i];
                ComboBox level = new ComboBox {
                    Top = levelTop,
                    Left = cboClass.Left,
                    Size = cboClass.Size
                };
                foreach (string className in controller.ClassDefinitions.Keys.OrderBy(x => x)) {
                    level.Items.Add(className);
                }
                level.Text = @class;
                level.SelectedValueChanged += SelectClassLevel(i);
                levels.Add(level);
                pagCharacter.Controls.Add(level);

                Button details = new Button {
                    Top = levelTop,
                    Left = btnLevelDetails.Left,
                    Size = btnLevelDetails.Size,
                    Text = btnLevelDetails.Text
                };
                details.Click += PopupLevelDetails(i);
                levelDetails.Add(details);
                pagCharacter.Controls.Add(details);

                TextBox rolledHp = new TextBox {
                    Top = levelTop,
                    Left = txtRolledHp.Left,
                    Size = txtRolledHp.Size,
                    TextAlign = txtRolledHp.TextAlign,
                };
                if (i < controller.Character.rolledHitDice.Count) {
                    int roll = controller.Character.rolledHitDice[i];
                    if (roll == 0) {
                        HitDieForLevel(rolledHp, i);
                    } else {
                        rolledHp.Text = "" + roll;
                    }
                } else {
                    controller.Character.rolledHitDice.Add(0);
                    HitDieForLevel(rolledHp, i);
                }

                rolledHp.Enter += Text_Enter;
                rolledHp.Leave += SetHp(i);
                hpRolls.Add(rolledHp);
                pagCharacter.Controls.Add(rolledHp);

                Label points = new Label {
                    Top = levelTop,
                    Left = cboAddSkill.Left + (skillNames.Count() * skillLabelOffset),
                    Size = lblPoints.Size,
                    TextAlign = lblPoints.TextAlign,
                };
                skillPoints.Add(points);
                pagCharacter.Controls.Add(points);
                DisplaySkillPoints(i);

                if ((i+1) % 4 == 0) {
                    ComboBox increment = new ComboBox {
                        Left = cboAbilityIncrement.Left,
                        Top = levelTop,
                        Size = cboAbilityIncrement.Size
                    };
                    foreach (string s in ABILITIES) {
                        increment.Items.Add(s);
                    }
                    if (controller.Character.abilityScoreIncrements.ContainsKey(i + 1)) {
                        increment.Text = controller.Character.abilityScoreIncrements[i + 1];
                    }
                    increment.SelectedValueChanged += SelectAbilityIncrement(i);
                    increments.Add(increment);
                    pagCharacter.Controls.Add(increment);
                }

                levelTop += levelBuffer;
            }
            btnAddLevel.Top = levelTop;

            int skillLeft = cboAddSkill.Left;
            cboAddSkill.Left = skillLeft;
            int skillIndex = 0;
            foreach (var skill in controller.Character.skillRanks) {
                AddSkillColumn(skill.Key, skillLeft);

                skillIndex++;
                skillLeft += skillLabelOffset;
            }

            DisplayTotalHp();
            foreach (Label skill in skillNames) {
                DisplaySkillRanks((string)skill.Tag);
            }
        }

        private void DisplayXp() {
            int xpNeeded = controller.Character.ExperienceToNextLevel();
            int nextLevel = controller.Character.classLevels.Count + 1;
            btnAddLevel.Text = xpNeeded < 0
                ? "Level Up!"
                : $"{xpNeeded.ToString("#,###")} XP to level {nextLevel}";

            ClearDynamicControls(pagCharacter, sessionXps);
            ClearDynamicControls(pagCharacter, sessionDescriptions);

            var sessions = controller.Character.sessions;
            int sessionTop = lblSampleSession2.Top - ((sessions.Count - 1) * transactionBuffer);
            for (int i = 0; i < sessions.Count; i++) {
                var (xp, description) = sessions[i];

                Label descLabel = new Label {
                    Top = sessionTop,
                    Left = lblSampleSession1.Left,
                    Anchor = lblSampleSession1.Anchor,
                    AutoSize = false,
                    Size = lblSampleSession1.Size,
                    TextAlign = lblSampleSession1.TextAlign,
                    Text = description
                };
                sessionDescriptions.Add(descLabel);
                pagCharacter.Controls.Add(descLabel);


                Label xpLabel = new Label {
                    Top = sessionTop,
                    Left = lblSampleXp1.Left,
                    Anchor = lblSampleXp1.Anchor,
                    AutoSize = false,
                    Size = lblSampleXp1.Size,
                    TextAlign = lblSampleXp1.TextAlign,
                    Text = "" + xp
                };
                sessionXps.Add(xpLabel);
                pagCharacter.Controls.Add(xpLabel);

                ContextMenuStrip deleteMenu = new ContextMenuStrip();
                ToolStripMenuItem deleteSession = new ToolStripMenuItem($"Delete {description}");
                deleteSession.Click += DeleteSession(i);
                deleteMenu.Items.Add(deleteSession);
                descLabel.ContextMenuStrip = deleteMenu;
                xpLabel.ContextMenuStrip = deleteMenu;

                sessionTop += transactionBuffer;
            }
            if (sessions.Any()) {
                txtSessionDesc.Text = "";
                txtSessionXp.Text = "";
            }

            lblTotalXp.Text = controller.Character.TotalExperience().ToString("#,###;-#,###;0");
        }

        private EventHandler DeleteSession(int i) {
            return (object sender, EventArgs e) => {
                controller.Character.sessions.RemoveAt(i);
                DisplayXp();
            };
        }

        private void AddSession_Click(object sender, EventArgs e) {
            if (int.TryParse(txtSessionXp.Text, out int value)) {
                txtSessionXp.BackColor = SystemColors.Window;
                controller.Character.sessions.Add((value, txtSessionDesc.Text));
                txtSessionDesc.Text = "";
                txtSessionXp.Text = "";
                txtSessionDesc.Focus();
                DisplayXp();
            } else {
                txtSessionXp.BackColor = Color.Red;
            }
        }

        private void SessionXp_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Enter) {
                e.SuppressKeyPress = true;
                e.Handled = true;
                btnAddSession.PerformClick();
            }
        }

        private void DisplayCharacter() {
            txtName.Text = controller.Character.name;
            txtAlignment.Text = controller.Character.alignment;
            txtGender.Text = controller.Character.gender;

            txtAssignedStr.Text = "" + controller.Character.assignedAbilityScores[STR];
            txtAssignedDex.Text = "" + controller.Character.assignedAbilityScores[DEX];
            txtAssignedCon.Text = "" + controller.Character.assignedAbilityScores[CON];
            txtAssignedInt.Text = "" + controller.Character.assignedAbilityScores[INT];
            txtAssignedWis.Text = "" + controller.Character.assignedAbilityScores[WIS];
            txtAssignedCha.Text = "" + controller.Character.assignedAbilityScores[CHA];

            DisplayRacialAbilityModifiers();
            DisplayTotalAbilityScores();

            HandleLanguages();

            DisplayLevels();
            cboAddSkill.Items.Clear();
            foreach (string skill in SKILLS_LIST) {
                cboAddSkill.Items.Add(skill);
            }
            foreach (string skill in controller.Character.skillRanks.Keys) {
                cboAddSkill.Items.Remove(skill);
            }
            DisplayFeats();
            DisplayXp();

            cboRace.Text = controller.Character.race;
        }

        private void CharacterTab_Enter(object sender, EventArgs e) {
            if (!loaded) {
                return;
            }

            DisplayCharacter();
        }

        private void CharacterTab_Leave(object sender, EventArgs e) {
            controller.Character.skillRanks = controller.Character.skillRanks
                .Where(x => x.Value.Sum() > 0)
                .ToDictionary(x => x.Key, x => x.Value);
        }

        public void RefreshEffects() {
            controller.ForceHiddenFlagsOff();
            ClearDynamicControls(pagStats, flagsByCategory);
            foreach (var toggle in controller.Effects.controllableFlags) {
                if (!toggle.Value.Show.On(controller.Effects, controller.EquippedItems)) {
                    continue;
                }

                string toggleName = toggle.Key;
                CheckBox chkNewToggle = new CheckBox() {
                    AutoSize = true,
                    Text = toggleName
                }.Initialize(this);

                ToolTipTextLabel(chkNewToggle, toggle.Value.Description);
                if (controller.Character.flagValues.ContainsKey(toggleName)) {
                    bool value = controller.Character.flagValues[toggleName];
                    chkNewToggle.Checked = value;
                    toggle.Value.On = value;
                }
                chkNewToggle.Click += Toggle_Click;

                string category = toggle.Value.Category;
                if (!flagsByCategory.ContainsKey(category)) {
                    flagsByCategory[category] = new List<CheckBox>();
                }
                flagsByCategory[category].Add(chkNewToggle);
                pagStats.Controls.Add(chkNewToggle);
            }

            Dictionary<string, List<(string name, string desc)>> textEffects = controller.Effects.GetTextEffects(controller.EquippedItems);

            name.Text = controller.Character.name.ToUpper();

            Dictionary<string, int> classLevels = controller.Character.SummarizeClassLevels();
            desc1.Text =
                controller.Character.gender + " " +
                controller.Character.race + " " +
                string.Join(
                    ", ",
                    controller.Character.classLevels.Distinct().Select(x => x.Replace(" ", "\u00a0").ToLower() + "\u00a0" + classLevels[x])
                );
            // I wanted the separator for classes to be "/" like it is in most stat blocks,
            // but I also wanted wrapping line breaks to only appear between classes.  Unfortunately,
            // the zero-width space isn't respected by the wrapping routine for some reason.  So I
            // decided to use a character that looks okay when I put a space after it instead.  I'll
            // probably revisit this when I move to a more modernized UI framework.
            string sizeCategory = SIZE_CATEGORIES[GetStatValue(SIZE_CATEGORY).value + 4];
            desc2.Text = controller.Character.alignment + " " + sizeCategory + " " +
                controller.Effects.CreatureType +
                (controller.Effects.SubTypes.Any() ? " (" + string.Join(", ", controller.Effects.SubTypes) + ")" : "");
            OneLineText(languages, textEffects, LANGUAGES);
            LabelStat(init, "Initiative", INITIATIVE);
            MultilineText(senses, textEffects, SENSES);

            if (controller.Effects.HasFlag("deafened", controller.EquippedItems)) {
                listen.Text = "Listen \u2014,";
                ToolTipTextLabel(listen, "Defeaned");
            } else {
                LabelStat(listen, "Listen", LISTEN, ","); // ok navi calm down.
            }
            if (controller.Effects.HasFlag("blinded", controller.EquippedItems)) {
                spot.Text = "Spot \u2014";
                ToolTipTextLabel(spot, "Blinded");
            } else {
                LabelStat(spot, "Spot", SPOT);
            }

            OneLineText(evade, textEffects, EVADE);
            MultilineText(evadeControls, null, EVADE, false);

            MultilineText(condition, textEffects, CONDITION);

            LabelStat(ac, "AC", AC, ",", false);
            LabelStat(touch, "touch", TOUCH_AC, "", false);
            OneLineText(acEffects, textEffects, AC, INDENT);
            MultilineText(acControls, null, AC, false);

            LabelStat(currentHp, "hp", CURRENT_HP, ";", false);
            LabelStat(maxHp, "max", MAX_HP, " (" + controller.Character.classLevels.Count() + " HD)", false);
            OneLineText(dr, textEffects, DR);
            if (dr.Text.Any()) {
                maxHp.Text += ";";
            }
            OneLineText(hpEffects, textEffects, HP, INDENT);
            MultilineText(hpControls, null, HP, false);

            MultilineText(react, textEffects, REACT);

            OneLineText(resist, textEffects, RESIST);
            MultilineText(resistControls, null, RESIST, false);

            OneLineText(immune, textEffects, IMMUNE);
            MultilineText(immuneControls, null, IMMUNE, false);

            OneLineText(sr, textEffects, SR);
            MultilineText(srControls, null, SR, false);

            RemoveDynamicSection(saves);
            DynamicStat(saves, "Fort save", FORT_SAVES);
            DynamicStat(saves, "Ref save", REF_SAVES);
            DynamicStat(saves, "Will save", WILL_SAVES);

            LabelStat(speed, "Speed", LAND_SPEED, " ft.", false);
            MultilineText(movement, textEffects, MOVEMENT);

            RemoveDynamicSection(attacks);
            List<Attack> singleAttacks = controller.GetAttacks();
            foreach (Attack attack in singleAttacks) {
                AttackLabels(attack, false);
            }
            foreach (List<Attack> fullAttack in controller.GetFullAttacks()) {
                if (fullAttack.Count == 1 && !fullAttack[0].ExtraAttacks.Any()) {
                    continue;
                }
                attacks.Add(new List<Control> { new Label() { Text = "Full Attack" }.Initialize(this) });
                foreach (Attack attack in fullAttack) {
                    AttackLabels(attack, true);
                }
            }

            LabelStat(space, "Space", SPACE, " ft.;", false);
            LabelStat(reach, "Reach", REACH, " ft.", false);
            LabelStat(bab, "Base Atk", BASE_ATTACK_BONUS, ";");
            LabelStat(grapple, "Grp", GRAPPLE);
            MultilineText(atkOptions, textEffects, ATK_OPTIONS);
            MultilineText(defOptions, textEffects, DEF_OPTIONS);
            MultilineText(specialActions, textEffects, SPECIAL_ACTIONS);
            MultilineText(actions, textEffects, ACTIONS);

            RemoveDynamicSection(abilityScores);
            AbilityScore("STR");
            AbilityScore("DEX");
            AbilityScore("CON");
            AbilityScore("INT");
            AbilityScore("WIS");
            AbilityScore("CHA");
            MultilineText(sq, textEffects, SQ);
            RemoveDynamicSection(skills);
            foreach (string skill in SKILLS_LIST) {
                DynamicStat(skills, skill, skill);
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

        private void RepositionRow(List<Control> row) {
            if (row.Count > 1) {
                int left = row[0].Right;
                int top = row[0].Top;
                foreach (Control control in row.Skip(1)) {
                    if ((control is Label && control.Text.Any()) || control is LabeledField) {
                        control.Top = top;
                        control.Left = left;
                        left = control.Right;
                    } else if (control is CheckBox) {
                        control.Top = top - 1;
                        control.Left = left;
                        left = control.Right;
                    }
                    control.BringToFront();
                }
            }
        }

        private void RepositionColumn(int columnLeft, List<List<List<Control>>> column) {
            int top = topBuffer;
            foreach (List<List<Control>> section in column) {
                bool anyText = false;
                foreach (List<Control> row in section) {
                    if (!row.Any()) {
                        continue;
                    }
                    Control first = row[0];
                    if (first.Text.Any() || !(first is Label)) {
                        anyText = true;
                        first.Left = columnLeft;
                        first.Top = top;
                        first.MaximumSize = new Size(columnWidth - 2 * leftBuffer, 1000);
                        top += lblTopLeft.Height + 1;
                        
                        // I cannot for the life of me get the custom labeled field control to size itself properly.
                        // If I AutoSize it and leave it alone, its height obscures everything else in the column.
                        // If I manually size it, no matter which width I choose, it cuts off its own label.
                        // So instead I'm just putting everything else on top of it.
                        first.BringToFront();

                        RepositionRow(row);
                    }
                }
                if (anyText) {
                    top += verticalBufferLarge;
                }
            }
        }

        private void RepositionLabels() {
            if (!loaded) {
                return;
            }
            columnWidth = ClientSize.Width / 4;

            RepositionColumn(leftBuffer, beforeCombat);
            RepositionColumn(leftBuffer + columnWidth, enemyTurn);
            RepositionColumn(leftBuffer + columnWidth * 2, myTurn);
            RepositionColumn(leftBuffer + columnWidth * 3, misc);
        }

        private void RemoveLocationGroup(string locationKey) {
            GroupBox locationGroup = locationGroups[locationKey];

            locationGroup.Controls.Remove(locationLists[locationKey]);
            locationLists.Remove(locationKey);

            locationGroup.Controls.Remove(locationWeights[locationKey]);
            locationWeights.Remove(locationKey);

            locationGroup.Controls.Remove(addItemButtons[locationKey]);
            addItemButtons.Remove(locationKey);

            pagInventory.Controls.Remove(locationGroup);
            locationGroups.Remove(locationKey);
        }

        private void InitializeInventory() {
            foreach (string locationKey in locationGroups.Keys.ToList()) {
                RemoveLocationGroup(locationKey);
            }

            int locationTop = grpSampleLocation.Top;
            int locationLeft = grpSampleLocation.Left;
            int locationBuffer = grpSampleLocation2.Left - grpSampleLocation.Right;
            locationWidthBuffer = grpSampleLocation.Width - lstSampleItems.Width;
            foreach (var location in controller.Character.inventory) {
                GroupBox locationBox = AddLocationGroup(locationTop, locationLeft, location.Key, location.Value);
                locationLeft += locationBox.Width + locationBuffer;
            }
            DisplayGold();
            RefreshEquipment();
        }

        private EventHandler AddItem(string locationName, InventoryLocation location) {
            return (object sender, EventArgs e) => {
                DialogResult result = addItemDialog.ShowDialog();
                if (result == DialogResult.Cancel) {
                    return;
                }

                if (addItemDialog.tabAddItem.SelectedTab == addItemDialog.pagWeapons) {
                    List<string> properties = new List<string>();
                    foreach (object o in addItemDialog.lstWeaponProperties.SelectedItems) {
                        properties.Add((string)o);
                    }
                    controller.AddWeapon(locationName, (string)addItemDialog.lstAddWeapon.SelectedItem, properties);
                } else if (addItemDialog.tabAddItem.SelectedTab == addItemDialog.pagArmor) {
                    List<string> properties = new List<string>();
                    foreach (object o in addItemDialog.lstArmorProperties.SelectedItems) {
                        properties.Add((string)o);
                    }
                    controller.AddArmor(locationName, (string)addItemDialog.lstAddArmor.SelectedItem, properties);
                } else if (addItemDialog.tabAddItem.SelectedTab == addItemDialog.pagShields) {
                    List<string> properties = new List<string>();
                    foreach (object o in addItemDialog.lstShieldProperties.SelectedItems) {
                        properties.Add((string)o);
                    }
                    controller.AddShield(locationName, (string)addItemDialog.lstAddShield.SelectedItem, properties);
                } else if (addItemDialog.tabAddItem.SelectedTab == addItemDialog.pagItems) {
                    controller.AddItem(locationName, (string)addItemDialog.lstAddItem.SelectedItem);
                }
                
                ListLocation(location, locationLists[locationName], locationGroups[locationName]);
                RefreshWeight();
            };
        }

        private EventHandler DeleteLocation(string locationName) {
            return (object sender, EventArgs e) => {
                controller.RemoveLocation(locationName);
                RemoveLocationGroup(locationName);
            };
        }

        private EventHandler DeleteItemFrom(string locationName) {
            return (object sender, EventArgs e) => {
                ListBox locationList = locationLists[locationName];
                if (!(locationList.SelectedItem is ItemDrag selected)) {
                    return;
                }
                controller.RemoveThing(locationName, selected.item);
                ListLocation(controller.Character.inventory[locationName], locationList, locationGroups[locationName]);
            };
        }

        private GroupBox AddLocationGroup(int top, int left, string name, InventoryLocation location) {
            GroupBox locationBox = new GroupBox {
                Text = name,
                Top = top,
                Left = left,
                Width = 100,
                Height = 0,
                AutoSize = true,
                Cursor = grpSampleLocation.Cursor,
            };
            locationBox.MouseDown += new MouseEventHandler(Location_MouseDown);
            locationBox.MouseUp += new MouseEventHandler(Location_MouseUp);
            locationBox.MouseMove += new MouseEventHandler(Location_MouseMove);

            ToolStripMenuItem deleteLocation = new ToolStripMenuItem {
                Text = $"Delete location {name}"
            };
            deleteLocation.Click += DeleteLocation(name);
            locationBox.ContextMenuStrip = new ContextMenuStrip();
            locationBox.ContextMenuStrip.Items.Add(deleteLocation);

            CheckBox countWeight = new CheckBox() {
                Text = chkWeightCounts.Text,
                Location = chkWeightCounts.Location,
                Checked = location.weighs,
                AutoSize = true,
                Cursor = chkWeightCounts.Cursor,
            };
            countWeight.CheckedChanged += CountWeight_CheckedChanged(name);
            locationWeights[name] = countWeight;
            locationBox.Controls.Add(countWeight);

            ListBox locationList = new ListBox() {
                AllowDrop = true,
                AutoSize = true,
                Location = lstSampleItems.Location,
                Height = 1,
                Cursor = lstSampleItems.Cursor,
            };
            locationList.MouseMove += new MouseEventHandler(LocationItem_MouseMove);
            locationList.DragEnter += new DragEventHandler(Location_DragEnter);
            locationList.DragDrop += new DragEventHandler(Location_DragDrop);
            ToolStripMenuItem deleteItem = new ToolStripMenuItem {
                Text = "Remove item"
            };
            deleteItem.Click += DeleteItemFrom(name);
            locationList.ContextMenuStrip = new ContextMenuStrip();
            locationList.ContextMenuStrip.Items.Add(deleteItem);
            locationLists[name] = locationList;
            locationBox.Controls.Add(locationList);

            Button addItem = new Button() {
                Location = btnAddItem.Location,
                Size = btnAddItem.Size,
                Anchor = btnAddItem.Anchor,
                Text = btnAddItem.Text,
                Cursor = btnAddItem.Cursor,
            };
            addItem.Click += AddItem(name, location);
            addItemButtons[name] = addItem;
            locationBox.Controls.Add(addItem);

            ListLocation(location, locationList, locationBox);
            pagInventory.Controls.Add(locationBox);
            locationGroups[name] = locationBox;
            return locationBox;
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
        private void LocationItem_MouseMove(object sender, MouseEventArgs e) {
            if (e.Button != MouseButtons.Left) {
                return;
            }

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
                ListLocation(location.Value, locationLists[location.Key], locationGroups[location.Key]);
            }
            RefreshEquipment();
        }

        private void DisplayGold() {
            ClearDynamicControls(pagInventory, transactionAmounts);
            ClearDynamicControls(pagInventory, transactionDescriptions);

            var transactions = controller.Character.transactions;
            int firstTransaction = Math.Max(0, transactions.Count - MAX_TRANSACTIONS);
            int numTransactions = Math.Min(transactions.Count, MAX_TRANSACTIONS);

            int transactionTop = lblTransactionAmount2.Top - ((numTransactions - 1) * transactionBuffer);
            for (int i = firstTransaction; i < transactions.Count; i++) {
                var (gp, description) = transactions[i];

                Label descLabel = new Label {
                    Top = transactionTop,
                    Left = lblTransactionDescription1.Left,
                    Anchor = lblTransactionDescription1.Anchor,
                    AutoSize = false,
                    Size = lblTransactionDescription1.Size,
                    TextAlign = lblTransactionDescription1.TextAlign,
                    Text = description
                };
                transactionDescriptions.Add(descLabel);
                pagInventory.Controls.Add(descLabel);


                Label gpLabel = new Label {
                    Top = transactionTop,
                    Left = lblTransactionAmount1.Left,
                    Anchor = lblTransactionAmount1.Anchor,
                    AutoSize = false,
                    Size = lblTransactionAmount1.Size,
                    TextAlign = lblTransactionAmount1.TextAlign,
                    Text = "" + gp
                };
                transactionAmounts.Add(gpLabel);
                pagInventory.Controls.Add(gpLabel);

                ContextMenuStrip deleteMenu = new ContextMenuStrip();
                ToolStripMenuItem deleteTransaction = new ToolStripMenuItem($"Delete {description}");
                deleteTransaction.Click += DeleteTransaction(i);
                deleteMenu.Items.Add(deleteTransaction);
                descLabel.ContextMenuStrip = deleteMenu;
                gpLabel.ContextMenuStrip = deleteMenu;

                transactionTop += transactionBuffer;
            }
            if (transactions.Any()) {
                txtTransactionDesc.Text = "";
                txtTransactionGp.Text = "";
            }

            lblTotalGold.Text = controller.Character.TotalGold().ToString("#,###.##;-#,###.##;0");
        }

        private EventHandler DeleteTransaction(int i) {
            return (object sender, EventArgs e) => {
                controller.Character.transactions.RemoveAt(i);
                DisplayGold();
            };
        }

        private void AddTransaction_Click(object sender, EventArgs e) {
            if (decimal.TryParse(txtTransactionGp.Text, out decimal value)) {
                txtTransactionGp.BackColor = SystemColors.Window;
                controller.Character.transactions.Add((value, txtTransactionDesc.Text));
                txtTransactionDesc.Text = "";
                txtTransactionGp.Text = "";
                txtTransactionDesc.Focus();
                DisplayGold();
            } else {
                txtTransactionGp.BackColor = Color.Red;
            }
        }

        private void TransactionGp_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Enter) {
                e.SuppressKeyPress = true;
                e.Handled = true;
                btnAddTransaction.PerformClick();
            }
        }

        private void AddLocation_Click(object sender, EventArgs e) {
            string newName = txtLocationName.Text;
            if (newName == "" || controller.Character.inventory.ContainsKey(newName)) {
                txtLocationName.BackColor = Color.Red;
                return;
            }

            txtLocationName.BackColor = SystemColors.Window;
            txtLocationName.Text = "";
            InventoryLocation location = new InventoryLocation();
            controller.Character.inventory[newName] = location;
            AddLocationGroup(this.Height / 2, this.Width / 2, newName, location);
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

        private void ListLocation(InventoryLocation location, ListBox locationBox, GroupBox locationGroup) {
            locationBox.Items.Clear();
            ListItems(location.weapons, locationBox);
            ListItems(location.armor, locationBox);
            ListItems(location.shields, locationBox);
            ListItems(location.items, locationBox);
            locationGroup.ContextMenuStrip.Items[0].Visible = location.IsEmpty();
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

        private void MainForm_Resize(object sender, EventArgs e) {
            RepositionLabels();
        }

        private void Toggle_Click(object sender, EventArgs e) {
            CheckBox toggle = (CheckBox)sender;
            string flag = toggle.Text;
            controller.Effects.controllableFlags[flag].On = toggle.Checked;
            controller.Character.flagValues[flag] = toggle.Checked;

            RefreshEffects();
        }



        private void TabSwitch(object sender, TabControlEventArgs e) {
            controller.Reload();
            RefreshEffects();
        }

        private void StatsTab_Enter(object sender, EventArgs e) {
            ClearDynamicControls(pagStats, fieldsByCategory);
            foreach (var parameter in controller.Effects.controllableParameters) {
                string paramName = parameter.Key;
                LabeledField field = new LabeledField(paramName, controller);
                field.lblName.Initialize(this, false);
                ToolTipTextLabel(field.lblName, parameter.Value.Description);

                if (controller.Character.paramValues.ContainsKey(paramName)) {
                    field.Value = controller.Character.paramValues[paramName];
                }
                string category = parameter.Value.Category;
                if (!fieldsByCategory.ContainsKey(category)) {
                    fieldsByCategory[category] = new List<LabeledField>();
                }
                fieldsByCategory[category].Add(field);
                pagStats.Controls.Add(field);
            }

            RefreshEffects();
        }

        private EventHandler ChooseVestige(int slotIndex, Vestige vestige) {
            return (object sender, EventArgs e) => {
                if (vestige == null) {
                    controller.SelectVestige(slotIndex, "", (Character.PactState) pactStates[slotIndex].SelectedIndex);
                    stateLabels[slotIndex].Text = "Good pact?";
                } else {
                    controller.SelectVestige(slotIndex, vestige.Name, (Character.PactState) pactStates[slotIndex].SelectedIndex);
                    stateLabels[slotIndex].Text = $"Good pact? (Binding DC {vestige.BindingDc})";
                }
            };
        }

        private EventHandler ChangePactState(int slotIndex) {
            return (object sender, EventArgs e) => {
                controller.ChangePactState(slotIndex, (Character.PactState)pactStates[slotIndex].SelectedIndex);
            };
        }

        private void VestigesTab_Enter(object sender, EventArgs e) {
            for (int i = 0; i < vestigePanels.Count; i++) {
                Panel vestigePanel = vestigePanels[i];
                vestigePanel.Controls.Remove(stateLabels[i]);
                vestigePanel.Controls.Remove(pactStates[i]);
                ClearDynamicControls(vestigePanel, vestigeChoices[i]);
            }
            stateLabels.Clear();
            pactStates.Clear();
            vestigeChoices.Clear();
            ClearDynamicControls(pagVestiges, vestigePanels);
            
            List<Vestige> availableVestiges = controller.AvailableVestiges();
            int numVestiges = GetStatValue(NUM_VESTIGES).value;

            int vestigeLeft = pnlVestige1.Left;
            int bindingCheckMod = GetStatValue(BINDING_CHECK).value;
            for (int i = 0; i < numVestiges; i++) {
                Panel vestigePanel = new Panel {
                    Top = pnlVestige1.Top,
                    Left = vestigeLeft,
                    Size = pnlVestige1.Size,
                    Anchor = pnlVestige1.Anchor,
                    AutoScroll = true
                };

                int? dc = controller.CurrentVestige(i)?.BindingDc;
                Label stateLabel = new Label {
                    Location = lblPactState.Location,
                    Text = (dc == null) ? "Pact state" : $"Pact state (Binding DC {dc})",
                    AutoSize = true
                };
                stateLabels.Add(stateLabel);
                vestigePanel.Controls.Add(stateLabel);
                Rollable(stateLabel, "binding check", $"1d20{bindingCheckMod.ToString("+#;-#;\"\"")}");

                ListBox state = new ListBox {
                    Location = lstPactState.Location,
                    Size = lstPactState.Size
                };
                state.Items.Add("Good pact");
                state.Items.Add("Poor pact");
                state.Items.Add("Defied");
                state.SelectedIndex = (int) controller.PactState(i);
                state.Click += ChangePactState(i);
                pactStates.Add(state);
                vestigePanel.Controls.Add(state);

                List<RadioButton> choices = new List<RadioButton>();
                int choiceTop = optVestige1.Top;

                string currentVestige = controller.CurrentVestige(i)?.Name ?? "";
                RadioButton noneChoice = new RadioButton {
                    Top = choiceTop,
                    Left = optVestige1.Left,
                    Text = "(None)",
                    Checked = "" == currentVestige
                };
                noneChoice.Click += ChooseVestige(i, null);
                choices.Add(noneChoice);
                vestigePanel.Controls.Add(noneChoice);
                choiceTop += vestigeChoiceBuffer;
                foreach (Vestige vestige in availableVestiges) {
                    RadioButton choice = new RadioButton {
                        Top = choiceTop,
                        Left = optVestige1.Left,
                        Text = vestige.Name,
                        Checked = vestige.Name == currentVestige
                    };
                    choice.Click += ChooseVestige(i, vestige);
                    choices.Add(choice);
                    vestigePanel.Controls.Add(choice);
                    choiceTop += vestigeChoiceBuffer;
                }
                vestigeChoices.Add(choices);

                vestigePanels.Add(vestigePanel);
                pagVestiges.Controls.Add(vestigePanel);
                vestigeLeft += vestigePanelBuffer;
            }
        }

        private void MainForm_Load(object sender, EventArgs e) {
            Font = scala;

            Rollable(lblTestRoll, "testing...", "1d7");

            // this "buffer" is actually the difference between the left side of the points label and
            // the left side of a hypothetical "extra" ranks box -- makes the math easier.
            skillPointsBuffer = lblPoints.Left - 2*(txtRanksClass.Left) + txtRanksCrossClass.Left;
            skillLabelOffset = lblSkill2.Left - lblSkill1.Left;

            leftBuffer = lblTopLeft.Left;
            topBuffer = lblTopLeft.Top + 5;
            verticalBufferLarge = 12;
            paramBuffer = lblParam2.Top - lblParam.Top;
            levelBuffer = cboBonusLanguage.Top - txtAutomaticLanguages.Top;
            levelUpBuffer = btnAddLevel.Top - cboClass.Top;
            skillBufferLeft = lblTotalRanks.Left - lblTotalHp.Left;
            transactionBuffer = lblTransactionAmount2.Top - lblTransactionAmount1.Top;
            vestigePanelBuffer = pnlVestige2.Left - pnlVestige1.Left;
            vestigeChoiceBuffer = optVestige2.Top - optVestige1.Top;

            // Remove all subsystem pages.  We will re-add them when the character gains access to the systems.
            RemoveSubsystemTabs();

            addItemDialog.Font = scala;

            controller = new Controller(new Character());
            controller.StartRollServer();
            levelDetailsDialog = new LevelDetailsDialog(controller) {
                Font = scala
            };
            availableFeats = controller.FeatDefinitions.Keys.ToList();
            availableFeats.Sort();

            List<string> availableRaces = controller.RaceDefinitions.Keys.ToList();
            availableRaces.Sort();
            foreach (string race in availableRaces) {
                cboRace.Items.Add(race);
            }

            name = new Label().Initialize(this);
            desc1 = new Label().Initialize(this);
            desc2 = new Label().Initialize(this);
            init = new Label().Initialize(this);
            listen = new Label().Initialize(this);
            spot = new Label().Initialize(this);
            evade = new Label().Initialize(this);
            languages = new Label().Initialize(this);
            beforeCombat = new List<List<List<Control>>> {
                new List<List<Control>> {
                    new List<Control> { name },
                    new List<Control> { desc1 },
                    new List<Control> { desc2 },
                    new List<Control> { languages },
                },
                new List<List<Control>> { new List<Control> { init } },
                senses,
                new List<List<Control>> { new List<Control> { listen, spot } },
                new List<List<Control>> { new List<Control> { evade } },
                evadeControls,
                downtime,
                condition,
            };

            ac = new Label().Initialize(this);
            touch = new Label().Initialize(this);
            acEffects = new Label().Initialize(this);
            currentHp = new Label().Initialize(this);
            maxHp = new Label().Initialize(this);
            dr = new Label().Initialize(this);
            hpEffects = new Label().Initialize(this);
            resist = new Label().Initialize(this);
            immune = new Label().Initialize(this);
            sr = new Label().Initialize(this);
            
            enemyTurn = new List<List<List<Control>>> {
                acControls,
                new List<List<Control>> {
                    new List<Control> { ac, touch },
                    new List<Control> { acEffects }, // in the spacing rework, move acControls here.
                    new List<Control> { currentHp, maxHp, dr },
                    new List<Control> { hpEffects },
                },
                hpControls, // put this above hpEffects
                defend,
                react,
                new List<List<Control>> {
                    new List<Control> { resist }, // resistControls here.
                    new List<Control> { immune }, // immuneControls here.
                },
                new List<List<Control>> { new List<Control> { sr } },
                srControls,
                saves
            };

            speed = new Label().Initialize(this);
            space = new Label().Initialize(this);
            reach = new Label().Initialize(this);
            bab = new Label().Initialize(this);
            grapple = new Label().Initialize(this);
            myTurn = new List<List<List<Control>>> {
                new List<List<Control>> {
                    new List<Control> { speed },
                },
                movement,
                new List<List<Control>> {
                    new List<Control> { space, reach },
                    new List<Control> { bab, grapple },
                },
                attacks,
                atkOptions,
                defOptions,
                specialActions,
                actions,
            };

            misc = new List<List<List<Control>>> {
                abilityScores,
                sq,
                skills,
                miscSkills,
            };

            whereFieldsGo = new Dictionary<string, List<List<Control>>>() {
                [SENSES] = senses,
                [EVADE] = evadeControls,
                [AC] = acControls,
                [HP] = hpControls,
                [REACT] = react,
                [RESIST] = resistControls,
                [IMMUNE] = immuneControls,
                [SR] = srControls,
                [MOVEMENT] = movement,
                [ACTIONS] = actions,
                [ATK_OPTIONS] = atkOptions,
                [DEF_OPTIONS] = defOptions,
                [SPECIAL_ACTIONS] = specialActions,
                [SQ] = sq,
                [SKILLS] = miscSkills,
                [CONDITION] = condition
            };

            new ToolTip().SetToolTip(lblRolledHp, "Die roll only (do not include CON bonus)");

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

            controller.WeaponDefinitions.Keys.OrderBy(x => x).ToList().ForEach(x => addItemDialog.lstAddWeapon.Items.Add(x));
            controller.WeaponPropertyDefinitions.Keys.OrderBy(x => x).ToList().ForEach(x => addItemDialog.lstWeaponProperties.Items.Add(x));
            controller.ArmorDefinitions.Keys.OrderBy(x => x).ToList().ForEach(x => addItemDialog.lstAddArmor.Items.Add(x));
            controller.ShieldDefinitions.Keys.OrderBy(x => x).ToList().ForEach(x => addItemDialog.lstAddShield.Items.Add(x));
            controller.ArmorPropertyDefinitions.Keys.OrderBy(x => x).ToList().ForEach(x => {
                addItemDialog.lstArmorProperties.Items.Add(x);
                addItemDialog.lstShieldProperties.Items.Add(x);
            });
            controller.ItemDefinitions.Keys.OrderBy(x => x).ToList().ForEach(x => addItemDialog.lstAddItem.Items.Add(x));
        }

        private void RemoveSubsystemTabs() {
            tabMain.TabPages.Remove(pagVestiges);
        }

        private void UpdateSubsystemTabs() {
            RemoveSubsystemTabs();

            const int SUBSYSTEMS_INDEX = 1;
            if (controller.HasVestiges()) {
                tabMain.TabPages.Insert(SUBSYSTEMS_INDEX, pagVestiges);

                // When you remove a control from a form, apparently all its events get thrown out?
                // Makes sense kinda, but it means we need to re-add here.
                pagVestiges.Enter += VestigesTab_Enter;
            }
        }

        private bool LoadCharacter() {
            using (OpenFileDialog dialog = new OpenFileDialog()) {
                dialog.InitialDirectory = "c:\\";
                if (dialog.ShowDialog() != DialogResult.OK) {
                    return false;
                }
                using (StreamReader reader = new StreamReader(dialog.OpenFile())) {
                    controller.Character = new Character(JObject.Parse(reader.ReadToEnd()));
                    UpdateSubsystemTabs();
                }
            }
            DisplayCharacter();
            InitializeInventory();
            return true;
        }

        private void LoadCharacter_Click(object sender, EventArgs e) {
            LoadCharacter();
        }

        private bool SaveCharacter() {
            SaveFileDialog saveCharacter = new SaveFileDialog {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                FilterIndex = 1
            };
            if (saveCharacter.ShowDialog() == DialogResult.OK) {
                controller.PrepCharacterForSave();
                File.WriteAllText(saveCharacter.FileName, controller.Character.ToJson().ToString());
                return true;
            }
            return false;
        }

        private void SaveCharacter_Click(object sender, EventArgs e) {
            SaveCharacter();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e) {
            if (!loaded) {
                return;
            }
            DialogResult r = MessageBox.Show("Save character first?", "Exiting", MessageBoxButtons.YesNoCancel);
            switch (r) {
                case DialogResult.Yes:
                    e.Cancel = !SaveCharacter();
                    break;
                case DialogResult.Cancel:
                    e.Cancel = true;
                    break;
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
        // Setting the max width of a tooltip doesn't have a convenient API, but the tooltip control *can* set its
        // maximum width by receiving a certain WinAPI message.
        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hwnd, uint msg, int wParam, int lParam);
        private const uint SET_MAX_TOOLTIP_WIDTH_MSG = 0x418;

        private static EventHandler ResizeTooltip(ToolTip toolTip) {
            return (object sender, EventArgs e) => {
                IntPtr hwnd = (IntPtr)typeof(ToolTip).InvokeMember(
                    "Handle",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.GetProperty,
                    null,
                    toolTip,
                    null
                );
                SendMessage(hwnd, SET_MAX_TOOLTIP_WIDTH_MSG, 0, 300);
            };
        }

        public static T Initialize<T>(this T label, MainForm form, bool addToStats = true) where T : Control {
            ToolTip toolTip = new ToolTip {
                AutoPopDelay = 32767 // maximum value.
            };

            label.AutoSize = true;
            label.MouseEnter += ResizeTooltip(toolTip);
            if (addToStats) {
                form.StatsTab().Controls.Add(label);
            }
            form.toolTips[label] = toolTip;
            
            return label;
        }
    }
}
