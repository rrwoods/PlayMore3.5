using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlayMore35.Effects;
using PlayMore35.GameRules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static PlayMore35.GameRules.Constants;

namespace PlayMore35.Effects.Tests {
    [TestClass()]
    public class EffectPoolTests {
        private GameRules.GameRules Rules() {
            GameRules.GameRules rules = new GameRules.GameRules();
            EffectPool p = rules.Rules;

            // make all the ability modifiers zero
            p.SetValue(10, STR_SCORE);
            p.SetValue(10, DEX_SCORE);
            p.SetValue(10, CON_SCORE);
            p.SetValue(10, INT_SCORE);
            p.SetValue(10, WIS_SCORE);
            p.SetValue(10, CHA_SCORE);

            // speed 30 to make sure jump isn't affected
            p.SetValue(30, LAND_SPEED);

            // 0 HP causes some weirdness too
            p.SetValue(1, MAX_HP);

            return rules;
        }

        private void CheckStat(int expected, GameRules.GameRules r, EffectPool p, string stat) {
            Assert.AreEqual(expected, r.Stats()[stat].GetValue(p).value);
        }

        [TestMethod()]
        public void TestComputeAbilityModifier() {
            GameRules.GameRules r = Rules();
            EffectPool p = r.Rules;

            p.SetValue(7, STR_SCORE);
            CheckStat(-2, r, p, STR_MODIFIER);
            CheckStat(0, r, p, DEX_MODIFIER);
        }

        [TestMethod()]
        public void TestConditionalEffects() {
            GameRules.GameRules r = Rules();
            EffectPool p = r.Rules;

            p.SetValue(5, TUMBLE + RANKS);
            CheckStat(+2, r, p, JUMP);
            CheckStat(+5, r, p, TUMBLE);
        }

        [TestMethod()]
        public void TestInitiativeChaining() {
            GameRules.GameRules r = Rules();
            EffectPool p = r.Rules;
            p.AddEffect(new Effect("somethin").AddModifier(ABILITY_CHECKS, new Modifier(4, UNTYPED)));
            CheckStat(+4, r, p, INITIATIVE);
        }

        [TestMethod()]
        public void TestAC() {
            GameRules.GameRules r = Rules();
            EffectPool p = r.Rules;

            p.SetValue(12, DEX_SCORE);
            p.AddEffect(new Effect("ring").AddModifier(AC, new Modifier(2, DEFLECTION)));
            p.AddEffect(new Effect("dragon scales").AddModifier(NATURAL_ARMOR, new Modifier(4, RACIAL)));

            CheckStat(17, r, p, AC);
            CheckStat(13, r, p, TOUCH_AC);
        }

        [TestMethod()]
        public void TestFlatFootedAC() {
            GameRules.GameRules r = Rules();
            EffectPool p = r.Rules;

            p.SetValue(12, DEX_SCORE);
            CheckStat(11, r, p, AC);
            p.AddEffect(new Effect("ff").SetFlag(FLAT_FOOTED));
            CheckStat(10, r, p, AC);
            p.AddEffect(new Effect("ud").SetFlag(UNCANNY_DODGE));
            CheckStat(11, r, p, AC);
        }

        [TestMethod()]
        public void TestDeniedDex() {
            GameRules.GameRules r = Rules();
            EffectPool p = r.Rules;

            p.SetValue(12, DEX_SCORE);
            CheckStat(11, r, p, AC);
            p.AddEffect(new Effect("deny").SetFlag(DENIED_DEX));
            CheckStat(10, r, p, AC);
            p.AddEffect(new Effect("ud").SetFlag(UNCANNY_DODGE));
            CheckStat(10, r, p, AC);
        }

        [TestMethod()]
        public void TestACP() {
            GameRules.GameRules r = Rules();
            EffectPool p = r.Rules;

            p.SetValue(-5, ARMOR_CHECK_PENALTY);
            CheckStat(-5, r, p, BALANCE);
            CheckStat(-10, r, p, SWIM);
            CheckStat(0, r, p, DIPLOMACY);
        }

        private Items.Weapon Dagger() {
            return new Items.Weapon("Dagger", 0) {
                Melee = (
                    new Items.AttackMode {
                        ProficencyCategory = Items.Weapon.Proficiency.SIMPLE,
                        EffortCategory = Items.Weapon.Effort.LIGHT,
                        DamageDieMedium = (1, 4),
                        DamageType = PIERCING + OR + SLASHING,
                        ThreatRange = 19,
                        CriticalMultiplier = 2,
                        RangeIncrement = 10
                    },
                    false
                )
            };
        }

        [TestMethod()]
        public void TestMeleeWeapon() {
            GameRules.GameRules r = Rules();
            EffectPool p = r.Rules;
            p.SetValue(16, STR_SCORE);
            p.SetValue(20, DEX_SCORE);

            Items.Weapon dagger = Dagger();
            dagger.Tags.Add("shadow hand");

            List<Items.Attack> daggerAttacks = Items.Attack.Generate(r, dagger, null, 0);
            Assert.AreEqual(Items.Weapon.Range.MELEE, daggerAttacks[0].Range);
            Assert.AreEqual(+3, daggerAttacks[0].AttackBonus.bonus);
            Assert.AreEqual("1d4 + 3", daggerAttacks[0].Damage.dice.ToString());
            Assert.AreEqual(Items.Weapon.Range.THROWN, daggerAttacks[1].Range);
            Assert.AreEqual(+5, daggerAttacks[1].AttackBonus.bonus);
            Assert.AreEqual("1d4 + 3", daggerAttacks[1].Damage.dice.ToString());

            p.AddEffect(new Effect("discipline focus")
                .AddAttackModifier(new WeaponClass().Tag("shadow hand"), new Modifier(+1, UNTYPED))
            );
            daggerAttacks = Items.Attack.Generate(r, dagger, null, 0);
            Assert.AreEqual(Items.Weapon.Range.MELEE, daggerAttacks[0].Range);
            Assert.AreEqual(+4, daggerAttacks[0].AttackBonus.bonus);
        }

        private Items.Weapon CompositeBow(int str) {
            return new Items.Weapon("Composite shortbow", 0) {
                Projectile = (
                    new Items.AttackMode {
                        ProficencyCategory = Items.Weapon.Proficiency.MARTIAL,
                        EffortCategory = Items.Weapon.Effort.ONE_HANDED,
                        DamageDieMedium = (1, 6),
                        DamageType = PIERCING,
                        ThreatRange = 20,
                        CriticalMultiplier = 3,
                        RangeIncrement = 70
                    },
                    true, str
                )
            };
        }

        [TestMethod()]
        public void TestCompositeBowStrong() {
            GameRules.GameRules r = Rules();
            EffectPool p = r.Rules;
            p.SetValue(16, STR_SCORE);
            p.SetValue(20, DEX_SCORE);

            Items.Weapon bow = CompositeBow(+2);
            List<Items.Attack> attack = Items.Attack.Generate(r, bow, bow, 0);
            Assert.AreEqual(1, attack.Count());
            Assert.AreEqual(+5, attack[0].AttackBonus.bonus);
            Assert.AreEqual("1d6 + 2", attack[0].Damage.dice.ToString());
        }

        [TestMethod()]
        public void TestCompositeBowWeak() {
            GameRules.GameRules r = Rules();
            EffectPool p = r.Rules;
            p.SetValue(16, STR_SCORE);
            p.SetValue(20, DEX_SCORE);

            Items.Weapon bow = CompositeBow(+4);
            List<Items.Attack> attack = Items.Attack.Generate(r, bow, bow, 0);
            Assert.AreEqual(1, attack.Count());
            Assert.AreEqual(+3, attack[0].AttackBonus.bonus);
            Assert.AreEqual("1d6 + 3", attack[0].Damage.dice.ToString());
        }

        [TestMethod()]
        public void TestFearStacking() {
            GameRules.GameRules r = Rules();
            EffectPool p = r.Rules;
            p.SetValue(20, DEX_SCORE);

            CheckStat(+5, r, p, DEX_CHECKS);
            p.SetValue(1, FEAR_LEVEL);
            CheckStat(+3, r, p, DEX_CHECKS);
        }

        [TestMethod()]
        public void TestSpecialSizeModifier() {
            GameRules.GameRules r = Rules();
            EffectPool p = r.Rules;
            p.SetValue(20, DEX_SCORE);

            // no ranks in hide, +5 dex +0 str +0 bab, medium size:  +5 hide, +0 grapple
            CheckStat(+5, r, p, HIDE);
            CheckStat(+0, r, p, GRAPPLE);

            // colossal size, which gives -16 hide +16 grapple
            p.SetValue(4, SIZE_CATEGORY);

            CheckStat(-11, r, p, HIDE);
            CheckStat(+16, r, p, GRAPPLE);
        }

        [TestMethod()]
        public void TestReachLookup() {
            GameRules.GameRules r = Rules();
            EffectPool p = r.Rules;

            CheckStat(5, r, p, REACH);
            p.SetValue(4, SIZE_CATEGORY);
            CheckStat(30, r, p, REACH);
        }

        [TestMethod()]
        public void TestSpeedModifierToJump() {
            GameRules.GameRules r = Rules();
            EffectPool p = r.Rules;

            // no ranks, +0 ability, no speed modifier.
            CheckStat(+0, r, p, JUMP);

            p.SetValue(35, LAND_SPEED);
            CheckStat(+0, r, p, JUMP);

            p.SetValue(40, LAND_SPEED);
            CheckStat(+4, r, p, JUMP);

            p.SetValue(45, LAND_SPEED);
            CheckStat(+4, r, p, JUMP);

            p.SetValue(50, LAND_SPEED);
            CheckStat(+8, r, p, JUMP);

            p.SetValue(25, LAND_SPEED);
            CheckStat(+0, r, p, JUMP);

            p.SetValue(20, LAND_SPEED);
            CheckStat(-6, r, p, JUMP);

            p.SetValue(15, LAND_SPEED);
            CheckStat(-6, r, p, JUMP);

            p.SetValue(10, LAND_SPEED);
            CheckStat(-12, r, p, JUMP);
        }

        [TestMethod()]
        public void TestEncumbrance() {
            GameRules.GameRules r = Rules();
            EffectPool p = r.Rules;

            ControllableToggle encumbered = (ControllableToggle) p.GetEffect("Encumbrance speed penalty").Toggle;
            encumbered.on = true;

            Dictionary<int, int> encumbranceTable = new Dictionary<int, int> {
                [20] = 15,
                [30] = 20,
                [40] = 30,
                [50] = 35,
                [60] = 40,
                [70] = 50,
                [80] = 55,
                [90] = 60,
                [100] = 70,
            };
            foreach (var item in encumbranceTable) {
                p.SetValue(item.Key, LAND_SPEED);
                CheckStat(item.Value, r, p, LAND_SPEED);
            }
            r.Stats()[LAND_SPEED].GetValue(p).explanation.ForEach(x => Console.WriteLine(x));
        }

        [TestMethod()]
        public void TestDiceBasic() {
            DiceGroup dice = new DiceGroup(1, 4);
            Assert.AreEqual("1d4", dice.ToString());

            dice.AddDice(2, 4);
            Assert.AreEqual("3d4", dice.ToString());

            dice.AddDice(5, 6);
            Assert.AreEqual("5d6 + 3d4", dice.ToString());

            dice.AddDice(4, 1);
            Assert.AreEqual("5d6 + 3d4 + 4", dice.ToString());
        }

        [TestMethod()]
        public void TestWeaponDamageBySize() {
            GameRules.GameRules r = Rules();
            EffectPool p = r.Rules;

            Items.Weapon dagger = Dagger();
            Assert.AreEqual("1d8", Items.Attack.Generate(r, dagger, null, +2)[0].Damage.dice.ToString());
            Assert.AreEqual("3d6", Items.Attack.Generate(r, dagger, null, +1000)[0].Damage.dice.ToString());
            Assert.AreEqual("—", Items.Attack.Generate(r, dagger, null, -1000)[0].Damage.dice.ToString());
        }

        [TestMethod()]
        public void TestExtraDamageEffects() {
            Items.Weapon dagger = Dagger();

            GameRules.GameRules r = Rules();
            EffectPool p = r.Rules;

            Items.Attack daggerMelee = Items.Attack.Generate(r, dagger, null, 0)[0];
            var (normalDice, normalExpl) = daggerMelee.Damage;
            Assert.AreEqual("1d4", normalDice.ToString());

            p.AddEffect(new Effect("Sneak attack")
                .SetExtraDice(new ExtraDice(new DiceGroup(2, 6), new WeaponClass()))
            );
            daggerMelee = Items.Attack.Generate(r, dagger, null, 0)[0];
            var (sneakDice, sneakExpl) = daggerMelee.Damage;
            Assert.AreEqual("2d6 + 1d4", sneakDice.ToString());

            p.SetValue(18, STR_SCORE);
            Items.Weapon greatsword = new Items.Weapon("Greatsword", 0) {
                Melee = (
                    new Items.AttackMode {
                        ProficencyCategory = Items.Weapon.Proficiency.MARTIAL,
                        EffortCategory = Items.Weapon.Effort.TWO_HANDED,
                        DamageDieMedium = (2, 6),
                        DamageType = SLASHING,
                        ThreatRange = 19,
                        CriticalMultiplier = 2,
                        RangeIncrement = 0
                    },
                    false
                )
            };

            p.AddEffect(new Effect("inspire courage")
                .AddDamageModifier(WeaponClass.ALL, new Modifier(+1, MORALE))
            );
            p.AddEffect(new Effect("prayer")
                .AddDamageModifier(WeaponClass.ALL, new Modifier(+1, LUCK))
            );

            p.SetValue(14, DEX_SCORE);
            p.AddEffect(new Effect("Hit-and-run fighter")
                .AddDamageModifier(WeaponClass.ALL, new Modifier(r.Stats()[DEX_MODIFIER], CIRCUMSTANCE))
            );

            List<Items.Attack> greatswordAttacks = Items.Attack.Generate(r, greatsword, greatsword, 0);
            Assert.AreEqual(1, greatswordAttacks.Count());
            Items.Attack greatswordAttack = greatswordAttacks[0];
            var (lotsaDice, bigExpl) = greatswordAttack.Damage;
            Assert.AreEqual("4d6 + 10", lotsaDice.ToString());

            var (critDice, critExpl) = greatswordAttack.CriticalDamage;
            Assert.AreEqual("6d6 + 20", critDice.ToString());
            critExpl.ForEach(x => Console.WriteLine(x));
        }

        [TestMethod()]
        public void TestWrongSizeWeapon() {
            Items.Weapon dagger = Dagger();
            dagger.SizeCategory = +2;

            GameRules.GameRules r = Rules();
            EffectPool p = r.Rules;
            // it's big enough that we have to two-hand it
            var daggerAttacks = Items.Attack.Generate(r, dagger, dagger, 0);
            Assert.AreEqual(2, daggerAttacks.Count());
            Items.Attack melee = daggerAttacks[0];
            Assert.AreEqual(-4, melee.AttackBonus.bonus);
            Assert.AreEqual(Items.Weapon.Effort.TWO_HANDED, melee.Effort);
            Items.Attack thrown = daggerAttacks[1];
            Assert.AreEqual(-4, melee.AttackBonus.bonus);
            Assert.AreEqual(Items.Weapon.Effort.TWO_HANDED, thrown.Effort);
        }

        [TestMethod()]
        public void TestTextEffects() {
            GameRules.GameRules r = Rules();
            EffectPool p = r.Rules;

            p.AddEffect(new Effect("Darkvision")
                .AddText(SENSES, "darkvision 60 ft.")
                .AddText(SENSES, "low-light vision")
            );
            p.AddEffect(new Effect("Something")
                .AddText("new category", "something something")
            );
            var allText = p.GetTextEffects();

            List<string> senses = allText[SENSES];
            Assert.AreEqual(2, senses.Count());
            Assert.AreEqual("darkvision 60 ft.", senses[0]);
            Assert.AreEqual("low-light vision", senses[1]);

            List<string> uncategorized = allText[UNCATEGORIZED];
            Assert.AreEqual(1, uncategorized.Count());
            Assert.AreEqual("[new category] something something", uncategorized[0]);
        }
    }
}