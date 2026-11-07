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
            var gameTime = new DateTime(2026, 10, 6, 20, 11, 30);
            var realDateWithTime = new DateTime(2026, 10, 9, 14, 30, 0);

            var tx = new GpTransaction(150.5m, "selling bandit loot", gameTime, realDateWithTime);

            Assert.AreEqual(150.5m, tx.Amount);
            Assert.AreEqual("selling bandit loot", tx.Description);
            Assert.AreEqual(gameTime, tx.GameTime);
            Assert.AreEqual(new DateTime(2026, 10, 9), tx.RealDate);
            Assert.AreEqual(TimeSpan.Zero, tx.RealDate.TimeOfDay, "RealDate must drop wall-clock time-of-day.");
        }

        [TestMethod]
        public void GpTransaction_SerializationAndDeserialization_RoundtripsAccurately() {
            var gameTime = new DateTime(2026, 10, 6, 20, 11, 30, DateTimeKind.Utc);
            var realDate = new DateTime(2026, 10, 9);
            var original = new GpTransaction(-45.25m, "purchased rations and torches", gameTime, realDate);

            JObject json = (JObject)original.ToJson();

            Assert.AreEqual(-45.25m, (decimal)json["amount"]);
            Assert.AreEqual("purchased rations and torches", (string)json["description"]);
            Assert.AreEqual(gameTime.ToString("O"), (string)json["game_time"]);
            Assert.AreEqual("2026-10-09", (string)json["real_date"]);

            var deserialized = new GpTransaction(json);

            Assert.AreEqual(original.Amount, deserialized.Amount);
            Assert.AreEqual(original.Description, deserialized.Description);
            Assert.AreEqual(original.GameTime, deserialized.GameTime);
            Assert.AreEqual(original.RealDate, deserialized.RealDate);
        }

        [TestMethod]
        public void Character_Transactions_SerializesAndDeserializesExplicitSchema() {
            var character = new Character();
            var t1 = new GpTransaction(100.0m, "starting gold", new DateTime(2026, 10, 6, 20, 11, 30), new DateTime(2026, 10, 9));
            var t2 = new GpTransaction(-25.5m, "bedroll and rations", new DateTime(2026, 10, 6, 21, 0, 0), new DateTime(2026, 10, 9));
            character.transactions.Add(t1);
            character.transactions.Add(t2);

            JObject json = (JObject)character.ToJson();
            JArray txArray = (JArray)json["transactions"];
            Assert.AreEqual(2, txArray.Count);
            Assert.AreEqual(100.0m, (decimal)txArray[0]["amount"]);
            Assert.AreEqual("starting gold", (string)txArray[0]["description"]);
            Assert.AreEqual("2026-10-06T20:11:30.0000000", (string)txArray[0]["game_time"]);
            Assert.AreEqual("2026-10-09", (string)txArray[0]["real_date"]);

            var reloaded = new Character(json);
            Assert.AreEqual(2, reloaded.transactions.Count);
            Assert.AreEqual(100.0m, reloaded.transactions[0].Amount);
            Assert.AreEqual("starting gold", reloaded.transactions[0].Description);
            Assert.AreEqual(new DateTime(2026, 10, 6, 20, 11, 30), reloaded.transactions[0].GameTime);
            Assert.AreEqual(new DateTime(2026, 10, 9), reloaded.transactions[0].RealDate);
            Assert.AreEqual(74.5m, reloaded.TotalGold());
        }

        [TestMethod]
        public void Character_ReadOnlyProjections_ExposeGpTransactions() {
            var character = new Character();
            var t = new GpTransaction(50m, "quest bounty", new DateTime(2026, 10, 7, 12, 0, 0), new DateTime(2026, 10, 9));
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

            var tx = new GpTransaction(120m, "dungeon treasure split", new DateTime(2026, 10, 6, 20, 11, 30), new DateTime(2026, 10, 9));
            session.Mutate(c => c.transactions.Add(tx));
            Assert.AreEqual(120m, session.Character.TotalGold());

            JObject savedJson = (JObject)session.Character.ToJson();
            var reloaded = new Character(savedJson);
            Assert.AreEqual(1, reloaded.transactions.Count);
            Assert.AreEqual(120m, reloaded.transactions[0].Amount);
            Assert.AreEqual("dungeon treasure split", reloaded.transactions[0].Description);
            Assert.AreEqual(120m, reloaded.TotalGold());
        }
    }
}
