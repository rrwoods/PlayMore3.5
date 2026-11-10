using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace PlayMore35.CharacterChoices {
    public class FeatAssignment : IJsonable, IEquatable<FeatAssignment> {
        public ProgressionSlotKey Slot { get; }
        public string Feat { get; set; }
        public Dictionary<string, string> Choices { get; }

        public FeatAssignment(ProgressionSlotKey slot, string feat, IDictionary<string, string>? choices = null) {
            Slot = slot ?? throw new ArgumentNullException(nameof(slot));
            Feat = feat ?? "";
            Choices = choices != null
                ? new Dictionary<string, string>(choices, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public FeatAssignment(JObject json)
            : this(
                ParseSlot(json),
                (string?)json?["feat"] ?? "",
                ParseChoices(json)) {
        }

        private static ProgressionSlotKey ParseSlot(JObject json) {
            if (json == null) throw new ArgumentNullException(nameof(json));
            var slotObj = (JObject?)json["slot"] ?? throw new ArgumentException("Missing 'slot' property", nameof(json));
            return new ProgressionSlotKey(slotObj);
        }

        private static Dictionary<string, string>? ParseChoices(JObject json) {
            if (json?["choices"] is JObject choicesObj) {
                var choices = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var prop in choicesObj.Properties()) {
                    choices[prop.Name] = (string?)prop.Value ?? "";
                }
                return choices;
            }
            return null;
        }

        public JToken ToJson() {
            var json = new JObject(
                new JProperty("slot", Slot.ToJson()),
                new JProperty("feat", Feat)
            );
            if (Choices != null && Choices.Count > 0) {
                var choicesObj = new JObject();
                foreach (var kvp in Choices) {
                    choicesObj[kvp.Key] = kvp.Value;
                }
                json["choices"] = choicesObj;
            }
            return json;
        }

        public bool Equals(FeatAssignment? other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (!Equals(Slot, other.Slot)) return false;
            if (!string.Equals(Feat, other.Feat, StringComparison.OrdinalIgnoreCase)) return false;

            if (Choices.Count != other.Choices.Count) return false;
            foreach (var kvp in Choices) {
                if (!other.Choices.TryGetValue(kvp.Key, out var otherVal)) return false;
                if (!string.Equals(kvp.Value, otherVal, StringComparison.OrdinalIgnoreCase)) return false;
            }
            return true;
        }

        public override bool Equals(object? obj) {
            return Equals(obj as FeatAssignment);
        }

        public override int GetHashCode() {
            unchecked {
                int hash = (Slot != null ? Slot.GetHashCode() : 0);
                hash = (hash * 397) ^ (Feat != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(Feat) : 0);
                if (Choices != null) {
                    int choicesHash = 0;
                    foreach (var kvp in Choices) {
                        int keyHash = kvp.Key != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(kvp.Key) : 0;
                        int valHash = kvp.Value != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(kvp.Value) : 0;
                        choicesHash ^= (keyHash * 397) ^ valHash;
                    }
                    hash = (hash * 397) ^ choicesHash;
                }
                return hash;
            }
        }

        public static bool operator ==(FeatAssignment? left, FeatAssignment? right) {
            if (ReferenceEquals(left, right)) return true;
            if (ReferenceEquals(left, null)) return false;
            return left.Equals(right);
        }

        public static bool operator !=(FeatAssignment? left, FeatAssignment? right) {
            return !(left == right);
        }

        public override string ToString() {
            if (Choices.Count > 0) {
                var choicesList = string.Join(", ", Choices.Select(kv => $"{kv.Key}={kv.Value}"));
                return $"{Slot} -> {Feat} ({choicesList})";
            }
            return $"{Slot} -> {Feat}";
        }
    }
}
