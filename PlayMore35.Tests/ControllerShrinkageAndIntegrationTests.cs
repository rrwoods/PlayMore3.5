using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Rules;
using PlayMore35.Subsystems;
using static PlayMore35.Effects.Constants;

namespace PlayMore35.Tests {
    [TestClass]
    public class ControllerShrinkageAndIntegrationTests {

        private static SubsystemElement CreateTestVestige(string name, int level = 1, int dc = 15) {
            var ability = new Effect($"{name} Ability Effect")
                .AddModifier("Armor Class", new Modifier(2, "competence"));
            var influence = new Effect("");
            influence.Text.Add(new Text { Category = "Social", Description = $"{name} begrudges wealth." });
            var json = new JObject(
                new JProperty("name", name),
                new JProperty("level", level),
                new JProperty("binding DC", dc),
                new JProperty("effects", new JObject(
                    new JProperty("abilities", new JArray(ability.ToJson())),
                    new JProperty("influence", new JArray(influence.ToJson()))
                ))
            );
            return new SubsystemElement(json);
        }

        [TestMethod]
        public void CharacterSession_HasNoBespokeVestigeMembers() {
            var vestigeMethods = typeof(CharacterSession).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.Name.IndexOf("Vestige", StringComparison.OrdinalIgnoreCase) >= 0 || m.Name.IndexOf("PactState", StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(m => m.Name)
                .ToList();
            Assert.AreEqual(0, vestigeMethods.Count, $"Expected 0 bespoke vestige methods on CharacterSession, but found: {string.Join(", ", vestigeMethods)}");

            var vestigeFields = typeof(CharacterSession).GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(f => f.Name.IndexOf("Vestige", StringComparison.OrdinalIgnoreCase) >= 0 || f.Name.IndexOf("PactState", StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(f => f.Name)
                .ToList();
            Assert.AreEqual(0, vestigeFields.Count, $"Expected 0 bespoke vestige fields on CharacterSession, but found: {string.Join(", ", vestigeFields)}");

            var vestigeProperties = typeof(CharacterSession).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(p => p.Name.IndexOf("Vestige", StringComparison.OrdinalIgnoreCase) >= 0 || p.Name.IndexOf("PactState", StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(p => p.Name)
                .ToList();
            Assert.AreEqual(0, vestigeProperties.Count, $"Expected 0 bespoke vestige properties on CharacterSession, but found: {string.Join(", ", vestigeProperties)}");
        }

        // Source: Tome of Magic, p. 10
        [TestMethod]
        public void Subsystem_FullBindingLifecycle_WithTimelineAdvanceAndReload() {
            var vestige = CreateTestVestige("Aym", 1, 15);
            var catalog = new FakeRulesCatalog(vestiges: new[] { vestige }, inner: new GameRulesCatalog());

            var character = new Character();
            character.classLevels.Add("Binder");
            var session = new CharacterSession(character, catalog);
            ICharacterContext ctx = session;
            var engine = session.GetSubsystem("pact_magic");
            Assert.IsNotNull(engine);
            Assert.IsTrue(engine.IsActive);
            Assert.AreEqual(1, engine.GetSlotCapacity("vestiges"));

            // Bind vestige in slot 0 with Good Pact
            engine.AssignSlot("vestiges", 0, "Aym", "good_pact", "Binder");
            Assert.AreEqual("Aym", engine.GetSlotElement("vestiges", 0));
            Assert.AreEqual("good_pact", engine.GetSlotState("vestiges", 0));

            // Reload character and verify effects contributed
            session.Recalculate();
            Assert.IsTrue(ctx.Effects.Effects.Any(e => e.Name == "Aym Ability Effect"), "Aym Ability Effect should be active in EffectPool.");
            Assert.IsFalse(ctx.Effects.Effects.Any(e => e.Name == "Aym's influence"), "Good Pact should not contribute influence effect.");

            // Override pact state to Poor Pact and defy once
            engine.AssignSlot("vestiges", 0, "Aym", "poor_pact", "Binder");
            session.Recalculate();
            Assert.IsTrue(ctx.Effects.Effects.Any(e => e.Name == "Aym Ability Effect"), "Aym Ability Effect should still be active in EffectPool.");
            Assert.IsTrue(ctx.Effects.Effects.Any(e => e.Name == "Aym's influence"), "Poor Pact should contribute influence effect.");
            Assert.IsFalse(ctx.Effects.Effects.Any(e => e.Name == "Vestige defiance"), "SubsystemEngine should not hardcode Vestige defiance.");

            int baseSaves = session.GetStatistic(FORT_SAVES).Value;
            var influenceEffect = ctx.Effects.Effects.First(e => e.Name == "Aym's influence");
            session.PerformAction(influenceEffect.associatedAction.Single());
            Assert.AreEqual(1, session.Character.paramValues["Aym's influence defied"]);
            Assert.IsTrue(ctx.Effects.Effects.Any(e => e.Name == "Aym's influence"), "Defied influence remains active.");
            Assert.AreEqual(baseSaves - 1, session.GetStatistic(FORT_SAVES).Value);

            // Re-bind with Good Pact and schedule 24-hour expiration on timeline
            engine.AssignSlot("vestiges", 0, "Aym", "good_pact", "Binder");
            session.Recalculate();
            Assert.AreEqual(0, session.Character.paramValues["Aym's influence defied"], "Rebinding resets defiance parameter to 0.");
            Assert.IsTrue(ctx.Effects.Effects.Any(e => e.Name == "Aym Ability Effect"));

            session.Timeline.Schedule(new SheetAction {
                Subsystem = "pact_magic",
                SlotGroup = "vestiges",
                SlotIndex = 0,
                Verb = "unbind"
            }, "24 hours");

            // Advance timeline by 24 hours (86,400 seconds)
            var result = session.AdvanceTimeline(new LengthOfTime("24 hours"));
            Assert.AreEqual(TimelineRefreshRequirement.FullReload, result.RefreshRequirement, "24-hour expiration should require FullReload.");

            // Execute the FullReload requirement
            session.Recalculate();

            // Verify slot is cleared and effects removed
            Assert.IsNull(engine.GetSlotElement("vestiges", 0), "Slot 0 should be unbound after 24 hours.");
            Assert.AreEqual("empty", engine.GetSlotState("vestiges", 0));
            Assert.IsFalse(ctx.Effects.Effects.Any(e => e.Name == "Aym Ability Effect"), "Aym Ability Effect should be removed after 24 hours.");
        }

        [TestMethod]
        public void DeclarativePactMagic_UnbindVestige_ResetsDefianceAndSlots() {
            var vestige = CreateTestVestige("Aym", 1, 15);
            var catalog = new FakeRulesCatalog(vestiges: new[] { vestige }, inner: new GameRulesCatalog());

            var character = new Character();
            var session = new CharacterSession(character, catalog);
            ICharacterContext ctx = session;
            var engine = session.GetSubsystem("pact_magic");
            Assert.IsNotNull(engine);

            ctx.Effects.AddStat(new Statistic("number of vestiges"));
            ctx.Effects.AddEffect(new Effect("Vestige Slot").AddModifier("number of vestiges", new Modifier(1, ModifierTypes.UNTYPED)));

            engine.AssignSlot("vestiges", 0, "Aym", "good_pact", "Binder");
            Assert.AreEqual("Aym", engine.GetSlotElement("vestiges", 0));

            // Unbind via declarative SheetAction
            session.PerformAction(new SheetAction {
                Subsystem = "pact_magic",
                SlotGroup = "vestiges",
                SlotIndex = 0,
                Verb = "unbind"
            });
            session.Recalculate();
            Assert.IsNull(engine.GetSlotElement("vestiges", 0));
            Assert.AreEqual("empty", engine.GetSlotState("vestiges", 0));
        }
    }
}
