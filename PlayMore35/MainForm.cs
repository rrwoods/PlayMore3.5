using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using PlayMore35.CharacterChoices;
using PlayMore35.Global;
using Newtonsoft.Json.Linq;

using static PlayMore35.Effects.Constants;

namespace PlayMore35 {
    public partial class MainForm : Form {
        private Controller controller = null;

        private int leftBuffer;
        private int topBuffer;
        private int verticalBuffer;
        private int columnWidth;

        private Label name, desc1, desc2, init, senses, listen, spot, evade, languages;

        private Label ac, touch, hp, dr, react, resist, immune, sr, fort, reflex, will;

        private Label speed;
        private List<Label> attacks;
        private Label space, reach, bab, grapple, atkOptions;

        private Label str_score, str_mod, str_check;
        private Label dex_score, dex_mod, dex_check;
        private Label con_score, con_mod, con_check;
        private Label int_score, int_mod, int_check;
        private Label wis_score, wis_mod, wis_check;
        private Label cha_score, cha_mod, cha_check;
        private Label sq;
        private List<Label> skills;

        public MainForm() {
            InitializeComponent();
        }

        private (int value, List<string> explanation) GetStatValue(string stat) {
            return controller.Effects.GetStat(stat).GetValue(controller.Effects, controller.EquippedItems);
        }

        private void LabelStat(Label label, string prefix, string stat, string postfix = "", bool plus = true) {
            (int value, List<string> explanation) = GetStatValue(stat);
            string statValue = (plus && value >= 0 ? "+" : "") + value;
            label.Text = prefix + " " + statValue + postfix;
            new ToolTip().SetToolTip(label, string.Join("\n", explanation));
        }

        private void AbilityScore(Label score, Label mod, Label check, string ability) {
            LabelStat(score, ability, ability + " score", "", false);

            var abilityModifier = GetStatValue(ability + " modifier");
            string value = (abilityModifier.value >= 0 ? "+" : "") + abilityModifier.value;
            mod.Text = "(" + value + ")";
            new ToolTip().SetToolTip(mod, string.Join("\n", abilityModifier.explanation));

            var abilityCheck = GetStatValue(ability + " checks");
            if (abilityCheck.value == abilityModifier.value) {
                check.Hide();
            } else {
                check.Text = "(check at " + (abilityCheck.value >= 0 ? "+" : "") + abilityCheck.value + ")";
                new ToolTip().SetToolTip(check, string.Join("\n", abilityCheck.explanation));
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

        private void RepositionColumn(int columnLeft, params Label[] labels) {
            int top = topBuffer;
            foreach (Label label in labels) {
                if (label.Text.Any()) {
                    label.Left = columnLeft;
                    label.Top = top;
                    label.MaximumSize = new Size(columnWidth - 2 * leftBuffer, 1000);
                    top = label.Bottom + verticalBuffer;
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
            columnWidth = ClientSize.Width / 4;

            RepositionColumn(leftBuffer, name, desc1, desc2, init, evade, languages);
            RepositionRow(init, senses, listen, spot);

            RepositionColumn(leftBuffer + columnWidth, ac, hp, react, resist, immune, sr, fort);
            RepositionRow(ac, touch);
            RepositionRow(hp, dr);
            RepositionRow(fort, reflex, will);

            RepositionColumn(leftBuffer + columnWidth * 2, speed, space, bab, atkOptions);
            RepositionRow(space, reach);
            RepositionRow(bab, grapple);

            RepositionColumn(leftBuffer + columnWidth * 3, str_score, dex_score, con_score, int_score, wis_score, cha_score, sq);
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
            verticalBuffer = lblSecondLine.Top - lblTopLeft.Bottom;

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

            speed = new Label().Initialize(this);
            space = new Label().Initialize(this);
            reach = new Label().Initialize(this);
            bab = new Label().Initialize(this);
            grapple = new Label().Initialize(this);
            atkOptions = new Label().Initialize(this);

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
            // attacks later...
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
            // skills later...

            RepositionLabels();
        }
    }

    static class LabelExtensions {
        public static Label Initialize(this Label label, Form form) {
            form.Controls.Add(label);
            label.AutoSize = true;
            return label;
        }
    }
}
