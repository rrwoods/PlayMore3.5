using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PlayMore35.Effects;

namespace PlayMore35.Tests {
    [TestClass]
    public class ChoiceToggleTests {
        [TestMethod]
        public void ChoiceToggle_InitialState_IsInactive() {
            var toggle = new ChoiceToggle("Feats");
            Assert.IsFalse(toggle.On(null, null));
        }

        [TestMethod]
        public void ChoiceToggle_ApplyMatchingChoice_IsActive() {
            var toggle = new ChoiceToggle("Feats");
            var applied = toggle.ApplyChoice("Feats");
            Assert.IsTrue(applied.On(null, null));
        }

        [TestMethod]
        public void ChoiceToggle_MatchingIsCaseInsensitive() {
            var toggle = new ChoiceToggle("Feats");
            var applied = toggle.ApplyChoice("feats");
            Assert.IsTrue(applied.On(null, null));
        }

        [TestMethod]
        public void ChoiceToggle_ApplyDifferentChoice_IsInactive() {
            var toggle = new ChoiceToggle("Feats");
            var applied = toggle.ApplyChoice("Warblade");
            Assert.IsFalse(applied.On(null, null));
        }

        [TestMethod]
        public void ChoiceToggle_ComposedWithUnToggle_InvertsChoice() {
            var toggle = new ChoiceToggle("Feats");
            var unToggle = new UnToggle(toggle);

            var featApplied = unToggle.ApplyChoice("Feats");
            Assert.IsFalse(featApplied.On(null, null));

            var warbladeApplied = unToggle.ApplyChoice("Warblade");
            Assert.IsTrue(warbladeApplied.On(null, null));
        }

        [TestMethod]
        public void ChoiceToggle_JsonRoundTrip_SerializesAndDeserializes() {
            var toggle = new ChoiceToggle("Feats");
            var json = toggle.ToJson();
            Assert.AreEqual("Feats", (string)json["choice"]);

            var restored = Toggle.FromJson(json);
            Assert.IsInstanceOfType(restored, typeof(ChoiceToggle));
            Assert.IsTrue(restored.ApplyChoice("Feats").On(null, null));
            Assert.IsFalse(restored.ApplyChoice("Warblade").On(null, null));
        }

        [TestMethod]
        public void Effect_WithGatedModifier_ActivatesOnlyWhenChoiceMatches() {
            var effectJson = JObject.Parse(@"{
                ""name"": ""Test Gated Benefit"",
                ""modifiers"": [
                    {
                        ""affects"": ""initiator level:feats"",
                        ""active"": { ""choice"": ""Feats"" },
                        ""mod"": {
                            ""type"": ""untyped"",
                            ""amounts"": [ { ""fixed"": 1 } ]
                        }
                    },
                    {
                        ""affects"": ""maneuvers known:${class}"",
                        ""mod"": {
                            ""type"": ""untyped"",
                            ""amounts"": [ { ""fixed"": 1 } ]
                        }
                    }
                ]
            }");

            var effect = new Effect(effectJson);
            var statFeatsIL = new Statistic("initiator level:feats");
            var statKnownWarblade = new Statistic("maneuvers known:warblade");
            var statKnownFeats = new Statistic("maneuvers known:feats");

            // Apply choice "Feats"
            var featsApplied = effect.ApplyChoice("Feats");
            var pool = new EffectPool(new PlayMore35.CharacterChoices.Character());

            var featsILMods = featsApplied.GetModifiers(pool, null, statFeatsIL).ToList();
            Assert.AreEqual(1, featsILMods.Count, "Expected initiator level:feats to be active when choice is Feats");

            var featsKnownMods = featsApplied.GetModifiers(pool, null, statKnownFeats).ToList();
            Assert.AreEqual(1, featsKnownMods.Count, "Expected maneuvers known:feats to be active when choice is Feats");

            // Apply choice "Warblade"
            var warbladeApplied = effect.ApplyChoice("Warblade");

            var warbladeILMods = warbladeApplied.GetModifiers(pool, null, statFeatsIL).ToList();
            Assert.AreEqual(0, warbladeILMods.Count, "Expected initiator level:feats to be INACTIVE when choice is Warblade");

            var warbladeKnownMods = warbladeApplied.GetModifiers(pool, null, statKnownWarblade).ToList();
            Assert.AreEqual(1, warbladeKnownMods.Count, "Expected maneuvers known:warblade to be active when choice is Warblade");
        }
    }
}
