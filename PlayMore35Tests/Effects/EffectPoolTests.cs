using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlayMore35.Effects;
using PlayMore35.GameRules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Effects.Tests {
    [TestClass()]
    public class EffectPoolTests {
        [TestMethod()]
        public void TestOverlapping() {
            EffectPool pool = new EffectPool();

            Effect bless = new Effect("bless");
            bless.AddModifier(new Modifier(+1, "morale", "attack rolls"));
            bless.AddModifier(new Modifier(+1, "morale", "saves vs fear"));
            pool.AddEffect(bless);

            Statistic willSavesVsFear = new Statistic("Will saves vs fear");
            willSavesVsFear.AddTag("saves vs fear");
            willSavesVsFear.AddTag("Will saves");
            willSavesVsFear.AddTag("All saves");
            Assert.AreEqual(+1, willSavesVsFear.GetValue(pool));

            Effect inspireCourage = new Effect("inspire courage");
            inspireCourage.AddModifier(new Modifier(+1, "morale", "attack rolls"));
            inspireCourage.AddModifier(new Modifier(+1, "morale", "damage rolls"));
            inspireCourage.AddModifier(new Modifier(+1, "morale", "saves vs charm"));
            inspireCourage.AddModifier(new Modifier(+1, "morale", "saves vs fear"));
            pool.AddEffect(inspireCourage);
            Assert.AreEqual(+1, willSavesVsFear.GetValue(pool));

            Effect weaponFocus = new Effect("Weapon Focus: longsword");
            weaponFocus.AddModifier(new Modifier(+1, "untyped", "attack rolls with longsword"));
            pool.AddEffect(weaponFocus);

            Statistic longswordAttack = new Statistic("attack rolls with longsword");
            longswordAttack.AddTag("attack rolls");
            Assert.AreEqual(+2, longswordAttack.GetValue(pool));

            Statistic shortSwordAttack = new Statistic("attack rolls with short sword");
            shortSwordAttack.AddTag("attack rolls");
            Assert.AreEqual(+1, shortSwordAttack.GetValue(pool));
        }

        private GameRules.GameRules Rules() {
            GameRules.GameRules rules = new GameRules.GameRules();
            EffectPool p = rules.Rules;

            // make all the ability modifiers zero
            p.SetValue(10, "STR score");
            p.SetValue(10, "DEX score");
            p.SetValue(10, "CON score");
            p.SetValue(10, "INT score");
            p.SetValue(10, "WIS score");
            p.SetValue(10, "CHA score");

            return rules;
        }

        private void CheckStat(int expected, GameRules.GameRules r, EffectPool p, string stat) {
            Assert.AreEqual(expected, r.Stats[stat].GetValue(p));
        }

        [TestMethod()]
        public void TestComputeAbilityModifier() {
            GameRules.GameRules r = Rules();
            EffectPool p = r.Rules;

            p.SetValue(7, "STR score");
            CheckStat(-2, r, p, "STR modifier");
            CheckStat(0, r, p, "DEX modifier");
        }

        [TestMethod()]
        public void TestConditionalEffects() {
            GameRules.GameRules r = Rules();
            EffectPool p = r.Rules;

            p.SetValue(5, "Tumble ranks");
            CheckStat(+2, r, p, "Jump");
            CheckStat(+5, r, p, "Tumble");
        }

        [TestMethod()]
        public void TestInitiativeChaining() {
            GameRules.GameRules r = Rules();
            EffectPool p = r.Rules;
            p.AddEffect(new Effect("somethin").AddModifier(new Modifier(4, "untyped", "Ability checks")));
            CheckStat(+4, r, p, "Initiative");
        }

        [TestMethod()]
        public void TestAC() {
            GameRules.GameRules r = Rules();
            EffectPool p = r.Rules;

            p.AddEffect(new Effect("haste").AddModifier(new Modifier(1, "dodge", "AC")));
            p.AddEffect(new Effect("ring").AddModifier(new Modifier(2, "deflection", "AC")));
            p.AddEffect(new Effect("breastplate").AddModifier(new Modifier(4, "armor", "AC")));

            CheckStat(17, r, p, "AC");
            CheckStat(16, r, p, "Flat-footed AC");
            CheckStat(13, r, p, "Touch AC");
            CheckStat(12, r, p, "Flat-footed touch AC");
        }

        [TestMethod()]
        public void TestACP() {
            GameRules.GameRules r = Rules();
            EffectPool p = r.Rules;

            p.SetValue(-5, "Armor check penalty");
            CheckStat(-5, r, p, "Balance");
            CheckStat(-10, r, p, "Swim");
            CheckStat(0, r, p, "Diplomacy");
        }
    }
}