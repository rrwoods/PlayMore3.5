using System;
using Newtonsoft.Json.Linq;

namespace PlayMore35.CharacterChoices {
    public class ProgressionSlotKey : IJsonable, IEquatable<ProgressionSlotKey> {
        public SlotKind Kind { get; }
        public string Source { get; }
        public int Level { get; }

        public ProgressionSlotKey(SlotKind kind, string source, int level) {
            Kind = kind;
            Source = source ?? "";
            Level = level;
        }

        public ProgressionSlotKey(JObject json) {
            if (json == null) throw new ArgumentNullException(nameof(json));
            var kindVal = (string?)json["kind"] ?? throw new ArgumentException("Missing 'kind' property", nameof(json));
            if (!Enum.TryParse<SlotKind>(kindVal, true, out var kind)) {
                throw new ArgumentException($"Invalid SlotKind '{kindVal}'", nameof(json));
            }
            Kind = kind;
            Source = (string?)json["source"] ?? "";
            Level = json["level"] != null ? (int)json["level"]! : 0;
        }

        public static ProgressionSlotKey Advancement(int level) =>
            new ProgressionSlotKey(SlotKind.Advancement, "Advancement", level);

        public static ProgressionSlotKey Race(string name, int level = 1) =>
            new ProgressionSlotKey(SlotKind.Race, name, level);

        public static ProgressionSlotKey Class(string name, int classLevel) =>
            new ProgressionSlotKey(SlotKind.Class, name, classLevel);

        public static ProgressionSlotKey Item(string name) =>
            new ProgressionSlotKey(SlotKind.Item, name, -1);

        public JToken ToJson() {
            return new JObject(
                new JProperty("kind", Kind.ToString()),
                new JProperty("source", Source),
                new JProperty("level", Level)
            );
        }

        public bool Equals(ProgressionSlotKey? other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Kind == other.Kind &&
                   string.Equals(Source, other.Source, StringComparison.OrdinalIgnoreCase) &&
                   Level == other.Level;
        }

        public override bool Equals(object? obj) {
            return Equals(obj as ProgressionSlotKey);
        }

        public override int GetHashCode() {
            unchecked {
                int hash = (int)Kind;
                hash = (hash * 397) ^ (Source != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(Source) : 0);
                hash = (hash * 397) ^ Level;
                return hash;
            }
        }

        public static bool operator ==(ProgressionSlotKey? left, ProgressionSlotKey? right) {
            if (ReferenceEquals(left, right)) return true;
            if (ReferenceEquals(left, null)) return false;
            return left.Equals(right);
        }

        public static bool operator !=(ProgressionSlotKey? left, ProgressionSlotKey? right) {
            return !(left == right);
        }

        public override string ToString() {
            return $"{Kind}:{Source}:{Level}";
        }
    }
}
