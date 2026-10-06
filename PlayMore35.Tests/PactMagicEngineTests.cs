using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Items;
using PlayMore35.Subsystems;
using PlayMore35.Subsystems.PactMagic;
using static PlayMore35.Effects.Constants;

namespace PlayMore35.Tests {
    [TestClass]
    public class PactMagicEngineTests {
        private class TestContext : ICharacterContext {
            public Character Character { get; set; } = new Character();
            public EffectPool Effects { get; set; } = new EffectPool();
            public Equipped EquippedItems { get; set; } = new Equipped();
            public Timeline Timeline => Character?.Timeline;
            public SubsystemRegistry Subsystems { get; } = new SubsystemRegistry();

            public void ClearSlot(string subsystem, int slotIndex) {
                var slotted = Subsystems.Get(subsystem) as ISlottedSubsystem;
                slotted?.ClearSlot(slotIndex);
            }

            public List<AttackRoutine> GetAllRoutines() => new List<AttackRoutine>();
            public List<AttackRoutine> GetVisibleRoutines() => new List<AttackRoutine>();
        }

        private class TestContentProvider : ISubsystemContentProvider {
            private readonly Dictionary<string, Vestige> vestiges;

            public TestContentProvider(params Vestige[] list) {
                vestiges = list.ToDictionary(v => v.Name, v => v);
            }

            public IEnumerable<Vestige> GetVestiges() => vestiges.Values;

            public Vestige GetVestige(string name) {
                return vestiges.TryGetValue(name, out var v) ? v : null;
            }
        }

        private static Vestige CreateTestVestige(string name, int level = 1, int dc = 15) {
            var ability = new Effect($"{name} Granted Armor")
                .AddModifier("Armor Class", new Modifier(2, "competence"));
            var influence = new Effect($"{name} Influence");
            var json = new JObject(
                new JProperty("name", name),
                new JProperty("level", level),
                new JProperty("binding DC", dc),
                new JProperty("influence", influence.ToJson()),
                new JProperty("abilities", new JArray(ability.ToJson()))
            );
            return new Vestige(json);
        }

        private static void SetStat(EffectPool pool, string statName, int value) {
            if (!pool.stats.ContainsKey(statName)) {
                pool.AddStat(new Statistic(statName));
            }
            pool.AddEffect(new Effect($"Base {statName}").AddModifier(statName, new Modifier(value, UNTYPED)));
        }

        [TestMethod]
        public void PactMagicEngine_IsActive_WhenNumberOfVestigesExceedsZero() {
            var vestige = CreateTestVestige("Aym");
            var contentProvider = new TestContentProvider(vestige);
            var engine = new PactMagicEngine(contentProvider);
            var context = new TestContext();

            engine.Initialize(context);

            // Initially no statistic -> inactive
            Assert.IsFalse(engine.IsActive);

            // Add number of vestiges = 0 -> still inactive
            SetStat(context.Effects, NUM_VESTIGES, 0);
            Assert.IsFalse(engine.IsActive);

            // Set number of vestiges = 1 -> active
            context.Effects = new EffectPool();
            SetStat(context.Effects, NUM_VESTIGES, 1);
            Assert.IsTrue(engine.IsActive);
        }

        [TestMethod]
        public void PactMagicEngine_BindingAndSlotClamping_UsesSubsystemSlotGroup() {
            var vestigeA = CreateTestVestige("Aym", 1, 15);
            var vestigeB = CreateTestVestige("Ronove", 1, 15);
            var contentProvider = new TestContentProvider(vestigeA, vestigeB);
            var engine = new PactMagicEngine(contentProvider);
            var context = new TestContext();

            SetStat(context.Effects, NUM_VESTIGES, 2);
            engine.Initialize(context);

            Assert.AreEqual(2, engine.SlotCount);

            // Bind slot 0 to Aym (good pact)
            engine.BindVestige(0, "Aym", PactState.GOOD_PACT);
            Assert.AreEqual("Aym", engine.GetSlot(0).VestigeName);
            Assert.AreEqual(PactState.GOOD_PACT, engine.GetSlot(0).State);
            Assert.AreEqual(vestigeA, engine.CurrentVestige(0));

            // Bind slot 1 to Ronove (poor pact)
            engine.BindVestige(1, "Ronove", PactState.POOR_PACT);
            Assert.AreEqual("Ronove", engine.GetSlot(1).VestigeName);
            Assert.AreEqual(PactState.POOR_PACT, engine.GetSlot(1).State);

            // Override pact state on slot 0
            engine.ChangePactState(0, PactState.DEFIED);
            Assert.AreEqual(PactState.DEFIED, engine.GetSlot(0).State);

            // Clear slot 0
            engine.ClearSlot(0);
            Assert.AreEqual("", engine.GetSlot(0).VestigeName);
            Assert.IsNull(engine.CurrentVestige(0));
        }

        [TestMethod]
        public void PactMagicEngine_BindingDc_AndCheckBonus_CalculatedCorrectly() {
            var vestige = CreateTestVestige("Aym", 1, 20);
            var contentProvider = new TestContentProvider(vestige);
            var engine = new PactMagicEngine(contentProvider);
            var context = new TestContext();

            SetStat(context.Effects, NUM_VESTIGES, 1);
            SetStat(context.Effects, BINDING_CHECK, 5);
            engine.Initialize(context);

            Assert.AreEqual(20, engine.BindingCheckDc("Aym"));
            Assert.AreEqual(5, engine.GetBindingCheckBonus().bonus);
        }

        [TestMethod]
        public void PactMagicEngine_ContributeEffects_AddsAbilitiesInfluenceAndDefianceDebuffs() {
            var vestige = CreateTestVestige("Aym", 1, 15);
            var contentProvider = new TestContentProvider(vestige);
            var engine = new PactMagicEngine(contentProvider);
            var context = new TestContext();

            SetStat(context.Effects, NUM_VESTIGES, 1);
            SetStat(context.Effects, "Armor Class", 10);
            SetStat(context.Effects, ALL_SAVES, 2);
            engine.Initialize(context);

            // 1. Good Pact: contributes granted abilities only
            engine.BindVestige(0, "Aym", PactState.GOOD_PACT);
            var pool = new EffectPool();
            SetStat(pool, NUM_VESTIGES, 1);
            SetStat(pool, "Armor Class", 10);
            SetStat(pool, ALL_SAVES, 2);
            engine.ContributeEffects(pool, context);

            // Ability grants +2 competence to Armor Class (10 + 2 = 12)
            Assert.AreEqual(12, pool.stats["Armor Class"].GetValue(pool, context.EquippedItems).value);
            // No influence effect
            Assert.IsFalse(pool.Effects.Any(e => e.Name.Contains("Influence")));
            // No defiance effect
            Assert.IsFalse(pool.Effects.Any(e => e.Name.Contains("Defied")));

            // 2. Poor Pact: contributes granted abilities + influence
            engine.ChangePactState(0, PactState.POOR_PACT);
            pool = new EffectPool();
            SetStat(pool, NUM_VESTIGES, 1);
            SetStat(pool, "Armor Class", 10);
            SetStat(pool, ALL_SAVES, 2);
            engine.ContributeEffects(pool, context);

            Assert.IsTrue(pool.Effects.Any(e => e.Name == "Aym Influence"));
            Assert.IsFalse(pool.Effects.Any(e => e.Name.Contains("Defied")));

            // 3. Defied: contributes granted abilities + defiance -1 untyped penalty to saves, attacks, checks, skills
            engine.ChangePactState(0, PactState.DEFIED);
            pool = new EffectPool();
            SetStat(pool, NUM_VESTIGES, 1);
            SetStat(pool, "Armor Class", 10);
            SetStat(pool, ALL_SAVES, 2);
            engine.ContributeEffects(pool, context);

            Assert.IsTrue(pool.Effects.Any(e => e.Name == "Defied Aym"));
            // All saves was 2, now 2 - 1 = 1
            Assert.AreEqual(1, pool.stats[ALL_SAVES].GetValue(pool, context.EquippedItems).value);
        }

        [TestMethod]
        public void Character_Subsystems_SerializesAndRoundTripsUnderSubsystemsJson() {
            var character = new Character();
            var pactState = new PactMagicState();
            pactState.Slots.Add(new PactSlot("Aym", PactState.GOOD_PACT));
            pactState.Slots.Add(new PactSlot("Ronove", PactState.DEFIED));
            character.Subsystems[PactMagicEngine.SubsystemId] = pactState;

            JToken json = character.ToJson();
            Assert.IsNotNull(json["subsystems"]);
            Assert.IsNotNull(json["subsystems"][PactMagicEngine.SubsystemId]);

            var restored = new Character(null, null, (JObject)json);
            Assert.IsTrue(restored.Subsystems.ContainsKey(PactMagicEngine.SubsystemId));
            var restoredPact = restored.Subsystems[PactMagicEngine.SubsystemId] as PactMagicState;
            Assert.IsNotNull(restoredPact);
            Assert.AreEqual(2, restoredPact.Slots.Count);
            Assert.AreEqual("Aym", restoredPact.Slots[0].VestigeName);
            Assert.AreEqual(PactState.GOOD_PACT, restoredPact.Slots[0].State);
            Assert.AreEqual("Ronove", restoredPact.Slots[1].VestigeName);
            Assert.AreEqual(PactState.DEFIED, restoredPact.Slots[1].State);
        }
    }
}
