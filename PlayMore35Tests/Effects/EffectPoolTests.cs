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
        [TestMethod()]
        public void TestOverlapping() {
            EffectPool pool = new EffectPool();

            string FEAR = "fear";
            Effect bless = new Effect("bless");
            bless.AddModifier(new Modifier(+1, MORALE, "attack rolls"));
            bless.AddModifier(new Modifier(+1, MORALE, ALL_SAVES + VS + FEAR));
            pool.AddEffect(bless);

            Statistic willSavesVsFear = new Statistic(WILL_SAVES + VS + FEAR);
            willSavesVsFear.AddTag(ALL_SAVES + VS + FEAR);
            willSavesVsFear.AddTag(WILL_SAVES);
            willSavesVsFear.AddTag(ALL_SAVES);
            Assert.AreEqual(+1, willSavesVsFear.GetValue(pool));

            Effect inspireCourage = new Effect("inspire courage");
            inspireCourage.AddModifier(new Modifier(+1, MORALE, ATTACK_ROLLS));
            inspireCourage.AddModifier(new Modifier(+1, MORALE, DAMAGE_ROLLS));
            inspireCourage.AddModifier(new Modifier(+1, MORALE, ALL_SAVES + VS + FEAR));
            pool.AddEffect(inspireCourage);
            Assert.AreEqual(+1, willSavesVsFear.GetValue(pool));

            Effect weaponFocus = new Effect("Weapon Focus: longsword");
            weaponFocus.AddModifier(new Modifier(+1, UNTYPED, ATTACK_ROLLS + WITH + "longsword"));
            pool.AddEffect(weaponFocus);

            Statistic longswordAttack = new Statistic(ATTACK_ROLLS + WITH + "longsword");
            longswordAttack.AddTag(ATTACK_ROLLS);
            Assert.AreEqual(+2, longswordAttack.GetValue(pool));

            Statistic shortSwordAttack = new Statistic(ATTACK_ROLLS + WITH + "short sword");
            shortSwordAttack.AddTag(ATTACK_ROLLS);
            Assert.AreEqual(+1, shortSwordAttack.GetValue(pool));
        }

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

            return rules;
        }

        private void CheckStat(int expected, GameRules.GameRules r, EffectPool p, string stat) {
            Assert.AreEqual(expected, r.Stats()[stat].GetValue(p));
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
            p.AddEffect(new Effect("somethin").AddModifier(new Modifier(4, UNTYPED, ABILITY_CHECKS)));
            CheckStat(+4, r, p, INITIATIVE);
        }

        [TestMethod()]
        public void TestAC() {
            GameRules.GameRules r = Rules();
            EffectPool p = r.Rules;

            p.SetValue(12, DEX_SCORE);
            p.AddEffect(new Effect("ring").AddModifier(new Modifier(2, DEFLECTION, AC)));
            p.AddEffect(new Effect("dragon scales").AddModifier(new Modifier(4, UNTYPED, NATURAL_ARMOR)));

            CheckStat(17, r, p, AC);
            CheckStat(13, r, p, TOUCH_AC);
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

        [TestMethod()]
        public void TestMeleeWeapon() {
            GameRules.GameRules r = Rules();
            EffectPool p = r.Rules;
            p.SetValue(16, STR_SCORE);

            Items.Weapon dagger = new Items.Weapon(); // dagger is the default set of stats including name, no changes required.
            dagger.Attacks.Add(new Items.Attack(dagger) {
                OffHand = true
            });
            dagger.Tags.Add("shadow hand");
            r.AddWeapon(dagger);
            CheckStat(+3, r, p, MELEE + ATTACK_ROLLS + WITH + dagger.Name);
            CheckStat(+1, r, p, MELEE + DAMAGE_ROLLS + WITH + dagger.Name);

            p.AddEffect(new Effect("discipline focus")
                .AddModifier(new Modifier(+1, UNTYPED, ATTACK_ROLLS + WITH + "shadow hand" + WEAPONS))
            );
            CheckStat(+4, r, p, MELEE + ATTACK_ROLLS + WITH + dagger.Name);
        }

        [TestMethod()]
        public void TestEnhancementBonus() {
            GameRules.GameRules r = Rules();
            EffectPool p = r.Rules;
            p.SetValue(16, STR_SCORE);

            Items.Weapon dagger = new Items.Weapon() {
                Name = "+1 dagger"
            };
            p.AddEffect(new Effect("+1 dagger enhancement bonus")
                .AddModifier(new Modifier(+1, ENHANCEMENT, ATTACK_ROLLS + WITH + dagger.Name))
                .AddModifier(new Modifier(+1, ENHANCEMENT, DAMAGE_ROLLS + WITH + dagger.Name))
            );
            dagger.Attacks.Add(new Items.Attack(dagger));
            r.AddWeapon(dagger);
            CheckStat(+4, r, p, MELEE + ATTACK_ROLLS + WITH + dagger.Name);
            CheckStat(+4, r, p, MELEE + DAMAGE_ROLLS + WITH + dagger.Name);
        }

        [TestMethod()]
        public void TestCompositeBowStrong() {
            GameRules.GameRules r = Rules();
            EffectPool p = r.Rules;
            p.SetValue(16, STR_SCORE);
            p.SetValue(20, DEX_SCORE);

            Items.Weapon compositeBow = new Items.Weapon {
                Name = "Composite shortbow (+2 Str)"
            };
            compositeBow.Attacks.Add(new Items.Attack(compositeBow) {
                ProficencyCategory = Items.Attack.Proficiency.MARTIAL,
                EffortCategory = Items.Attack.Effort.ONE_HANDED,
                RangeCategory = Items.Attack.Range.PROJECTILE,
                OffHand = false,
                DamageDiceMedium = new Dictionary<int, int> { [6] = 1 },
                ThreatRange = 20,
                CriticalMultiplier = 3,
                RangeIncrement = 70,
                StrengthRating = +2,
                DamageType = "Piercing"
            });
            r.AddWeapon(compositeBow);
            CheckStat(+5, r, p, RANGED + ATTACK_ROLLS + WITH + compositeBow.Name);
            CheckStat(+2, r, p, RANGED + DAMAGE_ROLLS + WITH + compositeBow.Name);
        }

        [TestMethod()]
        public void TestCompositeBowWeak() {
            GameRules.GameRules r = Rules();
            EffectPool p = r.Rules;
            p.SetValue(16, STR_SCORE);
            p.SetValue(20, DEX_SCORE);

            Items.Weapon compositeBow = new Items.Weapon {
                Name = "Composite shortbow (+4 Str)"
            };
            compositeBow.Attacks.Add(new Items.Attack(compositeBow) {
                ProficencyCategory = Items.Attack.Proficiency.MARTIAL,
                EffortCategory = Items.Attack.Effort.ONE_HANDED,
                RangeCategory = Items.Attack.Range.PROJECTILE,
                OffHand = false,
                DamageDiceMedium = new Dictionary<int, int> { [6] = 1 },
                ThreatRange = 20,
                CriticalMultiplier = 3,
                RangeIncrement = 70,
                StrengthRating = +4,
                DamageType = "Piercing"
            });
            r.AddWeapon(compositeBow);
            CheckStat(+3, r, p, RANGED + ATTACK_ROLLS + WITH + compositeBow.Name);
            CheckStat(+3, r, p, RANGED + DAMAGE_ROLLS + WITH + compositeBow.Name);
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
    }
}