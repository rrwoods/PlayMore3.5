using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Subsystems;
using PlayMore35.Subsystems.Maneuvers;
using static PlayMore35.Effects.Constants;

namespace PlayMore35.Tests {
    [TestClass]
    public class MartialManeuverEngineTests {

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

        private static Maneuver CreateManeuver(string name, string discipline, int level, ManeuverType type = ManeuverType.Strike, int prereqCount = 0) {
            return new Maneuver {
                Name = name,
                Discipline = discipline,
                Level = level,
                Type = type,
                PrerequisiteCount = prereqCount
            };
        }

        private static void SetStat(EffectPool pool, string statName, int value) {
            if (!pool.stats.ContainsKey(statName)) {
                pool.AddStat(new Statistic(statName));
            }
            pool.AddEffect(new Effect($"Base {statName}").AddModifier(statName, new Modifier(value, UNTYPED)));
        }

        // Source: Tome of Battle, p. 38
        [TestMethod]
        public void ManeuverState_DefinesFourCanonicalStates() {
            var names = Enum.GetNames(typeof(ManeuverState));
            CollectionAssert.AreEquivalent(new[] { "Unreadied", "Withheld", "Available", "Expended" }, names);
        }

        // Source: Tome of Battle, p. 38
        [TestMethod]
        public void KnownManeuverSlot_ComputesIsReadied_BasedOnState() {
            var unreadied = new KnownManeuverSlot("Steel Strike", ManeuverState.Unreadied);
            Assert.IsFalse(unreadied.IsReadied);

            var available = new KnownManeuverSlot("Steel Strike", ManeuverState.Available);
            Assert.IsTrue(available.IsReadied);

            var expended = new KnownManeuverSlot("Steel Strike", ManeuverState.Expended);
            Assert.IsTrue(expended.IsReadied);

            var withheld = new KnownManeuverSlot("Steel Strike", ManeuverState.Withheld);
            Assert.IsTrue(withheld.IsReadied);
        }

        // Source: Tome of Battle, p. 38
        [TestMethod]
        public void KnownManeuverSlot_SerializesAndDeserializes_Cleanly() {
            var slot = new KnownManeuverSlot("Iron Heart Surge", ManeuverState.Available);
            var json = (JObject)slot.ToJson();

            Assert.AreEqual("Iron Heart Surge", (string)json["name"]);
            Assert.AreEqual("Available", (string)json["state"]);

            var restored = new KnownManeuverSlot(json);
            Assert.AreEqual("Iron Heart Surge", restored.ManeuverName);
            Assert.AreEqual(ManeuverState.Available, restored.State);
            Assert.IsTrue(restored.IsReadied);
        }

        // Source: Tome of Battle, p. 38
        [TestMethod]
        public void StanceSlot_SerializesAndDeserializes_Cleanly() {
            var slot = new StanceSlot("Punishing Stance", true);
            var json = (JObject)slot.ToJson();

            Assert.AreEqual("Punishing Stance", (string)json["name"]);
            Assert.AreEqual(true, (bool)json["isActive"]);

            var restored = new StanceSlot(json);
            Assert.AreEqual("Punishing Stance", restored.StanceName);
            Assert.IsTrue(restored.IsActive);
        }

        // Source: Tome of Battle, pp. 38, 40
        [TestMethod]
        public void ManeuverSourceState_ManagesKnownAndStanceSlots_AndTracksReadied() {
            var source = new ManeuverSourceState(3, 2);
            source.SetKnownSlot(0, new KnownManeuverSlot("Steel Strike", ManeuverState.Available));
            source.SetKnownSlot(1, new KnownManeuverSlot("Disarming Strike", ManeuverState.Expended));
            source.SetKnownSlot(2, new KnownManeuverSlot("Iron Heart Surge", ManeuverState.Unreadied));
            source.SetStanceSlot(0, new StanceSlot("Punishing Stance", true));

            Assert.AreEqual(3, source.Known.Capacity);
            Assert.AreEqual(2, source.Stances.Capacity);
            Assert.AreEqual(2, source.ReadiedCount);
            Assert.AreEqual(1, source.ActiveStances.Count());

            // JSON round-trip
            var json = (JObject)source.ToJson();
            var restored = new ManeuverSourceState(json);

            Assert.AreEqual(3, restored.Known.Capacity);
            Assert.AreEqual(2, restored.ReadiedCount);
            Assert.AreEqual("Steel Strike", restored.GetKnownSlot(0).ManeuverName);
            Assert.AreEqual(ManeuverState.Available, restored.GetKnownSlot(0).State);
            Assert.AreEqual("Disarming Strike", restored.GetKnownSlot(1).ManeuverName);
            Assert.AreEqual(ManeuverState.Expended, restored.GetKnownSlot(1).State);
            Assert.AreEqual("Iron Heart Surge", restored.GetKnownSlot(2).ManeuverName);
            Assert.AreEqual(ManeuverState.Unreadied, restored.GetKnownSlot(2).State);
            Assert.AreEqual("Punishing Stance", restored.GetStanceSlot(0).StanceName);
            Assert.IsTrue(restored.GetStanceSlot(0).IsActive);
        }

        // Source: Tome of Battle, p. 40
        [TestMethod]
        public void MartialManeuverState_PreservesSourceIsolation_AndRoundTripsJson() {
            var state = new MartialManeuverState();
            var warblade = state.GetOrCreateSource("Warblade");
            warblade.SetKnownSlot(0, new KnownManeuverSlot("Steel Strike", ManeuverState.Available));

            var crusader = state.GetOrCreateSource("Crusader");
            crusader.SetKnownSlot(0, new KnownManeuverSlot("Crusader's Strike", ManeuverState.Available));

            var json = (JObject)state.ToJson();
            var restored = new MartialManeuverState(json);

            Assert.IsTrue(restored.Sources.ContainsKey("Warblade"));
            Assert.IsTrue(restored.Sources.ContainsKey("Crusader"));
            Assert.AreEqual("Steel Strike", restored.Sources["Warblade"].GetKnownSlot(0).ManeuverName);
            Assert.AreEqual("Crusader's Strike", restored.Sources["Crusader"].GetKnownSlot(0).ManeuverName);
            Assert.AreNotEqual(restored.Sources["Warblade"].GetKnownSlot(0).ManeuverName,
                               restored.Sources["Crusader"].GetKnownSlot(0).ManeuverName);
        }

        // Source: Tome of Battle, pp. 38, 39
        [TestMethod]
        public void MartialManeuverEngine_IsActive_EvaluatesAgainstSourceStats() {
            var engine = new MartialManeuverEngine();
            var context = new FakeCharacterContext();
            engine.Initialize(context);

            Assert.IsFalse(engine.IsActive);

            // Set warblade maneuvers known = 3
            SetStat(context.Effects, "maneuvers known:warblade", 3);
            Assert.IsTrue(engine.IsActive);

            // Reset context
            var inactiveContext = new FakeCharacterContext();
            engine.Initialize(inactiveContext);
            Assert.IsFalse(engine.IsActive);

            // Set initiator level:crusader = 1
            SetStat(inactiveContext.Effects, "initiator level:crusader", 1);
            Assert.IsTrue(engine.IsActive);
        }

        // Source: Tome of Battle, p. 39
        [TestMethod]
        public void MartialManeuverEngine_WiresCapacityAndLevelGating_ToCharacterStats() {
            var m1 = CreateManeuver("Steel Strike", "Iron Heart", 1);
            var m2 = CreateManeuver("Iron Heart Surge", "Iron Heart", 3);
            var stance = CreateManeuver("Punishing Stance", "Iron Heart", 1, ManeuverType.Stance);
            var content = new TestManeuverContentProvider(m1, m2, stance);

            var engine = new MartialManeuverEngine(content);
            var context = new FakeCharacterContext();
            SetStat(context.Effects, "maneuvers known:warblade", 3);
            SetStat(context.Effects, "max maneuver level:warblade", 2);
            SetStat(context.Effects, "stances known:warblade", 1);

            engine.Initialize(context);
            var warblade = engine.GetSource("Warblade");

            Assert.AreEqual(3, warblade.Known.Capacity);
            Assert.AreEqual(2, warblade.Known.MaxLevel);
            Assert.AreEqual(1, warblade.Stances.Capacity);
            Assert.AreEqual(2, warblade.Stances.MaxLevel);

            // Slot 0 with level 1 maneuver is valid
            warblade.SetKnownSlot(0, new KnownManeuverSlot("Steel Strike"));
            Assert.IsTrue(warblade.Known.IsSlotLevelValid(0));

            // Slot 1 with level 3 maneuver exceeds max level 2 (ADR-0001: accepts but reports invalid)
            warblade.SetKnownSlot(1, new KnownManeuverSlot("Iron Heart Surge"));
            Assert.IsFalse(warblade.Known.IsSlotLevelValid(1));
        }

        // Source: Tome of Battle, p. 39
        [TestMethod]
        public void ValidateManeuver_WarnsWhenLevelExceedsMaxManeuverLevel() {
            var m3 = CreateManeuver("Iron Heart Surge", "Iron Heart", 3);
            var content = new TestManeuverContentProvider(m3);

            var engine = new MartialManeuverEngine(content);
            var context = new FakeCharacterContext();
            SetStat(context.Effects, "max maneuver level:warblade", 2);
            engine.Initialize(context);

            var warnings = engine.ValidateManeuver("Iron Heart Surge", "Warblade");
            Assert.IsTrue(warnings.Any(w => w.Contains("exceeds maximum maneuver level 2")));
        }

        // Source: Tome of Battle, pp. 38, 43-44
        [TestMethod]
        public void ValidateManeuver_WarnsWhenDisciplinePrerequisitesUnmet() {
            var m1 = CreateManeuver("Steel Strike", "Iron Heart", 1);
            var m2 = CreateManeuver("Disarming Strike", "Iron Heart", 2, ManeuverType.Strike, prereqCount: 1);
            var content = new TestManeuverContentProvider(m1, m2);

            var engine = new MartialManeuverEngine(content);
            var context = new FakeCharacterContext();
            SetStat(context.Effects, "maneuvers known:warblade", 3);
            SetStat(context.Effects, "max maneuver level:warblade", 5);
            engine.Initialize(context);

            // Initially no Iron Heart maneuvers known -> warning
            var warnings = engine.ValidateManeuver("Disarming Strike", "Warblade");
            Assert.IsTrue(warnings.Any(w => w.Contains("Requires 1 Iron Heart maneuver, but only 0 known")));

            // Learn Steel Strike -> prerequisite satisfied
            var warblade = engine.GetSource("Warblade");
            warblade.SetKnownSlot(0, new KnownManeuverSlot("Steel Strike"));

            var satisfiedWarnings = engine.ValidateManeuver("Disarming Strike", "Warblade");
            Assert.IsFalse(satisfiedWarnings.Any(w => w.Contains("Requires 1 Iron Heart")));
        }

        // Source: Tome of Battle, pp. 43-44
        [TestMethod]
        public void ValidateManeuver_StancesCountTowardDisciplinePrerequisites() {
            var stance = CreateManeuver("Punishing Stance", "Iron Heart", 1, ManeuverType.Stance);
            var m2 = CreateManeuver("Disarming Strike", "Iron Heart", 2, ManeuverType.Strike, prereqCount: 1);
            var content = new TestManeuverContentProvider(stance, m2);

            var engine = new MartialManeuverEngine(content);
            var context = new FakeCharacterContext();
            SetStat(context.Effects, "maneuvers known:warblade", 2);
            SetStat(context.Effects, "stances known:warblade", 1);
            SetStat(context.Effects, "max maneuver level:warblade", 5);
            engine.Initialize(context);

            // Knowing Punishing Stance fulfills the 1 Iron Heart maneuver requirement
            var warblade = engine.GetSource("Warblade");
            warblade.SetStanceSlot(0, new StanceSlot("Punishing Stance"));

            var warnings = engine.ValidateManeuver("Disarming Strike", "Warblade");
            Assert.IsFalse(warnings.Any(w => w.Contains("Requires 1 Iron Heart")));
        }

        // Source: Tome of Battle, p. 40
        [TestMethod]
        public void ValidateManeuver_WarnsWhenLearnedUnderAnotherSource() {
            var m1 = CreateManeuver("Steel Strike", "Iron Heart", 1);
            var content = new TestManeuverContentProvider(m1);

            var engine = new MartialManeuverEngine(content);
            var context = new FakeCharacterContext();
            SetStat(context.Effects, "maneuvers known:warblade", 2);
            SetStat(context.Effects, "maneuvers known:crusader", 2);
            engine.Initialize(context);

            var warblade = engine.GetSource("Warblade");
            warblade.SetKnownSlot(0, new KnownManeuverSlot("Steel Strike"));

            // Attempting to learn Steel Strike under Crusader produces a cross-class duplicate warning
            var warnings = engine.ValidateManeuver("Steel Strike", "Crusader");
            Assert.IsTrue(warnings.Any(w => w.Contains("already learned under source 'Warblade'")));
        }

        // Source: Tome of Battle, p. 38
        [TestMethod]
        public void ValidateManeuver_WarnsWhenReadiedCountExceedsCapacity() {
            var m1 = CreateManeuver("Steel Strike", "Iron Heart", 1);
            var m2 = CreateManeuver("Steely Strike", "Iron Heart", 1);
            var content = new TestManeuverContentProvider(m1, m2);

            var engine = new MartialManeuverEngine(content);
            var context = new FakeCharacterContext();
            SetStat(context.Effects, "maneuvers known:warblade", 2);
            SetStat(context.Effects, "maneuvers readied:warblade", 1);
            engine.Initialize(context);

            var warblade = engine.GetSource("Warblade");
            warblade.SetKnownSlot(0, new KnownManeuverSlot("Steel Strike", ManeuverState.Available));
            warblade.SetKnownSlot(1, new KnownManeuverSlot("Steely Strike", ManeuverState.Available));

            // Total readied is 2, capacity is 1
            var warnings = engine.ValidateManeuver("Steel Strike", "Warblade");
            Assert.IsTrue(warnings.Any(w => w.Contains("Readied maneuvers count (2) exceeds readied capacity (1)")));
        }

        // Source: Tome of Battle, p. 38
        [TestMethod]
        public void Character_Subsystems_DeserializesMartialManeuverState() {
            var character = new Character();
            character.name = "Test Martial Adept";
            var mState = new MartialManeuverState();
            var warblade = mState.GetOrCreateSource("Warblade");
            warblade.SetKnownSlot(0, new KnownManeuverSlot("Steel Strike", ManeuverState.Available));
            warblade.SetStanceSlot(0, new StanceSlot("Punishing Stance", true));
            character.Subsystems["martial_maneuvers"] = mState;

            // Serialize to JSON
            var json = (JObject)character.ToJson();
            Assert.IsNotNull(json["subsystems"]?["martial_maneuvers"]);

            // Restore from JSON
            var restored = new Character(json);
            Assert.IsTrue(restored.Subsystems.ContainsKey("martial_maneuvers"));
            Assert.IsInstanceOfType(restored.Subsystems["martial_maneuvers"], typeof(MartialManeuverState));

            var restoredState = (MartialManeuverState)restored.Subsystems["martial_maneuvers"];
            Assert.IsTrue(restoredState.Sources.ContainsKey("Warblade"));
            Assert.AreEqual("Steel Strike", restoredState.Sources["Warblade"].GetKnownSlot(0).ManeuverName);
            Assert.AreEqual(ManeuverState.Available, restoredState.Sources["Warblade"].GetKnownSlot(0).State);
            Assert.AreEqual("Punishing Stance", restoredState.Sources["Warblade"].GetStanceSlot(0).StanceName);
            Assert.IsTrue(restoredState.Sources["Warblade"].GetStanceSlot(0).IsActive);
        }
    }
}
