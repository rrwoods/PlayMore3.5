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
using static PlayMore35.Effects.Constants;

namespace PlayMore35.Tests {
    [TestClass]
    public class MartialManeuverSessionIntegrationTests {
        private IRulesCatalog catalog;

        [TestInitialize]
        public void SetUp() {
            catalog = new GameRulesCatalog();
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
            Assert.AreEqual(3, session.GetStatistic("max maneuver level:warblade").Value, string.Join("; ", session.GetStatistic("max maneuver level:warblade").Explanation));

            var engine = session.GetSubsystem("martial_maneuvers")!;
            Assert.IsNotNull(engine);
            Assert.IsTrue(engine.IsActive);

            Assert.AreEqual(6, engine.GetSlotCapacity("maneuvers", "Warblade"));
            Assert.AreEqual(2, engine.GetSlotCapacity("stances", "Warblade"));
            Assert.AreEqual(3, engine.GetMaxLevel("maneuvers", "Warblade"));
            Assert.AreEqual(3, engine.GetMaxLevel("stances", "Warblade"));
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

            var engine = session.GetSubsystem("martial_maneuvers")!;
            Assert.IsNotNull(engine);
            Assert.IsTrue(engine.IsActive);

            Assert.AreEqual(3, engine.GetMaxLevel("maneuvers", "Warblade"));
            Assert.AreEqual(2, engine.GetMaxLevel("maneuvers", "Crusader"));
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

            var engine = session.GetSubsystem("martial_maneuvers")!;
            Assert.IsNotNull(engine);
            Assert.IsTrue(engine.IsActive);

            Assert.AreEqual(1, engine.GetSlotCapacity("maneuvers", "Feats"));
            Assert.AreEqual(1, engine.GetMaxLevel("maneuvers", "Feats"));
        }

        // Source: Tome of Battle, pp. 5, 38, 43, 69
        [TestMethod]
        public void ActiveStance_ModifiesArmorClass_AndRecalculatesDynamically() {
            var character = new Character();
            character.classLevels.Clear();
            character.classLevels.Add("Warblade");
            character.assignedAbilityScores["DEX"] = 10; // 0 DEX mod

            var session = new CharacterSession(character, catalog);
            var engine = session.GetSubsystem("martial_maneuvers")!;
            Assert.IsNotNull(engine);

            // Baseline AC: 10 (no armor, 0 DEX mod)
            Assert.AreEqual(10, session.GetStatistic("AC").Value);

            engine.AssignSlot("stances", 0, "Punishing Stance", "inactive", "Warblade");
            session.Recalculate();

            // Inactive stance: AC remains 10
            Assert.AreEqual(10, session.GetStatistic("AC").Value);

            // Adopt Punishing Stance (-2 penalty to AC)
            engine.InvokeVerb("adopt_stance", "stances", 0, source: "Warblade");
            session.Recalculate();
            Assert.AreEqual("active", engine.GetSlotState("stances", 0, "Warblade"));
            Assert.AreEqual(8, session.GetStatistic("AC").Value);

            // Exit stance: AC returns to baseline 10
            engine.InvokeVerb("exit_stance", "stances", 0, source: "Warblade");
            session.Recalculate();
            Assert.AreEqual("inactive", engine.GetSlotState("stances", 0, "Warblade"));
            Assert.AreEqual(10, session.GetStatistic("AC").Value);
        }

        // Source: Tome of Battle, pp. 5, 38, 43, 69, 91
        [TestMethod]
        public void ActiveStance_AffectingEquippedWeaponAttacks_RecalculatesRoutines() {
            var attackStanceEffect = new Effect("Stance of the Aggressor")
                .AddAttackModifier(AttackMatcher.ALL, new Modifier(2, "morale"));
            var testStance = new SubsystemElement {
                Name = "Stance of the Aggressor",
                Type = "Stance"
            };
            testStance.SetProperty("discipline", "Iron Heart");
            testStance.SetProperty("level", 1);
            testStance.AddEffect("benefits", attackStanceEffect);
            var customCatalog = new FakeRulesCatalog(new[] { testStance }, inner: catalog);
            var character = new Character();
            character.classLevels.Clear();
            for (int i = 0; i < 3; i++) character.classLevels.Add("Warblade"); // BAB +3
            character.assignedAbilityScores["STR"] = 16; // STR +3
            character.equipped.PrimaryHandWeapon = new WeaponChoice { Name = "greatsword" };

            var session = new CharacterSession(character, customCatalog);
            var customEngine = session.GetSubsystem("martial_maneuvers")!;
            session.Recalculate();

            // Greatsword attack without stance: BAB +3 + STR +3 = +6
            var routines = session.GetAllRoutines();
            var standardRoutine = routines.FirstOrDefault(r => r.ActionType == RoutineActionType.Standard && r.Category == RoutineCategory.PrimarySolo);
            Assert.IsNotNull(standardRoutine);
            Assert.AreEqual(6, standardRoutine.Attacks[0].AttackBonus.bonus);

            // Set up stance slot and adopt Stance of the Aggressor
            customEngine.AssignSlot("stances", 0, "Stance of the Aggressor", "inactive", "Warblade");
            customEngine.InvokeVerb("adopt_stance", "stances", 0, source: "Warblade");
            session.Recalculate();

            // Greatsword attack with active stance: +6 + 2 morale = +8
            routines = session.GetAllRoutines();
            standardRoutine = routines.FirstOrDefault(r => r.ActionType == RoutineActionType.Standard && r.Category == RoutineCategory.PrimarySolo);
            Assert.IsNotNull(standardRoutine);
            Assert.AreEqual(8, standardRoutine.Attacks[0].AttackBonus.bonus);
            Assert.IsTrue(standardRoutine.Attacks[0].AttackBonus.explanation.Any(e => e.Contains("morale") || e.Contains("Aggressor")));

            // Exit stance
            customEngine.InvokeVerb("exit_all_stances");
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
            var engine = session.GetSubsystem("martial_maneuvers")!;
            Assert.IsNotNull(engine);

            // Initial land speed
            Assert.AreEqual(30, session.GetStatistic("Land speed").Value);

            // Slot 0: Wind Stride (+10 enhancement bonus to Land speed for 1 round)
            engine.AssignSlot("maneuvers", 0, "Wind Stride", "unreadied", "Warblade");
            // Slot 1: Steely Strike
            engine.AssignSlot("maneuvers", 1, "Steely Strike", "unreadied", "Warblade");

            // Ready maneuvers
            engine.InvokeVerb("ready", "maneuvers", 0, source: "Warblade");
            engine.InvokeVerb("ready", "maneuvers", 1, source: "Warblade");
            Assert.AreEqual("available", engine.GetSlotState("maneuvers", 0, "Warblade"));
            Assert.AreEqual("available", engine.GetSlotState("maneuvers", 1, "Warblade"));

            session.StartCombat();

            // Initiate Wind Stride (Boost)
            var boostReq = engine.InvokeVerb("expend", "maneuvers", 0, source: "Warblade");
            Assert.AreNotEqual(TimelineRefreshRequirement.None, boostReq);
            Assert.AreEqual("expended", engine.GetSlotState("maneuvers", 0, "Warblade"));

            // Dynamic recalculation applies boost modifier to Land speed
            session.Recalculate();
            Assert.AreEqual(40, session.GetStatistic("Land speed").Value);

            // Advance timeline by 1 round (boost expires)
            var advanceResult = session.AdvanceTimeline(1);
            Assert.IsTrue(advanceResult.RefreshRequirement >= TimelineRefreshRequirement.RecomputeDerived);
            Assert.AreEqual(1, advanceResult.ExpiredActions.Count);
            Assert.IsInstanceOfType(advanceResult.ExpiredActions[0], typeof(SheetAction));
            Assert.AreEqual("Wind Stride", advanceResult.ExpiredActions[0].Name);

            // Land speed returns to baseline 30, but slot remains Expended
            Assert.AreEqual(30, session.GetStatistic("Land speed").Value);
            Assert.AreEqual("expended", engine.GetSlotState("maneuvers", 0, "Warblade"));

            // Initiate Steely Strike (Strike)
            var strikeReq = engine.InvokeVerb("expend", "maneuvers", 1, source: "Warblade");
            Assert.AreNotEqual(TimelineRefreshRequirement.None, strikeReq);
            Assert.AreEqual("expended", engine.GetSlotState("maneuvers", 1, "Warblade"));

            // Warblade Swift Recovery restores all expended maneuvers
            engine.InvokeVerb("recover", "maneuvers", source: "Warblade");
            Assert.AreEqual("available", engine.GetSlotState("maneuvers", 0, "Warblade"));
            Assert.AreEqual("available", engine.GetSlotState("maneuvers", 1, "Warblade"));
        }

        // Source: Tome of Battle, p. 38
        [TestMethod]
        public void CharacterSession_FullJsonSaveLoadRoundTrip_PreservesAllManeuverState() {
            var character = new Character { name = "Martial Adept Hero" };
            character.classLevels.Clear();
            for (int i = 0; i < 3; i++) character.classLevels.Add("Warblade");
            character.assignedAbilityScores["DEX"] = 10;

            var session = new CharacterSession(character, catalog);
            var engine = session.GetSubsystem("martial_maneuvers")!;

            engine.AssignSlot("maneuvers", 0, "Wind Stride", "available", "Warblade");
            engine.AssignSlot("maneuvers", 1, "Steely Strike", "expended", "Warblade");
            engine.AssignSlot("maneuvers", 2, "Iron Heart Surge", "unreadied", "Warblade");
            engine.AssignSlot("stances", 0, "Punishing Stance", "active", "Warblade");

            session.Recalculate();
            Assert.AreEqual(8, session.GetStatistic("AC").Value); // 10 - 2 from active Punishing Stance

            // Full character serialization round-trip
            JToken json = character.ToJson();
            Assert.IsNotNull(json["subsystems"]);
            Assert.IsNotNull(json["subsystems"]["martial_maneuvers"]);

            var restoredChar = new Character((JObject)json);
            Assert.IsTrue(restoredChar.Subsystems.ContainsKey("martial_maneuvers"));
            Assert.IsInstanceOfType(restoredChar.Subsystems["martial_maneuvers"], typeof(SubsystemState));

            // Load into a new CharacterSession
            var restoredSession = new CharacterSession(restoredChar, catalog);
            var restoredEngine = restoredSession.GetSubsystem("martial_maneuvers")!;
            Assert.IsNotNull(restoredEngine);
            Assert.IsTrue(restoredEngine.IsActive);

            // Verify active stance effect is immediately active
            Assert.AreEqual(8, restoredSession.GetStatistic("AC").Value);

            Assert.AreEqual("available", restoredEngine.GetSlotState("maneuvers", 0, "Warblade"));
            Assert.AreEqual("Wind Stride", restoredEngine.GetSlotElement("maneuvers", 0, "Warblade"));

            Assert.AreEqual("expended", restoredEngine.GetSlotState("maneuvers", 1, "Warblade"));
            Assert.AreEqual("Steely Strike", restoredEngine.GetSlotElement("maneuvers", 1, "Warblade"));

            Assert.AreEqual("unreadied", restoredEngine.GetSlotState("maneuvers", 2, "Warblade"));
            Assert.AreEqual("Iron Heart Surge", restoredEngine.GetSlotElement("maneuvers", 2, "Warblade"));

            Assert.AreEqual("Punishing Stance", restoredEngine.GetSlotElement("stances", 0, "Warblade"));
            Assert.AreEqual("active", restoredEngine.GetSlotState("stances", 0, "Warblade"));

            // Recovery on restored engine works cleanly
            restoredEngine.InvokeVerb("recover", "maneuvers", source: "Warblade");
            Assert.AreEqual("available", restoredEngine.GetSlotState("maneuvers", 1, "Warblade"));
        }

        // Source: Tome of Battle, pp. 38, 40
        [TestMethod]
        public void Crusader_EncounterDeck_DealAdvanceAndResetInSession() {
            var character = new Character();
            character.classLevels.Clear();
            character.classLevels.Add("Crusader"); // Level 1 Crusader: 5 known, 5 readied, 2 granted

            var session = new CharacterSession(character, catalog);
            var engine = session.GetSubsystem("martial_maneuvers")!;
            Assert.IsNotNull(engine);

            Assert.AreEqual(5, session.GetStatistic("maneuvers known:crusader").Value);
            Assert.AreEqual(5, session.GetStatistic("maneuvers readied:crusader").Value);
            Assert.AreEqual(2, session.GetStatistic("maneuvers granted:crusader").Value);

            engine.AssignSlot("maneuvers", 0, "Crusader's Strike", "withheld", "Crusader");
            engine.AssignSlot("maneuvers", 1, "Leading the Attack", "withheld", "Crusader");
            engine.AssignSlot("maneuvers", 2, "Douse the Flames", "withheld", "Crusader");
            engine.AssignSlot("maneuvers", 3, "Martial Spirit", "withheld", "Crusader");
            engine.AssignSlot("maneuvers", 4, "Shield Block", "withheld", "Crusader");

            // Fixed seed RNG for deterministic dealing
            engine.SetRandom(new Random(42));
            engine.InvokeVerb("deal", "maneuvers", source: "Crusader");

            // Exactly 2 granted (Available), 3 Withheld
            var crusaderSlots = engine.GetSlots("maneuvers", "Crusader");
            int availableCount = crusaderSlots.Count(s => s != null && !string.IsNullOrEmpty(s.Element) && string.Equals(s.State, "available", StringComparison.OrdinalIgnoreCase));
            int withheldCount = crusaderSlots.Count(s => s != null && !string.IsNullOrEmpty(s.Element) && string.Equals(s.State, "withheld", StringComparison.OrdinalIgnoreCase));
            Assert.AreEqual(2, availableCount);
            Assert.AreEqual(3, withheldCount);

            // Initiate one available maneuver
            int availableIndex = crusaderSlots.ToList().FindIndex(s => s != null && !string.IsNullOrEmpty(s.Element) && string.Equals(s.State, "available", StringComparison.OrdinalIgnoreCase));
            var initReq = engine.InvokeVerb("expend", "maneuvers", availableIndex, source: "Crusader");
            Assert.AreNotEqual(TimelineRefreshRequirement.None, initReq);
            Assert.AreEqual("expended", engine.GetSlotState("maneuvers", availableIndex, "Crusader"));

            // Advance Crusader turn: 1 Withheld becomes Available
            engine.InvokeVerb("advance_turn", "maneuvers", source: "Crusader");
            crusaderSlots = engine.GetSlots("maneuvers", "Crusader");
            int newAvailableCount = crusaderSlots.Count(s => s != null && !string.IsNullOrEmpty(s.Element) && string.Equals(s.State, "available", StringComparison.OrdinalIgnoreCase));
            Assert.AreEqual(2, newAvailableCount); // 1 unexpended + 1 newly granted
            int newWithheldCount = crusaderSlots.Count(s => s != null && !string.IsNullOrEmpty(s.Element) && string.Equals(s.State, "withheld", StringComparison.OrdinalIgnoreCase));
            Assert.AreEqual(2, newWithheldCount);

            // Reset encounter: reshuffles and redeals fresh hand of 2 available
            engine.InvokeVerb("reset_encounter");
            crusaderSlots = engine.GetSlots("maneuvers", "Crusader");
            Assert.AreEqual(2, crusaderSlots.Count(s => s != null && !string.IsNullOrEmpty(s.Element) && string.Equals(s.State, "available", StringComparison.OrdinalIgnoreCase)));
            Assert.AreEqual(3, crusaderSlots.Count(s => s != null && !string.IsNullOrEmpty(s.Element) && string.Equals(s.State, "withheld", StringComparison.OrdinalIgnoreCase)));
            Assert.AreEqual(0, crusaderSlots.Count(s => s != null && !string.IsNullOrEmpty(s.Element) && string.Equals(s.State, "expended", StringComparison.OrdinalIgnoreCase)));
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
            var engine = session.GetSubsystem("martial_maneuvers")!;
            Assert.IsNotNull(engine);

            var warbladeSlots = engine.GetSlots("maneuvers", "Warblade");
            var featSlot = warbladeSlots.FirstOrDefault(s => s != null && s.IsFeatBound);
            Assert.IsNotNull(featSlot, "Feat-bound slot should exist in Warblade known slots.");
            Assert.AreEqual("Steel Wind", featSlot.Element);
            Assert.IsTrue(featSlot.IsFeatBound);
            Assert.AreEqual("Level 1", featSlot.FeatSlot);
            Assert.IsTrue(warbladeSlots.Any(s => s != null && string.Equals(s.Element, "Steel Wind", StringComparison.OrdinalIgnoreCase)));
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
            var engine = session.GetSubsystem("martial_maneuvers")!;
            Assert.IsNotNull(engine);

            var featSlots = engine.GetSlots("maneuvers", "Feats");
            var featSlot = featSlots.FirstOrDefault(s => s != null && s.IsFeatBound);
            Assert.IsNotNull(featSlot, "Feat-bound slot should exist in Feats known slots.");
            Assert.AreEqual("Moment of Perfect Mind", featSlot.Element);
            Assert.IsTrue(featSlot.IsFeatBound);
            Assert.AreEqual("Level 1", featSlot.FeatSlot);
            Assert.IsTrue(featSlots.Any(s => s != null && string.Equals(s.Element, "Moment of Perfect Mind", StringComparison.OrdinalIgnoreCase)));
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
            var engine = session.GetSubsystem("martial_maneuvers")!;
            Assert.IsNotNull(engine);

            var warbladeSlots = engine.GetSlots("maneuvers", "Warblade");
            int featSlotIndex = -1;
            for (int i = 0; i < warbladeSlots.Count; i++) {
                if (warbladeSlots[i] != null && warbladeSlots[i].IsFeatBound) {
                    featSlotIndex = i;
                    break;
                }
            }
            Assert.IsTrue(featSlotIndex >= 0, "Feat-bound slot must be found.");

            // Attempting to overwrite throws
            Assert.ThrowsException<InvalidOperationException>(() => {
                engine.AssignSlot("maneuvers", featSlotIndex, "Iron Heart Surge", source: "Warblade");
            });

            // Attempting to clear throws
            Assert.ThrowsException<InvalidOperationException>(() => {
                engine.ClearSlot("maneuvers", featSlotIndex, "Warblade");
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
            var engine = session.GetSubsystem("martial_maneuvers")!;
            Assert.IsNotNull(engine);

            var warbladeSlots = engine.GetSlots("maneuvers", "Warblade");
            int featSlotIndex = warbladeSlots.ToList().FindIndex(s => s != null && s.IsFeatBound);
            Assert.IsTrue(featSlotIndex >= 0);

            // Ready the feat-bound maneuver
            engine.InvokeVerb("ready", "maneuvers", featSlotIndex, source: "Warblade");
            Assert.AreEqual("available", engine.GetSlotState("maneuvers", featSlotIndex, "Warblade"));

            // Recalculate session preserves readied state
            session.Recalculate();
            Assert.AreEqual("available", engine.GetSlotState("maneuvers", featSlotIndex, "Warblade"));

            // Initiate maneuver transitions state to Expended
            session.StartCombat();
            var req = engine.InvokeVerb("expend", "maneuvers", featSlotIndex, source: "Warblade");
            Assert.AreNotEqual(TimelineRefreshRequirement.None, req);
            Assert.AreEqual("expended", engine.GetSlotState("maneuvers", featSlotIndex, "Warblade"));

            // Warblade recovery restores it
            engine.InvokeVerb("recover", "maneuvers", source: "Warblade");
            Assert.AreEqual("available", engine.GetSlotState("maneuvers", featSlotIndex, "Warblade"));
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
            var engine = session.GetSubsystem("martial_maneuvers")!;
            Assert.IsNotNull(engine);

            // Populate class slots 0, 1, 2
            engine.AssignSlot("maneuvers", 0, "Moment of Perfect Mind", source: "Warblade");
            engine.AssignSlot("maneuvers", 1, "Steely Strike", source: "Warblade");
            engine.AssignSlot("maneuvers", 2, "Wind Stride", source: "Warblade");

            var featSlot = engine.GetSlots("maneuvers", "Warblade").FirstOrDefault(s => s != null && s.IsFeatBound);
            Assert.IsNotNull(featSlot);
            Assert.AreEqual("Steel Wind", featSlot.Element);

            // Level up to Warblade 4 (Table 1-3 allows retraining 1 maneuver at 4th level)
            character.classLevels.Add("Warblade"); // Level 2: 4 class + 1 feat = 5
            character.classLevels.Add("Warblade"); // Level 3: 5 class + 1 feat = 6
            character.classLevels.Add("Warblade"); // Level 4: 5 class + 1 feat = 6
            session.Recalculate();

            Assert.AreEqual(6, session.GetStatistic("maneuvers known:warblade").Value);

            // Normal class slots can be retrained (e.g. slot 0 to Iron Heart Surge)
            engine.AssignSlot("maneuvers", 0, "Iron Heart Surge", source: "Warblade");
            Assert.AreEqual("Iron Heart Surge", engine.GetSlotElement("maneuvers", 0, "Warblade"));

            // Feat-bound slot remains in known slots and cannot be retrained or cleared
            int featIndex = engine.GetSlots("maneuvers", "Warblade").ToList().FindIndex(s => s != null && s.IsFeatBound);
            Assert.IsTrue(featIndex >= 0);
            Assert.AreEqual("Steel Wind", engine.GetSlotElement("maneuvers", featIndex, "Warblade"));

            Assert.ThrowsException<InvalidOperationException>(() => {
                engine.AssignSlot("maneuvers", featIndex, "Mountain Hammer", source: "Warblade");
            });

            Assert.ThrowsException<InvalidOperationException>(() => {
                engine.ClearSlot("maneuvers", featIndex, "Warblade");
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
            var engine = session.GetSubsystem("martial_maneuvers")!;
            Assert.IsNotNull(engine);

            // Mountain Hammer must project into Crusader known slots, NOT Warblade
            Assert.IsTrue(engine.GetSlots("maneuvers", "Crusader").Any(s => s != null && string.Equals(s.Element, "Mountain Hammer", StringComparison.OrdinalIgnoreCase)));
            Assert.IsFalse(engine.GetSlots("maneuvers", "Warblade").Any(s => s != null && string.Equals(s.Element, "Mountain Hammer", StringComparison.OrdinalIgnoreCase)));

            var crusaderFeatSlot = engine.GetSlots("maneuvers", "Crusader").FirstOrDefault(s => s != null && s.IsFeatBound);
            Assert.IsNotNull(crusaderFeatSlot);
            Assert.AreEqual("Mountain Hammer", crusaderFeatSlot.Element);
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
            var engine = session.GetSubsystem("martial_maneuvers")!;

            int featIndex = engine.GetSlots("maneuvers", "Warblade").ToList().FindIndex(s => s != null && s.IsFeatBound);
            Assert.IsTrue(featIndex >= 0);
            engine.InvokeVerb("ready", "maneuvers", featIndex, source: "Warblade");

            // Full character serialization round-trip
            JToken json = character.ToJson();
            var restoredChar = new Character((JObject)json);
            var restoredSession = new CharacterSession(restoredChar, catalog);
            var restoredEngine = restoredSession.GetSubsystem("martial_maneuvers")!;

            var restoredSlot = restoredEngine.GetSlots("maneuvers", "Warblade").FirstOrDefault(s => s != null && s.IsFeatBound);
            Assert.IsNotNull(restoredSlot);
            Assert.AreEqual("Steel Wind", restoredSlot.Element);
            Assert.IsTrue(restoredSlot.IsFeatBound);
            Assert.AreEqual("Level 1", restoredSlot.FeatSlot);
            Assert.AreEqual("available", restoredSlot.State);
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
            var engine = session.GetSubsystem("martial_maneuvers")!;

            Assert.IsTrue(engine.GetSlots("maneuvers", "Warblade").Any(s => s != null && string.Equals(s.Element, "Steel Wind", StringComparison.OrdinalIgnoreCase)));

            // Changing the feat choice updates the projected slot name
            session.SetFeatChoice("Level 1", "maneuver", "Stone Bones");
            Assert.IsTrue(engine.GetSlots("maneuvers", "Warblade").Any(s => s != null && string.Equals(s.Element, "Stone Bones", StringComparison.OrdinalIgnoreCase)));
            Assert.IsFalse(engine.GetSlots("maneuvers", "Warblade").Any(s => s != null && string.Equals(s.Element, "Steel Wind", StringComparison.OrdinalIgnoreCase)));

            // Clearing the feat choice removes the projected slot
            session.SetFeatChoice("Level 1", "maneuver", "");
            Assert.IsFalse(engine.GetSlots("maneuvers", "Warblade").Any(s => s != null && string.Equals(s.Element, "Stone Bones", StringComparison.OrdinalIgnoreCase)));
            Assert.IsFalse(engine.GetSlots("maneuvers", "Warblade").Any(s => s != null && s.IsFeatBound));
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
            var engine = session.GetSubsystem("martial_maneuvers")!;
            Assert.IsNotNull(engine);

            // Warblade has 1 base stance known, feat grants +1 -> total 2
            Assert.AreEqual(2, session.GetStatistic("stances known:warblade").Value);

            var stanceSlots = engine.GetSlots("stances", "Warblade");
            var featSlot = stanceSlots.FirstOrDefault(s => s != null && s.IsFeatBound);
            Assert.IsNotNull(featSlot, "Feat-bound stance slot should exist in Warblade stance slots.");
            Assert.AreEqual("Punishing Stance", featSlot.Element);
            Assert.IsTrue(featSlot.IsFeatBound);
            Assert.AreEqual("Level 1", featSlot.FeatSlot);
            Assert.IsTrue(stanceSlots.Any(s => s != null && string.Equals(s.Element, "Punishing Stance", StringComparison.OrdinalIgnoreCase)));
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
            var engine = session.GetSubsystem("martial_maneuvers")!;
            Assert.IsNotNull(engine);

            // Non-adept: stances known:feats = 1, active stances = 1
            Assert.AreEqual(1, session.GetStatistic("stances known:feats").Value);
            Assert.AreEqual(1, session.GetStatistic("active stances").Value);

            var featSlots = engine.GetSlots("stances", "Feats");
            var featSlot = featSlots.FirstOrDefault(s => s != null && s.IsFeatBound);
            Assert.IsNotNull(featSlot, "Feat-bound stance slot should exist in Feats stance slots.");
            Assert.AreEqual("Bolstering Voice", featSlot.Element);
            Assert.IsTrue(featSlot.IsFeatBound);
            Assert.AreEqual("Level 1", featSlot.FeatSlot);
            Assert.IsTrue(featSlots.Any(s => s != null && string.Equals(s.Element, "Bolstering Voice", StringComparison.OrdinalIgnoreCase)));
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
            var engine = session.GetSubsystem("martial_maneuvers")!;
            Assert.IsNotNull(engine);

            var warbladeSlots = engine.GetSlots("stances", "Warblade");
            int featSlotIndex = -1;
            for (int i = 0; i < warbladeSlots.Count; i++) {
                if (warbladeSlots[i] != null && warbladeSlots[i].IsFeatBound) {
                    featSlotIndex = i;
                    break;
                }
            }
            Assert.IsTrue(featSlotIndex >= 0, "Feat-bound stance slot must be found.");

            // Attempting to overwrite throws
            Assert.ThrowsException<InvalidOperationException>(() => {
                engine.AssignSlot("stances", featSlotIndex, "Hunter's Sense", source: "Warblade");
            });

            // Attempting to clear throws
            Assert.ThrowsException<InvalidOperationException>(() => {
                engine.ClearSlot("stances", featSlotIndex, "Warblade");
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
            var engine = session.GetSubsystem("martial_maneuvers")!;
            Assert.IsNotNull(engine);

            var warbladeSlots = engine.GetSlots("stances", "Warblade");
            int featSlotIndex = warbladeSlots.ToList().FindIndex(s => s != null && s.IsFeatBound);
            Assert.IsTrue(featSlotIndex >= 0);

            // Adopt the feat-bound stance
            engine.InvokeVerb("adopt_stance", "stances", featSlotIndex, source: "Warblade");
            Assert.AreEqual("active", engine.GetSlotState("stances", featSlotIndex, "Warblade"));

            // Recalculate session preserves active stance
            session.Recalculate();
            Assert.AreEqual("active", engine.GetSlotState("stances", featSlotIndex, "Warblade"));
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
            var engine = session.GetSubsystem("martial_maneuvers")!;

            Assert.IsTrue(engine.GetSlots("stances", "Feats").Any(s => s != null && string.Equals(s.Element, "Bolstering Voice", StringComparison.OrdinalIgnoreCase)));

            // Multiclass to Warblade at level 2
            character.classLevels.Add("Warblade");
            session.Recalculate();

            // Stance must remain in Feats pool, not migrated to Warblade
            Assert.IsTrue(engine.GetSlots("stances", "Feats").Any(s => s != null && string.Equals(s.Element, "Bolstering Voice", StringComparison.OrdinalIgnoreCase)));
            Assert.IsFalse(engine.GetSlots("stances", "Warblade").Any(s => s != null && string.Equals(s.Element, "Bolstering Voice", StringComparison.OrdinalIgnoreCase)));
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
            var engine = session.GetSubsystem("martial_maneuvers")!;
            Assert.IsNotNull(engine);

            // Iron Guard's Glare must project into Crusader stance slots, NOT Warblade
            Assert.IsTrue(engine.GetSlots("stances", "Crusader").Any(s => s != null && string.Equals(s.Element, "Iron Guard's Glare", StringComparison.OrdinalIgnoreCase)));
            Assert.IsFalse(engine.GetSlots("stances", "Warblade").Any(s => s != null && string.Equals(s.Element, "Iron Guard's Glare", StringComparison.OrdinalIgnoreCase)));

            var crusaderFeatSlot = engine.GetSlots("stances", "Crusader").FirstOrDefault(s => s != null && s.IsFeatBound);
            Assert.IsNotNull(crusaderFeatSlot);
            Assert.AreEqual("Iron Guard's Glare", crusaderFeatSlot.Element);
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
            var engine = session.GetSubsystem("martial_maneuvers")!;

            int featIndex = engine.GetSlots("stances", "Warblade").ToList().FindIndex(s => s != null && s.IsFeatBound);
            Assert.IsTrue(featIndex >= 0);
            engine.InvokeVerb("adopt_stance", "stances", featIndex, source: "Warblade");

            // Full character serialization round-trip
            JToken json = character.ToJson();
            var restoredChar = new Character((JObject)json);
            var restoredSession = new CharacterSession(restoredChar, catalog);
            var restoredEngine = restoredSession.GetSubsystem("martial_maneuvers")!;

            var restoredSlot = restoredEngine.GetSlots("stances", "Warblade").FirstOrDefault(s => s != null && s.IsFeatBound);
            Assert.IsNotNull(restoredSlot);
            Assert.AreEqual("Punishing Stance", restoredSlot.Element);
            Assert.IsTrue(restoredSlot.IsFeatBound);
            Assert.AreEqual("Level 1", restoredSlot.FeatSlot);
            Assert.AreEqual("active", restoredSlot.State);
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
            var engine = session.GetSubsystem("martial_maneuvers")!;

            Assert.IsTrue(engine.GetSlots("stances", "Warblade").Any(s => s != null && string.Equals(s.Element, "Punishing Stance", StringComparison.OrdinalIgnoreCase)));

            // Changing the feat choice updates the projected stance name
            session.SetFeatChoice("Level 1", "stance", "Hunter's Sense");
            Assert.IsTrue(engine.GetSlots("stances", "Warblade").Any(s => s != null && string.Equals(s.Element, "Hunter's Sense", StringComparison.OrdinalIgnoreCase)));
            Assert.IsFalse(engine.GetSlots("stances", "Warblade").Any(s => s != null && string.Equals(s.Element, "Punishing Stance", StringComparison.OrdinalIgnoreCase)));

            // Clearing the feat choice removes the projected slot
            session.SetFeatChoice("Level 1", "stance", "");
            Assert.IsFalse(engine.GetSlots("stances", "Warblade").Any(s => s != null && string.Equals(s.Element, "Hunter's Sense", StringComparison.OrdinalIgnoreCase)));
            Assert.IsFalse(engine.GetSlots("stances", "Warblade").Any(s => s != null && s.IsFeatBound));
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
            var engine = session.GetSubsystem("martial_maneuvers")!;
            Assert.IsNotNull(engine);

            // Populate class slot 0 with the EXACT same maneuver name ("Steel Wind")
            engine.AssignSlot("maneuvers", 0, "Steel Wind", source: "Warblade");
            engine.AssignSlot("maneuvers", 1, "Steely Strike", source: "Warblade");
            engine.AssignSlot("maneuvers", 2, "Moment of Perfect Mind", source: "Warblade");

            session.Recalculate();

            var knownSlots = engine.GetSlots("maneuvers", "Warblade").ToList();
            Assert.AreEqual(4, knownSlots.Count, "Warblade should have 3 class slots + 1 feat slot.");

            // Slot 0 (class progression slot) must NOT be feat-bound
            Assert.IsNotNull(knownSlots[0]);
            Assert.AreEqual("Steel Wind", knownSlots[0].Element);
            Assert.IsFalse(knownSlots[0].IsFeatBound, "Class progression slot should never be hijacked into a feat-bound slot.");
            Assert.IsNull(knownSlots[0].FeatSlot);

            // Feat slot (slot 3) must be the dedicated feat-bound slot
            Assert.IsNotNull(knownSlots[3]);
            Assert.AreEqual("Steel Wind", knownSlots[3].Element);
            Assert.IsTrue(knownSlots[3].IsFeatBound, "Feat slot should be dedicated and marked IsFeatBound.");
            Assert.AreEqual("Level 1", knownSlots[3].FeatSlot);

            // Class slot 0 should still be retrainable / modifiable as a normal class slot
            engine.AssignSlot("maneuvers", 0, "Douse the Flames", source: "Warblade");
            Assert.AreEqual("Douse the Flames", engine.GetSlotElement("maneuvers", 0, "Warblade"));
            Assert.AreEqual("Steel Wind", engine.GetSlotElement("maneuvers", 3, "Warblade"));
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
            var engine = session.GetSubsystem("martial_maneuvers")!;
            Assert.IsNotNull(engine);

            // Populate class stance slot 0 with the EXACT same stance name ("Punishing Stance")
            engine.AssignSlot("stances", 0, "Punishing Stance", source: "Warblade");

            session.Recalculate();

            var stanceSlots = engine.GetSlots("stances", "Warblade").ToList();
            Assert.AreEqual(2, stanceSlots.Count, "Warblade should have 1 class stance slot + 1 feat stance slot.");

            // Slot 0 (class progression slot) must NOT be feat-bound
            Assert.IsNotNull(stanceSlots[0]);
            Assert.AreEqual("Punishing Stance", stanceSlots[0].Element);
            Assert.IsFalse(stanceSlots[0].IsFeatBound, "Class progression stance slot should never be hijacked into a feat-bound slot.");
            Assert.IsNull(stanceSlots[0].FeatSlot);

            // Feat slot (slot 1) must be dedicated feat-bound slot
            Assert.IsNotNull(stanceSlots[1]);
            Assert.AreEqual("Punishing Stance", stanceSlots[1].Element);
            Assert.IsTrue(stanceSlots[1].IsFeatBound, "Feat stance slot should be dedicated and marked IsFeatBound.");
            Assert.AreEqual("Level 1", stanceSlots[1].FeatSlot);
        }
    }
}
