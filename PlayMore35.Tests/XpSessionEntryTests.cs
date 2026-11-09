using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Global;

namespace PlayMore35.Tests {
    [TestClass]
    public class XpSessionEntryTests {
        [TestMethod]
        public void XpSessionEntry_Constructor_InitializesPropertiesAndNormalizesRealDate() {
            var gameTime = new DateTime(2026, 10, 6, 20, 11, 30);
            var realDateWithTime = new DateTime(2026, 10, 9, 14, 30, 0);

            var entry = new XpSessionEntry(750, "clearing goblin warrens", gameTime, realDateWithTime);

            Assert.AreEqual(750, entry.Xp);
            Assert.AreEqual("clearing goblin warrens", entry.Description);
            Assert.AreEqual(gameTime, entry.GameTime);
            Assert.AreEqual(new DateTime(2026, 10, 9), entry.RealDate);
            Assert.AreEqual(TimeSpan.Zero, entry.RealDate.TimeOfDay, "RealDate must drop wall-clock time-of-day.");
        }

        [TestMethod]
        public void XpSessionEntry_SerializationAndDeserialization_RoundtripsAccurately() {
            var gameTime = new DateTime(2026, 10, 6, 20, 11, 30, DateTimeKind.Utc);
            var realDate = new DateTime(2026, 10, 9);
            var original = new XpSessionEntry(1200, "defeated ogre chief", gameTime, realDate);

            JObject json = (JObject)original.ToJson();

            Assert.AreEqual(1200, (int)json["xp"]);
            Assert.AreEqual("defeated ogre chief", (string)json["description"]);
            Assert.AreEqual(gameTime.ToString("O"), (string)json["game_time"]);
            Assert.AreEqual("2026-10-09", (string)json["real_date"]);

            var deserialized = new XpSessionEntry(json);

            Assert.AreEqual(original.Xp, deserialized.Xp);
            Assert.AreEqual(original.Description, deserialized.Description);
            Assert.AreEqual(original.GameTime, deserialized.GameTime);
            Assert.AreEqual(original.RealDate, deserialized.RealDate);
        }

        [TestMethod]
        public void XpSessionEntry_EqualityAndHashCode_FunctionCorrectly() {
            var gameTime = new DateTime(2026, 10, 6, 20, 11, 30);
            var realDate = new DateTime(2026, 10, 9);

            var a = new XpSessionEntry(500, "quest reward", gameTime, realDate);
            var b = new XpSessionEntry(500, "quest reward", gameTime, realDate);
            var diffXp = new XpSessionEntry(600, "quest reward", gameTime, realDate);

            Assert.IsTrue(a.Equals(b));
            Assert.IsTrue(a.Equals((object)b));
            Assert.IsFalse(a.Equals(diffXp));
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [TestMethod]
        public void Character_Sessions_SerializesAndDeserializesExplicitSchema() {
            var character = new Character();
            var s1 = new XpSessionEntry(500, "clearing cellar rats", new DateTime(2026, 10, 6, 20, 11, 30), new DateTime(2026, 10, 9));
            var s2 = new XpSessionEntry(750, "escorting caravan", new DateTime(2026, 10, 6, 21, 0, 0), new DateTime(2026, 10, 9));
            character.sessions.Add(s1);
            character.sessions.Add(s2);

            JObject json = (JObject)character.ToJson();
            JArray sessionArray = (JArray)json["sessions"];
            Assert.AreEqual(2, sessionArray.Count);
            Assert.AreEqual(500, (int)sessionArray[0]["xp"]);
            Assert.AreEqual("clearing cellar rats", (string)sessionArray[0]["description"]);
            Assert.AreEqual("2026-10-06T20:11:30.0000000", (string)sessionArray[0]["game_time"]);
            Assert.AreEqual("2026-10-09", (string)sessionArray[0]["real_date"]);

            var reloaded = new Character(json);
            Assert.AreEqual(2, reloaded.sessions.Count);
            Assert.AreEqual(500, reloaded.sessions[0].Xp);
            Assert.AreEqual("clearing cellar rats", reloaded.sessions[0].Description);
            Assert.AreEqual(new DateTime(2026, 10, 6, 20, 11, 30), reloaded.sessions[0].GameTime);
            Assert.AreEqual(new DateTime(2026, 10, 9), reloaded.sessions[0].RealDate);
            Assert.AreEqual(1250, reloaded.TotalExperience());
        }

        [TestMethod]
        public void Character_ReadOnlyProjections_ExposeXpSessionEntries() {
            var character = new Character();
            var s = new XpSessionEntry(400, "patrol skirmish", new DateTime(2026, 10, 7, 12, 0, 0), new DateTime(2026, 10, 9));
            character.sessions.Add(s);

            IReadOnlyCharacter readOnlyChar = new ReadOnlyCharacter(character);
            IReadOnlyList<XpSessionEntry> sessions = readOnlyChar.sessions;

            Assert.AreEqual(1, sessions.Count);
            Assert.AreEqual(400, sessions[0].Xp);
            Assert.AreEqual("patrol skirmish", sessions[0].Description);
            Assert.AreEqual(400, readOnlyChar.TotalExperience());
        }

        [TestMethod]
        public void Character_ExperienceToNextLevel_ComputesFromTotalExperience() {
            var character = new Character();
            character.classLevels.Clear();
            character.classLevels.Add("Fighter"); // Level 1: threshold is 500 * 1 * 2 = 1,000 XP

            Assert.AreEqual(1000, character.ExperienceToNextLevel());

            character.sessions.Add(new XpSessionEntry(600, "session 1", new DateTime(2026, 10, 6), new DateTime(2026, 10, 9)));
            Assert.AreEqual(400, character.ExperienceToNextLevel());

            character.sessions.Add(new XpSessionEntry(450, "session 2", new DateTime(2026, 10, 7), new DateTime(2026, 10, 9)));
            Assert.AreEqual(-50, character.ExperienceToNextLevel(), "Negative XP to next level indicates ready to level up.");
        }

        [TestMethod]
        public void OdricJson_LoadsAndSavesWithSessionsSchema() {
            string odricPath = TestEnvironment.FindOdricJsonPath();
            string jsonText = System.IO.File.ReadAllText(odricPath);
            JObject json = JObject.Parse(jsonText);

            var odric = new Character(json);
            Assert.IsNotNull(odric.sessions);

            var session = new CharacterSession(odric, new Rules.GameRulesCatalog());
            Assert.AreEqual(0, session.Character.TotalExperience());

            var s = new XpSessionEntry(800, "tomb exploration", new DateTime(2026, 10, 6, 20, 11, 30), new DateTime(2026, 10, 9));
            session.Mutate(c => c.sessions.Add(s));
            Assert.AreEqual(800, session.Character.TotalExperience());

            JObject savedJson = (JObject)session.Character.ToJson();
            var reloaded = new Character(savedJson);
            Assert.AreEqual(1, reloaded.sessions.Count);
            Assert.AreEqual(800, reloaded.sessions[0].Xp);
            Assert.AreEqual("tomb exploration", reloaded.sessions[0].Description);
            Assert.AreEqual(800, reloaded.TotalExperience());
        }

        [TestMethod]
        public void MainForm_CharacterTab_InitializesLedgerXpAndDateOverridePicker() {
            var character = new Character();
            var entry = new XpSessionEntry(1500, "Defeated Red Dragon", new DateTime(2026, 10, 6, 20, 0, 0), new DateTime(2026, 10, 9));
            character.sessions.Add(entry);
            var session = new CharacterSession(character, new Rules.GameRulesCatalog());

            using (var form = new MainForm(session)) {
                _ = form.Handle;

                var ledgerField = typeof(MainForm).GetField("ledgerXp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.IsNotNull(ledgerField, "MainForm must have ledgerXp field.");
                var ledger = ledgerField.GetValue(form) as PlayMore35.UI.TabularLedgerPanel;
                Assert.IsNotNull(ledger, "ledgerXp must be TabularLedgerPanel.");
                Assert.AreEqual("XP", ledger.AmountColumnHeader);

                var dtpField = typeof(MainForm).GetField("dtpSessionDate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.IsNotNull(dtpField, "MainForm must have dtpSessionDate field.");
                var dtp = dtpField.GetValue(form) as PlayMore35.UI.DateOverridePicker;
                Assert.IsNotNull(dtp, "dtpSessionDate must be DateOverridePicker.");

                var displayXpMethod = typeof(MainForm).GetMethod("DisplayXp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                displayXpMethod.Invoke(form, new object[] { false });

                Assert.AreEqual(1, ledger.ViewModel.RowCount);
                Assert.AreEqual("+1,500", ledger.ViewModel.Rows[0].AmountText);
                Assert.AreEqual("Defeated Red Dragon", ledger.ViewModel.Rows[0].DescriptionText);
            }
        }

        [TestMethod]
        public void MainForm_AddSessionAndRightClickDelete_MutatesCharacterSessionsAndUpdatesLedger() {
            var character = new Character();
            var session = new CharacterSession(character, new Rules.GameRulesCatalog());

            using (var form = new MainForm(session)) {
                _ = form.Handle;

                var txtXp = typeof(MainForm).GetField("txtSessionXp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(form) as System.Windows.Forms.TextBox;
                var txtDesc = typeof(MainForm).GetField("txtSessionDesc", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(form) as System.Windows.Forms.TextBox;
                var btnAdd = typeof(MainForm).GetField("btnAddSession", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(form) as System.Windows.Forms.Button;
                var ledger = typeof(MainForm).GetField("ledgerXp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(form) as PlayMore35.UI.TabularLedgerPanel;
                var dtp = typeof(MainForm).GetField("dtpSessionDate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(form) as PlayMore35.UI.DateOverridePicker;

                dtp.SelectedDate = new DateTime(2026, 10, 8);
                txtXp.Text = "2000";
                txtDesc.Text = "Bounty hunt";

                var addSessionMethod = typeof(MainForm).GetMethod("AddSession_Click", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                addSessionMethod.Invoke(form, new object[] { btnAdd, EventArgs.Empty });

                Assert.AreEqual(1, session.Character.sessions.Count);
                Assert.AreEqual(2000, session.Character.sessions[0].Xp);
                Assert.AreEqual("Bounty hunt", session.Character.sessions[0].Description);
                Assert.AreEqual(new DateTime(2026, 10, 8), session.Character.sessions[0].RealDate);
                Assert.AreEqual(1, ledger.ViewModel.RowCount);
                Assert.AreEqual("+2,000", ledger.ViewModel.Rows[0].AmountText);

                // Right click delete
                var row = ledger.GetRow(0);
                Assert.IsNotNull(row.ContextMenuStrip);
                Assert.AreEqual("Delete Bounty hunt", row.ContextMenuStrip.Items[0].Text);
                row.ContextMenuStrip.Items[0].PerformClick();

                Assert.AreEqual(0, session.Character.sessions.Count);
                Assert.AreEqual(0, ledger.ViewModel.RowCount);
                Assert.AreEqual(0, session.Character.TotalExperience());
            }
        }
    }
}
