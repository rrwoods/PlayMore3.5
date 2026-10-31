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
    public class SubsystemEngineTests {
        private static SubsystemEngine CreateTestEngine(string id, string name = null, ICharacterContext context = null) {
            return new SubsystemEngine(new SubsystemDefinition {
                Id = id,
                Name = name ?? id
            }, context ?? new FakeCharacterContext());
        }

        [TestMethod]
        public void SubsystemEngine_Constructor_ThrowsArgumentNullException_OnNullDefinitionOrContext() {
            var validDef = new SubsystemDefinition { Id = "test", Name = "Test" };
            var validContext = new FakeCharacterContext();

            Assert.ThrowsException<ArgumentNullException>(() => new SubsystemEngine(null, validContext));
            Assert.ThrowsException<ArgumentNullException>(() => new SubsystemEngine(validDef, null));
            Assert.ThrowsException<ArgumentNullException>(() => new SubsystemEngine(null, validContext, new Random()));
            Assert.ThrowsException<ArgumentNullException>(() => new SubsystemEngine(validDef, null, new Random()));
        }

        [TestMethod]
        public void SubsystemSlotGroup_Slots_AreGuaranteedNonNullInstances() {
            var group = new SubsystemSlotGroup<SubsystemSlot>(3);
            Assert.AreEqual(3, group.Slots.Count);
            foreach (var slot in group.Slots) {
                Assert.IsNotNull(slot);
                Assert.AreEqual("", slot.Element);
            }

            group.ClearSlot(1);
            Assert.IsNotNull(group[1]);
            Assert.AreEqual("", group[1].Element);
        }

        [TestMethod]
        public void SubsystemSlotGrant_SerializationRoundTrip_AndEquality() {
            var grant = new SubsystemSlotGrant("martial_maneuvers", "maneuvers", "${maneuver}", "${class}");
            Assert.AreEqual("martial_maneuvers", grant.Subsystem);
            Assert.AreEqual("maneuvers", grant.SlotGroup);
            Assert.AreEqual("${maneuver}", grant.Element);
            Assert.AreEqual("${class}", grant.Source);

            var json = (JObject)grant.ToJson();
            Assert.AreEqual("martial_maneuvers", (string)json["subsystem"]);
            Assert.AreEqual("maneuvers", (string)json["slot_group"]);
            Assert.AreEqual("${maneuver}", (string)json["element"]);
            Assert.AreEqual("${class}", (string)json["source"]);

            var deserialized = new SubsystemSlotGrant(json);
            Assert.AreEqual(grant, deserialized);
            Assert.AreEqual(grant.GetHashCode(), deserialized.GetHashCode());

            var applied = grant.ApplyChoice("Warblade");
            Assert.AreEqual("Warblade", applied.Source);
            Assert.AreEqual("${maneuver}", applied.Element);
        }

        [TestMethod]
        public void Effect_Slots_SerializationAndDeserialization() {
            var effect = new Effect("Martial Grant")
                .AddSlot(new SubsystemSlotGrant("martial_maneuvers", "maneuvers", "${maneuver}", "${class}"))
                .AddSlot(new SubsystemSlotGrant("martial_maneuvers", "stances", "${stance}", "${class}"));

            Assert.AreEqual(2, effect.Slots.Count);
            var json = (JObject)effect.ToJson();
            Assert.IsNotNull(json["slots"]);
            var slotsArr = (JArray)json["slots"];
            Assert.AreEqual(2, slotsArr.Count);

            var roundTripped = new Effect(json);
            Assert.AreEqual(2, roundTripped.Slots.Count);
            Assert.AreEqual("martial_maneuvers", roundTripped.Slots[0].Subsystem);
            Assert.AreEqual("maneuvers", roundTripped.Slots[0].SlotGroup);
            Assert.AreEqual("${maneuver}", roundTripped.Slots[0].Element);
            Assert.AreEqual("${class}", roundTripped.Slots[0].Source);

            var applied = roundTripped.ApplyChoice("Crusader");
            Assert.AreEqual(2, applied.Slots.Count);
            Assert.AreEqual("Crusader", applied.Slots[0].Source);
        }

        [TestMethod]
        public void SubsystemRegistry_RegistersAndRetrievesEngines_ById() {
            var registry = new SubsystemRegistry();
            var first = CreateTestEngine("fake-first", "Fake First");
            var simple = CreateTestEngine("fake-simple", "Fake Simple");

            registry.Register(first);
            registry.Register(simple);

            Assert.AreSame(first, registry.Get("fake-first"));
            Assert.AreSame(simple, registry.Get("fake-simple"));

            CollectionAssert.AreEqual(new[] { first, simple }, registry.All.ToList());

            Assert.IsNull(registry.Get("nonexistent"));
            Assert.IsNull(registry.Get(null));
            Assert.IsNull(registry.Get(""));
        }

        [TestMethod]
        public void SubsystemRegistry_ReRegistration_ReplacesExistingEngineInAllAndGet() {
            var registry = new SubsystemRegistry();
            var first = CreateTestEngine("fake-first");
            var replacement = CreateTestEngine("fake-first");

            registry.Register(first);
            Assert.AreSame(first, registry.Get("fake-first"));

            registry.Register(replacement);
            Assert.AreSame(replacement, registry.Get("fake-first"));
            Assert.AreEqual(1, registry.All.Count());
            Assert.AreSame(replacement, registry.All.Single());
        }

        [TestMethod]
        public void SubsystemRegistry_CreateDefault_RegistersStandardEngines() {
            var registry = SubsystemRegistry.CreateDefault();
            var engine = registry.Get("pact_magic");
            Assert.IsNotNull(engine);
            Assert.AreSame(engine, registry.Get("pact_magic"));
            Assert.IsNull(registry.Get("vestige"));
            CollectionAssert.Contains(registry.All.ToList(), engine);

            var maneuverEngine = registry.Get("martial_maneuvers");
            Assert.IsNotNull(maneuverEngine);
            Assert.IsInstanceOfType(maneuverEngine, typeof(SubsystemEngine));
            Assert.AreSame(maneuverEngine, registry.Get("martial_maneuvers"));
            Assert.IsNull(registry.Get("maneuver"));
            CollectionAssert.Contains(registry.All.ToList(), maneuverEngine);
        }

        [TestMethod]
        public void SubsystemRegistry_NewInstance_RemainsEmptyForTestIsolation() {
            var registry = new SubsystemRegistry();
            Assert.AreEqual(0, registry.All.Count());
            Assert.IsNull(registry.Get("pact_magic"));
            Assert.IsNull(registry.Get("martial_maneuvers"));
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
            Assert.IsNotNull(session.Subsystems.Get("pact_magic"));
            Assert.IsNotNull(session.Subsystems.Get("martial_maneuvers"));
            Assert.IsNull(session.Subsystems.Get("vestige"));
            Assert.IsNull(session.Subsystems.Get("maneuver"));
        }

        [TestMethod]
        public void ActionParserSeam_SerializedManeuverActions_DeserializeIntoStronglyTypedRepresentations() {
            var maneuverJson = new JObject(
                new JProperty("name", "Wind Stride"),
                new JProperty("subsystem", "martial_maneuvers"),
                new JProperty("verb", "expend"),
                new JProperty("slot_group", "maneuvers"),
                new JProperty("element", "Wind Stride"),
                new JProperty("source", "Swordsage")
            );
            var deserialized = SheetAction.FromJson(maneuverJson);

            Assert.IsNotNull(deserialized);
            Assert.AreEqual("martial_maneuvers", deserialized.Subsystem);
            Assert.AreEqual("expend", deserialized.Verb);
            Assert.AreEqual("Wind Stride", deserialized.Element);
            Assert.AreEqual("Swordsage", deserialized.Source);
            Assert.AreEqual("Wind Stride", deserialized.Name);
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
            var martialDef = catalog.GetSubsystem("martial_maneuvers");
            Assert.IsNotNull(martialDef);
            var maneuvers = martialDef.Elements.Values.ToList();

            Assert.IsTrue(maneuvers.Count >= 190, $"Expected at least 190 maneuvers, got {maneuvers.Count}");

            string[] disciplines = {
                "Desert Wind", "Devoted Spirit", "Diamond Mind",
                "Iron Heart", "Setting Sun", "Shadow Hand",
                "Stone Dragon", "Tiger Claw", "White Raven"
            };

            foreach (var disc in disciplines) {
                var discManeuvers = maneuvers.Where(m => m.GetProperty<string>("discipline") == disc).ToList();
                Assert.IsTrue(discManeuvers.Count >= 15, $"Discipline '{disc}' should have at least 15 maneuvers, found {discManeuvers.Count}");
                var first = discManeuvers.First();
                var fetched = martialDef.Elements.TryGetValue(first.Name, out var elem) ? elem : null;
                Assert.IsNotNull(fetched, $"Failed to fetch maneuver '{first.Name}' via catalog");
                Assert.AreEqual(first.Name, fetched.Name);
            }
        }

        [TestMethod]
        public void SubsystemDefinition_Throws_WhenLevelGatingStatisticNotFoundInElementStatistics() {
            var json = new JObject(
                new JProperty("subsystem", new JObject(
                    new JProperty("id", "test_subsystem"),
                    new JProperty("name", "Test Subsystem"),
                    new JProperty("level_gating", new JObject(
                        new JProperty("element_statistic", "level")
                    ))
                ))
            );

            var ex = Assert.ThrowsException<InvalidOperationException>(() => new SubsystemDefinition(json));
            StringAssert.Contains(ex.Message, "level gating references element statistic 'level'");
        }

        [TestMethod]
        public void SubsystemDefinition_Throws_WhenLevelGatingStatisticIsEmptyOrMissing() {
            var json = new JObject(
                new JProperty("subsystem", new JObject(
                    new JProperty("id", "test_subsystem"),
                    new JProperty("name", "Test Subsystem"),
                    new JProperty("level_gating", new JObject())
                ))
            );

            var ex = Assert.ThrowsException<InvalidOperationException>(() => new SubsystemDefinition(json));
            StringAssert.Contains(ex.Message, "level gating references element statistic ''");
        }

        // Source: Tome of Magic, p. 11 (Table 1–1: The Binder)
        [TestMethod]
        public void SubsystemDefinition_ParsesLevelGatingAndSlotGroupMaxLevelAndEffects() {
            var json = new JObject(
                new JProperty("subsystem", new JObject(
                    new JProperty("id", "pact_magic"),
                    new JProperty("name", "Pact Magic"),
                    new JProperty("element_statistics", new JObject(
                        new JProperty("level", new JArray())
                    )),
                    new JProperty("level_gating", new JObject(
                        new JProperty("element_statistic", "level")
                    )),
                    new JProperty("slot_groups", new JObject(
                        new JProperty("vestiges", new JObject(
                            new JProperty("max_level", new JObject(
                                new JProperty("stat", "maximum vestige level"),
                                new JProperty("base", 0)
                            )),
                            new JProperty("effects", new JArray(
                                new JObject(
                                    new JProperty("name", "max level effect"),
                                    new JProperty("modifiers", new JArray(
                                        new JObject(
                                            new JProperty("affects", "maximum vestige level"),
                                            new JProperty("mod", new JObject(
                                                new JProperty("type", "untyped"),
                                                new JProperty("amounts", new JArray(
                                                    new JObject(new JProperty("fixed", 5))
                                                ))
                                            ))
                                        )
                                    ))
                                )
                            ))
                        ))
                    ))
                ))
            );

            var def = new SubsystemDefinition(json);
            Assert.IsNotNull(def.LevelGating);
            Assert.AreEqual("level", def.LevelGating.ElementStatistic);
            var group = def.SlotGroups["vestiges"];
            Assert.IsNotNull(group.MaxLevel);
            Assert.AreEqual("maximum vestige level", group.MaxLevel.Stat);
            Assert.AreEqual(0, group.MaxLevel.Base);
            Assert.AreEqual(1, group.Effects.Count);
            Assert.AreEqual("max level effect", group.Effects[0].Name);

            var serialized = (JObject)def.ToJson();
            var roundTrip = new SubsystemDefinition(serialized);
            Assert.AreEqual("level", roundTrip.LevelGating.ElementStatistic);
            Assert.AreEqual("maximum vestige level", roundTrip.SlotGroups["vestiges"].MaxLevel.Stat);
            Assert.AreEqual(1, roundTrip.SlotGroups["vestiges"].Effects.Count);
        }

        // Source: Tome of Magic, p. 11 (Table 1–1: The Binder)
        [TestMethod]
        public void SubsystemEngine_ContributeEffects_RegistersSlotGroupMaxLevelAndContributesSlotGroupEffects() {
            var def = new SubsystemDefinition {
                Id = "pact_magic",
                Name = "Pact Magic"
            };
            var group = new SlotGroupDefinition {
                Name = "vestiges",
                MaxLevel = new MaxLevelDefinition("maximum vestige level", 0)
            };
            var maxLevelEffect = new Effect("Max Vestige Level Progression")
                .AddModifier("maximum vestige level", new Modifier(new LookupAmount("EBL for max vestige level", new Dictionary<int, int> { { 1, 1 }, { 3, 2 } }, -1, 0), ModifierTypes.UNTYPED));
            group.Effects.Add(maxLevelEffect);
            def.SlotGroups["vestiges"] = group;

            var character = new Character { race = "human" };
            var session = new CharacterSession(character, new GameRulesCatalog());
            var engine = new SubsystemEngine(def, session);
            var pool = new EffectPool(character);
            pool.AddStat(new Statistic("EBL for max vestige level"));
            pool.AddEffect(new Effect("EBL").AddModifier("EBL for max vestige level", new Modifier(3, ModifierTypes.BASE)));

            engine.ContributeEffects(pool);

            Assert.IsTrue(pool.stats.ContainsKey("maximum vestige level"), "Slot group max level stat should be registered in pool.");
            Assert.AreEqual(2, pool.GetStatValue("maximum vestige level", null), "Max level stat should be modified by slot group effects.");
        }

        // Source: Tome of Magic, p. 11 (Table 1–1: The Binder)
        [TestMethod]
        public void SubsystemEngine_GetAccessibleElements_And_IsSlotLevelValid_GatesDynamicallyBySlotGroupMaxLevel() {
            var def = new SubsystemDefinition {
                Id = "pact_magic",
                Name = "Pact Magic"
            };
            def.ElementStatistics["level"] = new List<string>();
            def.LevelGating = new LevelGatingDefinition("level");
            var group = new SlotGroupDefinition {
                Name = "vestiges",
                FixedCapacity = 2,
                MaxLevel = new MaxLevelDefinition("maximum vestige level", 1)
            };
            def.SlotGroups["vestiges"] = group;

            var elem1 = new SubsystemElement();
            elem1.Name = "Amon";
            elem1.SetProperty("level", 1);
            def.Elements["Amon"] = elem1;

            var elem2 = new SubsystemElement();
            elem2.Name = "Savnok";
            elem2.SetProperty("level", 2);
            def.Elements["Savnok"] = elem2;

            var character = new Character { race = "human" };
            var session = new CharacterSession(character, new GameRulesCatalog());
            ICharacterContext ctx = session;
            ctx.Effects.AddStat(new Statistic("maximum vestige level"));
            ctx.Effects.AddEffect(new Effect("Base Max Level").AddModifier("maximum vestige level", new Modifier(1, ModifierTypes.BASE)));
            var engine = new SubsystemEngine(def, session);

            // With max vestige level = 1:
            var accessible = engine.GetAccessibleElements(null, null);
            CollectionAssert.Contains(accessible.ToList(), "Amon");
            CollectionAssert.DoesNotContain(accessible.ToList(), "Savnok");

            // Assign slot 0 to Amon (level 1 <= 1) -> valid
            engine.AssignSlot("vestiges", 0, "Amon", "good_pact");
            Assert.IsTrue(engine.IsSlotLevelValid("vestiges", 0));

            // Assign slot 1 to Savnok (level 2 > 1) -> invalid
            engine.AssignSlot("vestiges", 1, "Savnok", "good_pact");
            Assert.IsFalse(engine.IsSlotLevelValid("vestiges", 1));

            // Increase maximum vestige level to 2 dynamically
            ctx.Effects.AddEffect(new Effect("Boost Max Level").AddModifier("maximum vestige level", new Modifier(1, ModifierTypes.UNTYPED)));
            accessible = engine.GetAccessibleElements(null, null);
            CollectionAssert.Contains(accessible.ToList(), "Amon");
            CollectionAssert.Contains(accessible.ToList(), "Savnok");
            Assert.IsTrue(engine.IsSlotLevelValid("vestiges", 1));
        }

        // Source: Tome of Magic, p. 11 (Table 1–1: The Binder)
        [TestMethod]
        public void SubsystemEngine_ValidateChoice_EmitsAdvisoryWarning_WhenElementExceedsMaxLevel() {
            var def = new SubsystemDefinition {
                Id = "pact_magic",
                Name = "Pact Magic"
            };
            def.ElementStatistics["level"] = new List<string>();
            def.LevelGating = new LevelGatingDefinition("level");
            var group = new SlotGroupDefinition {
                Name = "vestiges",
                FixedCapacity = 1,
                MaxLevel = new MaxLevelDefinition("maximum vestige level", 1)
            };
            def.SlotGroups["vestiges"] = group;

            var elem1 = new SubsystemElement();
            elem1.Name = "Amon";
            elem1.SetProperty("level", 1);
            def.Elements["Amon"] = elem1;

            var elem2 = new SubsystemElement();
            elem2.Name = "Savnok";
            elem2.SetProperty("level", 2);
            def.Elements["Savnok"] = elem2;

            var character = new Character { race = "human" };
            var session = new CharacterSession(character, new GameRulesCatalog());
            ICharacterContext ctx = session;
            ctx.Effects.AddStat(new Statistic("maximum vestige level"));
            ctx.Effects.AddEffect(new Effect("Base Max Level").AddModifier("maximum vestige level", new Modifier(1, ModifierTypes.BASE)));
            var engine = new SubsystemEngine(def, session);

            var descriptor = new ChoiceDescriptor("vestige", "Choose Vestige", "SubsystemElements", "pact_magic");

            // Amon is level 1 <= 1 -> no warnings
            var warningsAmon = engine.ValidateChoice("feat_0", descriptor, "Amon", "Binder");
            Assert.AreEqual(0, warningsAmon.Count);

            // Savnok is level 2 > 1 -> advisory warning
            var warningsSavnok = engine.ValidateChoice("feat_0", descriptor, "Savnok", "Binder");
            Assert.AreEqual(1, warningsSavnok.Count);
            StringAssert.Contains(warningsSavnok[0], "Level 2 exceeds maximum 1 for Binder.");

            // Unknown element -> invalid element warning
            var warningsUnknown = engine.ValidateChoice("feat_0", descriptor, "Nonexistent", "Binder");
            Assert.AreEqual(1, warningsUnknown.Count);
            StringAssert.Contains(warningsUnknown[0], "'Nonexistent' is not a valid element in subsystem 'Pact Magic'.");
        }

        [TestMethod]
        public void SubsystemDefinition_EffectTemplates_RoundTripsAndResolvesElementEffectsWithMergedText() {
            var json = JObject.Parse(@"{
              ""subsystem"": {
                ""id"": ""test_subsystem"",
                ""name"": ""Test Subsystem"",
                ""effect_templates"": {
                  ""custom_stance"": {
                    ""name"": ""${element} Stance"",
                    ""parameter"": ""active"",
                    ""actions"": [
                      {
                        ""name"": ""Toggle ${element} Stance"",
                        ""adjust"": {
                          ""${element} Stance active"": 1
                        }
                      }
                    ]
                  }
                }
              },
              ""elements"": [
                {
                  ""name"": ""Iron Shell"",
                  ""effects"": {
                    ""custom_stance"": [
                      {
                        ""text"": [
                          {
                            ""category"": ""Stance"",
                            ""text"": ""You gain resistance while in Iron Shell stance.""
                          }
                        ]
                      }
                    ]
                  }
                }
              ]
            }");

            var def = new SubsystemDefinition(json);
            Assert.IsTrue(def.EffectTemplates.ContainsKey("custom_stance"));
            Assert.IsNotNull(def.EffectTemplates["custom_stance"]);

            // Round trip test
            var roundTripped = new SubsystemDefinition((JObject)def.ToJson());
            Assert.IsTrue(roundTripped.EffectTemplates.ContainsKey("custom_stance"));

            var element = def.Elements["Iron Shell"];
            var effects = def.GetElementEffects(element, "custom_stance").ToList();
            Assert.AreEqual(1, effects.Count);

            var effect = effects[0];
            Assert.AreEqual("Iron Shell Stance", effect.Name);
            Assert.AreEqual("Iron Shell Stance active", effect.ParameterName);
            Assert.AreEqual(1, effect.associatedAction.Count);
            Assert.AreEqual("Toggle Iron Shell Stance", effect.associatedAction[0].Name);

            // Merged prompt text from the element
            Assert.AreEqual(1, effect.Text.Count);
            Assert.AreEqual("Stance", effect.Text[0].Category);
            Assert.AreEqual("You gain resistance while in Iron Shell stance.", effect.Text[0].Description);

            // Parameter names discovery
            var paramNames = def.GetElementParameterNames(element).ToList();
            CollectionAssert.Contains(paramNames, "Iron Shell Stance active");
        }

        [TestMethod]
        public void SubsystemEngine_ContributeEffects_ProjectsTemplateEffectsAndResetsParametersGenerically() {
            var json = JObject.Parse(@"{
              ""subsystem"": {
                ""id"": ""synthetic"",
                ""name"": ""Synthetic Subsystem"",
                ""slot_groups"": {
                  ""stances"": {
                    ""states"": [""inactive"", ""active""],
                    ""default_state"": ""inactive""
                  }
                },
                ""projections"": [
                  {
                    ""slot_group"": ""stances"",
                    ""state"": ""active"",
                    ""effects"": [""stance_effect""]
                  }
                ],
                ""effect_templates"": {
                  ""stance_effect"": {
                    ""name"": ""${element} Stance"",
                    ""parameter"": ""active"",
                    ""actions"": [
                      {
                        ""name"": ""Toggle ${element} Stance"",
                        ""adjust"": {
                          ""${element} Stance active"": 1
                        }
                      }
                    ],
                    ""modifiers"": [
                      {
                        ""affects"": ""Fort saves"",
                        ""mod"": {
                          ""type"": ""untyped"",
                          ""amounts"": [
                            {
                              ""multiply"": {
                                ""variable"": {
                                  ""controllable"": ""${element} Stance active""
                                },
                                ""scalar"": -1
                              }
                            }
                          ]
                        }
                      }
                    ]
                  }
                }
              },
              ""elements"": [
                {
                  ""name"": ""Iron Shell"",
                  ""effects"": {
                    ""stance_effect"": [
                      {
                        ""text"": [
                          {
                            ""category"": ""Stance"",
                            ""text"": ""You gain resistance in Iron Shell stance.""
                          }
                        ]
                      }
                    ]
                  }
                }
              ]
            }");

            var def = new SubsystemDefinition(json);
            var character = new Character { race = "human" };
            var session = new CharacterSession(character, new GameRulesCatalog());
            var engine = new SubsystemEngine(def, session);
            session.Subsystems.Register(engine);

            // Inactive slot -> no projection
            engine.AssignSlot("stances", 0, "Iron Shell", "inactive");
            session.Recalculate();
            var ctx = (ICharacterContext)session;
            Assert.IsFalse(ctx.Effects.Effects.Any(e => e.Name == "Iron Shell Stance"));

            // Active slot -> generic projection of template effect
            engine.SetSlotState("stances", 0, "active");
            session.Recalculate();
            Assert.IsTrue(ctx.Effects.Effects.Any(e => e.Name == "Iron Shell Stance"));
            Assert.IsTrue(session.GetControllableParameters().Any(p => p.Name == "Iron Shell Stance active"));
            int baseFort = session.GetStatistic(PlayMore35.Effects.Constants.FORT_SAVES).Value;

            // Parameter value scaling
            session.SetParameter("Iron Shell Stance active", 3);
            session.Recalculate();
            Assert.AreEqual(baseFort - 3, session.GetStatistic(PlayMore35.Effects.Constants.FORT_SAVES).Value);

            // Parameter reset clears parameter on the character and effects
            engine.ResetElementParameters("Iron Shell");
            session.Recalculate();
            Assert.AreEqual(0, session.GetControllableParameters().First(p => p.Name == "Iron Shell Stance active").Value);
            Assert.AreEqual(baseFort, session.GetStatistic(PlayMore35.Effects.Constants.FORT_SAVES).Value);
        }

        [TestMethod]
        public void SubsystemEngine_HasNoProjectFeatSlotsMethod() {
            var method = typeof(SubsystemEngine).GetMethod("ProjectFeatSlots", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static);
            Assert.IsNull(method, "SubsystemEngine must have zero knowledge of feats and no ProjectFeatSlots method.");
        }

        [TestMethod]
        public void SubsystemEngine_GrantSlot_And_PruneStaleGrantedSlots_OperateDeterministically() {
            var catalog = new GameRulesCatalog();
            var subDef = catalog.GetSubsystem("martial_maneuvers");
            Assert.IsNotNull(subDef);

            var character = new Character { race = "Human" };
            character.classLevels.Add("Warblade");
            var session = new CharacterSession(character, catalog);
            var engine = (SubsystemEngine)session.Subsystems.Get("martial_maneuvers");
            Assert.IsNotNull(engine);

            // Grant slot for Warblade
            engine.GrantSlot("maneuvers", "Steel Wind", "Warblade", "Level 1");
            var slots = engine.GetSlots("maneuvers", "Warblade");
            var granted = slots.FirstOrDefault(s => s != null && s.IsFeatBound);
            Assert.IsNotNull(granted);
            Assert.AreEqual("Steel Wind", granted.Element);
            Assert.AreEqual("Level 1", granted.FeatSlot);

            // Update granted slot element
            engine.GrantSlot("maneuvers", "Mountain Hammer", "Warblade", "Level 1");
            slots = engine.GetSlots("maneuvers", "Warblade");
            granted = slots.FirstOrDefault(s => s != null && s.IsFeatBound);
            Assert.IsNotNull(granted);
            Assert.AreEqual("Mountain Hammer", granted.Element);

            // Pruning removes when not active
            engine.PruneStaleGrantedSlots(Enumerable.Empty<(string, string, string)>());
            slots = engine.GetSlots("maneuvers", "Warblade");
            Assert.IsFalse(slots.Any(s => s != null && s.IsFeatBound));
        }

        [TestMethod]
        public void CharacterSession_MartialStudyAndStance_AssignSlotsWithoutEngineFeatInspection() {
            var catalog = new GameRulesCatalog();
            var character = new Character { race = "Human" };
            character.classLevels.Clear();
            character.classLevels.Add("Warblade");
            character.classLevels.Add("Warblade");
            character.classLevels.Add("Warblade");
            character.feats["Level 1"] = "Martial Study";
            character.featChoices["Level 1:maneuver"] = "Steel Wind";
            character.feats["Level 3"] = "Martial Stance";
            character.featChoices["Level 3:stance"] = "Punishing Stance";

            var session = new CharacterSession(character, catalog);
            var engine = (SubsystemEngine)session.Subsystems.Get("martial_maneuvers");
            Assert.IsNotNull(engine);

            var maneuvers = engine.GetSlots("maneuvers", "Warblade");
            var maneuverSlot = maneuvers.FirstOrDefault(s => s != null && s.IsFeatBound);
            Assert.IsNotNull(maneuverSlot);
            Assert.AreEqual("Steel Wind", maneuverSlot.Element);
            Assert.AreEqual("Level 1", maneuverSlot.FeatSlot);

            var stances = engine.GetSlots("stances", "Warblade");
            var stanceSlot = stances.FirstOrDefault(s => s != null && s.IsFeatBound);
            Assert.IsNotNull(stanceSlot);
            Assert.AreEqual("Punishing Stance", stanceSlot.Element);
            Assert.AreEqual("Level 3", stanceSlot.FeatSlot);
        }
    }
}

