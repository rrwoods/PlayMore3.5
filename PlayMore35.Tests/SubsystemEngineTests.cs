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
using PlayMore35.Subsystems.Maneuvers;
using PlayMore35.Subsystems.PactMagic;

namespace PlayMore35.Tests {
    [TestClass]
    public class SubsystemEngineTests {
        private class FakeFirstSubsystem : ISubsystemEngine {
            public string Id => "fake-first";
            public bool IsActive => true;
            public void Initialize(ICharacterContext context) { }
            public void ContributeEffects(EffectPool pool, ICharacterContext context) { }
        }

        private class FakeSimpleSubsystem : ISubsystemEngine {
            public string Id => "fake-simple";
            public bool IsActive => false;
            public void Initialize(ICharacterContext context) { }
            public void ContributeEffects(EffectPool pool, ICharacterContext context) { }
        }

        [TestMethod]
        public void SubsystemRegistry_RegistersAndRetrievesEngines_ByTypeAndId() {
            var registry = new SubsystemRegistry();
            var first = new FakeFirstSubsystem();
            var simple = new FakeSimpleSubsystem();

            registry.Register(first);
            registry.Register(simple);

            Assert.AreSame(first, registry.Get<FakeFirstSubsystem>());
            Assert.AreSame(first, registry.Get("fake-first"));

            Assert.AreSame(simple, registry.Get<FakeSimpleSubsystem>());
            Assert.AreSame(simple, registry.Get("fake-simple"));

            Assert.IsNull(registry.Get("nonexistent"));
            var unregistered = new SubsystemRegistry();
            Assert.IsNull(unregistered.Get<FakeFirstSubsystem>());
        }

        [TestMethod]
        public void SubsystemRegistry_ReRegistration_ReplacesExistingEngineInAllAndGet() {
            var registry = new SubsystemRegistry();
            var first = new FakeFirstSubsystem();
            var replacement = new FakeFirstSubsystem();

            registry.Register(first, "custom-alias");
            Assert.AreSame(first, registry.Get<FakeFirstSubsystem>());
            Assert.AreSame(first, registry.Get("fake-first"));
            Assert.AreSame(first, registry.Get("custom-alias"));

            registry.Register(replacement, "custom-alias");
            Assert.AreSame(replacement, registry.Get<FakeFirstSubsystem>());
            Assert.AreSame(replacement, registry.Get("fake-first"));
            Assert.AreSame(replacement, registry.Get("custom-alias"));
            Assert.AreEqual(1, registry.All.Count());
            Assert.AreSame(replacement, registry.All.Single());
        }

        [TestMethod]
        public void SubsystemRegistry_CreateDefault_RegistersStandardEngines() {
            var registry = SubsystemRegistry.CreateDefault();
            var engine = registry.Get<PactMagicEngine>();
            Assert.IsNotNull(engine);
            Assert.AreSame(engine, registry.Get("pact_magic"));
            Assert.AreSame(engine, registry.Get("vestige"));
            CollectionAssert.Contains(registry.All.ToList(), engine);

            var maneuverEngine = registry.Get<MartialManeuverEngine>();
            Assert.IsNotNull(maneuverEngine);
            Assert.AreSame(maneuverEngine, registry.Get("martial_maneuvers"));
            Assert.AreSame(maneuverEngine, registry.Get("maneuver"));
            CollectionAssert.Contains(registry.All.ToList(), maneuverEngine);
        }

        [TestMethod]
        public void SubsystemRegistry_NewInstance_RemainsEmptyForTestIsolation() {
            var registry = new SubsystemRegistry();
            Assert.AreEqual(0, registry.All.Count());
            Assert.IsNull(registry.Get<PactMagicEngine>());
            Assert.IsNull(registry.Get("pact_magic"));
            Assert.IsNull(registry.Get("vestige"));
            Assert.IsNull(registry.Get<MartialManeuverEngine>());
            Assert.IsNull(registry.Get("martial_maneuvers"));
            Assert.IsNull(registry.Get("maneuver"));
        }

        [TestMethod]
        public void SubsystemSlotGroup_AllowsDecoupledAccessAndCapacityTracking_Dynamically() {
            int dynamicCapacity = 2;
            var slotGroup = new SubsystemSlotGroup<string>(() => dynamicCapacity);

            Assert.AreEqual(2, slotGroup.Capacity);
            Assert.AreEqual(2, slotGroup.Slots.Count);

            slotGroup[0] = "Item A";
            slotGroup[1] = "Item B";
            Assert.AreEqual("Item A", slotGroup[0]);
            Assert.AreEqual("Item B", slotGroup[1]);

            // Negative index throws
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => {
                var _ = slotGroup[-1];
            });
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => {
                slotGroup[-1] = "Item Invalid";
            });

            // Index beyond capacity does not throw: auto-expands storage
            slotGroup[2] = "Item C";
            Assert.AreEqual("Item C", slotGroup[2]);
            Assert.AreEqual(3, slotGroup.Slots.Count);

            // Dynamically expand capacity
            dynamicCapacity = 4;
            Assert.AreEqual(4, slotGroup.Capacity);
            Assert.AreEqual(4, slotGroup.Slots.Count);
            Assert.AreEqual("Item C", slotGroup[2]);
            Assert.IsNull(slotGroup[3]);

            // Dynamically shrink capacity below populated count preserves occupied slots
            dynamicCapacity = 1;
            Assert.AreEqual(1, slotGroup.Capacity);
            Assert.AreEqual(4, slotGroup.Slots.Count);
            Assert.AreEqual("Item A", slotGroup[0]);
            Assert.AreEqual("Item B", slotGroup[1]);
            Assert.AreEqual("Item C", slotGroup[2]);

            // Clear slot
            slotGroup.ClearSlot(0);
            Assert.IsNull(slotGroup[0]);
        }

        [TestMethod]
        public void SubsystemSlotGroup_FixedCapacity_InitializesAndSetsSlots() {
            var slotGroup = new SubsystemSlotGroup<string>(3);

            Assert.AreEqual(3, slotGroup.Capacity);
            Assert.AreEqual(3, slotGroup.Slots.Count);

            slotGroup[0] = "Alpha";
            slotGroup[1] = "Beta";
            slotGroup[2] = "Gamma";

            Assert.AreEqual("Alpha", slotGroup[0]);
            Assert.AreEqual("Beta", slotGroup[1]);
            Assert.AreEqual("Gamma", slotGroup[2]);

            var items = slotGroup.Slots;
            Assert.AreEqual("Alpha", items[0]);
            Assert.AreEqual("Beta", items[1]);
            Assert.AreEqual("Gamma", items[2]);
        }

        [TestMethod]
        public void SubsystemSlotGroup_ClearSlot_ResetsSpecificSlotToDefault() {
            var slotGroup = new SubsystemSlotGroup<string>(3);
            slotGroup[0] = "One";
            slotGroup[1] = "Two";
            slotGroup[2] = "Three";

            slotGroup.ClearSlot(1);

            Assert.AreEqual("One", slotGroup[0]);
            Assert.IsNull(slotGroup[1]);
            Assert.AreEqual("Three", slotGroup[2]);

            // Negative index throws
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => slotGroup.ClearSlot(-1));
            // Non-negative index beyond current count does not throw
            slotGroup.ClearSlot(3);
        }

        [TestMethod]
        public void SubsystemSlotGroup_ClearAll_ResetsAllSlotsToDefault() {
            var slotGroup = new SubsystemSlotGroup<string>(3);
            slotGroup[0] = "First";
            slotGroup[1] = "Second";
            slotGroup[2] = "Third";

            slotGroup.ClearAll();

            Assert.AreEqual(3, slotGroup.Slots.Count);
            Assert.IsNull(slotGroup[0]);
            Assert.IsNull(slotGroup[1]);
            Assert.IsNull(slotGroup[2]);
        }

        [TestMethod]
        public void SubsystemSlotGroup_NegativeCapacity_ClampsToZero_AndAllowsSlotAccess() {
            var slotGroup = new SubsystemSlotGroup<int>(() => -5);

            Assert.AreEqual(0, slotGroup.Capacity);
            Assert.AreEqual(0, slotGroup.Slots.Count);

            // Setting and getting at non-negative indices succeeds and auto-expands storage
            slotGroup[0] = 42;
            Assert.AreEqual(42, slotGroup[0]);
            Assert.AreEqual(1, slotGroup.Slots.Count);
            Assert.AreEqual(42, slotGroup.Slots[0]);

            // Negative indices throw
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => {
                var _ = slotGroup[-1];
            });
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => {
                slotGroup[-1] = 99;
            });
        }

        [TestMethod]
        public void SubsystemSlotGroup_ZeroCapacity_AllowsPopulatingAndReadingSlots() {
            var slotGroup = new SubsystemSlotGroup<string>(0);
            Assert.AreEqual(0, slotGroup.Capacity);
            Assert.AreEqual(0, slotGroup.Slots.Count);

            // Populating index 2 auto-expands backing list
            slotGroup[2] = "Echo";
            Assert.AreEqual(0, slotGroup.Capacity);
            Assert.AreEqual(3, slotGroup.Slots.Count);
            Assert.IsNull(slotGroup[0]);
            Assert.IsNull(slotGroup[1]);
            Assert.AreEqual("Echo", slotGroup[2]);

            // Accessing unpopulated index returns default and auto-expands
            var read = slotGroup[4];
            Assert.IsNull(read);
            Assert.AreEqual(5, slotGroup.Slots.Count);
        }

        [TestMethod]
        public void SubsystemSlotGroup_CapacityTransitions_PreserveOccupiedSlotsAndPadEmptySlots() {
            int dynamicCapacity = 2;
            var slotGroup = new SubsystemSlotGroup<string>(() => dynamicCapacity);

            slotGroup[0] = "Alpha";
            slotGroup[1] = "Beta";

            // Capacity expands from 2 to 4: pads with empty slots
            dynamicCapacity = 4;
            Assert.AreEqual(4, slotGroup.Capacity);
            Assert.AreEqual(4, slotGroup.Slots.Count);
            Assert.AreEqual("Alpha", slotGroup.Slots[0]);
            Assert.AreEqual("Beta", slotGroup.Slots[1]);
            Assert.IsNull(slotGroup.Slots[2]);
            Assert.IsNull(slotGroup.Slots[3]);

            // Set slot 3
            slotGroup[3] = "Delta";
            Assert.AreEqual("Delta", slotGroup.Slots[3]);

            // Capacity drops to 0 (e.g. temporary stat penalty or uninitialized state)
            dynamicCapacity = 0;
            Assert.AreEqual(0, slotGroup.Capacity);
            // Slots retains all occupied slots up to highest populated index
            Assert.AreEqual(4, slotGroup.Slots.Count);
            Assert.AreEqual("Alpha", slotGroup.Slots[0]);
            Assert.AreEqual("Beta", slotGroup.Slots[1]);
            Assert.IsNull(slotGroup.Slots[2]);
            Assert.AreEqual("Delta", slotGroup.Slots[3]);

            // Modifying retained slots at zero capacity succeeds without throwing
            slotGroup[1] = "Beta-Modified";
            Assert.AreEqual("Beta-Modified", slotGroup[1]);
            Assert.AreEqual("Beta-Modified", slotGroup.Slots[1]);
        }

        [TestMethod]
        public void SubsystemSlotGroup_NullCapacityProvider_ThrowsArgumentNullException() {
            Assert.ThrowsException<ArgumentNullException>(() => new SubsystemSlotGroup<string>((Func<int>)null));
        }

        [TestMethod]
        public void CharacterContext_ExposesSubsystemRegistry() {
            var session = new CharacterSession(new Character(), new PlayMore35.Rules.GameRulesCatalog());
            Assert.IsNotNull(session.Subsystems);
            Assert.IsInstanceOfType(session.Subsystems, typeof(SubsystemRegistry));

            ICharacterContext context = session;
            Assert.IsNotNull(context.Subsystems);
        }

        [TestMethod]
        public void CharacterSession_InitializesSubsystemsWithDefaults() {
            var session = new CharacterSession(new Character(), new PlayMore35.Rules.GameRulesCatalog());
            Assert.IsNotNull(session.Subsystems.Get<PactMagicEngine>());
            Assert.IsNotNull(session.Subsystems.Get("vestige"));
        }

        [TestMethod]
        public void ActionParserSeam_SerializedSubsystemActions_DeserializeIntoStronglyTypedRepresentations() {
            var original = new UnbindVestigeAction(0);
            var serialized = (JObject)original.ToJson();

            var deserialized = SheetAction.FromJson(serialized);

            Assert.IsNotNull(deserialized);
            Assert.IsInstanceOfType(deserialized, typeof(UnbindVestigeAction));
            var unbindAction = (UnbindVestigeAction)deserialized;
            Assert.AreEqual(0, unbindAction.SlotIndex);
            Assert.AreEqual("Unbind vestige slot 0", unbindAction.Name);
            Assert.IsTrue(original.Matches(deserialized));

            var directJson = new JObject(
                new JProperty("name", "Custom Unbind"),
                new JProperty("unbind_vestige", 2)
            );
            var parsedDirect = SheetAction.FromJson(directJson);
            Assert.IsInstanceOfType(parsedDirect, typeof(UnbindVestigeAction));
            Assert.AreEqual(2, ((UnbindVestigeAction)parsedDirect).SlotIndex);
            Assert.AreEqual("Custom Unbind", parsedDirect.Name);
        }

        private class TestSlottedElement {
            public string Name { get; set; }
            public int Level { get; set; }
        }

        [TestMethod]
        public void SubsystemSlotGroup_DefaultMaxLevel_IsIntMaxValue_WhenNoProviderSupplied() {
            var slotGroup = new SubsystemSlotGroup<string>(3);
            Assert.AreEqual(int.MaxValue, slotGroup.MaxLevel);

            var dynamicSlotGroup = new SubsystemSlotGroup<string>(() => 3);
            Assert.AreEqual(int.MaxValue, dynamicSlotGroup.MaxLevel);
        }

        [TestMethod]
        public void SubsystemSlotGroup_IsSlotLevelValid_ReturnsTrue_WhenNoProviderOrSelectorSupplied() {
            var slotGroup = new SubsystemSlotGroup<string>(2);
            slotGroup[0] = "HighLevelThing";
            Assert.IsTrue(slotGroup.IsSlotLevelValid(0));
            Assert.IsTrue(slotGroup.IsSlotLevelValid(1));
        }

        [TestMethod]
        public void SubsystemSlotGroup_IsSlotLevelValid_ValidatesLevelAgainstMaxLevel() {
            var slotGroup = new SubsystemSlotGroup<TestSlottedElement>(
                fixedCapacity: 3,
                maxLevelProvider: () => 3,
                levelSelector: e => e?.Level ?? 0
            );

            Assert.AreEqual(3, slotGroup.MaxLevel);

            // Empty slot returns true
            Assert.IsTrue(slotGroup.IsSlotLevelValid(0));

            // Slotted element <= MaxLevel returns true
            slotGroup[0] = new TestSlottedElement { Name = "Valid", Level = 3 };
            Assert.IsTrue(slotGroup.IsSlotLevelValid(0));

            // Slotted element < MaxLevel returns true
            slotGroup[1] = new TestSlottedElement { Name = "Low", Level = 1 };
            Assert.IsTrue(slotGroup.IsSlotLevelValid(1));

            // Slotted element > MaxLevel returns false
            slotGroup[2] = new TestSlottedElement { Name = "High", Level = 4 };
            Assert.IsFalse(slotGroup.IsSlotLevelValid(2));
        }

        [TestMethod]
        public void SubsystemSlotGroup_OutOfLevelAssignment_PreservesEnableDontEnforceWithoutThrowing() {
            var slotGroup = new SubsystemSlotGroup<TestSlottedElement>(
                fixedCapacity: 2,
                maxLevelProvider: () => 1,
                levelSelector: e => e?.Level ?? 0
            );

            var overleveled = new TestSlottedElement { Name = "Overleveled", Level = 9 };

            // ADR-0001: Assignment must not throw or clamp
            slotGroup[0] = overleveled;
            Assert.AreSame(overleveled, slotGroup[0]);
            Assert.IsFalse(slotGroup.IsSlotLevelValid(0));
        }

        [TestMethod]
        public void SubsystemSlotGroup_EvaluatesLevelProviderDynamically() {
            int currentMaxLevel = 2;
            var slotGroup = new SubsystemSlotGroup<TestSlottedElement>(
                capacityProvider: () => 2,
                maxLevelProvider: () => currentMaxLevel,
                levelSelector: e => e?.Level ?? 0
            );

            slotGroup[0] = new TestSlottedElement { Name = "Item", Level = 3 };

            Assert.AreEqual(2, slotGroup.MaxLevel);
            Assert.IsFalse(slotGroup.IsSlotLevelValid(0));

            // Dynamic progression increases MaxLevel
            currentMaxLevel = 3;
            Assert.AreEqual(3, slotGroup.MaxLevel);
            Assert.IsTrue(slotGroup.IsSlotLevelValid(0));

            // Dynamic regression decreases MaxLevel
            currentMaxLevel = 1;
            Assert.AreEqual(1, slotGroup.MaxLevel);
            Assert.IsFalse(slotGroup.IsSlotLevelValid(0));
        }

        [TestMethod]
        public void SubsystemSlotGroup_IsSlotLevelValid_NegativeSlotIndex_ThrowsArgumentOutOfRangeException() {
            var slotGroup = new SubsystemSlotGroup<TestSlottedElement>(
                fixedCapacity: 2,
                maxLevelProvider: () => 3,
                levelSelector: e => e?.Level ?? 0
            );

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => slotGroup.IsSlotLevelValid(-1));
        }

        [TestMethod]
        public void SubsystemSlotGroup_IsSlotLevelValid_EvaluatesCorrectly_AcrossSlotsBeyondCapacity() {
            int dynamicMaxLevel = 2;
            var slotGroup = new SubsystemSlotGroup<TestSlottedElement>(
                capacityProvider: () => 1,
                maxLevelProvider: () => dynamicMaxLevel,
                levelSelector: e => e?.Level ?? 0
            );

            // Slot 0 (within capacity) has level 2 element -> valid
            slotGroup[0] = new TestSlottedElement { Name = "Valid", Level = 2 };
            Assert.IsTrue(slotGroup.IsSlotLevelValid(0));

            // Slot 1 (beyond capacity) has level 3 element -> invalid (level > max level)
            slotGroup[1] = new TestSlottedElement { Name = "Overleveled", Level = 3 };
            Assert.IsFalse(slotGroup.IsSlotLevelValid(1));

            // Slot 2 (beyond capacity and unassigned) -> valid (empty slot)
            Assert.IsTrue(slotGroup.IsSlotLevelValid(2));

            // Dynamic progression increases MaxLevel: slot 1 becomes valid
            dynamicMaxLevel = 3;
            Assert.IsTrue(slotGroup.IsSlotLevelValid(1));
        }

        // Source: Tome of Battle, pp. 37–46
        [TestMethod]
        public void GameRulesCatalog_CanLoadFullManeuverCatalog_AndResolveAllNineDisciplines() {
            var catalog = new GameRulesCatalog();
            var maneuvers = catalog.Maneuvers.Values.ToList();

            Assert.IsTrue(maneuvers.Count >= 190, $"Expected at least 190 maneuvers, got {maneuvers.Count}");

            string[] disciplines = {
                "Desert Wind", "Devoted Spirit", "Diamond Mind",
                "Iron Heart", "Setting Sun", "Shadow Hand",
                "Stone Dragon", "Tiger Claw", "White Raven"
            };

            foreach (var disc in disciplines) {
                var discManeuvers = maneuvers.Where(m => m.Discipline == disc).ToList();
                Assert.IsTrue(discManeuvers.Count >= 15, $"Discipline '{disc}' should have at least 15 maneuvers, found {discManeuvers.Count}");
                var first = discManeuvers.First();
                var fetched = catalog.GetManeuver(first.Name);
                Assert.IsNotNull(fetched, $"Failed to fetch maneuver '{first.Name}' via catalog");
                Assert.AreEqual(first.Name, fetched.Name);
            }
        }
    }
}

