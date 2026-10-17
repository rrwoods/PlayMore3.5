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

        [TestMethod]
        public void ManeuverSourceState_HelperMethods_ReturnManeuversAndStances() {
            var src = new ManeuverSourceState(2, 2);
            src.SetKnownSlot(0, new KnownManeuverSlot("Steel Strike", ManeuverState.Available));
            src.SetKnownSlot(1, new KnownManeuverSlot("Iron Heart Surge", ManeuverState.Unreadied));
            src.SetStanceSlot(0, new StanceSlot("Punishing Stance", true));

            Assert.IsTrue(src.HasManeuver("Steel Strike"));
            Assert.IsTrue(src.HasManeuver("Iron Heart Surge"));
            Assert.IsFalse(src.HasManeuver("Nonexistent"));
            Assert.IsTrue(src.HasStance("Punishing Stance"));
            Assert.IsFalse(src.HasStance("Nonexistent"));

            var allKnown = src.AllKnownManeuverNames().ToList();
            Assert.AreEqual(2, allKnown.Count);
            Assert.IsTrue(allKnown.Contains("Steel Strike"));
            Assert.IsTrue(allKnown.Contains("Iron Heart Surge"));

            var allStances = src.AllStanceNames().ToList();
            Assert.AreEqual(1, allStances.Count);
            Assert.IsTrue(allStances.Contains("Punishing Stance"));
        }

        [TestMethod]
        public void Subsystems_Maneuvers_RejectsNonCanonicalJsonPropertiesWithoutFallback() {
            // Maneuver: actionType is canonical, action is non-canonical fallback
            var badManeuverJson = new JObject(
                new JProperty("name", "Test Strike"),
                new JProperty("action", "standard")
            );
            var maneuver = new Maneuver(badManeuverJson);
            Assert.AreEqual("", maneuver.ActionType);

            var canonicalManeuverJson = new JObject(
                new JProperty("name", "Test Strike"),
                new JProperty("actionType", "standard")
            );
            var canManeuver = new Maneuver(canonicalManeuverJson);
            Assert.AreEqual("standard", canManeuver.ActionType);

            // KnownManeuverSlot: name is canonical, maneuverName is non-canonical fallback
            var badSlotJson = new JObject(new JProperty("maneuverName", "Steel Strike"));
            var slot = new KnownManeuverSlot(badSlotJson);
            Assert.AreEqual("", slot.ManeuverName);

            // StanceSlot: name & isActive are canonical, stanceName & active are non-canonical
            var badStanceJson = new JObject(
                new JProperty("stanceName", "Punishing Stance"),
                new JProperty("active", true)
            );
            var stance = new StanceSlot(badStanceJson);
            Assert.AreEqual("", stance.StanceName);
            Assert.IsFalse(stance.IsActive);

            // ManeuverSourceState: known is canonical, maneuvers is non-canonical fallback
            var badSourceJson = new JObject(
                new JProperty("maneuvers", new JArray(new JObject(new JProperty("name", "Steel Strike"))))
            );
            var source = new ManeuverSourceState(badSourceJson);
            Assert.AreEqual(0, source.Known.Capacity);
            Assert.AreEqual(0, source.AllKnownManeuverNames().Count());

            // ManeuverSheetAction: maneuver_action is canonical, maneuver is non-canonical fallback
            var badActionJson = new JObject(
                new JProperty("maneuver", "Steel Strike"),
                new JProperty("source", "Warblade")
            );
            Assert.IsNull(ManeuverSheetAction.TryParse(badActionJson));
            var badAction = new ManeuverSheetAction(badActionJson);
            Assert.AreEqual("", badAction.ManeuverName);

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
        public void ValidateManeuver_WarnsWhenReadiedCountBelowCapacity() {
            var m1 = CreateManeuver("Steel Strike", "Iron Heart", 1);
            var m2 = CreateManeuver("Steely Strike", "Iron Heart", 1);
            var content = new TestManeuverContentProvider(m1, m2);

            var engine = new MartialManeuverEngine(content);
            var context = new FakeCharacterContext();
            SetStat(context.Effects, "maneuvers known:warblade", 2);
            SetStat(context.Effects, "maneuvers readied:warblade", 2);
            engine.Initialize(context);

            var warblade = engine.GetSource("Warblade");
            warblade.SetKnownSlot(0, new KnownManeuverSlot("Steel Strike", ManeuverState.Available));
            warblade.SetKnownSlot(1, new KnownManeuverSlot("Steely Strike", ManeuverState.Unreadied));

            // Total readied is 1, capacity is 2
            var warnings = engine.ValidateManeuver("Steel Strike", "Warblade");
            Assert.IsTrue(warnings.Any(w => w.Contains("Readied maneuvers count (1) is below readied capacity (2) for Warblade.")));
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

        // Source: Tome of Battle, p. 38
        [TestMethod]
        public void InitiateManeuver_TransitionsAvailableToExpended_AndRejectsNonAvailable() {
            var engine = new MartialManeuverEngine();
            var warblade = engine.GetSource("Warblade");
            warblade.SetKnownSlot(0, new KnownManeuverSlot("Steel Strike", ManeuverState.Available));
            warblade.SetKnownSlot(1, new KnownManeuverSlot("Disarming Strike", ManeuverState.Expended));
            warblade.SetKnownSlot(2, new KnownManeuverSlot("Iron Heart Surge", ManeuverState.Unreadied));
            warblade.SetKnownSlot(3, new KnownManeuverSlot("Wall of Blades", ManeuverState.Withheld));

            // Available succeeds and transitions to Expended
            bool success0 = engine.InitiateManeuver("Warblade", 0);
            Assert.IsTrue(success0);
            Assert.AreEqual(ManeuverState.Expended, warblade.GetKnownSlot(0).State);

            // Expended rejects
            bool success1 = engine.InitiateManeuver("Warblade", 1);
            Assert.IsFalse(success1);
            Assert.AreEqual(ManeuverState.Expended, warblade.GetKnownSlot(1).State);

            // Unreadied rejects
            bool success2 = engine.InitiateManeuver("Warblade", 2);
            Assert.IsFalse(success2);
            Assert.AreEqual(ManeuverState.Unreadied, warblade.GetKnownSlot(2).State);

            // Withheld rejects
            bool success3 = engine.InitiateManeuver("Warblade", 3);
            Assert.IsFalse(success3);
            Assert.AreEqual(ManeuverState.Withheld, warblade.GetKnownSlot(3).State);

            // throwOnInvalid throws InvalidOperationException when not Available
            Assert.ThrowsException<InvalidOperationException>(() => engine.InitiateManeuver("Warblade", 0, throwOnInvalid: true));
        }

        // Source: Tome of Battle, p. 40
        [TestMethod]
        public void RecoverWarblade_RecoversAllExpendedManeuvers_PreservingUnreadied() {
            var engine = new MartialManeuverEngine();
            var warblade = engine.GetSource("Warblade");
            warblade.SetKnownSlot(0, new KnownManeuverSlot("Steel Strike", ManeuverState.Expended));
            warblade.SetKnownSlot(1, new KnownManeuverSlot("Disarming Strike", ManeuverState.Expended));
            warblade.SetKnownSlot(2, new KnownManeuverSlot("Iron Heart Surge", ManeuverState.Unreadied));

            var crusader = engine.GetSource("Crusader");
            crusader.SetKnownSlot(0, new KnownManeuverSlot("Crusader's Strike", ManeuverState.Expended));

            engine.RecoverWarblade("Warblade");

            Assert.AreEqual(ManeuverState.Available, warblade.GetKnownSlot(0).State);
            Assert.AreEqual(ManeuverState.Available, warblade.GetKnownSlot(1).State);
            Assert.AreEqual(ManeuverState.Unreadied, warblade.GetKnownSlot(2).State);

            // Crusader expended slot is unaffected (source isolation)
            Assert.AreEqual(ManeuverState.Expended, crusader.GetKnownSlot(0).State);
        }

        // Source: Tome of Battle, p. 40
        [TestMethod]
        public void RecoverSwordsage_RecoversSingleChosenManeuver() {
            var engine = new MartialManeuverEngine();
            var swordsage = engine.GetSource("Swordsage");
            swordsage.SetKnownSlot(0, new KnownManeuverSlot("Burning Blade", ManeuverState.Expended));
            swordsage.SetKnownSlot(1, new KnownManeuverSlot("Flistering Flourish", ManeuverState.Expended));
            swordsage.SetKnownSlot(2, new KnownManeuverSlot("Shadow Jaunt", ManeuverState.Unreadied));

            engine.RecoverSwordsage("Swordsage", 0);

            Assert.AreEqual(ManeuverState.Available, swordsage.GetKnownSlot(0).State);
            Assert.AreEqual(ManeuverState.Expended, swordsage.GetKnownSlot(1).State);
            Assert.AreEqual(ManeuverState.Unreadied, swordsage.GetKnownSlot(2).State);

            // Attempting to recover unreadied slot does not alter its state
            engine.RecoverSwordsage("Swordsage", 2);
            Assert.AreEqual(ManeuverState.Unreadied, swordsage.GetKnownSlot(2).State);
        }

        // Source: Tome of Battle, pp. 38, 40
        [TestMethod]
        public void DealCrusader_DealsGrantedManeuversAvailable_AndRemainderWithheld() {
            var engine = new MartialManeuverEngine();
            var context = new FakeCharacterContext();
            SetStat(context.Effects, "maneuvers known:crusader", 6);
            SetStat(context.Effects, "maneuvers granted:crusader", 2);
            engine.Initialize(context);

            var crusader = engine.GetSource("Crusader");
            crusader.SetKnownSlot(0, new KnownManeuverSlot("Crusader's Strike", ManeuverState.Expended));
            crusader.SetKnownSlot(1, new KnownManeuverSlot("Vanguard Strike", ManeuverState.Expended));
            crusader.SetKnownSlot(2, new KnownManeuverSlot("Stone Bones", ManeuverState.Expended));
            crusader.SetKnownSlot(3, new KnownManeuverSlot("Charging Minotaur", ManeuverState.Expended));
            crusader.SetKnownSlot(4, new KnownManeuverSlot("Leading the Attack", ManeuverState.Expended));
            crusader.SetKnownSlot(5, new KnownManeuverSlot("Mountain Hammer", ManeuverState.Unreadied));

            engine.DealCrusader("Crusader", new Random(42));

            var readied = crusader.ReadiedManeuvers.ToList();
            Assert.AreEqual(5, readied.Count);

            int availableCount = readied.Count(s => s.State == ManeuverState.Available);
            int withheldCount = readied.Count(s => s.State == ManeuverState.Withheld);

            Assert.AreEqual(2, availableCount);
            Assert.AreEqual(3, withheldCount);
            Assert.AreEqual(ManeuverState.Unreadied, crusader.GetKnownSlot(5).State);
        }

        // Source: Tome of Battle, p. 38
        [TestMethod]
        public void AdvanceCrusaderTurn_GrantsOneWithheldManeuver_EachTurn() {
            var engine = new MartialManeuverEngine();
            var context = new FakeCharacterContext();
            SetStat(context.Effects, "maneuvers known:crusader", 5);
            SetStat(context.Effects, "maneuvers granted:crusader", 2);
            engine.Initialize(context);

            var crusader = engine.GetSource("Crusader");
            crusader.SetKnownSlot(0, new KnownManeuverSlot("Crusader's Strike", ManeuverState.Expended));
            crusader.SetKnownSlot(1, new KnownManeuverSlot("Vanguard Strike", ManeuverState.Expended));
            crusader.SetKnownSlot(2, new KnownManeuverSlot("Stone Bones", ManeuverState.Expended));
            crusader.SetKnownSlot(3, new KnownManeuverSlot("Charging Minotaur", ManeuverState.Expended));
            crusader.SetKnownSlot(4, new KnownManeuverSlot("Leading the Attack", ManeuverState.Expended));

            // Deal hand: 2 Available, 3 Withheld
            engine.DealCrusader("Crusader", new Random(42));
            Assert.AreEqual(2, crusader.ReadiedManeuvers.Count(s => s.State == ManeuverState.Available));
            Assert.AreEqual(3, crusader.ReadiedManeuvers.Count(s => s.State == ManeuverState.Withheld));

            // Turn 1 advance grants 1 withheld maneuver
            engine.AdvanceCrusaderTurn("Crusader", new Random(1));
            Assert.AreEqual(3, crusader.ReadiedManeuvers.Count(s => s.State == ManeuverState.Available));
            Assert.AreEqual(2, crusader.ReadiedManeuvers.Count(s => s.State == ManeuverState.Withheld));

            // Turn 2 advance grants another withheld maneuver
            engine.AdvanceCrusaderTurn("Crusader", new Random(1));
            Assert.AreEqual(4, crusader.ReadiedManeuvers.Count(s => s.State == ManeuverState.Available));
            Assert.AreEqual(1, crusader.ReadiedManeuvers.Count(s => s.State == ManeuverState.Withheld));

            // Turn 3 advance grants final withheld maneuver
            engine.AdvanceCrusaderTurn("Crusader", new Random(1));
            Assert.AreEqual(5, crusader.ReadiedManeuvers.Count(s => s.State == ManeuverState.Available));
            Assert.AreEqual(0, crusader.ReadiedManeuvers.Count(s => s.State == ManeuverState.Withheld));
        }

        // Source: Tome of Battle, p. 38
        [TestMethod]
        public void AdvanceCrusaderTurn_ReshufflesDeck_WhenWithheldPoolIsEmpty() {
            var engine = new MartialManeuverEngine();
            var context = new FakeCharacterContext();
            SetStat(context.Effects, "maneuvers known:crusader", 5);
            SetStat(context.Effects, "maneuvers granted:crusader", 2);
            engine.Initialize(context);

            var crusader = engine.GetSource("Crusader");
            crusader.SetKnownSlot(0, new KnownManeuverSlot("Crusader's Strike", ManeuverState.Expended));
            crusader.SetKnownSlot(1, new KnownManeuverSlot("Vanguard Strike", ManeuverState.Expended));
            crusader.SetKnownSlot(2, new KnownManeuverSlot("Stone Bones", ManeuverState.Expended));
            crusader.SetKnownSlot(3, new KnownManeuverSlot("Charging Minotaur", ManeuverState.Available));
            crusader.SetKnownSlot(4, new KnownManeuverSlot("Leading the Attack", ManeuverState.Available));

            // Pool currently has 0 withheld maneuvers
            Assert.AreEqual(0, crusader.ReadiedManeuvers.Count(s => s.State == ManeuverState.Withheld));

            // Advancing from empty withheld pool triggers deck reshuffle: 2 Available, 3 Withheld
            engine.AdvanceCrusaderTurn("Crusader", new Random(42));

            Assert.AreEqual(2, crusader.ReadiedManeuvers.Count(s => s.State == ManeuverState.Available));
            Assert.AreEqual(3, crusader.ReadiedManeuvers.Count(s => s.State == ManeuverState.Withheld));
            Assert.AreEqual(0, crusader.ReadiedManeuvers.Count(s => s.State == ManeuverState.Expended));
        }

        // Source: Tome of Battle, pp. 38, 40
        [TestMethod]
        public void ResetEncounter_RestoresReadiedManeuversAcrossAllSources_AndRedealsCrusader() {
            var engine = new MartialManeuverEngine();
            var context = new FakeCharacterContext();
            SetStat(context.Effects, "maneuvers known:warblade", 2);
            SetStat(context.Effects, "maneuvers known:swordsage", 2);
            SetStat(context.Effects, "maneuvers known:feats", 1);
            SetStat(context.Effects, "maneuvers known:crusader", 6);
            SetStat(context.Effects, "maneuvers granted:crusader", 2);
            engine.Initialize(context);

            var warblade = engine.GetSource("Warblade");
            warblade.SetKnownSlot(0, new KnownManeuverSlot("Steel Strike", ManeuverState.Expended));
            warblade.SetKnownSlot(1, new KnownManeuverSlot("Iron Heart Surge", ManeuverState.Unreadied));

            var swordsage = engine.GetSource("Swordsage");
            swordsage.SetKnownSlot(0, new KnownManeuverSlot("Burning Blade", ManeuverState.Expended));
            swordsage.SetKnownSlot(1, new KnownManeuverSlot("Shadow Jaunt", ManeuverState.Available));

            var feats = engine.GetSource("Feats");
            feats.SetKnownSlot(0, new KnownManeuverSlot("Moment of Perfect Mind", ManeuverState.Expended));

            var crusader = engine.GetSource("Crusader");
            crusader.SetKnownSlot(0, new KnownManeuverSlot("Crusader's Strike", ManeuverState.Expended));
            crusader.SetKnownSlot(1, new KnownManeuverSlot("Vanguard Strike", ManeuverState.Expended));
            crusader.SetKnownSlot(2, new KnownManeuverSlot("Stone Bones", ManeuverState.Expended));
            crusader.SetKnownSlot(3, new KnownManeuverSlot("Charging Minotaur", ManeuverState.Expended));
            crusader.SetKnownSlot(4, new KnownManeuverSlot("Leading the Attack", ManeuverState.Expended));
            crusader.SetKnownSlot(5, new KnownManeuverSlot("Mountain Hammer", ManeuverState.Unreadied));

            engine.ResetEncounter(new Random(42));

            // Warblade: Expended -> Available, Unreadied -> Unreadied
            Assert.AreEqual(ManeuverState.Available, warblade.GetKnownSlot(0).State);
            Assert.AreEqual(ManeuverState.Unreadied, warblade.GetKnownSlot(1).State);

            // Swordsage: Expended -> Available, Available -> Available
            Assert.AreEqual(ManeuverState.Available, swordsage.GetKnownSlot(0).State);
            Assert.AreEqual(ManeuverState.Available, swordsage.GetKnownSlot(1).State);

            // Feats: Expended -> Available
            Assert.AreEqual(ManeuverState.Available, feats.GetKnownSlot(0).State);

            // Crusader: re-deals 5 readied into 2 Available, 3 Withheld; slot 5 remains Unreadied
            Assert.AreEqual(2, crusader.ReadiedManeuvers.Count(s => s.State == ManeuverState.Available));
            Assert.AreEqual(3, crusader.ReadiedManeuvers.Count(s => s.State == ManeuverState.Withheld));
            Assert.AreEqual(0, crusader.ReadiedManeuvers.Count(s => s.State == ManeuverState.Expended));
            Assert.AreEqual(ManeuverState.Unreadied, crusader.GetKnownSlot(5).State);
        }

        // Source: Tome of Battle, p. 38
        [TestMethod]
        public void ReadyManeuver_AndUnreadyManeuver_TransitionsSlotLifecycle() {
            var engine = new MartialManeuverEngine();
            var warblade = engine.GetSource("Warblade");
            warblade.SetKnownSlot(0, new KnownManeuverSlot("Steel Strike", ManeuverState.Unreadied));

            var crusader = engine.GetSource("Crusader");
            crusader.SetKnownSlot(0, new KnownManeuverSlot("Crusader's Strike", ManeuverState.Unreadied));

            // Ready warblade maneuver -> becomes Available
            engine.ReadyManeuver("Warblade", 0);
            Assert.AreEqual(ManeuverState.Available, warblade.GetKnownSlot(0).State);
            Assert.IsTrue(warblade.GetKnownSlot(0).IsReadied);

            // Unready warblade maneuver -> returns to Unreadied
            engine.UnreadyManeuver("Warblade", 0);
            Assert.AreEqual(ManeuverState.Unreadied, warblade.GetKnownSlot(0).State);
            Assert.IsFalse(warblade.GetKnownSlot(0).IsReadied);

            // Ready crusader maneuver -> becomes Withheld
            engine.ReadyManeuver("Crusader", 0);
            Assert.AreEqual(ManeuverState.Withheld, crusader.GetKnownSlot(0).State);
            Assert.IsTrue(crusader.GetKnownSlot(0).IsReadied);

            // Unready crusader maneuver -> returns to Unreadied
            engine.UnreadyManeuver("Crusader", 0);
            Assert.AreEqual(ManeuverState.Unreadied, crusader.GetKnownSlot(0).State);
            Assert.IsFalse(crusader.GetKnownSlot(0).IsReadied);
        }

        // Source: Tome of Battle, pp. 5, 38, 43
        [TestMethod]
        public void AdoptStance_SetsActive_AndClearsOtherStances_WhenDualStanceNotPermitted() {
            var stance1 = CreateManeuver("Punishing Stance", "Iron Heart", 1, ManeuverType.Stance);
            var stance2 = CreateManeuver("Blood in the Water", "Tiger Claw", 1, ManeuverType.Stance);
            var stance3 = CreateManeuver("Hunter's Sense", "Tiger Claw", 1, ManeuverType.Stance);
            var content = new TestManeuverContentProvider(stance1, stance2, stance3);

            var engine = new MartialManeuverEngine(content);
            var context = new FakeCharacterContext();
            SetStat(context.Effects, "stances known:warblade", 2);
            SetStat(context.Effects, "stances known:swordsage", 1);
            engine.Initialize(context);

            var warblade = engine.GetSource("Warblade");
            warblade.SetStanceSlot(0, new StanceSlot("Punishing Stance"));
            warblade.SetStanceSlot(1, new StanceSlot("Blood in the Water"));

            var swordsage = engine.GetSource("Swordsage");
            swordsage.SetStanceSlot(0, new StanceSlot("Hunter's Sense"));

            // Adopt warblade stance 0
            engine.AdoptStance("Warblade", 0);
            Assert.IsTrue(warblade.GetStanceSlot(0).IsActive);
            Assert.IsFalse(warblade.GetStanceSlot(1).IsActive);
            Assert.IsFalse(swordsage.GetStanceSlot(0).IsActive);

            // Switch to swordsage stance 0 -> warblade stance 0 is cleared
            engine.AdoptStance("Swordsage", 0);
            Assert.IsFalse(warblade.GetStanceSlot(0).IsActive);
            Assert.IsTrue(swordsage.GetStanceSlot(0).IsActive);

            // Exit single stance
            engine.ExitStance("Swordsage", 0);
            Assert.IsFalse(swordsage.GetStanceSlot(0).IsActive);

            // Adopt again, then ExitAllStances
            engine.AdoptStance("Warblade", 1);
            Assert.IsTrue(warblade.GetStanceSlot(1).IsActive);
            engine.ExitAllStances();
            Assert.IsFalse(warblade.GetStanceSlot(1).IsActive);
        }

        // Source: Tome of Battle, pp. 5, 23, 38, 43
        [TestMethod]
        public void AdoptStance_PreservesBothActiveStances_WhenActiveStancesIsTwo() {
            var stance1 = CreateManeuver("Punishing Stance", "Iron Heart", 1, ManeuverType.Stance);
            var stance2 = CreateManeuver("Blood in the Water", "Tiger Claw", 1, ManeuverType.Stance);
            var content = new TestManeuverContentProvider(stance1, stance2);

            var engine = new MartialManeuverEngine(content);
            var context = new FakeCharacterContext();
            SetStat(context.Effects, "stances known:warblade", 2);
            SetStat(context.Effects, "active stances", 2);
            engine.Initialize(context);

            var warblade = engine.GetSource("Warblade");
            warblade.SetStanceSlot(0, new StanceSlot("Punishing Stance"));
            warblade.SetStanceSlot(1, new StanceSlot("Blood in the Water"));

            engine.AdoptStance("Warblade", 0);
            Assert.IsTrue(warblade.GetStanceSlot(0).IsActive);

            // Adopting second stance with active stances = 2 does not clear first stance
            engine.AdoptStance("Warblade", 1);
            Assert.IsTrue(warblade.GetStanceSlot(0).IsActive);
            Assert.IsTrue(warblade.GetStanceSlot(1).IsActive);
        }

        // Source: Tome of Battle, pp. 5, 23, 38, 40, 43
        [TestMethod]
        public void AdoptStance_CapsConcurrentActiveStancesAtTwo_UsingOldestFallback_WhenNoTargetSpecified() {
            var stance1 = CreateManeuver("Punishing Stance", "Iron Heart", 1, ManeuverType.Stance);
            var stance2 = CreateManeuver("Blood in the Water", "Tiger Claw", 1, ManeuverType.Stance);
            var stance3 = CreateManeuver("Hunter's Sense", "Tiger Claw", 1, ManeuverType.Stance);
            var content = new TestManeuverContentProvider(stance1, stance2, stance3);

            var engine = new MartialManeuverEngine(content);
            var context = new FakeCharacterContext();
            SetStat(context.Effects, "stances known:warblade", 3);
            SetStat(context.Effects, "active stances", 2);
            engine.Initialize(context);

            var warblade = engine.GetSource("Warblade");
            warblade.SetStanceSlot(0, new StanceSlot("Punishing Stance"));
            warblade.SetStanceSlot(1, new StanceSlot("Blood in the Water"));
            warblade.SetStanceSlot(2, new StanceSlot("Hunter's Sense"));

            // Adopt stance 0
            engine.AdoptStance("Warblade", 0);
            Assert.IsTrue(warblade.GetStanceSlot(0).IsActive);

            // Adopt stance 1 -> both active
            engine.AdoptStance("Warblade", 1);
            Assert.IsTrue(warblade.GetStanceSlot(0).IsActive);
            Assert.IsTrue(warblade.GetStanceSlot(1).IsActive);
            Assert.IsFalse(warblade.GetStanceSlot(2).IsActive);

            // Adopt stance 2 -> oldest (stance 0) is deactivated, stances 1 and 2 remain active
            engine.AdoptStance("Warblade", 2);
            Assert.IsFalse(warblade.GetStanceSlot(0).IsActive);
            Assert.IsTrue(warblade.GetStanceSlot(1).IsActive);
            Assert.IsTrue(warblade.GetStanceSlot(2).IsActive);

            // Adopt stance 0 again -> oldest (stance 1) is deactivated, stances 2 and 0 remain active
            engine.AdoptStance("Warblade", 0);
            Assert.IsTrue(warblade.GetStanceSlot(0).IsActive);
            Assert.IsFalse(warblade.GetStanceSlot(1).IsActive);
            Assert.IsTrue(warblade.GetStanceSlot(2).IsActive);
        }

        // Source: Tome of Battle, p. 23
        [TestMethod]
        public void AdoptStance_WithExplicitReplacementTarget_ReplacesSpecifiedStance_WhenAtCapacity() {
            var stance1 = CreateManeuver("Punishing Stance", "Iron Heart", 1, ManeuverType.Stance);
            var stance2 = CreateManeuver("Blood in the Water", "Tiger Claw", 1, ManeuverType.Stance);
            var stance3 = CreateManeuver("Hunter's Sense", "Tiger Claw", 1, ManeuverType.Stance);
            var content = new TestManeuverContentProvider(stance1, stance2, stance3);

            var engine = new MartialManeuverEngine(content);
            var context = new FakeCharacterContext();
            SetStat(context.Effects, "stances known:warblade", 3);
            SetStat(context.Effects, "active stances", 2);
            engine.Initialize(context);

            var warblade = engine.GetSource("Warblade");
            warblade.SetStanceSlot(0, new StanceSlot("Punishing Stance"));
            warblade.SetStanceSlot(1, new StanceSlot("Blood in the Water"));
            warblade.SetStanceSlot(2, new StanceSlot("Hunter's Sense"));

            // Adopt stances 0 and 1
            engine.AdoptStance("Warblade", 0);
            engine.AdoptStance("Warblade", 1);
            Assert.IsTrue(warblade.GetStanceSlot(0).IsActive);
            Assert.IsTrue(warblade.GetStanceSlot(1).IsActive);

            // Adopt stance 2, explicitly choosing to replace stance 1 (rather than oldest stance 0)
            engine.AdoptStance("Warblade", 2, "Warblade", 1);
            Assert.IsTrue(warblade.GetStanceSlot(0).IsActive, "Stance 0 should remain active");
            Assert.IsFalse(warblade.GetStanceSlot(1).IsActive, "Stance 1 was explicitly replaced");
            Assert.IsTrue(warblade.GetStanceSlot(2).IsActive, "Stance 2 should now be active");
        }

        // Source: Tome of Battle, p. 23
        [TestMethod]
        public void AdoptStances_AdoptsBothStancesSimultaneously_ClearingPrevious() {
            var stance1 = CreateManeuver("Punishing Stance", "Iron Heart", 1, ManeuverType.Stance);
            var stance2 = CreateManeuver("Blood in the Water", "Tiger Claw", 1, ManeuverType.Stance);
            var stance3 = CreateManeuver("Hunter's Sense", "Tiger Claw", 1, ManeuverType.Stance);
            var content = new TestManeuverContentProvider(stance1, stance2, stance3);

            var engine = new MartialManeuverEngine(content);
            var context = new FakeCharacterContext();
            SetStat(context.Effects, "stances known:warblade", 3);
            SetStat(context.Effects, "active stances", 2);
            engine.Initialize(context);

            var warblade = engine.GetSource("Warblade");
            warblade.SetStanceSlot(0, new StanceSlot("Punishing Stance"));
            warblade.SetStanceSlot(1, new StanceSlot("Blood in the Water"));
            warblade.SetStanceSlot(2, new StanceSlot("Hunter's Sense"));

            // Start in stance 2
            engine.AdoptStance("Warblade", 2);
            Assert.IsTrue(warblade.GetStanceSlot(2).IsActive);

            // Change both stances simultaneously in a single swift action
            engine.AdoptStances("Warblade", 0, "Warblade", 1);
            Assert.IsTrue(warblade.GetStanceSlot(0).IsActive);
            Assert.IsTrue(warblade.GetStanceSlot(1).IsActive);
            Assert.IsFalse(warblade.GetStanceSlot(2).IsActive);
        }

        // Source: ADR-0001
        [TestMethod]
        public void ValidateStances_WarnsWhenActiveStancesExceedsCapacity() {
            var stance1 = CreateManeuver("Punishing Stance", "Iron Heart", 1, ManeuverType.Stance);
            var content = new TestManeuverContentProvider(stance1);

            var engine = new MartialManeuverEngine(content);
            var context = new FakeCharacterContext();
            SetStat(context.Effects, "stances known:warblade", 2);
            SetStat(context.Effects, "active stances", 1);
            engine.Initialize(context);

            var warblade = engine.GetSource("Warblade");
            warblade.SetStanceSlot(0, new StanceSlot("Punishing Stance", true));
            warblade.SetStanceSlot(1, new StanceSlot("Blood in the Water", true));

            // Two active stances with active stances capacity of 1
            var warnings = engine.ValidateStances();
            Assert.IsTrue(warnings.Any(w => w.Contains("Active stances count (2) exceeds active stances capacity (1)")));

            var maneuverWarnings = engine.ValidateManeuver("Punishing Stance", "Warblade");
            Assert.IsTrue(maneuverWarnings.Any(w => w.Contains("Active stances count (2) exceeds active stances capacity (1)")));
        }

        // Source: Tome of Battle, pp. 5, 38, 43
        [TestMethod]
        public void ContributeEffects_ProjectsActiveStanceModifiers_AndIgnoresInactiveStances() {
            var stanceEffect = new Effect("Punishing Stance Effect")
                .AddModifier("AC", new Modifier(-2, UNTYPED));
            var stance = new Maneuver {
                Name = "Punishing Stance",
                Discipline = "Iron Heart",
                Level = 1,
                Type = ManeuverType.Stance,
                Effects = new List<Effect> { stanceEffect }
            };
            var content = new TestManeuverContentProvider(stance);

            var engine = new MartialManeuverEngine(content);
            var context = new FakeCharacterContext();
            SetStat(context.Effects, "stances known:warblade", 1);
            SetStat(context.Effects, "AC", 15);
            engine.Initialize(context);

            var warblade = engine.GetSource("Warblade");
            warblade.SetStanceSlot(0, new StanceSlot("Punishing Stance", isActive: false));

            // While inactive, ContributeEffects does not add stance modifier
            var pool = new EffectPool(context.Character);
            SetStat(pool, "AC", 15);
            engine.ContributeEffects(pool, context);
            Assert.AreEqual(15, pool.GetStatValue("AC", null));

            // When adopted / active, ContributeEffects applies the -2 penalty
            engine.AdoptStance("Warblade", 0);
            var activePool = new EffectPool(context.Character);
            SetStat(activePool, "AC", 15);
            engine.ContributeEffects(activePool, context);
            Assert.AreEqual(13, activePool.GetStatValue("AC", null));
        }

        // Source: Tome of Battle, pp. 42-45
        [TestMethod]
        public void InitiateManeuver_SchedulesSheetActionForBoostAndCounter_AndExpiresOnTimelineAdvance() {
            var boostEffect = new Effect("Wind Stride Effect")
                .AddModifier("Speed", new Modifier(10, ENHANCEMENT));
            var boost = new Maneuver {
                Name = "Wind Stride",
                Discipline = "Desert Wind",
                Level = 1,
                Type = ManeuverType.Boost,
                Effects = new List<Effect> { boostEffect }
            };
            var counter = CreateManeuver("Fire Riposte", "Desert Wind", 2, ManeuverType.Counter);
            var strike = CreateManeuver("Steel Strike", "Iron Heart", 1, ManeuverType.Strike);
            var content = new TestManeuverContentProvider(boost, counter, strike);

            var engine = new MartialManeuverEngine(content);
            var character = new Character();
            var timeline = new Timeline();
            character.Timeline = timeline;
            var context = new FakeCharacterContext(character);
            SetStat(context.Effects, "maneuvers known:warblade", 3);
            engine.Initialize(context);

            var warblade = engine.GetSource("Warblade");
            warblade.SetKnownSlot(0, new KnownManeuverSlot("Wind Stride", ManeuverState.Available));
            warblade.SetKnownSlot(1, new KnownManeuverSlot("Fire Riposte", ManeuverState.Available));
            warblade.SetKnownSlot(2, new KnownManeuverSlot("Steel Strike", ManeuverState.Available));

            // 1. Strike transitions to Expended without scheduling timeline action
            bool strikeInitiated = engine.InitiateManeuver("Warblade", 2);
            Assert.IsTrue(strikeInitiated);
            Assert.AreEqual(ManeuverState.Expended, warblade.GetKnownSlot(2).State);
            Assert.AreEqual(0, timeline.ScheduledActions.Count);

            // 2. Boost transitions to Expended and schedules ManeuverSheetAction on Timeline
            bool boostInitiated = engine.InitiateManeuver("Warblade", 0);
            Assert.IsTrue(boostInitiated);
            Assert.AreEqual(ManeuverState.Expended, warblade.GetKnownSlot(0).State);
            Assert.AreEqual(1, timeline.ScheduledActions.Count);
            Assert.IsTrue(timeline.ScheduledActions.ContainsKey(1));
            var scheduledAction = timeline.ScheduledActions[1][0] as ManeuverSheetAction;
            Assert.IsNotNull(scheduledAction);
            Assert.AreEqual("Wind Stride", scheduledAction.ManeuverName);
            Assert.AreEqual("Warblade", scheduledAction.Source);

            // 3. ContributeEffects projects temporary boost effect into EffectPool
            var pool = new EffectPool(character);
            SetStat(pool, "Speed", 30);
            engine.ContributeEffects(pool, context);
            Assert.AreEqual(40, pool.GetStatValue("Speed", null));

            // 4. Counter transitions to Expended and schedules ManeuverSheetAction
            bool counterInitiated = engine.InitiateManeuver("Warblade", 1);
            Assert.IsTrue(counterInitiated);
            Assert.AreEqual(ManeuverState.Expended, warblade.GetKnownSlot(1).State);
            Assert.AreEqual(2, timeline.ScheduledActions[1].Count);

            // 5. Timeline advances 1 round: actions expire, returning RecomputeDerived, removed from ScheduledActions
            var advanceResult = timeline.Advance(1, context);
            Assert.AreEqual(TimelineRefreshRequirement.RecomputeDerived, advanceResult.RefreshRequirement);
            Assert.AreEqual(0, timeline.ScheduledActions.Count);
            Assert.AreEqual(2, advanceResult.ExpiredActions.Count);

            // 6. ContributeEffects after expiration no longer applies boost effect
            var postExpirationPool = new EffectPool(character);
            SetStat(postExpirationPool, "Speed", 30);
            engine.ContributeEffects(postExpirationPool, context);
            Assert.AreEqual(30, postExpirationPool.GetStatValue("Speed", null));

            // 7. Maneuver slot remains Expended (does not un-expend)
            Assert.AreEqual(ManeuverState.Expended, warblade.GetKnownSlot(0).State);
            Assert.AreEqual(ManeuverState.Expended, warblade.GetKnownSlot(1).State);
        }

        // Source: Tome of Battle, p. 38
        [TestMethod]
        public void ManeuverSheetAction_SerializesAndRoundTripsViaFromJson() {
            var action = new ManeuverSheetAction("Wind Stride", "Swordsage", "1 round");
            var json = (JObject)action.ToJson();

            Assert.AreEqual("Wind Stride", (string)json["maneuver_action"]);
            Assert.AreEqual("Swordsage", (string)json["source"]);
            Assert.AreEqual("1 round", (string)json["duration"]);
            Assert.AreEqual("Wind Stride (Swordsage)", (string)json["name"]);

            var deserialized = SheetAction.FromJson(json);
            Assert.IsInstanceOfType(deserialized, typeof(ManeuverSheetAction));
            var mAction = (ManeuverSheetAction)deserialized;
            Assert.AreEqual("Wind Stride", mAction.ManeuverName);
            Assert.AreEqual("Swordsage", mAction.Source);
            Assert.IsTrue(action.Matches(deserialized));

            // Non-canonical "maneuver" key is rejected (ADR-0008: no JSON fallbacks)
            var altJson = new JObject(
                new JProperty("maneuver", "Fire Riposte"),
                new JProperty("source", "Desert Wind"),
                new JProperty("duration", "1 round")
            );
            var altDeserialized = SheetAction.FromJson(altJson);
            Assert.IsNotInstanceOfType(altDeserialized, typeof(ManeuverSheetAction));
            Assert.IsNull(ManeuverSheetAction.TryParse(altJson));
        }

        // Source: Tome of Battle, pp. 38, 43, 56, 66, 73, 85, 90
        [TestMethod]
        public void MartialManeuverEngine_FullCatalogStanceActivation_ModifiesStatsAcrossClasses() {
            var content = new ResourceSubsystemContentProvider();
            var engine = new MartialManeuverEngine(content);
            var context = new FakeCharacterContext();

            SetStat(context.Effects, "stances known:warblade", 2);
            SetStat(context.Effects, "stances known:swordsage", 1);
            SetStat(context.Effects, "stances known:crusader", 1);
            SetStat(context.Effects, "AC", 15);
            SetStat(context.Effects, "Land speed", 30);
            SetStat(context.Effects, "Listen", 4);
            SetStat(context.Effects, "Will saves", 3);

            engine.Initialize(context);

            var warblade = engine.GetSource("Warblade");
            warblade.SetStanceSlot(0, new StanceSlot("Punishing Stance", isActive: false));
            warblade.SetStanceSlot(1, new StanceSlot("Absolute Steel", isActive: false));

            var swordsage = engine.GetSource("Swordsage");
            swordsage.SetStanceSlot(0, new StanceSlot("Hearing the Air", isActive: false));

            var crusader = engine.GetSource("Crusader");
            crusader.SetStanceSlot(0, new StanceSlot("Bolstering Voice", isActive: false));

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
            engine.AdoptStance("Warblade", 0);
            var wbPool = new EffectPool(context.Character);
            SetStat(wbPool, "AC", 15);
            SetStat(wbPool, "Land speed", 30);
            engine.ContributeEffects(wbPool, context);
            Assert.AreEqual(13, wbPool.GetStatValue("AC", null));

            // Warblade switches to Absolute Steel (+10 Land speed)
            engine.AdoptStance("Warblade", 1);
            var wbSpeedPool = new EffectPool(context.Character);
            SetStat(wbSpeedPool, "AC", 15);
            SetStat(wbSpeedPool, "Land speed", 30);
            engine.ContributeEffects(wbSpeedPool, context);
            Assert.AreEqual(15, wbSpeedPool.GetStatValue("AC", null));
            Assert.AreEqual(40, wbSpeedPool.GetStatValue("Land speed", null));

            // Swordsage activates Hearing the Air (+5 Listen)
            engine.AdoptStance("Swordsage", 0);
            var ssPool = new EffectPool(context.Character);
            SetStat(ssPool, "Listen", 4);
            engine.ContributeEffects(ssPool, context);
            Assert.AreEqual(9, ssPool.GetStatValue("Listen", null));

            // Crusader activates Bolstering Voice (+2 Will saves)
            engine.AdoptStance("Crusader", 0);
            var crPool = new EffectPool(context.Character);
            SetStat(crPool, "Will saves", 3);
            engine.ContributeEffects(crPool, context);
            Assert.AreEqual(5, crPool.GetStatValue("Will saves", null));
        }

        // Source: Tome of Battle, pp. 38–40
        [TestMethod]
        public void MartialManeuverEngine_FullCatalogRecovery_OperatesAcrossCrusaderSwordsageAndWarblade() {
            var content = new ResourceSubsystemContentProvider();
            var engine = new MartialManeuverEngine(content);
            var context = new FakeCharacterContext();

            SetStat(context.Effects, "maneuvers known:warblade", 2);
            SetStat(context.Effects, "maneuvers readied:warblade", 2);
            SetStat(context.Effects, "maneuvers known:swordsage", 2);
            SetStat(context.Effects, "maneuvers readied:swordsage", 2);
            SetStat(context.Effects, "maneuvers known:crusader", 3);
            SetStat(context.Effects, "maneuvers readied:crusader", 3);
            SetStat(context.Effects, "maneuvers granted:crusader", 2);

            engine.Initialize(context);

            // 1. Warblade: initiate and recover via swift flourish
            var warblade = engine.GetSource("Warblade");
            warblade.SetKnownSlot(0, new KnownManeuverSlot("Steel Wind", ManeuverState.Available));
            warblade.SetKnownSlot(1, new KnownManeuverSlot("Iron Heart Surge", ManeuverState.Available));

            engine.InitiateManeuver("Warblade", 0);
            Assert.AreEqual(ManeuverState.Expended, warblade.GetKnownSlot(0).State);
            engine.RecoverWarblade("Warblade");
            Assert.AreEqual(ManeuverState.Available, warblade.GetKnownSlot(0).State);

            // 2. Swordsage: initiate and recover via targeted full-round meditation
            var swordsage = engine.GetSource("Swordsage");
            swordsage.SetKnownSlot(0, new KnownManeuverSlot("Fire Riposte", ManeuverState.Available));
            swordsage.SetKnownSlot(1, new KnownManeuverSlot("Burning Blade", ManeuverState.Available));

            engine.InitiateManeuver("Swordsage", 0);
            Assert.AreEqual(ManeuverState.Expended, swordsage.GetKnownSlot(0).State);
            engine.RecoverSwordsage("Swordsage", 0);
            Assert.AreEqual(ManeuverState.Available, swordsage.GetKnownSlot(0).State);

            // 3. Crusader: deal deck, initiate available maneuver, and advance turn to draw withheld
            var crusader = engine.GetSource("Crusader");
            crusader.SetKnownSlot(0, new KnownManeuverSlot("Crusader's Strike", ManeuverState.Available));
            crusader.SetKnownSlot(1, new KnownManeuverSlot("Shield Block", ManeuverState.Available));
            crusader.SetKnownSlot(2, new KnownManeuverSlot("Revitalizing Strike", ManeuverState.Withheld));

            engine.InitiateManeuver("Crusader", 0);
            Assert.AreEqual(ManeuverState.Expended, crusader.GetKnownSlot(0).State);
            engine.AdvanceCrusaderTurn("Crusader", new Random(42));
            Assert.AreEqual(ManeuverState.Available, crusader.GetKnownSlot(2).State);
        }
    }
}
