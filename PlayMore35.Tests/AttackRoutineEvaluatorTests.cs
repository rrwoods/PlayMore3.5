using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlayMore35.CharacterChoices;
using PlayMore35.Global;
using PlayMore35.Items;
using PlayMore35.Rules;

namespace PlayMore35.Tests {
    [TestClass]
    public class AttackRoutineEvaluatorTests {
        private CharacterSession CreateSessionWithFighter(int fighterLevel, int strScore = 14, IEnumerable<string> feats = null) {
            var character = new Character();
            character.classLevels.Clear();
            for (int i = 0; i < fighterLevel; i++) {
                character.classLevels.Add("Fighter");
            }
            character.assignedAbilityScores["STR"] = strScore;
            character.assignedAbilityScores["DEX"] = 14;
            character.assignedAbilityScores["CON"] = 10;
            character.assignedAbilityScores["INT"] = 10;
            character.assignedAbilityScores["WIS"] = 10;
            character.assignedAbilityScores["CHA"] = 10;

            if (feats != null) {
                var slots = new[] {
                    ProgressionSlotKey.Advancement(1),
                    ProgressionSlotKey.Class("Fighter", 1),
                    ProgressionSlotKey.Class("Fighter", 2),
                    ProgressionSlotKey.Advancement(3),
                    ProgressionSlotKey.Class("Fighter", 4),
                    ProgressionSlotKey.Advancement(6),
                    ProgressionSlotKey.Class("Fighter", 6),
                };
                int index = 0;
                foreach (var feat in feats) {
                    character.feats.Add(new FeatAssignment(slots[index], feat));
                    index++;
                }
            }

            var session = new CharacterSession(character, new GameRulesCatalog());
            return session;
        }

        // Source: Player's Handbook 3.5, p. 143
        [TestMethod]
        public void EvaluateRoutines_SoloPrimaryWeapon_GeneratesStandardAndIterativeFullAttacks() {
            var session = CreateSessionWithFighter(11, strScore: 16); // BAB +11/+6/+1, STR +3
            session.Mutate(c => c.equipped.PrimaryHandWeapon = new WeaponChoice { Name = "greatsword" });

            var routines = session.GetAllRoutines();

            // Verify Standard attack routine
            var standard = routines.FirstOrDefault(r => r.ActionType == RoutineActionType.Standard && r.Category == RoutineCategory.PrimarySolo);
            Assert.IsNotNull(standard, "Standard primary solo routine must exist");
            Assert.AreEqual(1, standard.Attacks.Count);
            // +11 BAB + 3 STR = +14
            Assert.AreEqual(14, standard.Attacks[0].AttackBonus.bonus);
            // 2d6 + 4 (1.5x STR = 4)
            Assert.AreEqual("2d6+4", standard.Attacks[0].BaseDamage.ToString());

            // Verify Full Attack routine
            var full = routines.FirstOrDefault(r => r.ActionType == RoutineActionType.FullAttack && r.Category == RoutineCategory.PrimarySolo);
            Assert.IsNotNull(full, "Full attack primary solo routine must exist");
            Assert.AreEqual(3, full.Attacks.Count, "Fighter 11 must have 3 iterative attacks");
            Assert.AreEqual(14, full.Attacks[0].AttackBonus.bonus); // +14
            Assert.AreEqual(9, full.Attacks[1].AttackBonus.bonus);  // +9 (+14 - 5)
            Assert.AreEqual(4, full.Attacks[2].AttackBonus.bonus);  // +4 (+14 - 10)

            // All attacks in full attack deal 1.5x STR
            Assert.AreEqual("2d6+4", full.Attacks[0].BaseDamage.ToString());
            Assert.AreEqual("2d6+4", full.Attacks[1].BaseDamage.ToString());
            Assert.AreEqual("2d6+4", full.Attacks[2].BaseDamage.ToString());
        }

        // Source: Player's Handbook 3.5, p. 143, p. 160
        [TestMethod]
        public void EvaluateRoutines_TwoWeaponFighting_SequencesProgressionTiers_WithTwfPenalties() {
            var session = CreateSessionWithFighter(6, strScore: 14, feats: new[] { "Two-Weapon Fighting", "Improved Two-Weapon Fighting" });
            session.Mutate(c => {
                c.equipped.PrimaryHandWeapon = new WeaponChoice { Name = "longsword" };
                c.equipped.OffHandWeapon = new WeaponChoice { Name = "short sword" };
            });

            var routines = session.GetAllRoutines();

            var twfRoutine = routines.FirstOrDefault(r => r.ActionType == RoutineActionType.FullAttack && r.Category == RoutineCategory.TwoWeaponFighting);
            Assert.IsNotNull(twfRoutine, "TWF full attack routine must exist");
            Assert.AreEqual(4, twfRoutine.Attacks.Count, "Fighter 6 with ITWF and light offhand must have 4 attacks (2 primary, 2 offhand)");

            // BAB +6, STR +2, TWF penalty -2 (light offhand + TWF feat) -> Net bonus: +6 + 2 - 2 = +6
            // Tier 1: Primary 1st (+6), Off-Hand 1st (+6)
            // Tier 2: Primary 2nd (+1), Off-Hand 2nd (+1)
            Assert.AreEqual("longsword", twfRoutine.Attacks[0].WeaponName);
            Assert.AreEqual(6, twfRoutine.Attacks[0].AttackBonus.bonus, string.Join("; ", twfRoutine.Attacks[0].AttackBonus.explanation));

            Assert.AreEqual("short sword", twfRoutine.Attacks[1].WeaponName);
            Assert.AreEqual(6, twfRoutine.Attacks[1].AttackBonus.bonus);

            Assert.AreEqual("longsword", twfRoutine.Attacks[2].WeaponName);
            Assert.AreEqual(1, twfRoutine.Attacks[2].AttackBonus.bonus);

            Assert.AreEqual("short sword", twfRoutine.Attacks[3].WeaponName);
            Assert.AreEqual(1, twfRoutine.Attacks[3].AttackBonus.bonus);

            // Primary gets full STR (+2), Off-hand gets half STR (+1)
            Assert.AreEqual("1d8+2", twfRoutine.Attacks[0].BaseDamage.ToString());
            Assert.AreEqual("1d6+1", twfRoutine.Attacks[1].BaseDamage.ToString());
        }

        // Source: Player's Handbook 3.5, p. 118, p. 160
        [TestMethod]
        public void EvaluateRoutines_DoubleWeapon_GeneratesTwoHandedSingleAndTwfRoutines() {
            var session = CreateSessionWithFighter(1, strScore: 14);
            session.Mutate(c => c.equipped.PrimaryHandWeapon = new WeaponChoice { Name = "orc double axe" });

            var routines = session.GetAllRoutines();

            // Double weapon single attack routine (two-handed)
            var standard = routines.FirstOrDefault(r => r.ActionType == RoutineActionType.Standard && r.Category == RoutineCategory.DoubleWeapon);
            Assert.IsNotNull(standard, "Double weapon standard routine must exist");
            Assert.AreEqual(1, standard.Attacks.Count);
            Assert.AreEqual("1d8+3", standard.Attacks[0].BaseDamage.ToString(), "Two-handed gets 1.5x STR (+3)");

            // Double weapon TWF routine
            var twf = routines.FirstOrDefault(r => r.ActionType == RoutineActionType.FullAttack && r.Category == RoutineCategory.DoubleWeapon);
            Assert.IsNotNull(twf, "Double weapon TWF full attack routine must exist");
            Assert.AreEqual(2, twf.Attacks.Count, "Level 1 fighter double weapon TWF has 2 attacks");
            // Head 1 primary, Head 2 off-hand
            Assert.AreEqual("orc double axe", twf.Attacks[0].WeaponName);
            Assert.AreEqual("orc double axe", twf.Attacks[1].WeaponName);
            Assert.AreEqual("1d8+2", twf.Attacks[0].BaseDamage.ToString(), "Primary head gets full STR");
            Assert.AreEqual("1d8+1", twf.Attacks[1].BaseDamage.ToString(), "Second head treated as light gets half STR");
        }

        // Source: Player's Handbook 3.5, p. 124, p. 125
        [TestMethod]
        public void EvaluateRoutines_SoloArmorSpikesAndSoloShieldBash_GenerateValidStandaloneRoutines() {
            var session = CreateSessionWithFighter(1, strScore: 14);
            session.Mutate(c => {
                c.equipped.DonnedArmor = new ArmorChoice { Name = "full plate", Spiked = true };
                c.equipped.OffHandShield = new ArmorChoice { Name = "heavy steel shield", IsShield = true };
            });

            var routines = session.GetAllRoutines();

            var spikesSolo = routines.FirstOrDefault(r => r.Category == RoutineCategory.ArmorSpikesSolo);
            Assert.IsNotNull(spikesSolo, "Solo armor spikes routine must exist");
            Assert.AreEqual("armor spikes", spikesSolo.Attacks[0].WeaponName);

            var shieldSolo = routines.FirstOrDefault(r => r.Category == RoutineCategory.ShieldBashSolo);
            Assert.IsNotNull(shieldSolo, "Solo shield bash routine must exist under RoutineCategory.ShieldBashSolo");
            Assert.AreEqual("shield bash", shieldSolo.Attacks[0].WeaponName);
        }

        // Source: Player's Handbook 3.5, p. 160
        [TestMethod]
        public void EvaluateRoutines_MultiWeaponSecondaryCombinations_CategorizedCorrectly() {
            var session = CreateSessionWithFighter(1, strScore: 14);
            session.Mutate(c => {
                c.equipped.PrimaryHandWeapon = new WeaponChoice { Name = "longsword" };
                c.equipped.DonnedArmor = new ArmorChoice { Name = "full plate", Spiked = true };
                c.equipped.OffHandShield = new ArmorChoice { Name = "heavy steel shield", IsShield = true };
            });

            var routines = session.GetAllRoutines();

            var shieldTwf = routines.FirstOrDefault(r => r.Category == RoutineCategory.OffHandShieldBash);
            Assert.IsNotNull(shieldTwf, "Off-hand shield bash TWF routine must exist");

            var spikesTwf = routines.FirstOrDefault(r => r.Category == RoutineCategory.ArmorSpikesTWF);
            Assert.IsNotNull(spikesTwf, "Armor spikes TWF routine must exist");
        }

        // Source: Player's Handbook 3.5, p. 143, p. 160
        [TestMethod]
        public void EvaluateRoutines_MorningstarAndDagger_GeneratesMeleeAndThrownFullAttackRoutine() {
            var session = CreateSessionWithFighter(1, strScore: 14);
            session.Mutate(c => {
                c.equipped.PrimaryHandWeapon = new WeaponChoice { Name = "morningstar" };
                c.equipped.OffHandWeapon = new WeaponChoice { Name = "dagger" };
            });

            var visibleRoutines = session.GetVisibleRoutines();

            var meleeAndMelee = visibleRoutines.FirstOrDefault(r =>
                r.ActionType == RoutineActionType.FullAttack &&
                r.Category == RoutineCategory.TwoWeaponFighting &&
                r.Attacks.Count == 2 &&
                r.Attacks[0].WeaponName == "morningstar" &&
                r.Attacks[0].Range == Weapon.Range.MELEE &&
                r.Attacks[1].WeaponName == "dagger" &&
                r.Attacks[1].Range == Weapon.Range.MELEE
            );
            Assert.IsNotNull(meleeAndMelee, "Expected a visible Full Attack TWF routine with melee morningstar and melee dagger");

            var meleeAndThrown = visibleRoutines.FirstOrDefault(r =>
                r.ActionType == RoutineActionType.FullAttack &&
                r.Category == RoutineCategory.TwoWeaponFighting &&
                r.Attacks.Count == 2 &&
                r.Attacks[0].WeaponName == "morningstar" &&
                r.Attacks[0].Range == Weapon.Range.MELEE &&
                r.Attacks[1].WeaponName == "dagger" &&
                r.Attacks[1].Range == Weapon.Range.THROWN
            );
            Assert.IsNotNull(meleeAndThrown, "Expected a visible Full Attack TWF routine with melee morningstar and thrown dagger");
            Assert.AreEqual(10, meleeAndThrown.Attacks[1].RangeIncrement);
        }

        // Source: Implied Rule: One-Handed Weapons in Two Hands (see docs/rules/mechanics/weapons.md#2-weapon-encumbrance--handling)
        // Also: Player's Handbook 3.5, p. 113, p. 134
        [TestMethod]
        public void EvaluateRoutines_SoloOneHandedWeapon_CountsAsTwoHanded_WithOneAndHalfStrAndEffectiveTwoHandedEffort() {
            var session = CreateSessionWithFighter(1, strScore: 14); // STR 14 (+2 mod)
            session.Mutate(c => c.equipped.PrimaryHandWeapon = new WeaponChoice { Name = "morningstar" });

            var routines = session.GetAllRoutines();
            var standard = routines.FirstOrDefault(r => r.ActionType == RoutineActionType.Standard && r.Category == RoutineCategory.PrimarySolo);
            Assert.IsNotNull(standard, "Solo one-handed weapon must generate standard PrimarySolo routine");
            Assert.AreEqual(1, standard.Attacks.Count);

            var soloAttack = standard.Attacks[0];
            Assert.AreEqual(Weapon.Hand.TWO_HANDED, soloAttack.Hand, "Empty off-hand should wield one-handed weapon with two hands");
            Assert.AreEqual(Weapon.Effort.TWO_HANDED, soloAttack.Effort, "Wielding with two hands promotes effective effort to TWO_HANDED");
            Assert.AreEqual("1d8+3", soloAttack.BaseDamage.ToString(), "Two-handed wielding gets 1.5x STR bonus (+3)");

            // Equipping a shield reverts morningstar to one-handed grip and effort
            session.Mutate(c => c.equipped.OffHandShield = new ArmorChoice { Name = "heavy wooden shield", IsShield = true });
            var shieldRoutines = session.GetAllRoutines();
            var shieldStandard = shieldRoutines.FirstOrDefault(r => r.ActionType == RoutineActionType.Standard && r.Category == RoutineCategory.PrimarySolo);
            Assert.IsNotNull(shieldStandard);

            var shieldAttack = shieldStandard.Attacks[0];
            Assert.AreEqual(Weapon.Hand.ONE_HANDED, shieldAttack.Hand, "Shield equipped should force primary weapon to ONE_HANDED");
            Assert.AreEqual(Weapon.Effort.ONE_HANDED, shieldAttack.Effort, "Shield equipped keeps effort as ONE_HANDED");
            Assert.AreEqual("1d8+2", shieldAttack.BaseDamage.ToString(), "One-handed wielding gets 1.0x STR bonus (+2)");
        }

        // Source: Implied Rule: One-Handed Weapons in Two Hands (see docs/rules/mechanics/weapons.md#2-weapon-encumbrance--handling)
        // Also: Player's Handbook 3.5, p. 113, p. 134
        [TestMethod]
        public void EvaluateRoutines_OdricSession_UnequipShield_PromotesMorningstarToTwoHanded() {
            string odricPath = TestEnvironment.FindOdricJsonPath();
            string jsonText = System.IO.File.ReadAllText(odricPath);
            var odric = new Character(Newtonsoft.Json.Linq.JObject.Parse(jsonText));
            var session = new CharacterSession(odric, new GameRulesCatalog());

            // Odric starts with morningstar and heavy wooden shield equipped
            var startRoutines = session.GetAllRoutines();
            var startStandard = startRoutines.FirstOrDefault(r => r.ActionType == RoutineActionType.Standard && r.Category == RoutineCategory.PrimarySolo);
            Assert.IsNotNull(startStandard);
            var startAtk = startStandard.Attacks[0];
            Assert.AreEqual(Weapon.Hand.ONE_HANDED, startAtk.Hand);
            Assert.AreEqual(Weapon.Effort.ONE_HANDED, startAtk.Effort);
            Assert.AreEqual("1d8+2", startAtk.BaseDamage.ToString());

            // Unequipping shield allows Odric to two-hand his morningstar
            session.Mutate(c => c.equipped.OffHandShield = null);
            var twoHandRoutines = session.GetAllRoutines();
            var twoHandStandard = twoHandRoutines.FirstOrDefault(r => r.ActionType == RoutineActionType.Standard && r.Category == RoutineCategory.PrimarySolo);
            Assert.IsNotNull(twoHandStandard);
            var twoHandAtk = twoHandStandard.Attacks[0];
            Assert.AreEqual(Weapon.Hand.TWO_HANDED, twoHandAtk.Hand);
            Assert.AreEqual(Weapon.Effort.TWO_HANDED, twoHandAtk.Effort);
            Assert.AreEqual("1d8+3", twoHandAtk.BaseDamage.ToString());
        }
    }
}
