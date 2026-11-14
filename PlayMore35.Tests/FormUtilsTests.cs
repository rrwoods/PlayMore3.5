using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlayMore35.CharacterChoices;
using PlayMore35.Global;

namespace PlayMore35.Tests {
    [TestClass]
    public class FormUtilsTests {
        [TestMethod]
        public void FormatSlotDisplayName_AdvancementSlots_ReturnsLevelFormat() {
            Assert.AreEqual("Level 1", FormUtils.FormatSlotDisplayName(ProgressionSlotKey.Advancement(1)));
            Assert.AreEqual("Level 3", FormUtils.FormatSlotDisplayName(ProgressionSlotKey.Advancement(3)));
            Assert.AreEqual("Level 18", FormUtils.FormatSlotDisplayName(ProgressionSlotKey.Advancement(18)));
        }

        [TestMethod]
        public void FormatSlotDisplayName_ClassSlots_ReturnsClassAndLevel() {
            Assert.AreEqual("Fighter 1", FormUtils.FormatSlotDisplayName(ProgressionSlotKey.Class("Fighter", 1)));
            Assert.AreEqual("Fighter 2", FormUtils.FormatSlotDisplayName(ProgressionSlotKey.Class("Fighter", 2)));
            Assert.AreEqual("Wizard 5", FormUtils.FormatSlotDisplayName(ProgressionSlotKey.Class("Wizard", 5)));
        }

        [TestMethod]
        public void FormatSlotDisplayName_RaceSlots_ReturnsRaceName() {
            Assert.AreEqual("Human", FormUtils.FormatSlotDisplayName(ProgressionSlotKey.Race("Human")));
        }

        [TestMethod]
        public void FormatSlotDisplayName_ItemSlots_ReturnsItemName() {
            Assert.AreEqual("Crown of White Raven", FormUtils.FormatSlotDisplayName(ProgressionSlotKey.Item("Crown of White Raven")));
        }

        [TestMethod]
        public void FormatSlotDisplayName_NullSlot_ReturnsEmptyString() {
            Assert.AreEqual("", FormUtils.FormatSlotDisplayName(null!));
        }
    }
}
