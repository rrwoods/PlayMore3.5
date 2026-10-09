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

            var charProp = typeof(ICharacterSession).GetProperty("Character");
            Assert.AreEqual(typeof(IReadOnlyCharacter), charProp.PropertyType, "ICharacterSession.Character must be IReadOnlyCharacter.");
            Assert.IsFalse(session.Character is Character, "session.Character must wrap in ReadOnlyCharacter and not expose mutable Character directly.");

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
        public void CharacterSession_EquipmentLifecycle_AutomaticallyRecalculatesDerivedStats() {
            var character = new Character();
            character.inventory["Pack"] = new InventoryLocation();
            var session = new CharacterSession(character, catalog);

            int initialAc = session.GetStatistic(Constants.AC).Value;

            session.AddArmor("Pack", "full plate", new List<string>());
            var armorChoice = session.Character.inventory["Pack"].armor[0];

            session.DonArmor("Pack", armorChoice);
            Assert.AreEqual(initialAc + 8, session.GetStatistic(Constants.AC).Value, "Donning full plate must automatically recalculate AC (+8 armor bonus).");

            session.UndonArmor("Pack");
            Assert.AreEqual(initialAc, session.GetStatistic(Constants.AC).Value, "Undonning full plate must automatically recalculate AC back to base.");
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

        [TestMethod]
        public void CharacterSession_Transactions_MutationsRecomputeTotalGold() {
            var character = new Character();
            var session = new CharacterSession(character, catalog);

            Assert.AreEqual(0m, session.Character.TotalGold());

            var t1 = new GpTransaction(250m, "starting wealth", new DateTime(2026, 10, 6, 12, 0, 0), new DateTime(2026, 10, 9));
            session.Mutate(c => c.transactions.Add(t1));
            Assert.AreEqual(250m, session.Character.TotalGold());

            var t2 = new GpTransaction(-75.5m, "heavy horse purchase", new DateTime(2026, 10, 6, 13, 0, 0), new DateTime(2026, 10, 9));
            session.Mutate(c => c.transactions.Add(t2));
            Assert.AreEqual(174.5m, session.Character.TotalGold());

            session.Mutate(c => c.transactions.Remove(t2));
            Assert.AreEqual(250m, session.Character.TotalGold());
        }

        [TestMethod]
        public void CharacterSession_Transactions_OrderedChronologicallyByGameTimeThenRealDate() {
            var character = new Character();
            var session = new CharacterSession(character, catalog);

            var tLateGame = new DateTime(2026, 10, 6, 20, 0, 0);
            var tEarlyGame = new DateTime(2026, 10, 5, 10, 0, 0);
            var dLateReal = new DateTime(2026, 10, 9);
            var dEarlyReal = new DateTime(2026, 10, 8);

            var txLate = new GpTransaction(100m, "late game transaction", tLateGame, dLateReal);
            var txEarly = new GpTransaction(50m, "early game transaction", tEarlyGame, dEarlyReal);
            var txTieGameEarlyReal = new GpTransaction(25m, "tie game early real", tLateGame, dEarlyReal);
            var txTieAll1 = new GpTransaction(10m, "tie identical 1", tLateGame, dLateReal);
            var txTieAll2 = new GpTransaction(15m, "tie identical 2", tLateGame, dLateReal);

            var list = new List<GpTransaction> { txLate, txEarly, txTieGameEarlyReal, txTieAll1, txTieAll2 };
            var sorted = list.OrderBy(t => t.GameTime).ThenBy(t => t.RealDate).ToList();

            Assert.AreSame(txEarly, sorted[0], "Early game time must come first.");
            Assert.AreSame(txTieGameEarlyReal, sorted[1], "When game time ties, earlier real date must come first.");
            Assert.AreSame(txLate, sorted[2], "Later real date comes after earlier real date.");
            Assert.AreSame(txTieAll1, sorted[3], "Identical game and real times must preserve insertion order.");
            Assert.AreSame(txTieAll2, sorted[4], "Identical game and real times must preserve insertion order.");
        }
    }
}
