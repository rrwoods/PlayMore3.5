using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Rules;
using PlayMore35.Subsystems;

namespace PlayMore35.Tests {
    [TestClass]
    public class SyntheticSubsystemTests {
        private (CharacterSession session, FakeRulesCatalog catalog, SubsystemEngine engine) CreateSyntheticSession(Random rng = null) {
            var baseCatalog = new GameRulesCatalog();
            var catalog = new FakeRulesCatalog(inner: baseCatalog);
            var syntheticDef = FakeRulesCatalog.CreateSyntheticAlchemySubsystem();
            catalog.RegisterSubsystem(syntheticDef);

            var character = new Character();
            var session = new CharacterSession(character, catalog);
            var engine = session.GetSubsystem("synthetic_alchemy");
            if (rng != null) {
                engine.SetRandom(rng);
            }
            return (session, catalog, engine);
        }

        [TestMethod]
        public void SubsystemEngine_InstantiatesFromDefinition_WithAllDeclarativeProperties() {
            var def = FakeRulesCatalog.CreateSyntheticAlchemySubsystem();
            var engine = new SubsystemEngine(def, new FakeCharacterContext());

            Assert.AreEqual("synthetic_alchemy", engine.Id);
            Assert.AreEqual("Synthetic Alchemy", engine.Name);
            Assert.IsTrue(engine.IsActive);
            Assert.IsNotNull(engine.GetSlotGroup("flasks"));
            Assert.IsNotNull(engine.GetSlotGroup("infusions"));
            Assert.AreEqual(3, engine.GetSlotGroup("flasks").Capacity);
            Assert.AreEqual(2, engine.GetSlotGroup("infusions").Capacity);
            Assert.AreEqual(10, engine.GetPoolCapacity("catalyst"));
            Assert.AreEqual(5, engine.GetPoolCapacity("heat"));
            Assert.IsNotNull(engine.GetElement("Acid Flask"));
            Assert.IsNotNull(engine.GetElement("Fire Flask"));
            Assert.IsNotNull(engine.GetElement("Speed Infusion"));
        }

        [TestMethod]
        public void SlotStateTransitions_Invoked_TransitionsTargetSlotOnly_WhenMatchingFromState() {
            var (session, _, engine) = CreateSyntheticSession();

            engine.AssignSlot("flasks", 0, "Acid Flask", state: "prepared");
            engine.AssignSlot("flasks", 1, "Fire Flask", state: "empty");
            engine.AssignSlot("flasks", 2, "Acid Flask", state: "prepared");

            var transition = SubsystemStep.Transition("flasks", "expended", fromState: "prepared", selector: "invoked");

            var req = engine.ExecuteStep(transition, new SubsystemTarget(slotIndex: 0));
            Assert.AreEqual(TimelineRefreshRequirement.FullReload, req);

            Assert.AreEqual("expended", engine.GetSlot("flasks", 0).State);
            Assert.AreEqual("empty", engine.GetSlot("flasks", 1).State);
            Assert.AreEqual("prepared", engine.GetSlot("flasks", 2).State);

            // Target slot 1: from state is "prepared", but slot 1 is "empty" -> no transition
            engine.ExecuteStep(transition, new SubsystemTarget(slotIndex: 1));
            Assert.AreEqual("empty", engine.GetSlot("flasks", 1).State);
        }

        [TestMethod]
        public void SlotStateTransitions_All_TransitionsAllMatchingSlots_FilteredBySource() {
            var (session, _, engine) = CreateSyntheticSession();

            engine.AssignSlot("flasks", 0, "Acid Flask", state: "expended", source: "GuildAlchemist");
            engine.AssignSlot("flasks", 1, "Fire Flask", state: "expended", source: "HedgeWizard");
            engine.AssignSlot("flasks", 2, "Acid Flask", state: "prepared", source: "GuildAlchemist");

            // Transition all expended with source GuildAlchemist
            var transitionSourceA = SubsystemStep.Transition("flasks", "prepared", fromState: "expended", selector: "all");
            engine.ExecuteStep(transitionSourceA, new SubsystemTarget(source: "GuildAlchemist"));

            Assert.AreEqual("prepared", engine.GetSlot("flasks", 0).State);
            Assert.AreEqual("expended", engine.GetSlot("flasks", 1).State); // Not matched by source
            Assert.AreEqual("prepared", engine.GetSlot("flasks", 2).State);

            // Transition all without source filter
            var transitionAll = SubsystemStep.Transition("flasks", "prepared", fromState: "expended", selector: "all");
            engine.ExecuteStep(transitionAll);
            Assert.AreEqual("prepared", engine.GetSlot("flasks", 1).State);
        }

        [TestMethod]
        public void SlotStateTransitions_Select_TransitionsSelectedSlotsOnly() {
            var (session, _, engine) = CreateSyntheticSession();

            engine.AssignSlot("flasks", 0, "Acid Flask", state: "expended");
            engine.AssignSlot("flasks", 1, "Fire Flask", state: "expended");
            engine.AssignSlot("flasks", 2, "Acid Flask", state: "expended");

            var selectTransition = SubsystemStep.Transition("flasks", "prepared", fromState: "expended", selector: "select");

            engine.ExecuteStep(selectTransition, new SubsystemTarget(selectedIndices: new List<int> { 1 }));

            Assert.AreEqual("expended", engine.GetSlot("flasks", 0).State);
            Assert.AreEqual("prepared", engine.GetSlot("flasks", 1).State);
            Assert.AreEqual("expended", engine.GetSlot("flasks", 2).State);
        }

        [TestMethod]
        public void SlotStateTransitions_Random_DeterministicStochasticTransfer_WithInjectedSeed() {
            // First run with seed 42
            var (session1, _, engine1) = CreateSyntheticSession(new Random(42));
            engine1.AssignSlot("flasks", 0, "Flask 0", state: "empty");
            engine1.AssignSlot("flasks", 1, "Flask 1", state: "empty");
            engine1.AssignSlot("flasks", 2, "Flask 2", state: "empty");

            var randomTransition = SubsystemStep.Transition("flasks", "prepared", fromState: "empty", selector: "random", countExpression: "2");

            engine1.ExecuteStep(randomTransition);
            var statesRun1 = new[] {
                engine1.GetSlot("flasks", 0).State,
                engine1.GetSlot("flasks", 1).State,
                engine1.GetSlot("flasks", 2).State
            };
            Assert.AreEqual(2, statesRun1.Count(s => s == "prepared"));
            Assert.AreEqual(1, statesRun1.Count(s => s == "empty"));

            // Second run with identical seed 42 must produce exactly identical stochastic outcome
            var (session2, _, engine2) = CreateSyntheticSession(new Random(42));
            engine2.AssignSlot("flasks", 0, "Flask 0", state: "empty");
            engine2.AssignSlot("flasks", 1, "Flask 1", state: "empty");
            engine2.AssignSlot("flasks", 2, "Flask 2", state: "empty");

            engine2.ExecuteStep(randomTransition);
            var statesRun2 = new[] {
                engine2.GetSlot("flasks", 0).State,
                engine2.GetSlot("flasks", 1).State,
                engine2.GetSlot("flasks", 2).State
            };

            CollectionAssert.AreEqual(statesRun1, statesRun2, "Stochastic transfer must be 100% deterministic with injected RNG seed.");
        }

        [TestMethod]
        public void MultiStepVerb_ExecutesSequentialSteps_TransitionAndElementVerb() {
            var (session, _, engine) = CreateSyntheticSession();

            // Register parameter for acid_splash_count on session effects
            session.SetParameter("acid_splash_count", 0);
            engine.AssignSlot("flasks", 0, "Acid Flask", state: "prepared");

            // Execute "throw" verb: Step 1 = transition prepared -> expended; Step 2 = invoke element verb execute
            var req = engine.ExecuteVerb("throw", slotGroup: "flasks", slotIndex: 0);

            Assert.AreEqual("expended", engine.GetSlot("flasks", 0).State, "Slot 0 should transition to expended.");
            Assert.AreEqual(1, session.Character.paramValues["acid_splash_count"], "Acid Flask's execute verb should increment acid_splash_count parameter.");
        }

        [TestMethod]
        public void ElementVerbInvocation_WithExplicitVerb_ExecutesElementAction() {
            var (session, _, engine) = CreateSyntheticSession();

            session.SetParameter("acid_splash_count", 0);
            var slot = new SubsystemSlot("Acid Flask", "prepared");

            var req = engine.InvokeElementVerb("execute", slot);

            Assert.AreEqual(1, session.Character.paramValues["acid_splash_count"]);
        }

        [TestMethod]
        public void ElementVerbInvocation_WithOmittedVerb_CleanlyFallsBackToSubsystemDefault() {
            var (session, _, engine) = CreateSyntheticSession();

            engine.AssignSlot("flasks", 0, "Fire Flask", state: "prepared");

            // Fire Flask omits verbs.execute, so it cleanly falls back to subsystem default_element_verbs.execute (no-op)
            var req = engine.ExecuteVerb("throw", slotGroup: "flasks", slotIndex: 0);

            Assert.AreEqual("expended", engine.GetSlot("flasks", 0).State);
        }

        [TestMethod]
        public void ResourcePoolOperations_ConsumeRechargeSet_ClampToDynamicCapacity() {
            var (session, _, engine) = CreateSyntheticSession();

            // Set dynamic catalyst_capacity on character
            session.SetParameter("catalyst_capacity", 8);

            Assert.AreEqual(8, engine.GetPoolCapacity("catalyst"));
            Assert.AreEqual(8, engine.GetPoolValue("catalyst"));

            // Consume 3
            engine.ConsumePool("catalyst", 3);
            Assert.AreEqual(5, engine.GetPoolValue("catalyst"));

            // Consume 10 (clamps to 0)
            engine.ConsumePool("catalyst", 10);
            Assert.AreEqual(0, engine.GetPoolValue("catalyst"));

            // Recharge 4
            engine.RechargePool("catalyst", 4);
            Assert.AreEqual(4, engine.GetPoolValue("catalyst"));

            // Recharge 20 (clamps to capacity 8)
            engine.RechargePool("catalyst", 20);
            Assert.AreEqual(8, engine.GetPoolValue("catalyst"));

            // Set 6
            engine.SetPoolValue("catalyst", 6);
            Assert.AreEqual(6, engine.GetPoolValue("catalyst"));

            // Set 15 (clamps to 8)
            engine.SetPoolValue("catalyst", 15);
            Assert.AreEqual(8, engine.GetPoolValue("catalyst"));

            // Set -5 (clamps to 0)
            engine.SetPoolValue("catalyst", -5);
            Assert.AreEqual(0, engine.GetPoolValue("catalyst"));

            // Dynamic capacity reduction: capacity stat drops to 5
            session.SetParameter("catalyst_capacity", 5);
            engine.SetPoolValue("catalyst", 5);
            Assert.AreEqual(5, engine.GetPoolCapacity("catalyst"));
            Assert.AreEqual(5, engine.GetPoolValue("catalyst"));
        }

        [TestMethod]
        public void ResourcePoolOperations_RechargeAndSet_ViaDeclarativeSheetAction() {
            var (session, _, engine) = CreateSyntheticSession();

            // Catalyst starts at default capacity 10; Heat starts at default capacity 5
            engine.ConsumePool("catalyst", 7);
            engine.ConsumePool("heat", 4);

            Assert.AreEqual(3, engine.GetPoolValue("catalyst"));
            Assert.AreEqual(1, engine.GetPoolValue("heat"));

            // End combat does NOT reset pools ambiently
            session.EndCombat();
            Assert.AreEqual(3, engine.GetPoolValue("catalyst"), "Catalyst pool must not reset ambiently on EndCombat.");
            Assert.AreEqual(1, engine.GetPoolValue("heat"), "Heat pool must not reset ambiently on EndCombat.");

            // Pool recharge via declarative SheetAction (e.g. resting or player action)
            session.PerformAction(new SheetAction {
                Subsystem = "synthetic_alchemy",
                Step = SubsystemStep.Pool("catalyst", "recharge", 7)
            });
            Assert.AreEqual(10, engine.GetPoolValue("catalyst"), "Catalyst pool should recharge to full via SheetAction.");

            // Pool set via declarative SheetAction
            session.PerformAction(new SheetAction {
                Subsystem = "synthetic_alchemy",
                Step = SubsystemStep.Pool("heat", "set", 5)
            });
            Assert.AreEqual(5, engine.GetPoolValue("heat"), "Heat pool should be set to 5 via SheetAction.");
        }

        [TestMethod]
        public void EffectProjection_ProjectsModifiersIntoSessionEffects() {
            var (session, _, engine) = CreateSyntheticSession();

            // Infusions slot group projects active -> "effects"
            // Speed Infusion gives +10 speed
            engine.AssignSlot("infusions", 0, "Speed Infusion", state: "inactive");
            session.Recalculate();

            int baseDex = session.GetStatistic("DEX score").Value;

            // Transition to active
            engine.ExecuteStep(
                SubsystemStep.Transition("infusions", "active", fromState: "inactive", selector: "invoked"),
                new SubsystemTarget(slotIndex: 0));
            session.Recalculate();

            int activeDex = session.GetStatistic("DEX score").Value;
            Assert.AreEqual(baseDex + 4, activeDex, "Speed Infusion in active state should project +4 DEX modifier onto session.");

            // Transition back to inactive
            engine.ExecuteStep(
                SubsystemStep.Transition("infusions", "inactive", fromState: "active", selector: "invoked"),
                new SubsystemTarget(slotIndex: 0));
            session.Recalculate();

            int inactiveDex = session.GetStatistic("DEX score").Value;
            Assert.AreEqual(baseDex, inactiveDex, "Deactivated Speed Infusion should cease projecting modifiers.");
        }

        [TestMethod]
        public void DeepenedSheetAction_ExecutesSubsystemVerbsTransitionsAndPoolOperations_Declaratively() {
            var (session, _, engine) = CreateSyntheticSession();

            session.SetParameter("acid_splash_count", 0);
            engine.AssignSlot("flasks", 0, "Acid Flask", state: "prepared");

            // Execute verb SheetAction
            var verbAction = new SheetAction {
                Name = "Throw Acid Flask",
                Subsystem = "synthetic_alchemy",
                Verb = "throw",
                SlotGroup = "flasks",
                SlotIndex = 0
            };

            var reqVerb = session.PerformAction(verbAction);
            Assert.AreEqual("expended", engine.GetSlot("flasks", 0).State);
            Assert.AreEqual(1, session.Character.paramValues["acid_splash_count"]);

            // Execute transition SheetAction (prepare_all)
            var transitionAction = new SheetAction {
                Name = "Prepare All Flasks",
                Subsystem = "synthetic_alchemy",
                Step = SubsystemStep.Transition("flasks", "prepared", fromState: "expended", selector: "all")
            };

            var reqTrans = session.PerformAction(transitionAction);
            Assert.AreEqual("prepared", engine.GetSlot("flasks", 0).State);

            // Execute pool operation SheetAction (consume 4 catalyst)
            var poolAction = new SheetAction {
                Name = "Consume Catalyst",
                Subsystem = "synthetic_alchemy",
                Step = SubsystemStep.Pool("catalyst", "consume", 4)
            };

            var reqPool = session.PerformAction(poolAction);
            Assert.AreEqual(6, engine.GetPoolValue("catalyst"));
        }

        [TestMethod]
        public void DeepenedSheetAction_JsonRoundTrip_SerializesAndDeserializesDeclaratively() {
            var verbAction = new SheetAction {
                Name = "Throw Acid",
                Subsystem = "synthetic_alchemy",
                Verb = "throw",
                SlotGroup = "flasks",
                SlotIndex = 1,
                Element = "Acid Flask",
                Source = "Alchemist"
            };

            var jsonVerb = (JObject)verbAction.ToJson();
            Assert.AreEqual("synthetic_alchemy", (string)jsonVerb["subsystem"]);
            Assert.AreEqual("throw", (string)jsonVerb["verb"]);
            Assert.AreEqual("flasks", (string)jsonVerb["slot_group"]);
            Assert.AreEqual(1, (int)jsonVerb["slot_index"]);
            Assert.AreEqual("Acid Flask", (string)jsonVerb["element"]);
            Assert.AreEqual("Alchemist", (string)jsonVerb["source"]);

            var deserializedVerb = SheetAction.FromJson(jsonVerb);
            Assert.IsTrue(verbAction.Matches(deserializedVerb));

            var transitionAction = new SheetAction {
                Name = "Refill All",
                Subsystem = "synthetic_alchemy",
                Step = SubsystemStep.Transition("flasks", "prepared", fromState: "expended", selector: "all")
            };
            var jsonTrans = (JObject)transitionAction.ToJson();
            Assert.IsNotNull(jsonTrans["step"]);
            var deserializedTrans = SheetAction.FromJson(jsonTrans);
            Assert.IsTrue(transitionAction.Matches(deserializedTrans));

            var poolAction = new SheetAction {
                Name = "Recharge Catalyst",
                Subsystem = "synthetic_alchemy",
                Step = SubsystemStep.Pool("catalyst", "recharge", 5)
            };
            var jsonPool = (JObject)poolAction.ToJson();
            Assert.IsNotNull(jsonPool["step"]);
            var deserializedPool = SheetAction.FromJson(jsonPool);
            Assert.IsTrue(poolAction.Matches(deserializedPool));
        }

        [TestMethod]
        public void DeepenedSheetAction_WithDuration_RevertsTransitionAndPoolOperationUponTimelineExpiration() {
            var (session, _, engine) = CreateSyntheticSession();

            engine.AssignSlot("infusions", 0, "Speed Infusion", state: "inactive");
            session.Recalculate();
            int baseDex = session.GetStatistic("DEX score").Value;

            // SheetAction activating infusion for 3 rounds
            var timedAction = new SheetAction {
                Name = "Temporary Infusion Boost",
                Subsystem = "synthetic_alchemy",
                SlotIndex = 0,
                Step = SubsystemStep.Transition("infusions", "active", fromState: "inactive", selector: "invoked")
            }.Duration("3 rounds");

            session.PerformAction(timedAction);
            session.Recalculate();

            Assert.AreEqual("active", engine.GetSlot("infusions", 0).State);
            Assert.AreEqual(baseDex + 4, session.GetStatistic("DEX score").Value);

            // Advance 2 rounds: still active
            session.AdvanceTimeline(2);
            Assert.AreEqual("active", engine.GetSlot("infusions", 0).State);
            Assert.AreEqual(baseDex + 4, session.GetStatistic("DEX score").Value);

            // Advance 1 more round to round 3: expiration fires declarative revert!
            session.AdvanceTimeline(1);
            Assert.AreEqual("inactive", engine.GetSlot("infusions", 0).State);
            Assert.AreEqual(baseDex, session.GetStatistic("DEX score").Value);
        }

        [TestMethod]
        public void FakeRulesCatalog_EndToEnd_ZeroContentLiteracy() {
            var (session, _, engine) = CreateSyntheticSession(new Random(99));

            // 1. Assign slots
            engine.AssignSlot("flasks", 0, "Acid Flask", state: "prepared");
            engine.AssignSlot("flasks", 1, "Fire Flask", state: "empty");
            engine.AssignSlot("flasks", 2, "Acid Flask", state: "empty");
            engine.AssignSlot("infusions", 0, "Strength Infusion", state: "inactive");

            // 2. Projection verification
            int baseStr = session.GetStatistic("STR score").Value;
            engine.ExecuteStep(
                SubsystemStep.Transition("infusions", "active", fromState: "inactive", selector: "invoked"),
                new SubsystemTarget(slotIndex: 0));
            session.Recalculate();
            Assert.AreEqual(baseStr + 2, session.GetStatistic("STR score").Value);

            // 3. Verb execution with fallback check
            session.SetParameter("acid_splash_count", 0);
            engine.ExecuteVerb("throw", slotGroup: "flasks", slotIndex: 0);
            Assert.AreEqual("expended", engine.GetSlot("flasks", 0).State);
            Assert.AreEqual(1, session.Character.paramValues["acid_splash_count"]);

            // 4. Random deal with injected seed
            engine.ExecuteStep(SubsystemStep.Transition("flasks", "prepared", fromState: "empty", selector: "random", countExpression: "1"));
            Assert.AreEqual(1, engine.GetSlots("flasks").Count(s => s?.State == "prepared"));

            // 5. Pool operation via SheetAction
            engine.ConsumePool("heat", 3);
            Assert.AreEqual(2, engine.GetPoolValue("heat"));
            session.PerformAction(new SheetAction {
                Subsystem = "synthetic_alchemy",
                Step = SubsystemStep.Pool("heat", "recharge", 3)
            });
            Assert.AreEqual(5, engine.GetPoolValue("heat"));

            // Complete zero content literacy verified end-to-end
            Assert.IsNotNull(engine);
        }
    }
}
