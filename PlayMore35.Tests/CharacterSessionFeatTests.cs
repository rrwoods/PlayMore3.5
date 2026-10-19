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

        // Source: Tome of Battle, pp. 32, 52; Player's Handbook, pp. 58-59, 62; dysfunction:skill-feats-same-level
        [TestMethod]
        public void MartialStudy_AcquiredAtLevel1_GrantsClassSkill_ForLevel1AndSubsequentLevels() {
            var character = new Character();
            character.race = "human";
            character.assignedAbilityScores["INT"] = 10;
            character.classLevels.Clear();
            character.classLevels.Add("Fighter");
            character.classLevels.Add("Fighter");
            character.feats["Level 1"] = "Martial Study";
            character.featChoices["Level 1:maneuver"] = "Steel Wind"; // Iron Heart -> Balance

            var session = new CharacterSession(character, catalog);

            // Balance is not a Fighter class skill by default, but is granted by Martial Study
            Assert.IsTrue(session.IsClassSkill("Balance", 0), "Balance should be class skill at level 1.");
            Assert.IsTrue(session.IsClassSkill("Balance", 1), "Balance should remain class skill at level 2.");

            // Skill points at level 1: Fighter INT 10 gets (2 + 0) * 4 = 8 points
            session.Mutate(c => {
                c.skillRanks["Balance"] = new List<int> { 4, 0 };
            });
            // 4 ranks * 1 pt (class skill) = 4 spent, 8 - 4 = 4 remaining
            Assert.AreEqual(4, session.SkillPointsRemainingAtLevel(0));

            // Rank cap at level 1: max class ranks = 1 + 3 = 4
            Assert.IsFalse(session.TooManyRanks("Balance", 0), "4 ranks should not exceed cap of 4.");

            session.Mutate(c => {
                c.skillRanks["Balance"] = new List<int> { 5, 0 };
            });
            Assert.IsTrue(session.TooManyRanks("Balance", 0), "5 ranks should exceed cap of 4.");
        }

        // Source: Tome of Battle, pp. 32, 52; Player's Handbook, pp. 58-59, 62; dysfunction:skill-feats-same-level
        [TestMethod]
        public void MartialStudy_AcquiredAtLevel3_GrantsClassSkill_StartingAtLevel3_NotPriorLevels() {
            var character = new Character();
            character.race = "human";
            character.assignedAbilityScores["INT"] = 10;
            character.classLevels.Clear();
            character.classLevels.Add("Fighter"); // Level 1
            character.classLevels.Add("Fighter"); // Level 2
            character.classLevels.Add("Fighter"); // Level 3
            character.classLevels.Add("Fighter"); // Level 4
            character.feats["Level 3"] = "Martial Study";
            character.featChoices["Level 3:maneuver"] = "Steel Wind"; // Iron Heart -> Balance

            var session = new CharacterSession(character, catalog);

            // Level 1 and 2: Balance was not yet granted and is cross-class
            Assert.IsFalse(session.IsClassSkill("Balance", 0), "Balance is cross-class at level 1.");
            Assert.IsFalse(session.IsClassSkill("Balance", 1), "Balance is cross-class at level 2.");

            // Level 3 and 4: Balance is a class skill starting at acquisition level
            Assert.IsTrue(session.IsClassSkill("Balance", 2), "Balance is class skill at level 3.");
            Assert.IsTrue(session.IsClassSkill("Balance", 3), "Balance remains class skill at level 4.");

            // Skill point costs:
            // At level 1 (cross-class): 2 ranks cost 2 * 2 = 4 points. 8 - 4 = 4 remaining.
            session.Mutate(c => {
                c.skillRanks["Balance"] = new List<int> { 2, 0, 2, 0 };
            });
            Assert.AreEqual(4, session.SkillPointsRemainingAtLevel(0));

            // At level 3 (class skill): 2 ranks cost 2 * 1 = 2 points. 2 - 2 = 0 remaining.
            Assert.AreEqual(0, session.SkillPointsRemainingAtLevel(2));

            // Rank caps:
            // At level 1: max cross-class ranks = (1 + 3) / 2 = 2. 2 ranks is allowed.
            Assert.IsFalse(session.TooManyRanks("Balance", 0));

            // If 3 ranks at level 1: exceeds cross-class cap of 2
            session.Mutate(c => {
                c.skillRanks["Balance"] = new List<int> { 3, 0, 1, 0 };
            });
            Assert.IsTrue(session.TooManyRanks("Balance", 0), "3 ranks at level 1 should exceed cross-class cap of 2.");

            // At level 3: total ranks = 4, max class ranks = 3 + 3 = 6. Not exceeded.
            Assert.IsFalse(session.TooManyRanks("Balance", 2), "Total 4 ranks at level 3 should not exceed class cap of 6.");
        }

        // Source: Tome of Battle, pp. 32, 52; Player's Handbook, pp. 58-59, 62
        [TestMethod]
        public void MartialStudy_MultipleAcquisitions_GrantsRespectiveDisciplineSkills() {
            var character = new Character();
            character.race = "human";
            character.classLevels.Clear();
            character.classLevels.Add("Fighter"); // Level 1
            character.classLevels.Add("Fighter"); // Level 2
            character.classLevels.Add("Fighter"); // Level 3
            character.feats["Level 1"] = "Martial Study";
            character.featChoices["Level 1:maneuver"] = "Crusader's Strike"; // Devoted Spirit -> Intimidate
            character.feats["Level 3"] = "Martial Study";
            character.featChoices["Level 3:maneuver"] = "Steel Wind"; // Iron Heart -> Balance

            var session = new CharacterSession(character, catalog);

            // Level 1: Intimidate is class skill; Balance is cross-class
            Assert.IsTrue(session.IsClassSkill("Intimidate", 0));
            Assert.IsFalse(session.IsClassSkill("Balance", 0));

            // Level 3: Both are class skills
            Assert.IsTrue(session.IsClassSkill("Intimidate", 2));
            Assert.IsTrue(session.IsClassSkill("Balance", 2));
        }
    }
}
