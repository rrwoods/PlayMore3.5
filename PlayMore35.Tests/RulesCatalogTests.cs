using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Items;
using PlayMore35.Rules;
using PlayMore35.Subsystems;
using static PlayMore35.Effects.Constants;

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

        [TestMethod]
        public void Catalog_TextEffectCategories_ExistInConstants() {
            var catalog = new GameRulesCatalog();
            var provider = new ResourceSubsystemContentProvider();

            // 1. Invariants: EXPLORATION and SOCIAL must be registered in Constants.TEXT_CATEGORIES
            CollectionAssert.Contains(TEXT_CATEGORIES, EXPLORATION);
            CollectionAssert.Contains(TEXT_CATEGORIES, SOCIAL);

            // 2. Feats catalog: Track and Endurance benefit text effects must have Exploration category
            var track = catalog.GetFeat("Track");
            Assert.IsNotNull(track, "Track feat must be in catalog");
            Assert.AreEqual(EXPLORATION, track.Benefit.Text[0].Category);

            var endurance = catalog.GetFeat("Endurance");
            Assert.IsNotNull(endurance, "Endurance feat must be in catalog");
            Assert.AreEqual(2, endurance.Benefit.Text.Count, "Endurance text effects should be split into 2");
            Assert.AreEqual(EXPLORATION, endurance.Benefit.Text[0].Category);
            Assert.AreEqual(SQ, endurance.Benefit.Text[1].Category);

            // 3. Known valid categories defined in Constants
            var validCategories = new HashSet<string>(TEXT_CATEGORIES, StringComparer.OrdinalIgnoreCase) {
                METAMAGIC,
                MISC_CASTING,
                ITEM_CREATION
            };

            // Assert all text categories loaded from feats.json exist in valid categories
            foreach (var feat in catalog.Feats.Values) {
                foreach (var text in feat.Benefit.Text) {
                    Assert.IsTrue(validCategories.Contains(text.Category), $"Unknown feat category: {text.Category}");
                }
            }

            // Assert all text categories loaded from vestiges.json exist in valid categories
            foreach (var vestige in provider.GetVestiges()) {
                foreach (var text in vestige.Influence.Text) {
                    Assert.IsTrue(validCategories.Contains(text.Category), $"Unknown vestige influence category: {text.Category}");
                }
                foreach (var ability in vestige.Abilities) {
                    foreach (var text in ability.Text) {
                        Assert.IsTrue(validCategories.Contains(text.Category), $"Unknown vestige ability category: {text.Category}");
                    }
                }
            }

            // Assert all text categories loaded from races.json exist in valid categories
            foreach (var race in catalog.Races.Values) {
                foreach (var effect in race.Effects) {
                    foreach (var text in effect.Text) {
                        Assert.IsTrue(validCategories.Contains(text.Category), $"Unknown race category: {text.Category}");
                    }
                }
            }

            // Assert all text categories loaded from rules.json exist in valid categories
            foreach (var effect in catalog.CoreEffects) {
                foreach (var text in effect.Text) {
                    Assert.IsTrue(validCategories.Contains(text.Category), $"Unknown core effect category: {text.Category}");
                }
            }

            // 4. Verify EffectPool produces only categories registered in Constants.TEXT_CATEGORIES
            var pool = new EffectPool();
            pool.GenerateFeatEffects(track, "");
            pool.GenerateFeatEffects(endurance, "");
            var textEffects = pool.GetTextEffects(new Equipped());
            foreach (var category in textEffects.Keys) {
                CollectionAssert.Contains(TEXT_CATEGORIES, category);
            }
            Assert.IsTrue(textEffects.ContainsKey(EXPLORATION));
            Assert.IsTrue(textEffects[EXPLORATION].Any(t => t.Name == "Track"));
            Assert.IsTrue(textEffects[EXPLORATION].Any(t => t.Name == "Endurance"));
        }
    }
}
