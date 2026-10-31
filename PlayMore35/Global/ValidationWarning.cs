using System;

namespace PlayMore35.Global {
    public enum WarningKind {
        Unmet,
        ItemDependent
    }

    public class ValidationWarning : IEquatable<ValidationWarning> {
        public string Message { get; }
        public WarningKind Kind { get; }

        public ValidationWarning(string message, WarningKind kind = WarningKind.Unmet) {
            Message = message ?? string.Empty;
            Kind = kind;
        }

        public override string ToString() => Message;

        public bool Equals(ValidationWarning? other) {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Kind == other.Kind && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj) => Equals(obj as ValidationWarning);

        public override int GetHashCode() {
            unchecked {
                return ((int)Kind * 397) ^ (Message != null ? StringComparer.Ordinal.GetHashCode(Message) : 0);
            }
        }
    }
}
