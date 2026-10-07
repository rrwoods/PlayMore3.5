using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Rules;

namespace PlayMore35.Tests {
    [TestClass]
    public class RulesCatalogTests {
        [TestMethod]
        public void GameRulesCatalog_LoadsCoreDefinitionsWithoutCharacter() {
            var catalog = new GameRulesCatalog();

            Assert.IsNotNull(catalog.GetRace("Human"), "Human race should be loaded.");
            Assert.IsNotNull(catalog.GetRace(""), "Default empty race should be loaded.");
            Assert.IsNotNull(catalog.GetClass("Fighter"), "Fighter class should be loaded.");
            Assert.IsNotNull(catalog.GetFeat("Power Attack"), "Power Attack feat should be loaded.");
            Assert.IsNotNull(catalog.GetWeapon("longsword"), "Longsword weapon should be loaded.");
            Assert.IsNotNull(catalog.GetArmor("full plate"), "Full plate armor should be loaded.");
            Assert.IsNotNull(catalog.GetShield("heavy steel shield"), "Heavy steel shield should be loaded.");
            Assert.IsNotNull(catalog.GetItem("panic button"), "Item should be loaded.");

            Assert.IsTrue(catalog.CoreFeatSlots.Count > 0, "Core feat slots should be populated.");
            Assert.IsTrue(catalog.CoreStats.Count > 0, "Core stats should be populated.");
            Assert.IsTrue(catalog.CoreFlags.Count > 0, "Core flags should be populated.");
            Assert.IsTrue(catalog.CoreParameters.Count > 0, "Core parameters should be populated.");
            Assert.IsTrue(catalog.CoreEffects.Count > 0, "Core effects should be populated.");
        }

        [TestMethod]
        public void FlagDefinition_And_ParameterDefinition_StoreOnlyMetadata() {
            var flagJson = new JObject(
                new JProperty("name", "test-flag"),
                new JProperty("category", "test-category"),
                new JProperty("description", "test-description")
            );

            var flagDef = new FlagDefinition(flagJson);
            Assert.AreEqual("test-flag", flagDef.Name);
            Assert.AreEqual("test-category", flagDef.Category);
            Assert.AreEqual("test-description", flagDef.Description);

            // Verify FlagDefinition does not have On property or Character field
            var flagProperties = typeof(FlagDefinition).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            Assert.IsFalse(flagProperties.Any(p => p.Name == "On"), "FlagDefinition must not have On property.");
            var flagFields = typeof(FlagDefinition).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsFalse(flagFields.Any(f => f.FieldType == typeof(Character)), "FlagDefinition must not hold Character reference.");

            var paramJson = new JObject(
                new JProperty("name", "test-param"),
                new JProperty("category", "test-cat"),
                new JProperty("description", "test-desc")
            );

            var paramDef = new ParameterDefinition(paramJson);
            Assert.AreEqual("test-param", paramDef.Name);
            Assert.AreEqual("test-cat", paramDef.Category);
            Assert.AreEqual("test-desc", paramDef.Description);

            // Verify ParameterDefinition does not have Value property or Character field
            var paramProperties = typeof(ParameterDefinition).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            Assert.IsFalse(paramProperties.Any(p => p.Name == "Value"), "ParameterDefinition must not have Value property.");
            var paramFields = typeof(ParameterDefinition).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsFalse(paramFields.Any(f => f.FieldType == typeof(Character)), "ParameterDefinition must not hold Character reference.");
        }

        [TestMethod]
        public void DefinitionClasses_CanBeConstructedWithoutCharacter() {
            var raceJson = new JObject(
                new JProperty("name", "TestRace"),
                new JProperty("type", "Humanoid"),
                new JProperty("flags", new JArray(new JObject(
                    new JProperty("name", "RacialFlag"),
                    new JProperty("category", "Race"),
                    new JProperty("description", "Desc")
                )))
            );

            var race = new Race(raceJson);
            Assert.AreEqual("TestRace", race.Name);
            Assert.AreEqual(1, race.Flags.Count);
            Assert.AreEqual("RacialFlag", race.Flags[0].Name);

            var featJson = new JObject(
                new JProperty("benefit", new JObject(new JProperty("name", "TestFeatEffect"))),
                new JProperty("flags", new JArray(new JObject(
                    new JProperty("name", "FeatFlag"),
                    new JProperty("category", "Feat"),
                    new JProperty("description", "Desc")
                ))),
                new JProperty("parameters", new JArray(new JObject(
                    new JProperty("name", "FeatParam"),
                    new JProperty("category", "Feat"),
                    new JProperty("description", "Desc")
                )))
            );

            var feat = new Feat(featJson);
            Assert.AreEqual("TestFeatEffect", feat.Benefit.Name);
            Assert.AreEqual(1, feat.Flags.Count);
            Assert.AreEqual(1, feat.Parameters.Count);
        }

        [TestMethod]
        public void GameRulesCatalog_ChoicesForKey_ReturnsExpectedOptions() {
            var catalog = new GameRulesCatalog();

            var crossbows = catalog.ChoicesForKey("hand or light crossbow").ToList();
            CollectionAssert.Contains(crossbows, "hand crossbow");
            CollectionAssert.Contains(crossbows, "light crossbow");

            var weapons = catalog.ChoicesForKey("weapon").ToList();
            Assert.IsTrue(weapons.Count > 0);
            CollectionAssert.Contains(weapons, "longsword");

            var martialWeapons = catalog.ChoicesForKey("martial weapon").ToList();
            Assert.IsTrue(martialWeapons.Count > 0);
            CollectionAssert.Contains(martialWeapons, "longsword");

            var exoticWeapons = catalog.ChoicesForKey("exotic weapon").ToList();
            Assert.IsTrue(exoticWeapons.Count > 0);
            CollectionAssert.Contains(exoticWeapons, "bastard sword");

            var skills = catalog.ChoicesForKey("skill").ToList();
            CollectionAssert.Contains(skills, "Jump");
            CollectionAssert.Contains(skills, "Listen");
        }

        [TestMethod]
        public void GameRulesCatalog_IsImmutableAndThreadSafe() {
            var catalog = new GameRulesCatalog();

            Parallel.For(0, 50, _ => {
                var race = catalog.GetRace("Human");
                Assert.IsNotNull(race);
                Assert.AreEqual("human", race.Name);

                var fighter = catalog.GetClass("Fighter");
                Assert.IsNotNull(fighter);
                Assert.AreEqual("Fighter", fighter.Name);

                var weapon = catalog.GetWeapon("longsword");
                Assert.IsNotNull(weapon);
                Assert.AreEqual("longsword", weapon.Name);

                var weapons = catalog.ChoicesForKey("weapon").ToList();
                Assert.IsTrue(weapons.Count > 0);

                var flags = catalog.CoreFlags;
                Assert.IsTrue(flags.ContainsKey("charging"));
            });
        }
    }
}
