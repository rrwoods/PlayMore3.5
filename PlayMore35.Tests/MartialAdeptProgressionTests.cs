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
    public class MartialAdeptProgressionTests {
        private IRulesCatalog catalog;

        [TestInitialize]
        public void SetUp() {
            catalog = new GameRulesCatalog();
        }

        // Source: Tome of Battle, p. 22 (Table 1–3: The Warblade)
        [TestMethod]
        public void Warblade_SingleClassProgression_EvaluatesStatisticsCorrectly() {
            var warblade = catalog.GetClass("Warblade");
            Assert.IsNotNull(warblade, "Warblade class must be defined in catalog.");
            Assert.AreEqual(12, warblade.HitDie);
            Assert.AreEqual(4, warblade.SkillPoints);
            Assert.AreEqual(CharacterClass.BonusProgression.FAST, warblade.BaseAttackBonus);
            Assert.AreEqual(CharacterClass.BonusProgression.FAST, warblade.BaseFortSave);
            Assert.AreEqual(CharacterClass.BonusProgression.SLOW, warblade.BaseRefSave);
            Assert.AreEqual(CharacterClass.BonusProgression.SLOW, warblade.BaseWillSave);

            int[] expectedKnown = { 3, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13 };
            int[] expectedReadied = { 3, 3, 3, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 7 };
            int[] expectedStances = { 1, 1, 1, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4 };
            int[] expectedMaxLevel = { 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 9, 9 };

            for (int lvl = 1; lvl <= 20; lvl++) {
                var pool = new EffectPool(new Character());
                pool.GenerateClassEffects(warblade, lvl);
                for (int i = 0; i < lvl && i < warblade.ClassFeatures.Count; i++) {
                    foreach (var f in warblade.ClassFeatures[i]) {
                        pool.AddEffect(f);
                    }
                }

                var ctx = new FakeCharacterContext(effects: pool, catalog: catalog);
                var engine = new SubsystemEngine(catalog.GetSubsystem("martial_maneuvers"), ctx);
                engine.ContributeEffects(pool);

                Assert.AreEqual(lvl, pool.GetStatValue("initiator level:warblade", null), $"Warblade level {lvl} IL");
                Assert.AreEqual(expectedKnown[lvl - 1], pool.GetStatValue("maneuvers known:warblade", null), $"Warblade level {lvl} known");
                Assert.AreEqual(expectedReadied[lvl - 1], pool.GetStatValue("maneuvers readied:warblade", null), $"Warblade level {lvl} readied");
                Assert.AreEqual(expectedStances[lvl - 1], pool.GetStatValue("stances known:warblade", null), $"Warblade level {lvl} stances");
                Assert.AreEqual(expectedMaxLevel[lvl - 1], pool.GetStatValue("max maneuver level:warblade", null), $"Warblade level {lvl} max maneuver level");
                int expectedActive = (lvl == 20) ? 2 : 1;
                Assert.AreEqual(expectedActive, pool.GetStatValue("active stances", null), $"Warblade level {lvl} active stances");
            }
        }

        // Source: Tome of Battle, p. 8 (Table 1–1: The Crusader)
        [TestMethod]
        public void Crusader_SingleClassProgression_EvaluatesStatisticsCorrectly() {
            var crusader = catalog.GetClass("Crusader");
            Assert.IsNotNull(crusader, "Crusader class must be defined in catalog.");
            Assert.AreEqual(10, crusader.HitDie);
            Assert.AreEqual(2, crusader.SkillPoints);
            Assert.AreEqual(CharacterClass.BonusProgression.FAST, crusader.BaseAttackBonus);
            Assert.AreEqual(CharacterClass.BonusProgression.FAST, crusader.BaseFortSave);
            Assert.AreEqual(CharacterClass.BonusProgression.SLOW, crusader.BaseRefSave);
            Assert.AreEqual(CharacterClass.BonusProgression.SLOW, crusader.BaseWillSave);

            int[] expectedKnown = { 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13, 14, 14 };
            int[] expectedReadied = { 5, 5, 5, 5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 7 };
            int[] expectedGranted = { 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 5 };
            int[] expectedStances = { 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 4 };
            int[] expectedMaxLevel = { 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 9, 9 };

            for (int lvl = 1; lvl <= 20; lvl++) {
                var pool = new EffectPool(new Character());
                pool.GenerateClassEffects(crusader, lvl);
                for (int i = 0; i < lvl && i < crusader.ClassFeatures.Count; i++) {
                    foreach (var f in crusader.ClassFeatures[i]) {
                        pool.AddEffect(f);
                    }
                }

                var ctx = new FakeCharacterContext(effects: pool, catalog: catalog);
                var engine = new SubsystemEngine(catalog.GetSubsystem("martial_maneuvers"), ctx);
                engine.ContributeEffects(pool);

                Assert.AreEqual(lvl, pool.GetStatValue("initiator level:crusader", null), $"Crusader level {lvl} IL");
                Assert.AreEqual(expectedKnown[lvl - 1], pool.GetStatValue("maneuvers known:crusader", null), $"Crusader level {lvl} known");
                Assert.AreEqual(expectedReadied[lvl - 1], pool.GetStatValue("maneuvers readied:crusader", null), $"Crusader level {lvl} readied");
                Assert.AreEqual(expectedGranted[lvl - 1], pool.GetStatValue("maneuvers granted:crusader", null), $"Crusader level {lvl} granted");
                Assert.AreEqual(expectedStances[lvl - 1], pool.GetStatValue("stances known:crusader", null), $"Crusader level {lvl} stances");
                Assert.AreEqual(expectedMaxLevel[lvl - 1], pool.GetStatValue("max maneuver level:crusader", null), $"Crusader level {lvl} max maneuver level");
                Assert.AreEqual(1, pool.GetStatValue("active stances", null), $"Crusader level {lvl} active stances");
            }
        }

        // Source: Tome of Battle, p. 16 (Table 1–2: The Swordsage)
        [TestMethod]
        public void Swordsage_SingleClassProgression_EvaluatesStatisticsCorrectly() {
            var swordsage = catalog.GetClass("Swordsage");
            Assert.IsNotNull(swordsage, "Swordsage class must be defined in catalog.");
            Assert.AreEqual(8, swordsage.HitDie);
            Assert.AreEqual(6, swordsage.SkillPoints);
            Assert.AreEqual(CharacterClass.BonusProgression.MEDIUM, swordsage.BaseAttackBonus);
            Assert.AreEqual(CharacterClass.BonusProgression.SLOW, swordsage.BaseFortSave);
            Assert.AreEqual(CharacterClass.BonusProgression.FAST, swordsage.BaseRefSave);
            Assert.AreEqual(CharacterClass.BonusProgression.FAST, swordsage.BaseWillSave);

            int[] expectedKnown = { 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25 };
            int[] expectedReadied = { 4, 4, 5, 5, 6, 6, 6, 7, 7, 7, 8, 8, 8, 9, 9, 9, 10, 10, 10, 11 };
            int[] expectedStances = { 1, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5, 6 };
            int[] expectedMaxLevel = { 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 9, 9 };

            for (int lvl = 1; lvl <= 20; lvl++) {
                var pool = new EffectPool(new Character());
                pool.GenerateClassEffects(swordsage, lvl);
                for (int i = 0; i < lvl && i < swordsage.ClassFeatures.Count; i++) {
                    foreach (var f in swordsage.ClassFeatures[i]) {
                        pool.AddEffect(f);
                    }
                }

                var ctx = new FakeCharacterContext(effects: pool, catalog: catalog);
                var engine = new SubsystemEngine(catalog.GetSubsystem("martial_maneuvers"), ctx);
                engine.ContributeEffects(pool);

                Assert.AreEqual(lvl, pool.GetStatValue("initiator level:swordsage", null), $"Swordsage level {lvl} IL");
                Assert.AreEqual(expectedKnown[lvl - 1], pool.GetStatValue("maneuvers known:swordsage", null), $"Swordsage level {lvl} known");
                Assert.AreEqual(expectedReadied[lvl - 1], pool.GetStatValue("maneuvers readied:swordsage", null), $"Swordsage level {lvl} readied");
                Assert.AreEqual(expectedStances[lvl - 1], pool.GetStatValue("stances known:swordsage", null), $"Swordsage level {lvl} stances");
                Assert.AreEqual(expectedMaxLevel[lvl - 1], pool.GetStatValue("max maneuver level:swordsage", null), $"Swordsage level {lvl} max maneuver level");
                Assert.AreEqual(1, pool.GetStatValue("active stances", null), $"Swordsage level {lvl} active stances");
            }
        }

        // Source: Tome of Battle, p. 39 (Multiclass Initiator Level Formula: Class Level + floor(0.5 * Other Levels))
        [TestMethod]
        public void Multiclass_WarbladeAndFighter_ComputesInitiatorLevelCorrectly() {
            var character = new Character();
            character.classLevels.Clear();
            character.classLevels.Add("Warblade");
            character.classLevels.Add("Warblade");
            character.classLevels.Add("Warblade");
            character.classLevels.Add("Fighter");
            character.classLevels.Add("Fighter");

            var session = new CharacterSession(character, catalog);

            // Total character level = 5
            Assert.AreEqual(5, session.GetStatValue("character level", 0));
            Assert.AreEqual(3, session.GetStatValue("Warblade level", 0));
            Assert.AreEqual(2, session.GetStatValue("Fighter level", 0));

            // Other levels for warblade = 5 - 3 = 2
            Assert.AreEqual(2, session.GetStatValue("other levels:warblade", 0));

            // IL for Warblade: 3 + floor(0.5 * 2) = 4
            Assert.AreEqual(4, session.GetStatValue("initiator level:warblade", 0));

            // Max maneuver level for IL 4: min(9, ceil(4 / 2)) = 2
            Assert.AreEqual(2, session.GetStatValue("max maneuver level:warblade", 0));
        }

        // Source: Tome of Battle, p. 39 (Multiclass Initiator Level Formula across two adept classes)
        [TestMethod]
        public void Multiclass_WarbladeAndCrusader_CalculatesIndependentInitiatorLevels() {
            var character = new Character();
            character.classLevels.Clear();
            for (int i = 0; i < 4; i++) character.classLevels.Add("Warblade");
            for (int i = 0; i < 2; i++) character.classLevels.Add("Crusader");

            var session = new CharacterSession(character, catalog);

            Assert.AreEqual(6, session.GetStatValue("character level", 0));

            // Warblade: 4 + floor(0.5 * 2) = 5
            Assert.AreEqual(5, session.GetStatValue("initiator level:warblade", 0));
            // Max maneuver level for Warblade IL 5: ceil(5 / 2) = 3
            Assert.AreEqual(3, session.GetStatValue("max maneuver level:warblade", 0));

            // Crusader: 2 + floor(0.5 * 4) = 4
            Assert.AreEqual(4, session.GetStatValue("initiator level:crusader", 0));
            // Max maneuver level for Crusader IL 4: ceil(4 / 2) = 2
            Assert.AreEqual(2, session.GetStatValue("max maneuver level:crusader", 0));
        }

        // Source: Tome of Battle, pp. 39, 52 (Martial Study Feat Initiator Level: floor(Character Level / 2))
        [TestMethod]
        public void Feats_MartialStudy_NonAdeptProgression() {
            var character = new Character();
            character.classLevels.Clear();
            for (int i = 0; i < 4; i++) character.classLevels.Add("Fighter");
            character.feats["Level 1"] = "Martial Study";

            var session = new CharacterSession(character, catalog);

            Assert.AreEqual(4, session.GetStatValue("character level", 0));

            // Feat IL: floor(4 / 2) = 2
            Assert.AreEqual(2, session.GetStatValue("initiator level:feats", 0));
            // Max maneuver level for Feat IL 2: ceil(2 / 2) = 1
            Assert.AreEqual(1, session.GetStatValue("max maneuver level:feats", 0));
            Assert.AreEqual(1, session.GetStatValue("maneuvers known:feats", 0));
            Assert.AreEqual(1, session.GetStatValue("maneuvers readied:feats", 0));
            Assert.AreEqual(0, session.GetStatValue("stances known:feats", 0));
        }

        // Source: Tome of Battle, pp. 39, 52 (Rules dysfunction fix: Martial Study grants at least IL 1 at level 1)
        [TestMethod]
        public void Feats_MartialStudy_Level1Character_ResolvesDysfunctionAndActivatesEngine() {
            var character = new Character();
            character.classLevels.Clear();
            character.classLevels.Add("Fighter");
            character.feats["Level 1"] = "Martial Study";

            var session = new CharacterSession(character, catalog);

            Assert.AreEqual(1, session.GetStatValue("character level", 0));
            // Dysfunction fix: max(1, floor(1 / 2)) = 1
            Assert.AreEqual(1, session.GetStatValue("initiator level:feats", 0));
            Assert.AreEqual(1, session.GetStatValue("max maneuver level:feats", 0));
            Assert.AreEqual(1, session.GetStatValue("maneuvers known:feats", 0));
            Assert.AreEqual(1, session.GetStatValue("maneuvers readied:feats", 0));

            var engine = (SubsystemEngine)session.Subsystems.Get("martial_maneuvers");
            Assert.IsNotNull(engine);
            Assert.IsTrue(engine.IsActive, "SubsystemEngine must be active for level 1 character with Martial Study.");
        }

        // Source: Tome of Battle, pp. 39, 52; ADR-0012
        [TestMethod]
        public void Feats_MartialStudyAndMartialStance_Level1_InternalBonusDoesNotStack() {
            var character = new Character { race = "human" };
            character.classLevels.Clear();
            character.classLevels.Add("Fighter");
            character.feats["Level 1"] = "Martial Study";
            character.feats["Human"] = "Martial Stance";

            var session = new CharacterSession(character, catalog);

            Assert.AreEqual(1, session.GetStatValue("character level", 0));
            // Both feats grant IL 1 with type "internal"; internal bonus does not stack
            Assert.AreEqual(1, session.GetStatValue("initiator level:feats", 0));
            Assert.AreEqual(1, session.GetStatValue("max maneuver level:feats", 0));
            Assert.AreEqual(1, session.GetStatValue("maneuvers known:feats", 0));
            Assert.AreEqual(1, session.GetStatValue("stances known:feats", 0));

            var engine = (SubsystemEngine)session.Subsystems.Get("martial_maneuvers");
            Assert.IsNotNull(engine);
            Assert.IsTrue(engine.IsActive);
        }

        // Source: Tome of Battle, pp. 39, 52 (Martial Study and Martial Stance Feats on Level 6 Character)
        [TestMethod]
        public void Feats_MartialStudyAndMartialStance_CombinedProgression() {
            var character = new Character();
            character.classLevels.Clear();
            for (int i = 0; i < 6; i++) character.classLevels.Add("Fighter");
            character.feats["Level 1"] = "Martial Study";
            character.feats["Level 3"] = "Martial Stance";

            var session = new CharacterSession(character, catalog);

            Assert.AreEqual(6, session.GetStatValue("character level", 0));

            // Feat IL: floor(6 / 2) = 3
            Assert.AreEqual(3, session.GetStatValue("initiator level:feats", 0));
            // Max maneuver level for Feat IL 3: ceil(3 / 2) = 2
            Assert.AreEqual(2, session.GetStatValue("max maneuver level:feats", 0));
            Assert.AreEqual(1, session.GetStatValue("maneuvers known:feats", 0));
            Assert.AreEqual(1, session.GetStatValue("maneuvers readied:feats", 0));
            Assert.AreEqual(1, session.GetStatValue("stances known:feats", 0));
        }

        // Source: Tome of Battle, pp. 5, 41–44 (Nine Disciplines and ManeuverType Categories)
        [TestMethod]
        public void ManeuversCatalog_LoadsAllDisciplinesAndAllManeuverTypes() {
            var martialDef = catalog.GetSubsystem("martial_maneuvers");
            Assert.IsNotNull(martialDef);
            var maneuvers = martialDef.Elements.Values.ToList();
            Assert.IsTrue(maneuvers.Count >= 190, $"Catalog should contain complete maneuvers, found: {maneuvers.Count}");

            string[] expectedDisciplines = {
                "Desert Wind", "Devoted Spirit", "Diamond Mind",
                "Iron Heart", "Setting Sun", "Shadow Hand",
                "Stone Dragon", "Tiger Claw", "White Raven"
            };

            foreach (var disc in expectedDisciplines) {
                Assert.IsTrue(maneuvers.Any(m => m.GetProperty<string>("discipline") == disc), $"Discipline '{disc}' should have maneuvers in catalog.");
            }

            var types = maneuvers.Select(m => m.Type).Distinct().ToList();
            Assert.IsTrue(types.Contains("Strike"), "Should contain strikes.");
            Assert.IsTrue(types.Contains("Boost"), "Should contain boosts.");
            Assert.IsTrue(types.Contains("Counter"), "Should contain counters.");
            Assert.IsTrue(types.Contains("Stance"), "Should contain stances.");
            Assert.IsTrue(types.Contains("Other"), "Should contain other maneuvers (e.g. Iron Heart Surge).");

            var ihSurge = martialDef.Elements.TryGetValue("Iron Heart Surge", out var surge) ? surge : null;
            Assert.IsNotNull(ihSurge);
            Assert.AreEqual("Other", ihSurge.Type);
            Assert.AreEqual("Iron Heart", ihSurge.GetProperty<string>("discipline"));
            Assert.AreEqual(3, ihSurge.GetProperty<int>("level"));
        }

        // Source: Tome of Battle, pp. 37–95 (Complete Maneuvers Catalog Deserialization and Integrity)
        [TestMethod]
        public void ManeuversCatalog_AllEntriesDeserializeValidly_AndMatchSchema() {
            var martialDef = catalog.GetSubsystem("martial_maneuvers");
            Assert.IsNotNull(martialDef);
            var maneuvers = martialDef.Elements.Values.ToList();
            Assert.IsTrue(maneuvers.Count >= 190, $"Expected full ~190+ maneuver catalog, found: {maneuvers.Count}");

            var validDisciplines = new HashSet<string> {
                "Desert Wind", "Devoted Spirit", "Diamond Mind",
                "Iron Heart", "Setting Sun", "Shadow Hand",
                "Stone Dragon", "Tiger Claw", "White Raven"
            };
            var validActionTypes = new HashSet<string> {
                "standard", "swift", "immediate", "full-round", "move"
            };
            var validTypes = new HashSet<string> {
                "Strike", "Boost", "Counter", "Stance", "Other"
            };

            foreach (var m in maneuvers) {
                Assert.IsFalse(string.IsNullOrWhiteSpace(m.Name), "Maneuver name must not be empty.");
                var disc = m.GetProperty<string>("discipline");
                var level = m.GetProperty<int>("level");
                var actionType = m.GetProperty<string>("actionType");
                var prereqCount = m.GetProperty<int>("prerequisiteCount");
                var prereqs = m.GetProperty<string>("prerequisites");
                var desc = m.GetProperty<string>("description");

                Assert.IsTrue(validDisciplines.Contains(disc), $"Maneuver '{m.Name}' has invalid discipline '{disc}'.");
                Assert.IsTrue(level >= 1 && level <= 9, $"Maneuver '{m.Name}' level {level} must be between 1 and 9.");
                Assert.IsTrue(validTypes.Contains(m.Type), $"Maneuver '{m.Name}' has invalid type '{m.Type}'.");
                Assert.IsTrue(validActionTypes.Contains(actionType), $"Maneuver '{m.Name}' has invalid action type '{actionType}'.");
                Assert.IsTrue(prereqCount >= 0, $"Maneuver '{m.Name}' has negative prerequisite count {prereqCount}.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(desc), $"Maneuver '{m.Name}' must have non-empty description.");

                if (prereqCount == 0) {
                    Assert.IsTrue(string.IsNullOrEmpty(prereqs), $"Maneuver '{m.Name}' with count 0 should have empty prerequisites string.");
                } else {
                    Assert.IsTrue(prereqs.StartsWith(prereqCount.ToString()),
                        $"Maneuver '{m.Name}' prerequisite string '{prereqs}' should start with count {prereqCount}.");
                }
            }

            foreach (var disc in validDisciplines) {
                int count = maneuvers.Count(m => m.GetProperty<string>("discipline") == disc);
                Assert.IsTrue(count >= 15, $"Discipline '{disc}' should have at least 15 maneuvers, found {count}.");
            }
        }

        // Source: Tome of Battle, pp. 37–46
        [TestMethod]
        public void GameRulesCatalog_LoadsManeuversCatalogCleanly() {
            var catalog = new GameRulesCatalog();
            var martialDef = catalog.GetSubsystem("martial_maneuvers");
            Assert.IsNotNull(martialDef);
            var all = martialDef.Elements.Values.ToList();
            Assert.IsTrue(all.Count >= 190);

            var strike = martialDef.Elements.TryGetValue("Steely Strike", out var s) ? s : null;
            Assert.IsNotNull(strike);
            Assert.AreEqual("Iron Heart", strike.GetProperty<string>("discipline"));
            Assert.AreEqual("Strike", strike.Type);

            var stance = martialDef.Elements.TryGetValue("Punishing Stance", out var st) ? st : null;
            Assert.IsNotNull(stance);
            Assert.AreEqual("Stance", stance.Type);
            Assert.IsTrue(stance.Effects.Count > 0, "Punishing Stance should have effects defined.");

            // Case-insensitive lookup
            var caseInsensitive = martialDef.Elements.TryGetValue("punishing stance", out var ci) ? ci : null;
            Assert.IsNotNull(caseInsensitive);
        }

        // Source: Tome of Battle, pp. 41–44
        [TestMethod]
        public void SubsystemElement_JsonSerializationRoundTrip_PreservesAllFields() {
            var original = new SubsystemElement {
                Name = "Custom Strike",
                Type = "Strike"
            };
            original.SetProperty("discipline", "Iron Heart");
            original.SetProperty("level", 3);
            original.SetProperty("actionType", "standard");
            original.SetProperty("prerequisites", "1 Iron Heart maneuver");
            original.SetProperty("prerequisiteCount", 1);
            original.SetProperty("description", "A strike delivering special damage.");
            original.AddEffect("benefits", new Effect("Custom Strike Effect").AddModifier("Attack", new Modifier(2, "morale")));

            var json = (JObject)original.ToJson();
            var restored = new SubsystemElement(json);

            Assert.AreEqual(original.Name, restored.Name);
            Assert.AreEqual(original.GetProperty<string>("discipline"), restored.GetProperty<string>("discipline"));
            Assert.AreEqual(original.GetProperty<int>("level"), restored.GetProperty<int>("level"));
            Assert.AreEqual(original.Type, restored.Type);
            Assert.AreEqual(original.GetProperty<string>("actionType"), restored.GetProperty<string>("actionType"));
            Assert.AreEqual(original.GetProperty<string>("prerequisites"), restored.GetProperty<string>("prerequisites"));
            Assert.AreEqual(original.GetProperty<int>("prerequisiteCount"), restored.GetProperty<int>("prerequisiteCount"));
            Assert.AreEqual(original.GetProperty<string>("description"), restored.GetProperty<string>("description"));
            Assert.AreEqual(1, restored.GetEffects("benefits").Count);
            Assert.AreEqual("Custom Strike Effect", restored.GetEffects("benefits")[0].Name);
        }

        // Source: Tome of Battle, pp. 23, 38; ADR-0010
        [TestMethod]
        public void ActiveStances_EvaluatesCorrectly_AcrossNonAdept_Multiclass_AndFeat() {
            // 1. Non-adept (Fighter) has 0 active stances
            var fighterChar = new Character();
            for (int i = 0; i < 5; i++) fighterChar.classLevels.Add("Fighter");
            var nonAdeptSession = new CharacterSession(fighterChar, catalog);
            Assert.AreEqual(0, nonAdeptSession.GetStatValue("active stances", 0), "Non-adept should evaluate to 0 active stances");

            // 2. Multiclass Warblade 1 / Swordsage 1 evaluates to 1 active stance (INTERNAL non-stacking bonuses overlap)
            var multiChar = new Character();
            multiChar.classLevels.Add("Warblade");
            multiChar.classLevels.Add("Swordsage");
            var multiSession = new CharacterSession(multiChar, catalog);
            Assert.AreEqual(1, multiSession.GetStatValue("active stances", 0), "Multiclass adepts should overlap to 1 active stance");

            // 3. Warblade 20 evaluates to 2 active stances (Stance Mastery untyped bonus stacks)
            var wb20Char = new Character();
            for (int i = 0; i < 20; i++) wb20Char.classLevels.Add("Warblade");
            var wb20Session = new CharacterSession(wb20Char, catalog);
            Assert.AreEqual(2, wb20Session.GetStatValue("active stances", 0), "Warblade 20 should have 2 active stances");

            // 4. Martial Stance feat grants 1 active stance for non-adepts ("Feats"), not for adepts ("Warblade")
            var martialStance = catalog.GetFeat("Martial Stance");
            Assert.IsNotNull(martialStance, "Martial Stance feat must be in catalog");
            var featPool = new EffectPool(new Character());
            featPool.GenerateFeatEffects(martialStance, "Feats");
            Assert.AreEqual(1, featPool.GetStatValue("active stances", null), "Martial Stance feat should grant 1 active stance for Feats");

            var adeptPool = new EffectPool(new Character());
            adeptPool.GenerateFeatEffects(martialStance, "Warblade");
            Assert.AreEqual(0, adeptPool.GetStatValue("active stances", null, 0), "Martial Stance feat should not grant active stance for adept classes");
        }
    }
}
