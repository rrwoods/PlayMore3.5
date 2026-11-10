using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Subsystems;

namespace PlayMore35.Tests {
    [TestClass]
    public class SubsystemSlotTests {
        [TestMethod]
        public void DefaultConstructor_GrantSlotIsNull() {
            var slot = new SubsystemSlot();
            Assert.IsNull(slot.GrantSlot);
            Assert.IsFalse(slot.IsGrantedByEquippedItem);
        }

        [TestMethod]
        public void ParameterizedConstructor_SetsGrantSlot() {
            var grantKey = ProgressionSlotKey.Class("Warblade", 1);
            var slot = new SubsystemSlot("Steel Wind", "available", "Warblade", null, grantKey);

            Assert.AreEqual("Steel Wind", slot.Element);
            Assert.AreEqual("available", slot.State);
            Assert.AreEqual("Warblade", slot.Source);
            Assert.IsNull(slot.FeatSlot);
            Assert.AreEqual(grantKey, slot.GrantSlot);
        }

        [TestMethod]
        public void IsGrantedByEquippedItem_DerivedFromItemGrantSlot() {
            var itemSlot = new SubsystemSlot("Iron Heart Surge", "available", "Item", null, ProgressionSlotKey.Item("Crown of White Raven"));
            Assert.IsTrue(itemSlot.IsGrantedByEquippedItem);

            var classSlot = new SubsystemSlot("Steel Wind", "available", "Warblade", null, ProgressionSlotKey.Class("Warblade", 1));
            Assert.IsFalse(classSlot.IsGrantedByEquippedItem);

            var advancementSlot = new SubsystemSlot("Burning Blade", "available", "Advancement", null, ProgressionSlotKey.Advancement(1));
            Assert.IsFalse(advancementSlot.IsGrantedByEquippedItem);
        }

        [TestMethod]
        public void IsGrantedByEquippedItem_RetainsLegacyFallbacks() {
            var legacyPrefix = new SubsystemSlot("Iron Heart Surge", "available", null, "Equipped:Crown of White Raven");
            Assert.IsTrue(legacyPrefix.IsGrantedByEquippedItem);

            var legacySourceItem = new SubsystemSlot("Iron Heart Surge", "available", "Item", null);
            Assert.IsTrue(legacySourceItem.IsGrantedByEquippedItem);

            var legacySourceEquipped = new SubsystemSlot("Iron Heart Surge", "available", "Equipped", null);
            Assert.IsTrue(legacySourceEquipped.IsGrantedByEquippedItem);

            var normalSlot = new SubsystemSlot("Steel Wind", "available", "Warblade", "Level 1");
            Assert.IsFalse(normalSlot.IsGrantedByEquippedItem);
        }

        [TestMethod]
        public void JsonRoundTrip_WithGrantSlot_SerializesAndDeserializes() {
            var grantKey = ProgressionSlotKey.Item("Crown of White Raven");
            var original = new SubsystemSlot("Iron Heart Surge", "available", "Item", null, grantKey);

            var json = (JObject)original.ToJson();
            Assert.IsNotNull(json["slot"]);
            Assert.AreEqual("Item", (string)json["slot"]!["kind"]!);
            Assert.AreEqual("Crown of White Raven", (string)json["slot"]!["source"]!);
            Assert.AreEqual(-1, (int)json["slot"]!["level"]!);

            var restored = new SubsystemSlot(json);
            Assert.AreEqual(original, restored);
            Assert.IsNotNull(restored.GrantSlot);
            Assert.AreEqual(SlotKind.Item, restored.GrantSlot!.Kind);
            Assert.AreEqual("Crown of White Raven", restored.GrantSlot.Source);
            Assert.AreEqual(-1, restored.GrantSlot.Level);
            Assert.IsTrue(restored.IsGrantedByEquippedItem);
        }

        [TestMethod]
        public void JsonRoundTrip_WithoutGrantSlot_OmitsSlotProperty() {
            var original = new SubsystemSlot("Steel Wind", "available", "Warblade");

            var json = (JObject)original.ToJson();
            Assert.IsNull(json["slot"]);

            var restored = new SubsystemSlot(json);
            Assert.AreEqual(original, restored);
            Assert.IsNull(restored.GrantSlot);
        }

        [TestMethod]
        public void Equality_IncludesGrantSlot() {
            var key1 = ProgressionSlotKey.Class("Warblade", 1);
            var key2 = ProgressionSlotKey.Class("Warblade", 2);

            var slot1 = new SubsystemSlot("Steel Wind", "available", "Warblade", null, key1);
            var slot2 = new SubsystemSlot("Steel Wind", "available", "Warblade", null, key1);
            var slotDiffGrant = new SubsystemSlot("Steel Wind", "available", "Warblade", null, key2);
            var slotNullGrant = new SubsystemSlot("Steel Wind", "available", "Warblade", null, null);

            Assert.AreEqual(slot1, slot2);
            Assert.AreEqual(slot1.GetHashCode(), slot2.GetHashCode());
            Assert.AreNotEqual(slot1, slotDiffGrant);
            Assert.AreNotEqual(slot1, slotNullGrant);
        }
    }
}
