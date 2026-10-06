using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Items;
using System.Collections.Generic;

namespace PlayMore35.Tests {
    [TestClass]
    public class CharacterContextTests {
        private class TestCharacterContext : ICharacterContext {
            public Character Character { get; set; }
            public EffectPool Effects { get; set; }
            public Equipped EquippedItems { get; set; }
            public Rounds Timeline => Character?.Timeline;

            public readonly List<(string subsystem, int slotIndex)> ClearedSlots = new List<(string, int)>();

            public void ClearSlot(string subsystem, int slotIndex) {
                ClearedSlots.Add((subsystem, slotIndex));
            }
        }

        [TestMethod]
        public void CharacterContext_OperatesHeadlessly_WithoutUiDependencies() {
            var character = new Character();
            var context = new TestCharacterContext {
                Character = character,
                Effects = new EffectPool(),
                EquippedItems = new Equipped()
            };

            Assert.IsNotNull(context.Character);
            Assert.IsNotNull(context.Effects);
            Assert.IsNotNull(context.EquippedItems);
            Assert.IsNotNull(context.Timeline);

            context.ClearSlot("vestige", 0);

            Assert.AreEqual(1, context.ClearedSlots.Count);
            Assert.AreEqual("vestige", context.ClearedSlots[0].subsystem);
            Assert.AreEqual(0, context.ClearedSlots[0].slotIndex);
        }
    }
}
