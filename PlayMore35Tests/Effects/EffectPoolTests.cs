using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlayMore35.Effects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Effects.Tests {
    [TestClass()]
    public class EffectPoolTests {
        [TestMethod()]
        public void EffectPoolTest() {
            EffectPool pool = new EffectPool();

            Effect bless = new Effect();
            bless.AddPart(new EffectPart(+1, "morale", "attack rolls"));
            bless.AddPart(new EffectPart(+1, "morale", "saves vs fear"));
            pool.AddEffect(bless);

            Statistic willSavesVsFear = new Statistic("Will saves vs fear");
            willSavesVsFear.AddTag("saves vs fear");
            willSavesVsFear.AddTag("Will saves");
            willSavesVsFear.AddTag("All saves");
            Assert.AreEqual(+1, willSavesVsFear.GetValue(pool));

            Effect inspireCourage = new Effect();
            inspireCourage.AddPart(new EffectPart(+1, "morale", "attack rolls"));
            inspireCourage.AddPart(new EffectPart(+1, "morale", "damage rolls"));
            inspireCourage.AddPart(new EffectPart(+1, "morale", "saves vs charm"));
            inspireCourage.AddPart(new EffectPart(+1, "morale", "saves vs fear"));
            pool.AddEffect(inspireCourage);
            Assert.AreEqual(+1, willSavesVsFear.GetValue(pool));

            Effect weaponFocus = new Effect();
            weaponFocus.AddPart(new EffectPart(+1, "untyped", "attack rolls with longsword"));
            pool.AddEffect(weaponFocus);

            Statistic longswordAttack = new Statistic("attack rolls with longsword");
            longswordAttack.AddTag("attack rolls");
            Assert.AreEqual(+2, longswordAttack.GetValue(pool));

            Statistic shortSwordAttack = new Statistic("attack rolls with short sword");
            shortSwordAttack.AddTag("attack rolls");
            Assert.AreEqual(+1, shortSwordAttack.GetValue(pool));
        }
    }
}