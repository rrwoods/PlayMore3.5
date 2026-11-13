using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Global;

namespace PlayMore35.Tests {
    [TestClass]
    public class GpTransactionTests {
        [TestMethod]
        public void GpTransaction_Constructor_InitializesPropertiesAndNormalizesRealDate() {
            long gameRound = 28835L;
            var realDateWithTime = new DateTime(2026, 10, 9, 14, 30, 0);

            var tx = new GpTransaction(150.5m, "selling bandit loot", gameRound, realDateWithTime);

            Assert.AreEqual(150.5m, tx.Amount);
            Assert.AreEqual("selling bandit loot", tx.Description);
            Assert.AreEqual(28835L, tx.GameRound);
            Assert.AreEqual(new DateTime(2026, 10, 9), tx.RealDate);
            Assert.AreEqual(TimeSpan.Zero, tx.RealDate.TimeOfDay, "RealDate must drop wall-clock time-of-day.");
        }

        [TestMethod]
        public void GpTransaction_SerializationAndDeserialization_RoundtripsAccurately() {
            long gameRound = 525949200L; // ~100 years of rounds
            var realDate = new DateTime(2026, 10, 9);
            var original = new GpTransaction(-45.25m, "purchased rations and torches", gameRound, realDate);

            JObject json = (JObject)original.ToJson();

            Assert.AreEqual(-45.25m, (decimal)json["amount"]);
            Assert.AreEqual("purchased rations and torches", (string)json["description"]);
            Assert.AreEqual(gameRound, (long)json["game_round"]);
            Assert.IsNull(json["game_time"], "GpTransaction JSON must emit game_round instead of game_time.");
            Assert.AreEqual("2026-10-09", (string)json["real_date"]);

            var deserialized = new GpTransaction(json);

            Assert.AreEqual(original.Amount, deserialized.Amount);
            Assert.AreEqual(original.Description, deserialized.Description);
            Assert.AreEqual(original.GameRound, deserialized.GameRound);
            Assert.AreEqual(original.RealDate, deserialized.RealDate);
        }

        [TestMethod]
        public void Character_Transactions_SerializesDeserializesAndOrdersByGameRoundThenRealDate() {
            var character = new Character();
            var tLate = new GpTransaction(-25.5m, "bedroll and rations", 485L, new DateTime(2026, 10, 9));
            var tEarly = new GpTransaction(100.0m, "starting gold", 0L, new DateTime(2026, 10, 9));
            character.transactions.Add(tLate);
            character.transactions.Add(tEarly);

            JObject json = (JObject)character.ToJson();
            var reloaded = new Character(json);
            Assert.AreEqual(2, reloaded.transactions.Count);
            Assert.AreEqual(100.0m, reloaded.transactions[0].Amount);
            Assert.AreEqual("starting gold", reloaded.transactions[0].Description);
            Assert.AreEqual(0L, reloaded.transactions[0].GameRound);
            Assert.AreEqual(new DateTime(2026, 10, 9), reloaded.transactions[0].RealDate);
            Assert.AreEqual(485L, reloaded.transactions[1].GameRound);
            Assert.AreEqual(74.5m, reloaded.TotalGold());
        }

        [TestMethod]
        public void Character_ReadOnlyProjections_ExposeGpTransactions() {
            var character = new Character();
            var t = new GpTransaction(50m, "quest bounty", 14400L, new DateTime(2026, 10, 9));
            character.transactions.Add(t);

            IReadOnlyCharacter readOnlyChar = new ReadOnlyCharacter(character);
            IReadOnlyList<GpTransaction> txs = readOnlyChar.transactions;

            Assert.AreEqual(1, txs.Count);
            Assert.AreEqual(50m, txs[0].Amount);
            Assert.AreEqual("quest bounty", txs[0].Description);
            Assert.AreEqual(50m, readOnlyChar.TotalGold());
        }

        [TestMethod]
        public void OdricJson_LoadsAndSavesWithTransactionsSchema() {
            string odricPath = TestEnvironment.FindOdricJsonPath();
            string jsonText = System.IO.File.ReadAllText(odricPath);
            JObject json = JObject.Parse(jsonText);

            var odric = new Character(json);
            Assert.IsNotNull(odric.transactions);

            var session = new CharacterSession(odric, new Rules.GameRulesCatalog());
            Assert.AreEqual(0m, session.Character.TotalGold());

            var tx = new GpTransaction(120m, "dungeon treasure split", session.Timeline.CurrentRound, new DateTime(2026, 10, 9));
            session.Mutate(c => c.transactions.Add(tx));
            Assert.AreEqual(120m, session.Character.TotalGold());

            JObject savedJson = (JObject)session.Character.ToJson();
            var reloaded = new Character(savedJson);
            Assert.AreEqual(1, reloaded.transactions.Count);
            Assert.AreEqual(120m, reloaded.transactions[0].Amount);
            Assert.AreEqual("dungeon treasure split", reloaded.transactions[0].Description);
            Assert.AreEqual(session.Timeline.CurrentRound, reloaded.transactions[0].GameRound);
            Assert.AreEqual(120m, reloaded.TotalGold());
        }
    }
}
