using System;

namespace PlayMore35.Global {
    public enum WarningKind {
        Unmet,
        ItemDependent
    }

    public enum ValidationCategory {
        General,
        LevelGate,
        Prerequisite,
        Capacity,
        SourceConflict,
        Invalid
    }

    public class ValidationWarning : IEquatable<ValidationWarning> {
        public string Message { get; }
        public WarningKind Kind { get; }
        public ValidationCategory Category { get; }

        public ValidationWarning(string message, WarningKind kind = WarningKind.Unmet, ValidationCategory category = ValidationCategory.General) {
            Message = message ?? string.Empty;
            Kind = kind;
            Category = category;
        }

        public ValidationWarning(string message, ValidationCategory category)
            : this(message, WarningKind.Unmet, category) {
        }

        public override string ToString() => Message;

        public bool Equals(ValidationWarning? other) {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Kind == other.Kind &&
                   Category == other.Category &&
                   string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj) => Equals(obj as ValidationWarning);

        public override int GetHashCode() {
            unchecked {
                int hash = ((int)Kind * 397) ^ (int)Category;
                return (hash * 397) ^ (Message != null ? StringComparer.Ordinal.GetHashCode(Message) : 0);
            }
        }
    }
}
