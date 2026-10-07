using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Items;
using PlayMore35.Rules;

namespace PlayMore35.Tests {
    [TestClass]
    public class CharacterSessionTests {
        private IRulesCatalog catalog;

        [TestInitialize]
        public void Setup() {
            catalog = new GameRulesCatalog();
        }

        [TestMethod]
        public void CharacterSession_EncapsulatesInternalsBehindSeam() {
            var character = new Character();
            var session = new CharacterSession(character, catalog);

            var sessionInterfaceProps = typeof(ICharacterSession).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            Assert.IsFalse(sessionInterfaceProps.Any(p => p.Name == "Effects"), "ICharacterSession must not expose Effects property.");
            Assert.IsFalse(sessionInterfaceProps.Any(p => p.Name == "EquippedItems"), "ICharacterSession must not expose EquippedItems property.");

            // Verify session implements ICharacterContext
            Assert.IsTrue(session is ICharacterContext, "CharacterSession must implement ICharacterContext.");
            var context = (ICharacterContext)session;
            Assert.IsNotNull(context.Effects, "ICharacterContext.Effects must be available to engine collaborators.");
            Assert.IsNotNull(context.EquippedItems, "ICharacterContext.EquippedItems must be available to engine collaborators.");
        }

        [TestMethod]
        public void CharacterSession_Mutate_AppliesChangesAndRecomputesAtomically() {
            var character = new Character();
            var session = new CharacterSession(character, catalog);

            session.Mutate(c => {
                c.race = "human";
                c.classLevels.Clear();
                c.classLevels.Add("Fighter");
                c.assignedAbilityScores["STR"] = 16;
                c.assignedAbilityScores["DEX"] = 14;
                c.assignedAbilityScores["CON"] = 14;
                c.assignedAbilityScores["INT"] = 10;
                c.assignedAbilityScores["WIS"] = 10;
                c.assignedAbilityScores["CHA"] = 10;
            });

            Assert.AreEqual("human", session.Character.race);
            Assert.AreEqual(1, session.Character.classLevels.Count);
            Assert.AreEqual(16, session.Character.assignedAbilityScores["STR"]);

            var context = (ICharacterContext)session;
            var strVal = context.Effects.GetStat("STR score").GetValue(context.Effects, context.EquippedItems).value;
            Assert.AreEqual(16, strVal, "STR score should be evaluated dynamically from mutation.");
        }

        [TestMethod]
        public void CharacterSession_CombatTogglesAndParameters_ModifyRuntimeState() {
            var character = new Character();
            var session = new CharacterSession(character, catalog);

            session.SetFlag("charging", true);
            Assert.IsTrue(session.Character.flagValues.ContainsKey("charging") && session.Character.flagValues["charging"]);

            var context = (ICharacterContext)session;
            Assert.IsTrue(context.Effects.controllableFlags["charging"].On);

            session.SetParameter("Lethal damage", 7);
            Assert.AreEqual(7, session.Character.paramValues["Lethal damage"]);
            Assert.AreEqual(7, context.Effects.controllableParameters["Lethal damage"].Value);
        }

        [TestMethod]
        public void CharacterSession_EquipmentLifecycle_FunctionsAccurately() {
            var character = new Character();
            character.inventory["Pack"] = new InventoryLocation();
            var session = new CharacterSession(character, catalog);

            session.AddWeapon("Pack", "longsword", new List<string>());
            Assert.AreEqual(1, session.Character.inventory["Pack"].weapons.Count);

            var weaponChoice = session.Character.inventory["Pack"].weapons[0];
            session.EquipWeapon("Pack", true, weaponChoice);

            Assert.AreEqual(0, session.Character.inventory["Pack"].weapons.Count);
            Assert.AreEqual("longsword", session.Character.equipped.PrimaryHandWeapon.Name);

            var context = (ICharacterContext)session;
            Assert.IsNotNull(context.EquippedItems.PrimaryHandWeapon);
            Assert.AreEqual("longsword", context.EquippedItems.PrimaryHandWeapon.Name);

            session.UnequipWeapon("Pack", true);
            Assert.IsNull(session.Character.equipped.PrimaryHandWeapon);
            Assert.AreEqual(1, session.Character.inventory["Pack"].weapons.Count);
        }

        [TestMethod]
        public void CharacterSession_AdvanceTimeline_ExpiresActions() {
            var character = new Character();
            var session = new CharacterSession(character, catalog);

            var action = new SheetAction()
                .SetFlag("charging")
                .Duration("1 round");

            action.Perform((ICharacterContext)session);
            Assert.IsTrue(session.Character.flagValues["charging"]);

            var result = session.AdvanceTimeline(new LengthOfTime("1 round"));
            Assert.IsFalse(session.Character.flagValues["charging"]);
        }
    }
}
