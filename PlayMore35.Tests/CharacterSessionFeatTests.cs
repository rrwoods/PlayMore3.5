using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Items;
using PlayMore35.Rules;
using PlayMore35.Subsystems;

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

        [TestMethod]
        public void Feat_JsonDeserialization_ReadsClassSkillsFromRootOnly_NotBenefit() {
            var jsonWithRoot = new JObject(
                new JProperty("benefit", new JObject(
                    new JProperty("name", "Test Feat"),
                    new JProperty("class skills", new JArray("Disguise"))
                )),
                new JProperty("class skills", new JArray("Bluff"))
            );

            var feat = new Feat(jsonWithRoot);
            Assert.AreEqual(1, feat.ClassSkills.Count);
            Assert.AreEqual("Bluff", feat.ClassSkills[0]);
            Assert.IsFalse(feat.ClassSkills.Contains("Disguise"), "Class skills should not be parsed from benefit JObject.");
        }

        [TestMethod]
        public void ChoiceDescriptor_JsonSerializationAndDeserialization_RoundTripsAllFields() {
            var desc = new ChoiceDescriptor("class", "Target Class", "CharacterClasses", "Adept", "Feats", "priorChoice");
            var json = (JObject)desc.ToJson();

            Assert.AreEqual("class", (string)json["id"]);
            Assert.AreEqual("Target Class", (string)json["label"]);
            Assert.AreEqual("CharacterClasses", (string)json["domain"]);
            Assert.AreEqual("Adept", (string)json["filter"]);
            Assert.AreEqual("Feats", (string)json["fallback"]);
            Assert.AreEqual("priorChoice", (string)json["dependsOn"]);

            var restored = new ChoiceDescriptor(json);
            Assert.AreEqual("class", restored.Id);
            Assert.AreEqual("Target Class", restored.Label);
            Assert.AreEqual("CharacterClasses", restored.Domain);
            Assert.AreEqual("Adept", restored.Filter);
            Assert.AreEqual("Feats", restored.Fallback);
            Assert.AreEqual("priorChoice", restored.DependsOn);
            Assert.AreEqual(0, restored.MinimumPrerequisiteCount);
        }

        [TestMethod]
        public void ChoiceDescriptor_JsonSerializationAndDeserialization_PreservesMinimumPrerequisiteCountWhenSet_AndOmitsWhenZero() {
            // When MinimumPrerequisiteCount > 0, serialized output includes it and roundtrips
            var descWithCount = new ChoiceDescriptor("stance", "Choose stance", "SubsystemElements", "stance", dependsOn: "class", minimumPrerequisiteCount: 1);
            Assert.AreEqual(1, descWithCount.MinimumPrerequisiteCount);

            var tokenWithCount = (JObject)descWithCount.ToJson();
            Assert.AreEqual(1, (int)tokenWithCount["minimumPrerequisiteCount"]);
            var restoredFromToken = new ChoiceDescriptor(tokenWithCount);
            Assert.AreEqual(1, restoredFromToken.MinimumPrerequisiteCount);

            // When MinimumPrerequisiteCount == 0 (default), serialized output omits it
            var descDefault = new ChoiceDescriptor("stance", "Choose stance", "SubsystemElements", "stance");
            Assert.AreEqual(0, descDefault.MinimumPrerequisiteCount);

            var tokenDefault = (JObject)descDefault.ToJson();
            Assert.IsNull(tokenDefault["minimumPrerequisiteCount"], "Zero minimumPrerequisiteCount must be omitted from JSON serialization.");
            var restoredFromDefaultToken = new ChoiceDescriptor(tokenDefault);
            Assert.AreEqual(0, restoredFromDefaultToken.MinimumPrerequisiteCount);
        }

        [TestMethod]
        public void Feat_Choices_RoundTripsThroughJson() {
            var original = new Feat(new JObject(
                new JProperty("benefit", new JObject(new JProperty("name", "Synthetic Feat"))),
                new JProperty("choices", new JArray(
                    new JObject(
                        new JProperty("id", "class"),
                        new JProperty("label", "Target Class"),
                        new JProperty("domain", "CharacterClasses"),
                        new JProperty("filter", "Adept"),
                        new JProperty("fallback", "Feats")
                    )
                ))
            ));

            Assert.AreEqual(1, original.Choices.Count);
            Assert.AreEqual("class", original.Choices[0].Id);
            Assert.AreEqual("Target Class", original.Choices[0].Label);
            Assert.AreEqual("CharacterClasses", original.Choices[0].Domain);
            Assert.AreEqual("Adept", original.Choices[0].Filter);
            Assert.AreEqual("Feats", original.Choices[0].Fallback);

            var serialized = (JObject)original.ToJson();
            var restored = new Feat(serialized);

            Assert.AreEqual(1, restored.Choices.Count);
            Assert.AreEqual("class", restored.Choices[0].Id);
            Assert.AreEqual("Target Class", restored.Choices[0].Label);
            Assert.AreEqual("CharacterClasses", restored.Choices[0].Domain);
            Assert.AreEqual("Adept", restored.Choices[0].Filter);
            Assert.AreEqual("Feats", restored.Choices[0].Fallback);
        }

        private static Feat CreateSyntheticClassTargetedFeat(string featName = "Synthetic Adept Feat", string filter = "Adept", string fallback = "Feats") {
            var feat = new Feat(new JObject(
                new JProperty("benefit", new JObject(new JProperty("name", featName))),
                new JProperty("choices", new JArray(
                    new JObject(
                        new JProperty("id", "class"),
                        new JProperty("label", "Target Class"),
                        new JProperty("domain", "CharacterClasses"),
                        new JProperty("filter", filter),
                        new JProperty("fallback", fallback)
                    )
                ))
            ));
            return feat;
        }

        [TestMethod]
        public void ResolveClassChoice_SyntheticFeat_ZeroQualifyingClasses_ResolvesToFallback() {
            var syntheticFeat = CreateSyntheticClassTargetedFeat();
            var fakeCatalog = new FakeRulesCatalog(inner: catalog);
            fakeCatalog.FeatsMap[syntheticFeat.Benefit.Name] = syntheticFeat;

            var character = new Character { race = "human" };
            character.classLevels.Clear();
            character.classLevels.Add("Fighter");
            character.feats["Level 1"] = syntheticFeat.Benefit.Name;

            var session = new CharacterSession(character, fakeCatalog);

            var resolvedChoice = session.ResolveClassChoice("Level 1", syntheticFeat.Choices[0]);
            Assert.AreEqual("Feats", resolvedChoice);
            Assert.AreEqual("Feats", session.ResolveFeatTargetSource("Level 1"));
        }

        [TestMethod]
        public void ResolveClassChoice_SyntheticFeat_SingleQualifyingClass_AutoResolvesToThatClass() {
            var syntheticFeat = CreateSyntheticClassTargetedFeat();
            var fakeCatalog = new FakeRulesCatalog(inner: catalog);
            fakeCatalog.FeatsMap[syntheticFeat.Benefit.Name] = syntheticFeat;

            var character = new Character { race = "human" };
            character.classLevels.Clear();
            character.classLevels.Add("Fighter");
            character.classLevels.Add("Crusader");
            character.classLevels.Add("Crusader");
            character.feats["Level 3"] = syntheticFeat.Benefit.Name;

            var session = new CharacterSession(character, fakeCatalog);

            var resolvedChoice = session.ResolveClassChoice("Level 3", syntheticFeat.Choices[0]);
            Assert.AreEqual("Crusader", resolvedChoice);
            Assert.AreEqual("Crusader", session.ResolveFeatTargetSource("Level 3"));
        }

        [TestMethod]
        public void ResolveClassChoice_SyntheticFeat_MultipleQualifyingClasses_ResolvesToChosenClass() {
            var syntheticFeat = CreateSyntheticClassTargetedFeat();
            var fakeCatalog = new FakeRulesCatalog(inner: catalog);
            fakeCatalog.FeatsMap[syntheticFeat.Benefit.Name] = syntheticFeat;

            var character = new Character { race = "human" };
            character.classLevels.Clear();
            character.classLevels.Add("Warblade");
            character.classLevels.Add("Crusader");
            character.classLevels.Add("Crusader");
            character.feats["Level 3"] = syntheticFeat.Benefit.Name;

            var session = new CharacterSession(character, fakeCatalog);

            // Default before explicit choice returns first qualifying class
            Assert.AreEqual("Warblade", session.ResolveClassChoice("Level 3", syntheticFeat.Choices[0]));

            session.SetFeatChoice("Level 3", "class", "Crusader");
            Assert.AreEqual("Crusader", session.ResolveClassChoice("Level 3", syntheticFeat.Choices[0]));

            session.SetFeatChoice("Level 3", "class", "Warblade");
            Assert.AreEqual("Warblade", session.ResolveClassChoice("Level 3", syntheticFeat.Choices[0]));
        }

        [TestMethod]
        public void ResolveClassChoice_SyntheticFeat_TemporalGating_ResolvesBasedOnAcquisitionLevel() {
            var syntheticFeat = CreateSyntheticClassTargetedFeat();
            var fakeCatalog = new FakeRulesCatalog(inner: catalog);
            fakeCatalog.FeatsMap[syntheticFeat.Benefit.Name] = syntheticFeat;

            var character = new Character { race = "human" };
            character.classLevels.Clear();
            character.classLevels.Add("Fighter");   // Level 1
            character.classLevels.Add("Warblade");  // Level 2
            character.classLevels.Add("Crusader");  // Level 3

            character.feats["Level 1"] = syntheticFeat.Benefit.Name;
            character.feats["Level 3"] = syntheticFeat.Benefit.Name;

            var session = new CharacterSession(character, fakeCatalog);

            // Level 1 slot acquired at level 1: 0 adept classes at level 1 -> Feats
            Assert.AreEqual("Feats", session.ResolveClassChoice("Level 1", syntheticFeat.Choices[0]));

            // Level 3 slot acquired at level 3: 2 adept classes at level 3
            session.SetFeatChoice("Level 3", "class", "Crusader");
            Assert.AreEqual("Crusader", session.ResolveClassChoice("Level 3", syntheticFeat.Choices[0]));

            // Slot acquired at level 1 remains Feats regardless of level 3 progression
            Assert.AreEqual("Feats", session.ResolveClassChoice("Level 1", syntheticFeat.Choices[0]));
        }

        [TestMethod]
        public void SyntheticFeat_EffectGeneration_AppliesResolvedChoiceWithoutContentLiteracy() {
            var feat = new Feat(new JObject(
                new JProperty("benefit", new JObject(
                    new JProperty("name", "Synthetic Adept Focus"),
                    new JProperty("flags", new JArray("SyntheticChoice:$"))
                )),
                new JProperty("choices", new JArray(
                    new JObject(
                        new JProperty("id", "class"),
                        new JProperty("label", "Target Class"),
                        new JProperty("domain", "CharacterClasses"),
                        new JProperty("filter", "Adept"),
                        new JProperty("fallback", "Feats")
                    )
                ))
            ));

            var fakeCatalog = new FakeRulesCatalog(inner: catalog);
            fakeCatalog.FeatsMap[feat.Benefit.Name] = feat;

            // Character A: Crusader 1 -> auto-resolves to Crusader
            var charA = new Character { race = "human" };
            charA.classLevels.Clear();
            charA.classLevels.Add("Crusader");
            charA.feats["Level 1"] = feat.Benefit.Name;
            var sessionA = new CharacterSession(charA, fakeCatalog);
            Assert.IsTrue(sessionA.HasFlag("SyntheticChoice:Crusader"), "Feat effect should have choice resolved to Crusader.");
            Assert.IsFalse(sessionA.HasFlag("SyntheticChoice:Warblade"));

            // Character B: Warblade 1 -> auto-resolves to Warblade
            var charB = new Character { race = "human" };
            charB.classLevels.Clear();
            charB.classLevels.Add("Warblade");
            charB.feats["Level 1"] = feat.Benefit.Name;
            var sessionB = new CharacterSession(charB, fakeCatalog);
            Assert.IsTrue(sessionB.HasFlag("SyntheticChoice:Warblade"), "Feat effect should have choice resolved to Warblade.");
            Assert.IsFalse(sessionB.HasFlag("SyntheticChoice:Crusader"));

            // Character C: Fighter 1 -> 0 adept classes -> fallback to Feats
            var charC = new Character { race = "human" };
            charC.classLevels.Clear();
            charC.classLevels.Add("Fighter");
            charC.feats["Level 1"] = feat.Benefit.Name;
            var sessionC = new CharacterSession(charC, fakeCatalog);
            Assert.IsTrue(sessionC.HasFlag("SyntheticChoice:Feats"), "Feat effect should have choice resolved to fallback 'Feats'.");
        }

        [TestMethod]
        public void SyntheticFeat_ClassSkillTemplate_ExpandsResolvedChoiceWithoutContentLiteracy() {
            var feat = new Feat(new JObject(
                new JProperty("benefit", new JObject(new JProperty("name", "Synthetic Skill Feat"))),
                new JProperty("class skills", new JArray("${class}")),
                new JProperty("choices", new JArray(
                    new JObject(
                        new JProperty("id", "class"),
                        new JProperty("label", "Target Class"),
                        new JProperty("domain", "CharacterClasses"),
                        new JProperty("filter", "Adept"),
                        new JProperty("fallback", "Feats")
                    )
                ))
            ));

            var fakeCatalog = new FakeRulesCatalog(inner: catalog);
            fakeCatalog.FeatsMap[feat.Benefit.Name] = feat;

            var character = new Character { race = "human" };
            character.classLevels.Clear();
            character.classLevels.Add("Warblade");
            character.feats["Level 1"] = feat.Benefit.Name;

            var session = new CharacterSession(character, fakeCatalog);
            Assert.IsTrue(session.IsClassSkill("Warblade", 0), "Template ${class} should expand to resolved choice 'Warblade'.");
        }

        private static Feat CreateSyntheticSubsystemMultiChoiceFeat(
            string featName = "Synthetic Subsystem Feat",
            string elementFilter = "maneuver",
            string elementId = "maneuver",
            string elementLabel = "Choose maneuver",
            string template = "${maneuver.discipline.keySkill}")
        {
            return new Feat(new JObject(
                new JProperty("subsystems", new JArray("martial_maneuvers")),
                new JProperty("stats", new JArray(
                    new JObject(new JProperty("name", "initiator level:feats")),
                    new JObject(new JProperty("name", "maneuvers known:feats")),
                    new JObject(new JProperty("name", "maneuvers readied:feats")),
                    new JObject(new JProperty("name", "stances known:feats"))
                )),
                new JProperty("benefit", new JObject(
                    new JProperty("name", featName),
                    new JProperty("slots", new JArray(
                        new JObject(
                            new JProperty("subsystem", "martial_maneuvers"),
                            new JProperty("slot_group", "maneuvers"),
                            new JProperty("element", $"${{{elementId}}}"),
                            new JProperty("source", "${class}")
                        )
                    )),
                    new JProperty("modifiers", new JArray(
                        new JObject(
                            new JProperty("affects", "maneuvers known:${class}"),
                            new JProperty("mod", new JObject(
                                new JProperty("type", "untyped"),
                                new JProperty("amounts", new JArray(new JObject(new JProperty("fixed", 1))))
                            ))
                        ),
                        new JObject(
                            new JProperty("affects", "initiator level:feats"),
                            new JProperty("active", new JObject(new JProperty("choice", "Feats"))),
                            new JProperty("mod", new JObject(
                                new JProperty("type", "internal"),
                                new JProperty("amounts", new JArray(new JObject(
                                    new JProperty("largest", new JArray(
                                        new JObject(new JProperty("fixed", 1)),
                                        new JObject(new JProperty("multiply", new JObject(
                                            new JProperty("variable", new JObject(new JProperty("stat value", "character level"))),
                                            new JProperty("scalar", 0.5)
                                        )))
                                    ))
                                )))
                            ))
                        )
                    ))
                )),
                new JProperty("class skills", new JArray(template)),
                new JProperty("choices", new JArray(
                    new JObject(
                        new JProperty("id", "class"),
                        new JProperty("label", "Target Class"),
                        new JProperty("domain", "CharacterClasses"),
                        new JProperty("filter", "adept"),
                        new JProperty("fallback", "Feats")
                    ),
                    new JObject(
                        new JProperty("id", elementId),
                        new JProperty("label", elementLabel),
                        new JProperty("domain", "SubsystemElements"),
                        new JProperty("filter", elementFilter),
                        new JProperty("dependsOn", "class")
                    )
                ))
            ));
        }

        [TestMethod]
        public void SyntheticMultiChoiceFeat_TargetClassAndSubsystemElement_ResolvesChoicesAndExpandsTemplate() {
            var syntheticFeat = CreateSyntheticSubsystemMultiChoiceFeat();
            var fakeCatalog = new FakeRulesCatalog(inner: catalog);
            fakeCatalog.FeatsMap[syntheticFeat.Benefit.Name] = syntheticFeat;

            var character = new Character { race = "human" };
            character.classLevels.Clear();
            character.classLevels.Add("Warblade");
            character.feats["Level 1"] = syntheticFeat.Benefit.Name;
            character.featChoices["Level 1:maneuver"] = "Steel Wind";

            var session = new CharacterSession(character, fakeCatalog);

            // 1. Target class auto-resolves to Warblade
            Assert.AreEqual("Warblade", session.ResolveClassChoice("Level 1", syntheticFeat.Choices[0]));
            Assert.AreEqual("Warblade", session.ResolveFeatChoice("Level 1", syntheticFeat.Choices[0]));

            // 1b. Maneuver choice resolves to SubsystemElement object
            var resolvedManeuver = session.ResolveFeatChoice("Level 1", syntheticFeat.Choices[1]);
            Assert.IsInstanceOfType(resolvedManeuver, typeof(SubsystemElement));
            Assert.AreEqual("Steel Wind", ((SubsystemElement)resolvedManeuver).Name);

            // 2. Class skill template ${maneuver.discipline.keySkill} resolves to Balance (Iron Heart key skill)
            Assert.IsTrue(session.IsClassSkill("Balance", 0), "Template should expand key skill of chosen maneuver to 'Balance'.");

            // 3. Modifier maneuvers known:${class} resolves to maneuvers known:warblade
            Assert.AreEqual(4, session.GetStatValue("maneuvers known:warblade", 0)); // base 3 + 1 from synthetic feat
        }

        [TestMethod]
        public void SyntheticMultiChoiceFeat_SlotProjection_ProjectsIntoSubsystemAsLockedSlot() {
            var syntheticFeat = CreateSyntheticSubsystemMultiChoiceFeat();
            var fakeCatalog = new FakeRulesCatalog(inner: catalog);
            fakeCatalog.FeatsMap[syntheticFeat.Benefit.Name] = syntheticFeat;

            var character = new Character { race = "human" };
            character.classLevels.Clear();
            character.classLevels.Add("Warblade");
            character.feats["Level 1"] = syntheticFeat.Benefit.Name;
            character.featChoices["Level 1:maneuver"] = "Steel Wind";

            var session = new CharacterSession(character, fakeCatalog);
            var engine = session.GetSubsystem("martial_maneuvers")!;
            Assert.IsNotNull(engine);

            var warbladeSlots = engine.GetSlots("maneuvers", "Warblade");
            var featSlot = warbladeSlots.FirstOrDefault(s => s != null && s.IsFeatBound);
            Assert.IsNotNull(featSlot, "Synthetic feat should project into Warblade known slots as a feat-bound slot.");
            Assert.AreEqual("Steel Wind", featSlot.Element);
            Assert.AreEqual("Level 1", featSlot.FeatSlot);

            // Attempting to overwrite or clear feat-bound slot throws InvalidOperationException
            int slotIndex = warbladeSlots.ToList().IndexOf(featSlot);
            Assert.ThrowsException<InvalidOperationException>(() => {
                engine.AssignSlot("maneuvers", slotIndex, "Stone Bones", "unreadied", "Warblade");
            });
        }

        [TestMethod]
        public void SyntheticMultiChoiceFeat_NonAdept_ProjectsIntoFeatsPool_AsLockedSlot() {
            var syntheticFeat = CreateSyntheticSubsystemMultiChoiceFeat();
            var fakeCatalog = new FakeRulesCatalog(inner: catalog);
            fakeCatalog.FeatsMap[syntheticFeat.Benefit.Name] = syntheticFeat;

            var character = new Character { race = "human" };
            character.classLevels.Clear();
            character.classLevels.Add("Fighter");
            character.feats["Level 1"] = syntheticFeat.Benefit.Name;
            character.featChoices["Level 1:maneuver"] = "Moment of Perfect Mind";

            var session = new CharacterSession(character, fakeCatalog);
            var engine = session.GetSubsystem("martial_maneuvers")!;
            Assert.IsNotNull(engine);

            var featSlots = engine.GetSlots("maneuvers", "Feats");
            var featSlot = featSlots.FirstOrDefault(s => s != null && s.IsFeatBound);
            Assert.IsNotNull(featSlot, "Synthetic feat on non-adept should project into Feats pool.");
            Assert.AreEqual("Moment of Perfect Mind", featSlot.Element);
            Assert.AreEqual("Level 1", featSlot.FeatSlot);
        }

        [TestMethod]
        public void SyntheticMultiChoiceFeat_DependentLevelGating_ValidatesMaxManeuverLevelAgainstTargetClass() {
            var syntheticFeat = CreateSyntheticSubsystemMultiChoiceFeat();
            var fakeCatalog = new FakeRulesCatalog(inner: catalog);
            fakeCatalog.FeatsMap[syntheticFeat.Benefit.Name] = syntheticFeat;

            // Character 1: Level 1 Fighter (non-adept: IL 1 -> Max Maneuver Level 1)
            var char1 = new Character { race = "human" };
            char1.classLevels.Clear();
            char1.classLevels.Add("Fighter");
            char1.feats["Level 1"] = syntheticFeat.Benefit.Name;

            var session1 = new CharacterSession(char1, fakeCatalog);
            var desc = syntheticFeat.Choices[1]; // SubsystemElements choice

            // Level 1 maneuver: valid
            var validWarnings = session1.ValidateProgressionChoice(new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Steel Wind",
                slotKey: "Level 1",
                targetSource: "Feats",
                descriptor: desc
            ));
            Assert.AreEqual(0, validWarnings.Count, "Level 1 maneuver should have 0 warnings for Feats at level 1.");

            // Level 2 maneuver (Mountain Hammer, level 2 Stone Dragon): exceeds max maneuver level 1 for Feats
            var invalidWarnings = session1.ValidateProgressionChoice(new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Mountain Hammer",
                slotKey: "Level 1",
                targetSource: "Feats",
                descriptor: desc
            ));
            Assert.IsTrue(invalidWarnings.Count > 0);
            StringAssert.Contains(invalidWarnings[0].Message, "exceeds maximum maneuver level 1 for Feats");

            // Character 2: Level 3 Warblade (IL 3 -> Max Maneuver Level 2)
            var char2 = new Character { race = "human" };
            char2.classLevels.Clear();
            char2.classLevels.Add("Warblade");
            char2.classLevels.Add("Warblade");
            char2.classLevels.Add("Warblade");
            char2.feats["Level 3"] = syntheticFeat.Benefit.Name;

            var session2 = new CharacterSession(char2, fakeCatalog);

            // Mountain Hammer is valid for Warblade at level 3
            var warbladeWarnings = session2.ValidateProgressionChoice(new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Mountain Hammer",
                slotKey: "Level 3",
                targetSource: "Warblade",
                descriptor: desc
            ));
            Assert.AreEqual(0, warbladeWarnings.Count, "Level 2 maneuver should have 0 warnings for Warblade at level 3.");
        }

        [TestMethod]
        public void ChoiceDescriptor_IsImmutable_HasNoParameterlessConstructor() {
            var type = typeof(ChoiceDescriptor);

            // 1. No parameterless constructor
            var defaultCtor = type.GetConstructor(Type.EmptyTypes);
            Assert.IsNull(defaultCtor, "ChoiceDescriptor must not have a public parameterless constructor.");

            // 2. All properties must be get-only (no public setter)
            var properties = type.GetProperties();
            Assert.IsTrue(properties.Length > 0, "ChoiceDescriptor should have properties.");
            foreach (var prop in properties) {
                Assert.IsNull(prop.GetSetMethod(), $"Property {prop.Name} must be get-only (no public setter).");
            }
        }

        [TestMethod]
        public void Recalculate_FeatWithGenericNonClassChoices_PassesChoicesToFeatEffects() {
            var syntheticFeat = new Feat(new JObject(
                new JProperty("benefit", new JObject(
                    new JProperty("name", "Synthetic Weapon Focus"),
                    new JProperty("modifiers", new JArray(
                        new JObject(
                            new JProperty("affects", "attack bonus:$"),
                            new JProperty("mod", new JObject(
                                new JProperty("type", "untyped"),
                                new JProperty("amounts", new JArray(new JObject(new JProperty("fixed", 1))))
                            ))
                        )
                    ))
                )),
                new JProperty("choices", new JArray(
                    new JObject(
                        new JProperty("id", "weapon"),
                        new JProperty("label", "Choose weapon"),
                        new JProperty("domain", "Weapons")
                    )
                )),
                new JProperty("stats", new JArray(
                    new JObject(new JProperty("name", "attack bonus:Longsword"))
                ))
            ));

            var fakeCatalog = new FakeRulesCatalog(inner: catalog);
            fakeCatalog.FeatsMap[syntheticFeat.Benefit.Name] = syntheticFeat;

            var character = new Character { race = "human" };
            character.classLevels.Clear();
            character.classLevels.Add("Fighter");
            character.feats["Level 1"] = syntheticFeat.Benefit.Name;
            character.featChoices["Level 1:weapon"] = "Longsword";

            var session = new CharacterSession(character, fakeCatalog);
            // Verify that the effect was generated with the choice 'Longsword' applied to $
            var stat = session.GetStatistic("attack bonus:Longsword");
            Assert.IsNotNull(stat, "Modifier for attack bonus:Longsword should be generated from generic feat choice.");
            Assert.AreEqual(1, stat.Value, "Attack bonus for chosen weapon should be 1.");
        }

        [TestMethod]
        public void BonusFeat_SwapToClassWithoutBonusFeat_RetainsInMemory_DoesNotProject_DoesNotSerialize() {
            var character = new Character { race = "human" };
            character.classLevels.Clear();
            character.classLevels.Add("Fighter");
            character.assignedAbilityScores["CON"] = 10;
            character.rolledHitDice.Add(10);
            character.feats["Fighter 1"] = "Toughness";

            var session = new CharacterSession(character, catalog);
            // Fighter base HP = 10 (d10), Toughness = +3 -> 13 HP
            Assert.AreEqual(13, session.GetStatistic(Constants.MAX_HP).Value);
            Assert.AreEqual("Toughness", session.Character.feats["Fighter 1"]);

            // Swap class from Fighter to Crusader
            session.Mutate(c => c.classLevels[0] = "Crusader");

            // 1. Not projected: Crusader base HP is 10 (d10), no Toughness
            Assert.AreEqual(10, session.GetStatistic(Constants.MAX_HP).Value);
            Assert.IsFalse(session.Character.feats.ContainsKey("Fighter 1"), "Inactive feat slot should not be in Character.feats");

            // 2. Retained in memory
            Assert.AreEqual("Toughness", session.RememberedFeats["Fighter 1"]);

            // 3. Not serialized
            var json = session.Character.ToJson();
            var featsJson = (JObject)json["feats"];
            Assert.IsFalse(featsJson.ContainsKey("Fighter 1"), "Inactive feat slot must not be serialized when saving");

            // 4. Restored when swapping back to Fighter
            session.Mutate(c => c.classLevels[0] = "Fighter");
            Assert.AreEqual(13, session.GetStatistic(Constants.MAX_HP).Value);
            Assert.IsTrue(session.Character.feats.ContainsKey("Fighter 1"));
            Assert.AreEqual("Toughness", session.Character.feats["Fighter 1"]);
            Assert.IsFalse(session.RememberedFeats.ContainsKey("Fighter 1"));
        }

        [TestMethod]
        public void BonusFeat_WithFeatChoices_SwapClass_RetainsChoicesInMemory_DoesNotSerialize_RestoresOnSwapBack() {
            var character = new Character { race = "human" };
            character.classLevels.Clear();
            character.classLevels.Add("Fighter");
            character.feats["Fighter 1"] = "Weapon Focus";
            character.featChoices["Fighter 1"] = "Longsword";
            character.featChoices["Fighter 1:weapon"] = "Longsword";

            var session = new CharacterSession(character, catalog);
            Assert.AreEqual("Longsword", session.Character.featChoices["Fighter 1"]);

            // Swap to Crusader
            session.Mutate(c => c.classLevels[0] = "Crusader");

            Assert.IsFalse(session.Character.feats.ContainsKey("Fighter 1"));
            Assert.IsFalse(session.Character.featChoices.ContainsKey("Fighter 1"));
            Assert.IsFalse(session.Character.featChoices.ContainsKey("Fighter 1:weapon"));
            Assert.AreEqual("Weapon Focus", session.RememberedFeats["Fighter 1"]);
            Assert.AreEqual("Longsword", session.RememberedFeatChoices["Fighter 1"]);
            Assert.AreEqual("Longsword", session.RememberedFeatChoices["Fighter 1:weapon"]);

            var json = session.Character.ToJson();
            Assert.IsFalse(((JObject)json["feats"]).ContainsKey("Fighter 1"));
            Assert.IsFalse(((JObject)json["feat choices"]).ContainsKey("Fighter 1"));
            Assert.IsFalse(((JObject)json["feat choices"]).ContainsKey("Fighter 1:weapon"));

            // Swap back to Fighter
            session.Mutate(c => c.classLevels[0] = "Fighter");
            Assert.AreEqual("Weapon Focus", session.Character.feats["Fighter 1"]);
            Assert.AreEqual("Longsword", session.Character.featChoices["Fighter 1"]);
            Assert.AreEqual("Longsword", session.Character.featChoices["Fighter 1:weapon"]);
            Assert.IsFalse(session.RememberedFeats.ContainsKey("Fighter 1"));
            Assert.IsFalse(session.RememberedFeatChoices.ContainsKey("Fighter 1"));
        }

        [TestMethod]
        public void BonusFeat_ActiveClearing_ClearsMemorySoItDoesNotResurrect() {
            var character = new Character { race = "human" };
            character.classLevels.Clear();
            character.classLevels.Add("Fighter");
            character.feats["Fighter 1"] = "Toughness";

            var session = new CharacterSession(character, catalog);
            session.Mutate(c => c.classLevels[0] = "Crusader");
            Assert.AreEqual("Toughness", session.RememberedFeats["Fighter 1"]);

            session.Mutate(c => c.classLevels[0] = "Fighter");
            Assert.AreEqual("Toughness", session.Character.feats["Fighter 1"]);

            // Explicitly clear the selection while active
            session.Mutate(c => {
                c.feats["Fighter 1"] = "";
            });
            Assert.IsFalse(session.RememberedFeats.ContainsKey("Fighter 1"));

            // Swap to Crusader and back to Fighter -> should remain unselected, not resurrect Toughness
            session.Mutate(c => c.classLevels[0] = "Crusader");
            session.Mutate(c => c.classLevels[0] = "Fighter");
            Assert.IsFalse(session.Character.feats.ContainsKey("Fighter 1"));
            Assert.IsFalse(session.RememberedFeats.ContainsKey("Fighter 1"));
        }

        [TestMethod]
        public void RacialBonusFeat_SwapRace_RetainsInMemory_DoesNotProject_RestoresOnSwapBack() {
            var character = new Character { race = "human" };
            character.classLevels.Clear();
            character.classLevels.Add("Crusader");
            character.assignedAbilityScores["CON"] = 10;
            character.rolledHitDice.Add(10);
            character.feats["Human"] = "Toughness";

            var session = new CharacterSession(character, catalog);
            Assert.AreEqual(13, session.GetStatistic(Constants.MAX_HP).Value);

            session.Mutate(c => c.race = "elf");
            // Elf has -2 racial CON penalty (CON 8 -> -1 mod), and no Toughness -> 9 HP
            Assert.AreEqual(9, session.GetStatistic(Constants.MAX_HP).Value);
            Assert.IsFalse(session.Character.feats.ContainsKey("Human"));
            Assert.AreEqual("Toughness", session.RememberedFeats["Human"]);

            var json = session.Character.ToJson();
            Assert.IsFalse(((JObject)json["feats"]).ContainsKey("Human"));

            session.Mutate(c => c.race = "human");
            Assert.AreEqual(13, session.GetStatistic(Constants.MAX_HP).Value);
            Assert.AreEqual("Toughness", session.Character.feats["Human"]);
        }

        [TestMethod]
        public void MartialStudy_InBonusFeatSlot_SwapClass_DeactivatesSubsystemAndRestores() {
            var character = new Character { race = "human" };
            character.classLevels.Clear();
            character.classLevels.Add("Fighter");
            character.feats["Fighter 1"] = "Martial Study";
            character.featChoices["Fighter 1:class"] = "Feats";
            character.featChoices["Fighter 1:maneuver"] = "Steel Wind";

            var session = new CharacterSession(character, catalog);
            Assert.AreEqual(1, session.GetStatistic("maneuvers known:feats").Value);
            Assert.AreEqual(1, session.GetStatistic("initiator level:feats").Value);

            // Swap Fighter to Crusader
            session.Mutate(c => c.classLevels[0] = "Crusader");
            Assert.AreEqual(0, session.GetStatistic("maneuvers known:feats").Value);
            Assert.AreEqual(0, session.GetStatistic("initiator level:feats").Value);
            Assert.IsFalse(session.Character.feats.ContainsKey("Fighter 1"));
            Assert.IsFalse(session.Character.featChoices.ContainsKey("Fighter 1:maneuver"));
            Assert.AreEqual("Martial Study", session.RememberedFeats["Fighter 1"]);
            Assert.AreEqual("Steel Wind", session.RememberedFeatChoices["Fighter 1:maneuver"]);

            // Swap back to Fighter
            session.Mutate(c => c.classLevels[0] = "Fighter");
            Assert.AreEqual(1, session.GetStatistic("maneuvers known:feats").Value);
            Assert.AreEqual(1, session.GetStatistic("initiator level:feats").Value);
            Assert.AreEqual("Martial Study", session.Character.feats["Fighter 1"]);
            Assert.AreEqual("Steel Wind", session.Character.featChoices["Fighter 1:maneuver"]);
        }

        [TestMethod]
        public void LevelDecrease_StashesHigherLevelFeats_RestoresOnLevelIncrease() {
            var character = new Character { race = "human" };
            character.classLevels.Clear();
            character.classLevels.Add("Fighter");
            character.classLevels.Add("Fighter");
            character.classLevels.Add("Fighter"); // Level 3: has Level 1, Fighter 1, Fighter 2, Level 3
            character.feats["Fighter 1"] = "Toughness";
            character.feats["Fighter 2"] = "Power Attack";
            character.feats["Level 3"] = "Iron Will";

            var session = new CharacterSession(character, catalog);
            Assert.IsTrue(session.Character.feats.ContainsKey("Fighter 2"));
            Assert.IsTrue(session.Character.feats.ContainsKey("Level 3"));

            // Level down to level 1 (remove levels 2 and 3)
            session.Mutate(c => {
                c.classLevels.RemoveAt(2);
                c.classLevels.RemoveAt(1);
            });

            Assert.IsTrue(session.Character.feats.ContainsKey("Fighter 1"));
            Assert.IsFalse(session.Character.feats.ContainsKey("Fighter 2"));
            Assert.IsFalse(session.Character.feats.ContainsKey("Level 3"));
            Assert.AreEqual("Power Attack", session.RememberedFeats["Fighter 2"]);
            Assert.AreEqual("Iron Will", session.RememberedFeats["Level 3"]);

            // Level back up to level 3
            session.Mutate(c => {
                c.classLevels.Add("Fighter");
                c.classLevels.Add("Fighter");
            });

            Assert.IsTrue(session.Character.feats.ContainsKey("Fighter 2"));
            Assert.IsTrue(session.Character.feats.ContainsKey("Level 3"));
            Assert.AreEqual("Power Attack", session.Character.feats["Fighter 2"]);
            Assert.AreEqual("Iron Will", session.Character.feats["Level 3"]);
            Assert.IsFalse(session.RememberedFeats.ContainsKey("Fighter 2"));
            Assert.IsFalse(session.RememberedFeats.ContainsKey("Level 3"));
        }

        // Source: Tome of Battle pp. 39, 52; ADR-0017
        [TestMethod]
        public void ValidateProgressionChoice_Level1SlotOnLevel10NonAdept_EmitsWarningWhenManeuverExceedsLevel1Gate() {
            var martialStudyDef = catalog.GetFeat("Martial Study");
            var desc = martialStudyDef.Choices.First(c => c.Id == "maneuver");

            // Level 10 pure Fighter (non-adept)
            var character = new Character { race = "human" };
            character.classLevels.Clear();
            for (int i = 0; i < 10; i++) character.classLevels.Add("Fighter");
            character.feats["Level 1"] = "Martial Study";

            var session = new CharacterSession(character, catalog);

            // Candidate maneuver level 1 (Steel Wind): valid at level 1
            var validWarnings = session.ValidateProgressionChoice(new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Steel Wind",
                slotKey: "Level 1",
                targetSource: "Feats",
                descriptor: desc
            ));
            Assert.AreEqual(0, validWarnings.Count, "Level 1 maneuver must have 0 warnings for Level 1 slot.");

            // Candidate maneuver level 2 (Mountain Hammer): exceeds Level 1 acquisition gate (max level 1)
            var invalidWarnings = session.ValidateProgressionChoice(new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Mountain Hammer",
                slotKey: "Level 1",
                targetSource: "Feats",
                descriptor: desc
            ));
            Assert.IsTrue(invalidWarnings.Count > 0, "Level 2 maneuver must emit warning for Level 1 slot on Level 10 character.");
            StringAssert.Contains(invalidWarnings[0].Message, "exceeds maximum maneuver level 1 for Feats");

            // On the same Level 10 character, a Level 6 slot has IL 3 -> max level 2
            character.feats["Level 6"] = "Martial Study";
            var validLvl6Warnings = session.ValidateProgressionChoice(new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Mountain Hammer",
                slotKey: "Level 6",
                targetSource: "Feats",
                descriptor: desc
            ));
            Assert.AreEqual(0, validLvl6Warnings.Count, "Level 2 maneuver is valid for Level 6 slot (IL 3).");

            // Level 3 maneuver (Iron Heart Surge) exceeds Level 6 gate (max level 2) even though character level 10 IL is 5
            var invalidLvl6Warnings = session.ValidateProgressionChoice(new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Iron Heart Surge",
                slotKey: "Level 6",
                targetSource: "Feats",
                descriptor: desc
            ));
            Assert.IsTrue(invalidLvl6Warnings.Count > 0, "Level 3 maneuver must emit warning for Level 6 slot.");
            StringAssert.Contains(invalidLvl6Warnings[0].Message, "exceeds maximum maneuver level 2 for Feats");
        }

        // Source: Tome of Battle pp. 39, 52; ADR-0017
        [TestMethod]
        public void ValidateProgressionChoice_Level1SlotOnLevel10MartialAdept_EmitsWarningWhenExceedingAcquisitionLevelGate() {
            var martialStudyDef = catalog.GetFeat("Martial Study");
            var desc = martialStudyDef.Choices.First(c => c.Id == "maneuver");

            // Level 10 pure Warblade
            var character = new Character { race = "human" };
            character.classLevels.Clear();
            for (int i = 0; i < 10; i++) character.classLevels.Add("Warblade");
            character.feats["Level 1"] = "Martial Study";

            var session = new CharacterSession(character, catalog);

            // Candidate maneuver level 1: valid for Level 1 slot
            var validWarnings = session.ValidateProgressionChoice(new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Steel Wind",
                slotKey: "Level 1",
                targetSource: "Warblade",
                descriptor: desc
            ));
            Assert.AreEqual(0, validWarnings.Count, "Level 1 maneuver must have 0 warnings for Level 1 Warblade slot.");

            // Candidate maneuver level 2 (Mountain Hammer): exceeds Level 1 Warblade gate (IL 1 -> max level 1)
            var invalidWarnings = session.ValidateProgressionChoice(new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Mountain Hammer",
                slotKey: "Level 1",
                targetSource: "Warblade",
                descriptor: desc
            ));
            Assert.IsTrue(invalidWarnings.Count > 0, "Level 2 maneuver must emit warning for Level 1 slot on Level 10 Warblade.");
            StringAssert.Contains(invalidWarnings[0].Message, "exceeds maximum maneuver level 1 for Warblade");

            // Level 3 slot has Warblade IL 3 -> max level 2
            character.feats["Level 3"] = "Martial Study";
            var validLvl3 = session.ValidateProgressionChoice(new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Mountain Hammer",
                slotKey: "Level 3",
                targetSource: "Warblade",
                descriptor: desc
            ));
            Assert.AreEqual(0, validLvl3.Count, "Level 2 maneuver is valid for Level 3 Warblade slot.");

            var invalidLvl3 = session.ValidateProgressionChoice(new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Iron Heart Surge",
                slotKey: "Level 3",
                targetSource: "Warblade",
                descriptor: desc
            ));
            Assert.IsTrue(invalidLvl3.Count > 0, "Level 3 maneuver must emit warning for Level 3 Warblade slot (max level 2).");
            StringAssert.Contains(invalidLvl3[0].Message, "exceeds maximum maneuver level 2 for Warblade");
        }

        // Source: Tome of Battle pp. 39, 52; ADR-0017
        [TestMethod]
        public void ValidateProgressionChoice_MulticlassAdept_EvaluatesInitiatorLevelAndSourceAsOfAcquisitionLevel() {
            var martialStudyDef = catalog.GetFeat("Martial Study");
            var desc = martialStudyDef.Choices.First(c => c.Id == "maneuver");

            // Multiclass: 4 levels Fighter, then 1 level Warblade, then 5 levels Fighter (total level 10)
            var character = new Character { race = "human" };
            character.classLevels.Clear();
            for (int i = 0; i < 4; i++) character.classLevels.Add("Fighter");
            character.classLevels.Add("Warblade"); // Level 5
            for (int i = 0; i < 5; i++) character.classLevels.Add("Fighter"); // Levels 6..10

            var session = new CharacterSession(character, catalog);

            // Level 1 slot (Fighter 1): non-adept, source resolves to "Feats", IL 1 -> max level 1
            var lvl1Warnings = session.ValidateProgressionChoice(new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Mountain Hammer",
                slotKey: "Level 1",
                targetSource: "Feats",
                descriptor: desc
            ));
            Assert.IsTrue(lvl1Warnings.Count > 0, "Level 2 maneuver must emit warning for Level 1 slot on multiclass character.");
            StringAssert.Contains(lvl1Warnings[0].Message, "exceeds maximum maneuver level 1 for Feats");

            // Level 6 slot (after gaining Warblade at level 5): Warblade IL is 1 + (4/2) = 3 -> max level 2
            // Candidate level 2 (Mountain Hammer) is valid for Warblade
            var lvl6Valid = session.ValidateProgressionChoice(new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Mountain Hammer",
                slotKey: "Level 6",
                targetSource: "Warblade",
                descriptor: desc
            ));
            Assert.AreEqual(0, lvl6Valid.Count, "Level 2 maneuver is valid for Level 6 Warblade slot with IL 3.");

            // Candidate level 3 (Iron Heart Surge) exceeds Warblade max level 2 at level 6
            var lvl6Invalid = session.ValidateProgressionChoice(new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Iron Heart Surge",
                slotKey: "Level 6",
                targetSource: "Warblade",
                descriptor: desc
            ));
            Assert.IsTrue(lvl6Invalid.Count > 0, "Level 3 maneuver must emit warning for Level 6 Warblade slot.");
            StringAssert.Contains(lvl6Invalid[0].Message, "exceeds maximum maneuver level 2 for Warblade");
        }

        // Source: Tome of Battle pp. 39, 44; ADR-0017
        [TestMethod]
        public void ValidateProgressionChoice_DisciplinePrerequisites_EnforcedAsOfAcquisitionLevel() {
            var martialStanceDef = catalog.GetFeat("Martial Stance");
            var stanceDesc = martialStanceDef.Choices.First(c => c.Id == "stance");

            // Multiclass: Level 1 Fighter, Level 2 Crusader, Levels 3..10 Fighter
            var character = new Character { race = "human" };
            character.classLevels.Clear();
            character.classLevels.Add("Fighter");
            character.classLevels.Add("Crusader");
            for (int i = 0; i < 8; i++) character.classLevels.Add("Fighter");

            // Crusader learned a Devoted Spirit maneuver at level 2
            var crusaderState = new SubsystemState();
            crusaderState.Slots["maneuvers"] = new List<SubsystemSlot> {
                new SubsystemSlot("Crusader's Strike", "unreadied", "Crusader") // Devoted Spirit
            };
            character.Subsystems["martial_maneuvers"] = crusaderState;

            var session = new CharacterSession(character, catalog);

            // Level 1 slot (Fighter 1): Crusader not yet taken, Devoted Spirit known = 0
            // Martial Stance for Martial Spirit (Devoted Spirit stance, requires 1 Devoted Spirit maneuver)
            var lvl1Warnings = session.ValidateProgressionChoice(new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Martial Spirit",
                slotKey: "Level 1",
                targetSource: "Feats",
                descriptor: stanceDesc
            ));
            Assert.IsTrue(lvl1Warnings.Count > 0, "Level 1 slot must fail Devoted Spirit prerequisite since Crusader was not yet taken.");
            StringAssert.Contains(lvl1Warnings[0].Message, "Requires 1 Devoted Spirit maneuver, but only 0 known");

            // Level 3 slot (Fighter 1 / Crusader 1 / Fighter 1): Crusader was taken at level 2 -> Devoted Spirit known = 1
            var lvl3Warnings = session.ValidateProgressionChoice(new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Martial Spirit",
                slotKey: "Level 3",
                targetSource: "Feats",
                descriptor: stanceDesc
            ));
            Assert.AreEqual(0, lvl3Warnings.Count, "Level 3 slot satisfies Devoted Spirit prerequisite from Level 2 Crusader grant.");
        }

        // Source: Tome of Battle pp. 39, 44; ADR-0017
        [TestMethod]
        public void ValidateProgressionChoice_AdeptClassSlots_TrimmedToCapacityAtAcquisitionLevel_ForDisciplinePrereqs() {
            var martialStanceDef = catalog.GetFeat("Martial Stance");
            var stanceDesc = martialStanceDef.Choices.First(c => c.Id == "stance");

            // Level 10 Warblade (has 9 maneuver slots total, but only 3 at Level 1)
            var character = new Character { race = "human" };
            character.classLevels.Clear();
            for (int i = 0; i < 10; i++) character.classLevels.Add("Warblade");

            // Slots 0..2 are Iron Heart maneuvers; Slot 3 (learned at later level) is Devoted Spirit
            var warbladeState = new SubsystemState();
            warbladeState.Slots["maneuvers"] = new List<SubsystemSlot> {
                new SubsystemSlot("Steel Wind", "unreadied", "Warblade"),      // Iron Heart (Slot 0)
                new SubsystemSlot("Steely Strike", "unreadied", "Warblade"),   // Iron Heart (Slot 1)
                new SubsystemSlot("Disarming Strike", "unreadied", "Warblade"),// Iron Heart (Slot 2)
                new SubsystemSlot("Crusader's Strike", "unreadied", "Warblade")// Devoted Spirit (Slot 3 - beyond Level 1 capacity)
            };
            character.Subsystems["martial_maneuvers"] = warbladeState;

            var session = new CharacterSession(character, catalog);

            // Level 1 slot: Warblade Level 1 capacity is 3. Slot 3 is pruned in the Level 1 snapshot.
            // Devoted Spirit maneuvers known at Level 1 is 0.
            var lvl1Warnings = session.ValidateProgressionChoice(new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Martial Spirit",
                slotKey: "Level 1",
                targetSource: "Warblade",
                descriptor: stanceDesc
            ));
            Assert.IsTrue(lvl1Warnings.Count > 0, "Level 1 slot must prune maneuvers beyond Level 1 capacity (Slot 3) so Devoted Spirit prereq is unmet.");
            StringAssert.Contains(lvl1Warnings[0].Message, "Requires 1 Devoted Spirit maneuver, but only 0 known");

            // Level 9 slot: Warblade Level 9 capacity is 8. Slot 3 is within capacity.
            var lvl9Warnings = session.ValidateProgressionChoice(new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Martial Spirit",
                slotKey: "Level 9",
                targetSource: "Warblade",
                descriptor: stanceDesc
            ));
            Assert.AreEqual(0, lvl9Warnings.Count, "Level 9 slot retains Slot 3 so Devoted Spirit prereq is satisfied.");
        }

        // Source: Tome of Battle, p. 52
        [TestMethod]
        public void ValidateProgressionChoice_MartialStance_RequiresOneManeuverInDisciplineViaChoiceDescriptorMinimumPrereqCount() {
            var martialStance = catalog.GetFeat("Martial Stance");
            Assert.IsNotNull(martialStance);
            var stanceDescriptor = martialStance.Choices.FirstOrDefault(c => c.Id == "stance");
            Assert.IsNotNull(stanceDescriptor);
            Assert.AreEqual(1, stanceDescriptor.MinimumPrerequisiteCount, "Martial Stance descriptor in feats.json must declare minimumPrerequisiteCount: 1.");

            var character = new Character { race = "human" };
            character.classLevels.Clear();
            character.classLevels.Add("Warblade");

            var session = new CharacterSession(character, catalog);

            // 1. With Martial Stance descriptor (minimumPrerequisiteCount = 1), selecting a 0-prereq stance (e.g. Martial Spirit)
            // with 0 Devoted Spirit maneuvers known emits a prerequisite warning.
            var warningsWithMinimum = session.ValidateProgressionChoice(new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Martial Spirit",
                slotKey: "Level 1",
                targetSource: "Feats",
                descriptor: stanceDescriptor
            ));
            Assert.AreEqual(1, warningsWithMinimum.Count);
            Assert.AreEqual(ValidationCategory.Prerequisite, warningsWithMinimum[0].Category);
            StringAssert.Contains(warningsWithMinimum[0].Message, "Requires 1 Devoted Spirit maneuver, but only 0 known");

            // 2. Data-driven: With a descriptor that has minimumPrerequisiteCount = 0 (or null), selecting Martial Spirit
            // does NOT emit a prerequisite warning because Martial Spirit has intrinsic prerequisiteCount = 0.
            var zeroPrereqDescriptor = new ChoiceDescriptor("stance", "Choose stance", "SubsystemElements", "stance", minimumPrerequisiteCount: 0);
            var warningsWithoutMinimum = session.ValidateProgressionChoice(new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Martial Spirit",
                slotKey: "Level 1",
                targetSource: "Feats",
                descriptor: zeroPrereqDescriptor
            ));
            Assert.AreEqual(0, warningsWithoutMinimum.Count, "0-minimum descriptor must not impose stance prerequisite threshold when element has 0 prereqs.");
        }

        // Source: Tome of Battle, p. 52
        [TestMethod]
        public void ValidateProgressionChoice_InFlightOverlay_LiveSession_SatisfiesPeerPrerequisites() {
            var martialStance = catalog.GetFeat("Martial Stance");
            var stanceDescriptor = martialStance.Choices.First(c => c.Id == "stance");

            var character = new Character { race = "human" };
            character.classLevels.Clear();
            character.classLevels.Add("Fighter");
            character.feats["Human"] = "Martial Study";
            character.feats["Level 1"] = "Martial Stance";

            var session = new CharacterSession(character, catalog);

            // 1. Without in-flight choices, Flame's Blessing in Level 1 has 0 Desert Wind maneuvers known -> warning
            var warningsWithout = session.ValidateProgressionChoice(new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Flame's Blessing",
                slotKey: "Level 1",
                targetSource: "Feats",
                descriptor: stanceDescriptor
            ));
            Assert.AreEqual(1, warningsWithout.Count);
            Assert.AreEqual(ValidationCategory.Prerequisite, warningsWithout[0].Category);
            StringAssert.Contains(warningsWithout[0].Message, "Requires 1 Desert Wind maneuver, but only 0 known");

            // 2. With in-flight CurrentChoices containing Burning Blade for Human slot, Flame's Blessing in Level 1 satisfies prerequisite
            var inFlightChoices = new Dictionary<string, string> {
                ["Human:maneuver"] = "Burning Blade",
                ["Level 1:stance"] = "Flame's Blessing"
            };
            var warningsWith = session.ValidateProgressionChoice(new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Flame's Blessing",
                slotKey: "Level 1",
                targetSource: "Feats",
                descriptor: stanceDescriptor,
                currentChoices: inFlightChoices
            ));
            Assert.AreEqual(0, warningsWith.Count, "In-flight peer choice (Burning Blade in Human slot) must satisfy Desert Wind prerequisite for Level 1 stance.");

            // 3. Vice versa: validating Human slot does NOT see itself or satisfy its own prerequisite
            // If Human slot is Martial Stance and CurrentChoices has ["Human:stance"] = "Flame's Blessing",
            // validating Flame's Blessing in Human slot must not see itself as fulfilling its own prerequisite.
            var humanStanceChar = new Character { race = "human" };
            humanStanceChar.classLevels.Clear();
            humanStanceChar.classLevels.Add("Fighter");
            humanStanceChar.feats["Human"] = "Martial Stance";

            var humanStanceSession = new CharacterSession(humanStanceChar, catalog);
            var selfChoices = new Dictionary<string, string> {
                ["Human:stance"] = "Flame's Blessing"
            };
            var selfWarnings = humanStanceSession.ValidateProgressionChoice(new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Flame's Blessing",
                slotKey: "Human",
                targetSource: "Feats",
                descriptor: stanceDescriptor,
                currentChoices: selfChoices
            ));
            Assert.AreEqual(1, selfWarnings.Count, "Validating Human slot must omit its own in-flight choice so it cannot satisfy its own prerequisite.");
            Assert.AreEqual(ValidationCategory.Prerequisite, selfWarnings[0].Category);
            StringAssert.Contains(selfWarnings[0].Message, "Requires 1 Desert Wind maneuver, but only 0 known");
        }

        // Source: Tome of Battle, p. 52
        [TestMethod]
        public void ValidateProgressionChoice_InFlightOverlay_SnapshotSession_SatisfiesPeerPrerequisites() {
            var martialStance = catalog.GetFeat("Martial Stance");
            var stanceDescriptor = martialStance.Choices.First(c => c.Id == "stance");

            var character = new Character { race = "human" };
            character.classLevels.Clear();
            character.classLevels.Add("Fighter");
            character.feats["Human"] = "Martial Study";
            character.feats["Level 1"] = "Martial Stance";

            var session = new CharacterSession(character, catalog);
            var snapSession = session.GetSessionAtLevel(1);

            // 1. Without in-flight choices on snapshot -> warning
            var warningsWithout = snapSession.ValidateProgressionChoice(new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Flame's Blessing",
                slotKey: "Level 1",
                targetSource: "Feats",
                descriptor: stanceDescriptor
            ));
            Assert.AreEqual(1, warningsWithout.Count);
            Assert.AreEqual(ValidationCategory.Prerequisite, warningsWithout[0].Category);

            // 2. With in-flight choices on snapshot -> 0 warnings
            var inFlightChoices = new Dictionary<string, string> {
                ["Human:maneuver"] = "Burning Blade",
                ["Level 1:stance"] = "Flame's Blessing"
            };
            var warningsWith = snapSession.ValidateProgressionChoice(new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Flame's Blessing",
                slotKey: "Level 1",
                targetSource: "Feats",
                descriptor: stanceDescriptor,
                currentChoices: inFlightChoices
            ));
            Assert.AreEqual(0, warningsWith.Count, "In-flight overlay on snapshot session must project peer choices and satisfy prerequisites.");
        }

        // Source: Tome of Battle, p. 52
        [TestMethod]
        public void ValidateProgressionChoice_InFlightOverlay_DoesNotPolluteLazyLevelSessionCache() {
            var martialStance = catalog.GetFeat("Martial Stance");
            var stanceDescriptor = martialStance.Choices.First(c => c.Id == "stance");

            var character = new Character { race = "human" };
            character.classLevels.Clear();
            character.classLevels.Add("Fighter");
            character.feats["Human"] = "Martial Study";
            character.feats["Level 1"] = "Martial Stance";

            var session = new CharacterSession(character, catalog);

            // Prime lazy cache
            var cachedSnapBefore = session.GetSessionAtLevel(1);
            Assert.IsFalse(cachedSnapBefore.Character.featChoices.ContainsKey("Human:maneuver"));

            // Validate with in-flight overlay
            var inFlightChoices = new Dictionary<string, string> {
                ["Human:maneuver"] = "Burning Blade"
            };
            var warnings = session.ValidateProgressionChoice(new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Flame's Blessing",
                slotKey: "Level 1",
                targetSource: "Feats",
                descriptor: stanceDescriptor,
                currentChoices: inFlightChoices
            ));
            Assert.AreEqual(0, warnings.Count);

            // Cached snapshot must remain clean and unpolluted
            var cachedSnapAfter = session.GetSessionAtLevel(1);
            Assert.AreSame(cachedSnapBefore, cachedSnapAfter, "Clean snapshot must still be retrieved from lazy cache.");
            Assert.IsFalse(cachedSnapAfter.Character.featChoices.ContainsKey("Human:maneuver"), "Cache must not be polluted with in-flight overlay choices.");

            // Validating on clean cached snapshot without CurrentChoices must still emit prerequisite warning
            var cleanWarnings = cachedSnapAfter.ValidateProgressionChoice(new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Flame's Blessing",
                slotKey: "Level 1",
                targetSource: "Feats",
                descriptor: stanceDescriptor
            ));
            Assert.AreEqual(1, cleanWarnings.Count);
        }

        // Source: Tome of Battle, p. 52
        [TestMethod]
        public void ValidateProgressionChoice_InFlightOverlay_ExcludesPreviouslyCommittedSlotChoices() {
            var martialStance = catalog.GetFeat("Martial Stance");
            var stanceDescriptor = martialStance.Choices.First(c => c.Id == "stance");

            var character = new Character { race = "human" };
            character.classLevels.Clear();
            character.classLevels.Add("Fighter");
            character.feats["Human"] = "Martial Study";
            character.feats["Level 1"] = "Martial Stance";
            // Pre-committed choices: Level 1 was previously saved as Flame's Blessing
            character.featChoices["Level 1:stance"] = "Flame's Blessing";

            var session = new CharacterSession(character, catalog);

            // When validating Level 1 slot (with 0 Desert Wind maneuvers), the slot currently being validated
            // must be excluded from the snapshot's active slots/choices so it does not satisfy itself
            var warnings = session.ValidateProgressionChoice(new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Flame's Blessing",
                slotKey: "Level 1",
                targetSource: "Feats",
                descriptor: stanceDescriptor,
                currentChoices: new Dictionary<string, string> {
                    ["Level 1:stance"] = "Flame's Blessing"
                }
            ));

            Assert.AreEqual(1, warnings.Count, "Pre-committed choice on the slot being validated must be excluded so it cannot satisfy itself.");
            Assert.AreEqual(ValidationCategory.Prerequisite, warnings[0].Category);
            StringAssert.Contains(warnings[0].Message, "Requires 1 Desert Wind maneuver, but only 0 known");
        }

        // Source: Tome of Battle, p. 52
        [TestMethod]
        public void ValidateProgressionChoice_InFlightOverlay_ProjectsGrantedSlotsIntoSubsystemEngineActiveSlots() {
            var character = new Character { race = "human" };
            character.classLevels.Clear();
            character.classLevels.Add("Fighter");
            character.feats["Human"] = "Martial Study";
            character.feats["Level 1"] = "Martial Stance";

            var session = new CharacterSession(character, catalog);

            var inFlightChoices = new Dictionary<string, string> {
                ["Human:maneuver"] = "Burning Blade",
                ["Level 1:stance"] = "Flame's Blessing"
            };

            var snapSession = session.CreateSessionAtLevel(1, includeEquipped: false, inFlightChoices, validatingSlotKey: "Level 1");

            // Verify snapSession state
            Assert.AreEqual("Burning Blade", snapSession.Character.featChoices["Human:maneuver"]);
            Assert.IsFalse(snapSession.Character.featChoices.ContainsKey("Level 1:stance"), "Validating slot choice must be omitted from snapSession featChoices.");

            // Verify subsystem active slots
            var engine = snapSession.GetSubsystem("martial_maneuvers");
            Assert.IsNotNull(engine);
            var maneuversGroup = engine.GetSlotGroup("maneuvers");
            Assert.IsNotNull(maneuversGroup);
            Assert.IsTrue(
                maneuversGroup.Slots.Any(s => string.Equals(s.Element, "Burning Blade", StringComparison.OrdinalIgnoreCase) && string.Equals(s.FeatSlot, "Human", StringComparison.OrdinalIgnoreCase)),
                "Peer feat (Martial Study in Human slot) must project granted slot into subsystem engine active slot groups.");

            var stancesGroup = engine.GetSlotGroup("stances");
            Assert.IsNotNull(stancesGroup);
            Assert.IsFalse(
                stancesGroup.Slots.Any(s => string.Equals(s.FeatSlot, "Level 1", StringComparison.OrdinalIgnoreCase)),
                "Slot being validated (Level 1) must not project active slots into snapshot subsystem engine.");
        }

        // Source: Tome of Battle, p. 52
        [TestMethod]
        public void ValidateProgressionChoice_InFlightOverlay_DualPassSatisfiedByGear_CategorizesAsItemDependent() {
            var itemEffect = new Effect("Ring of the Desert Sun")
                .AddSlot(new SubsystemSlotGrant("martial_maneuvers", "maneuvers", "Burning Blade", "Item"));
            var ringItem = new Item {
                ItemSlot = Equipped.Slot.RING1,
                ItemEffect = itemEffect,
                Weight = 1
            };

            var fakeCatalog = new FakeRulesCatalog(inner: catalog);
            fakeCatalog.ItemsMap["Ring of the Desert Sun"] = ringItem;

            var martialStanceDef = fakeCatalog.GetFeat("Martial Stance");
            var stanceDesc = martialStanceDef.Choices.First(c => c.Id == "stance");

            var character = new Character { race = "human" };
            character.classLevels.Clear();
            character.classLevels.Add("Fighter");
            character.feats["Human"] = "Martial Study";
            character.feats["Level 1"] = "Martial Stance";
            character.equipped.SlottedItems[Equipped.Slot.RING1] = "Ring of the Desert Sun";

            var session = new CharacterSession(character, fakeCatalog);

            // In-flight Human slot has Vanguard Strike (Devoted Spirit)
            // Equipped ring grants Burning Blade (Desert Wind)
            // Validating Flame's Blessing (level 1 Desert Wind stance requiring 1 Desert Wind maneuver) in Level 1 slot:
            // Intrinsic: in-flight overlay gives Vanguard Strike, 0 Desert Wind maneuvers -> unmet
            // Equipped: overlay gives Vanguard Strike + Ring gives Burning Blade -> 1 Desert Wind maneuver -> satisfied -> ItemDependent
            var inFlightChoices = new Dictionary<string, string> {
                ["Human:maneuver"] = "Vanguard Strike"
            };

            var context = new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Flame's Blessing",
                slotKey: "Level 1",
                targetSource: "Feats",
                descriptor: stanceDesc,
                currentChoices: inFlightChoices
            );

            var warnings = session.ValidateProgressionChoice(context);

            Assert.AreEqual(1, warnings.Count);
            Assert.AreEqual(WarningKind.ItemDependent, warnings[0].Kind, "Prerequisite satisfied by combining in-flight peer choice with gear must be ItemDependent.");
            Assert.AreEqual(ValidationCategory.Prerequisite, warnings[0].Category);
            StringAssert.Contains(warnings[0].Message, "Requires 1 Desert Wind maneuver, but only 0 known.");
        }
    }
}
