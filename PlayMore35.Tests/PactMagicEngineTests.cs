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
using PlayMore35.Rules;
using PlayMore35.Subsystems;
using static PlayMore35.Effects.Constants;

namespace PlayMore35.Tests {
    [TestClass]
    public class PactMagicEngineTests {

        private static SubsystemElement CreateTestVestige(string name, int level = 1, int dc = 15) {
            var ability = new Effect($"{name} Granted Ability")
                .AddModifier(STR_SCORE, new Modifier(2, "competence"));
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

        private static void SetStat(EffectPool pool, string statName, int value) {
            if (!pool.stats.ContainsKey(statName)) {
                pool.AddStat(new Statistic(statName));
            }
            pool.AddEffect(new Effect($"Base {statName}").AddModifier(statName, new Modifier(value, ModifierTypes.UNTYPED)));
        }

        [TestMethod]
        public void PactMagic_SubsystemDefinition_DeclaresInfluenceEffectTemplate_AndElementsDeclareOnlyText() {
            var catalog = new GameRulesCatalog();
            var pactDef = catalog.GetSubsystem("pact_magic");
            Assert.IsNotNull(pactDef);
            Assert.IsTrue(pactDef.EffectTemplates.ContainsKey("influence"));

            var template = pactDef.EffectTemplates["influence"];
            Assert.AreEqual("${element}'s influence", (string)template["name"]);
            Assert.AreEqual("defied", (string)template["parameter"]);

            var naberius = pactDef.Elements["Naberius"];
            var influences = naberius.GetEffects("influence");
            Assert.AreEqual(1, influences.Count);

            var influence = influences[0];
            Assert.AreEqual("", influence.Parameter);
            Assert.AreEqual(0, influence.associatedAction.Count);
            Assert.AreEqual(1, influence.Text.Count);
            Assert.IsFalse(string.IsNullOrEmpty(influence.Text[0].Description));

            var projected = pactDef.GetElementEffects(naberius, "influence").ToList();
            Assert.AreEqual(1, projected.Count);
            Assert.AreEqual("Naberius's influence", projected[0].Name);
            Assert.AreEqual("Naberius's influence defied", projected[0].ParameterName);
            Assert.AreEqual(1, projected[0].associatedAction.Count);
            Assert.AreEqual("Defy Naberius's influence", projected[0].associatedAction[0].Name);
        }

        [TestMethod]
        public void PactMagic_SlotGroup_ContainsExpectedStates() {
            var catalog = new GameRulesCatalog();
            var pactDef = catalog.GetSubsystem("pact_magic");
            Assert.IsNotNull(pactDef);
            Assert.IsTrue(pactDef.SlotGroups.ContainsKey("vestiges"));
            var group = pactDef.SlotGroups["vestiges"];
            CollectionAssert.AreEquivalent(new[] { "empty", "good_pact", "poor_pact" }, group.States);
            Assert.AreEqual("empty", group.DefaultState);
        }

        [TestMethod]
        public void PactMagic_IsActive_WhenNumberOfVestigesExceedsZero() {
            var vestige = CreateTestVestige("Aym");
            var contentProvider = new FakeRulesCatalog(vestiges: new[] { vestige }, inner: new GameRulesCatalog());
            var character = new Character();
            var session = new CharacterSession(character, contentProvider);
            var engine = session.GetSubsystem("pact_magic");

            // Initially no statistic -> inactive
            Assert.IsFalse(engine.IsActive);

            // Add Binder level -> number of vestiges = 1 -> active
            character.classLevels.Add("Binder");
            session.Recalculate();
            Assert.IsTrue(engine.IsActive);
        }

        [TestMethod]
        public void PactMagic_BindingAndSlotClamping_UsesSubsystemSlotGroup() {
            var vestigeA = CreateTestVestige("Aym", 1, 15);
            var vestigeB = CreateTestVestige("Ronove", 1, 15);
            var contentProvider = new FakeRulesCatalog(vestiges: new[] { vestigeA, vestigeB }, inner: new GameRulesCatalog());
            var character = new Character();
            for (int i = 0; i < 8; i++) character.classLevels.Add("Binder");
            var session = new CharacterSession(character, contentProvider);

            var engine = session.GetSubsystem("pact_magic");
            Assert.AreEqual(2, engine.GetSlotGroup("vestiges").Capacity);

            // Bind slot 0 to Aym (good pact)
            engine.AssignSlot("vestiges", 0, "Aym", "good_pact", "Binder");
            Assert.AreEqual("Aym", engine.GetSlot("vestiges", 0).Element);
            Assert.AreEqual("good_pact", engine.GetSlot("vestiges", 0).State);

            // Bind slot 1 to Ronove (poor pact)
            engine.AssignSlot("vestiges", 1, "Ronove", "poor_pact", "Binder");
            Assert.AreEqual("Ronove", engine.GetSlot("vestiges", 1).Element);
            Assert.AreEqual("poor_pact", engine.GetSlot("vestiges", 1).State);

            // Override pact state on slot 0
            engine.SetSlotState("vestiges", 0, "poor_pact");
            Assert.AreEqual("poor_pact", engine.GetSlot("vestiges", 0).State);

            // Unbind vestige slot 0 via declarative SheetAction
            session.PerformAction(new SheetAction {
                Subsystem = "pact_magic",
                SlotGroup = "vestiges",
                SlotIndex = 0,
                Verb = "unbind"
            });
            Assert.AreEqual("", engine.GetSlot("vestiges", 0).Element);
            Assert.AreEqual("empty", engine.GetSlot("vestiges", 0).State);
        }

        [TestMethod]
        public void PactMagic_BindingDc_AndCheckBonus_CalculatedCorrectly() {
            var vestige = CreateTestVestige("Aym", 1, 20);
            var contentProvider = new FakeRulesCatalog(vestiges: new[] { vestige }, inner: new GameRulesCatalog());
            var character = new Character();
            character.classLevels.Add("Binder");
            character.assignedAbilityScores["CHA"] = 18;
            var session = new CharacterSession(character, contentProvider);

            var engine = session.GetSubsystem("pact_magic");
            Assert.AreEqual(20, engine.GetElement("Aym").GetProperty<int>("binding DC"));
            Assert.AreEqual(5, session.GetStatistic("binding check").Value);
        }

        // Source: Tome of Magic, p. 10
        [TestMethod]
        public void PactMagic_ContributeEffects_AddsAbilitiesInfluenceAndDefianceDebuffs() {
            var vestige = CreateTestVestige("Aym", 1, 15);
            var contentProvider = new FakeRulesCatalog(vestiges: new[] { vestige }, inner: new GameRulesCatalog());
            var character = new Character();
            character.classLevels[0] = "Binder";
            character.assignedAbilityScores["STR"] = 10;
            character.assignedAbilityScores["CON"] = 10;
            var session = new CharacterSession(character, contentProvider);
            var ctx = (ICharacterContext)session;
            var engine = session.GetSubsystem("pact_magic");

            // 1. Good Pact: contributes granted abilities only
            engine.AssignSlot("vestiges", 0, "Aym", "good_pact", "Binder");
            session.Recalculate();

            // Ability grants +2 competence to STR score (10 base + 2 = 12)
            Assert.AreEqual(12, session.GetStatistic(STR_SCORE).Value);
            // No influence effect
            Assert.IsFalse(ctx.Effects.Effects.Any(e => e.Name.Contains("influence")));
            // No defiance effect
            Assert.IsFalse(ctx.Effects.Effects.Any(e => e.Name == "Vestige defiance"));

            // 2. Poor Pact (0 defiances): contributes granted abilities + influence (0 penalty)
            engine.SetSlotState("vestiges", 0, "poor_pact");
            session.Recalculate();

            Assert.IsTrue(ctx.Effects.Effects.Any(e => e.Name == "Aym's influence"));
            Assert.IsFalse(ctx.Effects.Effects.Any(e => e.Name == "Vestige defiance"));
            var savesAtZero = session.GetStatistic(FORT_SAVES);
            Assert.AreEqual(2, savesAtZero.Value);
            Assert.IsFalse(savesAtZero.Explanation.Any(s => s.Contains("influence")));

            // 3. Poor Pact (defied twice): retains influence and applies -2 untyped penalty to saves, checks, skills, and attacks
            session.SetParameter("Aym's influence defied", 2);
            session.Recalculate();

            Assert.IsTrue(ctx.Effects.Effects.Any(e => e.Name == "Aym's influence"));
            var savesDefied = session.GetStatistic(FORT_SAVES);
            Assert.AreEqual(0, savesDefied.Value);
            CollectionAssert.Contains(savesDefied.Explanation.ToList(), "–2 Aym's influence");
            Assert.AreEqual(-2, session.GetStatistic(CON_CHECKS).Value);
            Assert.AreEqual(-2, session.GetStatistic(CONCENTRATION).Value);
            Assert.IsFalse(savesDefied.Explanation.Any(s => s.Contains("Vestige defiance")));
        }

        // Source: Tome of Magic, p. 10
        [TestMethod]
        public void PactMagic_DefianceStacksAcrossSingleAndMultipleVestiges() {
            var character = new Character();
            for (int i = 0; i < 8; i++) {
                character.classLevels.Add("Binder");
            }
            character.equipped.PrimaryHandWeapon = new WeaponChoice { Name = "longsword", SizeCategory = 0 };
            var session = new CharacterSession(character, new PlayMore35.Rules.GameRulesCatalog());

            var pactEngine = session.GetSubsystem("pact_magic");
            Assert.AreEqual(2, pactEngine.GetSlotGroup("vestiges").Capacity);

            // Bind Naberius in slot 0 and Savnok in slot 1, both in poor_pact
            pactEngine.AssignSlot("vestiges", 0, "Naberius", "poor_pact", "Binder");
            pactEngine.AssignSlot("vestiges", 1, "Savnok", "poor_pact", "Binder");
            session.Recalculate();

            int baseFort = session.GetStatistic(FORT_SAVES).Value;
            int baseStrCheck = session.GetStatistic(STR_CHECKS).Value;
            int baseDiplomacy = session.GetStatistic(DIPLOMACY).Value;
            int baseAtk = session.GetAllRoutines().First().Attacks.First().AttackBonus.bonus;

            // With 0 defiances, no defiance entry appears in explanations
            Assert.IsFalse(session.GetStatistic(FORT_SAVES).Explanation.Any(e => e.Contains("influence")));
            Assert.IsFalse(session.GetAllRoutines().First().Attacks.First().AttackBonus.explanation.Any(e => e.Contains("influence")));

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

            // Attacks, saves, ability checks, and skills take independent and cumulative penalties (-2 Naberius, -1 Savnok = -3 total)
            var fortAfter = session.GetStatistic(FORT_SAVES);
            Assert.AreEqual(baseFort - 3, fortAfter.Value);
            CollectionAssert.Contains(fortAfter.Explanation.ToList(), "–2 Naberius's influence");
            CollectionAssert.Contains(fortAfter.Explanation.ToList(), "–1 Savnok's influence");
            Assert.IsFalse(fortAfter.Explanation.Any(e => e.Contains("Vestige defiance")));

            var strCheckAfter = session.GetStatistic(STR_CHECKS);
            Assert.AreEqual(baseStrCheck - 3, strCheckAfter.Value);
            CollectionAssert.Contains(strCheckAfter.Explanation.ToList(), "–2 Naberius's influence");
            CollectionAssert.Contains(strCheckAfter.Explanation.ToList(), "–1 Savnok's influence");

            var diplomacyAfter = session.GetStatistic(DIPLOMACY);
            Assert.AreEqual(baseDiplomacy - 3, diplomacyAfter.Value);
            CollectionAssert.Contains(diplomacyAfter.Explanation.ToList(), "–2 Naberius's influence");
            CollectionAssert.Contains(diplomacyAfter.Explanation.ToList(), "–1 Savnok's influence");

            var atkAfter = session.GetAllRoutines().First().Attacks.First();
            Assert.AreEqual(baseAtk - 3, atkAfter.AttackBonus.bonus);
            CollectionAssert.Contains(atkAfter.AttackBonus.explanation, "–2 Naberius's influence");
            CollectionAssert.Contains(atkAfter.AttackBonus.explanation, "–1 Savnok's influence");
            Assert.IsFalse(atkAfter.AttackBonus.explanation.Any(e => e.Contains("Vestige defiance")));
        }

        // Source: Tome of Magic, p. 10
        [TestMethod]
        public void PactMagic_DefianceResetsOnUnbindRebindGoodPactAndTimelineExpiration() {
            var character = new Character();
            for (int i = 0; i < 8; i++) {
                character.classLevels.Add("Binder");
            }
            var session = new CharacterSession(character, new PlayMore35.Rules.GameRulesCatalog());
            var pactEngine = session.GetSubsystem("pact_magic");

            int baseFort = session.GetStatistic(FORT_SAVES).Value;

            // 1. Changing state to good_pact resets defiance count to 0
            pactEngine.AssignSlot("vestiges", 0, "Naberius", "poor_pact", "Binder");
            session.Recalculate();
            session.SetParameter("Naberius's influence defied", 2);
            Assert.AreEqual(baseFort - 2, session.GetStatistic(FORT_SAVES).Value);

            pactEngine.SetSlotState("vestiges", 0, "good_pact");
            session.Recalculate();
            Assert.AreEqual(0, session.Character.paramValues["Naberius's influence defied"]);
            Assert.AreEqual(baseFort, session.GetStatistic(FORT_SAVES).Value);

            // Even if switched back to poor_pact, defiance count starts at 0
            pactEngine.SetSlotState("vestiges", 0, "poor_pact");
            session.Recalculate();
            Assert.AreEqual(0, session.GetControllableParameters().First(p => p.Name == "Naberius's influence defied").Value);
            Assert.AreEqual(baseFort, session.GetStatistic(FORT_SAVES).Value);

            // 2. Rebinding a slot resets defiance count to 0
            session.SetParameter("Naberius's influence defied", 3);
            Assert.AreEqual(baseFort - 3, session.GetStatistic(FORT_SAVES).Value);

            pactEngine.AssignSlot("vestiges", 0, "Savnok", "poor_pact", "Binder");
            session.Recalculate();
            Assert.AreEqual(0, session.Character.paramValues["Naberius's influence defied"]);
            Assert.AreEqual(0, session.GetControllableParameters().First(p => p.Name == "Savnok's influence defied").Value);
            Assert.AreEqual(baseFort, session.GetStatistic(FORT_SAVES).Value);

            // 3. Unbind resets defiance count to 0
            session.SetParameter("Savnok's influence defied", 2);
            Assert.AreEqual(baseFort - 2, session.GetStatistic(FORT_SAVES).Value);

            session.PerformAction(new SheetAction {
                Subsystem = "pact_magic",
                SlotGroup = "vestiges",
                SlotIndex = 0,
                Verb = "unbind"
            });
            session.Recalculate();
            Assert.AreEqual(0, session.Character.paramValues["Savnok's influence defied"]);
            Assert.AreEqual(baseFort, session.GetStatistic(FORT_SAVES).Value);

            // 4. 24-hour Timeline expiration unbinds and resets defiance count to 0
            pactEngine.AssignSlot("vestiges", 0, "Naberius", "poor_pact", "Binder");
            session.Recalculate();
            session.SetParameter("Naberius's influence defied", 2);
            Assert.AreEqual(baseFort - 2, session.GetStatistic(FORT_SAVES).Value);

            session.Timeline.Schedule(new SheetAction {
                Name = "Unbind vestige slot 0",
                Subsystem = "pact_magic",
                SlotGroup = "vestiges",
                SlotIndex = 0,
                Verb = "unbind"
            }, "24 hours");
            session.AdvanceTimeline(new LengthOfTime("24 hours"));

            Assert.IsTrue(string.IsNullOrEmpty(pactEngine.GetSlot("vestiges", 0)?.Element) || pactEngine.GetSlot("vestiges", 0)?.State == "empty");
            Assert.AreEqual(0, session.Character.paramValues["Naberius's influence defied"]);
            Assert.AreEqual(baseFort, session.GetStatistic(FORT_SAVES).Value);

            // 5. Setting slot state to empty via SetSlotState clears element and resets defiance count to 0
            pactEngine.AssignSlot("vestiges", 0, "Naberius", "poor_pact", "Binder");
            session.Recalculate();
            session.SetParameter("Naberius's influence defied", 2);
            Assert.AreEqual(baseFort - 2, session.GetStatistic(FORT_SAVES).Value);

            pactEngine.SetSlotState("vestiges", 0, "empty");
            session.Recalculate();
            Assert.AreEqual(0, session.Character.paramValues["Naberius's influence defied"]);
            Assert.AreEqual("", pactEngine.GetSlot("vestiges", 0).Element);
            Assert.AreEqual(baseFort, session.GetStatistic(FORT_SAVES).Value);
        }

        // Source: Tome of Magic, p. 10
        [TestMethod]
        public void PactMagic_PoorPactInfluence_RendersAndSynchronizesDefiedParameterInTextEffectPanels() {
            var character = new Character();
            character.classLevels.Add("Binder");
            var session = new CharacterSession(character, new PlayMore35.Rules.GameRulesCatalog());
            var pactEngine = session.GetSubsystem("pact_magic");

            pactEngine.AssignSlot("vestiges", 0, "Savnok", "poor_pact", "Binder");
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
            var pactState = new SubsystemState();
            pactState.Slots["vestiges"] = new List<SubsystemSlot> {
                new SubsystemSlot("Aym", "good_pact", "Binder"),
                new SubsystemSlot("Ronove", "poor_pact", "Binder")
            };
            character.Subsystems["pact_magic"] = pactState;

            JToken json = character.ToJson();
            Assert.IsNotNull(json["subsystems"]);
            Assert.IsNotNull(json["subsystems"]["pact_magic"]);

            var restored = new Character((JObject)json);
            Assert.IsTrue(restored.Subsystems.ContainsKey("pact_magic"));
            var restoredPact = restored.Subsystems["pact_magic"];
            Assert.IsNotNull(restoredPact);
            Assert.IsTrue(restoredPact.Slots.ContainsKey("vestiges"));
            Assert.AreEqual(2, restoredPact.Slots["vestiges"].Count);
            Assert.AreEqual("Aym", restoredPact.Slots["vestiges"][0].Element);
            Assert.AreEqual("good_pact", restoredPact.Slots["vestiges"][0].State);
            Assert.AreEqual("Ronove", restoredPact.Slots["vestiges"][1].Element);
            Assert.AreEqual("poor_pact", restoredPact.Slots["vestiges"][1].State);
        }

        [TestMethod]
        public void OdricJson_LoadsScheduledUnbindAction_Correctly() {
            var path = TestEnvironment.FindOdricJsonPath();
            var json = JObject.Parse(System.IO.File.ReadAllText(path));
            var character = new Character(json);

            Assert.IsNotNull(character.Timeline);
            var session = new CharacterSession(character, new PlayMore35.Rules.GameRulesCatalog());
            var engine = session.GetSubsystem("pact_magic");
            Assert.IsNotNull(engine);
            Assert.AreEqual("Savnok", engine.GetSlot("vestiges", 0)?.Element);

            // Advance timeline to scheduled unbind round (43214 - 28835 = 14379 rounds)
            var advanceResult = session.AdvanceTimeline(new LengthOfTime("14379 rounds"));
            Assert.AreEqual(TimelineRefreshRequirement.FullReload, advanceResult.RefreshRequirement);
            Assert.AreEqual(1, advanceResult.ExpiredActions.Count);
            var unbindAction = advanceResult.ExpiredActions[0];
            Assert.AreEqual("pact_magic", unbindAction.Subsystem);
            Assert.AreEqual("vestiges", unbindAction.SlotGroup);
            Assert.AreEqual(0, unbindAction.SlotIndex);
            Assert.AreEqual("unbind", unbindAction.Verb);
            Assert.IsTrue(string.IsNullOrEmpty(engine.GetSlot("vestiges", 0)?.Element) || engine.GetSlot("vestiges", 0)?.State == "empty");
        }

        [TestMethod]
        public void PactMagic_SlotLevelValidation_ReflectsVestigeLevelAndMaxVestigeLevel() {
            var vestige1 = CreateTestVestige("Aym", 1, 15);
            var vestige2 = CreateTestVestige("Savnok", 2, 15);
            var contentProvider = new FakeRulesCatalog(vestiges: new[] { vestige1, vestige2 }, inner: new GameRulesCatalog());
            var character = new Character();
            character.classLevels[0] = "Binder";
            character.assignedAbilityScores["STR"] = 10;
            character.assignedAbilityScores["CON"] = 10;
            var session = new CharacterSession(character, contentProvider);
            var engine = session.GetSubsystem("pact_magic");

            // Empty slot is valid
            Assert.IsTrue(engine.IsSlotLevelValid("vestiges", 0));

            // Slot 0: Level 1 vestige is valid
            engine.AssignSlot("vestiges", 0, "Aym", "good_pact", "Binder");
            Assert.IsTrue(engine.IsSlotLevelValid("vestiges", 0));

            // Slot 0: Level 2 vestige is bound without exception (Enable, Don't Enforce)
            engine.AssignSlot("vestiges", 0, "Savnok", "good_pact", "Binder");
            Assert.AreEqual("Savnok", engine.GetSlot("vestiges", 0).Element);
            Assert.IsFalse(engine.IsSlotLevelValid("vestiges", 0));

            // Dynamically increase max vestige level to 2 (Binder level 3)
            character.classLevels.Add("Binder");
            character.classLevels.Add("Binder");
            session.Recalculate();
            Assert.IsTrue(engine.IsSlotLevelValid("vestiges", 0));

            // Unbind resets to valid
            session.PerformAction(new SheetAction {
                Subsystem = "pact_magic",
                SlotGroup = "vestiges",
                SlotIndex = 0,
                Verb = "unbind"
            });
            Assert.IsTrue(engine.IsSlotLevelValid("vestiges", 0));
        }

        [TestMethod]
        public void SubsystemElement_ImplementsIPropertyProvider_AndExposesGenericProperties() {
            var catalog = new GameRulesCatalog();
            var engine = new SubsystemEngine(catalog.GetSubsystem("pact_magic"), new FakeCharacterContext(catalog: catalog));
            var aym = engine.GetElement("Aym");
            Assert.IsNotNull(aym);

            var provider = aym as IPropertyProvider;
            Assert.IsNotNull(provider);
            Assert.AreEqual("Aym", provider.Properties["name"]);
            Assert.AreEqual(15, Convert.ToInt32(provider.Properties["binding DC"]));
            Assert.AreEqual(15, aym.GetProperty<int>("binding DC"));

            // Generic custom property support and GetProperty/SetProperty
            var customElem = new SubsystemElement(new JObject(
                new JProperty("name", "Custom Element"),
                new JProperty("level", 3),
                new JProperty("customKey", "customValue"),
                new JProperty("customNumber", 42),
                new JProperty("tags", new JArray("fire", "breath"))
            ));
            var customProvider = customElem as IPropertyProvider;
            Assert.IsNotNull(customProvider);
            Assert.AreEqual("customValue", customProvider.Properties["customKey"]);
            Assert.AreEqual("customValue", customElem.GetProperty<string>("customKey"));
            Assert.AreEqual(42, customElem.GetProperty<int>("customNumber"));
            Assert.AreEqual(99, customElem.GetProperty<int>("missingKey", 99));

            customElem.SetProperty("customKey", "updatedValue");
            Assert.AreEqual("updatedValue", customElem.GetProperty<string>("customKey"));

            var tags = customElem.GetProperty<System.Collections.IList>("tags");
            Assert.IsNotNull(tags);
            Assert.AreEqual(2, tags.Count);
            Assert.AreEqual("fire", tags[0]);
        }

        [TestMethod]
        public void PactMagic_DeclarativeBinding_GoodPactAndPoorPact_ViaSheetActions() {
            var vestige = CreateTestVestige("Aym", 1, 15);
            var contentProvider = new FakeRulesCatalog(vestiges: new[] { vestige }, inner: new GameRulesCatalog());
            var character = new Character();
            character.classLevels[0] = "Binder";
            character.assignedAbilityScores["STR"] = 10;
            character.assignedAbilityScores["CON"] = 10;
            var session = new CharacterSession(character, contentProvider);
            var ctx = (ICharacterContext)session;
            var engine = session.GetSubsystem("pact_magic");

            // 1. Bind good pact via declarative SheetAction
            session.PerformAction(new SheetAction {
                Name = "Bind Aym slot 0",
                Subsystem = "pact_magic",
                SlotGroup = "vestiges",
                SlotIndex = 0,
                Element = "Aym",
                Verb = "bind_good_pact",
                Source = "Binder"
            });
            session.Recalculate();

            var slot = engine.GetSlot("vestiges", 0);
            Assert.IsNotNull(slot);
            Assert.AreEqual("Aym", slot.Element);
            Assert.AreEqual("good_pact", slot.State);
            Assert.IsTrue(ctx.Effects.Effects.Any(e => e.Name == "Aym Granted Ability"));
            Assert.IsFalse(ctx.Effects.Effects.Any(e => e.Name == "Aym's influence"));

            // 2. Bind poor pact via declarative SheetAction
            session.PerformAction(new SheetAction {
                Name = "Bind Aym slot 0",
                Subsystem = "pact_magic",
                SlotGroup = "vestiges",
                SlotIndex = 0,
                Element = "Aym",
                Verb = "bind_poor_pact",
                Source = "Binder"
            });
            session.Recalculate();

            slot = engine.GetSlot("vestiges", 0);
            Assert.IsNotNull(slot);
            Assert.AreEqual("Aym", slot.Element);
            Assert.AreEqual("poor_pact", slot.State);
            Assert.IsTrue(ctx.Effects.Effects.Any(e => e.Name == "Aym Granted Ability"));
            Assert.IsTrue(ctx.Effects.Effects.Any(e => e.Name == "Aym's influence"));

            // 3. Unbind via declarative SheetAction
            session.PerformAction(new SheetAction {
                Subsystem = "pact_magic",
                SlotGroup = "vestiges",
                SlotIndex = 0,
                Verb = "unbind"
            });
            session.Recalculate();

            slot = engine.GetSlot("vestiges", 0);
            Assert.IsTrue(string.IsNullOrEmpty(slot.Element) || slot.State == "empty");
            Assert.IsFalse(ctx.Effects.Effects.Any(e => e.Name == "Aym Granted Ability"));
            Assert.IsFalse(ctx.Effects.Effects.Any(e => e.Name == "Aym's influence"));
        }

        [TestMethod]
        public void SubsystemDefinition_RequiredElementProperties_EnforcesPresenceAndRoundTrips() {
            var catalog = new GameRulesCatalog();
            var pactDef = catalog.GetSubsystem("pact_magic");
            Assert.IsNotNull(pactDef);
            Assert.AreEqual(0, pactDef.RequiredElementProperties.Count);
            Assert.IsTrue(pactDef.ElementStatistics.ContainsKey("binding DC"));

            var validJson = new JObject(
                new JProperty("subsystem", new JObject(
                    new JProperty("id", "test_subsystem"),
                    new JProperty("required_element_properties", new JArray("binding DC"))
                )),
                new JProperty("elements", new JArray(
                    new JObject(
                        new JProperty("name", "Valid Element"),
                        new JProperty("level", 1),
                        new JProperty("binding DC", 15)
                    )
                ))
            );
            var validDef = new SubsystemDefinition(validJson);
            Assert.AreEqual(1, validDef.Elements.Count);
            Assert.AreEqual(15, validDef.Elements["Valid Element"].GetProperty<int>("binding DC"));

            var roundTripped = new SubsystemDefinition((JObject)validDef.ToJson());
            CollectionAssert.AreEqual(new[] { "binding DC" }, roundTripped.RequiredElementProperties);

            var invalidJson = new JObject(
                new JProperty("subsystem", new JObject(
                    new JProperty("id", "test_subsystem"),
                    new JProperty("required_element_properties", new JArray("binding DC"))
                )),
                new JProperty("elements", new JArray(
                    new JObject(
                        new JProperty("name", "Invalid Element"),
                        new JProperty("level", 1)
                    )
                ))
            );
            var ex = Assert.ThrowsException<InvalidOperationException>(() => new SubsystemDefinition(invalidJson));
            StringAssert.Contains(ex.Message, "Invalid Element");
            StringAssert.Contains(ex.Message, "binding DC");
        }

        [TestMethod]
        public void SubsystemDefinition_ElementStatistics_EnforcesPresenceAndRoundTrips() {
            var catalog = new GameRulesCatalog();
            var pactDef = catalog.GetSubsystem("pact_magic");
            Assert.IsNotNull(pactDef);
            Assert.IsTrue(pactDef.ElementStatistics.ContainsKey("binding DC"));
            CollectionAssert.AreEqual(new[] { "binding DCs" }, pactDef.ElementStatistics["binding DC"]);

            var validJson = new JObject(
                new JProperty("subsystem", new JObject(
                    new JProperty("id", "test_subsystem"),
                    new JProperty("element_statistics", new JObject(
                        new JProperty("binding DC", new JArray("binding DCs"))
                    ))
                )),
                new JProperty("elements", new JArray(
                    new JObject(
                        new JProperty("name", "Valid Element"),
                        new JProperty("level", 1),
                        new JProperty("binding DC", 15)
                    )
                ))
            );
            var validDef = new SubsystemDefinition(validJson);
            Assert.AreEqual(1, validDef.Elements.Count);
            Assert.AreEqual(15, validDef.Elements["Valid Element"].GetProperty<int>("binding DC"));

            var roundTripped = new SubsystemDefinition((JObject)validDef.ToJson());
            Assert.IsTrue(roundTripped.ElementStatistics.ContainsKey("binding DC"));
            CollectionAssert.AreEqual(new[] { "binding DCs" }, roundTripped.ElementStatistics["binding DC"]);

            var invalidMissingJson = new JObject(
                new JProperty("subsystem", new JObject(
                    new JProperty("id", "test_subsystem"),
                    new JProperty("element_statistics", new JObject(
                        new JProperty("binding DC", new JArray("binding DCs"))
                    ))
                )),
                new JProperty("elements", new JArray(
                    new JObject(
                        new JProperty("name", "Invalid Element"),
                        new JProperty("level", 1)
                    )
                ))
            );
            var ex1 = Assert.ThrowsException<InvalidOperationException>(() => new SubsystemDefinition(invalidMissingJson));
            StringAssert.Contains(ex1.Message, "Invalid Element");
            StringAssert.Contains(ex1.Message, "binding DC");
        }

        // Source: Tome of Magic, p. 19
        [TestMethod]
        public void PactMagic_ElementStatistics_ProjectedIntoEffectPool_AndModifiableByTagOrExactName() {
            var character = new Character();
            character.classLevels[0] = "Binder";
            character.assignedAbilityScores["STR"] = 10;
            character.assignedAbilityScores["CON"] = 10;
            var catalog = new GameRulesCatalog();
            var session = new CharacterSession(character, catalog);
            var ctx = (ICharacterContext)session;
            var pactEngine = session.GetSubsystem("pact_magic");
            Assert.IsNotNull(pactEngine);

            var aym = pactEngine.GetElement("Aym");
            var amon = pactEngine.GetElement("Amon");
            Assert.IsNotNull(aym);
            Assert.IsNotNull(amon);

            // Baseline check via session.GetStatistic and element.GetStatistic
            Assert.AreEqual("binding DC:Aym", aym.GetStatisticName("binding DC"));
            Assert.AreEqual("binding DC:Amon", amon.GetStatisticName("binding DC"));

            Assert.AreEqual(15, session.GetStatistic("binding DC:Aym").Value);
            Assert.AreEqual(20, session.GetStatistic("binding DC:Amon").Value);
            Assert.AreEqual(15, aym.GetStatistic("binding DC", session).Value);
            Assert.AreEqual(20, amon.GetStatistic("binding DC", session).Value);

            // Modifiers applied to "binding DCs" (tag) reduce all vestige DCs
            ctx.Effects.AddEffect(new Effect("Vestige Mastery").AddModifier("binding DCs", new Modifier(-2, ModifierTypes.UNTYPED)));
            Assert.AreEqual(13, session.GetStatistic("binding DC:Aym").Value);
            Assert.AreEqual(18, session.GetStatistic("binding DC:Amon").Value);
            Assert.AreEqual(13, aym.GetStatistic("binding DC", session).Value);
            Assert.AreEqual(18, amon.GetStatistic("binding DC", session).Value);

            // Modifiers applied to "binding DC:Amon" (exact name) reduce only Amon's DC
            ctx.Effects.AddEffect(new Effect("Favored Vestige (Amon)").AddModifier("binding DC:Amon", new Modifier(-4, ModifierTypes.UNTYPED)));
            Assert.AreEqual(14, session.GetStatistic("binding DC:Amon").Value);
            Assert.AreEqual(13, session.GetStatistic("binding DC:Aym").Value);
            Assert.AreEqual(14, amon.GetStatistic("binding DC", session).Value);
            Assert.AreEqual(13, aym.GetStatistic("binding DC", session).Value);

            // Fallback when context is null or omitted
            Assert.AreEqual(15, aym.GetStatistic("binding DC").Value);
            Assert.AreEqual(20, amon.GetStatistic("binding DC").Value);
            Assert.AreEqual(15, aym.GetStatistic("binding DC", (ICharacterContext)null).Value);
        }

        [TestMethod]
        public void SubsystemDefinition_LevelStatistic_EnforcesPresenceAndRoundTrips() {
            var catalog = new GameRulesCatalog();
            var pactDef = catalog.GetSubsystem("pact_magic");
            Assert.IsNotNull(pactDef);
            Assert.IsTrue(pactDef.ElementStatistics.ContainsKey("level"));
            CollectionAssert.AreEqual(new string[0], pactDef.ElementStatistics["level"]);

            var validJson = new JObject(
                new JProperty("subsystem", new JObject(
                    new JProperty("id", "test_subsystem"),
                    new JProperty("element_statistics", new JObject(
                        new JProperty("level", new JArray())
                    ))
                )),
                new JProperty("elements", new JArray(
                    new JObject(
                        new JProperty("name", "Valid Element"),
                        new JProperty("level", 2)
                    )
                ))
            );
            var validDef = new SubsystemDefinition(validJson);
            Assert.AreEqual(1, validDef.Elements.Count);
            Assert.AreEqual(2, validDef.Elements["Valid Element"].GetProperty<int>("level"));

            var roundTripped = new SubsystemDefinition((JObject)validDef.ToJson());
            Assert.IsTrue(roundTripped.ElementStatistics.ContainsKey("level"));
            CollectionAssert.AreEqual(new string[0], roundTripped.ElementStatistics["level"]);
            Assert.AreEqual(2, roundTripped.Elements["Valid Element"].GetProperty<int>("level"));

            var invalidMissingJson = new JObject(
                new JProperty("subsystem", new JObject(
                    new JProperty("id", "test_subsystem"),
                    new JProperty("element_statistics", new JObject(
                        new JProperty("level", new JArray())
                    ))
                )),
                new JProperty("elements", new JArray(
                    new JObject(
                        new JProperty("name", "Invalid Element")
                    )
                ))
            );
            var ex1 = Assert.ThrowsException<InvalidOperationException>(() => new SubsystemDefinition(invalidMissingJson));
            StringAssert.Contains(ex1.Message, "Invalid Element");
            StringAssert.Contains(ex1.Message, "level");

            var invalidNonNumericJson = new JObject(
                new JProperty("subsystem", new JObject(
                    new JProperty("id", "test_subsystem"),
                    new JProperty("element_statistics", new JObject(
                        new JProperty("level", new JArray())
                    ))
                )),
                new JProperty("elements", new JArray(
                    new JObject(
                        new JProperty("name", "Invalid Element"),
                        new JProperty("level", "not_a_number")
                    )
                ))
            );
            var ex2 = Assert.ThrowsException<InvalidOperationException>(() => new SubsystemDefinition(invalidNonNumericJson));
            StringAssert.Contains(ex2.Message, "Invalid Element");
            StringAssert.Contains(ex2.Message, "must be numeric");
        }

        // Source: Tome of Magic, p. 19
        [TestMethod]
        public void PactMagic_LevelStatistic_ProjectedIntoEffectPool_AndModifiable() {
            var character = new Character();
            character.classLevels[0] = "Binder";
            character.assignedAbilityScores["STR"] = 10;
            character.assignedAbilityScores["CON"] = 10;
            var catalog = new GameRulesCatalog();
            var session = new CharacterSession(character, catalog);
            var ctx = (ICharacterContext)session;
            var pactEngine = session.GetSubsystem("pact_magic");
            Assert.IsNotNull(pactEngine);

            var aym = pactEngine.GetElement("Aym");
            var savnok = pactEngine.GetElement("Savnok");
            Assert.IsNotNull(aym);
            Assert.IsNotNull(savnok);

            // Baseline check
            Assert.AreEqual("level:Aym", aym.GetStatisticName("level"));
            Assert.AreEqual("level:Savnok", savnok.GetStatisticName("level"));
            Assert.AreEqual(1, session.GetStatistic("level:Aym").Value);
            Assert.AreEqual(2, session.GetStatistic("level:Savnok").Value);
            Assert.AreEqual(1, aym.GetStatistic("level", session).Value);
            Assert.AreEqual(2, savnok.GetStatistic("level", session).Value);

            // Binder level 1: max vestige level is 1
            Assert.AreEqual(1, session.GetStatistic("maximum vestige level").Value);
            Assert.AreEqual(0, pactEngine.ValidateChoice(null, aym.Name, "Binder").Count);
            Assert.IsTrue(pactEngine.ValidateChoice(null, savnok.Name, "Binder").Any(w => w.Category == ValidationCategory.LevelGate));

            // Savnok in slot 0 is overleveled
            pactEngine.AssignSlot("vestiges", 0, "Savnok", "good_pact", "Binder");
            Assert.IsFalse(pactEngine.IsSlotLevelValid("vestiges", 0));

            // Modify Savnok's projected level by -1
            var reduceSavnok = new Effect("Savnok Affinity")
                .AddModifier("level:Savnok", new Modifier(-1, ModifierTypes.UNTYPED));
            ctx.Effects.AddEffect(reduceSavnok);

            // Projected level is now 1
            Assert.AreEqual(1, session.GetStatistic("level:Savnok").Value);
            Assert.AreEqual(1, savnok.GetStatistic("level", session).Value);

            // Savnok is now valid in slot 0 and passes level gate
            Assert.AreEqual(0, pactEngine.ValidateChoice(null, savnok.Name, "Binder").Count);
            Assert.IsTrue(pactEngine.IsSlotLevelValid("vestiges", 0));

            // Removing modifier restores Savnok to level 2
            ctx.Effects.RemoveEffect(reduceSavnok.Name);

            Assert.AreEqual(2, session.GetStatistic("level:Savnok").Value);
            Assert.AreEqual(2, savnok.GetStatistic("level", session).Value);
            Assert.IsFalse(pactEngine.IsSlotLevelValid("vestiges", 0));
        }
    }
}
