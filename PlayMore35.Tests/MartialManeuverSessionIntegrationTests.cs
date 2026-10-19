using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Items;
using PlayMore35.Rules;
using PlayMore35.Subsystems;
using PlayMore35.Subsystems.Maneuvers;
using static PlayMore35.Effects.Constants;

namespace PlayMore35.Tests {
    [TestClass]
    public class MartialManeuverSessionIntegrationTests {
        private IRulesCatalog catalog;

        [TestInitialize]
        public void SetUp() {
            catalog = new GameRulesCatalog();
            SubsystemRegistry.RegisterDefaultActionParsers();
        }

        [TestCleanup]
        public void TearDown() {
            SheetAction.ResetParsersForTesting();
        }


        [TestMethod]
        public void CharacterSession_HasNoBespokeManeuverMembers() {
            var maneuverMethods = typeof(CharacterSession).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.Name.IndexOf("Maneuver", StringComparison.OrdinalIgnoreCase) >= 0 || m.Name.IndexOf("Stance", StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(m => m.Name)
                .ToList();
            Assert.AreEqual(0, maneuverMethods.Count, $"Expected 0 bespoke maneuver methods on CharacterSession, but found: {string.Join(", ", maneuverMethods)}");

            var maneuverFields = typeof(CharacterSession).GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(f => f.Name.IndexOf("Maneuver", StringComparison.OrdinalIgnoreCase) >= 0 || f.Name.IndexOf("Stance", StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(f => f.Name)
                .ToList();
            Assert.AreEqual(0, maneuverFields.Count, $"Expected 0 bespoke maneuver fields on CharacterSession, but found: {string.Join(", ", maneuverFields)}");

            var maneuverProperties = typeof(CharacterSession).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(p => p.Name.IndexOf("Maneuver", StringComparison.OrdinalIgnoreCase) >= 0 || p.Name.IndexOf("Stance", StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(p => p.Name)
                .ToList();
            Assert.AreEqual(0, maneuverProperties.Count, $"Expected 0 bespoke maneuver properties on CharacterSession, but found: {string.Join(", ", maneuverProperties)}");
        }

        // Source: Tome of Battle, pp. 22, 39
        [TestMethod]
        public void SingleClass_Warblade_InitiatorLevelAndCapacities_CalculatedThroughSession() {
            var character = new Character();
            character.classLevels.Clear();
            for (int i = 0; i < 5; i++) character.classLevels.Add("Warblade");

            var session = new CharacterSession(character, catalog);

            Assert.AreEqual(5, session.GetStatistic(MartialManeuverEngine.InitiatorLevelStat("warblade")).Value);
            Assert.AreEqual(6, session.GetStatistic("maneuvers known:warblade").Value);
            Assert.AreEqual(4, session.GetStatistic("maneuvers readied:warblade").Value);
            Assert.AreEqual(2, session.GetStatistic("stances known:warblade").Value);
            Assert.AreEqual(3, session.GetStatistic("max maneuver level:warblade").Value, string.Join("; ", session.GetStatistic("max maneuver level:warblade").Explanation));

            var engine = session.Subsystems.Get<MartialManeuverEngine>();
            Assert.IsNotNull(engine);
            Assert.IsTrue(engine.IsActive);

            Assert.AreEqual(6, engine.GetKnownSlotGroup("Warblade").Capacity);
            Assert.AreEqual(2, engine.GetStanceSlotGroup("Warblade").Capacity);
            Assert.AreEqual(3, engine.GetKnownSlotGroup("Warblade").MaxLevel);
            Assert.AreEqual(3, engine.GetStanceSlotGroup("Warblade").MaxLevel);
        }

        // Source: Tome of Battle, p. 39
        [TestMethod]
        public void Multiclass_InitiatorLevel_CalculatedSeparatelyForWarbladeAndCrusader() {
            var character = new Character();
            character.classLevels.Clear();
            for (int i = 0; i < 4; i++) character.classLevels.Add("Warblade");
            for (int i = 0; i < 2; i++) character.classLevels.Add("Crusader");

            var session = new CharacterSession(character, catalog);

            Assert.AreEqual(6, session.GetStatistic("character level").Value);

            // Warblade IL: 4 + floor(0.5 * 2) = 5 -> Max maneuver level: ceil(5 / 2) = 3
            Assert.AreEqual(5, session.GetStatistic(MartialManeuverEngine.InitiatorLevelStat("warblade")).Value);
            Assert.AreEqual(3, session.GetStatistic("max maneuver level:warblade").Value);

            // Crusader IL: 2 + floor(0.5 * 4) = 4 -> Max maneuver level: ceil(4 / 2) = 2
            Assert.AreEqual(4, session.GetStatistic(MartialManeuverEngine.InitiatorLevelStat("crusader")).Value);
            Assert.AreEqual(2, session.GetStatistic("max maneuver level:crusader").Value);

            var engine = session.Subsystems.Get<MartialManeuverEngine>();
            Assert.IsNotNull(engine);
            Assert.IsTrue(engine.IsActive);

            Assert.AreEqual(3, engine.GetKnownSlotGroup("Warblade").MaxLevel);
            Assert.AreEqual(2, engine.GetKnownSlotGroup("Crusader").MaxLevel);
        }

        // Source: Tome of Battle, pp. 39, 52
        [TestMethod]
        public void NonAdept_MartialStudyFeat_CalculatesFeatInitiatorLevelInSession() {
            var character = new Character();
            character.classLevels.Clear();
            for (int i = 0; i < 4; i++) character.classLevels.Add("Fighter");
            character.feats["Level 1"] = "Martial Study";

            var session = new CharacterSession(character, catalog);

            Assert.AreEqual(4, session.GetStatistic("character level").Value);
            // Feat IL: floor(4 / 2) = 2
            Assert.AreEqual(2, session.GetStatistic(MartialManeuverEngine.InitiatorLevelStat("feats")).Value);
            Assert.AreEqual(1, session.GetStatistic("max maneuver level:feats").Value);
            Assert.AreEqual(1, session.GetStatistic("maneuvers known:feats").Value);
            Assert.AreEqual(1, session.GetStatistic("maneuvers readied:feats").Value);

            var engine = session.Subsystems.Get<MartialManeuverEngine>();
            Assert.IsNotNull(engine);
            Assert.IsTrue(engine.IsActive);

            Assert.AreEqual(1, engine.GetKnownSlotGroup("Feats").Capacity);
            Assert.AreEqual(1, engine.GetKnownSlotGroup("Feats").MaxLevel);
        }

        // Source: Tome of Battle, pp. 5, 38, 43, 69
        [TestMethod]
        public void ActiveStance_ModifiesArmorClass_AndRecalculatesDynamically() {
            var character = new Character();
            character.classLevels.Clear();
            character.classLevels.Add("Warblade");
            character.assignedAbilityScores["DEX"] = 10; // 0 DEX mod

            var session = new CharacterSession(character, catalog);
            var engine = session.Subsystems.Get<MartialManeuverEngine>();
            Assert.IsNotNull(engine);

            // Baseline AC: 10 (no armor, 0 DEX mod)
            Assert.AreEqual(10, session.GetStatistic("AC").Value);

            engine.SetStanceSlot("Warblade", 0, new StanceSlot("Punishing Stance", isActive: false));
            session.Recalculate();

            // Inactive stance: AC remains 10
            Assert.AreEqual(10, session.GetStatistic("AC").Value);

            // Adopt Punishing Stance (-2 penalty to AC)
            engine.AdoptStance("Warblade", 0);
            session.Recalculate();
            Assert.IsTrue(engine.GetStanceSlot("Warblade", 0).IsActive);
            Assert.AreEqual(8, session.GetStatistic("AC").Value);

            // Exit stance: AC returns to baseline 10
            engine.ExitStance("Warblade", 0);
            session.Recalculate();
            Assert.IsFalse(engine.GetStanceSlot("Warblade", 0).IsActive);
            Assert.AreEqual(10, session.GetStatistic("AC").Value);
        }

        // Source: Tome of Battle, pp. 5, 38, 43, 69, 91
        [TestMethod]
        public void ActiveStance_AffectingEquippedWeaponAttacks_RecalculatesRoutines() {
            var attackStanceEffect = new Effect("Stance of the Aggressor")
                .AddAttackModifier(AttackMatcher.ALL, new Modifier(2, "morale"));
            var testStance = new Maneuver {
                Name = "Stance of the Aggressor",
                Discipline = "Iron Heart",
                Level = 1,
                Type = ManeuverType.Stance,
                Effects = new List<Effect> { attackStanceEffect }
            };
            var customCatalog = new FakeRulesCatalog(new[] { testStance }, inner: catalog);
            var customEngine = new MartialManeuverEngine(customCatalog);

            var character = new Character();
            character.classLevels.Clear();
            for (int i = 0; i < 3; i++) character.classLevels.Add("Warblade"); // BAB +3
            character.assignedAbilityScores["STR"] = 16; // STR +3
            character.equipped.PrimaryHandWeapon = new WeaponChoice { Name = "greatsword" };

            var session = new CharacterSession(character, customCatalog);
            session.Subsystems.Register(customEngine, "maneuver");
            customEngine.Initialize(session);
            session.Recalculate();

            // Greatsword attack without stance: BAB +3 + STR +3 = +6
            var routines = session.GetAllRoutines();
            var standardRoutine = routines.FirstOrDefault(r => r.ActionType == RoutineActionType.Standard && r.Category == RoutineCategory.PrimarySolo);
            Assert.IsNotNull(standardRoutine);
            Assert.AreEqual(6, standardRoutine.Attacks[0].AttackBonus.bonus);

            // Set up stance slot and adopt Stance of the Aggressor
            customEngine.SetStanceSlot("Warblade", 0, new StanceSlot("Stance of the Aggressor"));
            customEngine.AdoptStance("Warblade", 0);
            session.Recalculate();

            // Greatsword attack with active stance: +6 + 2 morale = +8
            routines = session.GetAllRoutines();
            standardRoutine = routines.FirstOrDefault(r => r.ActionType == RoutineActionType.Standard && r.Category == RoutineCategory.PrimarySolo);
            Assert.IsNotNull(standardRoutine);
            Assert.AreEqual(8, standardRoutine.Attacks[0].AttackBonus.bonus);
            Assert.IsTrue(standardRoutine.Attacks[0].AttackBonus.explanation.Any(e => e.Contains("morale") || e.Contains("Aggressor")));

            // Exit stance
            customEngine.ExitAllStances();
            session.Recalculate();

            // Returns to baseline +6
            routines = session.GetAllRoutines();
            standardRoutine = routines.FirstOrDefault(r => r.ActionType == RoutineActionType.Standard && r.Category == RoutineCategory.PrimarySolo);
            Assert.IsNotNull(standardRoutine);
            Assert.AreEqual(6, standardRoutine.Attacks[0].AttackBonus.bonus);
        }

        // Source: Tome of Battle, pp. 38, 40, 42
        [TestMethod]
        public void InCombat_Initiation_TimelineBoostExpiration_AndRecoveryLoop() {
            var character = new Character { race = "Human" };
            character.classLevels.Clear();
            for (int i = 0; i < 3; i++) character.classLevels.Add("Warblade");
            character.rolledHitDice = new List<int> { 12, 10, 10 };

            var session = new CharacterSession(character, catalog);
            var engine = session.Subsystems.Get<MartialManeuverEngine>();
            Assert.IsNotNull(engine);

            // Initial land speed
            Assert.AreEqual(30, session.GetStatistic("Land speed").Value);

            // Slot 0: Wind Stride (+10 enhancement bonus to Land speed for 1 round)
            engine.SetKnownSlot("Warblade", 0, new KnownManeuverSlot("Wind Stride", ManeuverState.Unreadied));
            // Slot 1: Steely Strike
            engine.SetKnownSlot("Warblade", 1, new KnownManeuverSlot("Steely Strike", ManeuverState.Unreadied));

            // Ready maneuvers
            engine.ReadyManeuver("Warblade", 0);
            engine.ReadyManeuver("Warblade", 1);
            Assert.AreEqual(ManeuverState.Available, engine.GetKnownSlot("Warblade", 0).State);
            Assert.AreEqual(ManeuverState.Available, engine.GetKnownSlot("Warblade", 1).State);

            session.StartCombat();

            // Initiate Wind Stride (Boost)
            bool boostInitiated = engine.InitiateManeuver("Warblade", 0);
            Assert.IsTrue(boostInitiated);
            Assert.AreEqual(ManeuverState.Expended, engine.GetKnownSlot("Warblade", 0).State);

            // Dynamic recalculation applies boost modifier to Land speed
            session.Recalculate();
            Assert.AreEqual(40, session.GetStatistic("Land speed").Value);

            // Advance timeline by 1 round (boost expires)
            var advanceResult = session.AdvanceTimeline(1);
            Assert.AreEqual(TimelineRefreshRequirement.RecomputeDerived, advanceResult.RefreshRequirement);
            Assert.AreEqual(1, advanceResult.ExpiredActions.Count);
            Assert.IsInstanceOfType(advanceResult.ExpiredActions[0], typeof(ManeuverSheetAction));

            // Land speed returns to baseline 30, but slot remains Expended
            Assert.AreEqual(30, session.GetStatistic("Land speed").Value);
            Assert.AreEqual(ManeuverState.Expended, engine.GetKnownSlot("Warblade", 0).State);

            // Initiate Steely Strike (Strike)
            bool strikeInitiated = engine.InitiateManeuver("Warblade", 1);
            Assert.IsTrue(strikeInitiated);
            Assert.AreEqual(ManeuverState.Expended, engine.GetKnownSlot("Warblade", 1).State);

            // Warblade Swift Recovery restores all expended maneuvers
            engine.RecoverWarblade("Warblade");
            Assert.AreEqual(ManeuverState.Available, engine.GetKnownSlot("Warblade", 0).State);
            Assert.AreEqual(ManeuverState.Available, engine.GetKnownSlot("Warblade", 1).State);
        }

        // Source: Tome of Battle, p. 38
        [TestMethod]
        public void CharacterSession_FullJsonSaveLoadRoundTrip_PreservesAllManeuverState() {
            var character = new Character { name = "Martial Adept Hero" };
            character.classLevels.Clear();
            for (int i = 0; i < 3; i++) character.classLevels.Add("Warblade");
            character.assignedAbilityScores["DEX"] = 10;

            var session = new CharacterSession(character, catalog);
            var engine = session.Subsystems.Get<MartialManeuverEngine>();

            engine.SetKnownSlot("Warblade", 0, new KnownManeuverSlot("Wind Stride", ManeuverState.Available));
            engine.SetKnownSlot("Warblade", 1, new KnownManeuverSlot("Steely Strike", ManeuverState.Expended));
            engine.SetKnownSlot("Warblade", 2, new KnownManeuverSlot("Iron Heart Surge", ManeuverState.Unreadied));
            engine.SetStanceSlot("Warblade", 0, new StanceSlot("Punishing Stance", isActive: true));

            session.Recalculate();
            Assert.AreEqual(8, session.GetStatistic("AC").Value); // 10 - 2 from active Punishing Stance

            // Full character serialization round-trip
            JToken json = character.ToJson();
            Assert.IsNotNull(json["subsystems"]);
            Assert.IsNotNull(json["subsystems"][MartialManeuverEngine.SubsystemId]);

            var restoredChar = new Character((JObject)json);
            Assert.IsTrue(restoredChar.Subsystems.ContainsKey(MartialManeuverEngine.SubsystemId));
            Assert.IsInstanceOfType(restoredChar.Subsystems[MartialManeuverEngine.SubsystemId], typeof(MartialManeuverState));

            // Load into a new CharacterSession
            var restoredSession = new CharacterSession(restoredChar, catalog);
            var restoredEngine = restoredSession.Subsystems.Get<MartialManeuverEngine>();
            Assert.IsNotNull(restoredEngine);
            Assert.IsTrue(restoredEngine.IsActive);

            // Verify active stance effect is immediately active
            Assert.AreEqual(8, restoredSession.GetStatistic("AC").Value);

            Assert.AreEqual(ManeuverState.Available, restoredEngine.GetKnownSlot("Warblade", 0).State);
            Assert.AreEqual("Wind Stride", restoredEngine.GetKnownSlot("Warblade", 0).ManeuverName);

            Assert.AreEqual(ManeuverState.Expended, restoredEngine.GetKnownSlot("Warblade", 1).State);
            Assert.AreEqual("Steely Strike", restoredEngine.GetKnownSlot("Warblade", 1).ManeuverName);

            Assert.AreEqual(ManeuverState.Unreadied, restoredEngine.GetKnownSlot("Warblade", 2).State);
            Assert.AreEqual("Iron Heart Surge", restoredEngine.GetKnownSlot("Warblade", 2).ManeuverName);

            Assert.AreEqual("Punishing Stance", restoredEngine.GetStanceSlot("Warblade", 0).StanceName);
            Assert.IsTrue(restoredEngine.GetStanceSlot("Warblade", 0).IsActive);

            // Recovery on restored engine works cleanly
            restoredEngine.RecoverWarblade("Warblade");
            Assert.AreEqual(ManeuverState.Available, restoredEngine.GetKnownSlot("Warblade", 1).State);
        }

        // Source: Tome of Battle, pp. 38, 40
        [TestMethod]
        public void Crusader_EncounterDeck_DealAdvanceAndResetInSession() {
            var character = new Character();
            character.classLevels.Clear();
            character.classLevels.Add("Crusader"); // Level 1 Crusader: 5 known, 5 readied, 2 granted

            var session = new CharacterSession(character, catalog);
            var engine = session.Subsystems.Get<MartialManeuverEngine>();
            Assert.IsNotNull(engine);

            Assert.AreEqual(5, session.GetStatistic("maneuvers known:crusader").Value);
            Assert.AreEqual(5, session.GetStatistic("maneuvers readied:crusader").Value);
            Assert.AreEqual(2, session.GetStatistic("maneuvers granted:crusader").Value);

            engine.SetKnownSlot("Crusader", 0, new KnownManeuverSlot("Crusader's Strike", ManeuverState.Withheld));
            engine.SetKnownSlot("Crusader", 1, new KnownManeuverSlot("Leading the Attack", ManeuverState.Withheld));
            engine.SetKnownSlot("Crusader", 2, new KnownManeuverSlot("Douse the Flames", ManeuverState.Withheld));
            engine.SetKnownSlot("Crusader", 3, new KnownManeuverSlot("Martial Spirit", ManeuverState.Withheld));
            engine.SetKnownSlot("Crusader", 4, new KnownManeuverSlot("Shield Block", ManeuverState.Withheld));

            // Fixed seed RNG for deterministic dealing
            var rng = new Random(42);
            engine.DealCrusader("Crusader", rng);

            // Exactly 2 granted (Available), 3 Withheld
            int availableCount = engine.GetKnownSlots("Crusader").Count(s => s != null && s.State == ManeuverState.Available);
            int withheldCount = engine.GetKnownSlots("Crusader").Count(s => s != null && s.State == ManeuverState.Withheld);
            Assert.AreEqual(2, availableCount);
            Assert.AreEqual(3, withheldCount);

            // Initiate one available maneuver
            int availableIndex = engine.GetKnownSlots("Crusader").ToList().FindIndex(s => s != null && s.State == ManeuverState.Available);
            bool initiated = engine.InitiateManeuver("Crusader", availableIndex);
            Assert.IsTrue(initiated);
            Assert.AreEqual(ManeuverState.Expended, engine.GetKnownSlot("Crusader", availableIndex).State);

            // Advance Crusader turn: 1 Withheld becomes Available
            engine.AdvanceCrusaderTurn("Crusader", rng);
            int newAvailableCount = engine.GetKnownSlots("Crusader").Count(s => s != null && s.State == ManeuverState.Available);
            Assert.AreEqual(2, newAvailableCount); // 1 unexpended + 1 newly granted
            int newWithheldCount = engine.GetKnownSlots("Crusader").Count(s => s != null && s.State == ManeuverState.Withheld);
            Assert.AreEqual(2, newWithheldCount);

            // Reset encounter: reshuffles and redeals fresh hand of 2 available
            engine.ResetEncounter(rng);
            Assert.AreEqual(2, engine.GetKnownSlots("Crusader").Count(s => s != null && s.State == ManeuverState.Available));
            Assert.AreEqual(3, engine.GetKnownSlots("Crusader").Count(s => s != null && s.State == ManeuverState.Withheld));
            Assert.AreEqual(0, engine.GetKnownSlots("Crusader").Count(s => s != null && s.State == ManeuverState.Expended));
        }

        // Source: Tome of Battle, pp. 32, 52
        [TestMethod]
        public void MartialStudy_Adept_ProjectsIntoTargetKnownSlots_AsFeatBoundSlot() {
            var character = new Character { race = "Human" };
            character.classLevels.Clear();
            character.classLevels.Add("Warblade");
            character.feats["Level 1"] = "Martial Study";
            character.featChoices["Level 1:maneuver"] = "Steel Wind";

            var session = new CharacterSession(character, catalog);
            var engine = session.Subsystems.Get<MartialManeuverEngine>();
            Assert.IsNotNull(engine);

            var warbladeSlots = engine.GetKnownSlots("Warblade");
            var featSlot = warbladeSlots.FirstOrDefault(s => s != null && s.IsFeatBound);
            Assert.IsNotNull(featSlot, "Feat-bound slot should exist in Warblade known slots.");
            Assert.AreEqual("Steel Wind", featSlot.ManeuverName);
            Assert.IsTrue(featSlot.IsFeatBound);
            Assert.AreEqual("Level 1", featSlot.FeatSlot);
            Assert.IsTrue(engine.HasManeuver("Steel Wind", "Warblade"));
        }

        // Source: Tome of Battle, pp. 32, 52
        [TestMethod]
        public void MartialStudy_NonAdept_ProjectsIntoFeatsKnownSlots_AsFeatBoundSlot() {
            var character = new Character { race = "Human" };
            character.classLevels.Clear();
            character.classLevels.Add("Fighter");
            character.feats["Level 1"] = "Martial Study";
            character.featChoices["Level 1:maneuver"] = "Moment of Perfect Mind";

            var session = new CharacterSession(character, catalog);
            var engine = session.Subsystems.Get<MartialManeuverEngine>();
            Assert.IsNotNull(engine);

            var featSlots = engine.GetKnownSlots("Feats");
            var featSlot = featSlots.FirstOrDefault(s => s != null && s.IsFeatBound);
            Assert.IsNotNull(featSlot, "Feat-bound slot should exist in Feats known slots.");
            Assert.AreEqual("Moment of Perfect Mind", featSlot.ManeuverName);
            Assert.IsTrue(featSlot.IsFeatBound);
            Assert.AreEqual("Level 1", featSlot.FeatSlot);
            Assert.IsTrue(engine.HasManeuver("Moment of Perfect Mind", "Feats"));
        }

        // Source: Tome of Battle, p. 32
        [TestMethod]
        public void FeatBoundSlot_CannotBeOverwrittenOrCleared_ThrowsInvalidOperationException() {
            var character = new Character { race = "Human" };
            character.classLevels.Clear();
            character.classLevels.Add("Warblade");
            character.feats["Level 1"] = "Martial Study";
            character.featChoices["Level 1:maneuver"] = "Steel Wind";

            var session = new CharacterSession(character, catalog);
            var engine = session.Subsystems.Get<MartialManeuverEngine>();
            Assert.IsNotNull(engine);

            var warbladeSlots = engine.GetKnownSlots("Warblade");
            int featSlotIndex = -1;
            for (int i = 0; i < warbladeSlots.Count; i++) {
                if (warbladeSlots[i] != null && warbladeSlots[i].IsFeatBound) {
                    featSlotIndex = i;
                    break;
                }
            }
            Assert.IsTrue(featSlotIndex >= 0, "Feat-bound slot must be found.");

            // Attempting to overwrite via string overload throws
            Assert.ThrowsException<InvalidOperationException>(() => {
                engine.SetKnownSlot("Warblade", featSlotIndex, "Iron Heart Surge");
            });

            // Attempting to overwrite via KnownManeuverSlot overload throws
            Assert.ThrowsException<InvalidOperationException>(() => {
                engine.SetKnownSlot("Warblade", featSlotIndex, new KnownManeuverSlot("Iron Heart Surge"));
            });

            // Attempting to clear throws
            Assert.ThrowsException<InvalidOperationException>(() => {
                engine.ClearKnownSlot("Warblade", featSlotIndex);
            });
        }

        // Source: Tome of Battle, pp. 32, 38, 40
        [TestMethod]
        public void FeatBoundSlot_ReadiedState_PreservedAcrossRecalculations() {
            var character = new Character { race = "Human" };
            character.classLevels.Clear();
            character.classLevels.Add("Warblade");
            character.feats["Level 1"] = "Martial Study";
            character.featChoices["Level 1:maneuver"] = "Steel Wind";

            var session = new CharacterSession(character, catalog);
            var engine = session.Subsystems.Get<MartialManeuverEngine>();
            Assert.IsNotNull(engine);

            var warbladeSlots = engine.GetKnownSlots("Warblade");
            int featSlotIndex = warbladeSlots.ToList().FindIndex(s => s != null && s.IsFeatBound);
            Assert.IsTrue(featSlotIndex >= 0);

            // Ready the feat-bound maneuver
            engine.ReadyManeuver("Warblade", featSlotIndex);
            Assert.AreEqual(ManeuverState.Available, engine.GetKnownSlot("Warblade", featSlotIndex).State);

            // Recalculate session preserves readied state
            session.Recalculate();
            Assert.AreEqual(ManeuverState.Available, engine.GetKnownSlot("Warblade", featSlotIndex).State);

            // Initiate maneuver transitions state to Expended
            session.StartCombat();
            bool initiated = engine.InitiateManeuver("Warblade", featSlotIndex);
            Assert.IsTrue(initiated);
            Assert.AreEqual(ManeuverState.Expended, engine.GetKnownSlot("Warblade", featSlotIndex).State);

            // Warblade recovery restores it
            engine.RecoverWarblade("Warblade");
            Assert.AreEqual(ManeuverState.Available, engine.GetKnownSlot("Warblade", featSlotIndex).State);
        }

        // Source: Tome of Battle, pp. 22, 32
        [TestMethod]
        public void MartialAdept_LevelUp_PreservesFeatBoundSlots_AndPreventsRetraining() {
            var character = new Character { race = "Human" };
            character.classLevels.Clear();
            character.classLevels.Add("Warblade"); // Level 1: 3 class + 1 feat = 4
            character.feats["Level 1"] = "Martial Study";
            character.featChoices["Level 1:maneuver"] = "Steel Wind";

            var session = new CharacterSession(character, catalog);
            var engine = session.Subsystems.Get<MartialManeuverEngine>();
            Assert.IsNotNull(engine);

            // Populate class slots 0, 1, 2
            engine.SetKnownSlot("Warblade", 0, "Moment of Perfect Mind");
            engine.SetKnownSlot("Warblade", 1, "Steely Strike");
            engine.SetKnownSlot("Warblade", 2, "Wind Stride");

            var featSlot = engine.GetKnownSlots("Warblade").FirstOrDefault(s => s != null && s.IsFeatBound);
            Assert.IsNotNull(featSlot);
            Assert.AreEqual("Steel Wind", featSlot.ManeuverName);

            // Level up to Warblade 4 (Table 1-3 allows retraining 1 maneuver at 4th level)
            character.classLevels.Add("Warblade"); // Level 2: 4 class + 1 feat = 5
            character.classLevels.Add("Warblade"); // Level 3: 5 class + 1 feat = 6
            character.classLevels.Add("Warblade"); // Level 4: 5 class + 1 feat = 6
            session.Recalculate();

            Assert.AreEqual(6, session.GetStatistic("maneuvers known:warblade").Value);

            // Normal class slots can be retrained (e.g. slot 0 to Iron Heart Surge)
            engine.SetKnownSlot("Warblade", 0, "Iron Heart Surge");
            Assert.AreEqual("Iron Heart Surge", engine.GetKnownSlot("Warblade", 0).ManeuverName);

            // Feat-bound slot remains in known slots and cannot be retrained or cleared
            int featIndex = engine.GetKnownSlots("Warblade").ToList().FindIndex(s => s != null && s.IsFeatBound);
            Assert.IsTrue(featIndex >= 0);
            Assert.AreEqual("Steel Wind", engine.GetKnownSlot("Warblade", featIndex).ManeuverName);

            Assert.ThrowsException<InvalidOperationException>(() => {
                engine.SetKnownSlot("Warblade", featIndex, "Mountain Hammer");
            });

            Assert.ThrowsException<InvalidOperationException>(() => {
                engine.ClearKnownSlot("Warblade", featIndex);
            });
        }

        // Source: Tome of Battle, p. 32
        [TestMethod]
        public void Multiclass_TwoAdeptClasses_ProjectsIntoChosenClass() {
            var character = new Character { race = "Human" };
            character.classLevels.Clear();
            character.classLevels.Add("Warblade");
            character.classLevels.Add("Crusader");
            character.classLevels.Add("Crusader"); // Level 3: Warblade and Crusader both present
            character.feats["Level 3"] = "Martial Study";
            character.featChoices["Level 3:class"] = "Crusader";
            character.featChoices["Level 3:maneuver"] = "Mountain Hammer";

            var session = new CharacterSession(character, catalog);
            var engine = session.Subsystems.Get<MartialManeuverEngine>();
            Assert.IsNotNull(engine);

            // Mountain Hammer must project into Crusader known slots, NOT Warblade
            Assert.IsTrue(engine.HasManeuver("Mountain Hammer", "Crusader"));
            Assert.IsFalse(engine.HasManeuver("Mountain Hammer", "Warblade"));

            var crusaderFeatSlot = engine.GetKnownSlots("Crusader").FirstOrDefault(s => s != null && s.IsFeatBound);
            Assert.IsNotNull(crusaderFeatSlot);
            Assert.AreEqual("Mountain Hammer", crusaderFeatSlot.ManeuverName);
            Assert.AreEqual("Level 3", crusaderFeatSlot.FeatSlot);
        }

        // Source: Tome of Battle, p. 32
        [TestMethod]
        public void SerializationRoundTrip_PreservesFeatBoundSlotProperties() {
            var character = new Character { name = "Martial Study Hero", race = "Human" };
            character.classLevels.Clear();
            character.classLevels.Add("Warblade");
            character.feats["Level 1"] = "Martial Study";
            character.featChoices["Level 1:maneuver"] = "Steel Wind";

            var session = new CharacterSession(character, catalog);
            var engine = session.Subsystems.Get<MartialManeuverEngine>();

            int featIndex = engine.GetKnownSlots("Warblade").ToList().FindIndex(s => s != null && s.IsFeatBound);
            Assert.IsTrue(featIndex >= 0);
            engine.ReadyManeuver("Warblade", featIndex);

            // Full character serialization round-trip
            JToken json = character.ToJson();
            var restoredChar = new Character((JObject)json);
            var restoredSession = new CharacterSession(restoredChar, catalog);
            var restoredEngine = restoredSession.Subsystems.Get<MartialManeuverEngine>();

            var restoredSlot = restoredEngine.GetKnownSlots("Warblade").FirstOrDefault(s => s != null && s.IsFeatBound);
            Assert.IsNotNull(restoredSlot);
            Assert.AreEqual("Steel Wind", restoredSlot.ManeuverName);
            Assert.IsTrue(restoredSlot.IsFeatBound);
            Assert.AreEqual("Level 1", restoredSlot.FeatSlot);
            Assert.AreEqual(ManeuverState.Available, restoredSlot.State);
        }

        // Source: Tome of Battle, p. 32
        [TestMethod]
        public void FeatChoice_UpdatedOrCleared_UpdatesProjectedSlot() {
            var character = new Character { race = "Human" };
            character.classLevels.Clear();
            character.classLevels.Add("Warblade");
            character.feats["Level 1"] = "Martial Study";
            character.featChoices["Level 1:maneuver"] = "Steel Wind";

            var session = new CharacterSession(character, catalog);
            var engine = session.Subsystems.Get<MartialManeuverEngine>();

            Assert.IsTrue(engine.HasManeuver("Steel Wind", "Warblade"));

            // Changing the feat choice updates the projected slot name
            session.SetFeatChoice("Level 1", "maneuver", "Stone Bones");
            Assert.IsTrue(engine.HasManeuver("Stone Bones", "Warblade"));
            Assert.IsFalse(engine.HasManeuver("Steel Wind", "Warblade"));

            // Clearing the feat choice removes the projected slot
            session.SetFeatChoice("Level 1", "maneuver", "");
            Assert.IsFalse(engine.HasManeuver("Stone Bones", "Warblade"));
            Assert.IsFalse(engine.GetKnownSlots("Warblade").Any(s => s != null && s.IsFeatBound));
        }

        // Source: Tome of Battle, pp. 32, 52
        [TestMethod]
        public void MartialStance_Adept_ProjectsIntoAdeptStanceSlots_AsFeatBoundSlot() {
            var character = new Character { race = "Human" };
            character.classLevels.Clear();
            character.classLevels.Add("Warblade");
            character.feats["Level 1"] = "Martial Stance";
            character.featChoices["Level 1:stance"] = "Punishing Stance";

            var session = new CharacterSession(character, catalog);
            var engine = session.Subsystems.Get<MartialManeuverEngine>();
            Assert.IsNotNull(engine);

            // Warblade has 1 base stance known, feat grants +1 -> total 2
            Assert.AreEqual(2, session.GetStatistic("stances known:warblade").Value);

            var stanceSlots = engine.GetStanceSlots("Warblade");
            var featSlot = stanceSlots.FirstOrDefault(s => s != null && s.IsFeatBound);
            Assert.IsNotNull(featSlot, "Feat-bound stance slot should exist in Warblade stance slots.");
            Assert.AreEqual("Punishing Stance", featSlot.StanceName);
            Assert.IsTrue(featSlot.IsFeatBound);
            Assert.AreEqual("Level 1", featSlot.FeatSlot);
            Assert.IsTrue(engine.HasStance("Punishing Stance", "Warblade"));
        }

        // Source: Tome of Battle, pp. 32, 52
        [TestMethod]
        public void MartialStance_NonAdept_ProjectsIntoFeatsStanceSlots_AsFeatBoundSlot() {
            var character = new Character { race = "Human" };
            character.classLevels.Clear();
            character.classLevels.Add("Fighter");
            character.feats["Level 1"] = "Martial Stance";
            character.featChoices["Level 1:stance"] = "Bolstering Voice";

            var session = new CharacterSession(character, catalog);
            var engine = session.Subsystems.Get<MartialManeuverEngine>();
            Assert.IsNotNull(engine);

            // Non-adept: stances known:feats = 1, active stances = 1
            Assert.AreEqual(1, session.GetStatistic("stances known:feats").Value);
            Assert.AreEqual(1, session.GetStatistic("active stances").Value);

            var featSlots = engine.GetStanceSlots("Feats");
            var featSlot = featSlots.FirstOrDefault(s => s != null && s.IsFeatBound);
            Assert.IsNotNull(featSlot, "Feat-bound stance slot should exist in Feats stance slots.");
            Assert.AreEqual("Bolstering Voice", featSlot.StanceName);
            Assert.IsTrue(featSlot.IsFeatBound);
            Assert.AreEqual("Level 1", featSlot.FeatSlot);
            Assert.IsTrue(engine.HasStance("Bolstering Voice", "Feats"));
        }

        // Source: Tome of Battle, pp. 32, 52
        [TestMethod]
        public void MartialStance_FeatBoundSlot_CannotBeOverwrittenOrCleared_ThrowsInvalidOperationException() {
            var character = new Character { race = "Human" };
            character.classLevels.Clear();
            character.classLevels.Add("Warblade");
            character.feats["Level 1"] = "Martial Stance";
            character.featChoices["Level 1:stance"] = "Punishing Stance";

            var session = new CharacterSession(character, catalog);
            var engine = session.Subsystems.Get<MartialManeuverEngine>();
            Assert.IsNotNull(engine);

            var warbladeSlots = engine.GetStanceSlots("Warblade");
            int featSlotIndex = -1;
            for (int i = 0; i < warbladeSlots.Count; i++) {
                if (warbladeSlots[i] != null && warbladeSlots[i].IsFeatBound) {
                    featSlotIndex = i;
                    break;
                }
            }
            Assert.IsTrue(featSlotIndex >= 0, "Feat-bound stance slot must be found.");

            // Attempting to overwrite via string overload throws
            Assert.ThrowsException<InvalidOperationException>(() => {
                engine.SetStanceSlot("Warblade", featSlotIndex, "Hunter's Sense");
            });

            // Attempting to overwrite via StanceSlot overload throws
            Assert.ThrowsException<InvalidOperationException>(() => {
                engine.SetStanceSlot("Warblade", featSlotIndex, new StanceSlot("Hunter's Sense"));
            });

            // Attempting to clear throws
            Assert.ThrowsException<InvalidOperationException>(() => {
                engine.ClearStanceSlot("Warblade", featSlotIndex);
            });
        }

        // Source: Tome of Battle, pp. 5, 23, 32, 52
        [TestMethod]
        public void MartialStance_FeatBoundSlot_ActiveState_PreservedAcrossRecalculations() {
            var character = new Character { race = "Human" };
            character.classLevels.Clear();
            character.classLevels.Add("Warblade");
            character.feats["Level 1"] = "Martial Stance";
            character.featChoices["Level 1:stance"] = "Punishing Stance";

            var session = new CharacterSession(character, catalog);
            var engine = session.Subsystems.Get<MartialManeuverEngine>();
            Assert.IsNotNull(engine);

            var warbladeSlots = engine.GetStanceSlots("Warblade");
            int featSlotIndex = warbladeSlots.ToList().FindIndex(s => s != null && s.IsFeatBound);
            Assert.IsTrue(featSlotIndex >= 0);

            // Adopt the feat-bound stance
            engine.AdoptStance("Warblade", featSlotIndex);
            Assert.IsTrue(engine.GetStanceSlot("Warblade", featSlotIndex).IsActive);

            // Recalculate session preserves active stance
            session.Recalculate();
            Assert.IsTrue(engine.GetStanceSlot("Warblade", featSlotIndex).IsActive);
        }

        // Source: Tome of Battle, pp. 32, 52
        [TestMethod]
        public void MartialStance_LateMulticlassAdept_RemainsInFeatsPool() {
            // Level 1 Fighter with Martial Stance, then gains Warblade at Level 2
            var character = new Character { race = "Human" };
            character.classLevels.Clear();
            character.classLevels.Add("Fighter");
            character.feats["Level 1"] = "Martial Stance";
            character.featChoices["Level 1:stance"] = "Bolstering Voice";

            var session = new CharacterSession(character, catalog);
            var engine = session.Subsystems.Get<MartialManeuverEngine>();

            Assert.IsTrue(engine.HasStance("Bolstering Voice", "Feats"));

            // Multiclass to Warblade at level 2
            character.classLevels.Add("Warblade");
            session.Recalculate();

            // Stance must remain in Feats pool, not migrated to Warblade
            Assert.IsTrue(engine.HasStance("Bolstering Voice", "Feats"));
            Assert.IsFalse(engine.HasStance("Bolstering Voice", "Warblade"));
        }

        // Source: Tome of Battle, pp. 32, 52
        [TestMethod]
        public void MartialStance_Multiclass_TwoAdeptClasses_ProjectsIntoChosenClass() {
            var character = new Character { race = "Human" };
            character.classLevels.Clear();
            character.classLevels.Add("Warblade");
            character.classLevels.Add("Crusader");
            character.classLevels.Add("Crusader"); // Level 3: Warblade and Crusader both present
            character.feats["Level 3"] = "Martial Stance";
            character.featChoices["Level 3:class"] = "Crusader";
            character.featChoices["Level 3:stance"] = "Iron Guard's Glare";

            var session = new CharacterSession(character, catalog);
            var engine = session.Subsystems.Get<MartialManeuverEngine>();
            Assert.IsNotNull(engine);

            // Iron Guard's Glare must project into Crusader stance slots, NOT Warblade
            Assert.IsTrue(engine.HasStance("Iron Guard's Glare", "Crusader"));
            Assert.IsFalse(engine.HasStance("Iron Guard's Glare", "Warblade"));

            var crusaderFeatSlot = engine.GetStanceSlots("Crusader").FirstOrDefault(s => s != null && s.IsFeatBound);
            Assert.IsNotNull(crusaderFeatSlot);
            Assert.AreEqual("Iron Guard's Glare", crusaderFeatSlot.StanceName);
            Assert.AreEqual("Level 3", crusaderFeatSlot.FeatSlot);
        }

        // Source: Tome of Battle, pp. 32, 52
        [TestMethod]
        public void MartialStance_SerializationRoundTrip_PreservesFeatBoundSlotProperties() {
            var character = new Character { name = "Martial Stance Hero", race = "Human" };
            character.classLevels.Clear();
            character.classLevels.Add("Warblade");
            character.feats["Level 1"] = "Martial Stance";
            character.featChoices["Level 1:stance"] = "Punishing Stance";

            var session = new CharacterSession(character, catalog);
            var engine = session.Subsystems.Get<MartialManeuverEngine>();

            int featIndex = engine.GetStanceSlots("Warblade").ToList().FindIndex(s => s != null && s.IsFeatBound);
            Assert.IsTrue(featIndex >= 0);
            engine.AdoptStance("Warblade", featIndex);

            // Full character serialization round-trip
            JToken json = character.ToJson();
            var restoredChar = new Character((JObject)json);
            var restoredSession = new CharacterSession(restoredChar, catalog);
            var restoredEngine = restoredSession.Subsystems.Get<MartialManeuverEngine>();

            var restoredSlot = restoredEngine.GetStanceSlots("Warblade").FirstOrDefault(s => s != null && s.IsFeatBound);
            Assert.IsNotNull(restoredSlot);
            Assert.AreEqual("Punishing Stance", restoredSlot.StanceName);
            Assert.IsTrue(restoredSlot.IsFeatBound);
            Assert.AreEqual("Level 1", restoredSlot.FeatSlot);
            Assert.IsTrue(restoredSlot.IsActive);
        }

        // Source: Tome of Battle, pp. 32, 52
        [TestMethod]
        public void MartialStance_FeatChoice_UpdatedOrCleared_UpdatesProjectedSlot() {
            var character = new Character { race = "Human" };
            character.classLevels.Clear();
            character.classLevels.Add("Warblade");
            character.feats["Level 1"] = "Martial Stance";
            character.featChoices["Level 1:stance"] = "Punishing Stance";

            var session = new CharacterSession(character, catalog);
            var engine = session.Subsystems.Get<MartialManeuverEngine>();

            Assert.IsTrue(engine.HasStance("Punishing Stance", "Warblade"));

            // Changing the feat choice updates the projected stance name
            session.SetFeatChoice("Level 1", "stance", "Hunter's Sense");
            Assert.IsTrue(engine.HasStance("Hunter's Sense", "Warblade"));
            Assert.IsFalse(engine.HasStance("Punishing Stance", "Warblade"));

            // Clearing the feat choice removes the projected slot
            session.SetFeatChoice("Level 1", "stance", "");
            Assert.IsFalse(engine.HasStance("Hunter's Sense", "Warblade"));
            Assert.IsFalse(engine.GetStanceSlots("Warblade").Any(s => s != null && s.IsFeatBound));
        }

        [TestMethod]
        public void ProjectFeatSlots_SameManeuverAsClassProgression_DoesNotHijackClassSlot() {
            var character = new Character { race = "Human" };
            character.classLevels.Clear();
            character.classLevels.Add("Warblade"); // Level 1 Warblade: 3 class slots + 1 feat slot = 4
            character.feats["Level 1"] = "Martial Study";
            character.featChoices["Level 1:class"] = "Warblade";
            character.featChoices["Level 1:maneuver"] = "Steel Wind";

            var session = new CharacterSession(character, catalog);
            var engine = session.Subsystems.Get<MartialManeuverEngine>();
            Assert.IsNotNull(engine);

            // Populate class slot 0 with the EXACT same maneuver name ("Steel Wind")
            engine.SetKnownSlot("Warblade", 0, "Steel Wind");
            engine.SetKnownSlot("Warblade", 1, "Steely Strike");
            engine.SetKnownSlot("Warblade", 2, "Moment of Perfect Mind");

            session.Recalculate();

            var knownSlots = engine.GetKnownSlots("Warblade").ToList();
            Assert.AreEqual(4, knownSlots.Count, "Warblade should have 3 class slots + 1 feat slot.");

            // Slot 0 (class progression slot) must NOT be feat-bound
            Assert.IsNotNull(knownSlots[0]);
            Assert.AreEqual("Steel Wind", knownSlots[0].ManeuverName);
            Assert.IsFalse(knownSlots[0].IsFeatBound, "Class progression slot should never be hijacked into a feat-bound slot.");
            Assert.IsNull(knownSlots[0].FeatSlot);

            // Feat slot (slot 3) must be the dedicated feat-bound slot
            Assert.IsNotNull(knownSlots[3]);
            Assert.AreEqual("Steel Wind", knownSlots[3].ManeuverName);
            Assert.IsTrue(knownSlots[3].IsFeatBound, "Feat slot should be dedicated and marked IsFeatBound.");
            Assert.AreEqual("Level 1", knownSlots[3].FeatSlot);

            // Class slot 0 should still be retrainable / modifiable as a normal class slot
            engine.SetKnownSlot("Warblade", 0, "Douse the Flames");
            Assert.AreEqual("Douse the Flames", engine.GetKnownSlot("Warblade", 0).ManeuverName);
            Assert.AreEqual("Steel Wind", engine.GetKnownSlot("Warblade", 3).ManeuverName);
        }

        [TestMethod]
        public void ProjectFeatSlots_SameStanceAsClassProgression_DoesNotHijackClassSlot() {
            var character = new Character { race = "Human" };
            character.classLevels.Clear();
            character.classLevels.Add("Warblade"); // Level 1 Warblade: 1 class stance + 1 feat stance = 2
            character.feats["Level 1"] = "Martial Stance";
            character.featChoices["Level 1:class"] = "Warblade";
            character.featChoices["Level 1:stance"] = "Punishing Stance";

            var session = new CharacterSession(character, catalog);
            var engine = session.Subsystems.Get<MartialManeuverEngine>();
            Assert.IsNotNull(engine);

            // Populate class stance slot 0 with the EXACT same stance name ("Punishing Stance")
            engine.SetStanceSlot("Warblade", 0, "Punishing Stance");

            session.Recalculate();

            var stanceSlots = engine.GetStanceSlots("Warblade").ToList();
            Assert.AreEqual(2, stanceSlots.Count, "Warblade should have 1 class stance slot + 1 feat stance slot.");

            // Slot 0 (class progression slot) must NOT be feat-bound
            Assert.IsNotNull(stanceSlots[0]);
            Assert.AreEqual("Punishing Stance", stanceSlots[0].StanceName);
            Assert.IsFalse(stanceSlots[0].IsFeatBound, "Class progression stance slot should never be hijacked into a feat-bound slot.");
            Assert.IsNull(stanceSlots[0].FeatSlot);

            // Feat slot (slot 1) must be dedicated feat-bound slot
            Assert.IsNotNull(stanceSlots[1]);
            Assert.AreEqual("Punishing Stance", stanceSlots[1].StanceName);
            Assert.IsTrue(stanceSlots[1].IsFeatBound, "Feat stance slot should be dedicated and marked IsFeatBound.");
            Assert.AreEqual("Level 1", stanceSlots[1].FeatSlot);
        }
    }
}
