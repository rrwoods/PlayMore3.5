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
            bless.AddPart(new EffectPart(+1, "morale", "attack rolls"));
            bless.AddPart(new EffectPart(+1, "morale", "saves vs fear"));
            pool.AddEffect(bless);

            Statistic willSavesVsFear = new Statistic("Will saves vs fear");
            willSavesVsFear.AddTag("saves vs fear");
            willSavesVsFear.AddTag("Will saves");
            willSavesVsFear.AddTag("All saves");
            Assert.AreEqual(+1, willSavesVsFear.GetValue(pool));

            Effect inspireCourage = new Effect("inspire courage");
            inspireCourage.AddPart(new EffectPart(+1, "morale", "attack rolls"));
            inspireCourage.AddPart(new EffectPart(+1, "morale", "damage rolls"));
            inspireCourage.AddPart(new EffectPart(+1, "morale", "saves vs charm"));
            inspireCourage.AddPart(new EffectPart(+1, "morale", "saves vs fear"));
            pool.AddEffect(inspireCourage);
            Assert.AreEqual(+1, willSavesVsFear.GetValue(pool));

            Effect weaponFocus = new Effect("Weapon Focus: longsword");
            weaponFocus.AddPart(new EffectPart(+1, "untyped", "attack rolls with longsword"));
            pool.AddEffect(weaponFocus);

            Statistic longswordAttack = new Statistic("attack rolls with longsword");
            longswordAttack.AddTag("attack rolls");
            Assert.AreEqual(+2, longswordAttack.GetValue(pool));

            Statistic shortSwordAttack = new Statistic("attack rolls with short sword");
            shortSwordAttack.AddTag("attack rolls");
            Assert.AreEqual(+1, shortSwordAttack.GetValue(pool));
        }

        [TestMethod()]
        public void TestComputeAbilityModifier() {
            EffectPool pool = new EffectPool();
            
            Statistic strengthScore = new Statistic("STR");
            Effect assignedScore = new Effect("Assigned STR")
                .AddPart(new EffectPart(7, "untyped", "STR"));
            pool.AddEffect(assignedScore);

            Statistic strengthMod = new Statistic("STR mod");
            EffectPart fromScore = new EffectPart("untyped", "STR mod")
                .AddAmount(new VariableAmount(strengthScore).Halve())
                .AddAmount(-5);
            Effect rawMod = new Effect("Base STR mod")
                .AddPart(fromScore);
            pool.AddEffect(rawMod);

            Assert.AreEqual(-2, strengthMod.GetValue(pool));
        }

        [TestMethod()]
        public void TestConditionalEffects() {
            GameRules.GameRules r = new GameRules.GameRules();
            EffectPool rules = r.Rules;
            rules.SetValue(5, "Tumble ranks");

            // make all the ability modifiers zero
            rules.SetValue(10, "STR score");
            rules.SetValue(10, "DEX score");
            rules.SetValue(10, "CON score");
            rules.SetValue(10, "INT score");
            rules.SetValue(10, "WIS score");
            rules.SetValue(10, "CHA score");

            Assert.AreEqual(+2, new Statistic("Jump").GetValue(rules));
            Assert.AreEqual(+5, new Statistic("Tumble").GetValue(rules));
        }
    }
}