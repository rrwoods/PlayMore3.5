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

        private List<Label> bonusLabels = new List<Label>();
        private List<ComboBox> bonusLanguages = new List<ComboBox>();

        private static List<string> availableFeats = new List<string>();
        private List<ComboBox> feats = new List<ComboBox>();

        private List<ComboBox> increments = new List<ComboBox>();
        private List<ComboBox> levels = new List<ComboBox>();
        private List<TextBox> hpRolls = new List<TextBox>();
        private List<Label> skillNames = new List<Label>();
        private Dictionary<string, List<TextBox>> skillRanks = new Dictionary<string, List<TextBox>>();
        private List<Label> skillPoints = new List<Label>();
        private Dictionary<string, Label> totalSkillRanks = new Dictionary<string, Label>();
        private int levelBuffer, levelUpBuffer;
        private int skillBufferLeft, skillLabelOffset, skillPointsBuffer;

        // 1. each of these fields represents one of the four sections of the summary page.
        // 2. the outer list is list of subsections, with visual separation between each subsection.
        // 3. each subsection contains a list of rows
        // 4. each row is a list of labels
        private List<List<List<Label>>> beforeCombat;
        private List<List<List<Label>>> enemyTurn;
        private List<List<List<Label>>> myTurn;
        private List<List<List<Label>>> misc;

        private Label name, desc1, desc2, init;
        private List<List<Label>> senses = new List<List<Label>>();
        private Label listen, spot, evade, languages;
        private List<List<Label>> downtime = new List<List<Label>>();
        private List<List<Label>> condition = new List<List<Label>>();

        private Label ac, touch;
        private Label acEffects;
        private Label currentHp, maxHp, dr;
        private List<List<Label>> react = new List<List<Label>>();
        private Label resist, immune;
        private List<List<Label>> defend = new List<List<Label>>();
        private Label sr;
        private List<List<Label>> saves = new List<List<Label>>();

        private Label speed;
        private List<List<Label>> movement = new List<List<Label>>();
        private List<List<Label>> attacks = new List<List<Label>>();
        private Label space, reach, bab, grapple;
        private List<List<Label>> atkOptions = new List<List<Label>>();
        private List<List<Label>> actions = new List<List<Label>>();
        private List<List<Label>> specialActions = new List<List<Label>>();

        private List<List<Label>> abilityScores = new List<List<Label>>();
        private List<List<Label>> sq = new List<List<Label>>();
        private List<List<Label>> skills = new List<List<Label>>();
        private List<List<Label>> miscSkills = new List<List<Label>>();

        private List<CheckBox> toggles = new List<CheckBox>();
        private List<TextBox> paramBoxes = new List<TextBox>();
        private List<Label> paramLabels = new List<Label>();
        // Used to keep values of parameters around so that when we re-load the stats tab
        // we can fill them back out again -- otherwise it would (for example) forget how much
        // damage is on you when you check your inventory
        private Dictionary<string, int> paramValues = new Dictionary<string, int>();
        private HashSet<string> activeToggles = new HashSet<string>();

        private List<Label> slotBoxes;

        private Control dragging = null;
        private Size draggingFrom;
        public Dictionary<Label, ToolTip> toolTips = new Dictionary<Label, ToolTip>();

        private Dictionary<string, GroupBox> locationGroups = new Dictionary<string, GroupBox>();
        private Dictionary<string, ListBox> locationLists = new Dictionary<string, ListBox>();
        private Dictionary<string, CheckBox> locationWeights = new Dictionary<string, CheckBox>();
        private Control draggingItemFrom = null;
        private int numberDragging = 0;
        
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
            return statsTab;
        }

        private void ClearDynamicControls<T>(TabPage tab, List<T> controls) where T : Control {
            foreach (T control in controls) {
                tab.Controls.Remove(control);
            }
            controls.Clear();
        }

        private (int value, List<string> explanation) GetStatValue(string stat) {
            return controller.Effects.GetStat(stat).GetValue(controller.Effects, controller.EquippedItems);
        }

        private string ValueText(int value, bool plus) {
            return ((plus && value >= 0 ? "+" : "") + value).Replace("-", "–");
        }

        private void ToolTipStatLabel(Label label, List<string> explanation) {
            toolTips[label].SetToolTip(label, string.Join("\n", explanation));
        }

        private void ToolTipTextLabel(Label label, string description) {
            toolTips[label].SetToolTip(label, description);
        }

        private EventHandler RollStat(string dice, string description) {
            return (object sender, EventArgs e) => {
                controller.RollServer.Roll(dice, description);
            };
        }

        private void LabelStat(Label label, int value, List<string> explanation, string prefix, string stat, string postfix = "", bool plus = true) {
            string statValue = ValueText(value, plus);
            label.Text = $"{prefix} {statValue}{postfix}";
            ToolTipStatLabel(label, explanation);

            string modifier = value.ToString("+#;-#;\"\"");
            ToolStripMenuItem roll = new ToolStripMenuItem {
                Text = $"Roll {prefix} (1d20{modifier})"
            };
            roll.Click += RollStat($"1d20{modifier}", prefix);
            label.ContextMenuStrip = new ContextMenuStrip();
            label.ContextMenuStrip.Items.Add(roll);
        }

        private void LabelStat(Label label, string prefix, string stat, string postfix = "", bool plus = true) {
            var (value, explanation) = GetStatValue(stat);
            LabelStat(label, value, explanation, prefix, stat, postfix, plus);
        }

        private void DynamicStat(List<List<Label>> section, string prefix, string stat, string postfix = "", bool plus = true, int hideIfEqual = int.MinValue) {
            (int baseValue, List<string> baseExplanation) = GetStatValue(stat);
            IEnumerable<Effects.Statistic> specials = controller.Effects.RelevantStatsWithTag(stat, controller.EquippedItems);
            bool relevant =
                specials.Any() ||
                controller.Effects.GetStat(stat).Relevant(controller.Effects, controller.EquippedItems);
            if ((baseValue != hideIfEqual) && relevant) {
                Label label = new Label().Initialize(this);
                section.Add(new List<Label> { label });
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
                section.Add(new List<Label> { lblSpecial });
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

            abilityScores.Add(new List<Label> { lblScore, lblModifier });

            string checkStat = $"{ability} checks";
            DynamicStat(abilityScores, checkStat, checkStat, "", true, mValue);
        }

        private void RemoveDynamicSection(List<List<Label>> section) {
            section.ForEach(row => row.ForEach(label => {
                statsTab.Controls.Remove(label);
                toolTips.Remove(label);
            }));
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

            attacks.Add(new List<Label> { attackLabel, damageLabel, critLabel });
        }

        private void OneLineText(Label label, Dictionary<string, List<(string, string desc)>> textEffects, string key, string prefix = "") {
            if (textEffects[key].Any()) {
                label.Text = $"{prefix}{key} {string.Join(", ", textEffects[key].Select(x => x.desc))}";
            } else {
                label.Text = "";
            }
        }

        private void MultilineText(List<List<Label>> section, Dictionary<string, List<(string name, string desc)>> textEffects, string key, bool header = true) {
            RemoveDynamicSection(section);
            if (!textEffects[key].Any()) {
                return;
            }

            if (header) {
                Label headerLabel = new Label() {
                    Text = key
                }.Initialize(this);
                section.Add(new List<Label> { headerLabel });
                statsTab.Controls.Add(headerLabel);
            }
            foreach ((string name, string desc) in textEffects[key]) {
                Label effectLabel = new Label() {
                    Text = "    " + name
                }.Initialize(this);
                ToolTipTextLabel(effectLabel, desc);
                section.Add(new List<Label> { effectLabel });
                statsTab.Controls.Add(effectLabel);
            }
        }

        private void Text_Enter(object sender, EventArgs e) {
            // I have my suspicions that this will be inconsistent.
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
            HandleFeats();
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
            characterTab.Controls.Add(skillName);

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
            characterTab.Controls.Add(totalRanks);

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
                characterTab.Controls.Add(ranksBox);

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
            HandleFeats();
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
                HandleFeats();
                TextBox hp = hpRolls[levelIndex];
                if (!int.TryParse(hp.Text, out int _)) {
                    HitDieForLevel(hp, levelIndex);
                }
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
                characterTab.Controls.Add(bonusLabel);
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

                characterTab.Controls.Add(box);
                bonusLanguages.Add(box);

                langTop += langBuffer;
            }
            int excess = controller.Character.bonusLanguages.Count - numBonusLanguages;
            if (excess > 0) {
                controller.Character.bonusLanguages.RemoveRange(numBonusLanguages, excess);
            }
            for (int i = curBonusLanguages - 1; i >= numBonusLanguages; i--) {
                characterTab.Controls.Remove(bonusLanguages[i]);
                bonusLanguages.RemoveAt(i);
                characterTab.Controls.Remove(bonusLabels[i]);
                bonusLabels.RemoveAt(i);
            }
        }

        private void HandleFeats() {
            // For some reason, the race-selected method gets called before the character
            // is actually set up.
            if (!levels.Any()) {
                return;
            }
            ClearDynamicControls(characterTab, feats);
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
                    featLeft += featBox.Width + (cboFeat.Left - cboClass.Right);
                    characterTab.Controls.Add(featBox);

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

        private void DisplayLevels() {
            ClearDynamicControls(characterTab, skillNames);
            foreach (List<TextBox> boxes in skillRanks.Values) {
                foreach (TextBox box in boxes) {
                    characterTab.Controls.Remove(box);
                }
            }
            skillRanks.Clear();
            foreach (Label totalRanks in totalSkillRanks.Values) {
                characterTab.Controls.Remove(totalRanks);
            }
            totalSkillRanks.Clear();

            ClearDynamicControls(characterTab, increments);
            ClearDynamicControls(characterTab, levels);
            ClearDynamicControls(characterTab, hpRolls);
            ClearDynamicControls(characterTab, skillPoints);

            int levelTop = cboClass.Top;
            for (int i = 0; i < controller.Character.classLevels.Count; i++) {
                string @class = controller.Character.classLevels[i];
                ComboBox level = new ComboBox {
                    Top = levelTop,
                    Left = cboClass.Left,
                    Size = cboClass.Size
                };
                foreach (string className in controller.ClassDefinitions.Keys) {
                    level.Items.Add(className);
                }
                level.Text = @class;
                level.SelectedValueChanged += SelectClassLevel(i);
                levels.Add(level);
                characterTab.Controls.Add(level);

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
                characterTab.Controls.Add(rolledHp);

                Label points = new Label {
                    Top = levelTop,
                    Left = cboAddSkill.Left + (skillNames.Count() * skillLabelOffset),
                    Size = lblPoints.Size,
                    TextAlign = lblPoints.TextAlign,
                };
                skillPoints.Add(points);
                characterTab.Controls.Add(points);
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
                    characterTab.Controls.Add(increment);
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
            HandleFeats();

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

        private void RefreshEffects() {
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
            MultilineText(downtime, textEffects, DOWNTIME);
            MultilineText(condition, textEffects, CONDITION);

            LabelStat(ac, "AC", AC, ",", false);
            LabelStat(touch, "touch", TOUCH_AC, "", false);
            OneLineText(acEffects, textEffects, AC, "    ");
            LabelStat(currentHp, "hp", CURRENT_HP, ";", false);
            LabelStat(maxHp, "max", MAX_HP, " (" + controller.Character.classLevels.Count() + " HD)", false);
            OneLineText(dr, textEffects, DR);
            if (dr.Text.Any()) {
                maxHp.Text += ";";
            }
            MultilineText(react, textEffects, REACT);
            OneLineText(resist, textEffects, RESIST);
            OneLineText(immune, textEffects, IMMUNE);
            MultilineText(defend, textEffects, DEFEND);
            OneLineText(sr, textEffects, SR);
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
                attacks.Add(new List<Label> { new Label() { Text = "Full Attack" }.Initialize(this) });
                foreach (Attack attack in fullAttack) {
                    AttackLabels(attack, true);
                }
            }

            LabelStat(space, "Space", SPACE, " ft.;", false);
            LabelStat(reach, "Reach", REACH, " ft.", false);
            LabelStat(bab, "Base Atk", BASE_ATTACK_BONUS, ";");
            LabelStat(grapple, "Grp", GRAPPLE);
            MultilineText(atkOptions, textEffects, ATK_OPTIONS);
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

        private void RepositionRow(List<Label> row) {
            if (row.Count > 1) {
                int left = row[0].Right;
                int top = row[0].Top;
                foreach (Label label in row.Skip(1)) {
                    if (label.Text.Any()) {
                        label.Top = top;
                        label.Left = left;
                        left = label.Right;
                    }
                }
            }
        }

        private void RepositionColumn(int columnLeft, List<List<List<Label>>> column) {
            int top = topBuffer;
            foreach (List<List<Label>> section in column) {
                bool anyText = false;
                foreach (List<Label> row in section) {
                    if (!row.Any()) {
                        continue;
                    }
                    Label first = row[0];
                    if (first.Text.Any()) {
                        anyText = true;
                        first.Left = columnLeft;
                        first.Top = top;
                        first.MaximumSize = new Size(columnWidth - 2 * leftBuffer, 1000);
                        top = first.Bottom;
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

        private void DisplayGold() {
            ClearDynamicControls(inventoryTab, transactionAmounts);
            ClearDynamicControls(inventoryTab, transactionDescriptions);

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
                inventoryTab.Controls.Add(descLabel);


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
                inventoryTab.Controls.Add(gpLabel);

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

            lblTotalGold.Text = $"{controller.Character.TotalGold()} gp";
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
            foreach (string locationKey in locationGroups.Keys.ToList()) {
                GroupBox locationGroup = locationGroups[locationKey];

                locationGroup.Controls.Remove(locationLists[locationKey]);
                locationLists.Remove(locationKey);

                locationGroup.Controls.Remove(locationWeights[locationKey]);
                locationWeights.Remove(locationKey);

                inventoryTab.Controls.Remove(locationGroup);
                locationGroups.Remove(locationKey);
            }

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
                locationGroups[location.Key] = locationBox;
                locationLeft += locationBox.Width + locationBuffer;
            }
            DisplayGold();
            RefreshEquipment();
        }

        private void MainForm_Resize(object sender, EventArgs e) {
            RepositionLabels();
        }

        private void Toggle_Click(object sender, EventArgs e) {
            CheckBox toggle = (CheckBox)sender;
            string flag = toggle.Text;
            controller.Effects.controllableFlags[flag] = toggle.Checked;
            if (toggle.Checked) {
                activeToggles.Add(flag);
            } else {
                activeToggles.Remove(flag);
            }

            RefreshEffects();
        }

        private void Param_Leave(object sender, EventArgs e) {
            TextBox box = (TextBox)sender;
            if (int.TryParse(box.Text, out int value)) {
                box.BackColor = SystemColors.Window;
                string parameterName = (string)box.Tag;
                controller.Effects.controllableAmounts[parameterName] = value;
                paramValues[parameterName] = value;
                RefreshEffects();
            } else if (box.Text.Equals("")) {
                box.BackColor = SystemColors.Window;
                string parameterName = (string)box.Tag;
                controller.Effects.controllableAmounts[parameterName] = 0;
                paramValues.Remove(parameterName);
                RefreshEffects();
            } else {
                box.BackColor = Color.Red;
            }
        }

        private void StatsTab_Enter(object sender, EventArgs e) {
            controller.Reload();

            ClearDynamicControls(statsTab, toggles);
            ClearDynamicControls(statsTab, paramBoxes);
            ClearDynamicControls(statsTab, paramLabels);

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
                if (paramValues.ContainsKey(parameter)) {
                    txtNewParam.Text = "" + paramValues[parameter];
                }
                txtNewParam.Leave += Param_Leave;
                paramBoxes.Add(txtNewParam);
                statsTab.Controls.Add(txtNewParam);

                Label lblNewParam = new Label() {
                    Top = lblTop,
                    Left = lblParam.Left,
                    AutoSize = true,
                    Anchor = AnchorStyles.Bottom | AnchorStyles.Left
                };
                lblNewParam.Text = parameter;
                paramLabels.Add(lblNewParam);
                statsTab.Controls.Add(lblNewParam);

                txtTop += paramBuffer;
                lblTop += paramBuffer;
            }
            foreach (var param in paramValues) {
                if (controller.Effects.controllableAmounts.ContainsKey(param.Key)) {
                    controller.Effects.controllableAmounts[param.Key] = param.Value;
                }
            }

            foreach (string toggle in controller.Effects.controllableFlags.Keys) {
                CheckBox chkNewToggle = new CheckBox() {
                    Top = txtTop,
                    Left = txtParam.Left,
                    AutoSize = true,
                    Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                    Text = toggle
                };
                if (activeToggles.Contains(toggle)) {
                    chkNewToggle.Checked = true;
                }
                chkNewToggle.Click += Toggle_Click;
                toggles.Add(chkNewToggle);
                statsTab.Controls.Add(chkNewToggle);

                txtTop += paramBuffer;
            }
            foreach (string toggle in activeToggles) {
                if (controller.Effects.controllableFlags.ContainsKey(toggle)) {
                    controller.Effects.controllableFlags[toggle] = true;
                }
            }

            RefreshEffects();
        }

        private void MainForm_Load(object sender, EventArgs e) {
            Font = scala;

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

            controller = new Controller(new Character());
            controller.StartRollServer();

            List<string> availableRaces = controller.RaceDefinitions.Keys.ToList();
            availableRaces.Sort();
            foreach (string race in availableRaces) {
                cboRace.Items.Add(race);
            }

            availableFeats = controller.FeatDefinitions.Keys.ToList();
            availableFeats.Sort();

            name = new Label().Initialize(this);
            desc1 = new Label().Initialize(this);
            desc2 = new Label().Initialize(this);
            init = new Label().Initialize(this);
            listen = new Label().Initialize(this);
            spot = new Label().Initialize(this);
            evade = new Label().Initialize(this);
            languages = new Label().Initialize(this);
            beforeCombat = new List<List<List<Label>>> {
                new List<List<Label>> {
                    new List<Label> { name },
                    new List<Label> { desc1 },
                    new List<Label> { desc2 },
                    new List<Label> { languages },
                },
                new List<List<Label>> { new List<Label> { init } },
                senses,
                new List<List<Label>> { new List<Label> { listen, spot } },
                new List<List<Label>> { new List<Label> { evade } },
                downtime,
                condition,
            };

            ac = new Label().Initialize(this);
            touch = new Label().Initialize(this);
            acEffects = new Label().Initialize(this);
            currentHp = new Label().Initialize(this);
            maxHp = new Label().Initialize(this);
            dr = new Label().Initialize(this);
            resist = new Label().Initialize(this);
            immune = new Label().Initialize(this);
            sr = new Label().Initialize(this);
            
            enemyTurn = new List<List<List<Label>>> {
                new List<List<Label>> {
                    new List<Label> { ac, touch },
                    new List<Label> { acEffects },
                    new List<Label> { currentHp, maxHp, dr },
                },
                defend,
                react,
                new List<List<Label>> {
                    new List<Label> { resist },
                    new List<Label> { immune },
                },
                new List<List<Label>> { new List<Label> { sr } },
                saves
            };

            speed = new Label().Initialize(this);
            space = new Label().Initialize(this);
            reach = new Label().Initialize(this);
            bab = new Label().Initialize(this);
            grapple = new Label().Initialize(this);
            myTurn = new List<List<List<Label>>> {
                new List<List<Label>> {
                    new List<Label> { speed },
                },
                movement,
                new List<List<Label>> {
                    new List<Label> { space, reach },
                    new List<Label> { bab, grapple },
                },
                attacks,
                atkOptions,
                specialActions,
                actions,
            };

            misc = new List<List<List<Label>>> {
                abilityScores,
                sq,
                skills,
                miscSkills,
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
        }

        private bool LoadCharacter() {
            using (OpenFileDialog dialog = new OpenFileDialog()) {
                dialog.InitialDirectory = "c:\\";
                if (dialog.ShowDialog() != DialogResult.OK) {
                    return false;
                }
                using (StreamReader reader = new StreamReader(dialog.OpenFile())) {
                    controller.Character = new Character(JObject.Parse(reader.ReadToEnd()));
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

        public static Label Initialize(this Label label, MainForm form) {
            ToolTip toolTip = new ToolTip {
                AutoPopDelay = 32767 // maximum value.
            };

            label.AutoSize = true;
            label.MouseEnter += ResizeTooltip(toolTip);
            form.StatsTab().Controls.Add(label);
            form.toolTips[label] = toolTip;
            
            return label;
        }
    }
}
