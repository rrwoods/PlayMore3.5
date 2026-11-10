using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;

namespace PlayMore35.Tests {
    [TestClass]
    public class ProgressionSlotKeyTests {
        [TestMethod]
        public void Advancement_CreatesAdvancementSlotKey() {
            var key = ProgressionSlotKey.Advancement(1);
            Assert.AreEqual(SlotKind.Advancement, key.Kind);
            Assert.AreEqual("Advancement", key.Source);
            Assert.AreEqual(1, key.Level);
        }

        [TestMethod]
        public void Race_CreatesRaceSlotKey_WithDefaultAndExplicitLevel() {
            var defaultKey = ProgressionSlotKey.Race("Human");
            Assert.AreEqual(SlotKind.Race, defaultKey.Kind);
            Assert.AreEqual("Human", defaultKey.Source);
            Assert.AreEqual(1, defaultKey.Level);

            var explicitKey = ProgressionSlotKey.Race("Chameleon", 2);
            Assert.AreEqual(SlotKind.Race, explicitKey.Kind);
            Assert.AreEqual("Chameleon", explicitKey.Source);
            Assert.AreEqual(2, explicitKey.Level);
        }

        [TestMethod]
        public void Class_CreatesClassSlotKey() {
            var key = ProgressionSlotKey.Class("Fighter", 2);
            Assert.AreEqual(SlotKind.Class, key.Kind);
            Assert.AreEqual("Fighter", key.Source);
            Assert.AreEqual(2, key.Level);
        }

        [TestMethod]
        public void Item_CreatesItemSlotKey_WithNegativeOneLevel() {
            var key = ProgressionSlotKey.Item("Crown of White Raven");
            Assert.AreEqual(SlotKind.Item, key.Kind);
            Assert.AreEqual("Crown of White Raven", key.Source);
            Assert.AreEqual(-1, key.Level);
        }

        [TestMethod]
        public void Immutability_PropertiesHaveNoPublicOrInternalSetters() {
            var properties = typeof(ProgressionSlotKey).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties) {
                Assert.IsFalse(prop.CanWrite, $"Property '{prop.Name}' should be read-only (no setter).");
            }
        }

        [TestMethod]
        public void RejectsStringParse_NoParseOrTryParseMethodsExist() {
            var methods = typeof(ProgressionSlotKey).GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

            var forbiddenMethods = methods.Where(m => m.Name == "Parse" || m.Name == "TryParse").ToList();
            Assert.AreEqual(0, forbiddenMethods.Count,
                "ProgressionSlotKey must explicitly reject string parsing; no Parse or TryParse methods should exist.");
        }

        [TestMethod]
        public void Equality_ComparesKindSourceCaseInsensitivelyAndLevel() {
            var key1 = ProgressionSlotKey.Class("Fighter", 1);
            var key2 = ProgressionSlotKey.Class("fighter", 1);
            var diffClass = ProgressionSlotKey.Class("Wizard", 1);
            var diffLevel = ProgressionSlotKey.Class("Fighter", 2);
            var diffKind = ProgressionSlotKey.Race("Fighter", 1);

            Assert.AreEqual(key1, key2);
            Assert.IsTrue(key1.Equals(key2));
            Assert.IsTrue(key1.Equals((object)key2));
            Assert.IsTrue(key1 == key2);
            Assert.IsFalse(key1 != key2);
            Assert.AreEqual(key1.GetHashCode(), key2.GetHashCode());

            Assert.AreNotEqual(key1, diffClass);
            Assert.AreNotEqual(key1, diffLevel);
            Assert.AreNotEqual(key1, diffKind);
            Assert.IsFalse(key1 == diffClass);
            Assert.IsTrue(key1 != diffClass);
            Assert.IsFalse(key1.Equals(null));
            Assert.IsTrue(key1.Equals(key1));
        }

        [TestMethod]
        public void JsonRoundTrip_SerializesAndDeserializesCorrectly() {
            var original = ProgressionSlotKey.Class("Warblade", 3);
            var json = (JObject)original.ToJson();

            Assert.AreEqual("Class", (string)json["kind"]!);
            Assert.AreEqual("Warblade", (string)json["source"]!);
            Assert.AreEqual(3, (int)json["level"]!);

            var restored = new ProgressionSlotKey(json);
            Assert.AreEqual(original, restored);
        }

        [TestMethod]
        public void JsonConstructor_MissingOrInvalidKind_ThrowsArgumentException() {
            Assert.ThrowsException<ArgumentNullException>(() => new ProgressionSlotKey((JObject)null!));

            var missingKind = new JObject { ["source"] = "Fighter", ["level"] = 1 };
            Assert.ThrowsException<ArgumentException>(() => new ProgressionSlotKey(missingKind));

            var invalidKind = new JObject { ["kind"] = "InvalidKind", ["source"] = "Fighter", ["level"] = 1 };
            Assert.ThrowsException<ArgumentException>(() => new ProgressionSlotKey(invalidKind));
        }

        [TestMethod]
        public void ToString_ReturnsFormattedKindSourceLevel() {
            var key = ProgressionSlotKey.Advancement(1);
            Assert.AreEqual("Advancement:Advancement:1", key.ToString());

            var itemKey = ProgressionSlotKey.Item("Crown");
            Assert.AreEqual("Item:Crown:-1", itemKey.ToString());
        }
    }
}
