using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlayMore35.CharacterChoices;
using PlayMore35.Global;
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

            Assert.IsTrue(session.LightLoad > 0);
            Assert.IsTrue(session.MediumLoad > session.LightLoad);
            Assert.IsTrue(session.HeavyLoad > session.MediumLoad);

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

            int initialHeavyLoad = session.HeavyLoad;
            var initialStr = session.GetStatistic("STR score");
            Assert.AreEqual(10, initialStr.Value);

            session.Mutate(c => {
                c.assignedAbilityScores["STR"] = 18;
            });

            var updatedStr = session.GetStatistic("STR score");
            Assert.AreEqual(18, updatedStr.Value);
            Assert.IsTrue(session.HeavyLoad > initialHeavyLoad, "Heavy load should increase with higher STR.");
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
            context.Effects.GenerateUses(context.EquippedItems, character);

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
    }
}

