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

            public StatView GetStatistic(string name) {
                return Effects?.GetStatistic(name, EquippedItems) ?? new StatView(name ?? "", 0);
            }

            public int GetStatValue(string name, int defaultValue = 0) {
                return Effects?.GetStatValue(name, EquippedItems, defaultValue) ?? defaultValue;
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

        [TestMethod]
        public void CharacterContext_GetStatistic_EvaluatesActiveModifiersAndExplanations() {
            var character = new Character();
            var pool = new EffectPool(character);
            pool.AddStat(new Statistic("Morale Bonus"));
            pool.AddEffect(new Effect("Blessing")
                .AddModifier("Morale Bonus", new Modifier(5, "morale"))
            );

            var context = new TestCharacterContext {
                Character = character,
                Effects = pool,
                EquippedItems = new Equipped()
            };

            var statView = context.GetStatistic("Morale Bonus");
            Assert.AreEqual("Morale Bonus", statView.Name);
            Assert.AreEqual(5, statView.Value);
            Assert.IsTrue(statView.Explanation.Any(e => e.Contains("+5") && e.Contains("morale")), "Explanation must document the active modifier.");
        }

        [TestMethod]
        public void CharacterContext_GetStatValue_ReturnsScalarDirectly() {
            var character = new Character();
            var pool = new EffectPool(character);
            pool.AddStat(new Statistic("Vestige Capacity"));
            pool.AddEffect(new Effect("Binder Level")
                .AddModifier("Vestige Capacity", new Modifier(2, "base"))
            );

            var context = new TestCharacterContext {
                Character = character,
                Effects = pool,
                EquippedItems = new Equipped()
            };

            int value = context.GetStatValue("Vestige Capacity");
            Assert.AreEqual(2, value);
        }

        [TestMethod]
        public void CharacterContext_MissingStatistic_ReturnsDefaultValuesGracefully() {
            var character = new Character();
            var context = new TestCharacterContext {
                Character = character,
                Effects = new EffectPool(character),
                EquippedItems = new Equipped()
            };

            Assert.AreEqual(0, context.GetStatValue("NonExistentStat"));
            Assert.AreEqual(42, context.GetStatValue("NonExistentStat", 42));
            Assert.AreEqual(99, context.GetStatValue(null, 99));

            var missingView = context.GetStatistic("NonExistentStat");
            Assert.AreEqual("NonExistentStat", missingView.Name);
            Assert.AreEqual(0, missingView.Value);
            Assert.AreEqual(0, missingView.Explanation.Count);

            var nullView = context.GetStatistic(null);
            Assert.AreEqual("", nullView.Name);
            Assert.AreEqual(0, nullView.Value);
            Assert.AreEqual(0, nullView.Explanation.Count);
        }

        [TestMethod]
        public void CharacterSession_DelegatesStatisticEvaluationToEffectPool() {
            var character = new Character();
            character.assignedAbilityScores["STR"] = 16;
            var session = new CharacterSession(character, new PlayMore35.Rules.GameRulesCatalog());
            ICharacterContext context = session;

            Assert.AreEqual(16, session.GetStatValue(Constants.STR_SCORE));
            Assert.AreEqual(16, context.GetStatValue(Constants.STR_SCORE));
            Assert.AreEqual(16, context.GetStatistic(Constants.STR_SCORE).Value);
            Assert.AreEqual(50, context.GetStatValue("MissingStat", 50));
            Assert.AreEqual(50, session.GetStatValue("MissingStat", 50));
        }
    }
}
