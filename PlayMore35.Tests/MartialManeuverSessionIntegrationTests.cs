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

        private class TestManeuverContentProvider : ISubsystemContentProvider {
            private readonly Dictionary<string, Maneuver> maneuvers;

            public TestManeuverContentProvider(params Maneuver[] list) {
                maneuvers = list.ToDictionary(m => m.Name, m => m, StringComparer.OrdinalIgnoreCase);
            }

            public IEnumerable<Vestige> GetVestiges() => Enumerable.Empty<Vestige>();
            public Vestige GetVestige(string name) => null;

            public IEnumerable<Maneuver> GetManeuvers() => maneuvers.Values;

            public Maneuver GetManeuver(string name) {
                if (string.IsNullOrEmpty(name)) return null;
                return maneuvers.TryGetValue(name, out var m) ? m : null;
            }
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

            Assert.AreEqual(5, session.GetStatistic("initiator level:warblade").Value);
            Assert.AreEqual(6, session.GetStatistic("maneuvers known:warblade").Value);
            Assert.AreEqual(4, session.GetStatistic("maneuvers readied:warblade").Value);
            Assert.AreEqual(2, session.GetStatistic("stances known:warblade").Value);
            Assert.AreEqual(3, session.GetStatistic("max maneuver level:warblade").Value);

            var engine = session.Subsystems.Get<MartialManeuverEngine>();
            Assert.IsNotNull(engine);
            Assert.IsTrue(engine.IsActive);

            var warblade = engine.GetSource("Warblade");
            Assert.IsNotNull(warblade);
            Assert.AreEqual(6, warblade.Known.Capacity);
            Assert.AreEqual(2, warblade.Stances.Capacity);
            Assert.AreEqual(3, warblade.Known.MaxLevel);
            Assert.AreEqual(3, warblade.Stances.MaxLevel);
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
            Assert.AreEqual(5, session.GetStatistic("initiator level:warblade").Value);
            Assert.AreEqual(3, session.GetStatistic("max maneuver level:warblade").Value);

            // Crusader IL: 2 + floor(0.5 * 4) = 4 -> Max maneuver level: ceil(4 / 2) = 2
            Assert.AreEqual(4, session.GetStatistic("initiator level:crusader").Value);
            Assert.AreEqual(2, session.GetStatistic("max maneuver level:crusader").Value);

            var engine = session.Subsystems.Get<MartialManeuverEngine>();
            Assert.IsNotNull(engine);
            Assert.IsTrue(engine.IsActive);

            var warblade = engine.GetSource("Warblade");
            var crusader = engine.GetSource("Crusader");
            Assert.AreEqual(3, warblade.Known.MaxLevel);
            Assert.AreEqual(2, crusader.Known.MaxLevel);
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
            Assert.AreEqual(2, session.GetStatistic("initiator level:feats").Value);
            Assert.AreEqual(1, session.GetStatistic("max maneuver level:feats").Value);
            Assert.AreEqual(1, session.GetStatistic("maneuvers known:feats").Value);
            Assert.AreEqual(1, session.GetStatistic("maneuvers readied:feats").Value);

            var engine = session.Subsystems.Get<MartialManeuverEngine>();
            Assert.IsNotNull(engine);
            Assert.IsTrue(engine.IsActive);

            var feats = engine.GetSource("Feats");
            Assert.AreEqual(1, feats.Known.Capacity);
            Assert.AreEqual(1, feats.Known.MaxLevel);
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

            var warblade = engine.GetSource("Warblade");
            warblade.SetStanceSlot(0, new StanceSlot("Punishing Stance", isActive: false));
            session.Recalculate();

            // Inactive stance: AC remains 10
            Assert.AreEqual(10, session.GetStatistic("AC").Value);

            // Adopt Punishing Stance (-2 penalty to AC)
            engine.AdoptStance("Warblade", 0);
            session.Recalculate();
            Assert.IsTrue(warblade.GetStanceSlot(0).IsActive);
            Assert.AreEqual(8, session.GetStatistic("AC").Value);

            // Exit stance: AC returns to baseline 10
            engine.ExitStance("Warblade", 0);
            session.Recalculate();
            Assert.IsFalse(warblade.GetStanceSlot(0).IsActive);
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
            var customProvider = new TestManeuverContentProvider(testStance);
            var customEngine = new MartialManeuverEngine(customProvider);

            var character = new Character();
            character.classLevels.Clear();
            for (int i = 0; i < 3; i++) character.classLevels.Add("Warblade"); // BAB +3
            character.assignedAbilityScores["STR"] = 16; // STR +3
            character.equipped.PrimaryHandWeapon = new WeaponChoice { Name = "greatsword" };

            var session = new CharacterSession(character, catalog);
            session.Subsystems.Register(customEngine, "maneuver");
            customEngine.Initialize(session);
            session.Recalculate();

            // Greatsword attack without stance: BAB +3 + STR +3 = +6
            var routines = session.GetAllRoutines();
            var standardRoutine = routines.FirstOrDefault(r => r.ActionType == RoutineActionType.Standard && r.Category == RoutineCategory.PrimarySolo);
            Assert.IsNotNull(standardRoutine);
            Assert.AreEqual(6, standardRoutine.Attacks[0].AttackBonus.bonus);

            // Set up stance slot and adopt Stance of the Aggressor
            var warblade = customEngine.GetSource("Warblade");
            warblade.SetStanceSlot(0, new StanceSlot("Stance of the Aggressor"));
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

            var warblade = engine.GetSource("Warblade");
            // Slot 0: Wind Stride (+10 enhancement bonus to Land speed for 1 round)
            warblade.SetKnownSlot(0, new KnownManeuverSlot("Wind Stride", ManeuverState.Unreadied));
            // Slot 1: Steely Strike
            warblade.SetKnownSlot(1, new KnownManeuverSlot("Steely Strike", ManeuverState.Unreadied));

            // Ready maneuvers
            engine.ReadyManeuver("Warblade", 0);
            engine.ReadyManeuver("Warblade", 1);
            Assert.AreEqual(ManeuverState.Available, warblade.GetKnownSlot(0).State);
            Assert.AreEqual(ManeuverState.Available, warblade.GetKnownSlot(1).State);

            session.StartCombat();

            // Initiate Wind Stride (Boost)
            bool boostInitiated = engine.InitiateManeuver("Warblade", 0);
            Assert.IsTrue(boostInitiated);
            Assert.AreEqual(ManeuverState.Expended, warblade.GetKnownSlot(0).State);

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
            Assert.AreEqual(ManeuverState.Expended, warblade.GetKnownSlot(0).State);

            // Initiate Steely Strike (Strike)
            bool strikeInitiated = engine.InitiateManeuver("Warblade", 1);
            Assert.IsTrue(strikeInitiated);
            Assert.AreEqual(ManeuverState.Expended, warblade.GetKnownSlot(1).State);

            // Warblade Swift Recovery restores all expended maneuvers
            engine.RecoverWarblade("Warblade");
            Assert.AreEqual(ManeuverState.Available, warblade.GetKnownSlot(0).State);
            Assert.AreEqual(ManeuverState.Available, warblade.GetKnownSlot(1).State);
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

            var warblade = engine.GetSource("Warblade");
            warblade.SetKnownSlot(0, new KnownManeuverSlot("Wind Stride", ManeuverState.Available));
            warblade.SetKnownSlot(1, new KnownManeuverSlot("Steely Strike", ManeuverState.Expended));
            warblade.SetKnownSlot(2, new KnownManeuverSlot("Iron Heart Surge", ManeuverState.Unreadied));
            warblade.SetStanceSlot(0, new StanceSlot("Punishing Stance", isActive: true));

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

            var restoredWarblade = restoredEngine.GetSource("Warblade");
            Assert.AreEqual(ManeuverState.Available, restoredWarblade.GetKnownSlot(0).State);
            Assert.AreEqual("Wind Stride", restoredWarblade.GetKnownSlot(0).ManeuverName);

            Assert.AreEqual(ManeuverState.Expended, restoredWarblade.GetKnownSlot(1).State);
            Assert.AreEqual("Steely Strike", restoredWarblade.GetKnownSlot(1).ManeuverName);

            Assert.AreEqual(ManeuverState.Unreadied, restoredWarblade.GetKnownSlot(2).State);
            Assert.AreEqual("Iron Heart Surge", restoredWarblade.GetKnownSlot(2).ManeuverName);

            Assert.AreEqual("Punishing Stance", restoredWarblade.GetStanceSlot(0).StanceName);
            Assert.IsTrue(restoredWarblade.GetStanceSlot(0).IsActive);

            // Recovery on restored engine works cleanly
            restoredEngine.RecoverWarblade("Warblade");
            Assert.AreEqual(ManeuverState.Available, restoredWarblade.GetKnownSlot(1).State);
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

            var crusader = engine.GetSource("Crusader");
            crusader.SetKnownSlot(0, new KnownManeuverSlot("Crusader's Strike", ManeuverState.Withheld));
            crusader.SetKnownSlot(1, new KnownManeuverSlot("Leading the Attack", ManeuverState.Withheld));
            crusader.SetKnownSlot(2, new KnownManeuverSlot("Douse the Flames", ManeuverState.Withheld));
            crusader.SetKnownSlot(3, new KnownManeuverSlot("Martial Spirit", ManeuverState.Withheld));
            crusader.SetKnownSlot(4, new KnownManeuverSlot("Shield Block", ManeuverState.Withheld));

            // Fixed seed RNG for deterministic dealing
            var rng = new Random(42);
            engine.DealCrusader("Crusader", rng);

            // Exactly 2 granted (Available), 3 Withheld
            int availableCount = crusader.Known.Slots.Count(s => s.State == ManeuverState.Available);
            int withheldCount = crusader.Known.Slots.Count(s => s.State == ManeuverState.Withheld);
            Assert.AreEqual(2, availableCount);
            Assert.AreEqual(3, withheldCount);

            // Initiate one available maneuver
            int availableIndex = crusader.Known.Slots.ToList().FindIndex(s => s.State == ManeuverState.Available);
            bool initiated = engine.InitiateManeuver("Crusader", availableIndex);
            Assert.IsTrue(initiated);
            Assert.AreEqual(ManeuverState.Expended, crusader.GetKnownSlot(availableIndex).State);

            // Advance Crusader turn: 1 Withheld becomes Available
            engine.AdvanceCrusaderTurn("Crusader", rng);
            int newAvailableCount = crusader.Known.Slots.Count(s => s.State == ManeuverState.Available);
            Assert.AreEqual(2, newAvailableCount); // 1 unexpended + 1 newly granted
            int newWithheldCount = crusader.Known.Slots.Count(s => s.State == ManeuverState.Withheld);
            Assert.AreEqual(2, newWithheldCount);

            // Reset encounter: reshuffles and redeals fresh hand of 2 available
            engine.ResetEncounter(rng);
            Assert.AreEqual(2, crusader.Known.Slots.Count(s => s.State == ManeuverState.Available));
            Assert.AreEqual(3, crusader.Known.Slots.Count(s => s.State == ManeuverState.Withheld));
            Assert.AreEqual(0, crusader.Known.Slots.Count(s => s.State == ManeuverState.Expended));
        }
    }
}
