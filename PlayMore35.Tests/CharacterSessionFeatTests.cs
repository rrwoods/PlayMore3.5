using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Global;
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
            var engine = (SubsystemEngine)session.Subsystems.Get("martial_maneuvers");
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
            var engine = (SubsystemEngine)session.Subsystems.Get("martial_maneuvers");
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
            var validWarnings = session1.ValidateFeatChoice("Level 1", desc, "Steel Wind", "Feats");
            Assert.AreEqual(0, validWarnings.Count, "Level 1 maneuver should have 0 warnings for Feats at level 1.");

            // Level 2 maneuver (Mountain Hammer, level 2 Stone Dragon): exceeds max maneuver level 1 for Feats
            var invalidWarnings = session1.ValidateFeatChoice("Level 1", desc, "Mountain Hammer", "Feats");
            Assert.IsTrue(invalidWarnings.Count > 0);
            StringAssert.Contains(invalidWarnings[0], "exceeds maximum maneuver level 1 for Feats");

            // Character 2: Level 3 Warblade (IL 3 -> Max Maneuver Level 2)
            var char2 = new Character { race = "human" };
            char2.classLevels.Clear();
            char2.classLevels.Add("Warblade");
            char2.classLevels.Add("Warblade");
            char2.classLevels.Add("Warblade");
            char2.feats["Level 3"] = syntheticFeat.Benefit.Name;

            var session2 = new CharacterSession(char2, fakeCatalog);

            // Mountain Hammer is valid for Warblade at level 3
            var warbladeWarnings = session2.ValidateFeatChoice("Level 3", desc, "Mountain Hammer", "Warblade");
            Assert.AreEqual(0, warbladeWarnings.Count, "Level 2 maneuver should have 0 warnings for Warblade at level 3.");
        }

        [TestMethod]
        public void ChoiceDescriptor_IsImmutable_HasNoParameterlessConstructor_AndHasJsonConstructor() {
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

            // 3. The constructor has [JsonConstructor]
            var jsonCtor = type.GetConstructors()
                .FirstOrDefault(c => c.GetCustomAttributes(typeof(Newtonsoft.Json.JsonConstructorAttribute), false).Any());
            Assert.IsNotNull(jsonCtor, "ChoiceDescriptor must have a constructor decorated with [JsonConstructor].");
        }

        [TestMethod]
        public void GetAccessibleSubsystemElements_FiltersByAcquisitionLevelAndTargetSource() {
            var character = new Character { race = "human" };
            character.classLevels.Clear();
            character.classLevels.Add("Fighter");
            var session = new CharacterSession(character, catalog);

            // Level 1 Fighter with target "Feats": max maneuver level = 1
            var level1Elements = session.GetAccessibleSubsystemElements("SubsystemElements", "maneuver", "Feats");
            Assert.IsTrue(level1Elements.Any(e => e.EndsWith("Steel Wind")), "Level 1 maneuvers should be included.");
            Assert.IsFalse(level1Elements.Any(e => e.EndsWith("Disarming Strike")), "Level 2 maneuvers should be excluded for Level 1 Feats.");
            Assert.IsFalse(level1Elements.Any(e => e.EndsWith("Punishing Stance")), "Stances should be excluded when filter is maneuver.");

            // Stance filter: only stances returned
            var stanceElements = session.GetAccessibleSubsystemElements("SubsystemElements", "stance", "Feats");
            Assert.IsTrue(stanceElements.Any(e => e.EndsWith("Punishing Stance")), "Stances should be included.");
            Assert.IsFalse(stanceElements.Any(e => e.EndsWith("Steel Wind")), "Maneuvers should be excluded when filter is stance.");

            // Level 3 Warblade: max maneuver level = 2
            character.classLevels.Clear();
            character.classLevels.Add("Warblade");
            character.classLevels.Add("Warblade");
            character.classLevels.Add("Warblade");
            var session2 = new CharacterSession(character, catalog);

            var level3Elements = session2.GetAccessibleSubsystemElements("SubsystemElements", "maneuver", "Warblade");
            Assert.IsTrue(level3Elements.Any(e => e.EndsWith("Steel Wind")), "Level 1 maneuvers should be included.");
            Assert.IsTrue(level3Elements.Any(e => e.EndsWith("Disarming Strike")), "Level 2 maneuvers should be included for Level 3 Warblade.");
            Assert.IsFalse(level3Elements.Any(e => e.EndsWith("Iron Heart Surge")), "Level 3 maneuvers should be excluded for Level 3 Warblade.");
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
    }
}
