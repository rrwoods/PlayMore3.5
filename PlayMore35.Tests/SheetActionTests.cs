using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Items;
using PlayMore35.Subsystems;
using PlayMore35.Subsystems.PactMagic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PlayMore35.Tests {
    [TestClass]
    public class SheetActionTests {
        private (FakeCharacterContext context, ControllableFlag flag, ControllableParameter param) CreateTestContext() {
            var character = new Character();
            var timeline = new Timeline();
            character.Timeline = timeline;

            var effects = new EffectPool(character);
            var flag = new ControllableFlag("TestFlag", character);
            var param = new ControllableParameter("TestParam", character);

            effects.controllableFlags["TestFlag"] = flag;
            effects.controllableParameters["TestParam"] = param;

            var context = new FakeCharacterContext(character, effects, new Equipped());
            return (context, flag, param);
        }

        [TestCleanup]
        public void Cleanup() {
            SheetAction.ResetParsersForTesting();
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
        public void SheetAction_RegisterParser_AndFromJson_DispatchesPolymorphicActions() {
            var vestigeJson = new JObject(
                new JProperty("name", "Unbind Slot 1"),
                new JProperty("unbind_vestige", 1)
            );
            var parsed = SheetAction.FromJson(vestigeJson);

            Assert.IsInstanceOfType(parsed, typeof(UnbindVestigeAction));
            var unbindAction = (UnbindVestigeAction)parsed;
            Assert.AreEqual(1, unbindAction.SlotIndex);
            Assert.AreEqual("Unbind Slot 1", unbindAction.Name);

            var standardJson = new JObject(
                new JProperty("name", "Standard Action"),
                new JProperty("set", new JArray("SomeFlag"))
            );
            var parsedStandard = SheetAction.FromJson(standardJson);
            Assert.IsInstanceOfType(parsedStandard, typeof(SheetAction));
            Assert.IsNotInstanceOfType(parsedStandard, typeof(UnbindVestigeAction));
        }

        private class CustomTestAction : SheetAction {
            public string CustomField { get; set; }
            public CustomTestAction(JObject json) : base(json) {
                CustomField = (string)json["custom_action"];
            }
            public static SheetAction TryParse(JObject json) {
                return json["custom_action"] != null ? new CustomTestAction(json) : null;
            }
        }

        [TestMethod]
        public void SheetAction_RegisterParser_AndResetParsersForTesting_ManagesLifecycle() {
            try {
                SheetAction.RegisterParser(CustomTestAction.TryParse);
                var json = new JObject(new JProperty("custom_action", "hello"));
                var parsed = SheetAction.FromJson(json);
                Assert.IsInstanceOfType(parsed, typeof(CustomTestAction));
                Assert.AreEqual("hello", ((CustomTestAction)parsed).CustomField);
            } finally {
                SheetAction.ResetParsersForTesting();
            }

            var postResetParsed = SheetAction.FromJson(new JObject(new JProperty("custom_action", "hello")));
            Assert.IsNotInstanceOfType(postResetParsed, typeof(CustomTestAction));
            Assert.IsInstanceOfType(postResetParsed, typeof(SheetAction));

            var unbindJson = new JObject(new JProperty("unbind_vestige", 0));
            var unbindParsed = SheetAction.FromJson(unbindJson);
            Assert.IsInstanceOfType(unbindParsed, typeof(UnbindVestigeAction));
        }

        [TestMethod]
        public void UnbindVestigeAction_SerializesAndPerforms_DirectlyOnPactMagicEngine() {
            var action = new UnbindVestigeAction(2);
            var json = (JObject)action.ToJson();

            Assert.AreEqual(2, (int)json["unbind_vestige"]);

            var deserialized = SheetAction.FromJson(json);
            Assert.IsInstanceOfType(deserialized, typeof(UnbindVestigeAction));
            Assert.IsTrue(action.Matches(deserialized));

            var (context, _, _) = CreateTestContext();
            var engine = new PactMagicEngine();
            context.Subsystems.Register(engine);
            engine.Initialize(context);

            var req = deserialized.Perform(context);
            Assert.AreEqual(TimelineRefreshRequirement.FullReload, req);
        }

        [TestMethod]
        public void UnbindVestigeAction_MatchesAndDefaultName_BehavesCorrectly() {
            var actionSlot0 = new UnbindVestigeAction(0);
            var actionSlot1 = new UnbindVestigeAction(1);
            var standardAction = new SheetAction { Name = "Unbind vestige slot 0" };

            Assert.IsTrue(actionSlot0.Matches(new UnbindVestigeAction(0)));
            Assert.IsFalse(actionSlot0.Matches(actionSlot1));
            Assert.IsFalse(actionSlot0.Matches(standardAction));
            Assert.IsFalse(actionSlot0.Matches(null));

            var jsonWithoutName = new JObject(new JProperty("unbind_vestige", 3));
            var parsed = new UnbindVestigeAction(jsonWithoutName);
            Assert.AreEqual("Unbind vestige slot 3", parsed.Name);
            Assert.AreEqual(3, parsed.SlotIndex);

            Assert.IsNull(SheetAction.FromJson(null));
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

        [TestMethod]
        public void Odric_StandaloneDeserialization_DeserializesUnbindVestigeActionWithoutSessionOrRegistration() {
            var odricPath = TestEnvironment.FindOdricJsonPath();
            var json = JObject.Parse(File.ReadAllText(odricPath));

            var character = new Character(json);

            var scheduledActions = character.Timeline.ScheduledActions.Values.SelectMany(x => x).ToList();
            var unbindAction = scheduledActions.OfType<UnbindVestigeAction>().FirstOrDefault();

            Assert.IsNotNull(unbindAction, "Scheduled unbind_vestige action in Odric.json should deserialize as UnbindVestigeAction.");
            Assert.AreEqual(0, unbindAction.SlotIndex);
            Assert.AreEqual("Unbind vestige slot 0", unbindAction.Name);
        }

        [TestMethod]
        public void Timeline_StandaloneDeserialization_DeserializesUnbindVestigeActionWithoutSessionOrRegistration() {
            var timelineJson = new JObject(
                new JProperty("round zero", "2026-10-06T20:11:30.0000000"),
                new JProperty("encounter start time", "2026-10-06T20:12:00.0000000"),
                new JProperty("encounter start round", 5),
                new JProperty("current round", 10),
                new JProperty("combat", false),
                new JProperty("scheduled actions", new JObject(
                    new JProperty("15", new JArray(
                        new JObject(
                            new JProperty("name", "Unbind vestige slot 0"),
                            new JProperty("unbind_vestige", 0)
                        )
                    ))
                )),
                new JProperty("encounter actions", new JArray())
            );

            var timeline = new Timeline(timelineJson);

            var actions = timeline.ScheduledActions[15];
            Assert.AreEqual(1, actions.Count);
            Assert.IsInstanceOfType(actions[0], typeof(UnbindVestigeAction));
            var unbind = (UnbindVestigeAction)actions[0];
            Assert.AreEqual(0, unbind.SlotIndex);
        }
    }
}

