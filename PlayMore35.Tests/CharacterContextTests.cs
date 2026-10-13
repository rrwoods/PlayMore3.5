using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Items;
using PlayMore35.Subsystems;
using System.Collections.Generic;
using System.Linq;

namespace PlayMore35.Tests {
    [TestClass]
    public class CharacterContextTests {
        private class TestCharacterContext : ICharacterContext {
            public Character Character { get; set; }
            public EffectPool Effects { get; set; }
            public Equipped EquippedItems { get; set; }
            public Timeline Timeline => Character?.Timeline;
            public SubsystemRegistry Subsystems { get; } = new SubsystemRegistry();

            public List<AttackRoutine> GetAllRoutines() {
                return AttackRoutineEvaluator.EvaluateRoutines(Effects, EquippedItems, 0);
            }

            public List<AttackRoutine> GetVisibleRoutines() {
                var all = GetAllRoutines();
                var prefs = Character?.RoutinePreferences ?? new AttackRoutinePreferences();
                return all.Where(r => prefs.IsCategoryVisible(r.Category)).ToList();
            }
        }

        [TestMethod]
        public void CharacterContext_OperatesHeadlessly_WithoutUiDependencies() {
            var character = new Character();
            var context = new TestCharacterContext {
                Character = character,
                Effects = new EffectPool(character),
                EquippedItems = new Equipped()
            };

            Assert.IsNotNull(context.Character);
            Assert.IsNotNull(context.Effects);
            Assert.IsNotNull(context.EquippedItems);
            Assert.IsNotNull(context.Timeline);
            Assert.IsNotNull(context.Subsystems);
        }
    }
}
