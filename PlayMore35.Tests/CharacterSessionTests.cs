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

        [TestMethod]
        public void CharacterSession_Sessions_MutationsRecomputeTotalExperience() {
            var character = new Character();
            var session = new CharacterSession(character, catalog);

            Assert.AreEqual(0, session.Character.TotalExperience());

            var s1 = new XpSessionEntry(300, "session 1", new DateTime(2026, 10, 6, 12, 0, 0), new DateTime(2026, 10, 9));
            session.Mutate(c => c.sessions.Add(s1));
            Assert.AreEqual(300, session.Character.TotalExperience());

            var s2 = new XpSessionEntry(500, "session 2", new DateTime(2026, 10, 6, 13, 0, 0), new DateTime(2026, 10, 9));
            session.Mutate(c => c.sessions.Add(s2));
            Assert.AreEqual(800, session.Character.TotalExperience());

            session.Mutate(c => c.sessions.Remove(s2));
            Assert.AreEqual(300, session.Character.TotalExperience());
        }

        [TestMethod]
        public void CharacterSession_Sessions_OrderedChronologicallyByGameTimeThenRealDate() {
            var character = new Character();
            var session = new CharacterSession(character, catalog);

            var tLateGame = new DateTime(2026, 10, 6, 20, 0, 0);
            var tEarlyGame = new DateTime(2026, 10, 5, 10, 0, 0);
            var dLateReal = new DateTime(2026, 10, 9);
            var dEarlyReal = new DateTime(2026, 10, 8);

            var sLate = new XpSessionEntry(100, "late game session", tLateGame, dLateReal);
            var sEarly = new XpSessionEntry(50, "early game session", tEarlyGame, dEarlyReal);
            var sTieGameEarlyReal = new XpSessionEntry(25, "tie game early real", tLateGame, dEarlyReal);
            var sTieAll1 = new XpSessionEntry(10, "tie identical 1", tLateGame, dLateReal);
            var sTieAll2 = new XpSessionEntry(15, "tie identical 2", tLateGame, dLateReal);

            var list = new List<XpSessionEntry> { sLate, sEarly, sTieGameEarlyReal, sTieAll1, sTieAll2 };
            var sorted = list.OrderBy(s => s.GameTime).ThenBy(s => s.RealDate).ToList();

            Assert.AreSame(sEarly, sorted[0], "Early game time must come first.");
            Assert.AreSame(sTieGameEarlyReal, sorted[1], "When game time ties, earlier real date must come first.");
            Assert.AreSame(sLate, sorted[2], "Later real date comes after earlier real date.");
            Assert.AreSame(sTieAll1, sorted[3], "Identical game and real times must preserve insertion order.");
            Assert.AreSame(sTieAll2, sorted[4], "Identical game and real times must preserve insertion order.");
        }

        // Source: System Reference Document 3.5, Condition Summary — Fatigued (https://www.d20srd.org/srd/conditionSummary.htm#fatigued)
        [TestMethod]
        public void WhirlingFrenzy_AfterEffect_IncrementsFatigueLevelByOne_AndRevertsAtEncounterEnd() {
            var character = new Character();
            var session = new CharacterSession(character, catalog);
            session.Mutate(c => {
                c.race = "human";
                c.classLevels.Clear();
                c.classLevels.Add("Barbarian");
                c.assignedAbilityScores["STR"] = 14;
                c.assignedAbilityScores["DEX"] = 12;
                c.assignedAbilityScores["CON"] = 14;
                c.assignedAbilityScores["INT"] = 10;
                c.assignedAbilityScores["WIS"] = 10;
                c.assignedAbilityScores["CHA"] = 10;
            });

            Assert.AreEqual(0, session.GetStatistic("Fatigue level").Value);
            Assert.AreEqual(14, session.GetStatistic(Constants.STR_SCORE).Value);
            Assert.AreEqual(12, session.GetStatistic(Constants.DEX_SCORE).Value);

            session.StartCombat();

            var frenzyFlag = session.GetControllableFlags().First(f => f.Name == "whirling frenzy");
            var frenzyAction = new SheetAction().SetFlag(frenzyFlag.Name).Duration(frenzyFlag.EffectDuration);
            if (frenzyFlag.ActionAfter != null) {
                frenzyAction.AndThen(frenzyFlag.ActionAfter);
            }
            session.PerformAction(frenzyAction);

            Assert.IsTrue(session.HasFlag("whirling frenzy"));
            Assert.AreEqual(18, session.GetStatistic(Constants.STR_SCORE).Value);
            Assert.AreEqual(0, session.GetStatistic("Fatigue level").Value);

            // Advance 5 rounds so frenzy expires and after-effect triggers
            session.AdvanceTimeline(5);

            Assert.IsFalse(session.HasFlag("whirling frenzy"));
            Assert.AreEqual(1, session.GetStatistic("Fatigue level").Value);
            Assert.AreEqual(12, session.GetStatistic(Constants.STR_SCORE).Value);
            Assert.AreEqual(10, session.GetStatistic(Constants.DEX_SCORE).Value);
            Assert.IsTrue(session.HasFlag("Can't run or charge"));

            // End combat reverts the encounter duration fatigue
            session.EndCombat();

            Assert.AreEqual(0, session.GetStatistic("Fatigue level").Value);
            Assert.AreEqual(14, session.GetStatistic(Constants.STR_SCORE).Value);
            Assert.AreEqual(12, session.GetStatistic(Constants.DEX_SCORE).Value);
            Assert.IsFalse(session.HasFlag("Can't run or charge"));
        }

        // Source: System Reference Document 3.5, Condition Summary — Fatigued (https://www.d20srd.org/srd/conditionSummary.htm#fatigued)
        // Source: System Reference Document 3.5, Condition Summary — Exhausted (https://www.d20srd.org/srd/conditionSummary.htm#exhausted)
        [TestMethod]
        public void WhirlingFrenzy_AfterEffect_StacksWithExistingFatiguedToExhausted() {
            var character = new Character();
            var session = new CharacterSession(character, catalog);
            session.Mutate(c => {
                c.race = "human";
                c.classLevels.Clear();
                c.classLevels.Add("Barbarian");
                c.assignedAbilityScores["STR"] = 14;
                c.assignedAbilityScores["DEX"] = 12;
                c.assignedAbilityScores["CON"] = 14;
                c.assignedAbilityScores["INT"] = 10;
                c.assignedAbilityScores["WIS"] = 10;
                c.assignedAbilityScores["CHA"] = 10;
            });

            // Character is already fatigued via manual condition flag
            session.SetFlag("fatigued", true);
            Assert.AreEqual(1, session.GetStatistic("Fatigue level").Value);
            Assert.AreEqual(12, session.GetStatistic(Constants.STR_SCORE).Value);
            Assert.AreEqual(10, session.GetStatistic(Constants.DEX_SCORE).Value);

            session.StartCombat();

            var frenzyFlag = session.GetControllableFlags().First(f => f.Name == "whirling frenzy");
            var frenzyAction = new SheetAction().SetFlag(frenzyFlag.Name).Duration(frenzyFlag.EffectDuration);
            if (frenzyFlag.ActionAfter != null) {
                frenzyAction.AndThen(frenzyFlag.ActionAfter);
            }
            session.PerformAction(frenzyAction);

            // Advance 5 rounds so frenzy expires and after-effect increments fatigue level from 1 to 2
            session.AdvanceTimeline(5);

            Assert.IsFalse(session.HasFlag("whirling frenzy"));
            Assert.AreEqual(2, session.GetStatistic("Fatigue level").Value);
            // Exhausted: -6 penalty to STR and DEX
            Assert.AreEqual(8, session.GetStatistic(Constants.STR_SCORE).Value);
            Assert.AreEqual(6, session.GetStatistic(Constants.DEX_SCORE).Value);
            Assert.IsTrue(session.HasFlag("Can't run or charge"));

            // End combat reverts only the encounter-duration increment (-1), leaving manual fatigued (+1) intact
            session.EndCombat();

            Assert.AreEqual(1, session.GetStatistic("Fatigue level").Value);
            Assert.AreEqual(12, session.GetStatistic(Constants.STR_SCORE).Value);
            Assert.AreEqual(10, session.GetStatistic(Constants.DEX_SCORE).Value);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CharacterSession_Constructor_ThrowsArgumentNullException_WhenCharacterIsNull() {
            new CharacterSession(null, catalog);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CharacterSession_Constructor_ThrowsArgumentNullException_WhenCatalogIsNull() {
            new CharacterSession(new Character(), null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CharacterSession_Mutate_ThrowsArgumentNullException_WhenMutateActionIsNull() {
            var session = new CharacterSession(new Character(), catalog);
            session.Mutate(null);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void CharacterSession_GetStartingIntelligence_Throws_WhenRaceNotFoundInCatalog() {
            var character = new Character { race = "NonExistentRace" };
            var session = new CharacterSession(character, catalog);
            session.GetStartingIntelligence();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void CharacterSession_GetSizeCategory_Throws_WhenRaceNotFoundInCatalog() {
            var character = new Character { race = "NonExistentRace" };
            var session = new CharacterSession(character, catalog);
            session.GetSizeCategory();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void CharacterSession_BaseHp_Throws_WhenRaceNotFoundInCatalog() {
            var character = new Character { race = "NonExistentRace" };
            var session = new CharacterSession(character, catalog);
            session.BaseHp();
        }
    }
}
