using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Items;
using PlayMore35.Subsystems;
using PlayMore35.Subsystems.PactMagic;
using static PlayMore35.Effects.Constants;
using static PlayMore35.Subsystems.PactMagic.PactMagicEngine;

namespace PlayMore35.Tests {
    [TestClass]
    public class PactMagicEngineTests {

        private class TestContentProvider : ISubsystemContentProvider {
            private readonly Dictionary<string, Vestige> vestiges;

            public TestContentProvider(params Vestige[] list) {
                vestiges = list.ToDictionary(v => v.Name, v => v);
            }

            public IEnumerable<Vestige> GetVestiges() => vestiges.Values;

            public Vestige GetVestige(string name) {
                return vestiges.TryGetValue(name, out var v) ? v : null;
            }

            public IEnumerable<PlayMore35.Subsystems.Maneuvers.Maneuver> GetManeuvers() => Enumerable.Empty<PlayMore35.Subsystems.Maneuvers.Maneuver>();

            public PlayMore35.Subsystems.Maneuvers.Maneuver GetManeuver(string name) => null;
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
        public void Vestige_Influence_ConfiguredWithDefiedParameterAndAssociatedAction() {
            var vestige = CreateTestVestige("Naberius", 1, 15);

            Assert.AreEqual("defied", vestige.Influence.Parameter);
            Assert.AreEqual(1, vestige.Influence.associatedAction.Count);
            var expectedAction = new SheetAction { Name = "Defy Naberius Influence" }
                .AdjustParam("Naberius Influence defied", 1);
            Assert.IsTrue(vestige.Influence.associatedAction[0].Matches(expectedAction));

            // Round-trip through ToJson/Vestige(JObject) does not duplicate associatedAction
            var roundTripped = new Vestige((JObject)vestige.ToJson());
            Assert.AreEqual("defied", roundTripped.Influence.Parameter);
            Assert.AreEqual(1, roundTripped.Influence.associatedAction.Count);
            Assert.IsTrue(roundTripped.Influence.associatedAction[0].Matches(expectedAction));
        }

        [TestMethod]
        public void PactState_ContainsOnlyGoodPactAndPoorPact() {
            var values = System.Enum.GetNames(typeof(PactState));
            CollectionAssert.AreEquivalent(new[] { "GOOD_PACT", "POOR_PACT" }, values);
        }

        [TestMethod]
        public void PactMagicEngine_IsActive_WhenNumberOfVestigesExceedsZero() {
            var vestige = CreateTestVestige("Aym");
            var contentProvider = new TestContentProvider(vestige);
            var engine = new PactMagicEngine(contentProvider);
            var context = new FakeCharacterContext();

            engine.Initialize(context);

            // Initially no statistic -> inactive
            Assert.IsFalse(engine.IsActive);

            // Add number of vestiges = 0 -> still inactive
            SetStat(context.Effects, NUM_VESTIGES, 0);
            Assert.IsFalse(engine.IsActive);

            // Set number of vestiges = 1 -> active
            context.Effects = new EffectPool(context.Character);
            SetStat(context.Effects, NUM_VESTIGES, 1);
            Assert.IsTrue(engine.IsActive);
        }

        [TestMethod]
        public void PactMagicEngine_BindingAndSlotClamping_UsesSubsystemSlotGroup() {
            var vestigeA = CreateTestVestige("Aym", 1, 15);
            var vestigeB = CreateTestVestige("Ronove", 1, 15);
            var contentProvider = new TestContentProvider(vestigeA, vestigeB);
            var engine = new PactMagicEngine(contentProvider);
            var context = new FakeCharacterContext();

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
            engine.ChangePactState(0, PactState.POOR_PACT);
            Assert.AreEqual(PactState.POOR_PACT, engine.GetSlot(0).State);

            // Unbind vestige slot 0
            engine.UnbindVestige(0);
            Assert.AreEqual("", engine.GetSlot(0).VestigeName);
            Assert.IsNull(engine.CurrentVestige(0));
        }

        [TestMethod]
        public void PactMagicEngine_BindingDc_AndCheckBonus_CalculatedCorrectly() {
            var vestige = CreateTestVestige("Aym", 1, 20);
            var contentProvider = new TestContentProvider(vestige);
            var engine = new PactMagicEngine(contentProvider);
            var context = new FakeCharacterContext();

            SetStat(context.Effects, NUM_VESTIGES, 1);
            SetStat(context.Effects, BINDING_CHECK, 5);
            engine.Initialize(context);

            Assert.AreEqual(20, engine.BindingCheckDc("Aym"));
            Assert.AreEqual(5, engine.GetBindingCheckBonus().bonus);
        }

        // Source: Tome of Magic, p. 10
        [TestMethod]
        public void PactMagicEngine_ContributeEffects_AddsAbilitiesInfluenceAndDefianceDebuffs() {
            var vestige = CreateTestVestige("Aym", 1, 15);
            var contentProvider = new TestContentProvider(vestige);
            var engine = new PactMagicEngine(contentProvider);
            var context = new FakeCharacterContext();

            SetStat(context.Effects, NUM_VESTIGES, 1);
            SetStat(context.Effects, "Armor Class", 10);
            SetStat(context.Effects, ALL_SAVES, 2);
            engine.Initialize(context);

            // 1. Good Pact: contributes granted abilities only
            engine.BindVestige(0, "Aym", PactState.GOOD_PACT);
            var pool = new EffectPool(context.Character);
            SetStat(pool, NUM_VESTIGES, 1);
            SetStat(pool, "Armor Class", 10);
            SetStat(pool, ALL_SAVES, 2);
            engine.ContributeEffects(pool, context);

            // Ability grants +2 competence to Armor Class (10 + 2 = 12)
            Assert.AreEqual(12, pool.stats["Armor Class"].GetValue(pool, context.EquippedItems).value);
            // No influence effect
            Assert.IsFalse(pool.Effects.Any(e => e.Name.Contains("Influence")));
            // No defiance effect
            Assert.IsFalse(pool.Effects.Any(e => e.Name == "Vestige defiance"));

            // 2. Poor Pact (0 defiances): contributes granted abilities + influence + Vestige defiance (0 penalty)
            engine.ChangePactState(0, PactState.POOR_PACT);
            pool = new EffectPool(context.Character);
            SetStat(pool, NUM_VESTIGES, 1);
            SetStat(pool, "Armor Class", 10);
            SetStat(pool, ALL_SAVES, 2);
            SetStat(pool, ABILITY_CHECKS, 3);
            SetStat(pool, ALL_SKILLS, 4);
            engine.ContributeEffects(pool, context);

            Assert.IsTrue(pool.Effects.Any(e => e.Name == "Aym Influence"));
            Assert.IsTrue(pool.Effects.Any(e => e.Name == "Vestige defiance"));
            var savesAtZero = pool.stats[ALL_SAVES].GetValue(pool, context.EquippedItems);
            Assert.AreEqual(2, savesAtZero.value);
            Assert.IsFalse(savesAtZero.explanation.Any(s => s.Contains("Vestige defiance")));

            // 3. Poor Pact (defied twice): retains influence and applies -2 untyped penalty to saves, checks, skills, and attacks
            pool.controllableParameters["Aym Influence defied"].Value = 2;
            Assert.IsTrue(pool.Effects.Any(e => e.Name == "Aym Influence"));
            var savesDefied = pool.stats[ALL_SAVES].GetValue(pool, context.EquippedItems);
            Assert.AreEqual(0, savesDefied.value);
            CollectionAssert.Contains(savesDefied.explanation, "–2 Vestige defiance");
            Assert.AreEqual(1, pool.stats[ABILITY_CHECKS].GetValue(pool, context.EquippedItems).value);
            Assert.AreEqual(2, pool.stats[ALL_SKILLS].GetValue(pool, context.EquippedItems).value);
        }

        // Source: Tome of Magic, p. 10
        [TestMethod]
        public void PactMagicEngine_DefianceStacksAcrossSingleAndMultipleVestiges() {
            var character = new Character();
            for (int i = 0; i < 8; i++) {
                character.classLevels.Add("Binder");
            }
            character.equipped.PrimaryHandWeapon = new WeaponChoice { Name = "longsword", SizeCategory = 0 };
            var session = new CharacterSession(character, new PlayMore35.Rules.GameRulesCatalog());

            var pactEngine = session.Subsystems.Get<PactMagicEngine>();
            Assert.AreEqual(2, pactEngine.SlotCount);

            // Bind Naberius in slot 0 and Savnok in slot 1, both in POOR_PACT
            pactEngine.BindVestige(0, "Naberius", PactState.POOR_PACT);
            pactEngine.BindVestige(1, "Savnok", PactState.POOR_PACT);
            session.Recalculate();

            int baseFort = session.GetStatistic(FORT_SAVES).Value;
            int baseStrCheck = session.GetStatistic(STR_CHECKS).Value;
            int baseDiplomacy = session.GetStatistic(DIPLOMACY).Value;
            int baseAtk = session.GetAllRoutines().First().Attacks.First().AttackBonus.bonus;

            // With 0 defiances, no "Vestige defiance" entry appears in explanations
            Assert.IsFalse(session.GetStatistic(FORT_SAVES).Explanation.Any(e => e.Contains("Vestige defiance")));
            Assert.IsFalse(session.GetAllRoutines().First().Attacks.First().AttackBonus.explanation.Any(e => e.Contains("Vestige defiance")));

            // Find the associatedActions projected onto text effects
            var textEffects = session.GetTextEffects().Values.SelectMany(t => t).ToList();
            var naberiusInfluenceText = textEffects.First(t => t.Name == "Naberius's influence");
            var savnokInfluenceText = textEffects.First(t => t.Name == "Savnok's influence");

            Assert.AreEqual("Naberius's influence defied", naberiusInfluenceText.ParameterName);
            Assert.AreEqual("defied", naberiusInfluenceText.ParameterLabel);
            Assert.AreEqual("Defy Naberius's influence", naberiusInfluenceText.associatedAction.Single().Name);

            Assert.AreEqual("Savnok's influence defied", savnokInfluenceText.ParameterName);
            Assert.AreEqual("defied", savnokInfluenceText.ParameterLabel);
            Assert.AreEqual("Defy Savnok's influence", savnokInfluenceText.associatedAction.Single().Name);

            // Defy Naberius twice and Savnok once (total 3 defiances)
            session.PerformAction(naberiusInfluenceText.associatedAction.Single());
            session.PerformAction(naberiusInfluenceText.associatedAction.Single());
            session.PerformAction(savnokInfluenceText.associatedAction.Single());

            // Both influences remain visible in text effects
            var updatedTexts = session.GetTextEffects().Values.SelectMany(t => t).ToList();
            Assert.IsTrue(updatedTexts.Any(t => t.Name == "Naberius's influence"));
            Assert.IsTrue(updatedTexts.Any(t => t.Name == "Savnok's influence"));

            // Parameters reflect 2 and 1
            var paramsByName = session.GetControllableParameters().ToDictionary(p => p.Name, p => p.Value);
            Assert.AreEqual(2, paramsByName["Naberius's influence defied"]);
            Assert.AreEqual(1, paramsByName["Savnok's influence defied"]);

            // Attacks, saves, ability checks, and skills all take a single aggregated -3 Vestige defiance penalty
            var fortAfter = session.GetStatistic(FORT_SAVES);
            Assert.AreEqual(baseFort - 3, fortAfter.Value);
            Assert.AreEqual(1, fortAfter.Explanation.Count(e => e.Contains("Vestige defiance")));
            CollectionAssert.Contains(fortAfter.Explanation.ToList(), "–3 Vestige defiance");

            var strCheckAfter = session.GetStatistic(STR_CHECKS);
            Assert.AreEqual(baseStrCheck - 3, strCheckAfter.Value);
            CollectionAssert.Contains(strCheckAfter.Explanation.ToList(), "–3 Vestige defiance");

            var diplomacyAfter = session.GetStatistic(DIPLOMACY);
            Assert.AreEqual(baseDiplomacy - 3, diplomacyAfter.Value);
            CollectionAssert.Contains(diplomacyAfter.Explanation.ToList(), "–3 Vestige defiance");

            var atkAfter = session.GetAllRoutines().First().Attacks.First();
            Assert.AreEqual(baseAtk - 3, atkAfter.AttackBonus.bonus);
            Assert.AreEqual(1, atkAfter.AttackBonus.explanation.Count(e => e.Contains("Vestige defiance")));
            CollectionAssert.Contains(atkAfter.AttackBonus.explanation, "–3 Vestige defiance");
        }

        // Source: Tome of Magic, p. 10
        [TestMethod]
        public void PactMagicEngine_DefianceResetsOnUnbindRebindGoodPactAndTimelineExpiration() {
            var character = new Character();
            for (int i = 0; i < 8; i++) {
                character.classLevels.Add("Binder");
            }
            var session = new CharacterSession(character, new PlayMore35.Rules.GameRulesCatalog());
            var pactEngine = session.Subsystems.Get<PactMagicEngine>();

            int baseFort = session.GetStatistic(FORT_SAVES).Value;

            // 1. ChangePactState to GOOD_PACT resets defiance count to 0
            pactEngine.BindVestige(0, "Naberius", PactState.POOR_PACT);
            session.Recalculate();
            session.SetParameter("Naberius's influence defied", 2);
            Assert.AreEqual(baseFort - 2, session.GetStatistic(FORT_SAVES).Value);

            pactEngine.ChangePactState(0, PactState.GOOD_PACT);
            session.Recalculate();
            Assert.AreEqual(0, session.Character.paramValues["Naberius's influence defied"]);
            Assert.AreEqual(baseFort, session.GetStatistic(FORT_SAVES).Value);

            // Even if switched back to POOR_PACT, defiance count starts at 0
            pactEngine.ChangePactState(0, PactState.POOR_PACT);
            session.Recalculate();
            Assert.AreEqual(0, session.GetControllableParameters().First(p => p.Name == "Naberius's influence defied").Value);
            Assert.AreEqual(baseFort, session.GetStatistic(FORT_SAVES).Value);

            // 2. BindVestige replacing or rebinding a slot resets defiance count to 0
            session.SetParameter("Naberius's influence defied", 3);
            Assert.AreEqual(baseFort - 3, session.GetStatistic(FORT_SAVES).Value);

            pactEngine.BindVestige(0, "Savnok", PactState.POOR_PACT);
            session.Recalculate();
            Assert.AreEqual(0, session.Character.paramValues["Naberius's influence defied"]);
            Assert.AreEqual(0, session.GetControllableParameters().First(p => p.Name == "Savnok's influence defied").Value);
            Assert.AreEqual(baseFort, session.GetStatistic(FORT_SAVES).Value);

            // 3. UnbindVestige resets defiance count to 0
            session.SetParameter("Savnok's influence defied", 2);
            Assert.AreEqual(baseFort - 2, session.GetStatistic(FORT_SAVES).Value);

            pactEngine.UnbindVestige(0);
            session.Recalculate();
            Assert.AreEqual(0, session.Character.paramValues["Savnok's influence defied"]);
            Assert.AreEqual(baseFort, session.GetStatistic(FORT_SAVES).Value);

            // 4. 24-hour Timeline expiration unbinds and resets defiance count to 0
            pactEngine.BindVestige(0, "Naberius", PactState.POOR_PACT);
            session.Recalculate();
            session.SetParameter("Naberius's influence defied", 2);
            Assert.AreEqual(baseFort - 2, session.GetStatistic(FORT_SAVES).Value);

            session.Timeline.Schedule(new UnbindVestigeAction(0), "24 hours");
            session.AdvanceTimeline(new LengthOfTime("24 hours"));

            Assert.IsNull(pactEngine.CurrentVestige(0));
            Assert.AreEqual(0, session.Character.paramValues["Naberius's influence defied"]);
            Assert.AreEqual(baseFort, session.GetStatistic(FORT_SAVES).Value);
        }

        // Source: Tome of Magic, p. 10
        [TestMethod]
        public void PactMagicEngine_PoorPactInfluence_RendersAndSynchronizesDefiedParameterInTextEffectPanels() {
            var character = new Character();
            character.classLevels.Add("Binder");
            var session = new CharacterSession(character, new PlayMore35.Rules.GameRulesCatalog());
            var pactEngine = session.Subsystems.Get<PactMagicEngine>();

            pactEngine.BindVestige(0, "Savnok", PactState.POOR_PACT);
            session.Recalculate();

            var socialPanel = new TextEffectPanel {
                Tag = SOCIAL,
                DisplayStyle = TextEffectPanel.DisplayMode.MULTI_LINE_WITHOUT_HEADER
            };
            var actionsPanel = new TextEffectPanel {
                Tag = ACTIONS,
                DisplayStyle = TextEffectPanel.DisplayMode.MULTI_LINE_WITH_HEADER
            };

            using (var form = new MainForm(session)) {
                socialPanel.Initialize(form);
                actionsPanel.Initialize(form);

                var textEffects = session.GetTextEffects();
                socialPanel.Update(textEffects);
                actionsPanel.Update(textEffects);

                var socialLabel = socialPanel.Controls.OfType<Label>().FirstOrDefault(l => l.Text == FormUtils.INDENT + "Savnok's influence");
                var socialField = socialPanel.Controls.OfType<LabeledField>().FirstOrDefault(f => f.lblName.Text == "defied");
                var actionsField = actionsPanel.Controls.OfType<LabeledField>().FirstOrDefault(f => f.lblName.Text == "defied");

                Assert.IsNotNull(socialLabel);
                Assert.IsNotNull(socialLabel.ContextMenuStrip);
                Assert.AreEqual("Defy Savnok's influence", socialLabel.ContextMenuStrip.Items[0].Text);
                Assert.IsNotNull(socialField);
                Assert.IsNotNull(actionsField);
                Assert.AreEqual(0, socialField.Value);
                Assert.AreEqual(0, actionsField.Value);

                socialLabel.ContextMenuStrip.Items[0].PerformClick();
                textEffects = session.GetTextEffects();
                socialPanel.Update(textEffects);
                actionsPanel.Update(textEffects);

                Assert.AreEqual(1, socialField.Value);
                Assert.AreEqual(1, actionsField.Value);
                Assert.IsTrue(socialLabel.Visible, "Influence text remains visible after being defied.");
            }
        }

        [TestMethod]
        public void Character_Subsystems_SerializesAndRoundTripsUnderSubsystemsJson() {
            var character = new Character();
            var pactState = new PactMagicState();
            pactState.Slots.Add(new PactSlot("Aym", PactState.GOOD_PACT));
            pactState.Slots.Add(new PactSlot("Ronove", PactState.POOR_PACT));
            character.Subsystems[PactMagicEngine.SubsystemId] = pactState;

            JToken json = character.ToJson();
            Assert.IsNotNull(json["subsystems"]);
            Assert.IsNotNull(json["subsystems"][PactMagicEngine.SubsystemId]);

            var restored = new Character((JObject)json);
            Assert.IsTrue(restored.Subsystems.ContainsKey(PactMagicEngine.SubsystemId));
            var restoredPact = restored.Subsystems[PactMagicEngine.SubsystemId] as PactMagicState;
            Assert.IsNotNull(restoredPact);
            Assert.AreEqual(2, restoredPact.Slots.Count);
            Assert.AreEqual("Aym", restoredPact.Slots[0].VestigeName);
            Assert.AreEqual(PactState.GOOD_PACT, restoredPact.Slots[0].State);
            Assert.AreEqual("Ronove", restoredPact.Slots[1].VestigeName);
            Assert.AreEqual(PactState.POOR_PACT, restoredPact.Slots[1].State);
        }

        [TestMethod]
        public void OdricJson_LoadsScheduledUnbindVestigeAction_Correctly() {
            var path = TestEnvironment.FindOdricJsonPath();
            var json = JObject.Parse(System.IO.File.ReadAllText(path));
            var character = new Character(json);

            Assert.IsNotNull(character.Timeline);
            var session = new CharacterSession(character, new PlayMore35.Rules.GameRulesCatalog());
            var engine = session.Subsystems.Get<PactMagicEngine>();
            Assert.IsNotNull(engine);
            Assert.AreEqual("Savnok", engine.CurrentVestige(0)?.Name);

            // Advance timeline to scheduled unbind round (43214 - 28835 = 14379 rounds)
            var advanceResult = session.AdvanceTimeline(new LengthOfTime("14379 rounds"));
            Assert.AreEqual(TimelineRefreshRequirement.FullReload, advanceResult.RefreshRequirement);
            Assert.AreEqual(1, advanceResult.ExpiredActions.Count);
            Assert.IsInstanceOfType(advanceResult.ExpiredActions[0], typeof(UnbindVestigeAction));
            var unbindAction = (UnbindVestigeAction)advanceResult.ExpiredActions[0];
            Assert.AreEqual(0, unbindAction.SlotIndex);
            Assert.IsNull(engine.CurrentVestige(0));
        }

        [TestMethod]
        public void PactMagicEngine_SlotLevelValidation_ReflectsVestigeLevelAndMaxVestigeLevel() {
            var vestige1 = CreateTestVestige("Aym", 1, 15);
            var vestige2 = CreateTestVestige("Savnok", 2, 15);
            var contentProvider = new TestContentProvider(vestige1, vestige2);
            var engine = new PactMagicEngine(contentProvider);
            var context = new FakeCharacterContext();

            SetStat(context.Effects, NUM_VESTIGES, 2);
            SetStat(context.Effects, MAX_VESTIGE_LEVEL, 1);
            engine.Initialize(context);

            // Empty slots are valid
            Assert.IsTrue(engine.IsSlotLevelValid(0));
            Assert.IsTrue(engine.IsSlotLevelValid(1));

            // Slot 0: Level 1 vestige is valid
            engine.BindVestige(0, "Aym");
            Assert.IsTrue(engine.IsSlotLevelValid(0));

            // Slot 1: Level 2 vestige is bound without exception (Enable, Don't Enforce)
            engine.BindVestige(1, "Savnok");
            Assert.AreEqual("Savnok", engine.GetSlot(1).VestigeName);
            Assert.IsFalse(engine.IsSlotLevelValid(1));

            // Dynamically increase max vestige level to 2
            SetStat(context.Effects, MAX_VESTIGE_LEVEL, 2);
            Assert.IsTrue(engine.IsSlotLevelValid(1));

            // Unbind resets to valid
            engine.UnbindVestige(1);
            Assert.IsTrue(engine.IsSlotLevelValid(1));
        }
    }
}
