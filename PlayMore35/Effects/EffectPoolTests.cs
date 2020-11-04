using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Newtonsoft.Json.Linq;
using PlayMore35.Items;
using static PlayMore35.Effects.Constants;

namespace PlayMore35.Effects.Tests {
    static class Dependencies {
        public static JObject rulesJson;
    }

    [TestClass]
    public class EffectPoolTests {
        [ClassInitialize]
        public static void JsonLoad(TestContext context) {
            Dependencies.rulesJson = JObject.Parse(Encoding.UTF8.GetString(Properties.Resources.rules));
        }

        private Effect ModifyStat(int value, string stat) {
            return new Effect(stat).AddModifier(stat, new Modifier(value, UNTYPED));
        }

        private EffectPool Rules() {
            EffectPool p = new EffectPool();
            p.ReadJson(Dependencies.rulesJson);

            // make all the ability modifiers zero
            p.AddEffect(ModifyStat(10, STR_SCORE));
            p.AddEffect(ModifyStat(10, DEX_SCORE));
            p.AddEffect(ModifyStat(10, CON_SCORE));
            p.AddEffect(ModifyStat(10, INT_SCORE));
            p.AddEffect(ModifyStat(10, WIS_SCORE));
            p.AddEffect(ModifyStat(10, CHA_SCORE));

            // speed 30 to make sure jump isn't affected
            p.AddEffect(ModifyStat(30, LAND_SPEED));

            // 0 HP causes some weirdness too
            p.AddEffect(ModifyStat(1, MAX_HP));

            return p;
        }

        private void CheckStat(int expected, EffectPool r, string stat) {
            (int value, List<string> explanation) = r.GetStat(stat).GetValue(r);
            Console.WriteLine("------ " + stat + " " + value);
            explanation.ForEach(x => Console.WriteLine(x));
            Assert.AreEqual(expected, value);
        }

        [TestMethod]
        public void TestComputeAbilityModifier() {
            EffectPool r = Rules();
            r.AddEffect(ModifyStat(-3, STR_SCORE));
            CheckStat(-2, r, STR_MODIFIER);
            CheckStat(0, r, DEX_MODIFIER);
        }

        [TestMethod]
        public void TestConditionalEffects() {
            EffectPool r = Rules();
            r.AddEffect(ModifyStat(5, TUMBLE + RANKS));
            CheckStat(+2, r, JUMP);
            CheckStat(+5, r, TUMBLE);
        }

        [TestMethod]
        public void TestInitiativeChaining() {
            EffectPool r = Rules();
            r.AddEffect(new Effect("Improved 'initiative'").AddModifier(ABILITY_CHECKS, new Modifier(4, UNTYPED)));
            CheckStat(+4, r, INITIATIVE);
        }

        [TestMethod]
        public void TestAC() {
            EffectPool p = Rules();

            p.AddEffect(ModifyStat(+2, DEX_SCORE));
            p.AddEffect(new Effect("ring").AddModifier(AC, new Modifier(2, DEFLECTION)));
            p.AddEffect(new Effect("dragon scales").AddModifier(NATURAL_ARMOR, new Modifier(4, RACIAL)));

            CheckStat(17, p, AC);
            CheckStat(13, p, TOUCH_AC);
        }

        [TestMethod]
        public void TestFlatFootedAC() {
            EffectPool p = Rules();

            p.AddEffect(ModifyStat(+2, DEX_SCORE));
            CheckStat(11, p, AC);
            p.AddEffect(new Effect("ff").AddFlag(FLAT_FOOTED));
            CheckStat(10, p, AC);
            p.AddEffect(new Effect("ud").AddFlag(UNCANNY_DODGE));
            CheckStat(11, p, AC);
        }

        [TestMethod]
        public void TestDeniedDex() {
            EffectPool p = Rules();

            p.AddEffect(ModifyStat(+2, DEX_SCORE));
            CheckStat(11, p, AC);
            p.AddEffect(new Effect("deny").AddFlag(DENIED_DEX));
            CheckStat(10, p, AC);
            p.AddEffect(new Effect("ud").AddFlag(UNCANNY_DODGE));
            CheckStat(10, p, AC);
        }

        [TestMethod]
        public void TestACP() {
            EffectPool p = Rules();

            p.AddEffect(ModifyStat(-5, ARMOR_CHECK_PENALTY));
            CheckStat(-5, p, BALANCE);
            CheckStat(-10, p, SWIM);
            CheckStat(0, p, DIPLOMACY);
        }

        private Weapon Dagger(bool throwable = true) {
            return new Weapon("dagger", 0, Weapon.Effort.LIGHT) {
                Melee = (
                    new AttackMode {
                        ProficencyCategory = Weapon.Proficiency.SIMPLE,
                        DamageDieMedium = (1, 4),
                        DamageType = PIERCING + OR + SLASHING,
                        ThreatRange = 19,
                        CriticalMultiplier = 2,
                        RangeIncrement = throwable ? 10 : 0
                    },
                    false
                )
            };
        }

        private Weapon Longsword() {
            return new Weapon("longsword", 0, Weapon.Effort.ONE_HANDED) {
                Melee = (
                    new AttackMode {
                        ProficencyCategory = Weapon.Proficiency.MARTIAL,
                        DamageDieMedium = (1, 8),
                        DamageType = SLASHING,
                        ThreatRange = 19,
                        CriticalMultiplier = 2,
                        RangeIncrement = 0
                    },
                    false
                )
            };
        }

        private Weapon Greatsword() {
            return new Weapon("greatsword", 0, Weapon.Effort.TWO_HANDED) {
                Melee = (
                    new AttackMode {
                        ProficencyCategory = Weapon.Proficiency.MARTIAL,
                        DamageDieMedium = (2, 6),
                        DamageType = SLASHING,
                        ThreatRange = 19,
                        CriticalMultiplier = 2,
                        RangeIncrement = 0
                    },
                    false
                )
            };
        }

        [TestMethod]
        public void TestMeleeWeapon() {
            EffectPool p = Rules();
            p.AddEffect(ModifyStat(+6, STR_SCORE));
            p.AddEffect(ModifyStat(+10, DEX_SCORE));

            Weapon dagger = Dagger();
            dagger.Tags.Add("shadow hand");
            p.OneHandOneWeapon(dagger);

            List<Attack> daggerAttacks = Attack.Generate(p, 0);
            Assert.AreEqual(Weapon.Range.MELEE, daggerAttacks[0].Range);
            Assert.AreEqual(+3, daggerAttacks[0].AttackBonus.bonus);
            Assert.AreEqual("1d4 + 3", daggerAttacks[0].Damage.dice.ToString());
            Assert.AreEqual(Weapon.Range.THROWN, daggerAttacks[1].Range);
            Assert.AreEqual(+5, daggerAttacks[1].AttackBonus.bonus);
            Assert.AreEqual("1d4 + 3", daggerAttacks[1].Damage.dice.ToString());

            p.AddEffect(new Effect("discipline focus")
                .AddAttackModifier(new AttackClass().Tag("shadow hand"), new Modifier(+1, UNTYPED))
            );
            daggerAttacks = Attack.Generate(p, 0);
            Assert.AreEqual(Weapon.Range.MELEE, daggerAttacks[0].Range);
            Assert.AreEqual(+4, daggerAttacks[0].AttackBonus.bonus);
        }

        private Weapon CompositeBow(int str) {
            return new Weapon("Composite shortbow", 0, Weapon.Effort.ONE_HANDED) {
                Projectile = (
                    new AttackMode {
                        ProficencyCategory = Weapon.Proficiency.MARTIAL,
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

        [TestMethod]
        public void TestCompositeBowStrong() {
            EffectPool p = Rules();
            p.AddEffect(ModifyStat(+6, STR_SCORE));
            p.AddEffect(ModifyStat(+10, DEX_SCORE));

            Weapon bow = CompositeBow(+2);
            p.TwoHandOneWeapon(bow);

            List<Attack> attack = Attack.Generate(p, 0);
            Assert.AreEqual(1, attack.Count());
            Assert.AreEqual(+5, attack[0].AttackBonus.bonus);
            Assert.AreEqual("1d6 + 2", attack[0].Damage.dice.ToString());
        }

        [TestMethod]
        public void TestCompositeBowWeak() {
            EffectPool p = Rules();
            p.AddEffect(ModifyStat(+6, STR_SCORE));
            p.AddEffect(ModifyStat(+10, DEX_SCORE));

            Weapon bow = CompositeBow(+4);
            p.TwoHandOneWeapon(bow);

            List<Attack> attack = Attack.Generate(p, 0);
            Assert.AreEqual(1, attack.Count());
            Assert.AreEqual(+3, attack[0].AttackBonus.bonus);
            Assert.AreEqual("1d6 + 3", attack[0].Damage.dice.ToString());
        }

        [TestMethod]
        public void TestFearStacking() {
            EffectPool p = Rules();
            p.AddEffect(ModifyStat(+10, DEX_SCORE));

            CheckStat(+5, p, DEX_CHECKS);
            p.AddEffect(ModifyStat(1, FEAR_LEVEL));
            CheckStat(+3, p, DEX_CHECKS);
        }

        [TestMethod]
        public void TestSpecialSizeModifier() {
            EffectPool p = Rules();
            p.AddEffect(ModifyStat(+10, DEX_SCORE));

            // no ranks in hide, +5 dex +0 str +0 bab, medium size:  +5 hide, +0 grapple
            CheckStat(+5, p, HIDE);
            CheckStat(+0, p, GRAPPLE);

            // colossal size, which gives -16 hide +16 grapple
            p.AddEffect(ModifyStat(4, SIZE_CATEGORY));

            CheckStat(-11, p, HIDE);
            CheckStat(+16, p, GRAPPLE);
        }

        [TestMethod]
        public void TestReachLookup() {
            EffectPool p = Rules();

            CheckStat(5, p, REACH);
            p.AddEffect(ModifyStat(4, SIZE_CATEGORY));
            CheckStat(30, p, REACH);
        }

        [TestMethod]
        public void TestSpeedModifierToJump() {
            EffectPool p = Rules();

            // no ranks, +0 ability, no speed modifier.
            CheckStat(+0, p, JUMP);

            Effect speedModifier = ModifyStat(+5, LAND_SPEED);
            p.AddEffect(speedModifier);
            CheckStat(+0, p, JUMP);

            p.AddEffect(speedModifier);
            CheckStat(+4, p, JUMP);
        }

        [TestMethod]
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

        [TestMethod]
        public void TestWeaponDamageBySize() {
            EffectPool p = Rules();

            p.OneHandOneWeapon(Dagger());

            p.AddEffect(ModifyStat(+2, SIZE_CATEGORY));
            Assert.AreEqual("1d8", Attack.Generate(p, +2)[0].Damage.dice.ToString());
            p.AddEffect(ModifyStat(+1000, SIZE_CATEGORY));
            Assert.AreEqual("3d6", Attack.Generate(p, +1002)[0].Damage.dice.ToString());
            p.AddEffect(ModifyStat(-2002, SIZE_CATEGORY));
            Assert.AreEqual("—", Attack.Generate(p, -1000)[0].Damage.dice.ToString());
        }

        [TestMethod]
        public void TestExtraDamageEffects() {
            EffectPool p = Rules();
            p.OneHandOneWeapon(Dagger());

            Attack daggerMelee = Attack.Generate(p, 0)[0];
            var (normalDice, normalExpl) = daggerMelee.Damage;
            Assert.AreEqual("1d4", normalDice.ToString());

            p.AddEffect(new Effect("Sneak attack")
                .AddExtraDice(AttackClass.ALL, 2, 6)
            );
            daggerMelee = Attack.Generate(p, 0)[0];
            var (sneakDice, sneakExpl) = daggerMelee.Damage;
            Assert.AreEqual("2d6 + 1d4", sneakDice.ToString());

            p.AddEffect(ModifyStat(+8, STR_SCORE));

            p.AddEffect(new Effect("inspire courage")
                .AddDamageModifier(AttackClass.ALL, new Modifier(+1, MORALE))
            );
            p.AddEffect(new Effect("prayer")
                .AddDamageModifier(AttackClass.ALL, new Modifier(+1, LUCK))
            );

            p.AddEffect(ModifyStat(+4, DEX_SCORE));
            p.AddEffect(new Effect("Hit-and-run fighter")
                .AddDamageModifier(AttackClass.ALL, new Modifier((DEX_MODIFIER), CIRCUMSTANCE))
            );

            p.TwoHandOneWeapon(Greatsword());
            List<Attack> greatswordAttacks = Attack.Generate(p, 0);
            Assert.AreEqual(1, greatswordAttacks.Count());
            Attack greatswordAttack = greatswordAttacks[0];
            var (lotsaDice, bigExpl) = greatswordAttack.Damage;
            Assert.AreEqual("4d6 + 10", lotsaDice.ToString());

            var (critDice, critExpl) = greatswordAttack.CriticalDamage;
            Assert.AreEqual("6d6 + 20", critDice.ToString());
            critExpl.ForEach(x => Console.WriteLine(x));
        }

        [TestMethod]
        public void TestWrongSizeWeapon() {
            Weapon dagger = Dagger();
            dagger.SizeCategory = +2;

            EffectPool p = Rules();
            // it's big enough that we have to two-hand it
            p.TwoHandOneWeapon(dagger);

            var daggerAttacks = Attack.Generate(p, 0);
            Assert.AreEqual(2, daggerAttacks.Count());
            Attack melee = daggerAttacks[0];
            Assert.AreEqual(-4, melee.AttackBonus.bonus);
            Assert.AreEqual(Weapon.Effort.TWO_HANDED, melee.Effort);
            Attack thrown = daggerAttacks[1];
            Assert.AreEqual(-4, melee.AttackBonus.bonus);
            Assert.AreEqual(Weapon.Effort.TWO_HANDED, thrown.Effort);
        }

        [TestMethod]
        public void TestTextEffects() {
            EffectPool p = Rules();

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

        private void CheckAttack(int[] expAtkBonus, string[] expDamage, EffectPool r, Weapon pri, Weapon off) {
            r.TwoWeapons(pri, off); // this is really just a direct setter for pri and off.

            List<Attack> attacks = Attack.Generate(r, 0);
            Assert.AreEqual(expAtkBonus.Length, attacks.Count());
            for (int i = 0; i < expAtkBonus.Length; i++) {
                Console.WriteLine("------ " + attacks[i].WeaponName + " " + attacks[i].Hand);

                (int bonus, List<string> bonusExpl) = attacks[i].AttackBonus;
                Console.WriteLine("Attack bonus " + bonus);
                bonusExpl.ForEach(x => Console.WriteLine(x));

                (DiceGroup damage, List<string> damageExpl) = attacks[i].Damage;
                Console.WriteLine("Damage " + damage);
                damageExpl.ForEach(x => Console.WriteLine(x));

                Assert.AreEqual(expAtkBonus[i], attacks[i].AttackBonus.bonus);
                Assert.AreEqual(expDamage[i], attacks[i].Damage.dice.ToString());
            }
        }

        private void CheckAttack(int expAtkBonus, string expDamage, EffectPool r, Weapon pri, Weapon off) {
            CheckAttack(new int[] { expAtkBonus }, new string[] { expDamage }, r, pri, off);
        }

        [TestMethod]
        public void TestControllableAmount() {
            EffectPool p = Rules();

            ControllableAmount powerAttackAmount = new ControllableAmount("Power attack penalty")
                .Minimum(0)
                .Maximum((BASE_ATTACK_BONUS));

            p.AddEffect(new Effect("Power Attack")
                .AddAttackModifier(
                    new AttackClass().Melee(),
                    new Modifier(new MultiplyAmount(-1, powerAttackAmount), UNTYPED)
                )
                .AddDamageModifier(
                    new AttackClass().TwoHandedAttack(),
                    new Modifier(new MultiplyAmount(2, powerAttackAmount), UNTYPED)
                )
                .AddDamageModifier(
                    new AttackClass().OneHandedWeapon().OneHandedAttack(),
                    new Modifier(powerAttackAmount, UNTYPED)
                )
            );

            p.AddEffect(ModifyStat(+2, BASE_ATTACK_BONUS));
            Weapon dagger = Dagger(false);
            Weapon longsword = Longsword();

            // Just BAB -- attack at +2, no damage bonus.
            CheckAttack(+2, "1d4", p, dagger, null);
            CheckAttack(+2, "1d8", p, longsword, null);
            CheckAttack(+2, "1d8", p, longsword, longsword);

            // Power attack for 1 -- attack at +1... light +0 damage, 1h +1 damage, 2h +2 damage
            powerAttackAmount.Amt = 1;
            CheckAttack(+1, "1d4", p, dagger, null);
            CheckAttack(+1, "1d8 + 1", p, longsword, null);
            CheckAttack(+1, "1d8 + 2", p, longsword, longsword);

            // Power attack for "3", clamped to 2 -- attack at +0... light +0 damage, 1h +2 damage, 2h +4 damage
            powerAttackAmount.Amt = 3;
            CheckAttack(+0, "1d4", p, dagger, null);
            CheckAttack(+0, "1d8 + 2", p, longsword, null);
            CheckAttack(+0, "1d8 + 4", p, longsword, longsword);

            // Power attack for "-1", clamped to 0, same as no power attack
            powerAttackAmount.Amt = -1;
            CheckAttack(+2, "1d4", p, dagger, null);
            CheckAttack(+2, "1d8", p, longsword, null);
            CheckAttack(+2, "1d8", p, longsword, longsword);
        }

        [TestMethod]
        public void TestLocalWeaponProperty() {
            Weapon dagger = Dagger(false);
            dagger.Melee?.atk.Properties.Add(WeaponProperty.PLUS[2]);
            CheckAttack(+2, "1d4 + 2", Rules(), dagger, null);
        }

        [TestMethod]
        public void TestGlobalWeaponProperty() {
            EffectPool p = Rules();

            Weapon dagger = Dagger(false);
            dagger.Melee?.atk.Properties.Add(new WeaponProperty("warning") {
                GlobalEffect = new Effect("warning dagger")
                    .AddModifier(INITIATIVE, new Modifier(+5, INSIGHT))
            });
            p.OneHandOneWeapon(dagger);

            CheckStat(+5, p, INITIATIVE);
        }

        [TestMethod]
        public void TestTwfPenalties() {
            EffectPool p = Rules();

            Weapon priSword = Longsword();
            Weapon offSword = Longsword();
            Weapon offDagger = Dagger(false);

            CheckAttack(
                new int[] { +0, -6, -10 },
                new string[] { "1d8", "1d8", "1d8" },
                p,
                priSword,
                offSword
            );

            CheckAttack(
                new int[] { +0, -4, -8 },
                new string[] { "1d8", "1d8", "1d4" },
                p,
                priSword,
                offDagger
            );
            p.AddEffect(new Effect("Two-Weapon Fighting")
                .AddAttackModifier(new AttackClass().PrimaryHandTwfAttack(), new Modifier(+2, UNTYPED))
                .AddAttackModifier(new AttackClass().OffHandAttack(), new Modifier(+6, UNTYPED))
            );

            CheckAttack(
                new int[] { +0, -4, -4 },
                new string[] { "1d8", "1d8", "1d8" },
                p,
                priSword,
                offSword
            );

            CheckAttack(
                new int[] { +0, -2, -2 },
                new string[] { "1d8", "1d8", "1d4" },
                p,
                priSword,
                offDagger
            );
            p.AddEffect(ModifyStat(+4, STR_SCORE));
            Weapon quarterstaff = new Weapon("longsword", 0, Weapon.Effort.ONE_HANDED) {
                Melee = (
                    new AttackMode {
                        ProficencyCategory = Weapon.Proficiency.SIMPLE,
                        DamageDieMedium = (1, 4),
                        DamageType = BLUDGEONING,
                        ThreatRange = 20,
                        CriticalMultiplier = 2,
                        RangeIncrement = 0
                    },
                    false
                ),
                SecondIdenticalHead = true
            };

            CheckAttack(
                new int[] { +2, +0, +0 },
                new string[] { "1d4 + 3", "1d4 + 2", "1d4 + 1" },
                p,
                quarterstaff,
                quarterstaff
            );
        }
    }
}