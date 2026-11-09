using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Global;
using PlayMore35.Items;
using PlayMore35.Rules;
using PlayMore35.Effects;
using static PlayMore35.Effects.Constants;

namespace PlayMore35.Tests {
    [TestClass]
    public class ViewProjectionTests {
        private IRulesCatalog catalog;

        [TestInitialize]
        public void Setup() {
            catalog = new GameRulesCatalog();
        }

        [TestMethod]
        public void StatView_ToggleView_ParamView_PropertiesAreReadonlyAndPopulated() {
            var stat = new StatView("STR", 18, new List<string> { "Base: 18" });
            Assert.AreEqual("STR", stat.Name);
            Assert.AreEqual(18, stat.Value);
            Assert.AreEqual(1, stat.Explanation.Count);
            Assert.AreEqual("Base: 18", stat.Explanation[0]);

            var toggle = new ToggleView("Power Attack", "Atk Options", "Take penalty to hit", true, true);
            Assert.AreEqual("Power Attack", toggle.Name);
            Assert.AreEqual("Atk Options", toggle.Category);
            Assert.AreEqual("Take penalty to hit", toggle.Description);
            Assert.IsTrue(toggle.IsActive);
            Assert.IsTrue(toggle.IsVisible);
            Assert.IsFalse(toggle.HasResource);
            Assert.IsNull(typeof(ToggleView).GetProperty("Resource"), "ToggleView must not expose internal ActionResource model.");

            var resourceJson = new Newtonsoft.Json.Linq.JObject {
                ["action"] = "swift",
                ["recharge"] = "day",
                ["uses"] = 3,
                ["duration"] = "1 round"
            };
            var actionRes = new ActionResource(resourceJson);
            actionRes.DynamicUses = 3;
            actionRes.DynamicUsesExplanation = new List<string> { "Base: 3" };

            var resourceToggle = new ToggleView("Turn Undead", "Channeling", "Turn undead creatures", false, true, actionRes);
            Assert.IsTrue(resourceToggle.HasResource);
            Assert.IsFalse(resourceToggle.IsAtWill);
            Assert.AreEqual(3, resourceToggle.MaxUses);
            Assert.AreEqual("swift 3/day", resourceToggle.ResourceDescription);
            Assert.AreEqual("1 round", resourceToggle.EffectDuration);
            Assert.AreEqual(1, resourceToggle.UsesExplanation.Count);
            Assert.AreEqual("Base: 3", resourceToggle.UsesExplanation[0]);

            var param = new ParamView("Damage", "hp", "Damage taken", 12);
            Assert.AreEqual("Damage", param.Name);
            Assert.AreEqual("hp", param.Category);
            Assert.AreEqual("Damage taken", param.Description);
            Assert.AreEqual(12, param.Value);
        }

        [TestMethod]
        public void CharacterSession_GetStatistic_ReturnsStatViewWithExplanation() {
            var character = new Character();
            character.assignedAbilityScores["STR"] = 16;
            character.race = "human";
            var session = new CharacterSession(character, catalog);

            var strView = session.GetStatistic("STR score");
            Assert.IsNotNull(strView);
            Assert.AreEqual("STR score", strView.Name);
            Assert.AreEqual(16, strView.Value);
            Assert.IsTrue(strView.Explanation.Count > 0, "Explanation should explain base assigned score.");
        }

        [TestMethod]
        public void CharacterSession_GetRelevantStats_ReturnsRelevantProjections() {
            var character = new Character();
            character.assignedAbilityScores["DEX"] = 14;
            character.race = "human";
            var session = new CharacterSession(character, catalog);

            var relevantDex = session.GetRelevantStats("DEX modifier").ToList();
            Assert.IsNotNull(relevantDex);
            // Must contain checks with DEX modifier tag
            Assert.IsTrue(relevantDex.Any(s => s.Name.Contains("DEX")), "Expected relevant stats tagged with DEX modifier.");
        }

        [TestMethod]
        public void CharacterSession_GetControllableFlagsAndParameters_ReturnsProjections() {
            var character = new Character();
            var session = new CharacterSession(character, catalog);

            session.SetFlag("charging", true);
            session.SetParameter("Lethal damage", 4);

            var flags = session.GetControllableFlags().ToList();
            var chargingToggle = flags.FirstOrDefault(f => f.Name == "charging");
            Assert.IsNotNull(chargingToggle);
            Assert.IsTrue(chargingToggle.IsActive);

            var parameters = session.GetControllableParameters().ToList();
            var lethalParam = parameters.FirstOrDefault(p => p.Name == "Lethal damage");
            Assert.IsNotNull(lethalParam);
            Assert.AreEqual(4, lethalParam.Value);
        }

        [TestMethod]
        public void CharacterSession_DescriptorsAndRoutines_ReflectSessionState() {
            var character = new Character();
            character.race = "human";
            character.assignedAbilityScores["STR"] = 14;
            var session = new CharacterSession(character, catalog);

            Assert.AreEqual("humanoid", session.CreatureType);
            CollectionAssert.Contains(session.SubTypes.ToList(), "human");

            Assert.IsTrue(session.GetStatistic(LIGHT_LOAD).Value > 0);
            Assert.IsTrue(session.GetStatistic(MEDIUM_LOAD).Value > session.GetStatistic(LIGHT_LOAD).Value);
            Assert.IsTrue(session.GetStatistic(HEAVY_LOAD).Value > session.GetStatistic(MEDIUM_LOAD).Value);

            var allRoutines = session.GetAllRoutines();
            Assert.IsNotNull(allRoutines);

            var visibleRoutines = session.GetVisibleRoutines();
            Assert.IsNotNull(visibleRoutines);

            Assert.IsTrue(session.IsStatRelevant("STR score"));
            Assert.IsFalse(session.IsStatRelevant("NonexistentStatThatDoesNotExist"));

            var textEffects = session.GetTextEffects();
            Assert.IsNotNull(textEffects);
        }

        [TestMethod]
        public void CharacterSession_MutateStrength_UpdatesEncumbranceAndStatView() {
            var character = new Character();
            character.race = "human";
            character.assignedAbilityScores["STR"] = 10;
            var session = new CharacterSession(character, catalog);

            int initialHeavyLoad = session.GetStatistic(HEAVY_LOAD).Value;
            var initialStr = session.GetStatistic("STR score");
            Assert.AreEqual(10, initialStr.Value);

            session.Mutate(c => {
                c.assignedAbilityScores["STR"] = 18;
            });

            var updatedStr = session.GetStatistic("STR score");
            Assert.AreEqual(18, updatedStr.Value);
            Assert.IsTrue(session.GetStatistic(HEAVY_LOAD).Value > initialHeavyLoad, "Heavy load should increase with higher STR.");
        }

        [TestMethod]
        public void FormUtils_ShortName_AppliesCustomShortNamesAndFallbacks() {
            Assert.AreEqual("K:rel", FormUtils.ShortName("Knowledge (religion)"));
            Assert.AreEqual("K:dun", FormUtils.ShortName("Knowledge (dungeoneering)"));
            Assert.AreEqual("UMD", FormUtils.ShortName("Use Magic Device"));
            Assert.AreEqual("MS", FormUtils.ShortName("Move Silently"));
            Assert.AreEqual("Info", FormUtils.ShortName("Gather Information"));
            Assert.AreEqual("Open", FormUtils.ShortName("Open Lock"));
            Assert.AreEqual("Heal", FormUtils.ShortName("Heal"));
            Assert.AreEqual("Jump", FormUtils.ShortName("Jump"));
            Assert.AreEqual("Int", FormUtils.ShortName("Intimidate"));
            Assert.AreEqual("Sur", FormUtils.ShortName("Survival"));
            Assert.AreEqual("", FormUtils.ShortName(null));
        }

        [TestMethod]
        public void CharacterSession_EffectWithInlineParameterAndAssociatedActions_RegistersParameterAndProjectsToTextEffects() {
            var character = new Character();
            var session = new CharacterSession(character, catalog);
            var context = (ICharacterContext)session;

            var action = new SheetAction { Name = "Increment Stacks" }
                .AdjustParam("Custom Condition stacks", 1);
            var effect = new Effect("Custom Condition")
                .SetParameter("stacks")
                .AddAction(action)
                .AddText(SOCIAL, "Social effect description.")
                .AddText(ACTIONS, "Actions effect description.");

            context.Effects.AddEffect(effect);

            var paramView = session.GetControllableParameters().FirstOrDefault(p => p.Name == "Custom Condition stacks");
            Assert.IsNotNull(paramView, "EffectPool should register ControllableParameter for '<Effect.Name> <Effect.Parameter>'.");
            Assert.AreEqual("", paramView.Category, "Inline Effect parameter should have empty Category so it is not duplicated at top of category panels.");
            Assert.AreEqual(0, paramView.Value);

            var textEffects = session.GetTextEffects();
            var socialText = textEffects[SOCIAL].FirstOrDefault(t => t.Name == "Custom Condition");
            var actionsText = textEffects[ACTIONS].FirstOrDefault(t => t.Name == "Custom Condition");

            Assert.IsNotNull(socialText);
            Assert.AreEqual("Custom Condition stacks", socialText.ParameterName);
            Assert.AreEqual("stacks", socialText.ParameterLabel);
            Assert.AreEqual(1, socialText.associatedAction.Count);
            Assert.AreEqual("Increment Stacks", socialText.associatedAction[0].Name);

            Assert.IsNotNull(actionsText);
            Assert.AreEqual("Custom Condition stacks", actionsText.ParameterName);
            Assert.AreEqual("stacks", actionsText.ParameterLabel);
            Assert.AreEqual(1, actionsText.associatedAction.Count);
            Assert.AreEqual("Increment Stacks", actionsText.associatedAction[0].Name);

            // Execute associatedAction through Session.PerformAction
            session.PerformAction(socialText.associatedAction[0]);

            var updatedParam = session.GetControllableParameters().First(p => p.Name == "Custom Condition stacks");
            Assert.AreEqual(1, updatedParam.Value);
            Assert.AreEqual(1, session.Character.paramValues["Custom Condition stacks"]);
        }

        [TestMethod]
        public void EffectPool_Constructor_ThrowsArgumentNullExceptionWhenCharacterIsNull() {
            Assert.ThrowsException<ArgumentNullException>(() => new EffectPool(null));
        }

        [TestMethod]
        public void EffectPool_AddEffect_AutomaticallyRegistersInlineParameterAndResourceUses() {
            var character = new Character();
            character.paramValues["Test Ability stacks"] = 2;
            character.paramValues["Test Ability used"] = 1;
            var pool = new EffectPool(character);

            var resourceJson = new JObject {
                ["action"] = "swift",
                ["recharge"] = "day",
                ["uses"] = 3
            };
            var effect = new Effect("Test Ability")
                .SetParameter("stacks");
            effect.Text.Add(new Text {
                Category = ACTIONS,
                Description = "Daily ability.",
                Resource = new ActionResource(resourceJson)
            });

            pool.AddEffect(effect);

            Assert.IsTrue(pool.controllableParameters.ContainsKey("Test Ability stacks"));
            Assert.AreEqual(2, pool.controllableParameters["Test Ability stacks"].Value);

            Assert.IsTrue(pool.stats.ContainsKey("Test Ability uses"));
            Assert.AreEqual(3, pool.GetStat("Test Ability uses").GetValue(pool, new Equipped()).value);

            Assert.IsTrue(pool.controllableParameters.ContainsKey("Test Ability used"));
            Assert.AreEqual(1, pool.controllableParameters["Test Ability used"].Value);
            CollectionAssert.Contains(pool.resettableParameters["day"], pool.controllableParameters["Test Ability used"]);

            // Removing the effect removes orphaned inline ParameterName
            pool.RemoveEffect("Test Ability");
            Assert.IsFalse(pool.controllableParameters.ContainsKey("Test Ability stacks"));
        }

        [TestMethod]
        public void Equipped_ContributeEffects_PushesItemEffectsAndFlagsRespectingDonnedArmorOverride() {
            var character = new Character();
            var pool = new EffectPool(character);
            pool.AddStat(new Statistic(AC));

            var slotlessItem = new Item(
                Equipped.Slot.SLOTLESS,
                new Effect("Slotless Ward").AddModifier(AC, new Modifier(1, "deflection")),
                newFlags: new[] { new FlagDefinition("Slotless Flag", "Misc", "Slotless flag desc", AlwaysOn.Instance, null) });
            var neckItem = new Item(
                Equipped.Slot.NECK,
                new Effect("Amulet of Health").AddModifier(AC, new Modifier(2, "natural armor")),
                newFlags: new[] { new FlagDefinition("Neck Flag", "Misc", "Neck flag desc", AlwaysOn.Instance, null) });
            var bodyItem = new Item(
                Equipped.Slot.BODY,
                new Effect("Magic Robe").AddModifier(AC, new Modifier(4, "armor")),
                newFlags: new[] { new FlagDefinition("Robe Flag", "Misc", "Robe flag desc", AlwaysOn.Instance, null) });

            var equipped = new Equipped();
            equipped.SlotlessItems.Add(slotlessItem);
            equipped.SlottedItems[Equipped.Slot.NECK] = neckItem;
            equipped.SlottedItems[Equipped.Slot.BODY] = bodyItem;

            // Without DonnedArmor, Slot.BODY item effect and flag are contributed
            equipped.ContributeEffects(pool);
            Assert.AreEqual(7, pool.GetStat(AC).GetValue(pool, equipped).value);
            Assert.IsTrue(pool.controllableFlags.ContainsKey("Slotless Flag"));
            Assert.IsTrue(pool.controllableFlags.ContainsKey("Neck Flag"));
            Assert.IsTrue(pool.controllableFlags.ContainsKey("Robe Flag"));

            // With DonnedArmor, Slot.BODY item effect and flag are overridden by DonnedArmor
            var poolWithArmor = new EffectPool(character);
            poolWithArmor.AddStat(new Statistic(AC));
            var donnedArmor = new Armor(catalog.GetArmor("full plate"));
            donnedArmor.Properties.Add(new ArmorProperty("warding",
                globalEffect: new Effect("Armor Warding").AddModifier(AC, new Modifier(5, "insight"))
            ));
            equipped.DonnedArmor = donnedArmor;
            equipped.ContributeEffects(poolWithArmor);

            Assert.AreEqual(8, poolWithArmor.GetStat(AC).GetValue(poolWithArmor, equipped).value); // +1 deflection +2 natural +5 insight (Slot.BODY +4 excluded)
            Assert.IsTrue(poolWithArmor.controllableFlags.ContainsKey("Slotless Flag"));
            Assert.IsTrue(poolWithArmor.controllableFlags.ContainsKey("Neck Flag"));
            Assert.IsFalse(poolWithArmor.controllableFlags.ContainsKey("Robe Flag"));
        }
    }
}


