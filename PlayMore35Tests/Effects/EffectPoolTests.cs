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

            p.AddEffect(new Effect("haste").AddModifier(new Modifier(1, DODGE, AC)));
            p.AddEffect(new Effect("ring").AddModifier(new Modifier(2, DEFLECTION, AC)));
            p.AddEffect(new Effect("breastplate").AddModifier(new Modifier(4, ARMOR, AC)));

            CheckStat(17, r, p, AC);
            CheckStat(16, r, p, FLAT_FOOTED_AC);
            CheckStat(13, r, p, TOUCH_AC);
            CheckStat(12, r, p, FLAT_FOOTED_TOUCH_AC);
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
                DamageDieMedium = "1d6",
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
                DamageDieMedium = "1d6",
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
    }
}