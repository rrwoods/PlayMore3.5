using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Rules;
using PlayMore35.Subsystems;

namespace PlayMore35.Tests {
    [TestClass]
    public class SubsystemVerbPipelineTests {
        private (SubsystemEngine engine, SubsystemVerbPipeline pipeline) CreateAlchemyEngine(Random? rng = null) {
            var def = FakeRulesCatalog.CreateSyntheticAlchemySubsystem();
            var context = new FakeCharacterContext();
            var engine = new SubsystemEngine(def, context);
            context.SubsystemsMap[def.Id] = engine;
            if (rng != null) {
                engine.SetRandom(rng);
            }
            return (engine, engine.VerbPipeline);
        }

        private (SubsystemEngine engine, SubsystemVerbPipeline pipeline) CreateManeuverEngine(Random? rng = null) {
            var catalog = new FakeRulesCatalog();
            catalog.TryGetSubsystem("martial_maneuvers", out var def);
            Assert.IsNotNull(def);
            var context = new FakeCharacterContext(catalog: catalog);
            var engine = new SubsystemEngine(def, context);
            if (rng != null) {
                engine.SetRandom(rng);
            }
            return (engine, engine.VerbPipeline);
        }

        #region Isolated Step Cascades

        [TestMethod]
        public void ExecuteVerb_MultiStepCascade_ExecutesSequentialSteps() {
            var (engine, pipeline) = CreateAlchemyEngine();

            // Set up a custom multi-step cascade:
            // Step 1: transition flask from prepared -> expended
            // Step 2: consume 2 catalyst
            // Step 3: recharge 1 heat
            engine.Definition.Verbs["alchemical_burst"] = new List<SubsystemStep> {
                new SubsystemStep(type: "transition", slotGroup: "flasks", selector: "invoked", fromState: "prepared", toState: "expended"),
                new SubsystemStep(type: "pool", pool: "catalyst", op: "consume", amount: 2),
                new SubsystemStep(type: "pool", pool: "heat", op: "recharge", amount: 1)
            };

            engine.AssignSlot("flasks", 0, "Acid Flask", state: "prepared");
            engine.SetPoolValue("catalyst", 10);
            engine.SetPoolValue("heat", 1);

            var req = pipeline.ExecuteVerb("alchemical_burst", slotGroup: "flasks", slotIndex: 0);

            Assert.AreEqual(TimelineRefreshRequirement.FullReload, req);
            Assert.AreEqual("expended", engine.GetSlotState("flasks", 0));
            Assert.AreEqual(8, engine.GetPoolValue("catalyst"));
            Assert.AreEqual(2, engine.GetPoolValue("heat"));
        }

        [TestMethod]
        public void ExecuteVerb_MultiStepCascade_HaltsImmediately_WhenTransitionStepFails() {
            var (engine, pipeline) = CreateAlchemyEngine();

            // Step 1 expects fromState: "prepared", but slot is "empty"
            engine.Definition.Verbs["alchemical_burst"] = new List<SubsystemStep> {
                new SubsystemStep(type: "transition", slotGroup: "flasks", selector: "invoked", fromState: "prepared", toState: "expended"),
                new SubsystemStep(type: "pool", pool: "catalyst", op: "consume", amount: 3)
            };

            engine.AssignSlot("flasks", 0, "Acid Flask", state: "empty");
            engine.SetPoolValue("catalyst", 10);

            var req = pipeline.ExecuteVerb("alchemical_burst", slotGroup: "flasks", slotIndex: 0);

            Assert.AreEqual(TimelineRefreshRequirement.None, req);
            Assert.AreEqual("empty", engine.GetSlotState("flasks", 0));
            // Cascade must halt: catalyst must NOT be consumed
            Assert.AreEqual(10, engine.GetPoolValue("catalyst"));
        }

        [TestMethod]
        public void ExecuteVerb_CascadeWithInvokeElementVerb_InvokesNestedElementSteps() {
            var (engine, pipeline) = CreateAlchemyEngine();

            // Element "Fire Flask" has an element verb "detonate" that consumes 2 heat
            var fireFlask = engine.GetElement("Fire Flask");
            Assert.IsNotNull(fireFlask);
            fireFlask.Verbs["detonate"] = new List<SubsystemStep> {
                new SubsystemStep(type: "pool", pool: "heat", op: "consume", amount: 2)
            };

            engine.Definition.Verbs["throw_and_detonate"] = new List<SubsystemStep> {
                new SubsystemStep(type: "transition", slotGroup: "flasks", selector: "invoked", fromState: "prepared", toState: "expended"),
                new SubsystemStep(type: "invoke_element_verb", verb: "detonate")
            };

            engine.AssignSlot("flasks", 0, "Fire Flask", state: "prepared");
            engine.SetPoolValue("heat", 5);

            var req = pipeline.ExecuteVerb("throw_and_detonate", slotGroup: "flasks", slotIndex: 0);

            Assert.AreEqual(TimelineRefreshRequirement.FullReload, req);
            Assert.AreEqual("expended", engine.GetSlotState("flasks", 0));
            Assert.AreEqual(3, engine.GetPoolValue("heat"));
        }

        [TestMethod]
        public void ExecuteStep_AssignStep_DirectlyAssignsSlotAndState() {
            var (engine, pipeline) = CreateAlchemyEngine();

            var assignStep = new SubsystemStep(
                type: "assign",
                slotGroup: "flasks",
                toState: "prepared"
            );

            var req = pipeline.ExecuteStep(assignStep, slotGroup: "flasks", targetSlotIndex: 1, targetElement: "Acid Flask");

            Assert.AreEqual(TimelineRefreshRequirement.FullReload, req);
            var slot = engine.GetSlots("flasks")[1];
            Assert.AreEqual("Acid Flask", slot.Element);
            Assert.AreEqual("prepared", slot.State);
        }

        [TestMethod]
        public void ExecuteStep_ActionStep_ExecutesSheetAction() {
            var (engine, pipeline) = CreateAlchemyEngine();

            // Create a custom SheetAction that recharges a pool
            var actionStep = new SubsystemStep(
                type: "action",
                action: new SheetAction {
                    Subsystem = "synthetic_alchemy",
                    PoolOperation = new SubsystemPoolOperation("catalyst", "recharge", 4)
                }
            );

            engine.SetPoolValue("catalyst", 2);
            var req = pipeline.ExecuteStep(actionStep);

            Assert.AreEqual(TimelineRefreshRequirement.RecomputeDerived, req);
            Assert.AreEqual(6, engine.GetPoolValue("catalyst"));
        }

        #endregion

        #region Random Selection Fallback (on_empty_verb)

        [TestMethod]
        public void ExecuteTransition_RandomSelector_TransitionsCandidate_WithoutExecutingFallback() {
            var (engine, pipeline) = CreateManeuverEngine(new Random(42));

            engine.AssignSlot("maneuvers", 0, "Steel Wind", state: "withheld");
            engine.AssignSlot("maneuvers", 1, "Sapphire Nightmare Blade", state: "withheld");

            var transition = new SubsystemTransition {
                SlotGroup = "maneuvers",
                Selector = "random",
                FromState = "withheld",
                ToState = "available",
                CountExpression = "1",
                OnEmptyVerb = "deal"
            };

            var req = pipeline.ExecuteTransition(transition);

            Assert.AreEqual(TimelineRefreshRequirement.FullReload, req);
            var slots = engine.GetSlots("maneuvers");
            int availableCount = slots.Count(s => s.State == "available");
            int withheldCount = slots.Count(s => s.State == "withheld");
            Assert.AreEqual(1, availableCount);
            Assert.AreEqual(1, withheldCount);
        }

        [TestMethod]
        public void ExecuteTransition_RandomSelector_ExecutesOnEmptyVerb_WhenNoCandidatesMatch() {
            var (engine, pipeline) = CreateManeuverEngine(new Random(42));

            // Set up all slots in expended (zero in withheld)
            engine.AssignSlot("maneuvers", 0, "Steel Wind", state: "expended");
            engine.AssignSlot("maneuvers", 1, "Sapphire Nightmare Blade", state: "expended");

            // Define "deal" verb that moves expended -> withheld, then transitions 1 withheld -> available
            engine.Definition.Verbs["deal"] = new List<SubsystemStep> {
                new SubsystemStep(type: "transition", slotGroup: "maneuvers", selector: "all", fromState: "expended", toState: "withheld"),
                new SubsystemStep(type: "transition", slotGroup: "maneuvers", selector: "random", countExpression: "1", fromState: "withheld", toState: "available")
            };

            var transition = new SubsystemTransition {
                SlotGroup = "maneuvers",
                Selector = "random",
                FromState = "withheld",
                ToState = "available",
                CountExpression = "1",
                OnEmptyVerb = "deal"
            };

            var req = pipeline.ExecuteTransition(transition);

            Assert.AreEqual(TimelineRefreshRequirement.FullReload, req);
            var slots = engine.GetSlots("maneuvers");
            // Exactly 1 was dealt to available, and 1 remains in withheld
            Assert.AreEqual(1, slots.Count(s => s.State == "available"));
            Assert.AreEqual(1, slots.Count(s => s.State == "withheld"));
            Assert.AreEqual(0, slots.Count(s => s.State == "expended"));
        }

        [TestMethod]
        public void ExecuteTransition_RandomSelector_TransitionsMultipleCandidates_WhenCountExceedsOne() {
            var (engine, pipeline) = CreateManeuverEngine(new Random(42));

            engine.AssignSlot("maneuvers", 0, "Steel Wind", state: "withheld");
            engine.AssignSlot("maneuvers", 1, "Sapphire Nightmare Blade", state: "withheld");
            engine.AssignSlot("maneuvers", 2, "Leading the Attack", state: "withheld");

            var transition = new SubsystemTransition {
                SlotGroup = "maneuvers",
                Selector = "random",
                FromState = "withheld",
                ToState = "available",
                CountExpression = "2"
            };

            var req = pipeline.ExecuteTransition(transition);

            Assert.AreEqual(TimelineRefreshRequirement.FullReload, req);
            var slots = engine.GetSlots("maneuvers");
            Assert.AreEqual(2, slots.Count(s => s.State == "available"));
            Assert.AreEqual(1, slots.Count(s => s.State == "withheld"));
        }

        [TestMethod]
        public void ExecuteTransition_RandomSelector_TransitionsAllCandidates_WhenCountExceedsCandidateTotal() {
            var (engine, pipeline) = CreateManeuverEngine(new Random(42));

            engine.AssignSlot("maneuvers", 0, "Steel Wind", state: "withheld");
            engine.AssignSlot("maneuvers", 1, "Sapphire Nightmare Blade", state: "withheld");

            var transition = new SubsystemTransition {
                SlotGroup = "maneuvers",
                Selector = "random",
                FromState = "withheld",
                ToState = "available",
                CountExpression = "5"
            };

            var req = pipeline.ExecuteTransition(transition);

            Assert.AreEqual(TimelineRefreshRequirement.FullReload, req);
            var slots = engine.GetSlots("maneuvers");
            Assert.AreEqual(2, slots.Count(s => s.State == "available"));
            Assert.AreEqual(0, slots.Count(s => s.State == "withheld"));
        }

        #endregion

        #region Active Slot Clamping

        [TestMethod]
        public void ExecuteTransition_ActiveSlotClamping_DeactivatesOldestSlot_WhenExceedingCapacity() {
            var (engine, pipeline) = CreateManeuverEngine();

            // stances capacity is 1 by default
            engine.AssignSlot("stances", 0, "Iron Guard's Glare", state: "active");
            engine.AssignSlot("stances", 1, "Thicket of Blades", state: "inactive");

            var transition = new SubsystemTransition {
                SlotGroup = "stances",
                Selector = "invoked",
                FromState = "*",
                ToState = "active",
                SlotIndex = 1
            };

            var req = pipeline.ExecuteTransition(transition);

            Assert.AreEqual(TimelineRefreshRequirement.FullReload, req);
            Assert.AreEqual("inactive", engine.GetSlotState("stances", 0));
            Assert.AreEqual("active", engine.GetSlotState("stances", 1));
        }

        [TestMethod]
        public void ExecuteTransition_ActiveSlotClamping_UsesSelectedIndices_ToDeactivateSpecificSlot() {
            var (engine, pipeline) = CreateManeuverEngine();

            // Set active stances capacity to 2 via context
            var context = (FakeCharacterContext)engine.Context;
            context.Effects.AddStat(new Statistic("active stances"));
            context.Effects.AddEffect(new Effect("Dual Stance").AddModifier("active stances", new Modifier(2, "untyped")));

            engine.AssignSlot("stances", 0, "Stance A", state: "active");
            engine.AssignSlot("stances", 1, "Stance B", state: "active");
            engine.AssignSlot("stances", 2, "Stance C", state: "inactive");

            // Deactivate Slot 1 ("Stance B") explicitly via SelectedIndices
            var transition = new SubsystemTransition {
                SlotGroup = "stances",
                Selector = "invoked",
                FromState = "*",
                ToState = "active",
                SlotIndex = 2,
                SelectedIndices = new List<int> { 1 }
            };

            var req = pipeline.ExecuteTransition(transition);

            Assert.AreEqual(TimelineRefreshRequirement.FullReload, req);
            Assert.AreEqual("active", engine.GetSlotState("stances", 0));
            Assert.AreEqual("inactive", engine.GetSlotState("stances", 1));
            Assert.AreEqual("active", engine.GetSlotState("stances", 2));
        }

        [TestMethod]
        public void ExecuteTransition_ActiveSlotClamping_PreservesFifoOrder_AcrossConsecutiveTransitions() {
            var (engine, pipeline) = CreateManeuverEngine();

            // stances capacity is 1
            engine.AssignSlot("stances", 0, "Stance 0", state: "inactive");
            engine.AssignSlot("stances", 1, "Stance 1", state: "inactive");
            engine.AssignSlot("stances", 2, "Stance 2", state: "inactive");

            // Activate Slot 0
            pipeline.ExecuteTransition(new SubsystemTransition {
                SlotGroup = "stances", Selector = "invoked", FromState = "*", ToState = "active", SlotIndex = 0
            });
            Assert.AreEqual("active", engine.GetSlotState("stances", 0));

            // Activate Slot 1 (clamps Slot 0)
            pipeline.ExecuteTransition(new SubsystemTransition {
                SlotGroup = "stances", Selector = "invoked", FromState = "*", ToState = "active", SlotIndex = 1
            });
            Assert.AreEqual("inactive", engine.GetSlotState("stances", 0));
            Assert.AreEqual("active", engine.GetSlotState("stances", 1));

            // Activate Slot 2 (clamps Slot 1)
            pipeline.ExecuteTransition(new SubsystemTransition {
                SlotGroup = "stances", Selector = "invoked", FromState = "*", ToState = "active", SlotIndex = 2
            });
            Assert.AreEqual("inactive", engine.GetSlotState("stances", 0));
            Assert.AreEqual("inactive", engine.GetSlotState("stances", 1));
            Assert.AreEqual("active", engine.GetSlotState("stances", 2));
        }

        [TestMethod]
        public void ExecuteTransition_ActiveSlotClamping_NoOp_WhenSlotAlreadyActive() {
            var (engine, pipeline) = CreateManeuverEngine();

            engine.AssignSlot("stances", 0, "Stance 0", state: "active");
            engine.AssignSlot("stances", 1, "Stance 1", state: "inactive");

            var transition = new SubsystemTransition {
                SlotGroup = "stances",
                Selector = "invoked",
                FromState = "*",
                ToState = "active",
                SlotIndex = 0
            };

            var req = pipeline.ExecuteTransition(transition);

            Assert.AreEqual(TimelineRefreshRequirement.FullReload, req);
            Assert.AreEqual("active", engine.GetSlotState("stances", 0));
            Assert.AreEqual("inactive", engine.GetSlotState("stances", 1));
        }

        #endregion

        #region Pool Operations

        [TestMethod]
        public void ExecutePoolOperation_Consume_DecrementsPoolValue() {
            var (engine, pipeline) = CreateAlchemyEngine();

            engine.SetPoolValue("catalyst", 10);
            var op = new SubsystemPoolOperation("catalyst", "consume", 4);

            var req = pipeline.ExecutePoolOperation(op);

            Assert.AreEqual(TimelineRefreshRequirement.RecomputeDerived, req);
            Assert.AreEqual(6, engine.GetPoolValue("catalyst"));
        }

        [TestMethod]
        public void ExecutePoolOperation_Recharge_IncrementsPoolValue() {
            var (engine, pipeline) = CreateAlchemyEngine();

            engine.SetPoolValue("heat", 2);
            var op = new SubsystemPoolOperation("heat", "recharge", 3);

            var req = pipeline.ExecutePoolOperation(op);

            Assert.AreEqual(TimelineRefreshRequirement.RecomputeDerived, req);
            Assert.AreEqual(5, engine.GetPoolValue("heat"));
        }

        [TestMethod]
        public void ExecutePoolOperation_Set_SetsPoolValueDirectly() {
            var (engine, pipeline) = CreateAlchemyEngine();

            engine.SetPoolValue("catalyst", 2);
            var op = new SubsystemPoolOperation("catalyst", "set", 8);

            var req = pipeline.ExecutePoolOperation(op);

            Assert.AreEqual(TimelineRefreshRequirement.RecomputeDerived, req);
            Assert.AreEqual(8, engine.GetPoolValue("catalyst"));
        }

        [TestMethod]
        public void ExecutePoolOperation_NullOrEmptyOperation_ReturnsNoneWithoutMutation() {
            var (engine, pipeline) = CreateAlchemyEngine();

            engine.SetPoolValue("heat", 2);

            Assert.AreEqual(TimelineRefreshRequirement.None, pipeline.ExecutePoolOperation(null));
            Assert.AreEqual(TimelineRefreshRequirement.None, pipeline.ExecutePoolOperation(new SubsystemPoolOperation("", "consume", 1)));
            Assert.AreEqual(2, engine.GetPoolValue("heat"));
        }

        #endregion

        #region SubsystemStep Encapsulation

        [TestMethod]
        public void SubsystemStep_PropertiesAreImmutable_AndPreserveConstructorValues() {
            var action = new SheetAction();
            var step = new SubsystemStep(
                type: "transition",
                slotGroup: "maneuvers",
                selector: "select",
                fromState: "withheld",
                toState: "available",
                countExpression: "2",
                onEmptyVerb: "deal",
                verb: "strike",
                action: action,
                pool: "catalyst",
                op: "recharge",
                amount: 5
            );

            Assert.AreEqual("transition", step.Type);
            Assert.AreEqual("maneuvers", step.SlotGroup);
            Assert.AreEqual("select", step.Selector);
            Assert.AreEqual("withheld", step.FromState);
            Assert.AreEqual("available", step.ToState);
            Assert.AreEqual("2", step.CountExpression);
            Assert.AreEqual("deal", step.OnEmptyVerb);
            Assert.AreEqual("strike", step.Verb);
            Assert.AreSame(action, step.Action);
            Assert.AreEqual("catalyst", step.Pool);
            Assert.AreEqual("recharge", step.Op);
            Assert.AreEqual(5, step.Amount);
            Assert.IsTrue(step.IsTransition);
        }

        [TestMethod]
        public void SubsystemStep_IsTransition_ReturnsFalseForNonTransition() {
            var step = new SubsystemStep(type: "pool", pool: "heat", op: "consume", amount: 1);
            Assert.IsFalse(step.IsTransition);
        }

        [TestMethod]
        public void SubsystemStep_ToJson_ProducesExpectedFormat() {
            var step = new SubsystemStep(
                type: "transition",
                slotGroup: "flasks",
                selector: "invoked",
                fromState: "prepared",
                toState: "expended"
            );

            var json = step.ToJson();

            Assert.AreEqual("transition", (string?)json["type"]);
            Assert.AreEqual("flasks", (string?)json["slot_group"]);
            Assert.AreEqual("invoked", (string?)json["selector"]);
            Assert.AreEqual("prepared", (string?)json["from"]);
            Assert.AreEqual("expended", (string?)json["to"]);
        }

        #endregion
    }
}
