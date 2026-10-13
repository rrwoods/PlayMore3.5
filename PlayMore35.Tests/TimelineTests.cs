using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Items;
using PlayMore35.Subsystems;
using System;
using System.Collections.Generic;

namespace PlayMore35.Tests {
    [TestClass]
    public class TimelineTests {
        private class MockCharacterContext : ICharacterContext {
            public Character Character { get; set; }
            public EffectPool Effects { get; set; }
            public Equipped EquippedItems { get; set; }
            public Timeline Timeline => Character?.Timeline;
            public SubsystemRegistry Subsystems { get; } = new SubsystemRegistry();

            public List<AttackRoutine> GetAllRoutines() => new List<AttackRoutine>();
            public List<AttackRoutine> GetVisibleRoutines() => new List<AttackRoutine>();
        }

        [TestMethod]
        public void Advance_IncrementsCurrentRound_AndProjectsCalendarDate() {
            var roundZero = new DateTime(2026, 1, 1, 12, 0, 0);
            var json = new JObject(
                new JProperty("round zero", roundZero.ToString("O")),
                new JProperty("encounter start time", roundZero.ToString("O")),
                new JProperty("encounter start round", 0L),
                new JProperty("current round", 0L),
                new JProperty("combat", false)
            );
            var timeline = new Timeline(json);

            Assert.AreEqual(0L, timeline.CurrentRound);
            Assert.AreEqual(roundZero, timeline.CurrentTime);

            TimelineAdvanceResult res1 = timeline.Advance(1);
            Assert.AreEqual(1L, timeline.CurrentRound);
            Assert.AreEqual(roundZero.AddSeconds(6), timeline.CurrentTime);
            Assert.AreEqual(1L, res1.RoundsAdvanced);
            Assert.AreEqual(roundZero, res1.PreviousTime);
            Assert.AreEqual(timeline.CurrentTime, res1.CurrentTime);

            TimelineAdvanceResult res10 = timeline.Advance(10);
            Assert.AreEqual(11L, timeline.CurrentRound);
            Assert.AreEqual(roundZero.AddSeconds(66), timeline.CurrentTime);
            Assert.AreEqual(10L, res10.RoundsAdvanced);

            TimelineAdvanceResult resHour = timeline.Advance(600);
            Assert.AreEqual(611L, timeline.CurrentRound);
            Assert.AreEqual(roundZero.AddSeconds(66 + 3600), timeline.CurrentTime);
        }

        [TestMethod]
        public void SetTime_UpdatesCurrentRound_AndReturnsAdvanceResult() {
            var roundZero = new DateTime(2026, 1, 1, 12, 0, 0);
            var json = new JObject(
                new JProperty("round zero", roundZero.ToString("O")),
                new JProperty("encounter start time", roundZero.ToString("O")),
                new JProperty("encounter start round", 0L),
                new JProperty("current round", 0L),
                new JProperty("combat", false)
            );
            var timeline = new Timeline(json);

            var targetTime = roundZero.AddMinutes(5); // 300 seconds = 50 rounds
            TimelineAdvanceResult result = timeline.SetTime(targetTime);

            Assert.AreEqual(50L, timeline.CurrentRound);
            Assert.AreEqual(targetTime, timeline.CurrentTime);
            Assert.AreEqual(50L, result.RoundsAdvanced);
            Assert.AreEqual(roundZero, result.PreviousTime);
            Assert.AreEqual(targetTime, result.CurrentTime);
        }

        [TestMethod]
        public void Advance_ZeroesPeriodicParameters_AcrossCadenceThresholds() {
            var roundZero = new DateTime(2026, 1, 1, 0, 0, 0); // Midnight Thursday
            var json = new JObject(
                new JProperty("round zero", roundZero.ToString("O")),
                new JProperty("encounter start time", roundZero.ToString("O")),
                new JProperty("encounter start round", 0L),
                new JProperty("current round", 0L),
                new JProperty("combat", false)
            );
            var timeline = new Timeline(json);
            var character = new Character { Timeline = timeline };

            var effects = new EffectPool(character);
            var roundParam = new ControllableParameter("round_res", character);
            var minParam = new ControllableParameter("min_res", character);
            var hourParam = new ControllableParameter("hour_res", character);
            var dayParam = new ControllableParameter("day_res", character);
            var weekParam = new ControllableParameter("week_res", character);

            roundParam.Value = 3;
            minParam.Value = 3;
            hourParam.Value = 3;
            dayParam.Value = 3;
            weekParam.Value = 3;

            effects.resettableParameters["round"].Add(roundParam);
            effects.resettableParameters["minute"].Add(minParam);
            effects.resettableParameters["hour"].Add(hourParam);
            effects.resettableParameters["day"].Add(dayParam);
            effects.resettableParameters["week"].Add(weekParam);

            var context = new MockCharacterContext {
                Character = character,
                Effects = effects,
                EquippedItems = new Equipped()
            };
            timeline.Context = context;

            // 1. Advance 1 round (6s) -> only round parameter resets
            var resRound = timeline.Advance(1);
            Assert.AreEqual(0, roundParam.Value);
            Assert.AreEqual(3, minParam.Value);
            Assert.AreEqual(3, hourParam.Value);
            Assert.AreEqual(3, dayParam.Value);
            Assert.AreEqual(3, weekParam.Value);
            Assert.AreEqual(TimelineRefreshRequirement.RecomputeDerived, resRound.RefreshRequirement);

            // 2. Set round & minute param to 3, advance 9 rounds to reach 10 rounds (1 minute boundary)
            roundParam.Value = 3;
            var resMin = timeline.Advance(9);
            Assert.AreEqual(0, roundParam.Value);
            Assert.AreEqual(0, minParam.Value);
            Assert.AreEqual(3, hourParam.Value);
            Assert.AreEqual(3, dayParam.Value);
            Assert.AreEqual(3, weekParam.Value);
            Assert.AreEqual(TimelineRefreshRequirement.RecomputeDerived, resMin.RefreshRequirement);

            // 3. Set minute & hour param to 3, advance 590 rounds to reach 600 rounds (1 hour boundary)
            minParam.Value = 3;
            var resHour = timeline.Advance(590);
            Assert.AreEqual(0, minParam.Value);
            Assert.AreEqual(0, hourParam.Value);
            Assert.AreEqual(3, dayParam.Value);
            Assert.AreEqual(3, weekParam.Value);

            // 4. Set hour & day param to 3, advance 13800 rounds to reach 14400 rounds (1 day boundary)
            hourParam.Value = 3;
            var resDay = timeline.Advance(13800);
            Assert.AreEqual(0, hourParam.Value);
            Assert.AreEqual(0, dayParam.Value);
            Assert.AreEqual(3, weekParam.Value);

            // 5. Set day & week param to 3, advance 86400 rounds to reach 100800 rounds (1 week boundary)
            dayParam.Value = 3;
            var resWeek = timeline.Advance(86400);
            Assert.AreEqual(0, dayParam.Value);
            Assert.AreEqual(0, weekParam.Value);
        }

        [TestMethod]
        public void Character_DeserializesLegacyTimeJson_Seamlessly() {
            var roundZero = new DateTime(2026, 3, 15, 14, 30, 0);
            var characterJson = new JObject(
                new JProperty("name", "Odric"),
                new JProperty("gender", "Male"),
                new JProperty("alignment", "Chaotic Good"),
                new JProperty("race", "Human"),
                new JProperty("assigned", new JObject(
                    new JProperty("STR", 16), new JProperty("DEX", 14), new JProperty("CON", 14),
                    new JProperty("INT", 10), new JProperty("WIS", 12), new JProperty("CHA", 8)
                )),
                new JProperty("bonus languages", new JArray()),
                new JProperty("classes", new JArray("Binder")),
                new JProperty("incremented", new JObject()),
                new JProperty("hit die rolls", new JArray(8)),
                new JProperty("skill ranks", new JObject()),
                new JProperty("feats", new JObject()),
                new JProperty("feat choices", new JObject()),
                new JProperty("transactions", new JArray()),
                new JProperty("inventory", new JObject()),
                new JProperty("equipped", new EquippedChoices().ToJson()),
                new JProperty("sessions", new JArray()),
                new JProperty("effects", new JArray()),
                new JProperty("subsystems", new JObject()),
                new JProperty("flag values", new JObject()),
                new JProperty("parameter values", new JObject()),
                new JProperty("time", new JObject(
                    new JProperty("round zero", roundZero.ToString("O")),
                    new JProperty("encounter start time", roundZero.ToString("O")),
                    new JProperty("encounter start round", 5L),
                    new JProperty("current round", 42L),
                    new JProperty("combat", true),
                    new JProperty("scheduled actions", new JObject()),
                    new JProperty("encounter actions", new JArray())
                ))
            );

            var character = new Character(characterJson);
            Assert.IsNotNull(character.Timeline);
            Assert.AreEqual(42L, character.Timeline.CurrentRound);
            Assert.AreEqual(5L, character.Timeline.EncounterStartRound);
            Assert.IsTrue(character.Timeline.Combat);
            Assert.AreEqual(roundZero, character.Timeline.RoundZero);
            Assert.AreEqual(roundZero.AddSeconds(42 * 6), character.Timeline.CurrentTime);
        }
    }
}
