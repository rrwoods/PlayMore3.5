using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Global;
using PlayMore35.Rules;

namespace PlayMore35.Tests {
    [TestClass]
    public class CharacterSessionFeatTests {
        private IRulesCatalog catalog;

        [TestInitialize]
        public void Setup() {
            catalog = new GameRulesCatalog();
        }

        [TestMethod]
        public void GetFeatSlotLevel_DerivesAcquisitionLevel_FromGetFeatSlots() {
            var character = new Character();
            character.race = "human";
            character.classLevels.Clear();
            character.classLevels.Add("Fighter");
            character.classLevels.Add("Fighter");
            character.classLevels.Add("Fighter"); // Level 3

            var session = new CharacterSession(character, catalog);

            Assert.AreEqual(1, session.GetFeatSlotLevel("Level 1"));
            Assert.AreEqual(1, session.GetFeatSlotLevel("Human"));
            Assert.AreEqual(1, session.GetFeatSlotLevel("Fighter 1"));
            Assert.AreEqual(2, session.GetFeatSlotLevel("Fighter 2"));
            Assert.AreEqual(3, session.GetFeatSlotLevel("Level 3"));
            Assert.AreEqual(0, session.GetFeatSlotLevel("NonExistentSlot"));
        }

        [TestMethod]
        public void ResolveFeatTargetSource_ZeroAdeptClasses_ResolvesToFeats() {
            var character = new Character();
            character.race = "human";
            character.classLevels.Clear();
            character.classLevels.Add("Fighter");

            var session = new CharacterSession(character, catalog);

            // Level 1 slot on Fighter 1: 0 adept classes in classLevels.Take(1) -> "Feats"
            Assert.AreEqual("Feats", session.ResolveFeatTargetSource("Level 1"));
        }

        [TestMethod]
        public void ResolveFeatTargetSource_SingleAdeptClass_AutoResolvesToThatClass() {
            var character = new Character();
            character.race = "human";
            character.classLevels.Clear();
            character.classLevels.Add("Fighter");
            character.classLevels.Add("Crusader");
            character.classLevels.Add("Crusader"); // Level 3

            var session = new CharacterSession(character, catalog);

            // Level 3 slot: classLevels.Take(3) has [Fighter, Crusader, Crusader] -> exactly 1 adept class
            Assert.AreEqual("Crusader", session.ResolveFeatTargetSource("Level 3"));
        }

        [TestMethod]
        public void ResolveFeatTargetSource_TwoOrMoreAdeptClasses_ResolvesToChosenClass() {
            var character = new Character();
            character.race = "human";
            character.classLevels.Clear();
            character.classLevels.Add("Warblade");
            character.classLevels.Add("Crusader");
            character.classLevels.Add("Crusader"); // Level 3

            var session = new CharacterSession(character, catalog);

            // At level 3, Warblade and Crusader both exist.
            // Before choice is set, defaults or returns first eligible adept class
            session.SetFeatChoice("Level 3", "class", "Warblade");
            Assert.AreEqual("Warblade", session.ResolveFeatTargetSource("Level 3"));

            session.SetFeatChoice("Level 3", "class", "Crusader");
            Assert.AreEqual("Crusader", session.ResolveFeatTargetSource("Level 3"));
        }

        [TestMethod]
        public void CompositeKeys_PersistedInFeatChoices_WithFullSchemaStability() {
            var character = new Character();
            character.race = "human";
            character.classLevels.Clear();
            character.classLevels.Add("Warblade");

            var session = new CharacterSession(character, catalog);

            session.SetFeatChoice("Level 1", "class", "Warblade");
            session.SetFeatChoice("Level 1", "maneuver", "Steel Wind");

            Assert.AreEqual("Warblade", session.GetFeatChoice("Level 1", "class"));
            Assert.AreEqual("Steel Wind", session.GetFeatChoice("Level 1", "maneuver"));
            Assert.AreEqual("Warblade", session.Character.featChoices["Level 1:class"]);
            Assert.AreEqual("Steel Wind", session.Character.featChoices["Level 1:maneuver"]);

            // Round trip via Character serialization
            var json = session.Character.ToJson();
            var restored = new Character((JObject)json);
            Assert.AreEqual("Warblade", restored.featChoices["Level 1:class"]);
            Assert.AreEqual("Steel Wind", restored.featChoices["Level 1:maneuver"]);
        }

        [TestMethod]
        public void LateMulticlass_AdeptIsolation_PreservesEarlierFeatBinding() {
            var character = new Character();
            character.race = "human";
            character.classLevels.Clear();
            character.classLevels.Add("Fighter"); // Level 1

            var session = new CharacterSession(character, catalog);

            // At level 1, taking Martial Study binds to "Feats"
            Assert.AreEqual("Feats", session.ResolveFeatTargetSource("Level 1"));

            // Later multiclassing into Warblade at level 2..6
            session.Mutate(c => {
                for (int i = 0; i < 5; i++) c.classLevels.Add("Warblade");
            });

            // Even though the character is now level 6 with 5 Warblade levels:
            // "Level 1" slot had acquisition level 1 -> 0 adept classes at that moment -> STILL "Feats"
            Assert.AreEqual(1, session.GetFeatSlotLevel("Level 1"));
            Assert.AreEqual("Feats", session.ResolveFeatTargetSource("Level 1"));

            // "Level 6" slot has acquisition level 6 -> exactly 1 adept class (Warblade) -> "Warblade"
            Assert.AreEqual(6, session.GetFeatSlotLevel("Level 6"));
            Assert.AreEqual("Warblade", session.ResolveFeatTargetSource("Level 6"));
        }

        // Source: Tome of Battle, pp. 32, 52
        [TestMethod]
        public void MartialStudy_NonAdept_GrantsCapacityAndInitiatorLevelUnderFeats() {
            var character = new Character();
            character.race = "human";
            character.classLevels.Clear();
            character.classLevels.Add("Fighter"); // Level 1 non-adept
            character.feats["Level 1"] = "Martial Study";

            var session = new CharacterSession(character, catalog);

            Assert.AreEqual("Feats", session.ResolveFeatTargetSource("Level 1"));
            Assert.AreEqual(1, session.GetStatistic("maneuvers known:feats").Value);
            Assert.AreEqual(1, session.GetStatistic("maneuvers readied:feats").Value);
            Assert.AreEqual(1, session.GetStatistic("initiator level:feats").Value);
        }

        // Source: Tome of Battle, pp. 32, 52
        [TestMethod]
        public void MartialStudy_MartialAdept_GrantsKnownCapacityToAdeptClass_WithoutReadiedOrFeatIL() {
            var character = new Character();
            character.race = "human";
            character.classLevels.Clear();
            character.classLevels.Add("Warblade"); // Level 1 adept
            character.feats["Level 1"] = "Martial Study";

            var session = new CharacterSession(character, catalog);

            Assert.AreEqual("Warblade", session.ResolveFeatTargetSource("Level 1"));
            // Warblade base known at level 1 is 3. With Martial Study (+1), known is 4.
            Assert.AreEqual(4, session.GetStatistic("maneuvers known:warblade").Value);
            // Warblade readied is unchanged (3).
            Assert.AreEqual(3, session.GetStatistic("maneuvers readied:warblade").Value);
            // Warblade IL is unchanged (1).
            Assert.AreEqual(1, session.GetStatistic("initiator level:warblade").Value);

            // Gated modifiers under "Feats" are NOT active for adept
            Assert.AreEqual(0, session.GetStatistic("initiator level:feats").Value);
            Assert.AreEqual(0, session.GetStatistic("maneuvers readied:feats").Value);
        }

        // Source: Tome of Battle, pp. 32, 52
        [TestMethod]
        public void MartialStudy_LateMulticlass_KeepsFeatPoolIsolated() {
            var character = new Character();
            character.race = "human";
            character.classLevels.Clear();
            character.classLevels.Add("Fighter"); // Level 1
            character.feats["Level 1"] = "Martial Study";

            var session = new CharacterSession(character, catalog);

            Assert.AreEqual("Feats", session.ResolveFeatTargetSource("Level 1"));
            Assert.AreEqual(1, session.GetStatistic("maneuvers known:feats").Value);
            Assert.AreEqual(1, session.GetStatistic("maneuvers readied:feats").Value);
            Assert.AreEqual(1, session.GetStatistic("initiator level:feats").Value);

            // Multiclass into Warblade at level 2
            session.Mutate(c => {
                c.classLevels.Add("Warblade");
            });

            // At level 2 (Fighter 1 / Warblade 1):
            // Warblade known is base 3 (feat at level 1 did NOT merge into Warblade)
            Assert.AreEqual(3, session.GetStatistic("maneuvers known:warblade").Value);
            Assert.AreEqual(3, session.GetStatistic("maneuvers readied:warblade").Value);

            // Feat maneuvers remain isolated under Feats pool
            Assert.AreEqual(1, session.GetStatistic("maneuvers known:feats").Value);
            Assert.AreEqual(1, session.GetStatistic("maneuvers readied:feats").Value);
            Assert.AreEqual(1, session.GetStatistic("initiator level:feats").Value);
        }
    }
}
