using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Items;
using PlayMore35.Subsystems;

namespace PlayMore35.Tests {
    [TestClass]
    public class SubsystemEngineTests {
        private class FakeSlottedSubsystem : ISlottedSubsystem {
            public string Id => "fake-slotted";
            public bool IsActive { get; set; } = true;
            public int SlotCount => 3;
            public List<int> ClearedSlots { get; } = new List<int>();
            public bool Initialized { get; private set; }
            public bool ContributedEffects { get; private set; }

            public void Initialize(ICharacterContext context) {
                Initialized = true;
            }

            public void ContributeEffects(EffectPool pool, ICharacterContext context) {
                ContributedEffects = true;
            }

            public void ClearSlot(int slotIndex) {
                ClearedSlots.Add(slotIndex);
            }
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
            var slotted = new FakeSlottedSubsystem();
            var simple = new FakeSimpleSubsystem();

            registry.Register(slotted);
            registry.Register(simple);

            Assert.AreSame(slotted, registry.Get<FakeSlottedSubsystem>());
            Assert.AreSame(slotted, registry.Get<ISlottedSubsystem>());
            Assert.AreSame(slotted, registry.Get("fake-slotted"));

            Assert.AreSame(simple, registry.Get<FakeSimpleSubsystem>());
            Assert.AreSame(simple, registry.Get("fake-simple"));

            Assert.IsNull(registry.Get("nonexistent"));
            var unregistered = new SubsystemRegistry();
            Assert.IsNull(unregistered.Get<FakeSlottedSubsystem>());
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
        public void ResourcePool_HandlesSpendRestoreAndReset_RespectingCapacityCaps() {
            int maxCap = 10;
            var pool = new ResourcePool("power-points", () => maxCap);

            Assert.AreEqual("power-points", pool.Id);
            Assert.AreEqual(10, pool.Max);
            Assert.AreEqual(10, pool.Current);

            // Spend valid amount
            Assert.IsTrue(pool.Spend(4));
            Assert.AreEqual(6, pool.Current);

            // Spend more than available fails
            Assert.IsFalse(pool.Spend(7));
            Assert.AreEqual(6, pool.Current);

            // Restore clamps to Max
            pool.Restore(2);
            Assert.AreEqual(8, pool.Current);
            pool.Restore(10);
            Assert.AreEqual(10, pool.Current);

            // Spend and Reset restores to Max
            pool.Spend(5);
            Assert.AreEqual(5, pool.Current);
            pool.Reset();
            Assert.AreEqual(10, pool.Current);

            // Dynamic max shrink clamps current
            maxCap = 3;
            Assert.AreEqual(3, pool.Max);
            Assert.AreEqual(3, pool.Current);
        }

        [TestMethod]
        public void CharacterContext_ExposesSubsystemRegistry() {
            var controller = new Controller(new Character());
            Assert.IsNotNull(controller.Subsystems);
            Assert.IsInstanceOfType(controller.Subsystems, typeof(SubsystemRegistry));

            ICharacterContext context = controller;
            Assert.IsNotNull(context.Subsystems);
        }
    }
}
