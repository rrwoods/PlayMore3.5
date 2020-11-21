using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using PlayMore35.CharacterChoices;
using PlayMore35.Global;
using Newtonsoft.Json.Linq;

using static PlayMore35.Effects.Constants;

namespace PlayMore35 {
    public partial class MainForm : Form {
        private int leftBuffer;
        private int topBuffer;
        private int verticalBufferSmall;
        private int verticalBufferLarge;
        private int columnWidth;
        private bool loaded = false;

        private Label name, desc1, desc2, init, senses, listen, spot, evade, languages;
        private List<List<Label>> beforeCombat; // can't initialize right away apparently.

        private Label ac, touch, hp, dr, react, resist, immune, sr, fort, reflex, will;
        private List<List<Label>> enemyTurn;

        private Label speed;
        private List<Label> attacks = new List<Label>();
        private List<Label> damage = new List<Label>();
        private List<Label> criticals = new List<Label>();
        private Label space, reach, bab, grapple, atkOptions;
        private List<List<Label>> myTurn;

        private Label str_score, str_mod, str_check;
        private Label dex_score, dex_mod, dex_check;
        private Label con_score, con_mod, con_check;
        private Label int_score, int_mod, int_check;
        private Label wis_score, wis_mod, wis_check;
        private Label cha_score, cha_mod, cha_check;
        private Label sq;
        private List<Label> skills = new List<Label>();
        private List<List<Label>> misc;

        private Controller controller = null;

        public MainForm() {
            InitializeComponent();
        }

        private (int value, List<string> explanation) GetStatValue(string stat) {
            return controller.Effects.GetStat(stat).GetValue(controller.Effects, controller.EquippedItems);
        }

        private string ValueText(int value, bool plus) {
            return ((plus && value >= 0 ? "+" : "") + value).Replace("-", "–");
        }

        private void LabelStat(Label label, string prefix, string stat, string postfix = "", bool plus = true) {
            (int value, List<string> explanation) = GetStatValue(stat);
            string statValue = ValueText(value, plus);
            label.Text = prefix + " " + statValue + postfix;
            label.ToolTip(explanation);
        }

        private void AbilityScore(Label score, Label mod, Label check, string ability) {
            LabelStat(score, ability, ability + " score", "", false);

            var abilityModifier = GetStatValue(ability + " modifier");
            string value = ValueText(abilityModifier.value, true);
            mod.Text = "(" + value + ")";
            mod.ToolTip(abilityModifier.explanation);

            var abilityCheck = GetStatValue(ability + " checks");
            if (abilityCheck.value == abilityModifier.value) {
                check.Hide();
            } else {
                check.Text = "(check at " + (abilityCheck.value >= 0 ? "+" : "") + abilityCheck.value + ")";
                check.ToolTip(abilityCheck.explanation);
                check.Show();
            }
            
        }

        private void OneLineText(Label label, Dictionary<string, List<string>> textEffects, string key, string postfix = "") {
            if (textEffects[key].Any()) {
                label.Text = key + " " + string.Join(", ", textEffects[key]) + postfix;
            }
        }

        private void MultilineText(Label label, Dictionary<string, List<string>> textEffects, string key) {
            if (textEffects[key].Any()) {
                label.Text = key + "\n" + string.Join("\n", textEffects[key].Select(x => "\t" + x));
            }
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
                        top = label.Bottom + verticalBufferSmall;
                    }
                }
                if (anyText) {
                    top += (verticalBufferLarge - verticalBufferSmall);
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
            RepositionRow(init, senses, listen, spot);

            RepositionColumn(leftBuffer + columnWidth, enemyTurn);
            RepositionRow(ac, touch);
            RepositionRow(hp, dr);
            RepositionRow(fort, reflex, will);

            RepositionColumn(leftBuffer + columnWidth * 2, myTurn);
            RepositionRow(space, reach);
            for (int i=0; i<attacks.Count(); i++) {
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

        private void MainForm_Resize(object sender, EventArgs e) {
            RepositionLabels();
        }

        private void MainForm_Load(object sender, EventArgs e) {
            leftBuffer = lblTopLeft.Left;
            topBuffer = lblTopLeft.Top;
            verticalBufferSmall = lblSecondLine.Height - (2 * lblTopLeft.Height);
            verticalBufferLarge = lblSecondLine.Top - lblTopLeft.Bottom;

            Character character;
            using (OpenFileDialog dialog = new OpenFileDialog()) {
                dialog.InitialDirectory = "c:\\";
                if (dialog.ShowDialog() != DialogResult.OK) {
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
            beforeCombat = new List<List<Label>> {
                new List<Label> { name },
                new List<Label> { desc1 },
                new List<Label> { desc2 },
                new List<Label> { init },
                new List<Label> { evade },
                new List<Label> { languages }
            };

            ac = new Label().Initialize(this);
            touch = new Label().Initialize(this);
            hp = new Label().Initialize(this);
            dr = new Label().Initialize(this);
            react = new Label().Initialize(this);
            resist = new Label().Initialize(this);
            immune = new Label().Initialize(this);
            sr = new Label().Initialize(this);
            fort = new Label().Initialize(this);
            reflex = new Label().Initialize(this);
            will = new Label().Initialize(this);
            enemyTurn = new List<List<Label>> {
                new List<Label> { ac, hp },
                new List<Label> { react, resist, immune },
                new List<Label> { sr },
                new List<Label> { fort }
            };

            speed = new Label().Initialize(this);
            space = new Label().Initialize(this);
            reach = new Label().Initialize(this);
            bab = new Label().Initialize(this);
            grapple = new Label().Initialize(this);
            atkOptions = new Label().Initialize(this);
            myTurn = new List<List<Label>> {
                new List<Label> { speed },
                attacks,
                new List<Label> { space },
                new List<Label> { bab },
                new List<Label> { atkOptions }
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
            misc = new List<List<Label>> {
                new List<Label> { str_score, dex_score, con_score, int_score, wis_score, cha_score },
                new List<Label> { sq },
                skills
            };

            Dictionary<string, List<string>> textEffects = controller.Effects.GetTextEffects(controller.EquippedItems);

            name.Text = character.name;

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
                character.race +
                string.Join(", ", classLevels.Select(x => x.Key.Replace(" ", "\u00a0") + "\u00a0" + x.Value));
            // I wanted the separator for classes to be "/" like it is in most stat blocks,
            // but I also wanted wrapping line breaks to only appear between classes.  Unfortunately,
            // the zero-width space isn't respected by the wrapping routine for some reason.  So I
            // decided to use a character that looks okay when I put a space after it instead.

            string sizeCategory = SIZE_CATEGORIES[GetStatValue(SIZE_CATEGORY).value + 4];
            desc2.Text = character.alignment + " " + sizeCategory; // + creature type

            LabelStat(init, "Init", INITIATIVE, ";");
            OneLineText(senses, textEffects, SENSES, ";");
            LabelStat(listen, "Listen", LISTEN, ","); // ok navi calm down.
            LabelStat(spot, "Spot", SPOT);
            OneLineText(evade, textEffects, EVADE);
            // languages go here

            LabelStat(ac, "AC", AC, ",", false);
            LabelStat(touch, "touch", TOUCH_AC, "", false);
            LabelStat(hp, "hp", MAX_HP, "", false);
            hp.Text += " (" + character.classLevels.Count() + " HD)";
            OneLineText(dr, textEffects, DR);
            if (dr.Text.Any()) {
                hp.Text += ";";
            }
            MultilineText(react, textEffects, REACT);
            MultilineText(resist, textEffects, RESIST);
            MultilineText(immune, textEffects, IMMUNE);
            OneLineText(sr, textEffects, SR);
            LabelStat(fort, "Fort", FORT_SAVES, ",");
            LabelStat(reflex, "Ref", REF_SAVES, ",");
            LabelStat(will, "Will", WILL_SAVES);

            LabelStat(speed, "Speed", LAND_SPEED, " ft.", false);
            attacks.Clear();
            damage.Clear();
            criticals.Clear();
            foreach (Items.Attack attack in controller.GetAttacks()) {
                StringBuilder text = new StringBuilder();
                text.Append(attack.RangeIncrement > 0 ? "Ranged " : "Melee ");
                text.Append(attack.WeaponName + " " + attack.AttackBonus.bonus);
                foreach (int extra in attack.ExtraAttacks) {
                    text.Append("/" + (attack.AttackBonus.bonus - extra));
                }
                Label attackLabel = new Label().Initialize(this);
                attackLabel.Text = text.ToString();
                attackLabel.ToolTip(attack.AttackBonus.explanation);
                attacks.Add(attackLabel);
                Label damageLabel = new Label().Initialize(this);
                damageLabel.Text = "(" + attack.Damage.dice.ToString() + ")";
                damageLabel.ToolTip(attack.Damage.explanation);
                damage.Add(damageLabel);
                Label critLabel = new Label().Initialize(this);
                critLabel.Text = "(" + attack.ThreatRange + (attack.ThreatRange == 20 ? "" : "–20") + "/"
                    + attack.CriticalDamage.dice.ToString() + ")";
                critLabel.ToolTip(attack.CriticalDamage.explanation);
                criticals.Add(critLabel);
            }
            LabelStat(space, "Space", SPACE, " ft.;", false);
            LabelStat(reach, "Reach", REACH, " ft.", false);
            LabelStat(bab, "Base Atk", BASE_ATTACK_BONUS, ";");
            LabelStat(grapple, "Grp", GRAPPLE);
            MultilineText(atkOptions, textEffects, ATK_OPTIONS);

            AbilityScore(str_score, str_mod, str_check, "STR");
            AbilityScore(dex_score, dex_mod, dex_check, "DEX");
            AbilityScore(con_score, con_mod, con_check, "CON");
            AbilityScore(int_score, int_mod, int_check, "INT");
            AbilityScore(wis_score, wis_mod, wis_check, "WIS");
            AbilityScore(cha_score, cha_mod, cha_check, "CHA");
            MultilineText(sq, textEffects, SQ);
            skills.Clear();
            foreach (string skill in SKILLS_LIST) {
                if (controller.Effects.GetStat(skill).Relevant(controller.Effects, controller.EquippedItems)) {
                    Label label = new Label().Initialize(this);
                    LabelStat(label, skill, skill);
                    skills.Add(label);
                }
            }

            loaded = true;
            RepositionLabels();
        }
    }

    static class LabelExtensions {
        public static Label Initialize(this Label label, Form form) {
            form.Controls.Add(label);
            label.AutoSize = true;
            return label;
        }

        public static void ToolTip(this Label label, List<string> explanation) {
            new ToolTip().SetToolTip(label, string.Join("\n", explanation));
        }
    }
}
