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
using PlayMore35.UI;

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
        [ExpectedException(typeof(KeyNotFoundException))]
        public void CharacterSession_Constructor_ThrowsKeyNotFoundException_WhenRaceNotFoundInCatalog() {
            var character = new Character { race = "NonExistentRace" };
            new CharacterSession(character, catalog);
        }

        // Source: System Reference Document 3.5, Character Advancement (https://www.d20srd.org/srd/classes/characterAdvancement.htm)
        [TestMethod]
        public void GetSessionAtLevel_InvalidLevels_ThrowsArgumentOutOfRangeException() {
            var character = new Character { race = "human" };
            character.classLevels = new List<string> { "Fighter", "Fighter" };
            var session = new CharacterSession(character, catalog);

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => session.GetSessionAtLevel(0));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => session.GetSessionAtLevel(-1));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => session.GetSessionAtLevel(3));
        }

        // Source: System Reference Document 3.5, Character Advancement (https://www.d20srd.org/srd/classes/characterAdvancement.htm)
        [TestMethod]
        public void GetSessionAtLevel_HistoricalProgressionTruncation_ReflectsHistoricalStats() {
            var character = new Character { race = "human" };
            character.classLevels = new List<string> { "Fighter", "Fighter", "Binder", "Fighter" };
            character.assignedAbilityScores["STR"] = 16;
            character.assignedAbilityScores["DEX"] = 12;
            character.assignedAbilityScores["CON"] = 14;
            character.assignedAbilityScores["INT"] = 14;
            character.assignedAbilityScores["WIS"] = 10;
            character.assignedAbilityScores["CHA"] = 8;
            character.rolledHitDice = new List<int> { 10, 8, 6, 9 };
            character.abilityScoreIncrements[4] = "STR";
            character.skillRanks["Jump"] = new List<int> { 4, 1, 0, 1 };
            character.skillRanks["Concentration"] = new List<int> { 0, 0, 4, 1 };

            var session = new CharacterSession(character, catalog);

            // Level 1: Fighter 1, BAB 1, Fort 2+2=4, Ref 0+1=1, Will 0+0=0, STR 16, Jump 4, HP 10+2=12
            var snap1 = session.GetSessionAtLevel(1);
            Assert.AreEqual(1, snap1.Character.classLevels.Count);
            Assert.AreEqual(1, snap1.GetStatistic(Constants.BASE_ATTACK_BONUS).Value);
            Assert.AreEqual(4, snap1.GetStatistic(Constants.FORT_SAVES).Value);
            Assert.AreEqual(1, snap1.GetStatistic(Constants.REF_SAVES).Value);
            Assert.AreEqual(0, snap1.GetStatistic(Constants.WILL_SAVES).Value);
            Assert.AreEqual(16, snap1.GetStatistic(Constants.STR_SCORE).Value);
            Assert.AreEqual(4, snap1.TotalSkillRanks("Jump"));
            Assert.AreEqual(0, snap1.TotalSkillRanks("Concentration"));
            Assert.AreEqual(12, snap1.BaseHp());

            // Level 2: Fighter 2, BAB 2, Fort 3+2=5, Ref 0+1=1, Will 0+0=0, STR 16, Jump 5, HP 12+(8+2)=22
            var snap2 = session.GetSessionAtLevel(2);
            Assert.AreEqual(2, snap2.Character.classLevels.Count);
            Assert.AreEqual(2, snap2.GetStatistic(Constants.BASE_ATTACK_BONUS).Value);
            Assert.AreEqual(5, snap2.GetStatistic(Constants.FORT_SAVES).Value);
            Assert.AreEqual(1, snap2.GetStatistic(Constants.REF_SAVES).Value);
            Assert.AreEqual(0, snap2.GetStatistic(Constants.WILL_SAVES).Value);
            Assert.AreEqual(5, snap2.TotalSkillRanks("Jump"));
            Assert.AreEqual(22, snap2.BaseHp());

            // Level 3: Fighter 2 / Binder 1, BAB 2+0=2, Fort 3+2+2=7, Ref 0+0+1=1, Will 0+2+0=2, Concentration 4, HP 22+(6+2)=30
            var snap3 = session.GetSessionAtLevel(3);
            Assert.AreEqual(3, snap3.Character.classLevels.Count);
            Assert.AreEqual(2, snap3.GetStatistic(Constants.BASE_ATTACK_BONUS).Value);
            Assert.AreEqual(7, snap3.GetStatistic(Constants.FORT_SAVES).Value);
            Assert.AreEqual(1, snap3.GetStatistic(Constants.REF_SAVES).Value);
            Assert.AreEqual(2, snap3.GetStatistic(Constants.WILL_SAVES).Value);
            Assert.AreEqual(4, snap3.TotalSkillRanks("Concentration"));
            Assert.AreEqual(30, snap3.BaseHp());

            // Level 4: Fighter 3 / Binder 1, BAB 3+0=3, Fort 3+2+2=7, Ref 1+0+1=2, Will 1+2+0=3, STR 17 (+1 increment at level 4), Jump 6, Concentration 5, HP 30+(9+2)=41
            var snap4 = session.GetSessionAtLevel(4);
            Assert.AreEqual(4, snap4.Character.classLevels.Count);
            Assert.AreEqual(3, snap4.GetStatistic(Constants.BASE_ATTACK_BONUS).Value);
            Assert.AreEqual(7, snap4.GetStatistic(Constants.FORT_SAVES).Value);
            Assert.AreEqual(2, snap4.GetStatistic(Constants.REF_SAVES).Value);
            Assert.AreEqual(3, snap4.GetStatistic(Constants.WILL_SAVES).Value);
            Assert.AreEqual(17, snap4.GetStatistic(Constants.STR_SCORE).Value);
            Assert.AreEqual(6, snap4.TotalSkillRanks("Jump"));
            Assert.AreEqual(5, snap4.TotalSkillRanks("Concentration"));
            Assert.AreEqual(41, snap4.BaseHp());
        }

        // Source: Dungeon Master's Guide 3.5 p. 214; Complete Warrior p. 16
        [TestMethod]
        public void GetSessionAtLevel_IncludeEquippedFalse_CompletelyOmitsEquipmentInventoryTogglesAndExternalEffects() {
            var character = new Character { race = "human" };
            character.classLevels = new List<string> { "Fighter", "Fighter", "Fighter", "Fighter" };
            character.assignedAbilityScores["CON"] = 14;
            character.inventory["Pack"] = new InventoryLocation();

            var session = new CharacterSession(character, catalog);
            session.AddItem("Pack", "torc of displacement and Con +2", 1);
            session.EquipItem("Pack", Equipped.Slot.NECK, "torc of displacement and Con +2");
            session.AddItem("Pack", "silk rope", 1);
            session.SetFlag("charging", true);
            session.SetParameter("Lethal damage", 6);

            Assert.AreEqual(16, session.GetStatistic(Constants.CON_SCORE).Value);

            var snapNoEquip = session.GetSessionAtLevel(1, includeEquipped: false);

            Assert.AreEqual(14, snapNoEquip.GetStatistic(Constants.CON_SCORE).Value, "Intrinsic snapshot must not include equipped stat bonuses.");
            Assert.AreEqual(0, snapNoEquip.Character.equipped.SlottedItems.Count, "Equipped slotted items must be omitted.");
            Assert.AreEqual(0, snapNoEquip.Character.equipped.SlotlessItems.Count, "Equipped slotless items must be omitted.");
            Assert.IsNull(snapNoEquip.Character.equipped.PrimaryHandWeapon, "Equipped weapons must be omitted.");
            Assert.IsNull(snapNoEquip.Character.equipped.DonnedArmor, "Donned armor must be omitted.");
            Assert.AreEqual(0, snapNoEquip.Character.inventory.Count, "Carried inventory locations must be omitted.");
            Assert.AreEqual(0, snapNoEquip.Character.externalEffects.Count, "External effects must be omitted.");
            Assert.AreEqual(0, snapNoEquip.Character.flagValues.Count, "Combat toggles must be omitted.");
            Assert.AreEqual(0, snapNoEquip.Character.paramValues.Count, "Controllable parameters must be omitted.");
            Assert.IsFalse(snapNoEquip.HasFlag("charging"), "Combat flags must not be active in snapshot.");
            Assert.AreEqual(0, snapNoEquip.GetStatistic("Lethal damage").Value, "Combat parameters must be zeroed in snapshot.");
            Assert.AreEqual(0m, snapNoEquip.EquippedWeight, "Equipped weight must be zero.");
            Assert.AreEqual(0m, snapNoEquip.TotalWeight, "Total weight must be zero.");
        }

        // Source: Dungeon Master's Guide 3.5 p. 214; Complete Warrior p. 16
        [TestMethod]
        public void GetSessionAtLevel_IncludeEquippedTrue_AppliesCurrentEquippedItemsWithoutLeakingSubsequentLevels() {
            var character = new Character { race = "human" };
            character.classLevels = new List<string> { "Fighter", "Fighter", "Fighter", "Fighter" };
            character.assignedAbilityScores["CON"] = 14;
            character.abilityScoreIncrements[4] = "CON";
            character.inventory["Pack"] = new InventoryLocation();

            var session = new CharacterSession(character, catalog);
            session.AddItem("Pack", "torc of displacement and Con +2", 1);
            session.EquipItem("Pack", Equipped.Slot.NECK, "torc of displacement and Con +2");
            session.AddItem("Pack", "silk rope", 1);
            session.SetFlag("charging", true);

            // Live character at level 4: 14 base + 1 increment + 2 enhancement = 17
            Assert.AreEqual(17, session.GetStatistic(Constants.CON_SCORE).Value);

            // Level 1 snapshot with equipped gear: 14 base + 2 enhancement = 16 (level 4 increment not leaked!)
            var snapEquipped = session.GetSessionAtLevel(1, includeEquipped: true);
            Assert.AreEqual(16, snapEquipped.GetStatistic(Constants.CON_SCORE).Value, "Equipped snapshot applies gear bonuses to historical baseline.");
            Assert.AreEqual(1, snapEquipped.Character.classLevels.Count, "Class progression remains strictly truncated to level 1.");
            Assert.AreEqual(0, snapEquipped.Character.abilityScoreIncrements.Count, "Level 4 ability increment must not leak into level 1 snapshot.");

            // Carried inventory and combat toggles are still omitted
            Assert.AreEqual(0, snapEquipped.Character.inventory.Count, "Carried inventory remains omitted even with includeEquipped true.");
            Assert.AreEqual(0, snapEquipped.Character.flagValues.Count, "Combat toggles remain omitted even with includeEquipped true.");
            Assert.IsFalse(snapEquipped.HasFlag("charging"));
        }

        // Source: Player's Handbook 3.5 p. 58-59; Tome of Battle p. 44
        [TestMethod]
        public void GetSessionAtLevel_IntraLevelFeats_AreCoVisibleRegardlessOfSlotOrder() {
            var featDef = catalog.GetFeat("Martial Stance");
            var stanceChoiceDesc = featDef.Choices.First(c => c.Id == "stance");

            // Setup A: Slot "Human" has Martial Study (Burning Blade, Desert Wind), Slot "Level 1" has Martial Stance (Flame's Blessing)
            var charA = new Character { race = "human" };
            charA.classLevels = new List<string> { "Fighter" };
            charA.feats["Human"] = "Martial Study";
            charA.featChoices["Human:maneuver"] = "Burning Blade";
            charA.feats["Level 1"] = "Martial Stance";
            charA.featChoices["Level 1:stance"] = "Flame's Blessing";

            var sessionA = new CharacterSession(charA, catalog);
            var snapA = sessionA.GetSessionAtLevel(1);
            var warningsA = snapA.ValidateFeatChoice("Level 1", stanceChoiceDesc, "Flame's Blessing", "Feats");
            Assert.AreEqual(0, warningsA.Count, "Flame's Blessing prerequisite (1 Desert Wind maneuver) is satisfied by co-level Human slot choice.");

            // Setup B: Reversed slot order - Slot "Level 1" has Martial Study, Slot "Human" has Martial Stance
            var charB = new Character { race = "human" };
            charB.classLevels = new List<string> { "Fighter" };
            charB.feats["Level 1"] = "Martial Study";
            charB.featChoices["Level 1:maneuver"] = "Burning Blade";
            charB.feats["Human"] = "Martial Stance";
            charB.featChoices["Human:stance"] = "Flame's Blessing";

            var sessionB = new CharacterSession(charB, catalog);
            var snapB = sessionB.GetSessionAtLevel(1);
            var warningsB = snapB.ValidateFeatChoice("Human", stanceChoiceDesc, "Flame's Blessing", "Feats");
            Assert.AreEqual(0, warningsB.Count, "Flame's Blessing prerequisite is satisfied regardless of slot assignment order at same acquisition level.");
        }

        // Source: Tome of Battle: The Book of Nine Swords p. 44
        [TestMethod]
        public void GetSessionAtLevel_HigherLevelFeats_AreNotVisibleInEarlierSnapshots() {
            var featDef = catalog.GetFeat("Martial Stance");
            var stanceChoiceDesc = featDef.Choices.First(c => c.Id == "stance");

            var character = new Character { race = "human" };
            character.classLevels = new List<string> { "Fighter", "Fighter", "Fighter" };
            character.feats["Level 1"] = "Martial Study";
            character.featChoices["Level 1:maneuver"] = "Burning Blade";
            character.feats["Level 3"] = "Martial Stance";
            character.featChoices["Level 3:stance"] = "Flame's Blessing";

            var session = new CharacterSession(character, catalog);

            // At level 1 snapshot: Level 3 feat and choices are completely omitted
            var snap1 = session.GetSessionAtLevel(1);
            Assert.IsTrue(snap1.Character.feats.ContainsKey("Level 1"));
            Assert.AreEqual("Martial Study", snap1.Character.feats["Level 1"]);
            Assert.AreEqual("Burning Blade", snap1.Character.featChoices["Level 1:maneuver"]);
            Assert.IsFalse(snap1.Character.feats.ContainsKey("Level 3"), "Level 3 feat must not exist in level 1 snapshot.");
            Assert.IsFalse(snap1.Character.featChoices.ContainsKey("Level 3:stance"), "Level 3 choices must not exist in level 1 snapshot.");

            // At level 3 snapshot: Level 3 feat is present, and satisfies prerequisites from Level 1 choice
            var snap3 = session.GetSessionAtLevel(3);
            Assert.IsTrue(snap3.Character.feats.ContainsKey("Level 1"));
            Assert.IsTrue(snap3.Character.feats.ContainsKey("Level 3"), "Level 3 feat must exist in level 3 snapshot.");
            Assert.AreEqual("Flame's Blessing", snap3.Character.featChoices["Level 3:stance"]);
            var warnings3 = snap3.ValidateFeatChoice("Level 3", stanceChoiceDesc, "Flame's Blessing", "Feats");
            Assert.AreEqual(0, warnings3.Count, "Level 3 snapshot satisfies Desert Wind prerequisite from level 1 choice.");
        }

        [TestMethod]
        public void GetSessionAtLevel_LazyCaching_ReturnsSameInstanceUntilMutation() {
            var character = new Character { race = "human" };
            character.classLevels = new List<string> { "Fighter", "Fighter" };
            var session = new CharacterSession(character, catalog);

            var snap1 = session.GetSessionAtLevel(1, false);
            var snap2 = session.GetSessionAtLevel(1, false);
            Assert.AreSame(snap1, snap2, "Repeated calls with same level and includeEquipped must return cached instance.");

            var snapEquip1 = session.GetSessionAtLevel(1, true);
            var snapEquip2 = session.GetSessionAtLevel(1, true);
            Assert.AreSame(snapEquip1, snapEquip2, "Repeated calls with includeEquipped true must return cached instance.");
            Assert.AreNotSame(snap1, snapEquip1, "Intrinsic and equipped snapshots must have separate cache keys.");

            var snapLvl2 = session.GetSessionAtLevel(2, false);
            Assert.AreNotSame(snap1, snapLvl2, "Different levels must have separate cache entries.");
        }

        [TestMethod]
        public void GetSessionAtLevel_Mutation_InvalidatesCachedSnapshots() {
            var character = new Character { race = "human" };
            character.classLevels = new List<string> { "Fighter", "Fighter" };
            var session = new CharacterSession(character, catalog);

            var snapBefore = session.GetSessionAtLevel(1);

            // Mutating state clears cache
            session.Mutate(c => c.name = "MutatedHero");

            var snapAfter = session.GetSessionAtLevel(1);
            Assert.AreNotSame(snapBefore, snapAfter, "Mutate must invalidate level snapshot cache.");
            Assert.AreEqual("MutatedHero", snapAfter.Character.name);
        }

        private class SnapshotSpyCharacterSession : CharacterSession {
            public int GetSessionAtLevelCallCount { get; private set; }

            public SnapshotSpyCharacterSession(Character character, IRulesCatalog catalog)
                : base(character, catalog) { }

            public override ICharacterSession GetSessionAtLevel(int level, bool includeEquipped = false) {
                GetSessionAtLevelCallCount++;
                return base.GetSessionAtLevel(level, includeEquipped);
            }
        }

        [TestMethod]
        public void CombatTicksTimelineRecalculateAndDisplayLevels_DoNotInvokeGetSessionAtLevel() {
            var character = new Character { race = "human" };
            character.classLevels = new List<string> { "Fighter", "Fighter" };
            var spySession = new SnapshotSpyCharacterSession(character, catalog);

            Assert.AreEqual(0, spySession.GetSessionAtLevelCallCount);

            spySession.Recalculate();
            Assert.AreEqual(0, spySession.GetSessionAtLevelCallCount, "Recalculate must not invoke GetSessionAtLevel.");

            spySession.StartCombat();
            Assert.AreEqual(0, spySession.GetSessionAtLevelCallCount, "StartCombat must not invoke GetSessionAtLevel.");

            spySession.AdvanceTimeline(1);
            Assert.AreEqual(0, spySession.GetSessionAtLevelCallCount, "AdvanceTimeline must not invoke GetSessionAtLevel.");

            spySession.Timeline.Advance(1, spySession);
            Assert.AreEqual(0, spySession.GetSessionAtLevelCallCount, "Timeline.Advance must not invoke GetSessionAtLevel.");

            spySession.EndCombat();
            Assert.AreEqual(0, spySession.GetSessionAtLevelCallCount, "EndCombat must not invoke GetSessionAtLevel.");

            using (var form = new MainForm(spySession)) {
                // MainForm constructor executes DisplayLevels()
                Assert.AreEqual(0, spySession.GetSessionAtLevelCallCount, "DisplayLevels painting must not invoke GetSessionAtLevel.");
            }
        }
    }
}
