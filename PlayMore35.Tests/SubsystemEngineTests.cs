using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Items;
using PlayMore35.Subsystems;
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
        }

        [TestMethod]
        public void SubsystemRegistry_NewInstance_RemainsEmptyForTestIsolation() {
            var registry = new SubsystemRegistry();
            Assert.AreEqual(0, registry.All.Count());
            Assert.IsNull(registry.Get<PactMagicEngine>());
            Assert.IsNull(registry.Get("pact_magic"));
            Assert.IsNull(registry.Get("vestige"));
        }

        [TestMethod]
        public void SubsystemSlotGroup_ClampsAndValidatesCapacity_Dynamically() {
            int dynamicCapacity = 2;
            var slotGroup = new SubsystemSlotGroup<string>(() => dynamicCapacity);

            Assert.AreEqual(2, slotGroup.Capacity);
            Assert.AreEqual(2, slotGroup.Slots.Count);

            slotGroup[0] = "Item A";
            slotGroup[1] = "Item B";
            Assert.AreEqual("Item A", slotGroup[0]);
            Assert.AreEqual("Item B", slotGroup[1]);

            // Out of range access throws
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => {
                var _ = slotGroup[2];
            });
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => {
                slotGroup[2] = "Item C";
            });

            // Dynamically expand capacity
            dynamicCapacity = 3;
            Assert.AreEqual(3, slotGroup.Capacity);
            Assert.AreEqual(3, slotGroup.Slots.Count);
            slotGroup[2] = "Item C";
            Assert.AreEqual("Item C", slotGroup[2]);

            // Dynamically shrink capacity
            dynamicCapacity = 1;
            Assert.AreEqual(1, slotGroup.Capacity);
            Assert.AreEqual(1, slotGroup.Slots.Count);
            Assert.AreEqual("Item A", slotGroup[0]);

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

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => slotGroup.ClearSlot(-1));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => slotGroup.ClearSlot(3));
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
        public void SubsystemSlotGroup_NegativeCapacity_ClampsToZero() {
            var slotGroup = new SubsystemSlotGroup<int>(() => -5);

            Assert.AreEqual(0, slotGroup.Capacity);
            Assert.AreEqual(0, slotGroup.Slots.Count);
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => {
                var _ = slotGroup[0];
            });
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
    }
}
