using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Items;
using PlayMore35.Subsystems;
using System.Collections.Generic;

namespace PlayMore35.Tests {
    [TestClass]
    public class SheetActionTests {
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

        private (MockCharacterContext context, ControllableFlag flag, ControllableParameter param) CreateTestContext() {
            var character = new Character();
            var timeline = new Timeline();
            character.Timeline = timeline;

            var effects = new EffectPool(character);
            var flag = new ControllableFlag("TestFlag", character);
            var param = new ControllableParameter("TestParam", character);

            effects.controllableFlags["TestFlag"] = flag;
            effects.controllableParameters["TestParam"] = param;

            var context = new MockCharacterContext {
                Character = character,
                Effects = effects,
                EquippedItems = new Equipped()
            };
            timeline.Context = context;

            return (context, flag, param);
        }

        [TestMethod]
        public void SheetAction_PerformsOnContext_AdjustsFlagsAndParams() {
            var (context, flag, param) = CreateTestContext();

            var action = new SheetAction()
                .SetFlag("TestFlag")
                .AdjustParam("TestParam", 5);

            var requirement = action.Perform(context);

            Assert.IsTrue(flag.On);
            Assert.AreEqual(5, param.Value);
            Assert.AreEqual(TimelineRefreshRequirement.RecomputeDerived, requirement);
        }

        [TestMethod]
        public void SheetAction_ClearSlot_InvokesGenericSlotClearance() {
            var (context, _, _) = CreateTestContext();

            var action = new SheetAction()
                .ClearSlot("vestige", 1);

            var requirement = action.Perform(context);

            Assert.AreEqual(1, context.ClearedSlots.Count);
            Assert.AreEqual("vestige", context.ClearedSlots[0].subsystem);
            Assert.AreEqual(1, context.ClearedSlots[0].slotIndex);
            Assert.AreEqual(TimelineRefreshRequirement.FullReload, requirement);
        }

        [TestMethod]
        public void SheetAction_DeserializesLegacyUnbindJson_IntoGenericClearSlot() {
            var (context, _, _) = CreateTestContext();

            var json = new JObject(
                new JProperty("unbind", new JArray(0, 2))
            );
            var action = new SheetAction(json);

            var requirement = action.Perform(context);

            Assert.AreEqual(2, context.ClearedSlots.Count);
            Assert.AreEqual(("vestige", 0), context.ClearedSlots[0]);
            Assert.AreEqual(("vestige", 2), context.ClearedSlots[1]);
            Assert.AreEqual(TimelineRefreshRequirement.FullReload, requirement);
        }

        [TestMethod]
        public void SheetAction_WithDuration_SchedulesReversionOnTimeline() {
            var (context, flag, param) = CreateTestContext();

            var action = new SheetAction()
                .SetFlag("TestFlag")
                .AdjustParam("TestParam", 4)
                .Duration("10 rounds");

            action.Perform(context);

            Assert.IsTrue(flag.On);
            Assert.AreEqual(4, param.Value);

            // Reversion should not have executed yet at round 0
            Assert.AreEqual(0L, context.Timeline.CurrentRound);

            // Advance 9 rounds: still active
            context.Timeline.Advance(9);
            Assert.IsTrue(flag.On);
            Assert.AreEqual(4, param.Value);

            // Advance 1 more round to round 10: reversion fires!
            var advanceResult = context.Timeline.Advance(1);
            Assert.IsFalse(flag.On);
            Assert.AreEqual(0, param.Value);
            Assert.AreEqual(TimelineRefreshRequirement.RecomputeDerived, advanceResult.RefreshRequirement);
        }

        [TestMethod]
        public void SheetAction_ChainedAction_FiresUponExpiration() {
            var (context, flag, _) = CreateTestContext();
            var fatiguedFlag = new ControllableFlag("Fatigued", context.Character);
            context.Effects.controllableFlags["Fatigued"] = fatiguedFlag;

            var action = new SheetAction()
                .SetFlag("TestFlag")
                .Duration("5 rounds")
                .AndThen(new SheetAction().SetFlag("Fatigued"));

            action.Perform(context);

            Assert.IsTrue(flag.On);
            Assert.IsFalse(fatiguedFlag.On);

            // Advance 4 rounds
            context.Timeline.Advance(4);
            Assert.IsTrue(flag.On);
            Assert.IsFalse(fatiguedFlag.On);

            // Advance 1 round to round 5: duration expires, TestFlag reverts, Fatigued sets to true
            var advanceResult = context.Timeline.Advance(1);
            Assert.IsFalse(flag.On);
            Assert.IsTrue(fatiguedFlag.On);
            Assert.AreEqual(TimelineRefreshRequirement.RecomputeDerived, advanceResult.RefreshRequirement);
        }

        [TestMethod]
        public void SheetAction_ToJson_SerializesClearSlotWithoutRedundantUnbind() {
            var action = new SheetAction()
                .ClearSlot("vestige", 0);

            var json = (JObject)action.ToJson();

            Assert.IsNotNull(json["clear_slots"]);
            Assert.IsNull(json["unbind"], "Serialized SheetAction must not contain redundant unbind key.");
        }

        [TestMethod]
        public void SheetAction_Name_RoundTripsThroughJsonAndMatches() {
            var namedAction = new SheetAction { Name = "Custom Action" }
                .AdjustParam("TestParam", 1);

            var json = (JObject)namedAction.ToJson();
            Assert.AreEqual("Custom Action", (string)json["name"]);

            var deserialized = new SheetAction(json);
            Assert.AreEqual("Custom Action", deserialized.Name);
            Assert.IsTrue(namedAction.Matches(deserialized));

            var differentlyNamed = new SheetAction { Name = "Other Action" }
                .AdjustParam("TestParam", 1);
            Assert.IsFalse(namedAction.Matches(differentlyNamed));

            var unnamedAction = new SheetAction().AdjustParam("TestParam", 1);
            var unnamedJson = (JObject)unnamedAction.ToJson();
            Assert.IsNull(unnamedJson["name"]);
            Assert.IsFalse(namedAction.Matches(unnamedAction));
        }

        [TestMethod]
        public void Effect_ParameterAndAssociatedActions_RoundTripThroughJsonAndApplyChoice() {
            var action = new SheetAction { Name = "Trigger Counter" }
                .AdjustParam("Test Effect counter", 1);
            var effect = new Effect("Test Effect") {
                Parameter = "counter ($)"
            };
            effect.associatedAction.Add(action);
            effect.AddText("Actions", "A test effect with $.");

            var json = (JObject)effect.ToJson();
            Assert.AreEqual("counter ($)", (string)json["parameter"]);
            Assert.IsNotNull(json["actions"]);

            var deserialized = new Effect(json);
            Assert.AreEqual("counter ($)", deserialized.Parameter);
            Assert.AreEqual(1, deserialized.associatedAction.Count);
            Assert.AreEqual("Trigger Counter", deserialized.associatedAction[0].Name);
            Assert.IsTrue(action.Matches(deserialized.associatedAction[0]));

            var applied = deserialized.ApplyChoice("alpha");
            Assert.AreEqual("counter (alpha)", applied.Parameter);
            Assert.AreEqual(1, applied.associatedAction.Count);
            Assert.IsTrue(action.Matches(applied.associatedAction[0]));
        }
    }
}

