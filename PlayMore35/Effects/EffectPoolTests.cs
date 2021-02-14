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

            p.AddEffect(new Effect("Proficiencies")
                .AddProficiency(new AttackMatcher().SimpleOrMartial())
                .AddProficiency(Armor.WeightCategory.LIGHT)
                .AddProficiency(Armor.WeightCategory.MEDIUM)
                .AddShieldProficiency(false)
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
        public void TestSaves() {
            EffectPool r = Rules();
            Equipped items = new Equipped();

            r.AddEffect(new Effect("stuff")
                .AddModifier(CON_SCORE, new Modifier(4, UNTYPED)) // +2 fort
                .AddModifier(DEX_SCORE, new Modifier(8, UNTYPED)) // +4 reflex
                .AddModifier(WIS_SCORE, new Modifier(12, UNTYPED)) // +6 will
            );

            CheckStat(+2, r, items, FORT_SAVES);
            CheckStat(+4, r, items, REF_SAVES);
            CheckStat(+6, r, items, WILL_SAVES);
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

        public Armor Breastplate() {
            return new Armor {
                Type = Armor.WeightCategory.MEDIUM,
                Name = "breastplate",
                ArmorBonus = 5,
                MaxDexBonus = 3,
                ArmorCheckPenalty = -4,
                ArcaneSpellFailure = 25
            };
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

        private Armor FullPlate() {
            return new Armor {
                Type = Armor.WeightCategory.HEAVY,
                Name = "full plate",
                ArmorBonus = +8,
                MaxDexBonus = +1,
                ArmorCheckPenalty = -6,
                ArcaneSpellFailure = 35
            };
        }

        private Shield TowerShield() {
            return new Shield {
                Name = "tower shield",
                ShieldBonus = +4,
                Tower = true,
                MaxDexBonus = +2,
                ArmorCheckPenalty = -10,
                ArcaneSpellFailure = 50
            };
        }

        [TestMethod]
        public void TestACP() {
            EffectPool p = Rules();
            Equipped items = new Equipped {
                DonnedArmor = FullPlate()
            };

            CheckStat(-6, p, items, BALANCE);
            CheckStat(-6, p, items, RIDE);
            CheckStat(-12, p, items, SWIM);
            CheckStat(0, p, items, DIPLOMACY);

            items.OffHandShield = TowerShield();
            CheckStat(-16, p, items, BALANCE);
            CheckStat(-16, p, items, RIDE);
            CheckStat(-32, p, items, SWIM);
            CheckStat(0, p, items, DIPLOMACY);

            p.AddEffect(new Effect("Heavy armor proficiency")
                .AddProficiency(Armor.WeightCategory.HEAVY)
            );
            CheckStat(-16, p, items, BALANCE);
            CheckStat(-10, p, items, RIDE);
            CheckStat(-32, p, items, SWIM);
            CheckStat(0, p, items, DIPLOMACY);

            p.AddEffect(new Effect("Shield proficiency")
                .AddShieldProficiency(true)
            );
            CheckStat(-16, p, items, BALANCE);
            CheckStat(0, p, items, RIDE);
            CheckStat(-32, p, items, SWIM);
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
        public void TestLoadAmounts() {
            EffectPool p = Rules();
            Equipped noItems = new Equipped();
            CheckStat(33, p, noItems, LIGHT_LOAD);
            CheckStat(66, p, noItems, MEDIUM_LOAD);
            CheckStat(100, p, noItems, HEAVY_LOAD);

            p.AddEffect(ModifyStat(+30, STR_SCORE));
            CheckStat(2133, p, noItems, LIGHT_LOAD);
            CheckStat(4266, p, noItems, MEDIUM_LOAD);
            CheckStat(6400, p, noItems, HEAVY_LOAD);
        }

        [TestMethod]
        public void TestDiceBasic() {
            DiceGroup dice = new DiceGroup(1, 4);
            Assert.AreEqual("1d4", dice.ToString());

            dice.AddDice(2, 4);
            Assert.AreEqual("3d4", dice.ToString());

            dice.AddDice(5, 6);
            Assert.AreEqual("5d6+3d4", dice.ToString());

            dice.AddDice(4, 1);
            Assert.AreEqual("5d6+3d4+4", dice.ToString());
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
    }
}