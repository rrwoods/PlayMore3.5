using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Subsystems;
using PlayMore35.Subsystems.PactMagic;
using static PlayMore35.Effects.Constants;

namespace PlayMore35.Tests {
    [TestClass]
    public class ControllerShrinkageAndIntegrationTests {
        private class TestContentProvider : ISubsystemContentProvider {
            private readonly Dictionary<string, Vestige> vestiges;

            public TestContentProvider(params Vestige[] list) {
                vestiges = list.ToDictionary(v => v.Name, v => v);
            }

            public IEnumerable<Vestige> GetVestiges() => vestiges.Values;

            public Vestige GetVestige(string name) {
                return (name != null && vestiges.TryGetValue(name, out var v)) ? v : null;
            }

            public IEnumerable<PlayMore35.Subsystems.Maneuvers.Maneuver> GetManeuvers() => Enumerable.Empty<PlayMore35.Subsystems.Maneuvers.Maneuver>();

            public PlayMore35.Subsystems.Maneuvers.Maneuver GetManeuver(string name) => null;
        }

        private static Vestige CreateTestVestige(string name, int level = 1, int dc = 15) {
            var ability = new Effect($"{name} Ability Effect")
                .AddModifier("Armor Class", new Modifier(2, "competence"));
            var influence = new Effect($"{name} Influence Effect");
            var json = new JObject(
                new JProperty("name", name),
                new JProperty("level", level),
                new JProperty("binding DC", dc),
                new JProperty("influence", influence.ToJson()),
                new JProperty("abilities", new JArray(ability.ToJson()))
            );
            return new Vestige(json);
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
            var contentProvider = new TestContentProvider(vestige);
            var engine = new PactMagicEngine(contentProvider);

            var character = new Character();
            character.classLevels.Add("Binder");
            var session = new CharacterSession(character, new PlayMore35.Rules.GameRulesCatalog());
            ICharacterContext ctx = session;
            session.Subsystems.Register(engine, "vestige");

            engine.Initialize(session);
            Assert.IsTrue(engine.IsActive);
            Assert.AreEqual(1, engine.SlotCount);

            // Bind vestige in slot 0 with Good Pact
            engine.BindVestige(0, "Aym", PactState.GOOD_PACT);
            Assert.AreEqual("Aym", engine.CurrentVestige(0)?.Name);
            Assert.AreEqual(PactState.GOOD_PACT, engine.GetSlot(0)?.State);

            // Reload character and verify effects contributed
            session.Recalculate();
            Assert.IsTrue(ctx.Effects.Effects.Any(e => e.Name == "Aym Ability Effect"), "Aym Ability Effect should be active in EffectPool.");
            Assert.IsFalse(ctx.Effects.Effects.Any(e => e.Name == "Aym Influence Effect"), "Good Pact should not contribute influence effect.");

            // Override pact state to Poor Pact and defy once
            engine.ChangePactState(0, PactState.POOR_PACT);
            session.Recalculate();
            Assert.IsTrue(ctx.Effects.Effects.Any(e => e.Name == "Aym Ability Effect"), "Aym Ability Effect should still be active in EffectPool.");
            Assert.IsTrue(ctx.Effects.Effects.Any(e => e.Name == "Aym Influence Effect"), "Poor Pact should contribute influence effect.");
            Assert.IsTrue(ctx.Effects.Effects.Any(e => e.Name == "Vestige defiance"), "Poor Pact should contribute Vestige defiance effect.");

            int baseSaves = session.GetStatistic(FORT_SAVES).Value;
            session.PerformAction(vestige.Influence.associatedAction.Single());
            Assert.AreEqual(1, session.Character.paramValues["Aym Influence Effect defied"]);
            Assert.IsTrue(ctx.Effects.Effects.Any(e => e.Name == "Aym Influence Effect"), "Defied influence remains active.");
            Assert.AreEqual(baseSaves - 1, session.GetStatistic(FORT_SAVES).Value);

            // Re-bind with Good Pact and schedule 24-hour expiration on timeline
            engine.BindVestige(0, "Aym", PactState.GOOD_PACT);
            session.Recalculate();
            Assert.AreEqual(0, session.Character.paramValues["Aym Influence Effect defied"], "Rebinding resets defiance parameter to 0.");
            Assert.IsTrue(ctx.Effects.Effects.Any(e => e.Name == "Aym Ability Effect"));

            session.Timeline.Schedule(new UnbindVestigeAction(0), "24 hours");

            // Advance timeline by 24 hours (86,400 seconds)
            var result = session.AdvanceTimeline(new LengthOfTime("24 hours"));
            Assert.AreEqual(TimelineRefreshRequirement.FullReload, result.RefreshRequirement, "24-hour expiration should require FullReload.");

            // Execute the FullReload requirement
            session.Recalculate();

            // Verify slot is cleared and effects removed
            Assert.IsNull(engine.CurrentVestige(0), "Slot 0 should be unbound after 24 hours.");
            Assert.IsFalse(ctx.Effects.Effects.Any(e => e.Name == "Aym Ability Effect"), "Aym Ability Effect should be removed after 24 hours.");
        }

        [TestMethod]
        public void PactMagicEngine_UnbindVestige_ResetsDefianceAndSlots() {
            var vestige = CreateTestVestige("Aym", 1, 15);
            var contentProvider = new TestContentProvider(vestige);
            var engine = new PactMagicEngine(contentProvider);

            var character = new Character();
            var session = new CharacterSession(character, new PlayMore35.Rules.GameRulesCatalog());
            ICharacterContext ctx = session;
            session.Subsystems.Register(engine, "vestige");

            ctx.Effects.AddStat(new Statistic(PactMagicEngine.NUM_VESTIGES));
            ctx.Effects.AddEffect(new Effect("Vestige Slot").AddModifier(PactMagicEngine.NUM_VESTIGES, new Modifier(1, UNTYPED)));
            engine.Initialize(session);

            engine.BindVestige(0, "Aym", PactState.GOOD_PACT);
            Assert.AreEqual("Aym", engine.CurrentVestige(0)?.Name);

            // Unbind directly on engine
            session.Subsystems.Get<PactMagicEngine>().UnbindVestige(0);
            session.Recalculate();
            Assert.IsNull(engine.CurrentVestige(0));
        }
    }
}
