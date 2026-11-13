using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Items;
using PlayMore35.Subsystems;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlayMore35.Tests {
    [TestClass]
    public class TimelineTests {

        [TestMethod]
        public void Advance_IncrementsCurrentRound_AndProjectsCalendarDate() {
            var roundZero = new DateTime(612, 2, 28, 23, 59, 54); // Leap year 612
            var timeline = new Timeline();
            timeline.SetCampaignTime(roundZero);

            Assert.AreEqual(0L, timeline.CurrentRound);
            Assert.AreEqual(roundZero, timeline.CurrentTime);

            timeline.Advance(1);
            Assert.AreEqual(1L, timeline.CurrentRound);
            Assert.AreEqual(new DateTime(612, 2, 29, 0, 0, 0), timeline.CurrentTime);

            timeline.Advance(14400); // 1 full day across leap day
            Assert.AreEqual(14401L, timeline.CurrentRound);
            Assert.AreEqual(new DateTime(612, 3, 1, 0, 0, 0), timeline.CurrentTime);
            Assert.AreEqual(new DateTime(612, 2, 29, 0, 0, 0), timeline.GetTimeAtRound(1));
        }

        [TestMethod]
        public void AdvanceTo_UpdatesCurrentRound_AcrossMultiCenturyPre1900Spans() {
            var roundZero = new DateTime(1, 1, 1, 0, 0, 0);
            var timeline = new Timeline();
            timeline.SetCampaignTime(roundZero);

            var targetTime = new DateTime(1612, 5, 14, 20, 15, 0);
            TimelineAdvanceResult result = timeline.AdvanceTo(targetTime);

            Assert.IsNotNull(result);
            Assert.AreEqual(targetTime, timeline.CurrentTime);

            var maxTime = new DateTime(9999, 12, 31, 23, 59, 54);
            timeline.AdvanceTo(maxTime);
            Assert.AreEqual(maxTime, timeline.CurrentTime);
        }

        [TestMethod]
        public void AdvanceTo_ThrowsArgumentOutOfRange_WhenTargetTimePrecedesCurrentTime() {
            var timeline = new Timeline();
            timeline.SetCampaignTime(new DateTime(612, 5, 14, 20, 15, 0));

            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
                timeline.AdvanceTo(new DateTime(612, 5, 14, 20, 14, 54))
            );
            Assert.AreEqual(0L, timeline.CurrentRound);
        }

        [TestMethod]
        public void SetCampaignTime_RebasesRoundZero_WhilePreservingCurrentRoundScheduledActionsAndParameters() {
            var initialRoundZero = new DateTime(2026, 10, 6, 20, 11, 30);
            var timeline = new Timeline();
            timeline.SetCampaignTime(initialRoundZero);
            timeline.Advance(5);
            timeline.StartCombat();
            timeline.EndCombat();
            timeline.Advance(28830);
            var character = new Character { Timeline = timeline };
            var effects = new EffectPool(character);
            var dayParam = new ControllableParameter("day_res", character) { Value = 2 };
            effects.resettableParameters["day"].Add(dayParam);
            var context = new FakeCharacterContext(character, effects, new Equipped());
            timeline.Context = context;

            var scheduled = new SheetAction { Name = "Unbind vestige slot 0" };
            timeline.Schedule(scheduled, "1 day", context); // scheduled at 28835 + 14400 = 43235

            // Set campaign time to May 14, 612, 8:15:05 PM (quantizes to 8:15:00 PM)
            timeline.SetCampaignTime(new DateTime(612, 5, 14, 20, 15, 5));

            Assert.AreEqual(28835L, timeline.CurrentRound);
            Assert.AreEqual(new DateTime(612, 5, 14, 20, 15, 0), timeline.CurrentTime);
            Assert.AreEqual(new DateTime(612, 5, 12, 20, 11, 30), timeline.RoundZero);
            Assert.AreEqual(new DateTime(612, 5, 12, 20, 12, 0), timeline.EncounterStartTime);
            Assert.AreEqual(new DateTime(28835L * Timeline.TicksPerRound), timeline.MinCampaignTime);
            Assert.AreEqual(1, timeline.GetScheduledActions(43235).Count);
            Assert.AreEqual(2, dayParam.Value, "SetCampaignTime must not reset periodic parameters.");

            var serialized = (JObject)timeline.ToJson();
            Assert.IsNull(serialized["encounter start time"], "Timeline JSON must omit redundant encounter start time.");
        }

        [TestMethod]
        public void SetCampaignTime_ThrowsArgumentOutOfRange_WhenRoundZeroWouldUnderflowOrExceedMax() {
            var timeline = new Timeline();
            timeline.SetCampaignTime(new DateTime(612, 5, 14, 0, 0, 0));
            timeline.Advance(10);

            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
                timeline.SetCampaignTime(new DateTime(1, 1, 1, 0, 0, 54)) // 9 rounds after MinValue < 10 rounds needed
            );

            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
                timeline.SetCampaignTime(new DateTime(9999, 12, 31, 23, 59, 55))
            );
        }

        [TestMethod]
        public void Advance_ZeroesPeriodicParameters_AcrossCadenceThresholds() {
            var roundZero = new DateTime(2026, 1, 1, 0, 0, 0); // Midnight Thursday
            var timeline = new Timeline();
            timeline.SetCampaignTime(roundZero);
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

            var context = new FakeCharacterContext(character, effects, new Equipped());
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
                new JProperty("feats", new JArray()),
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

        [TestMethod]
        public void Timeline_ActiveActions_YieldsAcrossScheduledAndEncounterActions_AllocationFree() {
            var timeline = new Timeline();
            var schedAction1 = new SheetAction { Name = "Sched1" };
            var schedAction2 = new SheetAction { Name = "Sched2" };
            var encounterAction = new SheetAction { Name = "Enc1" };

            timeline.Schedule(schedAction1, "1 round");
            timeline.Schedule(schedAction2, "2 rounds");
            timeline.Schedule(encounterAction, "1 encounter");

            Assert.AreEqual(2, timeline.ScheduledActionCount);
            Assert.AreEqual(1, timeline.GetScheduledActions(1).Count);
            Assert.AreSame(schedAction1, timeline.GetScheduledActions(1)[0]);
            Assert.AreEqual(1, timeline.GetScheduledActions(2).Count);
            Assert.AreSame(schedAction2, timeline.GetScheduledActions(2)[0]);
            Assert.AreEqual(0, timeline.GetScheduledActions(99).Count);

            var active = timeline.ActiveActions.ToList();
            Assert.AreEqual(3, active.Count);
            CollectionAssert.Contains(active, schedAction1);
            CollectionAssert.Contains(active, schedAction2);
            CollectionAssert.Contains(active, encounterAction);
        }
    }
}
