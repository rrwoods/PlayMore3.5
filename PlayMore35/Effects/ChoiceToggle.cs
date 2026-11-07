using System;
using Newtonsoft.Json.Linq;
using PlayMore35.Items;

namespace PlayMore35.Effects {
    public class ChoiceToggle : IToggle {
        private readonly string expected;
        private readonly string? actual;

        public ChoiceToggle(string expected, string? actual = null) {
            this.expected = expected ?? "";
            this.actual = actual;
        }

        public JToken ToJson() {
            return new JObject(new JProperty("choice", expected));
        }

        public bool On(EffectPool pool, Equipped items) {
            return !string.IsNullOrEmpty(actual) &&
                   string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
        }

        public IToggle ApplyChoice(string choice) {
            if (string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase)) {
                return this;
            }
            return new ChoiceToggle(expected, choice);
        }
    }
}
