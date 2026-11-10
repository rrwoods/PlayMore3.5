using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Items;
using PlayMore35.Subsystems;
using PlayMore35.Rules;
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
        public void SheetAction_FromJson_DeserializesDeclarativeActions() {
            var vestigeJson = new JObject(
                new JProperty("name", "Unbind Slot 1"),
                new JProperty("subsystem", "pact_magic"),
                new JProperty("slot_group", "vestiges"),
                new JProperty("slot_index", 1),
                new JProperty("verb", "unbind")
            );
            var parsed = SheetAction.FromJson(vestigeJson);

            Assert.IsInstanceOfType(parsed, typeof(SheetAction));
            Assert.AreEqual("pact_magic", parsed.Subsystem);
            Assert.AreEqual("vestiges", parsed.SlotGroup);
            Assert.AreEqual(1, parsed.SlotIndex);
            Assert.AreEqual("unbind", parsed.Verb);
            Assert.AreEqual("Unbind Slot 1", parsed.Name);

            var maneuverJson = new JObject(
                new JProperty("name", "Wind Stride (Swordsage)"),
                new JProperty("subsystem", "martial_maneuvers"),
                new JProperty("slot_group", "maneuvers"),
                new JProperty("element", "Wind Stride"),
                new JProperty("source", "Swordsage"),
                new JProperty("verb", "expend")
            );
            var parsedManeuver = SheetAction.FromJson(maneuverJson);
            Assert.IsInstanceOfType(parsedManeuver, typeof(SheetAction));
            Assert.AreEqual("martial_maneuvers", parsedManeuver.Subsystem);
            Assert.AreEqual("maneuvers", parsedManeuver.SlotGroup);
            Assert.AreEqual("Wind Stride", parsedManeuver.Element);
            Assert.AreEqual("Swordsage", parsedManeuver.Source);
            Assert.AreEqual("expend", parsedManeuver.Verb);

            var standardJson = new JObject(
                new JProperty("name", "Standard Action"),
                new JProperty("set", new JArray("SomeFlag"))
            );
            var parsedStandard = SheetAction.FromJson(standardJson);
            Assert.IsInstanceOfType(parsedStandard, typeof(SheetAction));
        }

        [TestMethod]
        public void DeclarativeSubsystemAction_SerializesAndPerforms_DirectlyOnSubsystemEngine() {
            var action = new SheetAction {
                Name = "Unbind vestige slot 2",
                Subsystem = "pact_magic",
                SlotGroup = "vestiges",
                SlotIndex = 2,
                Verb = "unbind"
            };
            var json = (JObject)action.ToJson();

            Assert.AreEqual("pact_magic", (string)json["subsystem"]);
            Assert.AreEqual("vestiges", (string)json["slot_group"]);
            Assert.AreEqual(2, (int)json["slot_index"]);
            Assert.AreEqual("unbind", (string)json["verb"]);

            var deserialized = SheetAction.FromJson(json);
            Assert.IsNotNull(deserialized);
            Assert.IsTrue(action.Matches(deserialized));

            var (context, _, _) = CreateTestContext();
            var catalog = new GameRulesCatalog();
            var engine = new SubsystemEngine(catalog.GetSubsystem("pact_magic"), context);
            context.SubsystemsMap["pact_magic"] = engine;
            context.Effects.AddStat(new Statistic("number of vestiges"));
            context.Effects.AddEffect(new Effect("Vestige Slot").AddModifier("number of vestiges", new Modifier(3, ModifierTypes.UNTYPED)));
            engine.AssignSlot("vestiges", 2, "Aym", "good_pact", "Binder");
            Assert.AreEqual("Aym", engine.GetSlotElement("vestiges", 2));

            var req = deserialized.Perform(context);
            Assert.AreEqual(TimelineRefreshRequirement.FullReload, req);
            Assert.IsNull(engine.GetSlotElement("vestiges", 2));
            Assert.AreEqual("empty", engine.GetSlotState("vestiges", 2));
        }

        [TestMethod]
        public void DeclarativeSubsystemAction_MatchesAndDefaultName_BehavesCorrectly() {
            var actionSlot0 = new SheetAction { Subsystem = "pact_magic", SlotGroup = "vestiges", SlotIndex = 0, Verb = "unbind" };
            var actionSlot1 = new SheetAction { Subsystem = "pact_magic", SlotGroup = "vestiges", SlotIndex = 1, Verb = "unbind" };
            var standardAction = new SheetAction { Name = "Unbind vestige slot 0" };

            Assert.IsTrue(actionSlot0.Matches(new SheetAction { Subsystem = "pact_magic", SlotGroup = "vestiges", SlotIndex = 0, Verb = "unbind" }));
            Assert.IsFalse(actionSlot0.Matches(actionSlot1));
            Assert.IsFalse(actionSlot0.Matches(standardAction));
            Assert.IsFalse(actionSlot0.Matches(null));

            var json = new JObject(
                new JProperty("name", "Unbind vestige slot 3"),
                new JProperty("subsystem", "pact_magic"),
                new JProperty("slot_group", "vestiges"),
                new JProperty("slot_index", 3),
                new JProperty("verb", "unbind")
            );
            var parsed = SheetAction.FromJson(json);
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
        public void Odric_StandaloneDeserialization_DeserializesDeclarativeSubsystemActionWithoutSessionOrRegistration() {
            var odricPath = TestEnvironment.FindOdricJsonPath();
            var json = JObject.Parse(File.ReadAllText(odricPath));

            var character = new Character(json);

            var scheduledActions = character.Timeline.ActiveActions.ToList();
            var unbindAction = scheduledActions.FirstOrDefault(a => a.Subsystem == "pact_magic" && a.Verb == "unbind");

            Assert.IsNotNull(unbindAction, "Scheduled unbind action in Odric.json should deserialize as declarative SheetAction.");
            Assert.AreEqual("vestiges", unbindAction.SlotGroup);
            Assert.AreEqual(0, unbindAction.SlotIndex);
            Assert.AreEqual("Unbind vestige slot 0", unbindAction.Name);
        }

        [TestMethod]
        public void Timeline_StandaloneDeserialization_DeserializesDeclarativeSubsystemActionWithoutSessionOrRegistration() {
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
                            new JProperty("subsystem", "pact_magic"),
                            new JProperty("slot_group", "vestiges"),
                            new JProperty("slot_index", 0),
                            new JProperty("verb", "unbind")
                        )
                    ))
                )),
                new JProperty("encounter actions", new JArray())
            );

            var timeline = new Timeline(timelineJson);

            var actions = timeline.GetScheduledActions(15);
            Assert.AreEqual(1, actions.Count);
            Assert.AreEqual("pact_magic", actions[0].Subsystem);
            Assert.AreEqual("vestiges", actions[0].SlotGroup);
            Assert.AreEqual(0, actions[0].SlotIndex);
            Assert.AreEqual("unbind", actions[0].Verb);
        }

        private class SubsystemDispatchSpyContext : FakeCharacterContext {
            public bool GetSubsystemCalled { get; private set; }

            public override SubsystemEngine? GetSubsystem(string? id) {
                GetSubsystemCalled = true;
                return base.GetSubsystem(id);
            }
        }

        [TestMethod]
        public void SheetAction_Perform_DispatchesViaGetSubsystem() {
            var context = new SubsystemDispatchSpyContext();
            var catalog = new GameRulesCatalog();
            var engine = new SubsystemEngine(catalog.GetSubsystem("pact_magic"), context);
            context.SubsystemsMap["pact_magic"] = engine;

            context.Effects.AddStat(new Statistic("number of vestiges"));
            context.Effects.AddEffect(new Effect("Vestige Slot").AddModifier("number of vestiges", new Modifier(2, ModifierTypes.UNTYPED)));
            engine.AssignSlot("vestiges", 1, "Aym", "good_pact", "Binder");

            var action = new SheetAction {
                Subsystem = "pact_magic",
                SlotGroup = "vestiges",
                SlotIndex = 1,
                Verb = "unbind"
            };

            var req = action.Perform(context);

            Assert.IsTrue(context.GetSubsystemCalled, "Perform must dispatch via GetSubsystem.");
            Assert.AreEqual(TimelineRefreshRequirement.FullReload, req);
            Assert.IsNull(engine.GetSlotElement("vestiges", 1));
        }
    }
}

