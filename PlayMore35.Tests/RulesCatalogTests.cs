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
            var pactDef = catalog.GetSubsystem("pact_magic");
            Assert.IsNotNull(pactDef);
            foreach (var vestige in pactDef.Elements.Values) {
                foreach (var influence in vestige.GetEffects("influence")) {
                    foreach (var text in influence.Text) {
                        Assert.IsTrue(validCategories.Contains(text.Category), $"Unknown vestige influence category: {text.Category}");
                    }
                }
                foreach (var ability in vestige.GetEffects("abilities")) {
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
            var pactDef = catalog.GetSubsystem("pact_magic");
            Assert.IsNotNull(pactDef);

            var amon = pactDef.Elements["Amon"];
            Assert.IsNotNull(amon);
            Assert.AreEqual(CONDITION, amon.GetEffects("influence")[0].Text[0].Category);

            var aym = pactDef.Elements["Aym"];
            Assert.IsNotNull(aym);
            Assert.AreEqual(SOCIAL, aym.GetEffects("influence")[0].Text[0].Category);

            var leraje = pactDef.Elements["Leraje"];
            Assert.IsNotNull(leraje);
            Assert.AreEqual(SOCIAL, leraje.GetEffects("influence")[0].Text[1].Category);

            var naberius = pactDef.Elements["Naberius"];
            Assert.IsNotNull(naberius);
            Assert.AreEqual(SOCIAL, naberius.GetEffects("influence")[0].Text[0].Category);

            var ronove = pactDef.Elements["Ronove"];
            Assert.IsNotNull(ronove);
            var ronoveInfluence = ronove.GetEffects("influence")[0];
            Assert.AreEqual(2, ronoveInfluence.Text.Count);
            Assert.AreEqual(SOCIAL, ronoveInfluence.Text[0].Category);
            Assert.AreEqual(CONDITION, ronoveInfluence.Text[1].Category);

            var haagenti = pactDef.Elements["Haagenti"];
            Assert.IsNotNull(haagenti);
            Assert.AreEqual(SOCIAL, haagenti.GetEffects("influence")[0].Text[0].Category);

            var malphas = pactDef.Elements["Malphas"];
            Assert.IsNotNull(malphas);
            Assert.AreEqual(SOCIAL, malphas.GetEffects("influence")[0].Text[0].Category);
        }

        [TestMethod]
        public void FeatsCatalog_MartialStudyAndStance_DeclareSubsystemSlotGrants() {
            var catalog = new GameRulesCatalog();
            var martialStudy = catalog.GetFeat("Martial Study");
            Assert.IsNotNull(martialStudy);
            Assert.IsNotNull(martialStudy.Benefit);
            Assert.AreEqual(1, martialStudy.Benefit.Slots.Count);
            Assert.AreEqual("martial_maneuvers", martialStudy.Benefit.Slots[0].Subsystem);
            Assert.AreEqual("maneuvers", martialStudy.Benefit.Slots[0].SlotGroup);
            Assert.AreEqual("${maneuver}", martialStudy.Benefit.Slots[0].Element);
            Assert.AreEqual("${class}", martialStudy.Benefit.Slots[0].Source);

            var martialStance = catalog.GetFeat("Martial Stance");
            Assert.IsNotNull(martialStance);
            Assert.IsNotNull(martialStance.Benefit);
            Assert.AreEqual(1, martialStance.Benefit.Slots.Count);
            Assert.AreEqual("martial_maneuvers", martialStance.Benefit.Slots[0].Subsystem);
            Assert.AreEqual("stances", martialStance.Benefit.Slots[0].SlotGroup);
            Assert.AreEqual("${stance}", martialStance.Benefit.Slots[0].Element);
            Assert.AreEqual("${class}", martialStance.Benefit.Slots[0].Source);
        }

        [TestMethod]
        public void ClassesCatalog_AdeptClasses_DeclareAdeptTags() {
            var catalog = new GameRulesCatalog();
            var warblade = catalog.GetClass("Warblade");
            var crusader = catalog.GetClass("Crusader");
            var swordsage = catalog.GetClass("Swordsage");
            var fighter = catalog.GetClass("Fighter");

            Assert.IsNotNull(warblade);
            Assert.IsTrue(warblade.Tags.Contains("adept"), "Warblade must declare 'adept' tag.");

            Assert.IsNotNull(crusader);
            Assert.IsTrue(crusader.Tags.Contains("adept"), "Crusader must declare 'adept' tag.");

            Assert.IsNotNull(swordsage);
            Assert.IsTrue(swordsage.Tags.Contains("adept"), "Swordsage must declare 'adept' tag.");

            Assert.IsNotNull(fighter);
            Assert.IsFalse(fighter.Tags.Contains("adept"), "Fighter must not declare 'adept' tag.");
        }

        [TestMethod]
        public void FeatsCatalog_MartialStudyAndStance_DeclareSubsystems() {
            var catalog = new GameRulesCatalog();
            var martialStudy = catalog.GetFeat("Martial Study");
            Assert.IsNotNull(martialStudy);
            Assert.IsTrue(martialStudy.Subsystems.Contains("martial_maneuvers"), "Martial Study must declare 'martial_maneuvers' subsystem.");

            var martialStance = catalog.GetFeat("Martial Stance");
            Assert.IsNotNull(martialStance);
            Assert.IsTrue(martialStance.Subsystems.Contains("martial_maneuvers"), "Martial Stance must declare 'martial_maneuvers' subsystem.");
        }

        [TestMethod]
        public void RulesCatalog_FindFeatByChoiceDescriptor_FindsMatchingFeat() {
            var catalog = new GameRulesCatalog();
            var martialStance = catalog.GetFeat("Martial Stance");
            var stanceDescriptor = martialStance.Choices.First(c => c.Id == "stance");

            var found = catalog.FindFeatByChoiceDescriptor(stanceDescriptor);
            Assert.IsNotNull(found);
            Assert.AreEqual("Martial Stance", found.Benefit?.Name);

            // Null or non-matching descriptor returns null
            Assert.IsNull(catalog.FindFeatByChoiceDescriptor(null!));
            Assert.IsNull(catalog.FindFeatByChoiceDescriptor(new ChoiceDescriptor("nonexistent", "dummy", "dummy")));
        }

        // Source: Tome of Magic, pp. 16–50
        [TestMethod]
        public void GameRulesCatalog_LoadsVestiges_FromResources() {
            var catalog = new GameRulesCatalog();
            var pactDef = catalog.GetSubsystem("pact_magic");

            Assert.IsNotNull(pactDef, "Pact magic subsystem must be initialized.");
            Assert.IsNotNull(pactDef.Elements, "Vestiges dictionary must be initialized.");
            Assert.IsTrue(pactDef.Elements.Count >= 9, $"Expected at least 9 vestiges, got {pactDef.Elements.Count}");

            Assert.IsTrue(pactDef.Elements.TryGetValue("Amon", out var amon), "Amon should be present in catalog.");
            Assert.AreEqual("Amon", amon.Name);
            Assert.AreEqual(1, amon.GetProperty<int>("level"));
            Assert.AreEqual(20, amon.GetProperty<int>("binding DC"));
            Assert.IsTrue(amon.GetEffects("influence").Count > 0, "Amon should have influence.");
            Assert.IsTrue(amon.GetEffects("abilities").Count > 0, "Amon should have abilities.");

            // Case-insensitive lookup via Dictionary
            Assert.IsTrue(pactDef.Elements.TryGetValue("amon", out var amonLower), "Vestige lookup should be case-insensitive.");
            Assert.AreEqual("Amon", amonLower.Name);

            // Missing / null / empty
            Assert.IsFalse(pactDef.Elements.ContainsKey("NonExistentVestige"));
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

            // Engine constructed with context referencing customCatalog
            var defaultCatalog = new GameRulesCatalog();
            var context = new FakeCharacterContext(catalog: customCatalog);
            var engine = new SubsystemEngine(defaultCatalog.GetSubsystem("martial_maneuvers"), context);

            var postWarnings = engine.ValidateChoice(null, "Special Strike", "Warblade");
            Assert.IsFalse(postWarnings.Any(w => w.Message.Contains("not recognized") || w.Message.Contains("not a valid")), "Engine must recognize maneuver from context.Catalog.");
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
            var customVestige = new SubsystemElement(json);
            var customCatalog = new FakeRulesCatalog(vestiges: new[] { customVestige });

            // Engine constructed with context referencing customCatalog
            var defaultCatalog = new GameRulesCatalog();
            var context = new FakeCharacterContext(catalog: customCatalog);
            var engine = new SubsystemEngine(defaultCatalog.GetSubsystem("pact_magic"), context);
            context.Effects.AddStat(new Statistic("number of vestiges"));
            context.Effects.AddEffect(new Effect("Vestige Slot").AddModifier("number of vestiges", new Modifier(1, ModifierTypes.UNTYPED)));
            context.Effects.AddStat(new Statistic("maximum vestige level"));
            context.Effects.AddEffect(new Effect("Max Level").AddModifier("maximum vestige level", new Modifier(5, ModifierTypes.UNTYPED)));

            Assert.IsNotNull(engine.GetElement("Test Vestige"), "Engine must resolve custom vestige from context.Catalog.");
            Assert.AreEqual(22, engine.GetElement("Test Vestige").GetProperty<int>("binding DC"), "Engine must resolve DC from context.Catalog.");

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
            var choices = new Dictionary<string, object> {
                ["class"] = "Warblade"
            };

            Assert.AreEqual("Warblade", catalog.ResolveTemplate("${class}", choices));
            Assert.AreEqual("maneuvers known:Warblade", catalog.ResolveTemplate("maneuvers known:${class}", choices));
        }

        [TestMethod]
        public void ResolveTemplate_DotSeparatedPath_TraversesPropertyProviders() {
            var catalog = new GameRulesCatalog();
            var martialDef = catalog.GetSubsystem("martial_maneuvers");
            var steelWind = martialDef.Elements["Steel Wind"];
            var choices = new Dictionary<string, object> {
                ["maneuver"] = steelWind
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
            var martialDef = catalog.GetSubsystem("martial_maneuvers");
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
                var elem = martialDef.Elements[kvp.Key];
                var choices = new Dictionary<string, object> { ["maneuver"] = elem };
                string resolved = catalog.ResolveTemplate("${maneuver.discipline.keySkill}", choices);
                Assert.AreEqual(kvp.Value, resolved, $"Maneuver '{kvp.Key}' should resolve key skill to '{kvp.Value}'");
            }
        }

        [TestMethod]
        public void ResolveTemplate_PrefixedChoiceKey_ResolvesCorrectly() {
            var catalog = new GameRulesCatalog();
            var martialDef = catalog.GetSubsystem("martial_maneuvers");
            var steelWind = martialDef.Elements["Steel Wind"];
            var choices = new Dictionary<string, object> {
                ["Level 1:maneuver"] = steelWind
            };

            Assert.AreEqual("Balance", catalog.ResolveTemplate("${maneuver.discipline.keySkill}", choices));
        }

        [TestMethod]
        public void ResolveTemplate_UnsetOrMissingKey_SafelyReturnsEmptyString() {
            var catalog = new GameRulesCatalog();
            var martialDef = catalog.GetSubsystem("martial_maneuvers");
            var steelWind = martialDef.Elements["Steel Wind"];

            // Empty choices
            Assert.AreEqual("", catalog.ResolveTemplate("${class}", new Dictionary<string, object>()));
            Assert.AreEqual("", catalog.ResolveTemplate("${maneuver.discipline.keySkill}", new Dictionary<string, object>()));

            // Missing entity
            var choices = new Dictionary<string, object> { ["maneuver"] = "NonExistentManeuver" };
            Assert.AreEqual("", catalog.ResolveTemplate("${maneuver.discipline.keySkill}", choices));

            // Missing property
            choices["maneuver"] = steelWind;
            Assert.AreEqual("", catalog.ResolveTemplate("${maneuver.nonexistent.keySkill}", choices));
            Assert.AreEqual("", catalog.ResolveTemplate("${maneuver.discipline.nonexistent}", choices));

            // Null inputs
            Assert.AreEqual("", catalog.ResolveTemplate(null, choices));
            Assert.AreEqual("", catalog.ResolveTemplate("", choices));
            Assert.AreEqual("", catalog.ResolveTemplate("${class}", null));
        }

        [TestMethod]
        public void IRulesCatalog_Lookups_ThrowKeyNotFoundException_WhenMissing() {
            var catalog = new GameRulesCatalog();

            Assert.ThrowsException<KeyNotFoundException>(() => catalog.GetRace("NonexistentRace"));
            Assert.ThrowsException<KeyNotFoundException>(() => catalog.GetClass("NonexistentClass"));
            Assert.ThrowsException<KeyNotFoundException>(() => catalog.GetFeat("NonexistentFeat"));
            Assert.ThrowsException<KeyNotFoundException>(() => catalog.GetWeapon("NonexistentWeapon"));
            Assert.ThrowsException<KeyNotFoundException>(() => catalog.GetArmor("NonexistentArmor"));
            Assert.ThrowsException<KeyNotFoundException>(() => catalog.GetShield("NonexistentShield"));
            Assert.ThrowsException<KeyNotFoundException>(() => catalog.GetWeaponProperty("NonexistentProp"));
            Assert.ThrowsException<KeyNotFoundException>(() => catalog.GetArmorProperty("NonexistentProp"));
            Assert.ThrowsException<KeyNotFoundException>(() => catalog.GetItem("NonexistentItem"));
            Assert.ThrowsException<KeyNotFoundException>(() => catalog.GetExternalEffect("NonexistentEffect"));
            Assert.ThrowsException<KeyNotFoundException>(() => catalog.GetSubsystem("NonexistentSubsystem"));
        }

        [TestMethod]
        public void IRulesCatalog_TryGetMethods_ReturnExpectedValues() {
            var catalog = new GameRulesCatalog();

            Assert.IsTrue(catalog.TryGetRace("human", out var race));
            Assert.IsNotNull(race);
            Assert.AreEqual("human", race.Name);

            Assert.IsFalse(catalog.TryGetRace("NonexistentRace", out var missingRace));
            Assert.IsNull(missingRace);

            Assert.IsTrue(catalog.TryGetClass("Fighter", out var fighter));
            Assert.IsNotNull(fighter);
            Assert.AreEqual("Fighter", fighter.Name);

            Assert.IsFalse(catalog.TryGetClass("NonexistentClass", out var missingClass));
            Assert.IsNull(missingClass);

            Assert.IsTrue(catalog.TryGetFeat("Power Attack", out var feat));
            Assert.IsNotNull(feat);
            Assert.AreEqual("Power Attack", feat.Benefit.Name);

            Assert.IsFalse(catalog.TryGetFeat("NonexistentFeat", out var missingFeat));
            Assert.IsNull(missingFeat);

            Assert.IsTrue(catalog.TryGetWeapon("longsword", out var weapon));
            Assert.IsNotNull(weapon);
            Assert.AreEqual("longsword", weapon.Name);

            Assert.IsFalse(catalog.TryGetWeapon("NonexistentWeapon", out var missingWeapon));
            Assert.IsNull(missingWeapon);

            Assert.IsTrue(catalog.TryGetArmor("full plate", out var armor));
            Assert.IsNotNull(armor);
            Assert.AreEqual("full plate", armor.Name);

            Assert.IsFalse(catalog.TryGetArmor("NonexistentArmor", out var missingArmor));
            Assert.IsNull(missingArmor);

            Assert.IsTrue(catalog.TryGetShield("heavy steel shield", out var shield));
            Assert.IsNotNull(shield);
            Assert.AreEqual("heavy steel shield", shield.Name);

            Assert.IsFalse(catalog.TryGetShield("NonexistentShield", out var missingShield));
            Assert.IsNull(missingShield);

            Assert.IsTrue(catalog.TryGetWeaponProperty("+1", out var wProp));
            Assert.IsNotNull(wProp);
            Assert.AreEqual("+1", wProp.Name);

            Assert.IsFalse(catalog.TryGetWeaponProperty("NonexistentProp", out var missingWProp));
            Assert.IsNull(missingWProp);

            Assert.IsTrue(catalog.TryGetArmorProperty("+1", out var aProp));
            Assert.IsNotNull(aProp);
            Assert.AreEqual("+1", aProp.Name);

            Assert.IsFalse(catalog.TryGetArmorProperty("NonexistentProp", out var missingAProp));
            Assert.IsNull(missingAProp);

            Assert.IsTrue(catalog.TryGetItem("panic button", out var item));
            Assert.IsNotNull(item);
            Assert.AreEqual("panic button", item.ItemEffect.Name);

            Assert.IsFalse(catalog.TryGetItem("NonexistentItem", out var missingItem));
            Assert.IsNull(missingItem);

            Assert.IsTrue(catalog.TryGetExternalEffect("resistance", out var extEffect));
            Assert.IsNotNull(extEffect);
            Assert.AreEqual("resistance", extEffect.Effect.Name);

            Assert.IsFalse(catalog.TryGetExternalEffect("NonexistentEffect", out var missingExtEffect));
            Assert.IsNull(missingExtEffect);

            Assert.IsTrue(catalog.TryGetSubsystem("pact_magic", out var pactDef));
            Assert.IsNotNull(pactDef);
            Assert.AreEqual("pact_magic", pactDef.Id);

            Assert.IsFalse(catalog.TryGetSubsystem("NonexistentSubsystem", out var missingSubsystem));
            Assert.IsNull(missingSubsystem);
        }

        [TestMethod]
        public void FakeRulesCatalog_ThrowsKeyNotFoundException_WhenMissing() {
            var catalog = new FakeRulesCatalog();

            Assert.ThrowsException<KeyNotFoundException>(() => catalog.GetRace("NonexistentRace"));
            Assert.ThrowsException<KeyNotFoundException>(() => catalog.GetClass("NonexistentClass"));
            Assert.ThrowsException<KeyNotFoundException>(() => catalog.GetFeat("NonexistentFeat"));
            Assert.ThrowsException<KeyNotFoundException>(() => catalog.GetWeapon("NonexistentWeapon"));
            Assert.ThrowsException<KeyNotFoundException>(() => catalog.GetArmor("NonexistentArmor"));
            Assert.ThrowsException<KeyNotFoundException>(() => catalog.GetShield("NonexistentShield"));
            Assert.ThrowsException<KeyNotFoundException>(() => catalog.GetWeaponProperty("NonexistentProp"));
            Assert.ThrowsException<KeyNotFoundException>(() => catalog.GetArmorProperty("NonexistentProp"));
            Assert.ThrowsException<KeyNotFoundException>(() => catalog.GetItem("NonexistentItem"));
            Assert.ThrowsException<KeyNotFoundException>(() => catalog.GetExternalEffect("NonexistentEffect"));
            Assert.ThrowsException<KeyNotFoundException>(() => catalog.GetSubsystem("NonexistentSubsystem"));
        }

        [TestMethod]
        public void SavedCharacter_ReferencingNonexistentContent_FailsFastWithInformativeMessage() {
            var catalog = new GameRulesCatalog();

            var charBadRace = new Character { race = "IllithidDragon" };
            var exRace = Assert.ThrowsException<KeyNotFoundException>(() => new CharacterSession(charBadRace, catalog));
            StringAssert.Contains(exRace.Message, "IllithidDragon");

            var charBadClass = new Character { race = "human" };
            charBadClass.classLevels.Clear();
            charBadClass.classLevels.Add("Psion");
            var exClass = Assert.ThrowsException<KeyNotFoundException>(() => new CharacterSession(charBadClass, catalog));
            StringAssert.Contains(exClass.Message, "Psion");

            var charBadFeat = new Character { race = "human" };
            charBadFeat.feats.Add(new FeatAssignment(ProgressionSlotKey.Advancement(1), "EpicSpellcasting"));
            var exFeat = Assert.ThrowsException<KeyNotFoundException>(() => new CharacterSession(charBadFeat, catalog));
            StringAssert.Contains(exFeat.Message, "EpicSpellcasting");

            var charBadWeapon = new Character { race = "human" };
            charBadWeapon.equipped.PrimaryHandWeapon = new WeaponChoice { Name = "BFG9000" };
            var exWeapon = Assert.ThrowsException<KeyNotFoundException>(() => new CharacterSession(charBadWeapon, catalog));
            StringAssert.Contains(exWeapon.Message, "BFG9000");

            var charBadArmor = new Character { race = "human" };
            charBadArmor.equipped.DonnedArmor = new ArmorChoice { Name = "PoweredExoskeleton" };
            var exArmor = Assert.ThrowsException<KeyNotFoundException>(() => new CharacterSession(charBadArmor, catalog));
            StringAssert.Contains(exArmor.Message, "PoweredExoskeleton");

            var charBadShield = new Character { race = "human" };
            charBadShield.equipped.OffHandShield = new ArmorChoice { Name = "ForceFieldShield", IsShield = true };
            var exShield = Assert.ThrowsException<KeyNotFoundException>(() => new CharacterSession(charBadShield, catalog));
            StringAssert.Contains(exShield.Message, "ForceFieldShield");

            var charBadItem = new Character { race = "human" };
            charBadItem.equipped.SlotlessItems.Add("InfinityGauntlet");
            var exItem = Assert.ThrowsException<KeyNotFoundException>(() => new CharacterSession(charBadItem, catalog));
            StringAssert.Contains(exItem.Message, "InfinityGauntlet");

            var charBadEffect = new Character { race = "human" };
            charBadEffect.externalEffects.Add("WishSpellSuperBuff");
            var exEffect = Assert.ThrowsException<KeyNotFoundException>(() => new CharacterSession(charBadEffect, catalog));
            StringAssert.Contains(exEffect.Message, "WishSpellSuperBuff");
        }
    }
}
