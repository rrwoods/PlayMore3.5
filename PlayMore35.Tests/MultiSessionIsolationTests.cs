using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Items;
using PlayMore35.Rules;
using PlayMore35.Subsystems.PactMagic;

namespace PlayMore35.Tests {
    [TestClass]
    public class MultiSessionIsolationTests {
        private IRulesCatalog catalog;

        [TestInitialize]
        public void Setup() {
            catalog = new GameRulesCatalog();
        }

        [TestMethod]
        public void MultiSessionIsolation_MultipleSessionsShareSingleCatalog_EncounterTrackerSimulation() {
            // Simulate Encounter Tracker: multiple distinct characters sharing a single static/singleton IRulesCatalog
            var character1 = new Character { name = "Fighter A", race = "human" };
            character1.classLevels.Add("Fighter");
            character1.assignedAbilityScores["STR"] = 16;
            character1.assignedAbilityScores["DEX"] = 12;

            var character2 = new Character { name = "Monster B", race = "orc" };
            character2.classLevels.Add("Barbarian");
            character2.assignedAbilityScores["STR"] = 18;
            character2.assignedAbilityScores["DEX"] = 14;

            var session1 = new CharacterSession(character1, catalog);
            var session2 = new CharacterSession(character2, catalog);

            Assert.AreSame(catalog, session1.Catalog, "Session 1 should share the catalog.");
            Assert.AreSame(catalog, session2.Catalog, "Session 2 should share the same catalog.");

            Assert.AreEqual("Fighter A", session1.Character.name);
            Assert.AreEqual("Monster B", session2.Character.name);
            Assert.AreEqual(16, session1.GetStatistic(Constants.STR_SCORE).Value);
            Assert.AreEqual(18, session2.GetStatistic(Constants.STR_SCORE).Value);
        }

        [TestMethod]
        public void MultiSessionIsolation_MutationsOnOneSessionDoNotLeakToOtherSession() {
            var character1 = new Character { name = "Hero" };
            character1.classLevels.Clear();
            character1.classLevels.Add("Fighter");
            character1.inventory["Pack"] = new InventoryLocation();

            var character2 = new Character { name = "Sidekick" };
            character2.classLevels.Clear();
            character2.classLevels.Add("Rogue");
            character2.inventory["Pack"] = new InventoryLocation();

            var session1 = new CharacterSession(character1, catalog);
            var session2 = new CharacterSession(character2, catalog);

            // Mutate session 1 extensively: ability scores, class levels, equipment, routine preferences, flags, parameters
            session1.Mutate(c => {
                c.assignedAbilityScores["STR"] = 20;
                c.classLevels.Add("Fighter");
                c.RoutinePreferences.SetCategoryVisible(RoutineCategory.ArmorSpikesTWF, true);
            });
            session1.AddWeapon("Pack", "longsword", new List<string>());
            var weaponChoice = session1.Character.inventory["Pack"].weapons[0];
            session1.EquipWeapon("Pack", true, weaponChoice);
            session1.SetFlag("charging", true);
            session1.SetParameter(Constants.LETHAL_DAMAGE, 12);

            // Verify session 1 reflects all changes
            Assert.AreEqual(20, session1.GetStatistic(Constants.STR_SCORE).Value);
            Assert.AreEqual(2, session1.Character.classLevels.Count);
            Assert.IsTrue(session1.Character.RoutinePreferences.IsCategoryVisible(RoutineCategory.ArmorSpikesTWF));
            Assert.IsNotNull(session1.Character.equipped.PrimaryHandWeapon);
            Assert.AreEqual("longsword", session1.Character.equipped.PrimaryHandWeapon.Name);
            Assert.IsTrue(session1.Character.flagValues["charging"]);
            Assert.AreEqual(12, session1.Character.paramValues[Constants.LETHAL_DAMAGE]);

            // Verify session 2 remains completely pristine and unaffected
            Assert.AreEqual(8, session2.GetStatistic(Constants.STR_SCORE).Value);
            Assert.AreEqual(1, session2.Character.classLevels.Count);
            Assert.AreEqual("Rogue", session2.Character.classLevels[0]);
            Assert.IsFalse(session2.Character.RoutinePreferences.IsCategoryVisible(RoutineCategory.ArmorSpikesTWF));
            Assert.IsNull(session2.Character.equipped.PrimaryHandWeapon);
            Assert.IsFalse(session2.Character.flagValues.ContainsKey("charging") && session2.Character.flagValues["charging"]);
            Assert.AreEqual(0, session2.Character.paramValues.ContainsKey(Constants.LETHAL_DAMAGE) ? session2.Character.paramValues[Constants.LETHAL_DAMAGE] : 0);
        }

        [TestMethod]
        public void MultiSessionIsolation_TimelineProgressionIndependent() {
            var character1 = new Character { name = "Active Combatant" };
            var character2 = new Character { name = "Idle Combatant" };

            var session1 = new CharacterSession(character1, catalog);
            var session2 = new CharacterSession(character2, catalog);

            var action = new SheetAction()
                .SetFlag("charging")
                .Duration("1 round");
            action.Perform((ICharacterContext)session1);

            Assert.IsTrue(session1.Character.flagValues["charging"]);

            var initialRound2 = session2.Timeline.CurrentRound;

            // Advance Session 1 by 1 round
            session1.AdvanceTimeline(new LengthOfTime("1 round"));

            // Session 1 expired the timed action
            Assert.IsFalse(session1.Character.flagValues["charging"]);
            Assert.AreEqual(1, session1.Timeline.CurrentRound);

            // Session 2 is untouched
            Assert.AreEqual(initialRound2, session2.Timeline.CurrentRound);
            Assert.IsFalse(session2.Character.flagValues.ContainsKey("charging") && session2.Character.flagValues["charging"]);
        }

        [TestMethod]
        public void OdricJson_BackwardsCompatibility_LoadsAndRecalculatesAccurately() {
            string odricPath = TestEnvironment.FindOdricJsonPath();
            string jsonText = File.ReadAllText(odricPath);
            JObject jObj = JObject.Parse(jsonText);

            var character = new Character(jObj);
            var session = new CharacterSession(character, catalog);

            // Assert metadata & choices
            Assert.AreEqual("Odric Greatshield", session.Character.name);
            Assert.AreEqual("Male", session.Character.gender);
            Assert.AreEqual("NG", session.Character.alignment);
            Assert.AreEqual("dream dwarf", session.Character.race);
            Assert.AreEqual(4, session.Character.classLevels.Count);
            CollectionAssert.AreEqual(new[] { "Binder", "Barbarian", "Fighter", "Fighter" }, session.Character.classLevels.ToList());

            // Assert feats & choices
            Assert.AreEqual("Improved Binding", session.Character.feats["Level 1"]);
            Assert.AreEqual("Constant Guardian", session.Character.feats["Level 3"]);
            Assert.AreEqual("Endurance", session.Character.feats["Fighter 1"]);
            Assert.AreEqual("Weapon Focus", session.Character.feats["Fighter 2"]);
            Assert.AreEqual("dagger", session.Character.featChoices["Fighter 2"]);

            // Assert Subsystems: Pact Magic
            Assert.IsTrue(session.Character.Subsystems.ContainsKey("pact_magic"), "Should contain pact_magic subsystem.");
            var pactMagic = session.Character.Subsystems["pact_magic"];
            Assert.IsNotNull(pactMagic);
            Assert.IsTrue(pactMagic.Slots.ContainsKey("vestiges"));
            Assert.AreEqual(1, pactMagic.Slots["vestiges"].Count);
            Assert.AreEqual("Savnok", pactMagic.Slots["vestiges"][0].Element);
            Assert.AreEqual("poor_pact", pactMagic.Slots["vestiges"][0].State);

            var pactEngine = session.Subsystems.Get<PactMagicEngine>();
            Assert.IsNotNull(pactEngine, "PactMagicEngine should be registered.");
            Assert.IsTrue(pactEngine.IsActive, "PactMagicEngine should be active for Binder.");
            Assert.AreEqual(1, pactEngine.SlotCount);
            Assert.AreEqual("Savnok", pactEngine.GetSlot(0)?.VestigeName);
            Assert.AreEqual(PactState.POOR_PACT, pactEngine.GetSlot(0)?.State);
            Assert.AreEqual("Savnok", pactEngine.CurrentVestige(0)?.Name);

            // Assert Equipment
            Assert.IsNotNull(session.Character.equipped.PrimaryHandWeapon);
            Assert.AreEqual("morningstar", session.Character.equipped.PrimaryHandWeapon.Name);
            Assert.IsNotNull(session.Character.equipped.DonnedArmor);
            Assert.AreEqual("full plate", session.Character.equipped.DonnedArmor.Name);
            Assert.IsNotNull(session.Character.equipped.OffHandShield);
            Assert.AreEqual("heavy wooden shield", session.Character.equipped.OffHandShield.Name);

            // Assert Statistics calculation without errors
            var hp = session.GetStatistic(Constants.MAX_HP);
            Assert.IsTrue(hp.Value > 0, "Max HP should be positive.");

            var ac = session.GetStatistic(Constants.AC);
            Assert.IsTrue(ac.Value >= 10, "AC should be calculated.");

            var fort = session.GetStatistic(Constants.FORT_SAVES);
            Assert.IsTrue(fort.Value > 0, "Fortitude save should be positive.");

            var routines = session.GetAllRoutines();
            Assert.IsTrue(routines.Count > 0, "Should generate attack routines.");
        }

        [TestMethod]
        public void OdricJson_BackwardsCompatibility_SerializesCleanly() {
            string odricPath = TestEnvironment.FindOdricJsonPath();
            string jsonText = File.ReadAllText(odricPath);
            JObject jObj = JObject.Parse(jsonText);

            var session = new CharacterSession(new Character(jObj), catalog);
            var serialized = (JObject)session.Character.ToJson();

            // Verify essential keys exist in output
            string[] requiredKeys = {
                "name", "gender", "alignment", "assigned", "bonus languages", "race", "classes",
                "incremented", "hit die rolls", "skill ranks", "feats", "feat choices", "transactions",
                "inventory", "equipped", "sessions", "effects", "subsystems", "routine preferences",
                "flag values", "parameter values", "time"
            };

            foreach (var key in requiredKeys) {
                Assert.IsNotNull(serialized[key], $"Serialized JSON must contain key '{key}'.");
            }

            var serializedTime = serialized["time"] as JObject;
            Assert.IsNotNull(serializedTime);
            if (serializedTime["scheduled actions"] is JObject scheduled) {
                foreach (var prop in scheduled.Properties()) {
                    if (prop.Value is JArray actions) {
                        foreach (var act in actions) {
                            Assert.IsNull(act["unbind"], "Serialized sheet actions should not include redundant unbind key.");
                        }
                    }
                }
            }

            // Verify round-trip load
            var reloadedCharacter = new Character(serialized);
            var reloadedSession = new CharacterSession(reloadedCharacter, catalog);

            Assert.AreEqual(session.Character.name, reloadedSession.Character.name);
            Assert.AreEqual(session.Character.race, reloadedSession.Character.race);
            Assert.AreEqual(session.GetStatistic(Constants.MAX_HP).Value, reloadedSession.GetStatistic(Constants.MAX_HP).Value);
            Assert.AreEqual(session.GetStatistic(Constants.AC).Value, reloadedSession.GetStatistic(Constants.AC).Value);
            Assert.AreEqual(session.GetStatistic(Constants.FORT_SAVES).Value, reloadedSession.GetStatistic(Constants.FORT_SAVES).Value);
            Assert.AreEqual(session.GetAllRoutines().Count, reloadedSession.GetAllRoutines().Count);
        }
    }
}
