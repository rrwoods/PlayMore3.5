using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Items;
using PlayMore35.Subsystems;
using System;
using System.Collections.Generic;

namespace PlayMore35.Tests {
    [TestClass]
    public class CombatTransitionTests {
        private class MockCharacterContext : ICharacterContext {
            public Character Character { get; set; }
            public EffectPool Effects { get; set; }
            public Equipped EquippedItems { get; set; }
            public Timeline Timeline => Character?.Timeline;
            public SubsystemRegistry Subsystems { get; } = new SubsystemRegistry();

            public readonly List<(string subsystem, int slotIndex)> ClearedSlots = new List<(string, int)>();

            public void ClearSlot(string subsystem, int slotIndex) {
                ClearedSlots.Add((subsystem, slotIndex));
            }

            public List<AttackRoutine> GetAllRoutines() => new List<AttackRoutine>();
            public List<AttackRoutine> GetVisibleRoutines() => new List<AttackRoutine>();
        }

        private (MockCharacterContext context, Timeline timeline, EffectPool effects) CreateTestContext() {
            var timeline = new Timeline();
            var character = new Character { Timeline = timeline };
            var effects = new EffectPool(character);
            var context = new MockCharacterContext {
                Character = character,
                Effects = effects,
                EquippedItems = new Equipped()
            };
            timeline.Context = context;
            return (context, timeline, effects);
        }

        [TestMethod]
        public void StartCombat_SwitchesCombatMode_AndInitializesEncounterTiming() {
            var (_, timeline, _) = CreateTestContext();
            timeline.Advance(10); // round 10

            Assert.IsFalse(timeline.Combat);

            var result = timeline.StartCombat();

            Assert.IsTrue(timeline.Combat);
            Assert.AreEqual(10L, timeline.EncounterStartRound);
            Assert.AreEqual(timeline.CurrentTime, timeline.EncounterStartTime);
            Assert.AreEqual(TimelineRefreshRequirement.RecomputeDerived, result.RefreshRequirement);

            // Repeated call is no-op
            var secondResult = timeline.StartCombat();
            Assert.AreEqual(TimelineRefreshRequirement.None, secondResult.RefreshRequirement);
        }

        [TestMethod]
        public void EndCombat_SwitchesCombatMode_ZeroesEncounterParameters_AndExecutesEncounterActions() {
            var (context, timeline, effects) = CreateTestContext();
            timeline.StartCombat();

            var encounterParam = new ControllableParameter("encounter_uses", context.Character) {
                Value = 5
            };
            effects.resettableParameters["encounter"].Add(encounterParam);

            var followupFlag = new ControllableFlag("EncounterBuffEnded", context.Character);
            effects.controllableFlags["EncounterBuffEnded"] = followupFlag;

            var encounterAction = new SheetAction()
                .Duration("encounter")
                .AndThen(new SheetAction().SetFlag("EncounterBuffEnded"));

            timeline.Schedule(encounterAction, "encounter", context);

            Assert.AreEqual(5, encounterParam.Value);
            Assert.IsFalse(followupFlag.On);

            var endResult = timeline.EndCombat();

            Assert.IsFalse(timeline.Combat);
            Assert.AreEqual(0, encounterParam.Value);
            Assert.IsTrue(followupFlag.On);
            Assert.AreEqual(1, endResult.ExpiredActions.Count);
            Assert.AreEqual(TimelineRefreshRequirement.RecomputeDerived, endResult.RefreshRequirement);

            // Repeated call is no-op
            var secondResult = timeline.EndCombat();
            Assert.AreEqual(TimelineRefreshRequirement.None, secondResult.RefreshRequirement);
        }
    }
}
