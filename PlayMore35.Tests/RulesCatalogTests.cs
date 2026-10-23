using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Global;
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
            foreach (var vestige in catalog.Vestiges.Values) {
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
            var pool = new EffectPool(new Character());
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

        [TestMethod]
        public void VestigesCatalog_InfluenceCategories_MatchSection0Requirements() {
            var catalog = new GameRulesCatalog();

            var amon = catalog.GetVestige("Amon");
            Assert.IsNotNull(amon);
            Assert.AreEqual(CONDITION, amon.Influence.Text[0].Category);

            var aym = catalog.GetVestige("Aym");
            Assert.IsNotNull(aym);
            Assert.AreEqual(SOCIAL, aym.Influence.Text[0].Category);

            var leraje = catalog.GetVestige("Leraje");
            Assert.IsNotNull(leraje);
            Assert.AreEqual(SOCIAL, leraje.Influence.Text[1].Category);

            var naberius = catalog.GetVestige("Naberius");
            Assert.IsNotNull(naberius);
            Assert.AreEqual(SOCIAL, naberius.Influence.Text[0].Category);

            var ronove = catalog.GetVestige("Ronove");
            Assert.IsNotNull(ronove);
            Assert.AreEqual(2, ronove.Influence.Text.Count);
            Assert.AreEqual(SOCIAL, ronove.Influence.Text[0].Category);
            Assert.AreEqual(CONDITION, ronove.Influence.Text[1].Category);

            var haagenti = catalog.GetVestige("Haagenti");
            Assert.IsNotNull(haagenti);
            Assert.AreEqual(SOCIAL, haagenti.Influence.Text[0].Category);

            var malphas = catalog.GetVestige("Malphas");
            Assert.IsNotNull(malphas);
            Assert.AreEqual(SOCIAL, malphas.Influence.Text[0].Category);
        }

        // Source: Tome of Magic, pp. 16–50
        [TestMethod]
        public void GameRulesCatalog_LoadsVestiges_FromResources() {
            var catalog = new GameRulesCatalog();

            Assert.IsNotNull(catalog.Vestiges, "Vestiges dictionary must be initialized.");
            Assert.IsTrue(catalog.Vestiges.Count >= 9, $"Expected at least 9 vestiges, got {catalog.Vestiges.Count}");

            var amon = catalog.GetVestige("Amon");
            Assert.IsNotNull(amon, "Amon should be present in catalog.");
            Assert.AreEqual("Amon", amon.Name);
            Assert.AreEqual(1, amon.Level);
            Assert.AreEqual(20, amon.BindingDc);
            Assert.IsNotNull(amon.Influence);
            Assert.IsTrue(amon.Abilities.Count > 0, "Amon should have abilities.");

            // Case-insensitive lookup
            var amonLower = catalog.GetVestige("amon");
            Assert.IsNotNull(amonLower, "Vestige lookup should be case-insensitive.");
            Assert.AreEqual("Amon", amonLower.Name);

            // Missing / null / empty
            Assert.IsNull(catalog.GetVestige("NonExistentVestige"));
            Assert.IsNull(catalog.GetVestige(null));
            Assert.IsNull(catalog.GetVestige(""));
        }

        [TestMethod]
        public void MartialManeuverEngine_ResolvesCatalogFromContext_OnInit() {
            var customManeuver = new SubsystemElement {
                Name = "Special Strike",
                Type = "Strike"
            };
            customManeuver.SetProperty("discipline", "Iron Heart");
            customManeuver.SetProperty("level", 1);
            var customCatalog = new FakeRulesCatalog(customManeuver);

            // Engine constructed with default catalog
            var defaultCatalog = new GameRulesCatalog();
            var engine = new SubsystemEngine(defaultCatalog.GetSubsystem("martial_maneuvers"));
            var context = new FakeCharacterContext(catalog: customCatalog);

            // Before initialize, engine does not know about customManeuver
            Assert.IsNull(engine.GetElement("Special Strike"));

            // Initialize resolves context.Catalog
            engine.Initialize(context);

            var postWarnings = engine.ValidateChoice(null, null, "Special Strike", "Warblade");
            Assert.IsFalse(postWarnings.Any(w => w.Contains("not recognized") || w.Contains("not a valid")), "Initialized engine must recognize maneuver from context.Catalog.");
            Assert.IsNotNull(engine.GetElement("Special Strike"));
        }

        [TestMethod]
        public void PactMagicEngine_ResolvesCatalogFromContext_OnInit() {
            var ability = new Effect("Granted Test Power");
            var influence = new Effect("Test Influence");
            var json = new JObject(
                new JProperty("name", "Test Vestige"),
                new JProperty("level", 1),
                new JProperty("binding DC", 22),
                new JProperty("effects", new JObject(
                    new JProperty("abilities", new JArray(ability.ToJson())),
                    new JProperty("influence", new JArray(influence.ToJson()))
                ))
            );
            var customVestige = new Vestige(json);
            var customCatalog = new FakeRulesCatalog(customVestige);

            // Engine constructed with default catalog
            var defaultCatalog = new GameRulesCatalog();
            var engine = new SubsystemEngine(defaultCatalog.GetSubsystem("pact_magic"));
            var context = new FakeCharacterContext(catalog: customCatalog);
            context.Effects.AddStat(new Statistic("number of vestiges"));
            context.Effects.AddEffect(new Effect("Vestige Slot").AddModifier("number of vestiges", new Modifier(1, ModifierTypes.UNTYPED)));
            context.Effects.AddStat(new Statistic("maximum vestige level"));
            context.Effects.AddEffect(new Effect("Max Level").AddModifier("maximum vestige level", new Modifier(5, ModifierTypes.UNTYPED)));

            // Before initialize, engine uses default catalog
            Assert.IsNull(engine.GetElement("Test Vestige"));

            // Initialize resolves context.Catalog
            engine.Initialize(context);

            Assert.IsNotNull(engine.GetElement("Test Vestige"), "Initialized engine must resolve custom vestige from context.Catalog.");
            Assert.AreEqual(22, engine.GetElement("Test Vestige").GetProperty<int>("binding DC"), "Initialized engine must resolve DC from context.Catalog.");

            engine.AssignSlot("vestiges", 0, "Test Vestige", "good_pact", "Binder");
            Assert.AreEqual("Test Vestige", engine.GetSlotElement("vestiges", 0));
        }

        [TestMethod]
        public void Catalog_AllModifiers_UseValidModifierTypes() {
            var resourceFiles = new Dictionary<string, byte[]> {
                { "armor.json", Properties.Resources.armor },
                { "armorProperties.json", Properties.Resources.armorProperties },
                { "classes.json", Properties.Resources.classes },
                { "external.json", Properties.Resources.external },
                { "feats.json", Properties.Resources.feats },
                { "items.json", Properties.Resources.items },
                { "maneuvers.json", Properties.Resources.maneuvers },
                { "races.json", Properties.Resources.races },
                { "rules.json", Properties.Resources.rules },
                { "shields.json", Properties.Resources.shields },
                { "vestiges.json", Properties.Resources.vestiges },
                { "weaponProperties.json", Properties.Resources.weaponProperties },
                { "weapons.json", Properties.Resources.weapons }
            };

            int checkedModifiers = 0;
            var invalid = new List<string>();
            foreach (var kvp in resourceFiles) {
                if (kvp.Value == null || kvp.Value.Length == 0) continue;
                var json = JToken.Parse(System.Text.Encoding.UTF8.GetString(kvp.Value));
                foreach (var token in json.SelectTokens("$..[?(@.amounts)]")) {
                    if (token is JObject obj && obj["type"] != null) {
                        string modType = (string)obj["type"];
                        if (!ModifierTypes.IsValid(modType)) {
                            invalid.Add($"{kvp.Key}: {modType}");
                        }
                        checkedModifiers++;
                    }
                }
            }
            Assert.AreEqual(0, invalid.Count, $"Invalid modifier types found: {string.Join(", ", invalid)}. A missing modifier type is an unarbitrated Domain Specification requiring human arbitration to add to ModifierTypes and docs/rules/mechanics/stacking-and-modifiers.md.");
            Assert.IsTrue(checkedModifiers > 0, "Should have verified at least one modifier in the catalog.");
        }

        [TestMethod]
        public void MartialDiscipline_CatalogAndProperties_CorrectlyConfigured() {
            var catalog = new GameRulesCatalog();
            var martialDef = catalog.GetSubsystem("martial_maneuvers");
            Assert.IsNotNull(martialDef);
            Assert.IsTrue(martialDef.Taxonomies.ContainsKey("discipline"));
            var disciplines = martialDef.Taxonomies["discipline"];
            Assert.AreEqual(9, disciplines.Count);

            var ironHeart = disciplines["Iron Heart"];
            Assert.IsNotNull(ironHeart);
            Assert.AreEqual("Iron Heart", ironHeart.Name);
            Assert.AreEqual("Balance", ironHeart.GetProperty<string>("keySkill"));
            var weapons = ironHeart.GetProperty<List<object>>("weapons")?.Select(x => x.ToString()).ToList();
            Assert.IsNotNull(weapons);
            CollectionAssert.Contains(weapons, "bastard sword");

            // Case-insensitive IPropertyProvider
            Assert.AreEqual("Iron Heart", ironHeart.Properties["name"]);
            Assert.AreEqual("Balance", ironHeart.Properties["keyskill"]);
            Assert.AreEqual("Balance", ironHeart.Properties["KEYSKILL"]);
        }

        [TestMethod]
        public void Maneuver_ImplementsIPropertyProvider_WithDisciplineObject() {
            var catalog = new GameRulesCatalog();
            var martialDef = catalog.GetSubsystem("martial_maneuvers");
            Assert.IsNotNull(martialDef);
            var steelWind = martialDef.Elements["Steel Wind"];
            Assert.IsNotNull(steelWind);

            var provider = steelWind as IPropertyProvider;
            Assert.IsNotNull(provider);
            Assert.AreEqual("Steel Wind", provider.Properties["name"]);
            Assert.AreEqual(1, Convert.ToInt32(provider.Properties["level"]));

            var discipline = provider.Properties["discipline"] as TaxonomyEntry;
            Assert.IsNotNull(discipline);
            Assert.AreEqual("Iron Heart", discipline.Name);
            Assert.AreEqual("Balance", discipline.GetProperty<string>("keySkill"));
        }

        [TestMethod]
        public void ResolveTemplate_SimpleVariable_ReturnsChoiceValue() {
            var catalog = new GameRulesCatalog();
            var choices = new Dictionary<string, string> {
                ["class"] = "Warblade"
            };

            Assert.AreEqual("Warblade", catalog.ResolveTemplate("${class}", choices));
            Assert.AreEqual("maneuvers known:Warblade", catalog.ResolveTemplate("maneuvers known:${class}", choices));
        }

        [TestMethod]
        public void ResolveTemplate_DotSeparatedPath_TraversesPropertyProviders() {
            var catalog = new GameRulesCatalog();
            var choices = new Dictionary<string, string> {
                ["maneuver"] = "Steel Wind"
            };

            Assert.AreEqual("Balance", catalog.ResolveTemplate("${maneuver.discipline.keySkill}", choices));
            Assert.AreEqual("Iron Heart", catalog.ResolveTemplate("${maneuver.discipline.name}", choices));
            Assert.AreEqual("Steel Wind", catalog.ResolveTemplate("${maneuver.name}", choices));
            Assert.AreEqual("1", catalog.ResolveTemplate("${maneuver.level}", choices));
        }

        [TestMethod]
        public void ResolveTemplate_AllNineDisciplines_ResolveCorrectKeySkills() {
            // Source: Tome of Battle, pp. 41-43; docs/rules/subsystems/maneuvers.md
            var catalog = new GameRulesCatalog();
            var testCases = new Dictionary<string, string> {
                ["Burning Blade"] = "Tumble",               // Desert Wind
                ["Crusader's Strike"] = "Intimidate",       // Devoted Spirit
                ["Sapphire Nightmare Blade"] = "Concentration", // Diamond Mind
                ["Steel Wind"] = "Balance",                 // Iron Heart
                ["Mighty Throw"] = "Sense Motive",           // Setting Sun
                ["Shadow Blade Technique"] = "Hide",        // Shadow Hand
                ["Stone Bones"] = "Balance",                // Stone Dragon
                ["Wolf Fang Strike"] = "Jump",              // Tiger Claw
                ["Douse the Flames"] = "Diplomacy"          // White Raven
            };

            foreach (var kvp in testCases) {
                var choices = new Dictionary<string, string> { ["maneuver"] = kvp.Key };
                string resolved = catalog.ResolveTemplate("${maneuver.discipline.keySkill}", choices);
                Assert.AreEqual(kvp.Value, resolved, $"Maneuver '{kvp.Key}' should resolve key skill to '{kvp.Value}'");
            }
        }

        [TestMethod]
        public void ResolveTemplate_PrefixedChoiceKey_ResolvesCorrectly() {
            var catalog = new GameRulesCatalog();
            var choices = new Dictionary<string, string> {
                ["Level 1:maneuver"] = "Steel Wind"
            };

            Assert.AreEqual("Balance", catalog.ResolveTemplate("${maneuver.discipline.keySkill}", choices));
        }

        [TestMethod]
        public void ResolveTemplate_UnsetOrMissingKey_SafelyReturnsEmptyString() {
            var catalog = new GameRulesCatalog();

            // Empty choices
            Assert.AreEqual("", catalog.ResolveTemplate("${class}", new Dictionary<string, string>()));
            Assert.AreEqual("", catalog.ResolveTemplate("${maneuver.discipline.keySkill}", new Dictionary<string, string>()));

            // Missing entity
            var choices = new Dictionary<string, string> { ["maneuver"] = "NonExistentManeuver" };
            Assert.AreEqual("", catalog.ResolveTemplate("${maneuver.discipline.keySkill}", choices));

            // Missing property
            choices["maneuver"] = "Steel Wind";
            Assert.AreEqual("", catalog.ResolveTemplate("${maneuver.nonexistent.keySkill}", choices));
            Assert.AreEqual("", catalog.ResolveTemplate("${maneuver.discipline.nonexistent}", choices));

            // Null inputs
            Assert.AreEqual("", catalog.ResolveTemplate(null, choices));
            Assert.AreEqual("", catalog.ResolveTemplate("", choices));
            Assert.AreEqual("", catalog.ResolveTemplate("${class}", null));
        }
    }
}
