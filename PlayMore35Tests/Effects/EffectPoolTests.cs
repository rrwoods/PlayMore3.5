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

        private void CheckStat(int expected, GameRules.GameRules r, string stat) {
            (int value, List<string> explanation) = r.Stats[stat].GetValue(r.Rules);
            Console.WriteLine("------ " + stat + " " + value);
            explanation.ForEach(x => Console.WriteLine(x));
            Assert.AreEqual(expected, value);
        }

        [TestMethod()]
        public void TestComputeAbilityModifier() {
            GameRules.GameRules r = Rules();
            r.Rules.SetValue(7, STR_SCORE);
            CheckStat(-2, r, STR_MODIFIER);
            CheckStat(0, r, DEX_MODIFIER);
        }

        [TestMethod()]
        public void TestConditionalEffects() {
            GameRules.GameRules r = Rules();
            r.Rules.SetValue(5, TUMBLE + RANKS);
            CheckStat(+2, r, JUMP);
            CheckStat(+5, r, TUMBLE);
        }

        [TestMethod()]
        public void TestInitiativeChaining() {
            GameRules.GameRules r = Rules();
            r.Rules.AddEffect(new Effect("somethin").AddModifier(ABILITY_CHECKS, new Modifier(4, UNTYPED)));
            CheckStat(+4, r, INITIATIVE);
        }

        [TestMethod()]
        public void TestAC() {
            GameRules.GameRules r = Rules();
            EffectPool p = r.Rules;

            p.SetValue(12, DEX_SCORE);
            p.AddEffect(new Effect("ring").AddModifier(AC, new Modifier(2, DEFLECTION)));
            p.AddEffect(new Effect("dragon scales").AddModifier(NATURAL_ARMOR, new Modifier(4, RACIAL)));

            CheckStat(17, r, AC);
            CheckStat(13, r, TOUCH_AC);
        }

        [TestMethod()]
        public void TestFlatFootedAC() {
            GameRules.GameRules r = Rules();
            EffectPool p = r.Rules;

            p.SetValue(12, DEX_SCORE);
            CheckStat(11, r, AC);
            p.AddEffect(new Effect("ff").SetFlag(FLAT_FOOTED));
            CheckStat(10, r, AC);
            p.AddEffect(new Effect("ud").SetFlag(UNCANNY_DODGE));
            CheckStat(11, r, AC);
        }

        [TestMethod()]
        public void TestDeniedDex() {
            GameRules.GameRules r = Rules();
            EffectPool p = r.Rules;

            p.SetValue(12, DEX_SCORE);
            CheckStat(11, r, AC);
            p.AddEffect(new Effect("deny").SetFlag(DENIED_DEX));
            CheckStat(10, r, AC);
            p.AddEffect(new Effect("ud").SetFlag(UNCANNY_DODGE));
            CheckStat(10, r, AC);
        }

        [TestMethod()]
        public void TestACP() {
            GameRules.GameRules r = Rules();
            EffectPool p = r.Rules;

            p.SetValue(-5, ARMOR_CHECK_PENALTY);
            CheckStat(-5, r, BALANCE);
            CheckStat(-10, r, SWIM);
            CheckStat(0, r, DIPLOMACY);
        }

        private Items.Weapon Dagger(bool throwable = true) {
            return new Items.Weapon("dagger", 0) {
                Melee = (
                    new Items.AttackMode {
                        ProficencyCategory = Items.Weapon.Proficiency.SIMPLE,
                        EffortCategory = Items.Weapon.Effort.LIGHT,
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

        private Items.Weapon Longsword() {
            return new Items.Weapon("longsword", 0) {
                Melee = (
                    new Items.AttackMode {
                        ProficencyCategory = Items.Weapon.Proficiency.MARTIAL,
                        EffortCategory = Items.Weapon.Effort.ONE_HANDED,
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

        private Items.Weapon Greatsword() {
            return new Items.Weapon("greatsword", 0) {
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
        }

        [TestMethod()]
        public void TestMeleeWeapon() {
            GameRules.GameRules r = Rules();
            EffectPool p = r.Rules;
            p.SetValue(16, STR_SCORE);
            p.SetValue(20, DEX_SCORE);

            Items.Weapon dagger = Dagger();
            dagger.Tags.Add("shadow hand");
            p.OneHandOneWeapon(dagger);

            List<Items.Attack> daggerAttacks = Items.Attack.Generate(r, 0);
            Assert.AreEqual(Items.Weapon.Range.MELEE, daggerAttacks[0].Range);
            Assert.AreEqual(+3, daggerAttacks[0].AttackBonus.bonus);
            Assert.AreEqual("1d4 + 3", daggerAttacks[0].Damage.dice.ToString());
            Assert.AreEqual(Items.Weapon.Range.THROWN, daggerAttacks[1].Range);
            Assert.AreEqual(+5, daggerAttacks[1].AttackBonus.bonus);
            Assert.AreEqual("1d4 + 3", daggerAttacks[1].Damage.dice.ToString());

            p.AddEffect(new Effect("discipline focus")
                .AddAttackModifier(new WeaponClass().Tag("shadow hand"), new Modifier(+1, UNTYPED))
            );
            daggerAttacks = Items.Attack.Generate(r, 0);
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
            p.TwoHandOneWeapon(bow);

            List<Items.Attack> attack = Items.Attack.Generate(r, 0);
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
            p.TwoHandOneWeapon(bow);

            List<Items.Attack> attack = Items.Attack.Generate(r, 0);
            Assert.AreEqual(1, attack.Count());
            Assert.AreEqual(+3, attack[0].AttackBonus.bonus);
            Assert.AreEqual("1d6 + 3", attack[0].Damage.dice.ToString());
        }

        [TestMethod()]
        public void TestFearStacking() {
            GameRules.GameRules r = Rules();
            EffectPool p = r.Rules;
            p.SetValue(20, DEX_SCORE);

            CheckStat(+5, r, DEX_CHECKS);
            p.SetValue(1, FEAR_LEVEL);
            CheckStat(+3, r, DEX_CHECKS);
        }

        [TestMethod()]
        public void TestSpecialSizeModifier() {
            GameRules.GameRules r = Rules();
            EffectPool p = r.Rules;
            p.SetValue(20, DEX_SCORE);

            // no ranks in hide, +5 dex +0 str +0 bab, medium size:  +5 hide, +0 grapple
            CheckStat(+5, r, HIDE);
            CheckStat(+0, r, GRAPPLE);

            // colossal size, which gives -16 hide +16 grapple
            p.SetValue(4, SIZE_CATEGORY);

            CheckStat(-11, r, HIDE);
            CheckStat(+16, r, GRAPPLE);
        }

        [TestMethod()]
        public void TestReachLookup() {
            GameRules.GameRules r = Rules();
            EffectPool p = r.Rules;

            CheckStat(5, r, REACH);
            p.SetValue(4, SIZE_CATEGORY);
            CheckStat(30, r, REACH);
        }

        [TestMethod()]
        public void TestSpeedModifierToJump() {
            GameRules.GameRules r = Rules();
            EffectPool p = r.Rules;

            // no ranks, +0 ability, no speed modifier.
            CheckStat(+0, r, JUMP);

            p.SetValue(35, LAND_SPEED);
            CheckStat(+0, r, JUMP);

            p.SetValue(40, LAND_SPEED);
            CheckStat(+4, r, JUMP);

            p.SetValue(45, LAND_SPEED);
            CheckStat(+4, r, JUMP);

            p.SetValue(50, LAND_SPEED);
            CheckStat(+8, r, JUMP);

            p.SetValue(25, LAND_SPEED);
            CheckStat(+0, r, JUMP);

            p.SetValue(20, LAND_SPEED);
            CheckStat(-6, r, JUMP);

            p.SetValue(15, LAND_SPEED);
            CheckStat(-6, r, JUMP);

            p.SetValue(10, LAND_SPEED);
            CheckStat(-12, r, JUMP);
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
                CheckStat(item.Value, r, LAND_SPEED);
            }
            r.Stats[LAND_SPEED].GetValue(p).explanation.ForEach(x => Console.WriteLine(x));
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

            p.OneHandOneWeapon(Dagger());

            p.SetValue(+2, SIZE_CATEGORY);
            Assert.AreEqual("1d8", Items.Attack.Generate(r, +2)[0].Damage.dice.ToString());
            p.SetValue(+1000, SIZE_CATEGORY);
            Assert.AreEqual("3d6", Items.Attack.Generate(r, +1000)[0].Damage.dice.ToString());
            p.SetValue(-1000, SIZE_CATEGORY);
            Assert.AreEqual("—", Items.Attack.Generate(r, -1000)[0].Damage.dice.ToString());
        }

        [TestMethod()]
        public void TestExtraDamageEffects() {
            GameRules.GameRules r = Rules();
            EffectPool p = r.Rules;
            p.OneHandOneWeapon(Dagger());

            Items.Attack daggerMelee = Items.Attack.Generate(r, 0)[0];
            var (normalDice, normalExpl) = daggerMelee.Damage;
            Assert.AreEqual("1d4", normalDice.ToString());

            p.AddEffect(new Effect("Sneak attack")
                .SetExtraDice(new ExtraDice(new WeaponClass(), 2, 6))
            );
            daggerMelee = Items.Attack.Generate(r, 0)[0];
            var (sneakDice, sneakExpl) = daggerMelee.Damage;
            Assert.AreEqual("2d6 + 1d4", sneakDice.ToString());

            p.SetValue(18, STR_SCORE);

            p.AddEffect(new Effect("inspire courage")
                .AddDamageModifier(WeaponClass.ALL, new Modifier(+1, MORALE))
            );
            p.AddEffect(new Effect("prayer")
                .AddDamageModifier(WeaponClass.ALL, new Modifier(+1, LUCK))
            );

            p.SetValue(14, DEX_SCORE);
            p.AddEffect(new Effect("Hit-and-run fighter")
                .AddDamageModifier(WeaponClass.ALL, new Modifier(r.Stats[DEX_MODIFIER], CIRCUMSTANCE))
            );

            p.TwoHandOneWeapon(Greatsword());
            List<Items.Attack> greatswordAttacks = Items.Attack.Generate(r, 0);
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
            p.TwoHandOneWeapon(dagger);

            var daggerAttacks = Items.Attack.Generate(r, 0);
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

        private void CheckAttack(int[] expAtkBonus, string[] expDamage, GameRules.GameRules r, Items.Weapon pri, Items.Weapon off) {
            r.Rules.TwoWeapons(pri, off); // this is really just a direct setter for pri and off.

            List<Items.Attack> attacks = Items.Attack.Generate(r, 0);
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

        private void CheckAttack(int expAtkBonus, string expDamage, GameRules.GameRules r, Items.Weapon pri, Items.Weapon off) {
            CheckAttack(new int[] { expAtkBonus }, new string[] { expDamage }, r, pri, off);
        }

        [TestMethod()]
        public void TestControllableAmount() {
            GameRules.GameRules r = Rules();
            EffectPool p = r.Rules;

            ControllableAmount powerAttackAmount = new ControllableAmount("Power attack penalty")
                .Minimum(0)
                .Maximum(r.Stats[BASE_ATTACK_BONUS]);

            p.AddEffect(new Effect("Power Attack")
                .AddAttackModifier(
                    new WeaponClass().Melee(),
                    new Modifier(new MultiplyAmount(-1, powerAttackAmount), UNTYPED)
                )
                .AddDamageModifier(
                    new WeaponClass().TwoHandedAttack(),
                    new Modifier(new MultiplyAmount(2, powerAttackAmount), UNTYPED)
                )
                .AddDamageModifier(
                    new WeaponClass().OneHandedWeapon().OneHandedAttack(),
                    new Modifier(powerAttackAmount, UNTYPED)
                )
            );

            p.SetValue(+2, BASE_ATTACK_BONUS);
            Items.Weapon dagger = Dagger(false);
            Items.Weapon longsword = Longsword();

            // Just BAB -- attack at +2, no damage bonus.
            CheckAttack(+2, "1d4", r, dagger, null);
            CheckAttack(+2, "1d8", r, longsword, null);
            CheckAttack(+2, "1d8", r, longsword, longsword);

            // Power attack for 1 -- attack at +1... light +0 damage, 1h +1 damage, 2h +2 damage
            powerAttackAmount.Amount = 1;
            CheckAttack(+1, "1d4", r, dagger, null);
            CheckAttack(+1, "1d8 + 1", r, longsword, null);
            CheckAttack(+1, "1d8 + 2", r, longsword, longsword);

            // Power attack for "3", clamped to 2 -- attack at +0... light +0 damage, 1h +2 damage, 2h +4 damage
            powerAttackAmount.Amount = 3;
            CheckAttack(+0, "1d4", r, dagger, null);
            CheckAttack(+0, "1d8 + 2", r, longsword, null);
            CheckAttack(+0, "1d8 + 4", r, longsword, longsword);

            // Power attack for "-1", clamped to 0, same as no power attack
            powerAttackAmount.Amount = -1;
            CheckAttack(+2, "1d4", r, dagger, null);
            CheckAttack(+2, "1d8", r, longsword, null);
            CheckAttack(+2, "1d8", r, longsword, longsword);
        }

        [TestMethod()]
        public void TestLocalWeaponProperty() {
            Items.Weapon dagger = Dagger(false);
            dagger.Melee?.atk.Properties.Add(Items.WeaponProperty.PLUS[2]);
            CheckAttack(+2, "1d4 + 2", Rules(), dagger, null);
        }

        [TestMethod()]
        public void TestGlobalWeaponProperty() {
            GameRules.GameRules r = Rules();

            Items.Weapon dagger = Dagger(false);
            dagger.Melee?.atk.Properties.Add(new Items.WeaponProperty("warning") {
                GlobalEffect = new Effect("warning dagger")
                    .AddModifier(INITIATIVE, new Modifier(+5, INSIGHT))
            });

            r.Rules.OneHandOneWeapon(dagger);

            CheckStat(+5, r, INITIATIVE);
        }
    }
}