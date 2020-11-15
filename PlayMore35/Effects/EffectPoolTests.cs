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
            p.AddEffect(ModifyStat(30, BASE_SPEED));

            // 0 HP causes some weirdness too
            p.AddEffect(ModifyStat(1, MAX_HP));

            p.AddEffect(new Effect("Simple weapon proficiency")
                .AddProficiency(new AttackMatcher().SimpleOrMartial())
            );

            return p;
        }

        private void CheckStat(int expected, EffectPool r, Equipped items, string stat) {
            (int value, List<string> explanation) = r.GetStat(stat).GetValue(r, items);
            Console.WriteLine("------ " + stat + " " + value);
            explanation.ForEach(x => Console.WriteLine(x));
            Assert.AreEqual(expected, value);
        }

        [TestMethod]
        public void TestComputeAbilityModifier() {
            EffectPool r = Rules();
            r.AddEffect(ModifyStat(-3, STR_SCORE));
            Equipped items = new Equipped();
            CheckStat(-2, r, items, STR_MODIFIER);
            CheckStat(0, r, items, DEX_MODIFIER);
        }

        [TestMethod]
        public void TestConditionalEffects() {
            EffectPool r = Rules();
            r.AddEffect(ModifyStat(5, TUMBLE + RANKS));
            Equipped items = new Equipped();
            CheckStat(+2, r, items, JUMP);
            CheckStat(+5, r, items, TUMBLE);
        }

        [TestMethod]
        public void TestInitiativeChaining() {
            EffectPool r = Rules();
            r.AddEffect(new Effect("Improved 'initiative'").AddModifier(ABILITY_CHECKS, new Modifier(4, UNTYPED)));
            Equipped items = new Equipped();
            CheckStat(+4, r, items, INITIATIVE);
        }

        [TestMethod]
        public void TestAC() {
            EffectPool p = Rules();

            p.AddEffect(ModifyStat(+2, DEX_SCORE));
            p.AddEffect(new Effect("ring").AddModifier(AC, new Modifier(2, DEFLECTION)));
            p.AddEffect(new Effect("dragon scales").AddModifier(NATURAL_ARMOR, new Modifier(4, RACIAL)));

            Equipped items = new Equipped();
            CheckStat(17, p, items, AC);
            CheckStat(13, p, items, TOUCH_AC);
        }

        [TestMethod]
        public void TestFlatFootedAC() {
            EffectPool p = Rules();
            Equipped items = new Equipped();

            p.AddEffect(ModifyStat(+2, DEX_SCORE));
            CheckStat(11, p, items, AC);
            p.AddEffect(new Effect("ff").AddFlag(FLAT_FOOTED));
            CheckStat(10, p, items, AC);
            p.AddEffect(new Effect("ud").AddFlag(UNCANNY_DODGE));
            CheckStat(11, p, items, AC);
        }

        private Shield HeavyShield() {
            return new Shield {
                Name = "heavy steel shield",
                ShieldBonus = 2,
                ArmorCheckPenalty = -2,
                ArcaneSpellFailure = 15,
                ShieldBashEffort = Weapon.Effort.ONE_HANDED,
                ShieldBash = new AttackMode {
                    ProficencyCategory = Weapon.Proficiency.MARTIAL,
                    DamageDieMedium = (1, 4),
                    DamageType = BLUDGEONING
                }
            };
        }

        [TestMethod]
        public void TestShield() {
            EffectPool p = Rules();

            Equipped items = new Equipped {
                OffHandShield = HeavyShield()
            };
            CheckStat(12, p, items, AC);
            CheckAttack(+0, "1d4", p, items);

            items.PrimaryHandWeapon = Longsword();
            CheckAttack(
                new int[] { +0, +0, -6, -10 },
                new string[] { "1d4", "1d8", "1d8", "1d4" },
                p,
                items
            );
        }

        public Armor Breastplate() {
            return new Armor {
                ArmorWeight = Armor.Weight.MEDIUM,
                Name = "breastplate",
                ArmorBonus = 5,
                MaxDexBonus = 3,
                ArmorCheckPenalty = -4,
                ArcaneSpellFailure = 25
            };
        }

        [TestMethod]
        public void TestArmor() {
            EffectPool p = Rules();
            Armor breastplate = Breastplate();
            Equipped items = new Equipped {
                DonnedArmor = breastplate
            };
            p.AddEffect(ModifyStat(+30, DEX_SCORE)); // 40 dex, but for AC we can only use +3
            CheckStat(18, p, items, AC);

            breastplate.ArmorSpikes = new AttackMode {
                ProficencyCategory = Weapon.Proficiency.MARTIAL,
                DamageDieMedium = (1, 6),
                DamageType = PIERCING
            };
            CheckAttack(+0, "1d6", p, items);

            items.PrimaryHandWeapon = Longsword();
            CheckAttack(
                new int[] { +0, +0, -4, -8 },
                new string[] { "1d6", "1d8", "1d8", "1d6" },
                p,
                items
            );
        }

        [TestMethod]
        public void TestDeniedDex() {
            EffectPool p = Rules();
            Equipped items = new Equipped();

            p.AddEffect(ModifyStat(+2, DEX_SCORE));
            CheckStat(11, p, items, AC);
            p.AddEffect(new Effect("deny").AddFlag(DENIED_DEX));
            CheckStat(10, p, items, AC);
            p.AddEffect(new Effect("ud").AddFlag(UNCANNY_DODGE));
            CheckStat(10, p, items, AC);
        }

        private Armor Chainmail() {
            return new Armor {
                ArmorWeight = Armor.Weight.MEDIUM,
                Name = "chainmail",
                ArmorBonus = +5,
                MaxDexBonus = +2,
                ArmorCheckPenalty = -5,
                ArcaneSpellFailure = 30
            };
        }

        [TestMethod]
        public void TestACP() {
            EffectPool p = Rules();
            Equipped items = new Equipped {
                DonnedArmor = Chainmail()
            };

            CheckStat(-5, p, items, BALANCE);
            CheckStat(-5, p, items, RIDE);
            CheckStat(-10, p, items, SWIM);
            CheckStat(0, p, items, DIPLOMACY);

            items.OffHandShield = HeavyShield();
            CheckStat(-7, p, items, BALANCE);
            CheckStat(-7, p, items, RIDE);
            CheckStat(-14, p, items, SWIM);
            CheckStat(0, p, items, DIPLOMACY);

            p.AddEffect(new Effect("Medium armor proficiency")
                .AddProficiency(Armor.Weight.MEDIUM)
            );
            CheckStat(-7, p, items, BALANCE);
            CheckStat(-2, p, items, RIDE);
            CheckStat(-14, p, items, SWIM);
            CheckStat(0, p, items, DIPLOMACY);

            p.AddEffect(new Effect("Shield proficiency")
                .AddShieldProficiency(false)
            );
            CheckStat(-7, p, items, BALANCE);
            CheckStat(0, p, items, RIDE);
            CheckStat(-14, p, items, SWIM);
            CheckStat(0, p, items, DIPLOMACY);
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
            Equipped items = new Equipped();

            Weapon dagger = Dagger();
            dagger.Tags.Add("shadow hand");
            items.PrimaryHandWeapon = dagger;
            
            List<Attack> daggerAttacks = Attack.Generate(p, 0, items);
            Assert.AreEqual(Weapon.Range.MELEE, daggerAttacks[0].Range);
            Assert.AreEqual(+3, daggerAttacks[0].AttackBonus.bonus);
            Assert.AreEqual("1d4 + 3", daggerAttacks[0].Damage.dice.ToString());
            Assert.AreEqual(Weapon.Range.THROWN, daggerAttacks[1].Range);
            Assert.AreEqual(+5, daggerAttacks[1].AttackBonus.bonus);
            Assert.AreEqual("1d4 + 3", daggerAttacks[1].Damage.dice.ToString());

            p.AddEffect(new Effect("discipline focus")
                .AddAttackModifier(new AttackMatcher().Tag("shadow hand"), new Modifier(+1, UNTYPED))
            );
            daggerAttacks = Attack.Generate(p, 0, items);
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
            Equipped items = new Equipped() {
                PrimaryHandWeapon = bow,
                OffHandWeapon = bow
            };
            
            List<Attack> attack = Attack.Generate(p, 0, items);
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
            Equipped items = new Equipped() {
                PrimaryHandWeapon = bow,
                OffHandWeapon = bow
            };
            
            List<Attack> attack = Attack.Generate(p, 0, items);
            Assert.AreEqual(1, attack.Count());
            Assert.AreEqual(+3, attack[0].AttackBonus.bonus);
            Assert.AreEqual("1d6 + 3", attack[0].Damage.dice.ToString());
        }

        [TestMethod]
        public void TestFearStacking() {
            EffectPool p = Rules();
            p.AddEffect(ModifyStat(+10, DEX_SCORE));

            Equipped items = new Equipped();
            CheckStat(+5, p, items, DEX_CHECKS);
            p.AddEffect(ModifyStat(1, FEAR_LEVEL));
            CheckStat(+3, p, items, DEX_CHECKS);
        }

        [TestMethod]
        public void TestSpecialSizeModifier() {
            EffectPool p = Rules();
            p.AddEffect(ModifyStat(+10, DEX_SCORE));
            Equipped items = new Equipped();

            // no ranks in hide, +5 dex +0 str +0 bab, medium size:  +5 hide, +0 grapple
            CheckStat(+5, p, items, HIDE);
            CheckStat(+0, p, items, GRAPPLE);

            // colossal size, which gives -16 hide +16 grapple
            p.AddEffect(ModifyStat(4, SIZE_CATEGORY));

            CheckStat(-11, p, items, HIDE);
            CheckStat(+16, p, items, GRAPPLE);
        }

        [TestMethod]
        public void TestReachLookup() {
            EffectPool p = Rules();
            Equipped items = new Equipped();

            CheckStat(5, p, items, REACH);
            p.AddEffect(ModifyStat(4, SIZE_CATEGORY));
            CheckStat(30, p, items, REACH);
        }

        [TestMethod]
        public void TestSpeedModifierToJump() {
            EffectPool p = Rules();
            Equipped items = new Equipped();

            // no ranks, +0 ability, no speed modifier.
            CheckStat(+0, p, items, JUMP);

            Effect speedModifier = ModifyStat(+5, LAND_SPEED);
            p.AddEffect(speedModifier);
            CheckStat(+0, p, items, JUMP);

            p.AddEffect(speedModifier);
            CheckStat(+4, p, items, JUMP);
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
            Equipped items = new Equipped() {
                PrimaryHandWeapon = Dagger()
            };

            p.AddEffect(ModifyStat(+2, SIZE_CATEGORY));
            Assert.AreEqual("1d8", Attack.Generate(p, +2, items)[0].Damage.dice.ToString());
            p.AddEffect(ModifyStat(+1000, SIZE_CATEGORY));
            Assert.AreEqual("3d6", Attack.Generate(p, +1002, items)[0].Damage.dice.ToString());
            p.AddEffect(ModifyStat(-2002, SIZE_CATEGORY));
            Assert.AreEqual("—", Attack.Generate(p, -1000, items)[0].Damage.dice.ToString());
        }

        [TestMethod]
        public void TestExtraDamageEffects() {
            EffectPool p = Rules();
            Equipped items = new Equipped() {
                PrimaryHandWeapon = Dagger()
            };
            
            Attack daggerMelee = Attack.Generate(p, 0, items)[0];
            var (normalDice, normalExpl) = daggerMelee.Damage;
            Assert.AreEqual("1d4", normalDice.ToString());

            p.AddEffect(new Effect("Sneak attack")
                .AddExtraDice(AttackMatcher.ALL, 2, 6)
            );
            daggerMelee = Attack.Generate(p, 0, items)[0];
            var (sneakDice, sneakExpl) = daggerMelee.Damage;
            Assert.AreEqual("2d6 + 1d4", sneakDice.ToString());

            p.AddEffect(ModifyStat(+8, STR_SCORE));

            p.AddEffect(new Effect("inspire courage")
                .AddDamageModifier(AttackMatcher.ALL, new Modifier(+1, MORALE))
            );
            p.AddEffect(new Effect("prayer")
                .AddDamageModifier(AttackMatcher.ALL, new Modifier(+1, LUCK))
            );

            p.AddEffect(ModifyStat(+4, DEX_SCORE));
            p.AddEffect(new Effect("Hit-and-run fighter")
                .AddDamageModifier(AttackMatcher.ALL, new Modifier((DEX_MODIFIER), CIRCUMSTANCE))
            );

            items.PrimaryHandWeapon = items.OffHandWeapon = Greatsword();

            List<Attack> greatswordAttacks = Attack.Generate(p, 0, items);
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
            Equipped items = new Equipped();
            items.PrimaryHandWeapon = items.OffHandWeapon = dagger;

            var daggerAttacks = Attack.Generate(p, 0, items);
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
            var allText = p.GetTextEffects(new Equipped());

            List<string> senses = allText[SENSES];
            Assert.AreEqual(2, senses.Count());
            Assert.AreEqual("darkvision 60 ft.", senses[0]);
            Assert.AreEqual("low-light vision", senses[1]);

            List<string> uncategorized = allText[UNCATEGORIZED];
            Assert.AreEqual(1, uncategorized.Count());
            Assert.AreEqual("[new category] something something", uncategorized[0]);
        }

        private void CheckAttack(int[] expAtkBonus, string[] expDamage, EffectPool r, Equipped items) {
            List<Attack> attacks = Attack.Generate(r, 0, items);
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

        private void CheckAttack(int expAtkBonus, string expDamage, EffectPool r, Equipped items) {
            CheckAttack(new int[] { expAtkBonus }, new string[] { expDamage }, r, items);
        }

        [TestMethod]
        public void TestControllableAmount() {
            EffectPool p = Rules();

            ControllableAmount powerAttackAmount = new ControllableAmount("Power attack penalty")
                .Minimum(0)
                .Maximum((BASE_ATTACK_BONUS));

            p.AddEffect(new Effect("Power Attack")
                .AddAttackModifier(
                    new AttackMatcher().Melee(),
                    new Modifier(new MultiplyAmount(-1, powerAttackAmount), UNTYPED)
                )
                .AddDamageModifier(
                    new AttackMatcher().TwoHandedAttack(),
                    new Modifier(new MultiplyAmount(2, powerAttackAmount), UNTYPED)
                )
                .AddDamageModifier(
                    new AttackMatcher().OneHandedWeapon().OneHandedAttack(),
                    new Modifier(powerAttackAmount, UNTYPED)
                )
            );

            p.AddEffect(ModifyStat(+2, BASE_ATTACK_BONUS));
            Weapon dagger = Dagger(false);
            Weapon longsword = Longsword();

            Equipped daggerPri = new Equipped {
                PrimaryHandWeapon = dagger
            };
            Equipped longswordPri = new Equipped {
                PrimaryHandWeapon = longsword
            };
            Equipped longswordTwo = new Equipped {
                PrimaryHandWeapon = longsword,
                OffHandWeapon = longsword
            };

            // Just BAB -- attack at +2, no damage bonus.
            CheckAttack(+2, "1d4", p, daggerPri);
            CheckAttack(+2, "1d8", p, longswordPri);
            CheckAttack(+2, "1d8", p, longswordTwo);

            // Power attack for 1 -- attack at +1... light +0 damage, 1h +1 damage, 2h +2 damage
            powerAttackAmount.Amt = 1;
            CheckAttack(+1, "1d4", p, daggerPri);
            CheckAttack(+1, "1d8 + 1", p, longswordPri);
            CheckAttack(+1, "1d8 + 2", p, longswordTwo);

            // Power attack for "3", clamped to 2 -- attack at +0... light +0 damage, 1h +2 damage, 2h +4 damage
            powerAttackAmount.Amt = 3;
            CheckAttack(+0, "1d4", p, daggerPri);
            CheckAttack(+0, "1d8 + 2", p, longswordPri);
            CheckAttack(+0, "1d8 + 4", p, longswordTwo);

            // Power attack for "-1", clamped to 0, same as no power attack
            powerAttackAmount.Amt = -1;
            CheckAttack(+2, "1d4", p, daggerPri);
            CheckAttack(+2, "1d8", p, longswordPri);
            CheckAttack(+2, "1d8", p, longswordTwo);
        }

        [TestMethod]
        public void TestLocalWeaponProperty() {
            Weapon dagger = Dagger(false);
            dagger.Melee?.atk.Properties.Add(WeaponProperty.PLUS[2]);
            CheckAttack(+2, "1d4 + 2", Rules(), new Equipped { PrimaryHandWeapon = dagger });
        }

        [TestMethod]
        public void TestGlobalWeaponProperty() {
            EffectPool p = Rules();

            Weapon dagger = Dagger(false);
            dagger.Melee?.atk.Properties.Add(new WeaponProperty("warning") {
                GlobalEffect = new Effect("warning dagger")
                    .AddModifier(INITIATIVE, new Modifier(+5, INSIGHT))
            });
            Equipped items = new Equipped() {
                PrimaryHandWeapon = dagger
            };
            
            CheckStat(+5, p, items, INITIATIVE);
        }

        [TestMethod]
        public void TestTwfPenalties() {
            EffectPool p = Rules();

            Weapon priSword = Longsword();
            Weapon offSword = Longsword();
            Weapon offDagger = Dagger(false);

            Equipped twoSwords = new Equipped {
                PrimaryHandWeapon = priSword,
                OffHandWeapon = offSword
            };
            Equipped swordDagger = new Equipped {
                PrimaryHandWeapon = priSword,
                OffHandWeapon = offDagger
            };

            CheckAttack(
                new int[] { +0, -6, -10 },
                new string[] { "1d8", "1d8", "1d8" },
                p,
                twoSwords
            );

            CheckAttack(
                new int[] { +0, -4, -8 },
                new string[] { "1d8", "1d8", "1d4" },
                p,
                swordDagger
            );
            p.AddEffect(new Effect("Two-Weapon Fighting")
                .AddAttackModifier(new AttackMatcher().PrimaryHandTwfAttack(), new Modifier(+2, UNTYPED))
                .AddAttackModifier(new AttackMatcher().OffHandAttack(), new Modifier(+6, UNTYPED))
            );

            CheckAttack(
                new int[] { +0, -4, -4 },
                new string[] { "1d8", "1d8", "1d8" },
                p,
                twoSwords
            );

            CheckAttack(
                new int[] { +0, -2, -2 },
                new string[] { "1d8", "1d8", "1d4" },
                p,
                swordDagger
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
                SecondHead = (
                    new AttackMode {
                        ProficencyCategory = Weapon.Proficiency.SIMPLE,
                        DamageDieMedium = (1, 4),
                        DamageType = BLUDGEONING,
                        ThreatRange = 20,
                        CriticalMultiplier = 2,
                        RangeIncrement = 0
                    },
                    false
                )
            };

            CheckAttack(
                new int[] { +2, +0, +0, +0, +0 },
                new string[] { "1d4 + 3", "1d4 + 2", "1d4 + 1", "1d4 + 2", "1d4 + 1" },
                p,
                new Equipped {
                    PrimaryHandWeapon = quarterstaff,
                    OffHandWeapon = quarterstaff
                }
            );
        }

        private Weapon Waraxe() {
            return new Weapon("dwarven waraxe", 0, Weapon.Effort.ONE_HANDED) {
                Melee = (
                    new AttackMode {
                        ProficencyCategory = Weapon.Proficiency.EXOTIC,
                        DamageDieMedium = (1, 10),
                        DamageType = SLASHING,
                        ThreatRange = 3
                    },
                    false
                )
            };
        }

        [TestMethod]
        public void TestNonProficientWeapon() {
            EffectPool p = Rules();

            Weapon axe = Waraxe();
            Equipped items = new Equipped {
                PrimaryHandWeapon = axe
            };
            CheckAttack(-4, "1d10", p, items);

            p.AddEffect(new Effect("Exotic weapon proficiency")
                .AddProficiency(new AttackMatcher().Names(axe.Name))
            );
            CheckAttack(+0, "1d10", p, items);
        }

        [TestMethod]
        public void TestExtraAttacks() {
            EffectPool p = Rules();
            p.AddEffect(new Effect("haste")
                .AddExtraAttack(AttackMatcher.ALL_EXCEPT_OFF_HAND, new ExtraAttack(-0, ENHANCEMENT))
                .AddAttackModifier(AttackMatcher.ALL, new Modifier(+1, UNTYPED))
                .AddModifier(AC, new Modifier(+1, DODGE))
                .AddModifier(REF_SAVES, new Modifier(+1, DODGE))
                .AddModifier(ALL_SPEEDS, new Modifier(+30, ENHANCEMENT)) // not bothering with the max here
            );

            Weapon sword = Longsword();
            sword.Melee?.atk.Properties.Add(new WeaponProperty("speed") {
                ExtraAttack = new ExtraAttack(-0, ENHANCEMENT)
            });
            Equipped items = new Equipped {
                PrimaryHandWeapon = sword
            };

            List<Attack> attacks = Attack.Generate(p, 0, items);
            Assert.AreEqual(1, attacks.Count());
            Assert.AreEqual(+1, attacks[0].AttackBonus.bonus);
            Assert.AreEqual(1, attacks[0].ExtraAttacks.Count());
            Assert.AreEqual(-0, attacks[0].ExtraAttacks[0]);
        }

        [TestMethod]
        public void TestIteratives() {
            EffectPool p = Rules();
            p.AddEffect(ModifyStat(+16, BASE_ATTACK_BONUS));
            Equipped items = new Equipped {
                PrimaryHandWeapon = Longsword()
            };

            List<Attack> attacks = Attack.Generate(p, 0, items);
            Assert.AreEqual(1, attacks.Count());
            Assert.AreEqual(+16, attacks[0].AttackBonus.bonus);
            Assert.AreEqual(3, attacks[0].ExtraAttacks.Count());
            Assert.AreEqual(-5, attacks[0].ExtraAttacks[0]);
            Assert.AreEqual(-10, attacks[0].ExtraAttacks[1]);
            Assert.AreEqual(-15, attacks[0].ExtraAttacks[2]);
        }
    }
}