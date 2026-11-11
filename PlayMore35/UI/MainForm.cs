using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Global;
using PlayMore35.Items;
using PlayMore35.Subsystems;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static PlayMore35.Global.FormUtils;
using static PlayMore35.Effects.Constants;
using static PlayMore35.Properties.Resources;
using PlayMore35.Effects;
using PlayMore35.Rules;
using PlayMore35.UI;
using PlayMore35.UI.Subsystems;

namespace PlayMore35 {
    public partial class MainForm : Form, ISubsystemUIHost {
        [DllImport("gdi32.dll")]
        private static extern IntPtr AddFontMemResourceEx(IntPtr pbFont, uint cbFont, IntPtr pdv, [In] ref uint pcFonts);
        private PrivateFontCollection fonts = new PrivateFontCollection();
        Font scala;

        private bool loaded = false;

        private int locationWidthBuffer;
        private int transactionBuffer;

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

        private readonly SubsystemUIRegistry uiRegistry;
        private readonly Dictionary<string, TabPage> activeSubsystemTabs = new Dictionary<string, TabPage>(StringComparer.OrdinalIgnoreCase);

        private List<DynamicPanel> dynamicPanels = new List<DynamicPanel>();

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
        
        private ICharacterSession session;
        public ICharacterSession Session => session;

        public void RequestRecalculate() {
            session?.Recalculate();
        }

        public void RequestEffectsRefresh() {
            RefreshEffects();
        }

        public void RequestTimelineRefresh() {
            timelinePanel?.RefreshTimeline();
        }

        private readonly string? initialCharacterPath;
        private string? lastCharacterDirectory;

        public MainForm(ICharacterSession? session = null, SubsystemUIRegistry? uiRegistry = null, string? initialCharacterPath = null) {
            this.session = session;
            this.uiRegistry = uiRegistry ?? SubsystemUIRegistry.CreateDefault();
            this.initialCharacterPath = initialCharacterPath;
            InitializeComponent();

            ledgerXp.AlignInputControls(dtpSessionDate, txtSessionDesc, txtSessionXp, btnAddSession);
            UpdateXpLedgerBounds();
            pagCharacter.Resize += (s, e) => UpdateXpLedgerBounds();
            ledgerGold.AlignInputControls(dtpTransactionDate, txtTransactionDesc, txtTransactionGp, btnAddTransaction);
            txtLocationName.Left = ledgerGold.Left - txtLocationName.Width - 24;
            btnAddLocation.Left = txtLocationName.Left;

            explorationPanel.Category = EXPLORATION;
            explorationPanel.SkillDeclarations.Add(new SkillDeclaration(SURVIVAL));
            explorationPanel.SkillDeclarations.Add(new SkillDeclaration(SEARCH));

            socialPanel.Category = SOCIAL;
            socialPanel.SkillDeclarations.Add(new SkillDeclaration(DIPLOMACY));
            socialPanel.SkillDeclarations.Add(new SkillDeclaration(BLUFF));

            sensesPanel.Category = SENSES;
            sensesPanel.SkillDeclarations.Add(new SkillDeclaration(LISTEN, disableCondition: "deafened", disableReason: "Deafened"));
            sensesPanel.SkillDeclarations.Add(new SkillDeclaration(SPOT, disableCondition: "blinded", disableReason: "Blinded"));

            IntPtr fontPtr = Marshal.AllocCoTaskMem(Scala_SansRegular_LF.Length);
            Marshal.Copy(Scala_SansRegular_LF, 0, fontPtr, Scala_SansRegular_LF.Length);
            fonts.AddMemoryFont(fontPtr, Scala_SansRegular_LF.Length);
            uint dummy = 0;
            AddFontMemResourceEx(fontPtr, (uint)Scala_SansRegular_LF.Length, IntPtr.Zero, ref dummy);
            Marshal.FreeCoTaskMem(fontPtr);
            scala = new Font(fonts.Families[0], 9.75F);

            if (this.session != null) {
                UpdateSubsystemTabs();
            }
        }

        private void Text_Enter(object sender, EventArgs e) {
            BeginInvoke((Action)delegate {
                ((TextBox)sender).SelectAll();
            });
        }

        private void Name_Leave(object sender, EventArgs e) {
            session.Mutate(c => c.name = txtName.Text);
        }

        private void Alignment_Leave(object sender, EventArgs e) {
            session.Mutate(c => c.alignment = txtAlignment.Text);
        }

        private void Gender_Leave(object sender, EventArgs e) {
            session.Mutate(c => c.gender = txtGender.Text);
        }

        private void Race_SelectedValueChanged(object sender, EventArgs e) {
            session.Mutate(c => c.race = cboRace.Text);
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
            string scoreName = (string)txt.Tag;
            if (int.TryParse(txt.Text, out int scoreValue)) {
                session.Mutate(c => c.assignedAbilityScores[scoreName] = scoreValue);
            } else {
                session.Mutate(c => c.assignedAbilityScores[scoreName] = 0);
                txt.Text = "";
            }
            DisplayTotalAbilityScores();
            HandleLanguages();
            DisplayTotalHp();
            for (int i = 0; i < session.Character.classLevels.Count; i++) {
                DisplaySkillPoints(i);
            }
        }

        private void DisplayRacialAbilityModifiers() {
            const string MODIFIER_FORMAT = "+#;–#;\"\"";

            Race race = session.Catalog.Races[session.Character.race];
            txtRacialStr.Text = race.Str.ToString(MODIFIER_FORMAT);
            txtRacialDex.Text = race.Dex.ToString(MODIFIER_FORMAT);
            txtRacialCon.Text = race.Con.ToString(MODIFIER_FORMAT);
            txtRacialInt.Text = race.Int.ToString(MODIFIER_FORMAT);
            txtRacialWis.Text = race.Wis.ToString(MODIFIER_FORMAT);
            txtRacialCha.Text = race.Cha.ToString(MODIFIER_FORMAT);
        }

        private void DisplayTotalAbilityScores() {
            txtFinalStr.Text = session.GetStatistic(STR_SCORE).Value.ToString();
            txtFinalDex.Text = session.GetStatistic(DEX_SCORE).Value.ToString();
            txtFinalCon.Text = session.GetStatistic(CON_SCORE).Value.ToString();
            txtFinalInt.Text = session.GetStatistic(INT_SCORE).Value.ToString();
            txtFinalWis.Text = session.GetStatistic(WIS_SCORE).Value.ToString();
            txtFinalCha.Text = session.GetStatistic(CHA_SCORE).Value.ToString();
        }

        private EventHandler SelectBonusLanguage(int i) {
            return (object sender, EventArgs e) => {
                session.Mutate(c => c.bonusLanguages[i] = ((ComboBox)sender).Text);
            };
        }

        private EventHandler SelectFeat(ProgressionSlotKey slot) {
            return (object sender, EventArgs e) => {
                ComboBox featBox = ((ComboBox)sender);
                session.SetFeat(slot, featBox.Text);
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
                Text = ShortName(skill),
                Tag = skill,
            };
            TOOLTIP.SetToolTip(skillName, skill);

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
                i < session.Character.classLevels.Count;
                i++, levelTop += levelBuffer
            ) {
                TextBox ranksBox = new TextBox {
                    Top = levelTop,
                    Left = left,
                    Size = txtRanksCrossClass.Size,
                    TextAlign = txtRanksCrossClass.TextAlign,
                    BackColor = session.IsClassSkill(skill, i)
                        ? txtRanksClass.BackColor
                        : txtRanksCrossClass.BackColor,
                };
                if (i < session.Character.skillRanks[skill].Count) {
                    int ranks = session.Character.skillRanks[skill][i];
                    ranksBox.Text = ranks.ToString("#;#;\"\"");
                } else {
                    session.Mutate(c => c.skillRanks[skill].Add(0));
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

            session.Mutate(c => c.skillRanks[skill] = Enumerable.Repeat(0, c.classLevels.Count).ToList());
            AddSkillColumn(
                skill,
                skillNames.Any() ? skillNames.Last().Left + skillLabelOffset : cboAddSkill.Left
            );
        }

        private void AddLevel_Click(object sender, EventArgs e) {
            session.AddLevel();
            DisplayLevels();
            DisplayFeats();
            DisplayXp();
        }

        private EventHandler SelectAbilityIncrement(int levelIndex) {
            return (object sender, EventArgs e) => {
                session.Mutate(c => c.abilityScoreIncrements[levelIndex + 1] = ((ComboBox)sender).Text);
                DisplayTotalHp();
                for (int i = levelIndex; i < session.Character.classLevels.Count; i++) {
                    DisplaySkillPoints(i);
                }
            };
        }

        private EventHandler SelectClassLevel(int levelIndex) {
            return (object sender, EventArgs e) => {
                session.Mutate(c => c.classLevels[levelIndex] = ((ComboBox)sender).Text);

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
            box.Text = "d" + session.Catalog.Classes[session.Character.classLevels[levelIndex]].HitDie;
            box.Font = new Font(box.Font, FontStyle.Italic);
        }

        private EventHandler SetHp(int levelIndex) {
            return (object sender, EventArgs e) => {
                TextBox box = (TextBox)sender;
                try {
                    int roll = int.Parse(box.Text);
                    session.Mutate(c => c.rolledHitDice[levelIndex] = roll);
                    box.Font = new Font(box.Font, FontStyle.Regular);
                } catch (FormatException) {
                    session.Mutate(c => c.rolledHitDice[levelIndex] = 0);
                    HitDieForLevel(box, levelIndex);
                }
                DisplayTotalHp();
            };
        }

        private void CheckRanks(string skill, int levelIndex = 0) {
            for (int i = levelIndex; i < session.Character.classLevels.Count; i++) {
                TextBox ranksBox = skillRanks[skill][i];
                if (session.TooManyRanks(skill, i)) {
                    // Adding spaces around the text makes the strikethrough more apparent, and doesn't
                    // mess with the int.Parse at all!
                    ranksBox.Text = $"  {session.Character.skillRanks[skill][i]}  ";
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
                    int ranks = int.Parse(ranksBox.Text);
                    session.Mutate(c => c.skillRanks[skill][levelIndex] = ranks);
                    CheckRanks(skill, levelIndex);
                } catch (FormatException) {
                    session.Mutate(c => c.skillRanks[skill][levelIndex] = 0);
                    ranksBox.Text = "";
                }
                DisplaySkillPoints(levelIndex);
                DisplaySkillRanks(skill);
            };
        }

        private void HandleLanguages() {
            Race race = session.Catalog.Races[session.Character.race];
            txtAutomaticLanguages.Text = string.Join(", ", race.AutomaticLanguages);

            int numBonusLanguages = Math.Max(0, session.GetStartingIntelligence() / 2 - 5);
            int curBonusLanguages = bonusLanguages.Count;
            int langBuffer = cboBonusLanguage.Top - txtAutomaticLanguages.Top;
            int langTop = cboBonusLanguage.Top + langBuffer * curBonusLanguages;
            for (int i = 0; i < curBonusLanguages; i++) {
                bonusLanguages[i].Items.Clear();
                foreach (string language in session.Catalog.Races[session.Character.race].BonusLanguages) {
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
                foreach (string language in session.Catalog.Races[session.Character.race].BonusLanguages) {
                    box.Items.Add(language);
                }
                if (session.Character.bonusLanguages.Count > i) {
                    box.Text = session.Character.bonusLanguages[i];
                } else {
                    session.Mutate(c => c.bonusLanguages.Add(""));
                }

                box.SelectedValueChanged += SelectBonusLanguage(i);

                pagCharacter.Controls.Add(box);
                bonusLanguages.Add(box);

                langTop += langBuffer;
            }
            int excess = session.Character.bonusLanguages.Count - numBonusLanguages;
            if (excess > 0) {
                session.Mutate(c => c.bonusLanguages.RemoveRange(numBonusLanguages, excess));
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
            foreach (var level in session.GetFeatSlots()) {
                int featTop = levels[level.Key - 1].Top;
                int featLeft = cboFeat.Left;
                foreach (ProgressionSlotKey slot in level.Value) {
                    ComboBox featBox = new ComboBox {
                        AutoCompleteMode = AutoCompleteMode.Append,
                        AutoCompleteSource = AutoCompleteSource.ListItems,
                        Top = featTop,
                        Left = featLeft,
                        Size = cboFeat.Size,
                        Font = scala, // why do i need to do this?
                        Tag = slot
                    };
                    string? featName = session.GetFeat(slot);
                    if (!string.IsNullOrEmpty(featName)) {
                        featBox.Text = featName;
                        featBox.Font = new Font(featBox.Font, FontStyle.Regular);
                    } else {
                        featBox.Text = slot.Source;
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
            lblTotalHp.Text = "" + session.BaseHp();
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

            int remaining = session.SkillPointsRemainingAtLevel(levelIndex);
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
                skillRanks.Value[levelIndex].BackColor = session.IsClassSkill(skillRanks.Key, levelIndex)
                    ? txtRanksClass.BackColor
                    : txtRanksCrossClass.BackColor;
            };
        }

        private void DisplaySkillRanks(string skill) {
            totalSkillRanks[skill].Text = "" + session.TotalSkillRanks(skill);
        }

        private EventHandler PopupLevelDetails(int levelIndex) {
            return (object sender, EventArgs e) => {
                levelDetailsDialog.PrepareLevel(levelIndex);
                levelDetailsDialog.ShowDialog();
                levelDetailsDialog.SaveChanges();

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
            for (int i = 0; i < session.Character.classLevels.Count; i++) {
                string @class = session.Character.classLevels[i];
                ComboBox level = new ComboBox {
                    Top = levelTop,
                    Left = cboClass.Left,
                    Size = cboClass.Size
                };
                foreach (string className in session.Catalog.Classes.Keys.OrderBy(x => x)) {
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
                if (i < session.Character.rolledHitDice.Count) {
                    int roll = session.Character.rolledHitDice[i];
                    if (roll == 0) {
                        HitDieForLevel(rolledHp, i);
                    } else {
                        rolledHp.Text = "" + roll;
                    }
                } else {
                    session.Mutate(c => c.rolledHitDice.Add(0));
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
                    if (session.Character.abilityScoreIncrements.ContainsKey(i + 1)) {
                        increment.Text = session.Character.abilityScoreIncrements[i + 1];
                    }
                    increment.SelectedValueChanged += SelectAbilityIncrement(i);
                    increments.Add(increment);
                    pagCharacter.Controls.Add(increment);
                }

                levelTop += levelBuffer;
            }
            btnAddLevel.Top = levelTop;
            UpdateXpLedgerBounds();

            int skillLeft = cboAddSkill.Left;
            cboAddSkill.Left = skillLeft;
            int skillIndex = 0;
            foreach (var skill in session.Character.skillRanks) {
                AddSkillColumn(skill.Key, skillLeft);

                skillIndex++;
                skillLeft += skillLabelOffset;
            }

            DisplayTotalHp();
            foreach (Label skill in skillNames) {
                DisplaySkillRanks((string)skill.Tag);
            }
        }

        private void UpdateXpLedgerBounds() {
            ledgerXp.UpdateVerticalBounds(btnAddLevel.Top, txtSessionDesc.Top - 6);
        }

        private void DisplayXp(bool scrollToBottom = true) {
            int xpNeeded = session.Character.ExperienceToNextLevel();
            int nextLevel = session.Character.classLevels.Count + 1;
            btnAddLevel.Text = xpNeeded <= 0
                ? "Level Up!"
                : $"{xpNeeded.ToString("#,###")} XP to level {nextLevel}";

            var sessions = session.Character.sessions
                .OrderBy(s => s.GameTime)
                .ThenBy(s => s.RealDate)
                .ToList();

            ledgerXp.SetEntries(
                sessions,
                s => s.GameTime,
                s => s.RealDate,
                s => s.Description,
                s => LedgerFormatters.FormatXpAmount(s.Xp),
                s => {
                    session.Mutate(c => c.sessions.Remove(s));
                    DisplayXp();
                }
            );

            if (scrollToBottom) {
                ledgerXp.ScrollToBottom();
            }

            lblTotalXp.Text = LedgerFormatters.FormatTotalXp(session.Character.TotalExperience());
        }

        private void AddSession_Click(object sender, EventArgs e) {
            if (int.TryParse(txtSessionXp.Text, out int value)) {
                txtSessionXp.BackColor = SystemColors.Window;
                DateTime gameTime = session.Character.Timeline.CurrentTime;
                DateTime realDate = dtpSessionDate.SelectedDate;
                session.Mutate(c => c.sessions.Add(new XpSessionEntry(value, txtSessionDesc.Text, gameTime, realDate)));
                txtSessionDesc.Text = "";
                txtSessionXp.Text = "";
                dtpSessionDate.Reset();
                txtSessionDesc.Focus();
                DisplayXp(scrollToBottom: true);
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
            txtName.Text = session.Character.name;
            txtAlignment.Text = session.Character.alignment;
            txtGender.Text = session.Character.gender;

            txtAssignedStr.Text = "" + session.Character.assignedAbilityScores[STR];
            txtAssignedDex.Text = "" + session.Character.assignedAbilityScores[DEX];
            txtAssignedCon.Text = "" + session.Character.assignedAbilityScores[CON];
            txtAssignedInt.Text = "" + session.Character.assignedAbilityScores[INT];
            txtAssignedWis.Text = "" + session.Character.assignedAbilityScores[WIS];
            txtAssignedCha.Text = "" + session.Character.assignedAbilityScores[CHA];

            DisplayRacialAbilityModifiers();
            DisplayTotalAbilityScores();

            HandleLanguages();

            DisplayLevels();
            cboAddSkill.Items.Clear();
            foreach (string skill in SKILLS_LIST) {
                cboAddSkill.Items.Add(skill);
            }
            foreach (string skill in session.Character.skillRanks.Keys) {
                cboAddSkill.Items.Remove(skill);
            }
            DisplayFeats();
            DisplayXp();

            cboRace.Text = session.Character.race;
        }

        private void CharacterTab_Enter(object sender, EventArgs e) {
            if (!loaded) {
                return;
            }

            DisplayCharacter();
        }

        public void RefreshEffects() {
            session.ForceHiddenFlagsOff();

            Dictionary<string, List<Text>> textEffects = session.GetTextEffects();
            foreach (DynamicPanel p in dynamicPanels) {
                p.Update(textEffects);
            }

            loaded = true;
            RefreshWeight();
            UpdateSubsystemTabs();
        }

        private void RefreshWeight() {
            foreach (var weight in locationWeights) {
                weight.Value.Text = $"Count weight ({session.WeightAtLocation(weight.Key)} lbs.)";
            }

            lblEquippedWeight.Text = $"Equipped weight: {session.EquippedWeight} lbs.";
            lblTotalWeight.Text = $"Total load: {session.TotalWeight} lbs.";
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
            foreach (var location in session.Character.inventory) {
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
                    session.AddWeapon(locationName, (string)addItemDialog.lstAddWeapon.SelectedItem, properties);
                } else if (addItemDialog.tabAddItem.SelectedTab == addItemDialog.pagArmor) {
                    List<string> properties = new List<string>();
                    foreach (object o in addItemDialog.lstArmorProperties.SelectedItems) {
                        properties.Add((string)o);
                    }
                    session.AddArmor(locationName, (string)addItemDialog.lstAddArmor.SelectedItem, properties);
                } else if (addItemDialog.tabAddItem.SelectedTab == addItemDialog.pagShields) {
                    List<string> properties = new List<string>();
                    foreach (object o in addItemDialog.lstShieldProperties.SelectedItems) {
                        properties.Add((string)o);
                    }
                    session.AddShield(locationName, (string)addItemDialog.lstAddShield.SelectedItem, properties);
                } else if (addItemDialog.tabAddItem.SelectedTab == addItemDialog.pagItems) {
                    session.AddItem(locationName, (string)addItemDialog.lstAddItem.SelectedItem);
                }
                
                ListLocation(location, locationLists[locationName], locationGroups[locationName]);
                RefreshWeight();
            };
        }

        private EventHandler DeleteLocation(string locationName) {
            return (object sender, EventArgs e) => {
                session.RemoveLocation(locationName);
                RemoveLocationGroup(locationName);
            };
        }

        private EventHandler DeleteItemFrom(string locationName) {
            return (object sender, EventArgs e) => {
                ListBox locationList = locationLists[locationName];
                if (!(locationList.SelectedItem is ItemDrag selected)) {
                    return;
                }
                session.RemoveThing(locationName, selected.item);
                ListLocation(session.Character.inventory[locationName], locationList, locationGroups[locationName]);
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
                session.CountWeight(location, ((CheckBox)sender).Checked);
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
                    session.UnequipSlotlessItem(targetList.Parent.Text, (string)item.item);
                } else {
                    session.MoveItem(fromList.Parent.Text, to, item.item, numberDragging);
                }
            } else if (draggingItemFrom is Label fromSlot) {
                if (fromSlot == txtPrimaryWeapon) {
                    session.UnequipWeapon(to, true);
                } else if (fromSlot == txtOffHandWeapon) {
                    session.UnequipWeapon(to, false);
                } else if (fromSlot == txtArmor) {
                    session.UndonArmor(to);
                } else if (fromSlot == txtShield) {
                    session.UnequipShield(to);
                } else {
                    session.UnequipItem(item.slot, to);
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
                    ? session.Character.equipped.PrimaryHandWeapon
                    : session.Character.equipped.OffHandWeapon
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
                session.SwapWeapons();
            } else {
                bool primary = sender == txtPrimaryWeapon;
                session.EquipWeapon(draggingItemFrom.Parent.Text, primary, (WeaponChoice)item.item);
            }
            draggingItemFrom = null;
            RefreshInventory();
        }

        // Begin dragging donned armor
        private void Armor_MouseDown(object sender, MouseEventArgs e) {
            draggingItemFrom = (Control)sender;
            draggingItemFrom.DoDragDrop(
                new ItemDrag { item = session.Character.equipped.DonnedArmor },
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

            session.DonArmor(draggingItemFrom.Parent.Text, (ArmorChoice)item.item);
            draggingItemFrom = null;
            RefreshInventory();
        }

        // Begin dragging equipped shield
        private void Shield_MouseDown(object sender, MouseEventArgs e) {
            draggingItemFrom = (Control)sender;
            draggingItemFrom.DoDragDrop(
                new ItemDrag { item = session.Character.equipped.OffHandShield },
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

            session.EquipShield(draggingItemFrom.Parent.Text, armor);
            draggingItemFrom = null;
            RefreshInventory();
        }

        // Begin dragging an item from a slot
        private void Slot_MouseDown(object sender, MouseEventArgs e) {
            draggingItemFrom = (Control)sender;
            draggingItemFrom.DoDragDrop(
                new ItemDrag {
                    item = draggingItemFrom.Text, // is this cheating?
                    slot = (Equipped.Slot) draggingItemFrom.Tag
                }, 
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
            session.EquipItem(draggingItemFrom.Parent.Text, slot, item);
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
            session.EquipItem(draggingItemFrom.Parent.Text, Equipped.Slot.SLOTLESS, (string)item.item);
            draggingItemFrom = null;
            RefreshInventory();
        }

        private void RefreshInventory() {
            foreach (var location in session.Character.inventory) {
                ListLocation(location.Value, locationLists[location.Key], locationGroups[location.Key]);
            }
            RefreshEquipment();
        }

        private void DisplayGold(bool scrollToBottom = true) {
            var transactions = session.Character.transactions
                .OrderBy(t => t.GameTime)
                .ThenBy(t => t.RealDate)
                .ToList();

            ledgerGold.SetEntries(
                transactions,
                t => t.GameTime,
                t => t.RealDate,
                t => t.Description,
                t => LedgerFormatters.FormatGoldAmount(t.Amount),
                t => {
                    session.Mutate(c => c.transactions.Remove(t));
                    DisplayGold();
                }
            );

            if (scrollToBottom) {
                ledgerGold.ScrollToBottom();
            }

            lblTotalGold.Text = LedgerFormatters.FormatTotalGold(session.Character.TotalGold());
        }

        private void AddTransaction_Click(object sender, EventArgs e) {
            if (decimal.TryParse(txtTransactionGp.Text, out decimal value)) {
                txtTransactionGp.BackColor = SystemColors.Window;
                DateTime gameTime = session.Character.Timeline.CurrentTime;
                DateTime realDate = dtpTransactionDate.SelectedDate;
                session.Mutate(c => c.transactions.Add(new GpTransaction(value, txtTransactionDesc.Text, gameTime, realDate)));
                txtTransactionDesc.Text = "";
                txtTransactionGp.Text = "";
                dtpTransactionDate.Reset();
                txtTransactionDesc.Focus();
                DisplayGold(scrollToBottom: true);
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
            if (newName == "" || session.Character.inventory.ContainsKey(newName)) {
                txtLocationName.BackColor = Color.Red;
                return;
            }

            txtLocationName.BackColor = SystemColors.Window;
            txtLocationName.Text = "";
            InventoryLocation location = new InventoryLocation();
            session.Mutate(c => c.inventory[newName] = location);
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
                    slot = session.SlotForItem(item.Key)
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
            EquippedChoices equipped = session.Character.equipped;
            txtPrimaryWeapon.Text = equipped.PrimaryHandWeapon?.FullName() ?? "";
            txtOffHandWeapon.Text = equipped.OffHandWeapon?.FullName() ?? "";
            txtArmor.Text = equipped.DonnedArmor?.FullName() ?? "";
            txtShield.Text = equipped.OffHandShield?.FullName() ?? "";

            foreach (Label slotBox in slotBoxes) {
                if (equipped.SlottedItems.ContainsKey((Equipped.Slot)slotBox.Tag)) {
                    slotBox.Text = equipped.SlottedItems[(Equipped.Slot)slotBox.Tag];
                } else {
                    slotBox.Text = "";
                }
            }
            lstSlotless.Items.Clear();
            foreach (string itemName in session.Character.equipped.SlotlessItems) {
                lstSlotless.Items.Add(new ItemDrag { item = itemName });
            }
            // XXX: why in the world does this need to be one higher????
            if (lstSlotless.Items.Count > 2) {
                lstSlotless.Height = lstSlotless.ItemHeight * (lstSlotless.Items.Count + 1);
            }
            RefreshEffects();
            RefreshWeight();
        }
        
        private void TabSwitch(object sender, TabControlEventArgs e) {
            session.Recalculate();
            RefreshEffects();
        }

        private void MainForm_Load(object sender, EventArgs e) {
            Font = scala;
            UpdateXpLedgerBounds();

            // this "buffer" is actually the difference between the left side of the points label and
            // the left side of a hypothetical "extra" ranks box -- makes the math easier.
            skillPointsBuffer = lblPoints.Left - 2*(txtRanksClass.Left) + txtRanksCrossClass.Left;
            skillLabelOffset = lblSkill2.Left - lblSkill1.Left;

            levelBuffer = cboBonusLanguage.Top - txtAutomaticLanguages.Top;
            levelUpBuffer = btnAddLevel.Top - cboClass.Top;
            skillBufferLeft = lblTotalRanks.Left - lblTotalHp.Left;
            transactionBuffer = lblTransactionAmount2.Top - lblTransactionAmount1.Top;

            // Remove all subsystem pages.  We will re-add them when the character gains access to the systems.
            RemoveSubsystemTabs();

            addItemDialog.Font = scala;

            session = session ?? new CharacterSession(new Character(), new GameRulesCatalog());
            levelDetailsDialog = new LevelDetailsDialog(session, uiRegistry) {
                Font = scala
            };

            availableFeats = session.Catalog.Feats.Keys.ToList();
            availableFeats.Sort();

            List<string> availableRaces = session.Catalog.Races.Keys.ToList();
            availableRaces.Sort();
            foreach (string race in availableRaces) {
                cboRace.Items.Add(race);
            }

            TOOLTIP.SetToolTip(lblRolledHp, "Die roll only (do not include CON bonus)");

            dynamicPanels = new List<DynamicPanel> {
                descriptionPanel, explorationPanel, socialPanel, initiativePanel, sensesPanel, evadePanel, conditionPanel,
                acPanel, acTextPanel, hpPanel, hpTextPanel, reactPanel, resistPanel, immunePanel, srPanel, savesPanel,
                speedPanel, baseCombatPanel1, attacksPanel, atkOptionsPanel, defOptionsPanel, specialActionsPanel, actionsPanel,
                abilityScoresPanel, sqPanel, skillsPanel, skillEffectsPanel,
            };
            foreach (DynamicPanel p in dynamicPanels) {
                p.Initialize(this);
            }
            timelinePanel.Initialize(this);

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

            session.Catalog.Weapons.Keys.OrderBy(x => x).ToList().ForEach(x => addItemDialog.lstAddWeapon.Items.Add(x));
            session.Catalog.WeaponProperties.Keys.OrderBy(x => x).ToList().ForEach(x => addItemDialog.lstWeaponProperties.Items.Add(x));
            session.Catalog.Armors.Keys.OrderBy(x => x).ToList().ForEach(x => addItemDialog.lstAddArmor.Items.Add(x));
            session.Catalog.Shields.Keys.OrderBy(x => x).ToList().ForEach(x => addItemDialog.lstAddShield.Items.Add(x));
            session.Catalog.ArmorProperties.Keys.OrderBy(x => x).ToList().ForEach(x => {
                addItemDialog.lstArmorProperties.Items.Add(x);
                addItemDialog.lstShieldProperties.Items.Add(x);
            });
            session.Catalog.Items.Keys.OrderBy(x => x).ToList().ForEach(x => addItemDialog.lstAddItem.Items.Add(x));

            if (!string.IsNullOrEmpty(initialCharacterPath) && File.Exists(initialCharacterPath)) {
                string pathToLoad = initialCharacterPath;
                BeginInvoke(new Action(() => LoadCharacter(pathToLoad)));
            }
        }

        private void RemoveSubsystemTabs() {
            foreach (var kvp in activeSubsystemTabs.ToList()) {
                tabMain.TabPages.Remove(kvp.Value);
                activeSubsystemTabs.Remove(kvp.Key);
            }
        }

        private void UpdateSubsystemTabs() {
            if (session == null) return;

            const int SUBSYSTEMS_INDEX = 1;
            var providers = uiRegistry.GetAll();

            foreach (var ui in providers) {
                bool active = ui.IsActive(session);
                bool hasTab = activeSubsystemTabs.TryGetValue(ui.SubsystemId, out var existingTab);

                if (active && !hasTab) {
                    var tab = ui.CreateDedicatedTab(this) as TabPage;
                    if (tab != null) {
                        tab.Font = scala;
                        int insertIndex = Math.Min(SUBSYSTEMS_INDEX, tabMain.TabPages.Count);
                        tabMain.TabPages.Insert(insertIndex, tab);
                        activeSubsystemTabs[ui.SubsystemId] = tab;
                    }
                } else if (!active && hasTab && existingTab != null) {
                    tabMain.TabPages.Remove(existingTab);
                    activeSubsystemTabs.Remove(ui.SubsystemId);
                }
            }
        }

        public bool LoadCharacter(string filePath) {
            lastCharacterDirectory = Path.GetDirectoryName(Path.GetFullPath(filePath));
            using (StreamReader reader = new StreamReader(filePath)) {
                Character loadedCharacter = new Character(JObject.Parse(reader.ReadToEnd()));
                session = new CharacterSession(loadedCharacter, session.Catalog);
                levelDetailsDialog = new LevelDetailsDialog(session, uiRegistry) {
                    Font = scala
                };
                UpdateSubsystemTabs();
            }
            DisplayCharacter();
            InitializeInventory();
            timelinePanel.RefreshTimeline();
            RefreshEffects();
            return true;
        }

        private bool LoadCharacter() {
            using (OpenFileDialog dialog = new OpenFileDialog()) {
                dialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                dialog.InitialDirectory = !string.IsNullOrEmpty(lastCharacterDirectory) && Directory.Exists(lastCharacterDirectory)
                    ? lastCharacterDirectory
                    : AppDomain.CurrentDomain.BaseDirectory;
                if (dialog.ShowDialog() != DialogResult.OK) {
                    return false;
                }
                return LoadCharacter(dialog.FileName);
            }
        }

        private void LoadCharacter_Click(object sender, EventArgs e) {
            LoadCharacter();
        }

        private bool SaveCharacter() {
            SaveFileDialog saveCharacter = new SaveFileDialog {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                FilterIndex = 1,
                InitialDirectory = !string.IsNullOrEmpty(lastCharacterDirectory) && Directory.Exists(lastCharacterDirectory)
                    ? lastCharacterDirectory
                    : AppDomain.CurrentDomain.BaseDirectory
            };
            if (saveCharacter.ShowDialog() == DialogResult.OK) {
                lastCharacterDirectory = Path.GetDirectoryName(saveCharacter.FileName);
                File.WriteAllText(saveCharacter.FileName, session.Character.ToJson().ToString());
                return true;
            }
            return false;
        }

        private void SaveCharacter_Click(object sender, EventArgs e) {
            SaveCharacter();
        }

        private void ConfigureAttackRoutines_Click(object sender, EventArgs e) {
            using (var dialog = new ConfigureAttackRoutinesDialog(session.Character.RoutinePreferences)) {
                if (dialog.ShowDialog(this) == DialogResult.OK) {
                    RefreshEffects();
                }
            }
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
}
