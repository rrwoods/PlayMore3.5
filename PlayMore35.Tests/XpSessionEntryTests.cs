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
            long gameRound = 28835L;
            var realDateWithTime = new DateTime(2026, 10, 9, 14, 30, 0);

            var entry = new XpSessionEntry(750, "clearing goblin warrens", gameRound, realDateWithTime);

            Assert.AreEqual(750, entry.Xp);
            Assert.AreEqual("clearing goblin warrens", entry.Description);
            Assert.AreEqual(gameRound, entry.GameRound);
            Assert.AreEqual(new DateTime(2026, 10, 9), entry.RealDate);
            Assert.AreEqual(TimeSpan.Zero, entry.RealDate.TimeOfDay, "RealDate must drop wall-clock time-of-day.");
        }

        [TestMethod]
        public void XpSessionEntry_SerializationAndDeserialization_RoundtripsAccurately() {
            long gameRound = 525949200L;
            var realDate = new DateTime(2026, 10, 9);
            var original = new XpSessionEntry(1200, "defeated ogre chief", gameRound, realDate);

            JObject json = (JObject)original.ToJson();

            Assert.AreEqual(1200, (int)json["xp"]);
            Assert.AreEqual("defeated ogre chief", (string)json["description"]);
            Assert.AreEqual(gameRound, (long)json["game_round"]);
            Assert.IsNull(json["game_time"], "XpSessionEntry JSON must emit game_round instead of game_time.");
            Assert.AreEqual("2026-10-09", (string)json["real_date"]);

            var deserialized = new XpSessionEntry(json);

            Assert.AreEqual(original.Xp, deserialized.Xp);
            Assert.AreEqual(original.Description, deserialized.Description);
            Assert.AreEqual(original.GameRound, deserialized.GameRound);
            Assert.AreEqual(original.RealDate, deserialized.RealDate);
        }

        [TestMethod]
        public void XpSessionEntry_EqualityAndHashCode_FunctionCorrectly() {
            long gameRound = 120L;
            var realDate = new DateTime(2026, 10, 9);

            var a = new XpSessionEntry(500, "quest reward", gameRound, realDate);
            var b = new XpSessionEntry(500, "quest reward", gameRound, realDate);
            var diffXp = new XpSessionEntry(600, "quest reward", gameRound, realDate);

            Assert.IsTrue(a.Equals(b));
            Assert.IsTrue(a.Equals((object)b));
            Assert.IsFalse(a.Equals(diffXp));
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [TestMethod]
        public void Character_Sessions_SerializesDeserializesAndOrdersByGameRoundThenRealDate() {
            var character = new Character();
            var sLate = new XpSessionEntry(750, "escorting caravan", 485L, new DateTime(2026, 10, 9));
            var sEarly = new XpSessionEntry(500, "clearing cellar rats", 0L, new DateTime(2026, 10, 9));
            character.sessions.Add(sLate);
            character.sessions.Add(sEarly);

            JObject json = (JObject)character.ToJson();
            var reloaded = new Character(json);
            Assert.AreEqual(2, reloaded.sessions.Count);
            Assert.AreEqual(500, reloaded.sessions[0].Xp);
            Assert.AreEqual("clearing cellar rats", reloaded.sessions[0].Description);
            Assert.AreEqual(0L, reloaded.sessions[0].GameRound);
            Assert.AreEqual(new DateTime(2026, 10, 9), reloaded.sessions[0].RealDate);
            Assert.AreEqual(485L, reloaded.sessions[1].GameRound);
            Assert.AreEqual(1250, reloaded.TotalExperience());
        }

        [TestMethod]
        public void Character_ReadOnlyProjections_ExposeXpSessionEntries() {
            var character = new Character();
            var s = new XpSessionEntry(400, "patrol skirmish", 14400L, new DateTime(2026, 10, 9));
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

            character.sessions.Add(new XpSessionEntry(600, "session 1", 0L, new DateTime(2026, 10, 9)));
            Assert.AreEqual(400, character.ExperienceToNextLevel());

            character.sessions.Add(new XpSessionEntry(450, "session 2", 14400L, new DateTime(2026, 10, 9)));
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

            var s = new XpSessionEntry(800, "tomb exploration", session.Timeline.CurrentRound, new DateTime(2026, 10, 9));
            session.Mutate(c => c.sessions.Add(s));
            Assert.AreEqual(800, session.Character.TotalExperience());

            JObject savedJson = (JObject)session.Character.ToJson();
            var reloaded = new Character(savedJson);
            Assert.AreEqual(1, reloaded.sessions.Count);
            Assert.AreEqual(800, reloaded.sessions[0].Xp);
            Assert.AreEqual("tomb exploration", reloaded.sessions[0].Description);
            Assert.AreEqual(session.Timeline.CurrentRound, reloaded.sessions[0].GameRound);
            Assert.AreEqual(800, reloaded.TotalExperience());
        }

        [TestMethod]
        public void MainForm_CharacterTab_InitializesLedgerXpAndDateOverridePicker() {
            var character = new Character();
            character.Timeline.SetCampaignTime(new DateTime(612, 5, 14, 20, 0, 0));
            var entry = new XpSessionEntry(1500, "Defeated Red Dragon", 0L, new DateTime(2026, 10, 9));
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
                Assert.AreEqual("0612-05-14 20:00", ledger.ViewModel.Rows[0].GameTimeText);
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
