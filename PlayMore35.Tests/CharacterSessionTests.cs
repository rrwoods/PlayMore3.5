using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Items;
using PlayMore35.Rules;
using PlayMore35.Subsystems;
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
            Assert.IsTrue(session.HasFlag("charging"));

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
            Assert.IsTrue(session.HasFlag("charging"));

            var result = session.AdvanceTimeline(new LengthOfTime("1 round"));
            Assert.IsFalse(session.HasFlag("charging"));
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

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => session.GetSessionAtLevel(-1));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => session.GetSessionAtLevel(3));
        }

        // Source: Player's Handbook 3.5, pp. 22, 58; ADR-0017, ADR-0018
        [TestMethod]
        public void GetSessionAtLevel_LevelZero_ProjectsBaselineWithoutClassesOrAdvancement() {
            var character = new Character { race = "elf" };
            character.classLevels = new List<string> { "Fighter", "Fighter" };
            character.assignedAbilityScores["STR"] = 14;
            character.assignedAbilityScores["DEX"] = 14;
            character.assignedAbilityScores["CON"] = 14;
            character.assignedAbilityScores["INT"] = 10;
            character.assignedAbilityScores["WIS"] = 10;
            character.assignedAbilityScores["CHA"] = 10;
            character.rolledHitDice = new List<int> { 10, 8 };
            character.skillRanks["Jump"] = new List<int> { 4, 1 };
            character.feats.Add(new FeatAssignment(ProgressionSlotKey.Advancement(1), "Power Attack"));

            var session = new CharacterSession(character, catalog);
            var snap0 = session.GetSessionAtLevel(0);

            Assert.IsNotNull(snap0);
            Assert.AreEqual(0, snap0.Character.classLevels.Count, "Level 0 snapshot must have 0 class levels.");
            Assert.AreEqual(0, snap0.Character.rolledHitDice.Count, "Level 0 snapshot must have 0 rolled hit dice.");
            Assert.AreEqual(0, snap0.BaseHp(), "Level 0 snapshot base HP must be 0.");
            Assert.AreEqual(0, snap0.TotalSkillRanks("Jump"), "Level 0 snapshot must have 0 skill ranks.");
            Assert.AreEqual(0, snap0.Character.feats.Count, "Level 0 snapshot must have 0 feats.");
            Assert.AreEqual(0, snap0.GetStatistic(Constants.BASE_ATTACK_BONUS).Value, "Level 0 snapshot must have BAB 0.");

            // Ability scores: assigned + racial traits (Elf: +2 DEX, -2 CON)
            Assert.AreEqual(14, snap0.GetStatistic(Constants.STR_SCORE).Value);
            Assert.AreEqual(16, snap0.GetStatistic(Constants.DEX_SCORE).Value, "Elf +2 DEX applies at level 0.");
            Assert.AreEqual(12, snap0.GetStatistic(Constants.CON_SCORE).Value, "Elf -2 CON applies at level 0.");

            // Saves: ability modifiers without class base save bonuses (CON mod = +1, DEX mod = +3, WIS mod = 0)
            Assert.AreEqual(1, snap0.GetStatistic(Constants.FORT_SAVES).Value);
            Assert.AreEqual(3, snap0.GetStatistic(Constants.REF_SAVES).Value);
            Assert.AreEqual(0, snap0.GetStatistic(Constants.WILL_SAVES).Value);
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
            Assert.AreEqual(0, ((ICharacterContext)snapNoEquip).Character.externalEffects.Count, "External effects must be omitted.");
            Assert.AreEqual(0, ((ICharacterContext)snapNoEquip).Character.flagValues.Count, "Combat toggles must be omitted.");
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
            Assert.AreEqual(0, ((ICharacterContext)snapEquipped).Character.flagValues.Count, "Combat toggles remain omitted even with includeEquipped true.");
            Assert.IsFalse(snapEquipped.HasFlag("charging"));
        }

        // Source: Player's Handbook 3.5 p. 58-59; Tome of Battle p. 44
        [TestMethod]
        public void GetSessionAtLevel_IntraLevelFeats_AreCoVisibleRegardlessOfSlotOrder() {
            var featDef = catalog.GetFeat("Martial Stance");
            var stanceChoiceDesc = featDef.Choices.First(c => c.Id == "stance");
            var humanSlot = ProgressionSlotKey.Race("human", 1);
            var slot1 = ProgressionSlotKey.Advancement(1);

            // Setup A: Slot "Human" has Martial Study (Burning Blade, Desert Wind), Slot "Level 1" has Martial Stance (Flame's Blessing)
            var charA = new Character { race = "human" };
            charA.classLevels = new List<string> { "Fighter" };
            charA.feats.Add(new FeatAssignment(
                humanSlot,
                "Martial Study",
                new Dictionary<string, string> { { "maneuver", "Burning Blade" } }));
            charA.feats.Add(new FeatAssignment(
                slot1,
                "Martial Stance",
                new Dictionary<string, string> { { "stance", "Flame's Blessing" } }));

            var sessionA = new CharacterSession(charA, catalog);
            var snapA = sessionA.GetSessionAtLevel(1);
            var warningsA = snapA.ValidateProgressionChoice(new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Flame's Blessing",
                slotKey: slot1.ToString(),
                targetSource: "Feats",
                descriptor: stanceChoiceDesc,
                slot: slot1
            ));
            Assert.AreEqual(0, warningsA.Count, "Flame's Blessing prerequisite (1 Desert Wind maneuver) is satisfied by co-level Human slot choice.");

            // Setup B: Reversed slot order - Slot "Level 1" has Martial Study, Slot "Human" has Martial Stance
            var charB = new Character { race = "human" };
            charB.classLevels = new List<string> { "Fighter" };
            charB.feats.Add(new FeatAssignment(
                slot1,
                "Martial Study",
                new Dictionary<string, string> { { "maneuver", "Burning Blade" } }));
            charB.feats.Add(new FeatAssignment(
                humanSlot,
                "Martial Stance",
                new Dictionary<string, string> { { "stance", "Flame's Blessing" } }));

            var sessionB = new CharacterSession(charB, catalog);
            var snapB = sessionB.GetSessionAtLevel(1);
            var warningsB = snapB.ValidateProgressionChoice(new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Flame's Blessing",
                slotKey: humanSlot.ToString(),
                targetSource: "Feats",
                descriptor: stanceChoiceDesc,
                slot: humanSlot
            ));
            Assert.AreEqual(0, warningsB.Count, "Flame's Blessing prerequisite is satisfied regardless of slot assignment order at same acquisition level.");
        }

        // Source: Tome of Battle: The Book of Nine Swords p. 44
        [TestMethod]
        public void GetSessionAtLevel_HigherLevelFeats_AreNotVisibleInEarlierSnapshots() {
            var featDef = catalog.GetFeat("Martial Stance");
            var stanceChoiceDesc = featDef.Choices.First(c => c.Id == "stance");

            var character = new Character { race = "human" };
            character.classLevels = new List<string> { "Fighter", "Fighter", "Fighter" };
            var slot1 = ProgressionSlotKey.Advancement(1);
            var slot3 = ProgressionSlotKey.Advancement(3);
            character.feats.Add(new FeatAssignment(
                slot1,
                "Martial Study",
                new Dictionary<string, string> { { "maneuver", "Burning Blade" } }));
            character.feats.Add(new FeatAssignment(
                slot3,
                "Martial Stance",
                new Dictionary<string, string> { { "stance", "Flame's Blessing" } }));

            var session = new CharacterSession(character, catalog);

            // At level 1 snapshot: Level 3 feat and choices are completely omitted
            var snap1 = session.GetSessionAtLevel(1);
            Assert.AreEqual("Martial Study", snap1.GetFeat(slot1));
            Assert.AreEqual("Burning Blade", snap1.GetFeatChoice(slot1, "maneuver"));
            Assert.IsNull(snap1.GetFeat(slot3), "Level 3 feat must not exist in level 1 snapshot.");
            Assert.IsNull(snap1.GetFeatChoice(slot3, "stance"), "Level 3 choices must not exist in level 1 snapshot.");

            // At level 3 snapshot: Level 3 feat is present, and satisfies prerequisites from Level 1 choice
            var snap3 = session.GetSessionAtLevel(3);
            Assert.AreEqual("Martial Study", snap3.GetFeat(slot1));
            Assert.AreEqual("Martial Stance", snap3.GetFeat(slot3), "Level 3 feat must exist in level 3 snapshot.");
            Assert.AreEqual("Flame's Blessing", snap3.GetFeatChoice(slot3, "stance"));
            var warnings3 = snap3.ValidateProgressionChoice(new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Flame's Blessing",
                slotKey: slot3.ToString(),
                targetSource: "Feats",
                descriptor: stanceChoiceDesc,
                slot: slot3
            ));
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

        [TestMethod]
        public void SetFeatChoice_DirectChoice_InvalidatesCachedSnapshotsAndRecalculatesDependentStats() {
            var syntheticFeat = new Feat(new JObject(
                new JProperty("benefit", new JObject(
                    new JProperty("name", "Synthetic Direct Feat"),
                    new JProperty("modifiers", new JArray(
                        new JObject(
                            new JProperty("affects", "$"),
                            new JProperty("mod", new JObject(
                                new JProperty("type", "untyped"),
                                new JProperty("amounts", new JArray(new JObject(new JProperty("fixed", 3))))
                            ))
                        )
                    ))
                )),
                new JProperty("choice", "skill"),
                new JProperty("stats", new JArray(
                    new JObject(new JProperty("name", "Climb"))
                ))
            ));

            var fakeCatalog = new FakeRulesCatalog(inner: catalog);
            fakeCatalog.FeatsMap[syntheticFeat.Benefit.Name] = syntheticFeat;

            var character = new Character { race = "human" };
            character.classLevels.Add("Fighter");
            var slot1 = ProgressionSlotKey.Advancement(1);
            character.feats.Add(new FeatAssignment(slot1, syntheticFeat.Benefit.Name));

            var session = new CharacterSession(character, fakeCatalog);
            var climbBefore = session.GetStatistic("Climb").Value;
            var snapBefore = session.GetSessionAtLevel(1);

            session.SetFeatChoice(slot1, "", "Climb");

            var snapAfter = session.GetSessionAtLevel(1);
            Assert.AreNotSame(snapBefore, snapAfter, "SetFeatChoice must invalidate level snapshot cache.");
            Assert.AreEqual("Climb", session.GetFeatChoice(slot1, ""));
            Assert.AreEqual(climbBefore + 3, session.GetStatistic("Climb").Value, "SetFeatChoice must recalculate dependent statistics.");
            Assert.AreEqual(climbBefore + 3, snapAfter.GetStatistic("Climb").Value, "New snapshot must reflect updated feat choice.");
        }

        [TestMethod]
        public void SetFeatChoice_CompositeKeyChoice_InvalidatesCachedSnapshotsAndRecalculatesDependentStats() {
            var syntheticFeat = new Feat(new JObject(
                new JProperty("benefit", new JObject(
                    new JProperty("name", "Synthetic Weapon Focus"),
                    new JProperty("modifiers", new JArray(
                        new JObject(
                            new JProperty("affects", "attack bonus:$"),
                            new JProperty("mod", new JObject(
                                new JProperty("type", "untyped"),
                                new JProperty("amounts", new JArray(new JObject(new JProperty("fixed", 1))))
                            ))
                        )
                    ))
                )),
                new JProperty("choices", new JArray(
                    new JObject(
                        new JProperty("id", "weapon"),
                        new JProperty("label", "Choose weapon"),
                        new JProperty("domain", "Weapons")
                    )
                )),
                new JProperty("stats", new JArray(
                    new JObject(new JProperty("name", "attack bonus:Longsword"))
                ))
            ));

            var fakeCatalog = new FakeRulesCatalog(inner: catalog);
            fakeCatalog.FeatsMap[syntheticFeat.Benefit.Name] = syntheticFeat;

            var character = new Character { race = "human" };
            character.classLevels.Add("Fighter");
            var slot1 = ProgressionSlotKey.Advancement(1);
            character.feats.Add(new FeatAssignment(slot1, syntheticFeat.Benefit.Name));

            var session = new CharacterSession(character, fakeCatalog);
            var initialStat = session.GetStatistic("attack bonus:Longsword").Value;
            Assert.AreEqual(0, initialStat);

            var snapBefore = session.GetSessionAtLevel(1);

            session.SetFeatChoice(slot1, "weapon", "Longsword");

            var snapAfter = session.GetSessionAtLevel(1);
            Assert.AreNotSame(snapBefore, snapAfter, "SetFeatChoice with composite key must invalidate level snapshot cache.");
            Assert.AreEqual("Longsword", session.GetFeatChoice(slot1, "weapon"));
            Assert.AreEqual(1, session.GetStatistic("attack bonus:Longsword").Value, "SetFeatChoice with composite key must recalculate dependent statistics.");
            Assert.AreEqual(1, snapAfter.GetStatistic("attack bonus:Longsword").Value, "New snapshot must reflect updated composite feat choice.");
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

        [TestMethod]
        public void ICharacterSession_ExposesGetSubsystem_AndSubsystemsEnumeration() {
            var method = typeof(ICharacterSession).GetMethod("GetSubsystem", new[] { typeof(string) });
            Assert.IsNotNull(method, "ICharacterSession must expose GetSubsystem(string? id).");
            Assert.AreEqual(typeof(SubsystemEngine), method.ReturnType, "GetSubsystem must return SubsystemEngine.");

            var prop = typeof(ICharacterSession).GetProperty("Subsystems");
            Assert.IsNotNull(prop, "ICharacterSession must expose Subsystems property.");
            Assert.AreEqual(typeof(IEnumerable<SubsystemEngine>), prop.PropertyType, "ICharacterSession.Subsystems must be IEnumerable<SubsystemEngine>.");

            var contextMethod = typeof(ICharacterContext).GetMethod("GetSubsystem", new[] { typeof(string) });
            Assert.IsNotNull(contextMethod, "ICharacterContext must expose GetSubsystem(string? id).");
            Assert.AreEqual(typeof(SubsystemEngine), contextMethod.ReturnType, "ICharacterContext.GetSubsystem must return SubsystemEngine.");
        }

        [TestMethod]
        public void CharacterSession_GetSubsystem_RetrievesValidSubsystem_CaseInsensitively() {
            var character = new Character();
            var session = new CharacterSession(character, catalog);

            var pact1 = session.GetSubsystem("pact_magic");
            Assert.IsNotNull(pact1, "session.GetSubsystem('pact_magic') must return the engine.");
            Assert.AreEqual("pact_magic", pact1.Id);

            var pact2 = session.GetSubsystem("PACT_MAGIC");
            Assert.AreSame(pact1, pact2, "Subsystem retrieval must be case-insensitive.");

            var pact3 = session.GetSubsystem("Pact_Magic");
            Assert.AreSame(pact1, pact3, "Subsystem retrieval must be case-insensitive.");

            var martial = session.GetSubsystem("martial_maneuvers");
            Assert.IsNotNull(martial, "session.GetSubsystem('martial_maneuvers') must return the engine.");
            Assert.AreEqual("martial_maneuvers", martial.Id);
        }

        [TestMethod]
        public void CharacterSession_GetSubsystem_ReturnsNullForMissingNullOrEmpty() {
            var character = new Character();
            var session = new CharacterSession(character, catalog);

            Assert.IsNull(session.GetSubsystem(null), "GetSubsystem(null) must return null.");
            Assert.IsNull(session.GetSubsystem(""), "GetSubsystem(\"\") must return null.");
            Assert.IsNull(session.GetSubsystem("nonexistent_subsystem"), "GetSubsystem with missing key must return null.");
        }

        [TestMethod]
        public void CharacterSession_Subsystems_EnumeratesAllCatalogEngines() {
            var character = new Character();
            var session = new CharacterSession(character, catalog);
            ICharacterSession sessionInterface = session;

            var engines = sessionInterface.Subsystems.ToList();
            Assert.AreEqual(catalog.Subsystems.Count, engines.Count, "Subsystems must enumerate all declared catalog engines.");
            foreach (var kvp in catalog.Subsystems) {
                Assert.IsTrue(engines.Any(e => string.Equals(e.Id, kvp.Key, StringComparison.OrdinalIgnoreCase)),
                    $"Subsystems enumeration must contain engine for catalog subsystem '{kvp.Key}'.");
            }
        }

        [TestMethod]
        public void CharacterSession_SnapshotSlicing_TrimsAndEvaluatesSubsystems() {
            var character = new Character { race = "human" };
            for (int i = 0; i < 10; i++) character.classLevels.Add("Warblade");

            var warbladeState = new SubsystemState();
            warbladeState.Slots["maneuvers"] = new List<SubsystemSlot> {
                new SubsystemSlot("Steel Wind", "unreadied", "Warblade"),
                new SubsystemSlot("Steely Strike", "unreadied", "Warblade"),
                new SubsystemSlot("Disarming Strike", "unreadied", "Warblade"),
                new SubsystemSlot("Crusader's Strike", "unreadied", "Warblade")
            };
            character.Subsystems["martial_maneuvers"] = warbladeState;

            var session = new CharacterSession(character, catalog);

            var stanceDesc = catalog.GetFeat("Martial Stance").Choices.First(c => c.Id == "stance");
            var slot1 = ProgressionSlotKey.Advancement(1);
            var lvl1Warnings = session.ValidateProgressionChoice(new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Martial Spirit",
                slotKey: slot1.ToString(),
                targetSource: "Warblade",
                descriptor: stanceDesc,
                slot: slot1
            ));
            Assert.IsTrue(lvl1Warnings.Count > 0, "Level 1 snapshot must prune maneuvers beyond capacity so Devoted Spirit prereq is unmet.");

            var slot9 = ProgressionSlotKey.Advancement(9);
            var lvl9Warnings = session.ValidateProgressionChoice(new ProgressionChoiceContext(
                ProgressionChoiceDomain.Feat,
                "Martial Spirit",
                slotKey: slot9.ToString(),
                targetSource: "Warblade",
                descriptor: stanceDesc,
                slot: slot9
            ));
            Assert.AreEqual(0, lvl9Warnings.Count, "Level 9 snapshot retains Slot 3 so Devoted Spirit prereq is satisfied.");
        }

        [TestMethod]
        public void CharacterSession_ResolveSubsystemElementChoices_ResolvesViaDirectEngines() {
            var character = new Character { race = "human" };
            character.classLevels.Add("Warblade");
            var session = new CharacterSession(character, catalog);

            var desc = new ChoiceDescriptor("maneuver", "Choose Maneuver", "SubsystemElements", "maneuver");
            var slot1 = ProgressionSlotKey.Advancement(1);
            session.SetFeatChoice(slot1, "maneuver", "Steel Wind");

            var elem = session.ResolveFeatChoice(slot1, desc) as SubsystemElement;
            Assert.IsNotNull(elem, "ResolveFeatChoice must resolve element from direct subsystem engine.");
            Assert.AreEqual("Steel Wind", elem.Name);
        }

        private class RecalculateSpySession : CharacterSession {
            public int RecalculateCallCount { get; set; }

            public RecalculateSpySession(Character character, IRulesCatalog catalog)
                : base(character, catalog) {
                RecalculateCallCount = 0;
            }

            public override void Recalculate() {
                RecalculateCallCount++;
                base.Recalculate();
            }
        }

        [TestMethod]
        public void EquipmentMutations_EvictCachedSnapshotsInLevelSessionCache() {
            var character = new Character { race = "human" };
            character.classLevels.Add("Fighter");
            character.inventory["Pack"] = new InventoryLocation();

            var session = new CharacterSession(character, catalog);

            var sword = new WeaponChoice { Name = "longsword" };
            var dagger = new WeaponChoice { Name = "dagger" };
            var armor = new ArmorChoice { Name = "full plate" };
            var shield = new ArmorChoice { Name = "heavy steel shield", IsShield = true };

            session.Character.inventory["Pack"].weapons.Add(sword);
            session.Character.inventory["Pack"].weapons.Add(dagger);
            session.Character.inventory["Pack"].armor.Add(armor);
            session.Character.inventory["Pack"].shields.Add(shield);
            session.AddItem("Pack", "torc of displacement and Con +2", 1);
            session.AddItem("Pack", "panic button", 1);

            // 1. EquipWeapon
            var snap = session.GetSessionAtLevel(1);
            session.EquipWeapon("Pack", true, sword);
            var nextSnap = session.GetSessionAtLevel(1);
            Assert.AreNotSame(snap, nextSnap, "EquipWeapon must evict snapshot cache.");
            snap = nextSnap;

            // 2. DonArmor
            session.DonArmor("Pack", armor);
            nextSnap = session.GetSessionAtLevel(1);
            Assert.AreNotSame(snap, nextSnap, "DonArmor must evict snapshot cache.");
            snap = nextSnap;

            // 3. EquipShield
            session.EquipShield("Pack", shield);
            nextSnap = session.GetSessionAtLevel(1);
            Assert.AreNotSame(snap, nextSnap, "EquipShield must evict snapshot cache.");
            snap = nextSnap;

            // 4. EquipItem (slotted)
            session.EquipItem("Pack", Equipped.Slot.NECK, "torc of displacement and Con +2");
            nextSnap = session.GetSessionAtLevel(1);
            Assert.AreNotSame(snap, nextSnap, "EquipItem must evict snapshot cache.");
            snap = nextSnap;

            // 5. UnequipItem
            session.UnequipItem(Equipped.Slot.NECK, "Pack");
            nextSnap = session.GetSessionAtLevel(1);
            Assert.AreNotSame(snap, nextSnap, "UnequipItem must evict snapshot cache.");
            snap = nextSnap;

            // 6. EquipItem (slotless)
            session.EquipItem("Pack", Equipped.Slot.SLOTLESS, "panic button");
            nextSnap = session.GetSessionAtLevel(1);
            Assert.AreNotSame(snap, nextSnap, "EquipItem (slotless) must evict snapshot cache.");
            snap = nextSnap;

            // 7. UnequipSlotlessItem
            session.UnequipSlotlessItem("Pack", "panic button");
            nextSnap = session.GetSessionAtLevel(1);
            Assert.AreNotSame(snap, nextSnap, "UnequipSlotlessItem must evict snapshot cache.");
            snap = nextSnap;

            // 8. SwapWeapons (equip secondary weapon first)
            session.Character.inventory["Pack"].weapons.Add(dagger);
            session.EquipWeapon("Pack", false, dagger);
            snap = session.GetSessionAtLevel(1);
            session.SwapWeapons();
            nextSnap = session.GetSessionAtLevel(1);
            Assert.AreNotSame(snap, nextSnap, "SwapWeapons must evict snapshot cache.");
            snap = nextSnap;

            // Re-equip to test UnequipWeapon, UndonArmor, UnequipShield
            session.Character.inventory["Pack"].weapons.Add(sword);
            session.Character.inventory["Pack"].armor.Add(armor);
            session.Character.inventory["Pack"].shields.Add(shield);
            session.EquipWeapon("Pack", true, sword);
            session.DonArmor("Pack", armor);
            session.EquipShield("Pack", shield);
            snap = session.GetSessionAtLevel(1);

            // 12. UnequipWeapon
            session.UnequipWeapon("Pack", true);
            nextSnap = session.GetSessionAtLevel(1);
            Assert.AreNotSame(snap, nextSnap, "UnequipWeapon must evict snapshot cache.");
            snap = nextSnap;

            // 13. UndonArmor
            session.UndonArmor("Pack");
            nextSnap = session.GetSessionAtLevel(1);
            Assert.AreNotSame(snap, nextSnap, "UndonArmor must evict snapshot cache.");
            snap = nextSnap;

            // 14. UnequipShield
            session.UnequipShield("Pack");
            nextSnap = session.GetSessionAtLevel(1);
            Assert.AreNotSame(snap, nextSnap, "UnequipShield must evict snapshot cache.");
        }

        [TestMethod]
        public void CompoundEquipmentActions_TriggerRecalculateExactlyOnce() {
            var character = new Character { race = "human" };
            character.classLevels.Add("Fighter");
            character.inventory["Pack"] = new InventoryLocation();

            var spy = new RecalculateSpySession(character, catalog);

            var sword1 = new WeaponChoice { Name = "longsword" };
            var sword2 = new WeaponChoice { Name = "greatsword" };
            var offHandDagger = new WeaponChoice { Name = "dagger" };
            var armor1 = new ArmorChoice { Name = "full plate" };
            var armor2 = new ArmorChoice { Name = "breastplate" };
            var heavyShield = new ArmorChoice { Name = "heavy steel shield", IsShield = true };

            spy.Character.inventory["Pack"].weapons.Add(sword1);
            spy.Character.inventory["Pack"].weapons.Add(sword2);
            spy.Character.inventory["Pack"].weapons.Add(offHandDagger);
            spy.Character.inventory["Pack"].armor.Add(armor1);
            spy.Character.inventory["Pack"].armor.Add(armor2);
            spy.Character.inventory["Pack"].shields.Add(heavyShield);

            // Equip sword1 into primary hand
            spy.EquipWeapon("Pack", true, sword1);

            // 1. EquipWeapon displacing existing primary weapon
            spy.RecalculateCallCount = 0;
            spy.EquipWeapon("Pack", true, sword2);
            Assert.AreEqual(1, spy.RecalculateCallCount, "EquipWeapon replacing existing weapon must recalculate exactly once.");
            Assert.AreEqual(sword2, spy.Character.equipped.PrimaryHandWeapon);
            Assert.IsTrue(spy.Character.inventory["Pack"].weapons.Contains(sword1));

            // Equip heavy shield in off-hand
            spy.EquipShield("Pack", heavyShield);

            // 2. EquipWeapon (off-hand) displacing a non-buckler shield
            spy.RecalculateCallCount = 0;
            spy.EquipWeapon("Pack", false, offHandDagger);
            Assert.AreEqual(1, spy.RecalculateCallCount, "EquipWeapon in off-hand displacing shield must recalculate exactly once.");
            Assert.AreEqual(offHandDagger, spy.Character.equipped.OffHandWeapon);
            Assert.IsNull(spy.Character.equipped.OffHandShield);
            Assert.IsTrue(spy.Character.inventory["Pack"].shields.Contains(heavyShield));

            // 3. EquipShield displacing existing off-hand weapon
            spy.RecalculateCallCount = 0;
            spy.EquipShield("Pack", heavyShield);
            Assert.AreEqual(1, spy.RecalculateCallCount, "EquipShield displacing off-hand weapon must recalculate exactly once.");
            Assert.AreEqual(heavyShield, spy.Character.equipped.OffHandShield);
            Assert.IsNull(spy.Character.equipped.OffHandWeapon);
            Assert.IsTrue(spy.Character.inventory["Pack"].weapons.Contains(offHandDagger));

            // Don armor1
            spy.DonArmor("Pack", armor1);

            // 4. DonArmor displacing existing donned armor
            spy.RecalculateCallCount = 0;
            spy.DonArmor("Pack", armor2);
            Assert.AreEqual(1, spy.RecalculateCallCount, "DonArmor displacing existing armor must recalculate exactly once.");
            Assert.AreEqual(armor2, spy.Character.equipped.DonnedArmor);
            Assert.IsTrue(spy.Character.inventory["Pack"].armor.Contains(armor1));

            // 5. UnequipWeapon
            spy.RecalculateCallCount = 0;
            spy.UnequipWeapon("Pack", true);
            Assert.AreEqual(1, spy.RecalculateCallCount, "UnequipWeapon must recalculate exactly once.");
            Assert.IsNull(spy.Character.equipped.PrimaryHandWeapon);

            // 6. UndonArmor
            spy.RecalculateCallCount = 0;
            spy.UndonArmor("Pack");
            Assert.AreEqual(1, spy.RecalculateCallCount, "UndonArmor must recalculate exactly once.");
            Assert.IsNull(spy.Character.equipped.DonnedArmor);

            // 7. UnequipShield
            spy.RecalculateCallCount = 0;
            spy.UnequipShield("Pack");
            Assert.AreEqual(1, spy.RecalculateCallCount, "UnequipShield must recalculate exactly once.");
            Assert.IsNull(spy.Character.equipped.OffHandShield);
        }

        [TestMethod]
        public void InvalidEquipmentOperations_ReturnEarlyWithoutInvalidatingCacheOrRecalculating() {
            var character = new Character { race = "human" };
            character.classLevels.Add("Fighter");
            character.inventory["Pack"] = new InventoryLocation();

            var spy = new RecalculateSpySession(character, catalog);

            var initialSnap = spy.GetSessionAtLevel(1);
            spy.RecalculateCallCount = 0;

            // 1. UnequipWeapon when empty or invalid location
            spy.UnequipWeapon("Pack", true);
            Assert.AreEqual(0, spy.RecalculateCallCount, "UnequipWeapon on empty slot must not recalculate.");
            Assert.AreSame(initialSnap, spy.GetSessionAtLevel(1));

            spy.UnequipWeapon("NonExistent", true);
            Assert.AreEqual(0, spy.RecalculateCallCount, "UnequipWeapon with invalid location must not recalculate.");
            Assert.AreSame(initialSnap, spy.GetSessionAtLevel(1));

            // 5. UndonArmor when empty or invalid location
            spy.UndonArmor("Pack");
            Assert.AreEqual(0, spy.RecalculateCallCount, "UndonArmor on empty slot must not recalculate.");
            Assert.AreSame(initialSnap, spy.GetSessionAtLevel(1));

            spy.UndonArmor("NonExistent");
            Assert.AreEqual(0, spy.RecalculateCallCount, "UndonArmor with invalid location must not recalculate.");
            Assert.AreSame(initialSnap, spy.GetSessionAtLevel(1));

            // 6. UnequipShield when empty or invalid location
            spy.UnequipShield("Pack");
            Assert.AreEqual(0, spy.RecalculateCallCount, "UnequipShield on empty slot must not recalculate.");
            Assert.AreSame(initialSnap, spy.GetSessionAtLevel(1));

            spy.UnequipShield("NonExistent");
            Assert.AreEqual(0, spy.RecalculateCallCount, "UnequipShield with invalid location must not recalculate.");
            Assert.AreSame(initialSnap, spy.GetSessionAtLevel(1));

            // 7. UnequipItem when empty or invalid location
            spy.UnequipItem(Equipped.Slot.NECK, "Pack");
            Assert.AreEqual(0, spy.RecalculateCallCount, "UnequipItem on empty slot must not recalculate.");
            Assert.AreSame(initialSnap, spy.GetSessionAtLevel(1));

            spy.UnequipItem(Equipped.Slot.NECK, "NonExistent");
            Assert.AreEqual(0, spy.RecalculateCallCount, "UnequipItem with invalid location must not recalculate.");
            Assert.AreSame(initialSnap, spy.GetSessionAtLevel(1));

            // 8. UnequipSlotlessItem when not equipped or invalid location
            spy.UnequipSlotlessItem("Pack", "panic button");
            Assert.AreEqual(0, spy.RecalculateCallCount, "UnequipSlotlessItem when not equipped must not recalculate.");
            Assert.AreSame(initialSnap, spy.GetSessionAtLevel(1));

            spy.UnequipSlotlessItem("NonExistent", "panic button");
            Assert.AreEqual(0, spy.RecalculateCallCount, "UnequipSlotlessItem with invalid location must not recalculate.");
            Assert.AreSame(initialSnap, spy.GetSessionAtLevel(1));

            // 9. EquipWeapon with missing weapon or invalid location
            spy.EquipWeapon("Pack", true, new WeaponChoice { Name = "longsword" });
            Assert.AreEqual(0, spy.RecalculateCallCount, "EquipWeapon with unpossessed weapon must not recalculate.");
            Assert.AreSame(initialSnap, spy.GetSessionAtLevel(1));

            spy.EquipWeapon("NonExistent", true, new WeaponChoice { Name = "longsword" });
            Assert.AreEqual(0, spy.RecalculateCallCount, "EquipWeapon with invalid location must not recalculate.");
            Assert.AreSame(initialSnap, spy.GetSessionAtLevel(1));

            // 10. DonArmor with missing armor or invalid location
            spy.DonArmor("Pack", new ArmorChoice { Name = "full plate" });
            Assert.AreEqual(0, spy.RecalculateCallCount, "DonArmor with unpossessed armor must not recalculate.");
            Assert.AreSame(initialSnap, spy.GetSessionAtLevel(1));

            spy.DonArmor("NonExistent", new ArmorChoice { Name = "full plate" });
            Assert.AreEqual(0, spy.RecalculateCallCount, "DonArmor with invalid location must not recalculate.");
            Assert.AreSame(initialSnap, spy.GetSessionAtLevel(1));

            // 11. EquipShield with missing shield or invalid location
            spy.EquipShield("Pack", new ArmorChoice { Name = "heavy steel shield", IsShield = true });
            Assert.AreEqual(0, spy.RecalculateCallCount, "EquipShield with unpossessed shield must not recalculate.");
            Assert.AreSame(initialSnap, spy.GetSessionAtLevel(1));

            spy.EquipShield("NonExistent", new ArmorChoice { Name = "heavy steel shield", IsShield = true });
            Assert.AreEqual(0, spy.RecalculateCallCount, "EquipShield with invalid location must not recalculate.");
            Assert.AreSame(initialSnap, spy.GetSessionAtLevel(1));

            // 12. EquipItem with missing item, invalid location, or mismatched slot
            spy.EquipItem("Pack", Equipped.Slot.NECK, "torc of displacement and Con +2");
            Assert.AreEqual(0, spy.RecalculateCallCount, "EquipItem with unpossessed item must not recalculate.");
            Assert.AreSame(initialSnap, spy.GetSessionAtLevel(1));

            spy.EquipItem("NonExistent", Equipped.Slot.NECK, "torc of displacement and Con +2");
            Assert.AreEqual(0, spy.RecalculateCallCount, "EquipItem with invalid location must not recalculate.");
            Assert.AreSame(initialSnap, spy.GetSessionAtLevel(1));

            spy.AddItem("Pack", "torc of displacement and Con +2", 1);
            spy.EquipItem("Pack", Equipped.Slot.FEET, "torc of displacement and Con +2"); // Wrong slot
            Assert.AreEqual(0, spy.RecalculateCallCount, "EquipItem with mismatched slot must not recalculate.");
            Assert.AreSame(initialSnap, spy.GetSessionAtLevel(1));

            // 13. SwapWeapons when both hands empty
            spy.SwapWeapons();
            Assert.AreEqual(0, spy.RecalculateCallCount, "SwapWeapons when both hands empty must not recalculate.");
            Assert.AreSame(initialSnap, spy.GetSessionAtLevel(1));
        }

        [TestMethod]
        public void CombatTimelineOperations_ExecuteRecalculation_WithoutEvictingLevelSessionCache() {
            var character = new Character { race = "human" };
            character.classLevels.Add("Fighter");

            var spy = new RecalculateSpySession(character, catalog);

            var snap = spy.GetSessionAtLevel(1);

            // 1. AdvanceTimeline
            spy.RecalculateCallCount = 0;
            spy.AdvanceTimeline(1);
            Assert.AreEqual(1, spy.RecalculateCallCount, "AdvanceTimeline must recalculate.");
            Assert.AreSame(snap, spy.GetSessionAtLevel(1), "AdvanceTimeline must not evict level session cache.");

            // 2. SetTime
            spy.RecalculateCallCount = 0;
            spy.SetTime(DateTime.UtcNow.AddMinutes(5));
            Assert.AreEqual(1, spy.RecalculateCallCount, "SetTime must recalculate.");
            Assert.AreSame(snap, spy.GetSessionAtLevel(1), "SetTime must not evict level session cache.");

            // 3. StartCombat
            spy.RecalculateCallCount = 0;
            spy.StartCombat();
            Assert.AreEqual(1, spy.RecalculateCallCount, "StartCombat must recalculate.");
            Assert.AreSame(snap, spy.GetSessionAtLevel(1), "StartCombat must not evict level session cache.");

            // 4. EndCombat
            spy.RecalculateCallCount = 0;
            spy.EndCombat();
            Assert.AreEqual(1, spy.RecalculateCallCount, "EndCombat must recalculate.");
            Assert.AreSame(snap, spy.GetSessionAtLevel(1), "EndCombat must not evict level session cache.");
        }
    }
}
