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
using static PlayMore35.Effects.Constants;

namespace PlayMore35.Tests {
    [TestClass]
    public class MartialManeuverEngineTests {

        private static SubsystemElement CreateManeuver(string name, string discipline, int level, string type = "Strike", int prereqCount = 0) {
            var elem = new SubsystemElement {
                Name = name,
                Type = type
            };
            elem.SetProperty("discipline", discipline);
            elem.SetProperty("level", level);
            elem.SetProperty("prerequisiteCount", prereqCount);
            return elem;
        }

        private static SubsystemEngine CreateMartialEngine(params SubsystemElement[] maneuvers) {
            var catalog = new FakeRulesCatalog(maneuvers);
            if (maneuvers == null || maneuvers.Length == 0) {
                catalog.BuildMartialManeuversSubsystem();
            }
            var engine = new SubsystemEngine(catalog.GetSubsystem("martial_maneuvers"));
            var context = new FakeCharacterContext(catalog: catalog);
            engine.Initialize(context);
            return engine;
        }

        private static SubsystemEngine CreateMartialEngineWithContext(FakeRulesCatalog catalog, FakeCharacterContext context) {
            if (catalog.GetSubsystem("martial_maneuvers") == null) {
                catalog.BuildMartialManeuversSubsystem();
            }
            context.Catalog = catalog;
            var engine = new SubsystemEngine(catalog.GetSubsystem("martial_maneuvers"));
            engine.Initialize(context);
            return engine;
        }

        private static void SetStat(EffectPool pool, string statName, int value) {
            if (!pool.stats.ContainsKey(statName)) {
                pool.AddStat(new Statistic(statName));
            }
            pool.AddEffect(new Effect($"Base {statName}").AddModifier(statName, new Modifier(value, ModifierTypes.UNTYPED)));
        }

        // Source: Tome of Battle, p. 38
        [TestMethod]
        public void ReadiedSlotGroup_DefinesFourCanonicalStates() {
            var catalog = new GameRulesCatalog();
            var def = catalog.GetSubsystem("martial_maneuvers");
            Assert.IsNotNull(def);
            Assert.IsTrue(def.SlotGroups.ContainsKey("readied"));
            CollectionAssert.AreEquivalent(
                new[] { "unreadied", "withheld", "available", "expended" },
                def.SlotGroups["readied"].States.Select(s => s.ToLowerInvariant()).ToArray());
        }

        // Source: Tome of Battle, p. 38
        [TestMethod]
        public void SubsystemSlot_TracksReadiedAndStanceState() {
            var unreadied = new SubsystemSlot("Steel Strike", "unreadied", "Warblade");
            Assert.AreEqual("unreadied", unreadied.State);

            var available = new SubsystemSlot("Steel Strike", "available", "Warblade");
            Assert.AreEqual("available", available.State);

            var expended = new SubsystemSlot("Steel Strike", "expended", "Warblade");
            Assert.AreEqual("expended", expended.State);

            var withheld = new SubsystemSlot("Steel Strike", "withheld", "Crusader");
            Assert.AreEqual("withheld", withheld.State);
        }

        // Source: Tome of Battle, p. 38
        [TestMethod]
        public void SubsystemSlot_Readied_SerializesAndDeserializes_Cleanly() {
            var slot = new SubsystemSlot("Iron Heart Surge", "available", "Warblade");
            var json = (JObject)slot.ToJson();

            Assert.AreEqual("Iron Heart Surge", (string)json["element"]);
            Assert.AreEqual("available", (string)json["state"]);
            Assert.AreEqual("Warblade", (string)json["source"]);

            var restored = new SubsystemSlot(json);
            Assert.AreEqual("Iron Heart Surge", restored.Element);
            Assert.AreEqual("available", restored.State);
            Assert.AreEqual("Warblade", restored.Source);
        }

        // Source: Tome of Battle, p. 38
        [TestMethod]
        public void SubsystemSlot_Stance_SerializesAndDeserializes_Cleanly() {
            var slot = new SubsystemSlot("Punishing Stance", "active", "Warblade");
            var json = (JObject)slot.ToJson();

            Assert.AreEqual("Punishing Stance", (string)json["element"]);
            Assert.AreEqual("active", (string)json["state"]);

            var restored = new SubsystemSlot(json);
            Assert.AreEqual("Punishing Stance", restored.Element);
            Assert.AreEqual("active", restored.State);
        }

        // Source: Tome of Battle, pp. 38, 40
        [TestMethod]
        public void SubsystemEngine_ManagesKnownAndStanceSlots_AndTracksReadied() {
            var engine = CreateMartialEngine();
            engine.AssignSlot("readied", 0, "Steel Strike", "available", "Warblade");
            engine.AssignSlot("readied", 1, "Disarming Strike", "expended", "Warblade");
            engine.AssignSlot("readied", 2, "Iron Heart Surge", "unreadied", "Warblade");
            engine.AssignSlot("stances", 0, "Punishing Stance", "active", "Warblade");

            var wbReadied = engine.GetSlots("readied", "Warblade");
            Assert.AreEqual(2, wbReadied.Count(s => s != null && !string.IsNullOrEmpty(s.Element) && !string.Equals(s.State, "unreadied", StringComparison.OrdinalIgnoreCase)));
            Assert.AreEqual(1, engine.GetSlots("stances", "Warblade").Count(s => s != null && string.Equals(s.State, "active", StringComparison.OrdinalIgnoreCase)));

            // JSON round-trip on engine.ExportState()
            var json = (JObject)engine.ExportState().ToJson();
            var restoredState = new SubsystemState(json);
            var restoredEngine = CreateMartialEngine();
            restoredEngine.LoadState(restoredState);

            Assert.AreEqual("Steel Strike", restoredEngine.GetSlotElement("readied", 0, "Warblade"));
            Assert.AreEqual("available", restoredEngine.GetSlotState("readied", 0, "Warblade"));
            Assert.AreEqual("Disarming Strike", restoredEngine.GetSlotElement("readied", 1, "Warblade"));
            Assert.AreEqual("expended", restoredEngine.GetSlotState("readied", 1, "Warblade"));
            Assert.AreEqual("Iron Heart Surge", restoredEngine.GetSlotElement("readied", 2, "Warblade"));
            Assert.AreEqual("unreadied", restoredEngine.GetSlotState("readied", 2, "Warblade"));
            Assert.AreEqual("Punishing Stance", restoredEngine.GetSlotElement("stances", 0, "Warblade"));
            Assert.AreEqual("active", restoredEngine.GetSlotState("stances", 0, "Warblade"));
        }

        [TestMethod]
        public void SubsystemEngine_SlotLookupMethods_ReturnManeuversAndStances() {
            var engine = CreateMartialEngine();
            engine.AssignSlot("readied", 0, "Steel Strike", "available", "Warblade");
            engine.AssignSlot("readied", 1, "Iron Heart Surge", "unreadied", "Warblade");
            engine.AssignSlot("stances", 0, "Punishing Stance", "active", "Warblade");

            var allKnown = engine.GetSlots("readied", "Warblade").Where(s => s != null && !string.IsNullOrEmpty(s.Element)).Select(s => s.Element).ToList();
            Assert.AreEqual(2, allKnown.Count);
            Assert.IsTrue(allKnown.Contains("Steel Strike"));
            Assert.IsTrue(allKnown.Contains("Iron Heart Surge"));
            Assert.IsFalse(allKnown.Contains("Nonexistent"));

            var allStances = engine.GetSlots("stances", "Warblade").Where(s => s != null && !string.IsNullOrEmpty(s.Element)).Select(s => s.Element).ToList();
            Assert.AreEqual(1, allStances.Count);
            Assert.IsTrue(allStances.Contains("Punishing Stance"));
            Assert.IsFalse(allStances.Contains("Nonexistent"));
        }

        [TestMethod]
        public void Subsystems_Maneuvers_RejectsNonCanonicalJsonPropertiesWithoutFallback() {
            // SubsystemElement: actionType is canonical, action is non-canonical fallback
            var badManeuverJson = new JObject(
                new JProperty("name", "Test Strike"),
                new JProperty("action", "standard")
            );
            var maneuver = new SubsystemElement(badManeuverJson);
            Assert.AreEqual("", maneuver.GetProperty<string>("actionType", ""));

            var canonicalManeuverJson = new JObject(
                new JProperty("name", "Test Strike"),
                new JProperty("actionType", "standard")
            );
            var canManeuver = new SubsystemElement(canonicalManeuverJson);
            Assert.AreEqual("standard", canManeuver.GetProperty<string>("actionType"));

            // SubsystemSlot: element is canonical, name/maneuverName are non-canonical
            var badSlotJson = new JObject(new JProperty("maneuverName", "Steel Strike"));
            var slot = new SubsystemSlot(badSlotJson);
            Assert.AreEqual("", slot.Element);

            // CharacterClass: subsystems.martial_maneuvers is canonical, root martial_maneuvers is non-canonical fallback
            var classJson = new JObject(
                new JProperty("name", "TestClass"),
                new JProperty("prerequisites", ""),
                new JProperty("hit die", 8),
                new JProperty("class skills", new JArray()),
                new JProperty("skill points", 2),
                new JProperty("base attack bonus", "SLOW"),
                new JProperty("base fort save", "SLOW"),
                new JProperty("base ref save", "SLOW"),
                new JProperty("base will save", "SLOW"),
                new JProperty("weapon proficiencies", new JArray()),
                new JProperty("armor proficiencies", new JArray()),
                new JProperty("shield proficiencies", new JArray()),
                new JProperty("class features", new JArray()),
                new JProperty("martial_maneuvers", new JObject(
                    new JProperty("stats", new JArray("maneuvers known:testclass"))
                ))
            );
            var cClass = new CharacterClass(classJson);
            Assert.IsFalse(cClass.Subsystems.ContainsKey("martial_maneuvers"));
        }

        // Source: Tome of Battle, p. 40
        [TestMethod]
        public void SubsystemState_PreservesSourceIsolation_AndRoundTripsJson() {
            var engine = CreateMartialEngine();
            engine.AssignSlot("readied", 0, "Steel Strike", "available", "Warblade");
            engine.AssignSlot("readied", 0, "Crusader's Strike", "available", "Crusader");

            var json = (JObject)engine.ExportState().ToJson();
            var restoredState = new SubsystemState(json);
            var restoredEngine = CreateMartialEngine();
            restoredEngine.LoadState(restoredState);

            Assert.AreEqual("Steel Strike", restoredEngine.GetSlotElement("readied", 0, "Warblade"));
            Assert.AreEqual("Crusader's Strike", restoredEngine.GetSlotElement("readied", 0, "Crusader"));
            Assert.AreNotEqual(restoredEngine.GetSlotElement("readied", 0, "Warblade"),
                               restoredEngine.GetSlotElement("readied", 0, "Crusader"));
        }

        // Source: Tome of Battle, pp. 38, 39
        [TestMethod]
        public void SubsystemEngine_IsActive_EvaluatesAgainstSourceStats() {
            var catalog = new FakeRulesCatalog();
            catalog.BuildMartialManeuversSubsystem();
            var context = new FakeCharacterContext(catalog: catalog);
            var engine = CreateMartialEngineWithContext(catalog, context);

            Assert.IsFalse(engine.IsActive);

            // Set maneuvers known:crusader = 1
            SetStat(context.Effects, "maneuvers known:crusader", 1);
            Assert.IsTrue(engine.IsActive);
        }

        // Source: Tome of Battle, p. 39
        [TestMethod]
        public void SubsystemEngine_WiresCapacityAndLevelGating_ToCharacterStats() {
            var m1 = CreateManeuver("Steel Strike", "Iron Heart", 1);
            var m2 = CreateManeuver("Iron Heart Surge", "Iron Heart", 3);
            var stance = CreateManeuver("Punishing Stance", "Iron Heart", 1, "Stance");
            var content = new FakeRulesCatalog(m1, m2, stance);

            var context = new FakeCharacterContext(catalog: content);
            SetStat(context.Effects, "maneuvers known:warblade", 3);
            SetStat(context.Effects, "max maneuver level:warblade", 2);
            SetStat(context.Effects, "stances known:warblade", 1);

            var engine = CreateMartialEngineWithContext(content, context);
            engine.Initialize(context);

            Assert.AreEqual(3, engine.GetSlotCapacity("readied", "Warblade"));
            Assert.AreEqual(2, engine.GetMaxLevel("readied", "Warblade"));
            Assert.AreEqual(1, engine.GetSlotCapacity("stances", "Warblade"));
            Assert.AreEqual(2, engine.GetMaxLevel("stances", "Warblade"));

            // Slot 0 with level 1 maneuver is valid
            engine.AssignSlot("readied", 0, "Steel Strike", source: "Warblade");
            Assert.IsTrue(engine.IsSlotLevelValid("readied", 0, "Warblade"));

            // Slot 1 with level 3 maneuver exceeds max level 2 (ADR-0001: accepts but reports invalid)
            engine.AssignSlot("readied", 1, "Iron Heart Surge", source: "Warblade");
            Assert.IsFalse(engine.IsSlotLevelValid("readied", 1, "Warblade"));
        }

        // Source: Tome of Battle, p. 39
        [TestMethod]
        public void ValidateChoice_WarnsWhenLevelExceedsMaxManeuverLevel() {
            var m3 = CreateManeuver("Iron Heart Surge", "Iron Heart", 3);
            var content = new FakeRulesCatalog(m3);

            var context = new FakeCharacterContext(catalog: content);
            SetStat(context.Effects, "max maneuver level:warblade", 2);
            var engine = CreateMartialEngineWithContext(content, context);

            var warnings = engine.ValidateChoice(null, null, "Iron Heart Surge", "Warblade");
            Assert.IsTrue(warnings.Any(w => w.Contains("exceeds maximum maneuver level 2")));
        }

        // Source: Tome of Battle, pp. 43-44
        [TestMethod]
        public void ValidateChoice_WarnsWhenDisciplinePrerequisitesNotMet() {
            var m1 = CreateManeuver("Steel Strike", "Iron Heart", 1);
            var m2 = CreateManeuver("Disarming Strike", "Iron Heart", 2, "Strike", prereqCount: 1);
            var content = new FakeRulesCatalog(m1, m2);

            var context = new FakeCharacterContext(catalog: content);
            SetStat(context.Effects, "maneuvers known:warblade", 3);
            SetStat(context.Effects, "max maneuver level:warblade", 5);
            var engine = CreateMartialEngineWithContext(content, context);

            // Initially no Iron Heart maneuvers known -> warning
            var warnings = engine.ValidateChoice(null, null, "Disarming Strike", "Warblade");
            Assert.IsTrue(warnings.Any(w => w.Contains("Requires 1 Iron Heart maneuver, but only 0 known")));

            // Learn Steel Strike -> prerequisite satisfied
            engine.AssignSlot("readied", 0, "Steel Strike", source: "Warblade");

            var satisfiedWarnings = engine.ValidateChoice(null, null, "Disarming Strike", "Warblade");
            Assert.IsFalse(satisfiedWarnings.Any(w => w.Contains("Requires 1 Iron Heart")));
        }

        // Source: Tome of Battle, pp. 43-44
        [TestMethod]
        public void ValidateChoice_StancesCountTowardDisciplinePrerequisites() {
            var stance = CreateManeuver("Punishing Stance", "Iron Heart", 1, "Stance");
            var m2 = CreateManeuver("Disarming Strike", "Iron Heart", 2, "Strike", prereqCount: 1);
            var content = new FakeRulesCatalog(stance, m2);

            var context = new FakeCharacterContext(catalog: content);
            SetStat(context.Effects, "maneuvers known:warblade", 2);
            SetStat(context.Effects, "stances known:warblade", 1);
            SetStat(context.Effects, "max maneuver level:warblade", 5);
            var engine = CreateMartialEngineWithContext(content, context);

            // Knowing Punishing Stance fulfills the 1 Iron Heart maneuver requirement
            engine.AssignSlot("stances", 0, "Punishing Stance", source: "Warblade");

            var warnings = engine.ValidateChoice(null, null, "Disarming Strike", "Warblade");
            Assert.IsFalse(warnings.Any(w => w.Contains("Requires 1 Iron Heart")));
        }

        // Source: Tome of Battle, p. 40
        [TestMethod]
        public void ValidateChoice_WarnsWhenLearnedUnderAnotherSource() {
            var m1 = CreateManeuver("Steel Strike", "Iron Heart", 1);
            var content = new FakeRulesCatalog(m1);

            var context = new FakeCharacterContext(catalog: content);
            SetStat(context.Effects, "maneuvers known:warblade", 2);
            SetStat(context.Effects, "maneuvers known:crusader", 2);
            var engine = CreateMartialEngineWithContext(content, context);

            engine.AssignSlot("readied", 0, "Steel Strike", source: "Warblade");

            // Attempting to learn Steel Strike under Crusader produces a cross-class duplicate warning
            var warnings = engine.ValidateChoice(null, null, "Steel Strike", "Crusader");
            Assert.IsTrue(warnings.Any(w => w.Contains("already learned under source 'Warblade'")));
        }

        // Source: Tome of Battle, p. 38
        [TestMethod]
        public void ValidateChoice_WarnsWhenReadiedCountExceedsCapacity() {
            var m1 = CreateManeuver("Steel Strike", "Iron Heart", 1);
            var m2 = CreateManeuver("Steely Strike", "Iron Heart", 1);
            var content = new FakeRulesCatalog(m1, m2);

            var context = new FakeCharacterContext(catalog: content);
            SetStat(context.Effects, "maneuvers known:warblade", 2);
            SetStat(context.Effects, "maneuvers readied:warblade", 1);
            var engine = CreateMartialEngineWithContext(content, context);

            engine.AssignSlot("readied", 0, "Steel Strike", "available", "Warblade");
            engine.AssignSlot("readied", 1, "Steely Strike", "available", "Warblade");

            // Total readied is 2, capacity is 1
            var warnings = engine.ValidateChoice(null, null, "Steel Strike", "Warblade");
            Assert.IsTrue(warnings.Any(w => w.Contains("Readied maneuvers count (2) exceeds readied capacity (1)")));
        }

        // Source: Tome of Battle, p. 38
        [TestMethod]
        public void ValidateChoice_WarnsWhenReadiedCountBelowCapacity() {
            var m1 = CreateManeuver("Steel Strike", "Iron Heart", 1);
            var m2 = CreateManeuver("Steely Strike", "Iron Heart", 1);
            var content = new FakeRulesCatalog(m1, m2);

            var context = new FakeCharacterContext(catalog: content);
            SetStat(context.Effects, "maneuvers known:warblade", 2);
            SetStat(context.Effects, "maneuvers readied:warblade", 2);
            var engine = CreateMartialEngineWithContext(content, context);

            engine.AssignSlot("readied", 0, "Steel Strike", "available", "Warblade");
            engine.AssignSlot("readied", 1, "Steely Strike", "unreadied", "Warblade");

            // Total readied is 1, capacity is 2
            var warnings = engine.ValidateChoice(null, null, "Steel Strike", "Warblade");
            Assert.IsTrue(warnings.Any(w => w.Contains("Readied maneuvers count (1) is below readied capacity (2) for Warblade.")));
        }

        // Source: Tome of Battle, p. 38
        [TestMethod]
        public void Character_Subsystems_DeserializesSubsystemState() {
            var character = new Character();
            character.name = "Test Martial Adept";
            var mState = new SubsystemState();
            mState.Slots["readied"] = new List<SubsystemSlot> {
                new SubsystemSlot("Steel Strike", "available", "Warblade")
            };
            mState.Slots["stances"] = new List<SubsystemSlot> {
                new SubsystemSlot("Punishing Stance", "active", "Warblade")
            };
            character.Subsystems["martial_maneuvers"] = mState;

            // Serialize to JSON
            var json = (JObject)character.ToJson();
            Assert.IsNotNull(json["subsystems"]?["martial_maneuvers"]);

            // Restore from JSON
            var restored = new Character(json);
            Assert.IsTrue(restored.Subsystems.ContainsKey("martial_maneuvers"));
            Assert.IsInstanceOfType(restored.Subsystems["martial_maneuvers"], typeof(SubsystemState));

            var restoredState = restored.Subsystems["martial_maneuvers"];
            Assert.IsTrue(restoredState.Slots.ContainsKey("readied"));
            Assert.AreEqual("Steel Strike", restoredState.Slots["readied"][0].Element);
            Assert.AreEqual("available", restoredState.Slots["readied"][0].State);
            Assert.AreEqual("Punishing Stance", restoredState.Slots["stances"][0].Element);
            Assert.AreEqual("active", restoredState.Slots["stances"][0].State);
        }

        // Source: Tome of Battle, p. 38
        [TestMethod]
        public void ExpendVerb_TransitionsAvailableToExpended_AndRejectsNonAvailable() {
            var engine = CreateMartialEngine();
            engine.AssignSlot("readied", 0, "Steel Strike", "available", "Warblade");
            engine.AssignSlot("readied", 1, "Disarming Strike", "expended", "Warblade");
            engine.AssignSlot("readied", 2, "Iron Heart Surge", "unreadied", "Warblade");
            engine.AssignSlot("readied", 3, "Wall of Blades", "withheld", "Warblade");

            // Available succeeds and transitions to Expended
            var req0 = engine.InvokeVerb("expend", "readied", 0, source: "Warblade");
            Assert.AreNotEqual(TimelineRefreshRequirement.None, req0);
            Assert.AreEqual("expended", engine.GetSlotState("readied", 0, "Warblade"));

            // Expended rejects
            var req1 = engine.InvokeVerb("expend", "readied", 1, source: "Warblade");
            Assert.AreEqual(TimelineRefreshRequirement.None, req1);
            Assert.AreEqual("expended", engine.GetSlotState("readied", 1, "Warblade"));

            // Unreadied rejects
            var req2 = engine.InvokeVerb("expend", "readied", 2, source: "Warblade");
            Assert.AreEqual(TimelineRefreshRequirement.None, req2);
            Assert.AreEqual("unreadied", engine.GetSlotState("readied", 2, "Warblade"));

            // Withheld rejects
            var req3 = engine.InvokeVerb("expend", "readied", 3, source: "Warblade");
            Assert.AreEqual(TimelineRefreshRequirement.None, req3);
            Assert.AreEqual("withheld", engine.GetSlotState("readied", 3, "Warblade"));
        }

        // Source: Tome of Battle, p. 40
        [TestMethod]
        public void RecoverWarblade_RecoversAllExpendedManeuvers_PreservingUnreadied() {
            var engine = CreateMartialEngine();
            engine.AssignSlot("readied", 0, "Steel Strike", "expended", "Warblade");
            engine.AssignSlot("readied", 1, "Disarming Strike", "expended", "Warblade");
            engine.AssignSlot("readied", 2, "Iron Heart Surge", "unreadied", "Warblade");

            engine.AssignSlot("readied", 0, "Crusader's Strike", "expended", "Crusader");

            engine.InvokeVerb("recover", "readied", source: "Warblade");

            Assert.AreEqual("available", engine.GetSlotState("readied", 0, "Warblade"));
            Assert.AreEqual("available", engine.GetSlotState("readied", 1, "Warblade"));
            Assert.AreEqual("unreadied", engine.GetSlotState("readied", 2, "Warblade"));

            // Crusader expended slot is unaffected (source isolation)
            Assert.AreEqual("expended", engine.GetSlotState("readied", 0, "Crusader"));
        }

        // Source: Tome of Battle, p. 40
        [TestMethod]
        public void RecoverSwordsage_RecoversSingleChosenManeuver() {
            var engine = CreateMartialEngine();
            engine.AssignSlot("readied", 0, "Burning Blade", "expended", "Swordsage");
            engine.AssignSlot("readied", 1, "Blistering Flourish", "expended", "Swordsage");
            engine.AssignSlot("readied", 2, "Shadow Jaunt", "unreadied", "Swordsage");

            engine.InvokeVerb("recover", "readied", 0, source: "Swordsage");

            Assert.AreEqual("available", engine.GetSlotState("readied", 0, "Swordsage"));
            Assert.AreEqual("expended", engine.GetSlotState("readied", 1, "Swordsage"));
            Assert.AreEqual("unreadied", engine.GetSlotState("readied", 2, "Swordsage"));

            // Attempting to recover unreadied slot does not alter its state
            engine.InvokeVerb("recover", "readied", 2, source: "Swordsage");
            Assert.AreEqual("unreadied", engine.GetSlotState("readied", 2, "Swordsage"));
        }

        // Source: Tome of Battle, pp. 38, 40
        [TestMethod]
        public void DealCrusader_DealsGrantedManeuversAvailable_AndRemainderWithheld() {
            var catalog = new FakeRulesCatalog();
            var context = new FakeCharacterContext(catalog: catalog);
            SetStat(context.Effects, "maneuvers known:crusader", 6);
            SetStat(context.Effects, "maneuvers granted:crusader", 2);
            var engine = CreateMartialEngineWithContext(catalog, context);

            engine.AssignSlot("readied", 0, "Crusader's Strike", "expended", "Crusader");
            engine.AssignSlot("readied", 1, "Vanguard Strike", "expended", "Crusader");
            engine.AssignSlot("readied", 2, "Stone Bones", "expended", "Crusader");
            engine.AssignSlot("readied", 3, "Charging Minotaur", "expended", "Crusader");
            engine.AssignSlot("readied", 4, "Leading the Attack", "expended", "Crusader");
            engine.AssignSlot("readied", 5, "Mountain Hammer", "unreadied", "Crusader");

            engine.SetRandom(new Random(42));
            engine.InvokeVerb("deal", "readied", source: "Crusader");

            var crusaderSlots = engine.GetSlots("readied", "Crusader");
            var readied = crusaderSlots.Where(s => s != null && !string.IsNullOrEmpty(s.Element) && !string.Equals(s.State, "unreadied", StringComparison.OrdinalIgnoreCase)).ToList();
            Assert.AreEqual(5, readied.Count);

            int availableCount = readied.Count(s => string.Equals(s.State, "available", StringComparison.OrdinalIgnoreCase));
            int withheldCount = readied.Count(s => string.Equals(s.State, "withheld", StringComparison.OrdinalIgnoreCase));

            Assert.AreEqual(2, availableCount);
            Assert.AreEqual(3, withheldCount);
            Assert.AreEqual("unreadied", engine.GetSlotState("readied", 5, "Crusader"));
        }

        // Source: Tome of Battle, p. 38
        [TestMethod]
        public void AdvanceCrusaderTurn_GrantsOneWithheldManeuver_EachTurn() {
            var catalog = new FakeRulesCatalog();
            var context = new FakeCharacterContext(catalog: catalog);
            SetStat(context.Effects, "maneuvers known:crusader", 5);
            SetStat(context.Effects, "maneuvers granted:crusader", 2);
            var engine = CreateMartialEngineWithContext(catalog, context);

            engine.AssignSlot("readied", 0, "Crusader's Strike", "expended", "Crusader");
            engine.AssignSlot("readied", 1, "Vanguard Strike", "expended", "Crusader");
            engine.AssignSlot("readied", 2, "Stone Bones", "expended", "Crusader");
            engine.AssignSlot("readied", 3, "Charging Minotaur", "expended", "Crusader");
            engine.AssignSlot("readied", 4, "Leading the Attack", "expended", "Crusader");

            // Deal hand: 2 Available, 3 Withheld
            engine.SetRandom(new Random(42));
            engine.InvokeVerb("deal", "readied", source: "Crusader");
            Assert.AreEqual(2, engine.GetSlots("readied", "Crusader").Count(s => s != null && string.Equals(s.State, "available", StringComparison.OrdinalIgnoreCase)));
            Assert.AreEqual(3, engine.GetSlots("readied", "Crusader").Count(s => s != null && string.Equals(s.State, "withheld", StringComparison.OrdinalIgnoreCase)));

            // Turn 1 advance grants 1 withheld maneuver
            engine.SetRandom(new Random(1));
            engine.InvokeVerb("advance_turn", "readied", source: "Crusader");
            Assert.AreEqual(3, engine.GetSlots("readied", "Crusader").Count(s => s != null && string.Equals(s.State, "available", StringComparison.OrdinalIgnoreCase)));
            Assert.AreEqual(2, engine.GetSlots("readied", "Crusader").Count(s => s != null && string.Equals(s.State, "withheld", StringComparison.OrdinalIgnoreCase)));

            // Turn 2 advance grants another withheld maneuver
            engine.SetRandom(new Random(1));
            engine.InvokeVerb("advance_turn", "readied", source: "Crusader");
            Assert.AreEqual(4, engine.GetSlots("readied", "Crusader").Count(s => s != null && string.Equals(s.State, "available", StringComparison.OrdinalIgnoreCase)));
            Assert.AreEqual(1, engine.GetSlots("readied", "Crusader").Count(s => s != null && string.Equals(s.State, "withheld", StringComparison.OrdinalIgnoreCase)));

            // Turn 3 advance grants final withheld maneuver
            engine.SetRandom(new Random(1));
            engine.InvokeVerb("advance_turn", "readied", source: "Crusader");
            Assert.AreEqual(5, engine.GetSlots("readied", "Crusader").Count(s => s != null && string.Equals(s.State, "available", StringComparison.OrdinalIgnoreCase)));
            Assert.AreEqual(0, engine.GetSlots("readied", "Crusader").Count(s => s != null && string.Equals(s.State, "withheld", StringComparison.OrdinalIgnoreCase)));
        }

        // Source: Tome of Battle, p. 38
        [TestMethod]
        public void AdvanceCrusaderTurn_ReshufflesDeck_WhenWithheldPoolIsEmpty() {
            var catalog = new FakeRulesCatalog();
            var context = new FakeCharacterContext(catalog: catalog);
            SetStat(context.Effects, "maneuvers known:crusader", 5);
            SetStat(context.Effects, "maneuvers granted:crusader", 2);
            var engine = CreateMartialEngineWithContext(catalog, context);

            engine.AssignSlot("readied", 0, "Crusader's Strike", "expended", "Crusader");
            engine.AssignSlot("readied", 1, "Vanguard Strike", "expended", "Crusader");
            engine.AssignSlot("readied", 2, "Stone Bones", "expended", "Crusader");
            engine.AssignSlot("readied", 3, "Charging Minotaur", "available", "Crusader");
            engine.AssignSlot("readied", 4, "Leading the Attack", "available", "Crusader");

            // Pool currently has 0 withheld maneuvers
            Assert.AreEqual(0, engine.GetSlots("readied", "Crusader").Count(s => s != null && string.Equals(s.State, "withheld", StringComparison.OrdinalIgnoreCase)));

            // Advancing from empty withheld pool triggers deck reshuffle: 2 Available, 3 Withheld
            engine.SetRandom(new Random(42));
            engine.InvokeVerb("advance_turn", "readied", source: "Crusader");

            Assert.AreEqual(2, engine.GetSlots("readied", "Crusader").Count(s => s != null && string.Equals(s.State, "available", StringComparison.OrdinalIgnoreCase)));
            Assert.AreEqual(3, engine.GetSlots("readied", "Crusader").Count(s => s != null && string.Equals(s.State, "withheld", StringComparison.OrdinalIgnoreCase)));
            Assert.AreEqual(0, engine.GetSlots("readied", "Crusader").Count(s => s != null && string.Equals(s.State, "expended", StringComparison.OrdinalIgnoreCase)));
        }

        // Source: Tome of Battle, pp. 38, 40
        [TestMethod]
        public void ResetEncounter_RestoresReadiedManeuversAcrossAllSources_AndRedealsCrusader() {
            var catalog = new FakeRulesCatalog();
            var context = new FakeCharacterContext(catalog: catalog);
            SetStat(context.Effects, "maneuvers known:warblade", 2);
            SetStat(context.Effects, "maneuvers known:swordsage", 2);
            SetStat(context.Effects, "maneuvers known:feats", 1);
            SetStat(context.Effects, "maneuvers known:crusader", 6);
            SetStat(context.Effects, "maneuvers granted:crusader", 2);
            var engine = CreateMartialEngineWithContext(catalog, context);

            engine.AssignSlot("readied", 0, "Steel Strike", "expended", "Warblade");
            engine.AssignSlot("readied", 1, "Iron Heart Surge", "unreadied", "Warblade");

            engine.AssignSlot("readied", 0, "Burning Blade", "expended", "Swordsage");
            engine.AssignSlot("readied", 1, "Shadow Jaunt", "available", "Swordsage");

            engine.AssignSlot("readied", 0, "Moment of Perfect Mind", "expended", "Feats");

            engine.AssignSlot("readied", 0, "Crusader's Strike", "expended", "Crusader");
            engine.AssignSlot("readied", 1, "Vanguard Strike", "expended", "Crusader");
            engine.AssignSlot("readied", 2, "Stone Bones", "expended", "Crusader");
            engine.AssignSlot("readied", 3, "Charging Minotaur", "expended", "Crusader");
            engine.AssignSlot("readied", 4, "Leading the Attack", "expended", "Crusader");
            engine.AssignSlot("readied", 5, "Mountain Hammer", "unreadied", "Crusader");

            engine.SetRandom(new Random(42));
            engine.InvokeVerb("reset_encounter");

            // Warblade: Expended -> Available, Unreadied -> Unreadied
            Assert.AreEqual("available", engine.GetSlotState("readied", 0, "Warblade"));
            Assert.AreEqual("unreadied", engine.GetSlotState("readied", 1, "Warblade"));

            // Swordsage: Expended -> Available, Available -> Available
            Assert.AreEqual("available", engine.GetSlotState("readied", 0, "Swordsage"));
            Assert.AreEqual("available", engine.GetSlotState("readied", 1, "Swordsage"));

            // Feats: Expended -> Available
            Assert.AreEqual("available", engine.GetSlotState("readied", 0, "Feats"));

            // Crusader: re-deals 5 readied into 2 Available, 3 Withheld; slot 5 remains Unreadied
            var crSlots = engine.GetSlots("readied", "Crusader");
            Assert.AreEqual(2, crSlots.Count(s => s != null && string.Equals(s.State, "available", StringComparison.OrdinalIgnoreCase)));
            Assert.AreEqual(3, crSlots.Count(s => s != null && string.Equals(s.State, "withheld", StringComparison.OrdinalIgnoreCase)));
            Assert.AreEqual(0, crSlots.Count(s => s != null && string.Equals(s.State, "expended", StringComparison.OrdinalIgnoreCase)));
            Assert.AreEqual("unreadied", engine.GetSlotState("readied", 5, "Crusader"));
        }

        // Source: Tome of Battle, p. 38
        [TestMethod]
        public void ReadyManeuver_AndUnreadyManeuver_TransitionsSlotLifecycle() {
            var engine = CreateMartialEngine();
            engine.AssignSlot("readied", 0, "Steel Strike", "unreadied", "Warblade");
            engine.AssignSlot("readied", 0, "Crusader's Strike", "unreadied", "Crusader");

            // Ready warblade maneuver -> becomes Available
            engine.InvokeVerb("ready", "readied", 0, source: "Warblade");
            Assert.AreEqual("available", engine.GetSlotState("readied", 0, "Warblade"));

            // Unready warblade maneuver -> returns to Unreadied
            engine.InvokeVerb("unready", "readied", 0, source: "Warblade");
            Assert.AreEqual("unreadied", engine.GetSlotState("readied", 0, "Warblade"));

            // Ready crusader maneuver -> becomes Withheld
            engine.InvokeVerb("ready", "readied", 0, source: "Crusader");
            Assert.AreEqual("withheld", engine.GetSlotState("readied", 0, "Crusader"));

            // Unready crusader maneuver -> returns to Unreadied
            engine.InvokeVerb("unready", "readied", 0, source: "Crusader");
            Assert.AreEqual("unreadied", engine.GetSlotState("readied", 0, "Crusader"));
        }

        // Source: Tome of Battle, pp. 5, 38, 43
        [TestMethod]
        public void AdoptStance_SetsActive_AndClearsOtherStances_WhenDualStanceNotPermitted() {
            var stance1 = CreateManeuver("Punishing Stance", "Iron Heart", 1, "Stance");
            var stance2 = CreateManeuver("Blood in the Water", "Tiger Claw", 1, "Stance");
            var stance3 = CreateManeuver("Hunter's Sense", "Tiger Claw", 1, "Stance");
            var content = new FakeRulesCatalog(stance1, stance2, stance3);

            var context = new FakeCharacterContext(catalog: content);
            SetStat(context.Effects, "stances known:warblade", 2);
            SetStat(context.Effects, "stances known:swordsage", 1);
            var engine = CreateMartialEngineWithContext(content, context);

            engine.AssignSlot("stances", 0, "Punishing Stance", "inactive", "Warblade");
            engine.AssignSlot("stances", 1, "Blood in the Water", "inactive", "Warblade");
            engine.AssignSlot("stances", 0, "Hunter's Sense", "inactive", "Swordsage");

            // Adopt warblade stance 0
            engine.InvokeVerb("adopt_stance", "stances", 0, source: "Warblade");
            Assert.AreEqual("active", engine.GetSlotState("stances", 0, "Warblade"));
            Assert.AreEqual("inactive", engine.GetSlotState("stances", 1, "Warblade"));
            Assert.AreEqual("inactive", engine.GetSlotState("stances", 0, "Swordsage"));

            // Switch to swordsage stance 0 -> warblade stance 0 is cleared
            engine.InvokeVerb("adopt_stance", "stances", 0, source: "Swordsage");
            Assert.AreEqual("inactive", engine.GetSlotState("stances", 0, "Warblade"));
            Assert.AreEqual("active", engine.GetSlotState("stances", 0, "Swordsage"));

            // Exit single stance
            engine.InvokeVerb("exit_stance", "stances", 0, source: "Swordsage");
            Assert.AreEqual("inactive", engine.GetSlotState("stances", 0, "Swordsage"));

            // Adopt again, then ExitAllStances
            engine.InvokeVerb("adopt_stance", "stances", 1, source: "Warblade");
            Assert.AreEqual("active", engine.GetSlotState("stances", 1, "Warblade"));
            engine.InvokeVerb("exit_all_stances");
            Assert.AreEqual("inactive", engine.GetSlotState("stances", 1, "Warblade"));
        }

        // Source: Tome of Battle, pp. 5, 23, 38, 43
        [TestMethod]
        public void AdoptStance_PreservesBothActiveStances_WhenActiveStancesIsTwo() {
            var stance1 = CreateManeuver("Punishing Stance", "Iron Heart", 1, "Stance");
            var stance2 = CreateManeuver("Blood in the Water", "Tiger Claw", 1, "Stance");
            var content = new FakeRulesCatalog(stance1, stance2);

            var context = new FakeCharacterContext(catalog: content);
            SetStat(context.Effects, "stances known:warblade", 2);
            SetStat(context.Effects, "active stances", 2);
            var engine = CreateMartialEngineWithContext(content, context);

            engine.AssignSlot("stances", 0, "Punishing Stance", "inactive", "Warblade");
            engine.AssignSlot("stances", 1, "Blood in the Water", "inactive", "Warblade");

            engine.InvokeVerb("adopt_stance", "stances", 0, source: "Warblade");
            Assert.AreEqual("active", engine.GetSlotState("stances", 0, "Warblade"));

            // Adopting second stance with active stances = 2 does not clear first stance
            engine.InvokeVerb("adopt_stance", "stances", 1, source: "Warblade");
            Assert.AreEqual("active", engine.GetSlotState("stances", 0, "Warblade"));
            Assert.AreEqual("active", engine.GetSlotState("stances", 1, "Warblade"));
        }

        // Source: Tome of Battle, pp. 5, 23, 38, 40, 43
        [TestMethod]
        public void AdoptStance_CapsConcurrentActiveStancesAtTwo_UsingOldestFallback_WhenNoTargetSpecified() {
            var stance1 = CreateManeuver("Punishing Stance", "Iron Heart", 1, "Stance");
            var stance2 = CreateManeuver("Blood in the Water", "Tiger Claw", 1, "Stance");
            var stance3 = CreateManeuver("Hunter's Sense", "Tiger Claw", 1, "Stance");
            var content = new FakeRulesCatalog(stance1, stance2, stance3);

            var context = new FakeCharacterContext(catalog: content);
            SetStat(context.Effects, "stances known:warblade", 3);
            SetStat(context.Effects, "active stances", 2);
            var engine = CreateMartialEngineWithContext(content, context);

            engine.AssignSlot("stances", 0, "Punishing Stance", "inactive", "Warblade");
            engine.AssignSlot("stances", 1, "Blood in the Water", "inactive", "Warblade");
            engine.AssignSlot("stances", 2, "Hunter's Sense", "inactive", "Warblade");

            // Adopt stance 0
            engine.InvokeVerb("adopt_stance", "stances", 0, source: "Warblade");
            Assert.AreEqual("active", engine.GetSlotState("stances", 0, "Warblade"));

            // Adopt stance 1 -> both active
            engine.InvokeVerb("adopt_stance", "stances", 1, source: "Warblade");
            Assert.AreEqual("active", engine.GetSlotState("stances", 0, "Warblade"));
            Assert.AreEqual("active", engine.GetSlotState("stances", 1, "Warblade"));
            Assert.AreEqual("inactive", engine.GetSlotState("stances", 2, "Warblade"));

            // Adopt stance 2 -> oldest (stance 0) is deactivated, stances 1 and 2 remain active
            engine.InvokeVerb("adopt_stance", "stances", 2, source: "Warblade");
            Assert.AreEqual("inactive", engine.GetSlotState("stances", 0, "Warblade"));
            Assert.AreEqual("active", engine.GetSlotState("stances", 1, "Warblade"));
            Assert.AreEqual("active", engine.GetSlotState("stances", 2, "Warblade"));

            // Adopt stance 0 again -> oldest (stance 1) is deactivated, stances 2 and 0 remain active
            engine.InvokeVerb("adopt_stance", "stances", 0, source: "Warblade");
            Assert.AreEqual("active", engine.GetSlotState("stances", 0, "Warblade"));
            Assert.AreEqual("inactive", engine.GetSlotState("stances", 1, "Warblade"));
            Assert.AreEqual("active", engine.GetSlotState("stances", 2, "Warblade"));
        }

        // Source: Tome of Battle, p. 23
        [TestMethod]
        public void AdoptStance_WithExplicitReplacementTarget_ReplacesSpecifiedStance_WhenAtCapacity() {
            var stance1 = CreateManeuver("Punishing Stance", "Iron Heart", 1, "Stance");
            var stance2 = CreateManeuver("Blood in the Water", "Tiger Claw", 1, "Stance");
            var stance3 = CreateManeuver("Hunter's Sense", "Tiger Claw", 1, "Stance");
            var content = new FakeRulesCatalog(stance1, stance2, stance3);

            var context = new FakeCharacterContext(catalog: content);
            SetStat(context.Effects, "stances known:warblade", 3);
            SetStat(context.Effects, "active stances", 2);
            var engine = CreateMartialEngineWithContext(content, context);

            engine.AssignSlot("stances", 0, "Punishing Stance", "inactive", "Warblade");
            engine.AssignSlot("stances", 1, "Blood in the Water", "inactive", "Warblade");
            engine.AssignSlot("stances", 2, "Hunter's Sense", "inactive", "Warblade");

            // Adopt stances 0 and 1
            engine.InvokeVerb("adopt_stance", "stances", 0, source: "Warblade");
            engine.InvokeVerb("adopt_stance", "stances", 1, source: "Warblade");
            Assert.AreEqual("active", engine.GetSlotState("stances", 0, "Warblade"));
            Assert.AreEqual("active", engine.GetSlotState("stances", 1, "Warblade"));

            // Adopt stance 2, explicitly choosing to replace stance 1 (rather than oldest stance 0)
            engine.ExecuteVerb("adopt_stance", context, "stances", 2, null, "Warblade", new List<int> { 1 });
            Assert.AreEqual("active", engine.GetSlotState("stances", 0, "Warblade"), "Stance 0 should remain active");
            Assert.AreEqual("inactive", engine.GetSlotState("stances", 1, "Warblade"), "Stance 1 was explicitly replaced");
            Assert.AreEqual("active", engine.GetSlotState("stances", 2, "Warblade"), "Stance 2 should now be active");
        }

        // Source: Tome of Battle, p. 23
        [TestMethod]
        public void AdoptStances_AdoptsBothStancesSimultaneously_ClearingPrevious() {
            var stance1 = CreateManeuver("Punishing Stance", "Iron Heart", 1, "Stance");
            var stance2 = CreateManeuver("Blood in the Water", "Tiger Claw", 1, "Stance");
            var stance3 = CreateManeuver("Hunter's Sense", "Tiger Claw", 1, "Stance");
            var content = new FakeRulesCatalog(stance1, stance2, stance3);

            var context = new FakeCharacterContext(catalog: content);
            SetStat(context.Effects, "stances known:warblade", 3);
            SetStat(context.Effects, "active stances", 2);
            var engine = CreateMartialEngineWithContext(content, context);

            engine.AssignSlot("stances", 0, "Punishing Stance", "inactive", "Warblade");
            engine.AssignSlot("stances", 1, "Blood in the Water", "inactive", "Warblade");
            engine.AssignSlot("stances", 2, "Hunter's Sense", "inactive", "Warblade");

            // Start in stance 2
            engine.InvokeVerb("adopt_stance", "stances", 2, source: "Warblade");
            Assert.AreEqual("active", engine.GetSlotState("stances", 2, "Warblade"));

            // Change both stances simultaneously in a single swift action
            engine.ExecuteVerb("adopt_stance", context, "stances", null, null, "Warblade", new List<int> { 0, 1 });
            Assert.AreEqual("active", engine.GetSlotState("stances", 0, "Warblade"));
            Assert.AreEqual("active", engine.GetSlotState("stances", 1, "Warblade"));
            Assert.AreEqual("inactive", engine.GetSlotState("stances", 2, "Warblade"));
        }

        // Source: ADR-0001
        [TestMethod]
        public void Validate_WarnsWhenActiveStancesExceedsCapacity() {
            var stance1 = CreateManeuver("Punishing Stance", "Iron Heart", 1, "Stance");
            var content = new FakeRulesCatalog(stance1);

            var context = new FakeCharacterContext(catalog: content);
            SetStat(context.Effects, "stances known:warblade", 2);
            SetStat(context.Effects, "active stances", 1);
            var engine = CreateMartialEngineWithContext(content, context);

            engine.AssignSlot("stances", 0, "Punishing Stance", "active", "Warblade");
            engine.AssignSlot("stances", 1, "Blood in the Water", "active", "Warblade");

            // Two active stances with active stances capacity of 1
            var warnings = engine.Validate(context);
            Assert.IsTrue(warnings.Any(w => w.Contains("Active stances count (2) exceeds active stances capacity (1)")));

            var maneuverWarnings = engine.ValidateChoice(null, null, "Punishing Stance", "Warblade");
            Assert.IsTrue(maneuverWarnings.Any(w => w.Contains("Active stances count (2) exceeds active stances capacity (1)")));
        }

        // Source: Tome of Battle, pp. 5, 38, 43
        [TestMethod]
        public void ContributeEffects_ProjectsActiveStanceModifiers_AndIgnoresInactiveStances() {
            var stanceEffect = new Effect("Punishing Stance Effect")
                .AddModifier("AC", new Modifier(-2, ModifierTypes.UNTYPED));
            var stance = new SubsystemElement {
                Name = "Punishing Stance",
                Type = "Stance"
            };
            stance.SetProperty("discipline", "Iron Heart");
            stance.SetProperty("level", 1);
            stance.AddEffect("benefits", stanceEffect);
            var content = new FakeRulesCatalog(stance);

            var context = new FakeCharacterContext(catalog: content);
            SetStat(context.Effects, "stances known:warblade", 1);
            SetStat(context.Effects, "AC", 15);
            var engine = CreateMartialEngineWithContext(content, context);

            engine.AssignSlot("stances", 0, "Punishing Stance", "inactive", "Warblade");

            // While inactive, ContributeEffects does not add stance modifier
            var pool = new EffectPool(context.Character);
            SetStat(pool, "AC", 15);
            engine.ContributeEffects(pool, context);
            Assert.AreEqual(15, pool.GetStatValue("AC", null));

            // When adopted / active, ContributeEffects applies the -2 penalty
            engine.InvokeVerb("adopt_stance", "stances", 0, source: "Warblade");
            var activePool = new EffectPool(context.Character);
            SetStat(activePool, "AC", 15);
            engine.ContributeEffects(activePool, context);
            Assert.AreEqual(13, activePool.GetStatValue("AC", null));
        }

        // Source: Tome of Battle, pp. 42-45
        [TestMethod]
        public void ExpendVerb_SchedulesSheetActionForBoostAndCounter_AndExpiresOnTimelineAdvance() {
            var boostEffect = new Effect("Wind Stride Effect")
                .AddModifier("Speed", new Modifier(10, ModifierTypes.ENHANCEMENT));
            var boost = new SubsystemElement {
                Name = "Wind Stride",
                Type = "Boost"
            };
            boost.SetProperty("discipline", "Desert Wind");
            boost.SetProperty("level", 1);
            boost.AddEffect("benefits", boostEffect);
            boost.Verbs["execute"] = new List<SubsystemStep> {
                new SubsystemStep {
                    Type = "action",
                    Action = new SheetAction { Name = "Wind Stride" }.Duration("1 round")
                }
            };
            var counter = CreateManeuver("Fire Riposte", "Desert Wind", 2, "Counter");
            counter.Verbs["execute"] = new List<SubsystemStep> {
                new SubsystemStep {
                    Type = "action",
                    Action = new SheetAction { Name = "Fire Riposte" }.Duration("1 round")
                }
            };
            var strike = CreateManeuver("Steel Strike", "Iron Heart", 1, "Strike");
            var content = new FakeRulesCatalog(boost, counter, strike);

            var character = new Character();
            var timeline = new Timeline();
            character.Timeline = timeline;
            var context = new FakeCharacterContext(character, catalog: content);
            SetStat(context.Effects, "maneuvers known:warblade", 3);
            var engine = CreateMartialEngineWithContext(content, context);

            engine.AssignSlot("readied", 0, "Wind Stride", "available", "Warblade");
            engine.AssignSlot("readied", 1, "Fire Riposte", "available", "Warblade");
            engine.AssignSlot("readied", 2, "Steel Strike", "available", "Warblade");

            // 1. Strike transitions to Expended without scheduling timeline action
            var strikeReq = engine.InvokeVerb("expend", "readied", 2, source: "Warblade");
            Assert.AreNotEqual(TimelineRefreshRequirement.None, strikeReq);
            Assert.AreEqual("expended", engine.GetSlotState("readied", 2, "Warblade"));
            Assert.AreEqual(0, timeline.ScheduledActions.Count);

            // 2. Boost transitions to Expended and schedules SheetAction on Timeline
            var boostReq = engine.InvokeVerb("expend", "readied", 0, source: "Warblade");
            Assert.AreNotEqual(TimelineRefreshRequirement.None, boostReq);
            Assert.AreEqual("expended", engine.GetSlotState("readied", 0, "Warblade"));
            Assert.AreEqual(1, timeline.ScheduledActions.Count);
            Assert.IsTrue(timeline.ScheduledActions.ContainsKey(1));
            var scheduledAction = timeline.ScheduledActions[1][0];
            Assert.IsNotNull(scheduledAction);
            Assert.AreEqual("Wind Stride", scheduledAction.Name);

            // 3. ContributeEffects projects temporary boost effect into EffectPool
            var pool = new EffectPool(character);
            SetStat(pool, "Speed", 30);
            engine.ContributeEffects(pool, context);
            Assert.AreEqual(40, pool.GetStatValue("Speed", null));

            // 4. Counter transitions to Expended and schedules SheetAction
            var counterReq = engine.InvokeVerb("expend", "readied", 1, source: "Warblade");
            Assert.AreNotEqual(TimelineRefreshRequirement.None, counterReq);
            Assert.AreEqual("expended", engine.GetSlotState("readied", 1, "Warblade"));
            Assert.AreEqual(2, timeline.ScheduledActions[1].Count);

            // 5. Timeline advances 1 round: actions expire, returning refresh requirement, removed from ScheduledActions
            var advanceResult = timeline.Advance(1, context);
            Assert.IsTrue(advanceResult.RefreshRequirement >= TimelineRefreshRequirement.RecomputeDerived);
            Assert.AreEqual(0, timeline.ScheduledActions.Count);
            Assert.AreEqual(2, advanceResult.ExpiredActions.Count);

            // 6. ContributeEffects after expiration no longer applies boost effect
            var postExpirationPool = new EffectPool(character);
            SetStat(postExpirationPool, "Speed", 30);
            engine.ContributeEffects(postExpirationPool, context);
            Assert.AreEqual(30, postExpirationPool.GetStatValue("Speed", null));

            // 7. Maneuver slot remains Expended (does not un-expend)
            Assert.AreEqual("expended", engine.GetSlotState("readied", 0, "Warblade"));
            Assert.AreEqual("expended", engine.GetSlotState("readied", 1, "Warblade"));
        }

        // Source: Tome of Battle, p. 38
        [TestMethod]
        public void BoostDurationSheetAction_SerializesAndRoundTripsViaFromJson() {
            var action = new SheetAction { Name = "Wind Stride" }.Duration("1 round");
            var json = (JObject)action.ToJson();

            Assert.AreEqual("Wind Stride", (string)json["name"]);
            Assert.AreEqual("1 round", (string)json["duration"]);

            var deserialized = SheetAction.FromJson(json);
            Assert.IsNotNull(deserialized);
            Assert.AreEqual("Wind Stride", deserialized.Name);
            Assert.IsTrue(action.Matches(deserialized));
        }

        // Source: Tome of Battle, pp. 38, 43, 56, 66, 73, 85, 90
        [TestMethod]
        public void SubsystemEngine_FullCatalogStanceActivation_ModifiesStatsAcrossClasses() {
            var content = new GameRulesCatalog();
            var context = new FakeCharacterContext(catalog: content);
            var engine = new SubsystemEngine(content.GetSubsystem("martial_maneuvers"));

            SetStat(context.Effects, "stances known:warblade", 2);
            SetStat(context.Effects, "stances known:swordsage", 1);
            SetStat(context.Effects, "stances known:crusader", 1);
            SetStat(context.Effects, "AC", 15);
            SetStat(context.Effects, "Land speed", 30);
            SetStat(context.Effects, "Listen", 4);
            SetStat(context.Effects, "Will saves", 3);

            engine.Initialize(context);

            engine.AssignSlot("stances", 0, "Punishing Stance", "inactive", "Warblade");
            engine.AssignSlot("stances", 1, "Absolute Steel", "inactive", "Warblade");

            engine.AssignSlot("stances", 0, "Hearing the Air", "inactive", "Swordsage");

            engine.AssignSlot("stances", 0, "Bolstering Voice", "inactive", "Crusader");

            // While stances are inactive, base stats are unchanged
            var baselinePool = new EffectPool(context.Character);
            SetStat(baselinePool, "AC", 15);
            SetStat(baselinePool, "Land speed", 30);
            SetStat(baselinePool, "Listen", 4);
            SetStat(baselinePool, "Will saves", 3);
            engine.ContributeEffects(baselinePool, context);
            Assert.AreEqual(15, baselinePool.GetStatValue("AC", null));
            Assert.AreEqual(30, baselinePool.GetStatValue("Land speed", null));
            Assert.AreEqual(4, baselinePool.GetStatValue("Listen", null));
            Assert.AreEqual(3, baselinePool.GetStatValue("Will saves", null));

            // Warblade activates Punishing Stance (-2 AC)
            engine.InvokeVerb("adopt_stance", "stances", 0, source: "Warblade");
            var wbPool = new EffectPool(context.Character);
            SetStat(wbPool, "AC", 15);
            SetStat(wbPool, "Land speed", 30);
            engine.ContributeEffects(wbPool, context);
            Assert.AreEqual(13, wbPool.GetStatValue("AC", null));

            // Warblade switches to Absolute Steel (+10 Land speed)
            engine.InvokeVerb("adopt_stance", "stances", 1, source: "Warblade");
            var wbSpeedPool = new EffectPool(context.Character);
            SetStat(wbSpeedPool, "AC", 15);
            SetStat(wbSpeedPool, "Land speed", 30);
            engine.ContributeEffects(wbSpeedPool, context);
            Assert.AreEqual(15, wbSpeedPool.GetStatValue("AC", null));
            Assert.AreEqual(40, wbSpeedPool.GetStatValue("Land speed", null));

            // Swordsage activates Hearing the Air (+5 Listen)
            engine.InvokeVerb("adopt_stance", "stances", 0, source: "Swordsage");
            var ssPool = new EffectPool(context.Character);
            SetStat(ssPool, "Listen", 4);
            engine.ContributeEffects(ssPool, context);
            Assert.AreEqual(9, ssPool.GetStatValue("Listen", null));

            // Crusader activates Bolstering Voice (+2 Will saves)
            engine.InvokeVerb("adopt_stance", "stances", 0, source: "Crusader");
            var crPool = new EffectPool(context.Character);
            SetStat(crPool, "Will saves", 3);
            engine.ContributeEffects(crPool, context);
            Assert.AreEqual(5, crPool.GetStatValue("Will saves", null));
        }

        // Source: Tome of Battle, pp. 38–40
        [TestMethod]
        public void SubsystemEngine_FullCatalogRecovery_OperatesAcrossCrusaderSwordsageAndWarblade() {
            var content = new GameRulesCatalog();
            var context = new FakeCharacterContext(catalog: content);
            var engine = new SubsystemEngine(content.GetSubsystem("martial_maneuvers"));

            SetStat(context.Effects, "maneuvers known:warblade", 2);
            SetStat(context.Effects, "maneuvers readied:warblade", 2);
            SetStat(context.Effects, "maneuvers known:swordsage", 2);
            SetStat(context.Effects, "maneuvers readied:swordsage", 2);
            SetStat(context.Effects, "maneuvers known:crusader", 3);
            SetStat(context.Effects, "maneuvers readied:crusader", 3);
            SetStat(context.Effects, "maneuvers granted:crusader", 2);

            engine.Initialize(context);

            // 1. Warblade: initiate and recover via swift flourish
            engine.AssignSlot("readied", 0, "Steel Wind", "available", "Warblade");
            engine.AssignSlot("readied", 1, "Iron Heart Surge", "available", "Warblade");

            engine.InvokeVerb("expend", "readied", 0, source: "Warblade");
            Assert.AreEqual("expended", engine.GetSlotState("readied", 0, "Warblade"));
            engine.InvokeVerb("recover", "readied", source: "Warblade");
            Assert.AreEqual("available", engine.GetSlotState("readied", 0, "Warblade"));

            // 2. Swordsage: initiate and recover via targeted full-round meditation
            engine.AssignSlot("readied", 0, "Fire Riposte", "available", "Swordsage");
            engine.AssignSlot("readied", 1, "Burning Blade", "available", "Swordsage");

            engine.InvokeVerb("expend", "readied", 0, source: "Swordsage");
            Assert.AreEqual("expended", engine.GetSlotState("readied", 0, "Swordsage"));
            engine.InvokeVerb("recover", "readied", 0, source: "Swordsage");
            Assert.AreEqual("available", engine.GetSlotState("readied", 0, "Swordsage"));

            // 3. Crusader: deal deck, initiate available maneuver, and advance turn to draw withheld
            engine.AssignSlot("readied", 0, "Crusader's Strike", "available", "Crusader");
            engine.AssignSlot("readied", 1, "Shield Block", "available", "Crusader");
            engine.AssignSlot("readied", 2, "Revitalizing Strike", "withheld", "Crusader");

            engine.InvokeVerb("expend", "readied", 0, source: "Crusader");
            Assert.AreEqual("expended", engine.GetSlotState("readied", 0, "Crusader"));
            engine.SetRandom(new Random(42));
            engine.InvokeVerb("advance_turn", "readied", source: "Crusader");
            Assert.AreEqual("available", engine.GetSlotState("readied", 2, "Crusader"));
        }

        [TestMethod]
        public void AssignSlot_AndClearSlot_Readied_MutateSlotsCorrectly() {
            var strike = CreateManeuver("Steel Wind", "Iron Heart", 1, "Strike");
            var content = new FakeRulesCatalog(strike);
            var context = new FakeCharacterContext(catalog: content);
            SetStat(context.Effects, "maneuvers known:warblade", 2);
            var engine = CreateMartialEngineWithContext(content, context);

            engine.AssignSlot("readied", 0, "Steel Wind", source: "Warblade");
            Assert.AreEqual("Steel Wind", engine.GetSlotElement("readied", 0, "Warblade"));

            engine.ClearSlot("readied", 0, "Warblade");
            Assert.IsNull(engine.GetSlotElement("readied", 0, "Warblade"));
        }

        [TestMethod]
        public void AssignSlot_AndClearSlot_Stances_MutateSlotsCorrectly() {
            var stance = CreateManeuver("Punishing Stance", "Iron Heart", 1, "Stance");
            var content = new FakeRulesCatalog(stance);
            var context = new FakeCharacterContext(catalog: content);
            SetStat(context.Effects, "stances known:warblade", 2);
            var engine = CreateMartialEngineWithContext(content, context);

            engine.AssignSlot("stances", 0, "Punishing Stance", source: "Warblade");
            Assert.AreEqual("Punishing Stance", engine.GetSlotElement("stances", 0, "Warblade"));

            engine.ClearSlot("stances", 0, "Warblade");
            Assert.IsNull(engine.GetSlotElement("stances", 0, "Warblade"));
        }
    }
}
