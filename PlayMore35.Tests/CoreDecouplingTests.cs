using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Items;
using PlayMore35.Rules;
using PlayMore35.Subsystems;

namespace PlayMore35.Tests {
    [TestClass]
    public class CoreDecouplingTests {
        private class TestContextWithSlottedSubsystem : FakeCharacterContext {
        }

        [TestMethod]
        public void Character_HasNoVestigeFieldsOrEnums() {
            var fields = typeof(Character).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsFalse(fields.Any(f => f.Name.ToLower().Contains("vestige")), "Character must have zero vestige fields.");

            var nestedTypes = typeof(Character).GetNestedTypes();
            Assert.IsFalse(nestedTypes.Any(t => t.Name == "PactState"), "Character must not define PactState enum.");
        }

        [TestMethod]
        public void EffectPool_HasNoVestigeMethods() {
            var methods = typeof(EffectPool).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsFalse(methods.Any(m => m.Name.Contains("Vestige")), "EffectPool must have zero vestige-specific methods.");
        }

        [TestMethod]
        public void CharacterClass_Subsystems_ParsedAndContributedGenerically() {
            var json = new JObject(
                new JProperty("name", "TestClass"),
                new JProperty("prerequisites", ""),
                new JProperty("hit die", 8),
                new JProperty("class skills", new JArray()),
                new JProperty("skill points", 2),
                new JProperty("base attack bonus", "SLOW"),
                new JProperty("base fort save", "SLOW"),
                new JProperty("base ref save", "SLOW"),
                new JProperty("base will save", "SLOW"),
                new JProperty("weapon proficiencies", new JArray()),
                new JProperty("armor proficiencies", new JArray()),
                new JProperty("shield proficiencies", new JArray()),
                new JProperty("class features", new JArray()),
                new JProperty("subsystems", new JObject(
                    new JProperty("test_subsystem", new JObject(
                        new JProperty("stats", new JArray("generic capacity")),
                        new JProperty("effects", new JArray(
                            new JObject(
                                new JProperty("name", "generic feature effect")
                            )
                        ))
                    ))
                ))
            );

            var @class = new CharacterClass(json);
            Assert.IsTrue(@class.Subsystems.ContainsKey("test_subsystem"));
            Assert.AreEqual(1, @class.Subsystems["test_subsystem"].Stats.Count);
            Assert.AreEqual("generic capacity", @class.Subsystems["test_subsystem"].Stats[0]);

            var serialized = (JObject)@class.ToJson();
            Assert.IsNotNull(serialized["subsystems"]?["test_subsystem"]);
            Assert.IsNull(serialized["vestiges"]);

            var pool = new EffectPool(new Character());
            pool.GenerateClassEffects(@class, 1);
            Assert.IsTrue(pool.stats.ContainsKey("generic capacity"));
            Assert.IsTrue(pool.Effects.Any(e => e.Name == "generic feature effect"));
        }

        [TestMethod]
        public void SubsystemAction_UnbindVestige_DispatchesThroughSubsystemEngine() {
            var context = new TestContextWithSlottedSubsystem();
            var catalog = new GameRulesCatalog();
            var engine = new SubsystemEngine(catalog.GetSubsystem("pact_magic"), context);
            context.SubsystemsMap["pact_magic"] = engine;

            context.Effects.AddStat(new Statistic("number of vestiges"));
            context.Effects.AddEffect(new Effect("Vestige Slot").AddModifier("number of vestiges", new Modifier(1, ModifierTypes.UNTYPED)));

            engine.AssignSlot("vestiges", 0, "Aym", "good_pact", "Binder");
            Assert.AreEqual("Aym", engine.GetSlotElement("vestiges", 0));

            var action = new SheetAction { Subsystem = "pact_magic", SlotGroup = "vestiges", SlotIndex = 0, Verb = "unbind" };
            action.Perform(context);

            Assert.IsNull(engine.GetSlotElement("vestiges", 0));
            Assert.AreEqual("empty", engine.GetSlotState("vestiges", 0));
        }

        [TestMethod]
        public void CoreConstants_ContainsNoSubsystemStatistics() {
            var fields = typeof(Constants).GetFields(BindingFlags.Public | BindingFlags.Static);
            var names = fields.Select(f => f.Name).ToList();
            var values = fields.Where(f => f.FieldType == typeof(string)).Select(f => (string)f.GetValue(null)).ToList();

            CollectionAssert.DoesNotContain(names, "NUM_VESTIGES");
            CollectionAssert.DoesNotContain(names, "BINDING_CHECK");
            CollectionAssert.DoesNotContain(names, "MAX_VESTIGE_LEVEL");
            CollectionAssert.DoesNotContain(names, "EFFECTIVE_BINDER_LEVEL");
            CollectionAssert.DoesNotContain(names, "ACTIVE_STANCES");
            CollectionAssert.DoesNotContain(names, "INITIATOR_LEVEL_WARBLADE");
            CollectionAssert.DoesNotContain(names, "INITIATOR_LEVEL_CRUSADER");
            CollectionAssert.DoesNotContain(names, "INITIATOR_LEVEL_SWORDSAGE");
            CollectionAssert.DoesNotContain(names, "INITIATOR_LEVEL_FEATS");

            Assert.IsFalse(values.Any(v => v != null && v.Contains("vestige")), "Constants must not contain vestige stats.");
            Assert.IsFalse(values.Any(v => v != null && v.Contains("stances")), "Constants must not contain stance stats.");
            Assert.IsFalse(values.Any(v => v != null && v.Contains("initiator level:")), "Constants must not contain specific initiator level stats.");
        }

        [TestMethod]
        public void SubsystemDefinitions_DefineSubsystemStatistics() {
            var catalog = new GameRulesCatalog();
            Assert.IsTrue(catalog.Subsystems.ContainsKey("pact_magic"));
            var pact = catalog.Subsystems["pact_magic"];
            Assert.AreEqual("number of vestiges", pact.SlotGroups["vestiges"].CapacityStat);
            Assert.AreEqual("maximum vestige level", pact.SlotGroups["vestiges"].MaxLevel.Stat);

            Assert.IsTrue(catalog.Subsystems.ContainsKey("martial_maneuvers"));
            var martial = catalog.Subsystems["martial_maneuvers"];
            Assert.AreEqual("active stances", martial.SlotGroups["stances"].StateCapacities[0].Stat);
            Assert.AreEqual("maneuvers readied:{source}", martial.SlotGroups["maneuvers"].StateCapacities[0].Stat);
        }

        [TestMethod]
        public void CoreAssembly_ContainsNoLegacySubsystemEnginesOrParserRegistry() {
            var assemblyTypes = typeof(SubsystemEngine).Assembly.GetTypes().Select(t => t.Name).ToList();
            CollectionAssert.DoesNotContain(assemblyTypes, "ISubsystemEngine");
            CollectionAssert.DoesNotContain(assemblyTypes, "MartialManeuverEngine");
            CollectionAssert.DoesNotContain(assemblyTypes, "MartialManeuverState");
            CollectionAssert.DoesNotContain(assemblyTypes, "KnownManeuverSlot");
            CollectionAssert.DoesNotContain(assemblyTypes, "StanceSlot");
            CollectionAssert.DoesNotContain(assemblyTypes, "ManeuverState");
            CollectionAssert.DoesNotContain(assemblyTypes, "PactMagicEngine");
            CollectionAssert.DoesNotContain(assemblyTypes, "Maneuver");
            CollectionAssert.DoesNotContain(assemblyTypes, "MartialDiscipline");
            CollectionAssert.DoesNotContain(assemblyTypes, "ManeuverType");
            CollectionAssert.DoesNotContain(assemblyTypes, "SubsystemRegistry");

            Assert.AreEqual(0, typeof(SubsystemEngine).GetInterfaces().Length, "SubsystemEngine must not implement an ISubsystemEngine interface shim.");
            Assert.IsNull(typeof(SheetAction).GetMethod("RegisterParser", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
            Assert.IsNull(typeof(SheetAction).GetMethod("ResetParsersForTesting", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));

            var catalog = new GameRulesCatalog();
            var binder = catalog.GetClass("Binder");
            Assert.IsNotNull(binder);
            Assert.IsTrue(binder.Subsystems.ContainsKey("pact_magic"), "Binder must declare subsystem 'pact_magic' under 'subsystems'.");
            Assert.IsFalse(binder.Subsystems.ContainsKey("vestiges"), "Binder must not use legacy 'vestiges' subsystem key.");

            string projectDir = Path.Combine(TestEnvironment.FindRepoRoot(), "PlayMore35");
            string[] forbiddenClassNames = { "Warblade", "Crusader", "Swordsage", "Binder" };
            var csFiles = Directory.GetFiles(projectDir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)
                         && !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar));

            foreach (var file in csFiles) {
                string content = File.ReadAllText(file);
                foreach (var forbidden in forbiddenClassNames) {
                    Assert.IsFalse(
                        content.IndexOf(forbidden, StringComparison.OrdinalIgnoreCase) >= 0,
                        $"Core C# file '{Path.GetFileName(file)}' must not contain hardcoded class name '{forbidden}'."
                    );
                }
            }
        }

        [TestMethod]
        public void Character_DoesNotDeserializeLegacyVestigesJson() {
            var json = new JObject(
                new JProperty("name", "LegacyChar"),
                new JProperty("gender", ""),
                new JProperty("alignment", ""),
                new JProperty("race", ""),
                new JProperty("assigned", new JObject(
                    new JProperty("STR", 10), new JProperty("DEX", 10), new JProperty("CON", 10),
                    new JProperty("INT", 10), new JProperty("WIS", 10), new JProperty("CHA", 10)
                )),
                new JProperty("bonus languages", new JArray()),
                new JProperty("classes", new JArray("Binder")),
                new JProperty("incremented", new JObject()),
                new JProperty("hit die rolls", new JArray()),
                new JProperty("skill ranks", new JObject()),
                new JProperty("feats", new JObject()),
                new JProperty("feat choices", new JObject()),
                new JProperty("transactions", new JArray()),
                new JProperty("inventory", new JObject()),
                new JProperty("equipped", new EquippedChoices().ToJson()),
                new JProperty("sessions", new JArray()),
                new JProperty("effects", new JArray()),
                new JProperty("vestiges", new JArray(new JObject(
                    new JProperty("name", "Aym"),
                    new JProperty("state", "GOOD_PACT")
                ))),
                new JProperty("flag values", new JObject()),
                new JProperty("parameter values", new JObject()),
                new JProperty("time", new Timeline().ToJson())
            );

            var character = new Character(json);
            Assert.AreEqual(0, character.Subsystems.Count, "Character must not parse legacy json['vestiges'] into Subsystems.");
        }
    }
}
