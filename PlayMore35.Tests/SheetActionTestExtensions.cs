using System;
using System.Linq;

namespace PlayMore35.Global {
    public static class SheetActionTestExtensions {
        public static bool Matches(this SheetAction action, SheetAction? other) {
            if (action == null && other == null) return true;
            if (action == null || other == null) return false;

            bool baseMatches = action.Name == other.Name &&
                   action.duration == other.duration &&
                   action.flagsToSet.SequenceEqual(other.flagsToSet) &&
                   action.flagsToClear.SequenceEqual(other.flagsToClear) &&
                   action.paramsToAdjust.Count == other.paramsToAdjust.Count && !action.paramsToAdjust.Except(other.paramsToAdjust).Any() &&
                   action.actionsAfter.Count == other.actionsAfter.Count && (action.actionsAfter.Zip(other.actionsAfter, (x, y) => x.Matches(y)).All(x => x));

            if (!baseMatches) return false;

            if (!string.IsNullOrEmpty(action.Subsystem) || !string.IsNullOrEmpty(other.Subsystem)) {
                if (!string.Equals(action.Subsystem, other.Subsystem, StringComparison.OrdinalIgnoreCase)) return false;
                if (!string.Equals(action.Verb, other.Verb, StringComparison.OrdinalIgnoreCase)) return false;
                if (!string.Equals(action.SlotGroup, other.SlotGroup, StringComparison.OrdinalIgnoreCase)) return false;
                if (action.SlotIndex != other.SlotIndex) return false;
                if (!string.Equals(action.Element, other.Element, StringComparison.OrdinalIgnoreCase)) return false;
                if (!string.Equals(action.Source, other.Source, StringComparison.OrdinalIgnoreCase)) return false;
                if (!Equals(action.Step, other.Step)) return false;
            }

            return true;
        }
    }
}
