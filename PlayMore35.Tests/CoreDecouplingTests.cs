using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Items;
using PlayMore35.Subsystems;
using PlayMore35.Subsystems.PactMagic;

namespace PlayMore35.Tests {
    [TestClass]
    public class CoreDecouplingTests {
        private class TestContextWithSlottedSubsystem : ICharacterContext {
            public Character Character { get; set; } = new Character();
            public EffectPool Effects { get; set; } = new EffectPool();
            public Equipped EquippedItems { get; set; } = new Equipped();
            public Timeline Timeline => Character?.Timeline;
            public SubsystemRegistry Subsystems { get; } = new SubsystemRegistry();

            public void ClearSlot(string subsystem, int slotIndex) {
                var slotted = Subsystems.Get(subsystem) as ISlottedSubsystem;
                slotted?.ClearSlot(slotIndex);
            }

            public List<AttackRoutine> GetAllRoutines() {
                return AttackRoutineEvaluator.EvaluateRoutines(Effects, EquippedItems, 0);
            }

            public List<AttackRoutine> GetVisibleRoutines() {
                var all = GetAllRoutines();
                var prefs = Character?.RoutinePreferences ?? new AttackRoutinePreferences();
                return all.Where(r => prefs.IsCategoryVisible(r.Category)).ToList();
            }
        }

        private class SpySlottedSubsystem : ISlottedSubsystem {
            public string Id => "spy-slotted";
            public bool IsActive => true;
            public int SlotCount => 2;
            public List<int> ClearedSlots { get; } = new List<int>();

            public void Initialize(ICharacterContext context) { }
            public void ContributeEffects(EffectPool pool, ICharacterContext context) { }
            public void ClearSlot(int slotIndex) {
                ClearedSlots.Add(slotIndex);
            }
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
                new JProperty("vestiges", new JObject(
                    new JProperty("stats", new JArray("generic capacity")),
                    new JProperty("effects", new JArray(
                        new JObject(
                            new JProperty("name", "generic feature effect")
                        )
                    ))
                ))
            );

            var @class = new CharacterClass(json);
            Assert.IsTrue(@class.Subsystems.ContainsKey("vestiges"));
            Assert.AreEqual(1, @class.Subsystems["vestiges"].Stats.Count);
            Assert.AreEqual("generic capacity", @class.Subsystems["vestiges"].Stats[0]);

            var pool = new EffectPool();
            pool.GenerateClassEffects(@class, 1);
            Assert.IsTrue(pool.stats.ContainsKey("generic capacity"));
            Assert.IsTrue(pool.Effects.Any(e => e.Name == "generic feature effect"));
        }

        [TestMethod]
        public void SheetAction_ClearSlot_DispatchesThroughSlottedSubsystem() {
            var context = new TestContextWithSlottedSubsystem();
            var spy = new SpySlottedSubsystem();
            context.Subsystems.Register(spy, "vestige");

            var action = new SheetAction().ClearSlot("vestige", 1);
            action.Perform(context);

            Assert.AreEqual(1, spy.ClearedSlots.Count);
            Assert.AreEqual(1, spy.ClearedSlots[0]);
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
