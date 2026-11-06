using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlayMore35.Effects;
using PlayMore35.Items;

namespace PlayMore35.Tests {
    [TestClass]
    public class AtomicAttackTests {
        // Source: Player's Handbook 3.5, p. 140
        [TestMethod]
        public void Attack_RepresentsAtomicSingleAttack_WithoutIterativeList() {
            var attackType = typeof(Attack);
            var property = attackType.GetProperty("ExtraAttacks");
            Assert.IsNull(property, "Attack must not contain legacy ExtraAttacks list");
        }

        // Source: Player's Handbook 3.5, p. 134, p. 140
        [TestMethod]
        public void Attack_ComponentizesDamage_IntoBaseAndBonusDice() {
            var baseDice = new DiceGroup(1, 8).AddDice(4, 1); // 1d8 + 4
            var flaming = new DiceGroup("Flaming").AddDice(1, 6); // 1d6 fire

            var attack = TestFactory.CreateAttack(
                name: "Longsword",
                bonus: 5,
                baseDamage: baseDice,
                bonusDice: new List<DiceGroup> { flaming },
                criticalMultiplier: 2,
                threatRange: 19
            );

            Assert.AreEqual("1d8+4", attack.BaseDamage.ToString());
            Assert.AreEqual(1, attack.BonusDice.Count);
            Assert.AreEqual("1d6", attack.BonusDice[0].ToString());
            Assert.AreEqual("1d8+1d6+4", attack.TotalDamage.ToString());
        }

        // Source: Player's Handbook 3.5, p. 140
        [TestMethod]
        public void Attack_CriticalHits_MultiplyBaseDamageOnly_LeavingBonusDiceUnmultiplied() {
            var baseDice = new DiceGroup(1, 8).AddDice(4, 1); // 1d8 + 4
            var flaming = new DiceGroup("Flaming").AddDice(1, 6); // 1d6 fire

            var attack = TestFactory.CreateAttack(
                name: "Longsword",
                bonus: 5,
                baseDamage: baseDice,
                bonusDice: new List<DiceGroup> { flaming },
                criticalMultiplier: 2,
                threatRange: 19
            );

            // Base damage multiplies (1d8+4) * 2 = 2d8+8
            Assert.AreEqual("2d8+8", attack.CriticalBaseDamage.ToString());
            // Bonus dice remain unmultiplied
            Assert.AreEqual("2d8+1d6+8", attack.CriticalTotalDamage.ToString());
        }

        // Source: Player's Handbook 3.5, p. 140; Dungeon Master's Guide 3.5, p. 224
        [TestMethod]
        public void Attack_CriticalBonusDice_IncludesBurstDice_OnCriticalDamage() {
            var baseDice = new DiceGroup(1, 8).AddDice(4, 1); // 1d8 + 4
            var flaming = new DiceGroup("Flaming").AddDice(1, 6); // 1d6 fire
            var burst = new DiceGroup("Flaming Burst").AddDice(1, 10); // 1d10 burst

            var attack = TestFactory.CreateAttack(
                name: "Flaming Burst Longsword",
                bonus: 5,
                baseDamage: baseDice,
                bonusDice: new List<DiceGroup> { flaming },
                criticalBonusDice: new List<DiceGroup> { flaming, burst },
                criticalMultiplier: 2,
                threatRange: 19
            );

            Assert.AreEqual("2d8+8", attack.CriticalBaseDamage.ToString());
            Assert.AreEqual(2, attack.CriticalBonusDice.Count);
            Assert.AreEqual("1d10+2d8+1d6+8", attack.CriticalTotalDamage.ToString());
        }

        // Source: Player's Handbook 3.5, p. 143
        [TestMethod]
        public void AttackRoutine_InstantiatesWithStructuredMetadata() {
            var attack = TestFactory.CreateAttack(
                name: "Longsword",
                bonus: 7,
                baseDamage: new DiceGroup(1, 8).AddDice(3, 1),
                bonusDice: new List<DiceGroup>(),
                criticalMultiplier: 2,
                threatRange: 19
            );

            var routine = new AttackRoutine(
                RoutineActionType.Standard,
                RoutineCategory.PrimarySolo,
                "Longsword Standard Attack",
                weapons: Enumerable.Empty<Weapon>(),
                attacks: new[] { attack }
            );

            Assert.AreEqual(RoutineActionType.Standard, routine.ActionType);
            Assert.AreEqual(RoutineCategory.PrimarySolo, routine.Category);
            Assert.AreEqual("Longsword Standard Attack", routine.DisplayName);
            Assert.AreEqual(1, routine.Attacks.Count);
            Assert.AreSame(attack, routine.Attacks[0]);
        }
    }
}
