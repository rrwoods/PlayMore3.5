using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Rules;
using PlayMore35.Subsystems;

namespace PlayMore35.Tests {
    [TestClass]
    public class UniversalSubsystemPersistenceTests {
        private (CharacterSession session, FakeRulesCatalog catalog, SubsystemEngine engine) CreateSyntheticSession() {
            var baseCatalog = new GameRulesCatalog();
            var catalog = new FakeRulesCatalog(inner: baseCatalog);
            var syntheticDef = FakeRulesCatalog.CreateSyntheticAlchemySubsystem();
            catalog.RegisterSubsystem(syntheticDef);

            var character = new Character();
            var session = new CharacterSession(character, catalog);
            var engine = session.GetSubsystem("synthetic_alchemy");
            return (session, catalog, engine);
        }

        [TestMethod]
        public void UniversalSlotModel_SerializesAndDeserializesExplicitly_WithOptionalFeatSlot() {
            // Slot with FeatSlot
            var slotWithFeat = new SubsystemSlot("Acid Flask", "prepared", "GuildAlchemist", "Level 1");
            var jsonWithFeat = (JObject)slotWithFeat.ToJson();

            Assert.AreEqual("Acid Flask", (string)jsonWithFeat["element"]);
            Assert.AreEqual("prepared", (string)jsonWithFeat["state"]);
            Assert.AreEqual("GuildAlchemist", (string)jsonWithFeat["source"]);
            Assert.AreEqual("Level 1", (string)jsonWithFeat["feat_slot"]);

            var restoredWithFeat = new SubsystemSlot(jsonWithFeat);
            Assert.AreEqual(slotWithFeat, restoredWithFeat);
            Assert.AreEqual("Level 1", restoredWithFeat.FeatSlot);

            // Slot without FeatSlot omits feat_slot property
            var slotWithoutFeat = new SubsystemSlot("Fire Flask", "available", "HedgeWizard");
            var jsonWithoutFeat = (JObject)slotWithoutFeat.ToJson();

            Assert.AreEqual("Fire Flask", (string)jsonWithoutFeat["element"]);
            Assert.AreEqual("available", (string)jsonWithoutFeat["state"]);
            Assert.AreEqual("HedgeWizard", (string)jsonWithoutFeat["source"]);
            Assert.IsNull(jsonWithoutFeat["feat_slot"], "feat_slot should be omitted from JSON when null or empty.");

            var restoredWithoutFeat = new SubsystemSlot(jsonWithoutFeat);
            Assert.AreEqual(slotWithoutFeat, restoredWithoutFeat);
            Assert.IsNull(restoredWithoutFeat.FeatSlot);
        }

        [TestMethod]
        public void UniversalPoolModel_KeyedByPoolId_SerializesAndDeserializesIntegerValues() {
            var state = new SubsystemState();
            state.Pools["catalyst"] = 8;
            state.Pools["heat"] = 3;

            var json = (JObject)state.ToJson();
            Assert.IsNotNull(json["pools"]);
            var poolsObj = (JObject)json["pools"];
            Assert.AreEqual(8, (int)poolsObj["catalyst"]);
            Assert.AreEqual(3, (int)poolsObj["heat"]);

            var restored = new SubsystemState(json);
            Assert.AreEqual(2, restored.Pools.Count);
            Assert.AreEqual(8, restored.Pools["catalyst"]);
            Assert.AreEqual(3, restored.Pools["heat"]);

            // ADR-0008: Strict integer token validation - string-encoded numbers must be rejected with zero fallback
            var nonIntJson = new JObject(
                new JProperty("pools", new JObject(
                    new JProperty("valid_int", 5),
                    new JProperty("string_encoded", "5")
                ))
            );
            var nonIntRestored = new SubsystemState(nonIntJson);
            Assert.IsTrue(nonIntRestored.Pools.ContainsKey("valid_int"));
            Assert.IsFalse(nonIntRestored.Pools.ContainsKey("string_encoded"), "String-encoded numbers must not be parsed via fallback shims.");
        }

        [TestMethod]
        public void Character_Subsystems_StoresUniformMapOfSubsystemState_SerializingToNormalizedJsonObject() {
            var character = new Character { name = "Universal Tester" };

            var alchemyState = new SubsystemState();
            alchemyState.Slots["flasks"] = new List<SubsystemSlot> {
                new SubsystemSlot("Acid Flask", "prepared", "GuildAlchemist"),
                new SubsystemSlot("Fire Flask", "empty", "HedgeWizard")
            };
            alchemyState.Pools["catalyst"] = 7;
            alchemyState.Pools["heat"] = 4;

            character.Subsystems["synthetic_alchemy"] = alchemyState;

            var json = (JObject)character.ToJson();
            Assert.IsNotNull(json["subsystems"]);
            var subObj = (JObject)json["subsystems"];
            Assert.IsNotNull(subObj["synthetic_alchemy"]);

            var alchemyJson = (JObject)subObj["synthetic_alchemy"];
            Assert.IsNotNull(alchemyJson["slots"]);
            Assert.IsNotNull(alchemyJson["pools"]);

            var flasksJson = (JArray)alchemyJson["slots"]["flasks"];
            Assert.AreEqual(2, flasksJson.Count);
            Assert.AreEqual("Acid Flask", (string)flasksJson[0]["element"]);
            Assert.AreEqual("Fire Flask", (string)flasksJson[1]["element"]);

            Assert.AreEqual(7, (int)alchemyJson["pools"]["catalyst"]);
            Assert.AreEqual(4, (int)alchemyJson["pools"]["heat"]);

            // Deserialization via explicit constructor
            var restored = new Character(json);
            Assert.IsTrue(restored.Subsystems.ContainsKey("synthetic_alchemy"));
            var restoredState = restored.Subsystems["synthetic_alchemy"];
            Assert.IsNotNull(restoredState);
            Assert.AreEqual(2, restoredState.Slots["flasks"].Count);
            Assert.AreEqual("Acid Flask", restoredState.Slots["flasks"][0].Element);
            Assert.AreEqual("prepared", restoredState.Slots["flasks"][0].State);
            Assert.AreEqual("Fire Flask", restoredState.Slots["flasks"][1].Element);
            Assert.AreEqual("empty", restoredState.Slots["flasks"][1].State);
            Assert.AreEqual(7, restoredState.Pools["catalyst"]);
            Assert.AreEqual(4, restoredState.Pools["heat"]);
        }

        [TestMethod]
        public void Character_Deserialization_ZeroHardcodedSubsystemChecksOrBespokeTypes_ArchitecturalAudit() {
            // Verify Character class has zero bespoke subsystem types or references
            var charType = typeof(Character);

            // Subsystems property must be Dictionary<string, SubsystemState>
            var subProp = charType.GetProperty("Subsystems");
            Assert.IsNotNull(subProp);
            Assert.AreEqual(typeof(Dictionary<string, SubsystemState>), subProp.PropertyType);

            // Field and property audit: no hardcoded subsystem names
            var fieldsAndProps = charType.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Select(m => m.Name.ToLowerInvariant());

            Assert.IsFalse(fieldsAndProps.Any(n => n.Contains("vestige")), "Character must not contain bespoke vestige members.");
            Assert.IsFalse(fieldsAndProps.Any(n => n.Contains("maneuver")), "Character must not contain bespoke maneuver members.");
            Assert.IsFalse(fieldsAndProps.Any(n => n.Contains("pactmagic")), "Character must not contain bespoke pact magic members.");
        }

        [TestMethod]
        public void Character_Deserialization_RejectsLegacyBespokeShapes_WithZeroFallbackShims() {
            // Start from valid character JSON and inject pre-ADR-0006 legacy bespoke subsystem shapes
            var baseChar = new Character { name = "Legacy Hero" };
            var legacyJson = (JObject)baseChar.ToJson();

            legacyJson["subsystems"] = new JObject(
                // Legacy pact magic shape: "slots" is JArray directly under subsystem
                new JProperty("pact_magic", new JObject(
                    new JProperty("slots", new JArray(
                        new JObject(new JProperty("name", "Amon"), new JProperty("state", "GOOD_PACT"))
                    ))
                )),
                // Legacy martial maneuvers shape: "sources" instead of "slots"
                new JProperty("martial_maneuvers", new JObject(
                    new JProperty("sources", new JObject(
                        new JProperty("Warblade", new JObject(
                            new JProperty("known", new JArray())
                        ))
                    ))
                ))
            );

            var character = new Character(legacyJson);

            // SubsystemState parsed via explicit constructor with zero fallback shims
            // In pact_magic: "slots" was a JArray instead of JObject mapping slot groups -> Slots map is empty
            Assert.IsTrue(character.Subsystems.ContainsKey("pact_magic"));
            Assert.AreEqual(0, character.Subsystems["pact_magic"].Slots.Count, "Legacy array slots must NOT be parsed via fallback shims.");

            // In martial_maneuvers: "sources" is ignored since only "slots" and "pools" are parsed
            Assert.IsTrue(character.Subsystems.ContainsKey("martial_maneuvers"));
            Assert.AreEqual(0, character.Subsystems["martial_maneuvers"].Slots.Count, "Legacy sources property must NOT be parsed via fallback shims.");
        }

        [TestMethod]
        public void CharacterSession_InitializesEnginesFromCharacterSubsystems() {
            var baseCatalog = new GameRulesCatalog();
            var catalog = new FakeRulesCatalog(inner: baseCatalog);
            var syntheticDef = FakeRulesCatalog.CreateSyntheticAlchemySubsystem();
            catalog.RegisterSubsystem(syntheticDef);

            var character = new Character();
            var state = new SubsystemState();
            state.Slots["flasks"] = new List<SubsystemSlot> {
                new SubsystemSlot("Acid Flask", "prepared", "GuildAlchemist"),
                new SubsystemSlot("Fire Flask", "empty", "HedgeWizard")
            };
            state.Pools["catalyst"] = 4;
            state.Pools["heat"] = 2;
            character.Subsystems["synthetic_alchemy"] = state;

            // CharacterSession initializes engines from character.Subsystems
            var session = new CharacterSession(character, catalog);
            var engine = session.GetSubsystem("synthetic_alchemy");
            Assert.IsNotNull(engine);

            Assert.AreEqual("Acid Flask", engine.GetSlot("flasks", 0)?.Element);
            Assert.AreEqual("prepared", engine.GetSlot("flasks", 0)?.State);
            Assert.AreEqual("GuildAlchemist", engine.GetSlot("flasks", 0)?.Source);

            Assert.AreEqual("Fire Flask", engine.GetSlot("flasks", 1)?.Element);
            Assert.AreEqual("empty", engine.GetSlot("flasks", 1)?.State);
            Assert.AreEqual("HedgeWizard", engine.GetSlot("flasks", 1)?.Source);

            Assert.AreEqual(4, engine.GetPoolValue("catalyst"));
            Assert.AreEqual(2, engine.GetPoolValue("heat"));
        }

        [TestMethod]
        public void CharacterSession_TwoWaySync_EngineMutationsSyncToCharacterSubsystems() {
            var (session, _, engine) = CreateSyntheticSession();
            var character = ((ICharacterContext)session).Character;

            // 1. Assign slot updates Character.Subsystems
            engine.AssignSlot("flasks", 0, "Acid Flask", state: "prepared", source: "GuildAlchemist");
            var state = character.Subsystems["synthetic_alchemy"];
            Assert.AreEqual(1, state.Slots["flasks"].Count);
            Assert.AreEqual("Acid Flask", state.Slots["flasks"][0].Element);
            Assert.AreEqual("prepared", state.Slots["flasks"][0].State);
            Assert.AreEqual("GuildAlchemist", state.Slots["flasks"][0].Source);

            // 2. Clear slot updates Character.Subsystems
            engine.AssignSlot("flasks", 1, "Fire Flask", state: "empty", source: "HedgeWizard");
            Assert.AreEqual(2, character.Subsystems["synthetic_alchemy"].Slots["flasks"].Count);
            engine.ClearSlot("flasks", 1);
            Assert.AreEqual(1, character.Subsystems["synthetic_alchemy"].Slots["flasks"].Count);

            // 3. Pool operations update Character.Subsystems
            engine.SetPoolValue("catalyst", 6);
            Assert.AreEqual(6, character.Subsystems["synthetic_alchemy"].Pools["catalyst"]);

            engine.ConsumePool("catalyst", 2);
            Assert.AreEqual(4, character.Subsystems["synthetic_alchemy"].Pools["catalyst"]);

            engine.RechargePool("catalyst", 5);
            Assert.AreEqual(9, character.Subsystems["synthetic_alchemy"].Pools["catalyst"]);

            // 4. Transitions update Character.Subsystems
            engine.ExecuteTransition(new SubsystemTransition {
                SlotGroup = "flasks",
                Selector = "invoked",
                SlotIndex = 0,
                FromState = "prepared",
                ToState = "expended"
            });
            Assert.AreEqual("expended", character.Subsystems["synthetic_alchemy"].Slots["flasks"][0].State);
        }

        [TestMethod]
        public void CharacterSession_TwoWaySync_CharacterSubsystemsMutationsSyncToEngineUponRecalculate() {
            var (session, _, engine) = CreateSyntheticSession();
            var character = ((ICharacterContext)session).Character;

            engine.AssignSlot("flasks", 0, "Acid Flask", state: "prepared");
            Assert.AreEqual("prepared", engine.GetSlot("flasks", 0).State);

            // Directly mutate Character.Subsystems
            character.Subsystems["synthetic_alchemy"].Slots["flasks"][0].State = "expended";
            character.Subsystems["synthetic_alchemy"].Pools["catalyst"] = 3;

            // Recalculate synchronizes Character.Subsystems into SubsystemEngine
            session.Recalculate();

            Assert.AreEqual("expended", engine.GetSlot("flasks", 0).State);
            Assert.AreEqual(3, engine.GetPoolValue("catalyst"));
        }

        [TestMethod]
        public void CharacterSession_RoundTrip_PreservesAllSubsystemSlotAssignmentsStatesSourcesAndPools() {
            var (session, catalog, engine) = CreateSyntheticSession();

            // Setup state
            engine.AssignSlot("flasks", 0, "Acid Flask", state: "prepared", source: "GuildAlchemist");
            engine.AssignSlot("flasks", 1, "Fire Flask", state: "empty", source: "HedgeWizard");
            engine.AssignSlot("infusions", 0, "Speed Infusion", state: "active", source: "Alchemist");
            engine.SetPoolValue("catalyst", 7);
            engine.SetPoolValue("heat", 3);

            session.Recalculate();
            int activeDex = session.GetStatistic("DEX score").Value;

            // Serialize character
            var json = (JObject)session.Character.ToJson();

            // Deserialize into new character and session
            var restoredChar = new Character(json);
            var restoredSession = new CharacterSession(restoredChar, catalog);
            var restoredEngine = restoredSession.GetSubsystem("synthetic_alchemy");

            Assert.IsNotNull(restoredEngine);
            Assert.AreEqual("Acid Flask", restoredEngine.GetSlot("flasks", 0)?.Element);
            Assert.AreEqual("prepared", restoredEngine.GetSlot("flasks", 0)?.State);
            Assert.AreEqual("GuildAlchemist", restoredEngine.GetSlot("flasks", 0)?.Source);

            Assert.AreEqual("Fire Flask", restoredEngine.GetSlot("flasks", 1)?.Element);
            Assert.AreEqual("empty", restoredEngine.GetSlot("flasks", 1)?.State);
            Assert.AreEqual("HedgeWizard", restoredEngine.GetSlot("flasks", 1)?.Source);

            Assert.AreEqual("Speed Infusion", restoredEngine.GetSlot("infusions", 0)?.Element);
            Assert.AreEqual("active", restoredEngine.GetSlot("infusions", 0)?.State);
            Assert.AreEqual("Alchemist", restoredEngine.GetSlot("infusions", 0)?.Source);

            Assert.AreEqual(7, restoredEngine.GetPoolValue("catalyst"));
            Assert.AreEqual(3, restoredEngine.GetPoolValue("heat"));

            // Verify projected effect (active Speed Infusion projects +4 DEX)
            Assert.AreEqual(activeDex, restoredSession.GetStatistic("DEX score").Value);
        }

        [TestMethod]
        public void CharacterSession_InactiveCatalogEngine_DoesNotPopulateEmptySubsystemInCharacter() {
            var baseCatalog = new GameRulesCatalog();
            var catalog = new FakeRulesCatalog(inner: baseCatalog);
            var inactiveDef = new SubsystemDefinition("dormant_system", "Dormant System");
            inactiveDef.SlotGroups["dormant_slots"] = new SlotGroupDefinition(
                name: "dormant_slots",
                capacityStat: "dormant_slots_count", // resolves to 0
                fixedCapacity: 0
            );
            inactiveDef.Pools["dormant_pool"] = new PoolDefinition(
                id: "dormant_pool",
                capacityStat: "dormant_pool_cap", // resolves to 0
                defaultCapacity: 0
            );
            catalog.RegisterSubsystem(inactiveDef);

            var character = new Character();
            var session = new CharacterSession(character, catalog);

            var engine = session.GetSubsystem("dormant_system");
            Assert.IsNotNull(engine);
            Assert.IsFalse(engine.IsActive);
            Assert.IsFalse(character.Subsystems.ContainsKey("dormant_system"), "Inactive catalog subsystems must not populate empty entries in Character.Subsystems.");
        }

        [TestMethod]
        public void SubsystemDefinition_MinimalConstructor_InitializesEmptyCollectionsAndIdAndName() {
            var minimalDef = new SubsystemDefinition("martial_maneuvers");
            Assert.AreEqual("martial_maneuvers", minimalDef.Id);
            Assert.AreEqual("martial_maneuvers", minimalDef.Name);
            Assert.IsNull(minimalDef.PrerequisiteUnit);
            Assert.IsNull(minimalDef.PrerequisiteProperty);
            Assert.IsNull(minimalDef.LevelGating);
            Assert.IsNotNull(minimalDef.SlotGroups);
            Assert.AreEqual(0, minimalDef.SlotGroups.Count);
            Assert.IsNotNull(minimalDef.Verbs);
            Assert.AreEqual(0, minimalDef.Verbs.Count);
            Assert.IsNotNull(minimalDef.DefaultElementVerbs);
            Assert.AreEqual(0, minimalDef.DefaultElementVerbs.Count);
            Assert.IsNotNull(minimalDef.Pools);
            Assert.AreEqual(0, minimalDef.Pools.Count);
            Assert.IsNotNull(minimalDef.EffectTemplates);
            Assert.AreEqual(0, minimalDef.EffectTemplates.Count);
            Assert.IsNotNull(minimalDef.Projections);
            Assert.AreEqual(0, minimalDef.Projections.Count);
            Assert.IsNotNull(minimalDef.RequiredElementProperties);
            Assert.AreEqual(0, minimalDef.RequiredElementProperties.Count);
            Assert.IsNotNull(minimalDef.ElementStatistics);
            Assert.AreEqual(0, minimalDef.ElementStatistics.Count);
            Assert.IsNotNull(minimalDef.Taxonomies);
            Assert.AreEqual(0, minimalDef.Taxonomies.Count);
            Assert.IsNotNull(minimalDef.Elements);
            Assert.AreEqual(0, minimalDef.Elements.Count);
            Assert.IsNotNull(minimalDef.Actions);
            Assert.AreEqual(0, minimalDef.Actions.Count);

            var namedDef = new SubsystemDefinition("pact_magic", "Pact Magic");
            Assert.AreEqual("pact_magic", namedDef.Id);
            Assert.AreEqual("Pact Magic", namedDef.Name);
            Assert.AreEqual(0, namedDef.SlotGroups.Count);
        }

        [TestMethod]
        public void SubsystemDefinition_DirectJsonDeserialization_PopulatesAllPropertiesDirectly() {
            var json = new JObject {
                ["subsystem"] = new JObject {
                    ["id"] = "test_sub",
                    ["name"] = "Test Subsystem",
                    ["prerequisite_unit"] = "unit_x",
                    ["prerequisite_property"] = "prop_y",
                    ["level_gating"] = new JObject {
                        ["element_statistic"] = "level",
                        ["max_level_label"] = "Max Level"
                    },
                    ["taxonomies"] = new JObject {
                        ["discipline"] = new JObject {
                            ["Iron Heart"] = new JObject {
                                ["key"] = "iron_heart"
                            }
                        }
                    },
                    ["slot_groups"] = new JObject {
                        ["group1"] = new JObject {
                            ["name"] = "group1",
                            ["fixed_capacity"] = 4
                        }
                    },
                    ["verbs"] = new JObject {
                        ["verb1"] = new JArray(new JObject { ["type"] = "transition", ["to"] = "ready" })
                    },
                    ["default_element_verbs"] = new JObject {
                        ["elem_verb1"] = new JArray(new JObject { ["type"] = "invoke" })
                    },
                    ["pools"] = new JObject {
                        ["pool1"] = new JObject { ["id"] = "pool1", ["default_capacity"] = 10 }
                    },
                    ["effect_templates"] = new JObject {
                        ["tmpl1"] = new JObject { ["name"] = "template_effect" }
                    },
                    ["projections"] = new JArray(
                        new JObject { ["slot_group"] = "group1", ["state"] = "ready", ["effects"] = new JArray("eff1") }
                    ),
                    ["required_element_properties"] = new JArray("prop_a"),
                    ["element_statistics"] = new JObject {
                        ["level"] = new JArray("tag1")
                    },
                    ["actions"] = new JArray(
                        new JObject { ["name"] = "Action1", ["type"] = "Standard" }
                    )
                },
                ["elements"] = new JArray(
                    new JObject {
                        ["name"] = "Element1",
                        ["prop_a"] = "val_a",
                        ["level"] = 1,
                        ["discipline"] = "Iron Heart"
                    }
                )
            };

            var def = new SubsystemDefinition(json);

            Assert.AreEqual("test_sub", def.Id);
            Assert.AreEqual("Test Subsystem", def.Name);
            Assert.AreEqual("unit_x", def.PrerequisiteUnit);
            Assert.AreEqual("prop_y", def.PrerequisiteProperty);
            Assert.IsNotNull(def.LevelGating);
            Assert.AreEqual("level", def.LevelGating.ElementStatistic);
            Assert.AreEqual("Max Level", def.LevelGating.MaxLevelLabel);
            Assert.IsTrue(def.Taxonomies.ContainsKey("discipline"));
            Assert.AreEqual("iron_heart", def.Taxonomies["discipline"]["Iron Heart"].GetProperty<string>("key"));
            Assert.IsTrue(def.SlotGroups.ContainsKey("group1"));
            Assert.AreEqual(4, def.SlotGroups["group1"].FixedCapacity);
            Assert.IsTrue(def.Verbs.ContainsKey("verb1"));
            Assert.AreEqual(1, def.Verbs["verb1"].Count);
            Assert.IsTrue(def.DefaultElementVerbs.ContainsKey("elem_verb1"));
            Assert.AreEqual(1, def.DefaultElementVerbs["elem_verb1"].Count);
            Assert.IsTrue(def.Pools.ContainsKey("pool1"));
            Assert.AreEqual(10, def.Pools["pool1"].DefaultCapacity);
            Assert.IsTrue(def.EffectTemplates.ContainsKey("tmpl1"));
            Assert.AreEqual(1, def.Projections.Count);
            CollectionAssert.AreEqual(new[] { "prop_a" }, def.RequiredElementProperties);
            Assert.IsTrue(def.ElementStatistics.ContainsKey("level"));
            Assert.AreEqual(1, def.Actions.Count);
            Assert.IsTrue(def.Elements.ContainsKey("Element1"));
            Assert.AreEqual("Iron Heart", def.Elements["Element1"].Properties["discipline"]?.ToString());
        }

        [TestMethod]
        public void SubsystemDefinition_DirectJsonDeserialization_EnforcesInvariants() {
            // 1. Missing required property on element throws
            var missingPropJson = new JObject {
                ["subsystem"] = new JObject {
                    ["id"] = "test_sub",
                    ["required_element_properties"] = new JArray("mandatory_prop")
                },
                ["elements"] = new JArray(
                    new JObject { ["name"] = "Elem1" }
                )
            };
            var ex1 = Assert.ThrowsException<InvalidOperationException>(() => new SubsystemDefinition(missingPropJson));
            Assert.IsTrue(ex1.Message.Contains("missing required property 'mandatory_prop'"));

            // 2. Missing element statistic on element throws
            var missingStatJson = new JObject {
                ["subsystem"] = new JObject {
                    ["id"] = "test_sub",
                    ["element_statistics"] = new JObject {
                        ["level"] = new JArray()
                    }
                },
                ["elements"] = new JArray(
                    new JObject { ["name"] = "Elem1" }
                )
            };
            var ex2 = Assert.ThrowsException<InvalidOperationException>(() => new SubsystemDefinition(missingStatJson));
            Assert.IsTrue(ex2.Message.Contains("missing required element statistic 'level'"));

            // 3. Non-numeric element statistic on element throws
            var nonNumericStatJson = new JObject {
                ["subsystem"] = new JObject {
                    ["id"] = "test_sub",
                    ["element_statistics"] = new JObject {
                        ["level"] = new JArray()
                    }
                },
                ["elements"] = new JArray(
                    new JObject { ["name"] = "Elem1", ["level"] = "not_a_number" }
                )
            };
            var ex3 = Assert.ThrowsException<InvalidOperationException>(() => new SubsystemDefinition(nonNumericStatJson));
            Assert.IsTrue(ex3.Message.Contains("must be numeric"));

            // 4. Level gating referencing undefined element statistic throws
            var undefinedLevelGatingJson = new JObject {
                ["subsystem"] = new JObject {
                    ["id"] = "test_sub",
                    ["level_gating"] = new JObject {
                        ["element_statistic"] = "grade"
                    },
                    ["element_statistics"] = new JObject {
                        ["level"] = new JArray()
                    }
                }
            };
            var ex4 = Assert.ThrowsException<InvalidOperationException>(() => new SubsystemDefinition(undefinedLevelGatingJson));
            Assert.IsTrue(ex4.Message.Contains("level gating references element statistic 'grade' which is not defined in element_statistics"));
        }
    }
}
