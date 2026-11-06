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
            return new SubsystemEngine(TestFactory.CreateSubsystemDefinition(
                id: id,
                name: name ?? id
            ), context ?? new FakeCharacterContext());
        }

        [TestMethod]
        public void SubsystemEngine_Constructor_ThrowsArgumentNullException_OnNullDefinitionOrContext() {
            var validDef = TestFactory.CreateSubsystemDefinition("test", "Test");
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
        public void CharacterSession_InitializesSubsystemsWithDefaults() {
            var session = new CharacterSession(new Character(), new PlayMore35.Rules.GameRulesCatalog());
            Assert.AreEqual(2, session.Subsystems.Count());
            Assert.IsNotNull(session.GetSubsystem("pact_magic"));
            Assert.IsNotNull(session.GetSubsystem("martial_maneuvers"));
            Assert.IsNull(session.GetSubsystem("vestige"));
            Assert.IsNull(session.GetSubsystem("maneuver"));
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
            var def = TestFactory.CreateSubsystemDefinition("pact_magic", "Pact Magic");
            var group = TestFactory.CreateSlotGroupDefinition(
                name: "vestiges",
                maxLevel: TestFactory.CreateMaxLevelDefinition("maximum vestige level", 0)
            );
            var maxLevelEffect = new Effect("Max Vestige Level Progression")
                .AddModifier("maximum vestige level", TestFactory.CreateModifier(TestFactory.CreateLookupAmount("EBL for max vestige level", new Dictionary<int, int> { { 1, 1 }, { 3, 2 } }, -1, 0), ModifierTypes.UNTYPED));
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
        public void SubsystemEngine_IsSlotLevelValid_GatesDynamicallyBySlotGroupMaxLevel() {
            var def = TestFactory.CreateSubsystemDefinition(
                id: "pact_magic",
                name: "Pact Magic",
                levelGating: TestFactory.CreateLevelGatingDefinition("level")
            );
            var group = TestFactory.CreateSlotGroupDefinition(
                name: "vestiges",
                fixedCapacity: 2,
                maxLevel: TestFactory.CreateMaxLevelDefinition("maximum vestige level", 1)
            );
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
            Assert.AreEqual(0, engine.ValidateChoice(null, elem1.Name).Count);
            Assert.IsTrue(engine.ValidateChoice(null, elem2.Name).Any(w => w.Category == ValidationCategory.LevelGate));

            // Assign slot 0 to Amon (level 1 <= 1) -> valid
            engine.AssignSlot("vestiges", 0, "Amon", "good_pact");
            Assert.IsTrue(engine.IsSlotLevelValid("vestiges", 0));

            // Assign slot 1 to Savnok (level 2 > 1) -> invalid
            engine.AssignSlot("vestiges", 1, "Savnok", "good_pact");
            Assert.IsFalse(engine.IsSlotLevelValid("vestiges", 1));

            // Increase maximum vestige level to 2 dynamically
            ctx.Effects.AddEffect(new Effect("Boost Max Level").AddModifier("maximum vestige level", new Modifier(1, ModifierTypes.UNTYPED)));
            Assert.AreEqual(0, engine.ValidateChoice(null, elem2.Name).Count);
            Assert.IsTrue(engine.IsSlotLevelValid("vestiges", 1));
        }

        // Source: Tome of Magic, p. 11 (Table 1–1: The Binder)
        [TestMethod]
        public void SubsystemEngine_ValidateChoice_EmitsAdvisoryWarning_WhenElementExceedsMaxLevel() {
            var def = TestFactory.CreateSubsystemDefinition(
                id: "pact_magic",
                name: "Pact Magic",
                levelGating: TestFactory.CreateLevelGatingDefinition("level")
            );
            var group = TestFactory.CreateSlotGroupDefinition(
                name: "vestiges",
                fixedCapacity: 1,
                maxLevel: TestFactory.CreateMaxLevelDefinition("maximum vestige level", 1)
            );
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
            var warningsAmon = engine.ValidateChoice(descriptor, "Amon", "Binder");
            Assert.AreEqual(0, warningsAmon.Count);

            // Savnok is level 2 > 1 -> advisory warning
            var warningsSavnok = engine.ValidateChoice(descriptor, "Savnok", "Binder");
            Assert.AreEqual(1, warningsSavnok.Count);
            Assert.AreEqual(ValidationCategory.LevelGate, warningsSavnok[0].Category);
            StringAssert.Contains(warningsSavnok[0].Message, "Level 2 exceeds maximum 1 for Binder.");

            // Unknown element -> invalid element warning
            var warningsUnknown = engine.ValidateChoice(descriptor, "Nonexistent", "Binder");
            Assert.AreEqual(1, warningsUnknown.Count);
            Assert.AreEqual(ValidationCategory.Invalid, warningsUnknown[0].Category);
            StringAssert.Contains(warningsUnknown[0].Message, "'Nonexistent' is not a valid element in subsystem 'Pact Magic'.");
        }

        // Source: Tome of Battle, pp. 43–44 (Stances and Maneuver types)
        [TestMethod]
        public void SubsystemEngine_ValidateChoice_EmitsInvalidWarning_WhenElementDoesNotMatchFilter() {
            var def = TestFactory.CreateSubsystemDefinition("martial_maneuvers", "Martial Maneuvers");
            var strike = new SubsystemElement { Name = "Steel Strike", Type = "Strike" };
            var stance = new SubsystemElement { Name = "Punishing Stance", Type = "Stance" };
            def.Elements["Steel Strike"] = strike;
            def.Elements["Punishing Stance"] = stance;

            var engine = new SubsystemEngine(def, new FakeCharacterContext());
            var stanceDescriptor = new ChoiceDescriptor("stance", "Choose Stance", "SubsystemElements", filter: "Stance");

            var warnings = engine.ValidateChoice(stanceDescriptor, "Steel Strike", "Warblade");
            Assert.AreEqual(1, warnings.Count);
            Assert.AreEqual(ValidationCategory.Invalid, warnings[0].Category);
            StringAssert.Contains(warnings[0].Message, "'Steel Strike' is not a valid Stance in subsystem 'Martial Maneuvers'.");
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
            var catalog = new FakeRulesCatalog(inner: new GameRulesCatalog());
            catalog.SubsystemsMap[def.Id] = def;
            var character = new Character { race = "human" };
            var session = new CharacterSession(character, catalog);
            var engine = session.GetSubsystem(def.Id)!;

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
            var engine = session.GetSubsystem("martial_maneuvers")!;
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
            var engine = session.GetSubsystem("martial_maneuvers")!;
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

        [TestMethod]
        public void CharacterSession_IsClassEligible_ChecksTags_Subsystems_AndName() {
            var catalog = new GameRulesCatalog();
            var character = new Character { race = "Human" };
            var session = new CharacterSession(character, catalog);

            Assert.IsTrue(session.IsClassEligible("Warblade", "adept"));
            Assert.IsFalse(session.IsClassEligible("Fighter", "adept"));

            Assert.IsTrue(session.IsClassEligible("Warblade", "martial_maneuvers"));
            Assert.IsFalse(session.IsClassEligible("Fighter", "martial_maneuvers"));

            Assert.IsTrue(session.IsClassEligible("Fighter", "Fighter"));
            Assert.IsFalse(session.IsClassEligible("Fighter", "Wizard"));

            Assert.IsTrue(session.IsClassEligible("Fighter", ""));
            Assert.IsTrue(session.IsClassEligible("Fighter", null));
        }

        [TestMethod]
        public void CharacterSession_And_ICharacterSession_HaveNoIsAdeptClassMethod() {
            var sessionMethod = typeof(CharacterSession).GetMethod("IsAdeptClass", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNull(sessionMethod, "CharacterSession must not define IsAdeptClass.");

            var interfaceMethod = typeof(ICharacterSession).GetMethod("IsAdeptClass", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            Assert.IsNull(interfaceMethod, "ICharacterSession must not define IsAdeptClass.");
        }

        [TestMethod]
        public void SubsystemEngine_And_CharacterSession_HaveNoGetAccessibleElementsMethods() {
            var engineMethod = typeof(SubsystemEngine).GetMethod("GetAccessibleElements", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNull(engineMethod, "SubsystemEngine must not define GetAccessibleElements.");

            var sessionMethod = typeof(CharacterSession).GetMethod("GetAccessibleSubsystemElements", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNull(sessionMethod, "CharacterSession must not define GetAccessibleSubsystemElements.");

            var interfaceMethod = typeof(ICharacterSession).GetMethod("GetAccessibleSubsystemElements", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            Assert.IsNull(interfaceMethod, "ICharacterSession must not define GetAccessibleSubsystemElements.");
        }

        [TestMethod]
        public void SubsystemEngine_HasNoInitiatorLevelFormulas() {
            var ilMethod = typeof(SubsystemEngine).GetMethod("GetInitiatorLevelAtCharacterLevel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNull(ilMethod, "SubsystemEngine must not hardcode GetInitiatorLevelAtCharacterLevel.");

            var evalMethod = typeof(SubsystemEngine).GetMethod("EvaluateMaxLevelForInitiatorLevel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNull(evalMethod, "SubsystemEngine must not hardcode EvaluateMaxLevelForInitiatorLevel.");
        }

        [TestMethod]
        public void SubsystemEngine_GetActiveSources_DiscoversFromClassesAndFeats() {
            var catalog = new GameRulesCatalog();

            // 1. Pure Fighter with no subsystem feats -> zero sources
            var fighterChar = new Character { race = "Human" };
            fighterChar.classLevels.Add("Fighter");
            var fighterSession = new CharacterSession(fighterChar, catalog);
            var fighterEngine = fighterSession.GetSubsystem("martial_maneuvers")!;
            var fighterSources = fighterEngine.GetActiveSources();
            Assert.AreEqual(0, fighterSources.Count);

            // 2. Pure Fighter with Martial Study -> discovers "Feats" source
            fighterChar.feats["Level 1"] = "Martial Study";
            fighterSession.Recalculate();
            fighterSources = fighterEngine.GetActiveSources();
            Assert.AreEqual(1, fighterSources.Count);
            Assert.AreEqual("Feats", fighterSources[0]);

            // 3. Warblade with no feats -> discovers "Warblade" source
            var warbladeChar = new Character { race = "Human" };
            warbladeChar.classLevels.Clear();
            warbladeChar.classLevels.Add("Warblade");
            var warbladeSession = new CharacterSession(warbladeChar, catalog);
            var warbladeEngine = warbladeSession.GetSubsystem("martial_maneuvers")!;
            var warbladeSources = warbladeEngine.GetActiveSources();
            Assert.AreEqual(1, warbladeSources.Count);
            Assert.AreEqual("Warblade", warbladeSources[0]);

            // 4. Warblade with Martial Study assigned to Warblade -> only "Warblade" source
            warbladeChar.feats["Level 1"] = "Martial Study";
            warbladeChar.featChoices["Level 1:class"] = "Warblade";
            warbladeSession.Recalculate();
            warbladeSources = warbladeEngine.GetActiveSources();
            Assert.AreEqual(1, warbladeSources.Count);
            Assert.AreEqual("Warblade", warbladeSources[0]);
        }

        // Source: Tome of Battle, p. 52
        [TestMethod]
        public void SubsystemEngine_ValidateChoice_WorksPurelyThroughDeclarativeActiveSlotState() {
            var catalog = new GameRulesCatalog();
            var martialDef = catalog.GetSubsystem("martial_maneuvers");
            var context = new FakeCharacterContext(catalog: catalog);
            var engine = new SubsystemEngine(martialDef, context);

            var stanceDescriptor = new ChoiceDescriptor("stance", "Choose Stance", "martial_maneuvers", filter: "Stance", minimumPrerequisiteCount: 1);

            // Without any active slots, selecting Flame's Blessing (Desert Wind stance) requires 1 Desert Wind maneuver
            var warningsBefore = engine.ValidateChoice(stanceDescriptor, "Flame's Blessing", "Feats");
            Assert.AreEqual(1, warningsBefore.Count);
            Assert.AreEqual(ValidationCategory.Prerequisite, warningsBefore[0].Category);
            StringAssert.Contains(warningsBefore[0].Message, "Requires 1 Desert Wind maneuver, but only 0 known.");

            // Directly grant a Desert Wind maneuver slot to the engine (declarative active slot state)
            engine.GrantSlot("maneuvers", "Burning Blade", "Feats", "Level 1");

            // Now validating Flame's Blessing purely checks slotGroups and satisfies prerequisite without any temporal slicing or character feats lookup
            var warningsAfter = engine.ValidateChoice(stanceDescriptor, "Flame's Blessing", "Feats");
            Assert.AreEqual(0, warningsAfter.Count, "Active slot in engine must satisfy discipline prerequisite purely through declarative slot state.");
        }

        // Source: Tome of Battle, p. 52
        [TestMethod]
        public void CharacterSession_MartialStudyAndStance_PrerequisiteValidation_IdenticalOnLiveSessionAndSnapshots() {
            var catalog = new GameRulesCatalog();
            var martialStance = catalog.GetFeat("Martial Stance");
            var stanceDescriptor = martialStance.Choices.First(c => c.Id == "stance");

            var character = new Character { race = "Human" };
            character.classLevels.Clear();
            character.classLevels.Add("Fighter");
            character.classLevels.Add("Fighter");
            character.classLevels.Add("Fighter");
            character.feats["Level 1"] = "Martial Study";
            character.featChoices["Level 1:maneuver"] = "Burning Blade";
            character.feats["Level 3"] = "Martial Stance";

            var liveSession = new CharacterSession(character, catalog);
            var snapSession = liveSession.GetSessionAtLevel(3);

            var context = new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Flame's Blessing",
                slotKey: "Level 3",
                targetSource: "Feats",
                descriptor: stanceDescriptor
            );

            var liveWarnings = liveSession.ValidateProgressionChoice(context);
            var snapWarnings = snapSession.ValidateProgressionChoice(context);

            Assert.AreEqual(0, liveWarnings.Count, "Live session must pass prerequisite validation.");
            Assert.AreEqual(0, snapWarnings.Count, "Snapshot session must pass prerequisite validation identically to live session.");
        }

        // Source: Tome of Battle, p. 52
        [TestMethod]
        public void CharacterSession_MartialStance_PrerequisiteValidation_FailsOnSnapshotPriorToStudyAcquisition() {
            var catalog = new GameRulesCatalog();
            var martialStance = catalog.GetFeat("Martial Stance");
            var stanceDescriptor = martialStance.Choices.First(c => c.Id == "stance");

            var character = new Character { race = "Human" };
            character.classLevels.Clear();
            character.classLevels.Add("Fighter");
            character.classLevels.Add("Fighter");
            character.classLevels.Add("Fighter");
            character.feats["Level 1"] = "Martial Stance";
            character.feats["Level 3"] = "Martial Study";
            character.featChoices["Level 3:maneuver"] = "Burning Blade";

            var liveSession = new CharacterSession(character, catalog);

            var context = new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Flame's Blessing",
                slotKey: "Level 1",
                targetSource: "Feats",
                descriptor: stanceDescriptor
            );

            var warnings = liveSession.ValidateProgressionChoice(context);
            Assert.AreEqual(1, warnings.Count, "Validating Level 1 feat choice must fail because prerequisite feat was acquired at Level 3.");
            Assert.AreEqual(ValidationCategory.Prerequisite, warnings[0].Category);
            StringAssert.Contains(warnings[0].Message, "Requires 1 Desert Wind maneuver, but only 0 known.");
        }

        [TestMethod]
        public void SubsystemDefinition_PrerequisiteProperty_SerializationRoundTrip() {
            var def = TestFactory.CreateSubsystemDefinition(
                id: "shadow_magic",
                name: "Shadow Magic",
                prerequisiteProperty: "path",
                prerequisiteUnit: "mystery"
            );

            var json = (JObject)def.ToJson();
            var subJson = (JObject?)json["subsystem"];
            Assert.IsNotNull(subJson);
            Assert.AreEqual("path", (string?)subJson["prerequisite_property"]);
            Assert.AreEqual("mystery", (string?)subJson["prerequisite_unit"]);

            var deserialized = new SubsystemDefinition(json);
            Assert.AreEqual("path", deserialized.PrerequisiteProperty);
            Assert.AreEqual("mystery", deserialized.PrerequisiteUnit);

            // Null / omitted property round-trip
            var defOmitted = TestFactory.CreateSubsystemDefinition(id: "psionics", name: "Psionics");
            var jsonOmitted = (JObject)defOmitted.ToJson();
            var subJsonOmitted = (JObject?)jsonOmitted["subsystem"];
            Assert.IsNotNull(subJsonOmitted);
            Assert.IsNull(subJsonOmitted["prerequisite_property"]);

            var deserializedOmitted = new SubsystemDefinition(jsonOmitted);
            Assert.IsNull(deserializedOmitted.PrerequisiteProperty);
        }

        [TestMethod]
        public void ManeuversResource_DeclaresPrerequisiteProperty() {
            var catalog = new GameRulesCatalog();
            var def = catalog.GetSubsystem("martial_maneuvers");
            Assert.IsNotNull(def);
            Assert.AreEqual("discipline", def.PrerequisiteProperty);
            Assert.AreEqual("maneuver", def.PrerequisiteUnit);
        }

        [TestMethod]
        public void SubsystemEngine_CountSamePropertyKnown_PartitionsByConfiguredProperty() {
            var def = TestFactory.CreateSubsystemDefinition(
                id: "test_magic",
                name: "Test Magic",
                prerequisiteProperty: "discipline"
            );
            var group = TestFactory.CreateSlotGroupDefinition(
                name: "powers",
                fixedCapacity: 5
            );
            def.SlotGroups["powers"] = group;

            var elem1 = new SubsystemElement { Name = "Power A" };
            elem1.SetProperty("discipline", "Psychokinesis");
            var elem2 = new SubsystemElement { Name = "Power B" };
            elem2.SetProperty("discipline", "Psychokinesis");
            var elem3 = new SubsystemElement { Name = "Power C" };
            elem3.SetProperty("discipline", "Telepathy");

            def.Elements["Power A"] = elem1;
            def.Elements["Power B"] = elem2;
            def.Elements["Power C"] = elem3;

            var engine = new SubsystemEngine(def, new FakeCharacterContext());
            engine.AssignSlot("powers", 0, "Power A");
            engine.AssignSlot("powers", 1, "Power B");
            engine.AssignSlot("powers", 2, "Power C");

            // Counts for Psychokinesis
            Assert.AreEqual(2, engine.CountSamePropertyKnown("discipline", "Psychokinesis"));
            // Counts for Telepathy
            Assert.AreEqual(1, engine.CountSamePropertyKnown("discipline", "Telepathy"));
            // Counts for unknown discipline
            Assert.AreEqual(0, engine.CountSamePropertyKnown("discipline", "Clairsentience"));

            // Null or empty property name -> counts all known distinct elements across slot groups
            Assert.AreEqual(3, engine.CountSamePropertyKnown(null, null));
            Assert.AreEqual(3, engine.CountSamePropertyKnown("", ""));

            // Exclude element name
            Assert.AreEqual(1, engine.CountSamePropertyKnown("discipline", "Psychokinesis", excludeElementName: "Power A"));
            Assert.AreEqual(2, engine.CountSamePropertyKnown(null, null, excludeElementName: "Power A"));
        }

        [TestMethod]
        public void SubsystemEngine_ValidateChoice_DynamicPrerequisitePropertyAndWarningFormatting() {
            var def = TestFactory.CreateSubsystemDefinition(
                id: "shadow_magic",
                name: "Shadow Magic",
                prerequisiteProperty: "path",
                prerequisiteUnit: "mystery"
            );
            var group = TestFactory.CreateSlotGroupDefinition(
                name: "mysteries",
                fixedCapacity: 5
            );
            def.SlotGroups["mysteries"] = group;

            var m1 = new SubsystemElement { Name = "Shadow Skin" };
            m1.SetProperty("path", "Cloak of Shadows");
            m1.SetProperty("prerequisiteCount", 0);

            var m2 = new SubsystemElement { Name = "Shadow Veil" };
            m2.SetProperty("path", "Cloak of Shadows");
            m2.SetProperty("prerequisiteCount", 1);

            def.Elements["Shadow Skin"] = m1;
            def.Elements["Shadow Veil"] = m2;

            var engine = new SubsystemEngine(def, new FakeCharacterContext());

            // 0 Cloak of Shadows known -> warns for 1 mystery
            var warnings = engine.ValidateChoice(null, "Shadow Veil");
            Assert.AreEqual(1, warnings.Count);
            Assert.AreEqual(ValidationCategory.Prerequisite, warnings[0].Category);
            Assert.AreEqual("Requires 1 Cloak of Shadows mystery, but only 0 known.", warnings[0].Message);

            // Learn m1 -> satisfies prerequisite
            engine.AssignSlot("mysteries", 0, "Shadow Skin");
            var satisfied = engine.ValidateChoice(null, "Shadow Veil");
            Assert.AreEqual(0, satisfied.Count);
        }

        [TestMethod]
        public void SubsystemEngine_ValidateChoice_UnpartitionedPrerequisites_WhenPrerequisitePropertyNull() {
            var def = TestFactory.CreateSubsystemDefinition(
                id: "truenaming",
                name: "Truenaming",
                prerequisiteProperty: null,
                prerequisiteUnit: "utterance"
            );
            var group = TestFactory.CreateSlotGroupDefinition(
                name: "utterances",
                fixedCapacity: 5
            );
            def.SlotGroups["utterances"] = group;

            var u1 = new SubsystemElement { Name = "Word of Minor Nurture" };
            u1.SetProperty("prerequisiteCount", 0);

            var u2 = new SubsystemElement { Name = "Word of Moderate Nurture" };
            u2.SetProperty("prerequisiteCount", 2);

            def.Elements["Word of Minor Nurture"] = u1;
            def.Elements["Word of Moderate Nurture"] = u2;

            var engine = new SubsystemEngine(def, new FakeCharacterContext());

            // 0 utterances known -> requires 2 utterances (plural)
            var warnings0 = engine.ValidateChoice(null, "Word of Moderate Nurture");
            Assert.AreEqual(1, warnings0.Count);
            Assert.AreEqual("Requires 2 utterances, but only 0 known.", warnings0[0].Message);

            // Learn 1 utterance -> requires 2 utterances, only 1 known
            engine.AssignSlot("utterances", 0, "Word of Minor Nurture");
            var warnings1 = engine.ValidateChoice(null, "Word of Moderate Nurture");
            Assert.AreEqual(1, warnings1.Count);
            Assert.AreEqual("Requires 2 utterances, but only 1 known.", warnings1[0].Message);
        }

        [TestMethod]
        public void SubsystemEngine_Validate_EvaluatesDynamicPrerequisites() {
            var def = TestFactory.CreateSubsystemDefinition(
                id: "shadow_magic",
                name: "Shadow Magic",
                prerequisiteProperty: "path",
                prerequisiteUnit: "mystery"
            );
            var group = TestFactory.CreateSlotGroupDefinition(
                name: "mysteries",
                fixedCapacity: 5
            );
            def.SlotGroups["mysteries"] = group;

            var m1 = new SubsystemElement { Name = "Shadow Veil" };
            m1.SetProperty("path", "Cloak of Shadows");
            m1.SetProperty("prerequisiteCount", 1);
            def.Elements["Shadow Veil"] = m1;

            var engine = new SubsystemEngine(def, new FakeCharacterContext());
            engine.AssignSlot("mysteries", 0, "Shadow Veil");

            var warnings = engine.Validate();
            Assert.IsTrue(warnings.Any(w => w.Contains("Shadow Veil requires 1 Cloak of Shadows mystery, but only 1 known.") == false &&
                                            w.Contains("Shadow Veil requires 1 Cloak of Shadows mystery, but only 0 known.")),
                $"Expected prerequisite warning from Validate, got: {string.Join(", ", warnings)}");
        }

        [TestMethod]
        public void StateCapacityDefinition_ExplicitConstructorAndJson_RoundTrips() {
            var def = TestFactory.CreateStateCapacityDefinition(new[] { "available", "expended", "withheld" }, "maneuvers readied:{source}", "readied");
            Assert.AreEqual(3, def.States.Count);
            Assert.AreEqual("available", def.States[0]);
            Assert.AreEqual("expended", def.States[1]);
            Assert.AreEqual("withheld", def.States[2]);
            Assert.AreEqual("maneuvers readied:{source}", def.Stat);
            Assert.AreEqual("readied", def.Label);

            var json = def.ToJson();
            Assert.AreEqual("maneuvers readied:{source}", (string?)json["stat"]);
            Assert.AreEqual("readied", (string?)json["label"]);
            var statesArr = json["states"] as JArray;
            Assert.IsNotNull(statesArr);
            Assert.AreEqual(3, statesArr!.Count);

            var deserialized = new StateCapacityDefinition(json);
            Assert.AreEqual(3, deserialized.States.Count);
            Assert.AreEqual("available", deserialized.States[0]);
            Assert.AreEqual("expended", deserialized.States[1]);
            Assert.AreEqual("withheld", deserialized.States[2]);
            Assert.AreEqual("maneuvers readied:{source}", deserialized.Stat);
            Assert.AreEqual("readied", deserialized.Label);

            // Null json constructor test
            var empty = new StateCapacityDefinition((JObject)null!);
            Assert.AreEqual(0, empty.States.Count);
            Assert.AreEqual(string.Empty, empty.Stat);
            Assert.AreEqual(string.Empty, empty.Label);
        }

        [TestMethod]
        public void SlotGroupDefinition_StateCapacities_JsonRoundTrip() {
            var group = TestFactory.CreateSlotGroupDefinition(
                name: "maneuvers",
                capacityStat: "maneuvers known:{source}",
                defaultState: "unreadied",
                states: new[] { "unreadied", "available", "expended", "withheld" },
                stateCapacities: new[] {
                    TestFactory.CreateStateCapacityDefinition(
                        new[] { "available", "expended", "withheld" },
                        "maneuvers readied:{source}",
                        "readied"
                    )
                }
            );

            var json = (JObject)group.ToJson();
            var scArr = json["state_capacities"] as JArray;
            Assert.IsNotNull(scArr);
            Assert.AreEqual(1, scArr.Count);

            var deserialized = new SlotGroupDefinition(json);
            Assert.AreEqual(1, deserialized.StateCapacities.Count);
            Assert.AreEqual("readied", deserialized.StateCapacities[0].Label);
            Assert.AreEqual("maneuvers readied:{source}", deserialized.StateCapacities[0].Stat);
            Assert.AreEqual(3, deserialized.StateCapacities[0].States.Count);
        }

        [TestMethod]
        public void SubsystemEngine_Validate_GenericStateCapacities_EmitsDynamicCapacityWarning() {
            var def = TestFactory.CreateSubsystemDefinition(
                id: "custom_magic",
                name: "Custom Magic"
            );
            var group = TestFactory.CreateSlotGroupDefinition(
                name: "auras",
                fixedCapacity: 5,
                defaultState: "dormant",
                states: new[] { "dormant", "radiating" },
                stateCapacities: new[] {
                    TestFactory.CreateStateCapacityDefinition(
                        new[] { "radiating" },
                        "active auras:{source}",
                        "radiant"
                    )
                }
            );
            def.SlotGroups["auras"] = group;

            def.Elements["Solar Flare"] = new SubsystemElement { Name = "Solar Flare" };
            def.Elements["Lunar Beam"] = new SubsystemElement { Name = "Lunar Beam" };

            var context = new FakeCharacterContext();
            context.Character.paramValues["active auras:paladin"] = 1;

            var engine = new SubsystemEngine(def, context);
            engine.AssignSlot("auras", 0, "Solar Flare", "radiating", "Paladin");
            engine.AssignSlot("auras", 1, "Lunar Beam", "radiating", "Paladin");

            var warnings = engine.Validate();
            Assert.IsTrue(warnings.Any(w => w.Contains("Paladin has 2 radiant auras, exceeding maximum of 1.")),
                $"Expected capacity warning, got: {string.Join("; ", warnings)}");
        }

        [TestMethod]
        public void SubsystemEngine_TransitionSlot_ActiveStateClamping_UsesStateCapacities() {
            var def = TestFactory.CreateSubsystemDefinition(
                id: "custom_magic",
                name: "Custom Magic"
            );
            var group = TestFactory.CreateSlotGroupDefinition(
                name: "stances",
                fixedCapacity: 5,
                defaultState: "inactive",
                states: new[] { "inactive", "active" },
                stateCapacities: new[] {
                    TestFactory.CreateStateCapacityDefinition(
                        new[] { "active" },
                        "max active stances",
                        "active"
                    )
                }
            );
            def.SlotGroups["stances"] = group;

            def.Elements["Stance 1"] = new SubsystemElement { Name = "Stance 1" };
            def.Elements["Stance 2"] = new SubsystemElement { Name = "Stance 2" };

            def.Verbs["adopt"] = new List<SubsystemStep> {
                TestFactory.CreateSubsystemStep(type: "transition", slotGroup: "stances", selector: "invoked", fromState: "*", toState: "active")
            };

            var context = new FakeCharacterContext();
            context.Character.paramValues["max active stances"] = 1;

            var engine = new SubsystemEngine(def, context);
            engine.AssignSlot("stances", 0, "Stance 1", "active");
            engine.AssignSlot("stances", 1, "Stance 2", "inactive");

            Assert.AreEqual("active", engine.GetSlotState("stances", 0));
            Assert.AreEqual("inactive", engine.GetSlotState("stances", 1));

            // Adopting second stance should clamp and deactivate the first stance
            engine.ExecuteVerb("adopt", "stances", 1);

            Assert.AreEqual("inactive", engine.GetSlotState("stances", 0));
            Assert.AreEqual("active", engine.GetSlotState("stances", 1));
        }

        [TestMethod]
        public void ChoiceDescriptor_SlotGroup_RoundTripSerialization() {
            var descriptor = new ChoiceDescriptor("stance", "Choose stance", "SubsystemElements", slotGroup: "stances");
            Assert.AreEqual("stances", descriptor.SlotGroup);

            var json = descriptor.ToJson() as JObject;
            Assert.IsNotNull(json);
            Assert.AreEqual("stances", (string?)json["slotGroup"]);

            var deserialized = new ChoiceDescriptor(json);
            Assert.AreEqual("stances", deserialized.SlotGroup);
        }

        [TestMethod]
        public void FeatsResource_MartialStanceAndStudy_DeclareSlotGroup() {
            var catalog = new GameRulesCatalog();
            var stance = catalog.GetFeat("Martial Stance");
            var stanceChoice = stance.Choices.FirstOrDefault(c => c.Id == "stance");
            Assert.IsNotNull(stanceChoice);
            Assert.AreEqual("stances", stanceChoice.SlotGroup);

            var study = catalog.GetFeat("Martial Study");
            var maneuverChoice = study.Choices.FirstOrDefault(c => c.Id == "maneuver");
            Assert.IsNotNull(maneuverChoice);
            Assert.AreEqual("maneuvers", maneuverChoice.SlotGroup);
        }

        private static void SetStat(EffectPool pool, string statName, int value) {
            if (!pool.stats.ContainsKey(statName)) {
                pool.AddStat(new Statistic(statName));
            }
            pool.AddEffect(new Effect($"Base {statName}").AddModifier(statName, new Modifier(value, ModifierTypes.UNTYPED)));
        }

        [TestMethod]
        public void SubsystemEngine_ValidateChoice_EmitsCapacityWarning_WhenSlotGroupCapacityReached() {
            var catalog = new GameRulesCatalog();
            var martialDef = catalog.GetSubsystem("martial_maneuvers");
            var context = new FakeCharacterContext(catalog: catalog);
            SetStat(context.Effects, "stances known:feats", 1);
            var engine = new SubsystemEngine(martialDef, context);

            var stanceDescriptor = new ChoiceDescriptor("stance", "Choose stance", "SubsystemElements", filter: "stance", slotGroup: "stances");

            // When 0 slots assigned, candidate choice passes without capacity warning
            var warnings0 = engine.ValidateChoice(stanceDescriptor, "Punishing Stance", "Feats");
            Assert.IsFalse(warnings0.Any(w => w.Category == ValidationCategory.Capacity));

            // Assign 1 slot for Feats bound to "Level 1"
            engine.GrantSlot("stances", "Punishing Stance", "Feats", "Level 1");

            // Validating for another slot ("Level 3") exceeds capacity (1)
            var warnings1 = engine.ValidateChoice(stanceDescriptor, "Blood in the Water", "Feats", "Level 3");
            Assert.IsTrue(warnings1.Any(w => w.Category == ValidationCategory.Capacity && w.Message.Contains("Selection exceeds stances capacity (1) for Feats.")));

            // Validating for the same slot ("Level 1") excludes it and does NOT warn
            var warningsReSelect = engine.ValidateChoice(stanceDescriptor, "Blood in the Water", "Feats", "Level 1");
            Assert.IsFalse(warningsReSelect.Any(w => w.Category == ValidationCategory.Capacity));
        }

        [TestMethod]
        public void SubsystemEngine_ValidateChoice_CapacityWarning_FormatsWithoutSource_WhenSourceNullOrEmpty() {
            var def = TestFactory.CreateSubsystemDefinition(
                id: "custom_auras",
                name: "Custom Auras"
            );
            var group = TestFactory.CreateSlotGroupDefinition(
                name: "auras",
                fixedCapacity: 1,
                capacityStat: "max auras"
            );
            def.SlotGroups["auras"] = group;
            def.Elements["Solar Flare"] = new SubsystemElement { Name = "Solar Flare" };
            def.Elements["Lunar Beam"] = new SubsystemElement { Name = "Lunar Beam" };

            var descriptor = new ChoiceDescriptor("aura", "Choose aura", "SubsystemElements", slotGroup: "auras");
            var engine = new SubsystemEngine(def, new FakeCharacterContext());
            engine.AssignSlot("auras", 0, "Solar Flare");

            var warnings = engine.ValidateChoice(descriptor, "Lunar Beam", null);
            Assert.AreEqual(1, warnings.Count);
            Assert.AreEqual(ValidationCategory.Capacity, warnings[0].Category);
            Assert.AreEqual("Selection exceeds auras capacity (1).", warnings[0].Message);
        }

        [TestMethod]
        public void SubsystemEngine_Validate_EmitsCharacterCapacityWarning_WhenDistinctSourcesNotOne() {
            var def = TestFactory.CreateSubsystemDefinition(
                id: "martial_maneuvers",
                name: "Martial Maneuvers"
            );
            var group = TestFactory.CreateSlotGroupDefinition(
                name: "stances",
                fixedCapacity: 5,
                defaultState: "inactive",
                states: new[] { "inactive", "active" },
                stateCapacities: new[] {
                    TestFactory.CreateStateCapacityDefinition(
                        new[] { "active" },
                        "active stances",
                        "active"
                    )
                }
            );
            def.SlotGroups["stances"] = group;

            def.Elements["Punishing Stance"] = new SubsystemElement { Name = "Punishing Stance" };
            def.Elements["Flame's Blessing"] = new SubsystemElement { Name = "Flame's Blessing" };

            var context = new FakeCharacterContext();
            context.Character.paramValues["active stances"] = 1;

            var engine = new SubsystemEngine(def, context);
            engine.AssignSlot("stances", 0, "Punishing Stance", "active", "Warblade");
            engine.AssignSlot("stances", 1, "Flame's Blessing", "active", "Swordsage");

            var warnings = engine.Validate();
            Assert.IsTrue(warnings.Any(w => w == "Character has 2 active stances, exceeding maximum of 1."),
                $"Expected 'Character has 2 active stances, exceeding maximum of 1.', got: {string.Join("; ", warnings)}");
        }

        [TestMethod]
        public void SubsystemEngine_TransitionSlot_DynamicStateClamping_CustomNonActiveState() {
            var def = TestFactory.CreateSubsystemDefinition(
                id: "custom_auras",
                name: "Custom Auras"
            );
            var group = TestFactory.CreateSlotGroupDefinition(
                name: "auras",
                fixedCapacity: 5,
                defaultState: "dormant",
                states: new[] { "dormant", "radiating" },
                stateCapacities: new[] {
                    TestFactory.CreateStateCapacityDefinition(
                        new[] { "radiating" },
                        "max radiating auras",
                        "radiant"
                    )
                }
            );
            def.SlotGroups["auras"] = group;

            def.Elements["Solar Flare"] = new SubsystemElement { Name = "Solar Flare" };
            def.Elements["Lunar Beam"] = new SubsystemElement { Name = "Lunar Beam" };

            def.Verbs["radiate"] = new List<SubsystemStep> {
                TestFactory.CreateSubsystemStep(type: "transition", slotGroup: "auras", selector: "invoked", fromState: "*", toState: "radiating")
            };

            var context = new FakeCharacterContext();
            context.Character.paramValues["max radiating auras"] = 1;

            var engine = new SubsystemEngine(def, context);
            engine.AssignSlot("auras", 0, "Solar Flare", "radiating");
            engine.AssignSlot("auras", 1, "Lunar Beam", "dormant");

            Assert.AreEqual("radiating", engine.GetSlotState("auras", 0));
            Assert.AreEqual("dormant", engine.GetSlotState("auras", 1));

            // Radiating second aura should clamp and demote the first aura to dormant
            engine.ExecuteVerb("radiate", "auras", 1);

            Assert.AreEqual("dormant", engine.GetSlotState("auras", 0));
            Assert.AreEqual("radiating", engine.GetSlotState("auras", 1));
        }
    }
}


