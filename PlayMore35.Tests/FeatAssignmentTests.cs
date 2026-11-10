using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;

namespace PlayMore35.Tests {
    [TestClass]
    public class FeatAssignmentTests {
        [TestMethod]
        public void Constructor_InitializesPropertiesAndChoices() {
            var slot = ProgressionSlotKey.Advancement(1);
            var assignment = new FeatAssignment(slot, "Power Attack");

            Assert.AreEqual(slot, assignment.Slot);
            Assert.AreEqual("Power Attack", assignment.Feat);
            Assert.IsNotNull(assignment.Choices);
            Assert.AreEqual(0, assignment.Choices.Count);
        }

        [TestMethod]
        public void Constructor_WithChoices_StoresChoicesCaseInsensitively() {
            var slot = ProgressionSlotKey.Class("Fighter", 1);
            var choices = new Dictionary<string, string> {
                { "Weapon", "Greatsword" }
            };
            var assignment = new FeatAssignment(slot, "Weapon Focus", choices);

            Assert.AreEqual(1, assignment.Choices.Count);
            Assert.IsTrue(assignment.Choices.ContainsKey("weapon"));
            Assert.AreEqual("Greatsword", assignment.Choices["WEAPON"]);
        }

        [TestMethod]
        public void Constructor_NullSlot_ThrowsArgumentNullException() {
            Assert.ThrowsException<ArgumentNullException>(() => new FeatAssignment(null!, "Power Attack"));
        }

        [TestMethod]
        public void Equality_MatchesSlotFeatCaseInsensitivelyAndChoices() {
            var slot = ProgressionSlotKey.Advancement(1);
            var a1 = new FeatAssignment(slot, "Weapon Focus", new Dictionary<string, string> { { "weapon", "dagger" } });
            var a2 = new FeatAssignment(slot, "weapon focus", new Dictionary<string, string> { { "WEAPON", "dagger" } });
            var diffFeat = new FeatAssignment(slot, "Power Attack");
            var diffChoice = new FeatAssignment(slot, "Weapon Focus", new Dictionary<string, string> { { "weapon", "shortsword" } });
            var diffSlot = new FeatAssignment(ProgressionSlotKey.Advancement(3), "Weapon Focus", new Dictionary<string, string> { { "weapon", "dagger" } });

            Assert.AreEqual(a1, a2);
            Assert.IsTrue(a1.Equals(a2));
            Assert.IsTrue(a1.Equals((object)a2));
            Assert.IsTrue(a1 == a2);
            Assert.IsFalse(a1 != a2);
            Assert.AreEqual(a1.GetHashCode(), a2.GetHashCode());

            Assert.AreNotEqual(a1, diffFeat);
            Assert.AreNotEqual(a1, diffChoice);
            Assert.AreNotEqual(a1, diffSlot);
            Assert.IsFalse(a1 == diffFeat);
            Assert.IsTrue(a1 != diffFeat);
            Assert.IsFalse(a1.Equals(null));
        }

        [TestMethod]
        public void JsonRoundTrip_WithChoices_SerializesAndDeserializesCorrectly() {
            var slot = ProgressionSlotKey.Class("Warblade", 1);
            var choices = new Dictionary<string, string> {
                { "class", "Warblade" },
                { "maneuver", "Burning Blade" }
            };
            var assignment = new FeatAssignment(slot, "Martial Study", choices);

            var json = (JObject)assignment.ToJson();
            Assert.IsNotNull(json["slot"]);
            Assert.AreEqual("Martial Study", (string)json["feat"]!);
            Assert.IsNotNull(json["choices"]);
            Assert.AreEqual("Warblade", (string)json["choices"]!["class"]!);
            Assert.AreEqual("Burning Blade", (string)json["choices"]!["maneuver"]!);

            var restored = new FeatAssignment(json);
            Assert.AreEqual(assignment, restored);
            Assert.AreEqual("Warblade", restored.Choices["class"]);
            Assert.AreEqual("Burning Blade", restored.Choices["maneuver"]);
        }

        [TestMethod]
        public void JsonRoundTrip_WithoutChoices_OmitsChoicesProperty() {
            var slot = ProgressionSlotKey.Advancement(1);
            var assignment = new FeatAssignment(slot, "Toughness");

            var json = (JObject)assignment.ToJson();
            Assert.IsNotNull(json["slot"]);
            Assert.AreEqual("Toughness", (string)json["feat"]!);
            Assert.IsNull(json["choices"], "Choices property should be omitted when empty.");

            var restored = new FeatAssignment(json);
            Assert.AreEqual(assignment, restored);
            Assert.AreEqual(0, restored.Choices.Count);
        }

        [TestMethod]
        public void JsonConstructor_MissingSlot_ThrowsArgumentException() {
            Assert.ThrowsException<ArgumentNullException>(() => new FeatAssignment((JObject)null!));

            var missingSlot = new JObject { ["feat"] = "Toughness" };
            Assert.ThrowsException<ArgumentException>(() => new FeatAssignment(missingSlot));
        }

        [TestMethod]
        public void ToString_ReturnsFormattedSummary() {
            var slot = ProgressionSlotKey.Advancement(1);
            var simple = new FeatAssignment(slot, "Power Attack");
            Assert.AreEqual("Advancement:Advancement:1 -> Power Attack", simple.ToString());

            var withChoices = new FeatAssignment(slot, "Weapon Focus", new Dictionary<string, string> { { "weapon", "dagger" } });
            Assert.AreEqual("Advancement:Advancement:1 -> Weapon Focus (weapon=dagger)", withChoices.ToString());
        }
    }
}
