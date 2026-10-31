using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using PlayMore35.Global;

namespace PlayMore35.Subsystems {
    public class SubsystemSlot : IJsonable, IEquatable<SubsystemSlot> {
        public string Element { get; set; } = "";
        public string State { get; set; } = "";
        public string Source { get; set; } = "";
        public string? FeatSlot { get; set; } = null;
        public bool IsFeatBound => !string.IsNullOrEmpty(FeatSlot);

        public SubsystemSlot() { }

        public SubsystemSlot(string? element, string? state, string? source = null, string? featSlot = null) {
            Element = element ?? "";
            State = state ?? "";
            Source = source ?? "";
            FeatSlot = featSlot;
        }

        public SubsystemSlot(JObject? json) {
            if (json == null) return;
            Element = (string?)(json["element"] ?? "") ?? "";
            State = (string?)(json["state"] ?? "") ?? "";
            Source = (string?)(json["source"] ?? "") ?? "";
            FeatSlot = (string?)json["feat_slot"];
        }

        public JToken ToJson() {
            var json = new JObject(
                new JProperty("element", Element),
                new JProperty("state", State),
                new JProperty("source", Source)
            );
            if (!string.IsNullOrEmpty(FeatSlot)) {
                json["feat_slot"] = FeatSlot;
            }
            return json;
        }

        public bool Equals(SubsystemSlot? other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Element, other.Element, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(State, other.State, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(Source, other.Source, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(FeatSlot, other.FeatSlot, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj) {
            return Equals(obj as SubsystemSlot);
        }

        public override int GetHashCode() {
            unchecked {
                int hash = (Element != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(Element) : 0);
                hash = (hash * 397) ^ (State != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(State) : 0);
                hash = (hash * 397) ^ (Source != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(Source) : 0);
                hash = (hash * 397) ^ (FeatSlot != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(FeatSlot) : 0);
                return hash;
            }
        }
    }

    public class SubsystemTransition : IJsonable {
        public string SlotGroup { get; set; } = "";
        public string Selector { get; set; } = "invoked";
        public string? FromState { get; set; } = null;
        public string ToState { get; set; } = "";
        public string CountExpression { get; set; } = "1";
        public string OnEmptyVerb { get; set; } = "";
        public int? SlotIndex { get; set; } = null;
        public List<int>? SelectedIndices { get; set; } = null;

        public SubsystemTransition() { }

        public SubsystemTransition(JObject? json) {
            if (json == null) return;
            SlotGroup = (string?)(json["slot_group"] ?? "") ?? "";
            Selector = (string?)(json["selector"] ?? "invoked") ?? "invoked";
            FromState = (string?)json["from"];
            ToState = (string?)(json["to"] ?? "") ?? "";
            if (json["count"] != null) {
                CountExpression = (string?)json["count"] ?? "1";
            }
            OnEmptyVerb = (string?)(json["on_empty_verb"] ?? "") ?? "";
            if (json["slot_index"] != null) {
                SlotIndex = (int)json["slot_index"]!;
            }
            if (json["selected_indices"] is JArray arr) {
                SelectedIndices = arr.Select(x => (int)x).ToList();
            }
        }

        public JToken ToJson() {
            var json = new JObject(
                new JProperty("slot_group", SlotGroup),
                new JProperty("selector", Selector),
                new JProperty("to", ToState)
            );
            if (FromState != null) {
                json["from"] = FromState;
            }
            if (!string.IsNullOrEmpty(CountExpression)) {
                json["count"] = CountExpression;
            }
            if (!string.IsNullOrEmpty(OnEmptyVerb)) {
                json["on_empty_verb"] = OnEmptyVerb;
            }
            if (SlotIndex.HasValue) {
                json["slot_index"] = SlotIndex.Value;
            }
            if (SelectedIndices != null && SelectedIndices.Count > 0) {
                json["selected_indices"] = new JArray(SelectedIndices);
            }
            return json;
        }
    }

    public class SubsystemPoolOperation : IJsonable {
        public string Pool { get; set; } = "";
        public string Op { get; set; } = "consume";
        public int Amount { get; set; } = 0;

        public SubsystemPoolOperation() { }

        public SubsystemPoolOperation(string? pool, string? op, int amount) {
            Pool = pool ?? "";
            Op = op ?? "consume";
            Amount = amount;
        }

        public SubsystemPoolOperation(JObject? json) {
            if (json == null) return;
            Pool = (string?)(json["pool"] ?? "") ?? "";
            Op = (string?)(json["op"] ?? "consume") ?? "consume";
            Amount = (int)(json["amount"] ?? 0);
        }

        public JToken ToJson() {
            return new JObject(
                new JProperty("pool", Pool),
                new JProperty("op", Op),
                new JProperty("amount", Amount)
            );
        }
    }

    public class SubsystemTarget {
        public string? SlotGroup { get; set; }
        public int? SlotIndex { get; set; }
        public string? Element { get; set; }
        public string? Source { get; set; }
        public List<int>? SelectedIndices { get; set; }

        public SubsystemTarget() { }

        public SubsystemTarget(string? slotGroup = null, int? slotIndex = null, string? element = null, string? source = null, List<int>? selectedIndices = null) {
            SlotGroup = slotGroup;
            SlotIndex = slotIndex;
            Element = element;
            Source = source;
            SelectedIndices = selectedIndices;
        }
    }

    public class SubsystemSlotGrant : IJsonable, IEquatable<SubsystemSlotGrant> {
        public string Subsystem { get; set; } = "";
        public string SlotGroup { get; set; } = "";
        public string Element { get; set; } = "";
        public string Source { get; set; } = "";

        public SubsystemSlotGrant() { }

        public SubsystemSlotGrant(string? subsystem, string? slotGroup, string? element, string? source) {
            Subsystem = subsystem ?? "";
            SlotGroup = slotGroup ?? "";
            Element = element ?? "";
            Source = source ?? "";
        }

        public SubsystemSlotGrant(JObject? json) {
            if (json == null) return;
            Subsystem = (string?)(json["subsystem"] ?? "") ?? "";
            SlotGroup = (string?)(json["slot_group"] ?? "") ?? "";
            Element = (string?)(json["element"] ?? "") ?? "";
            Source = (string?)(json["source"] ?? "") ?? "";
        }

        public JToken ToJson() {
            return new JObject(
                new JProperty("subsystem", Subsystem),
                new JProperty("slot_group", SlotGroup),
                new JProperty("element", Element),
                new JProperty("source", Source)
            );
        }

        public SubsystemSlotGrant ApplyChoice(string choice) {
            return new SubsystemSlotGrant(
                ReplaceChoiceToken(Subsystem, choice),
                ReplaceChoiceToken(SlotGroup, choice),
                ReplaceChoiceToken(Element, choice),
                ReplaceChoiceToken(Source, choice)
            );
        }

        private static string ReplaceChoiceToken(string text, string choice) {
            if (string.IsNullOrEmpty(text)) return "";
            if (text.Contains("${")) {
                return text.Replace("${class}", choice);
            }
            return text.Replace("$", choice);
        }

        public bool Equals(SubsystemSlotGrant? other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Subsystem, other.Subsystem, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(SlotGroup, other.SlotGroup, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(Element, other.Element, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(Source, other.Source, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj) => Equals(obj as SubsystemSlotGrant);

        public override int GetHashCode() {
            unchecked {
                int hash = Subsystem != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(Subsystem) : 0;
                hash = (hash * 397) ^ (SlotGroup != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(SlotGroup) : 0);
                hash = (hash * 397) ^ (Element != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(Element) : 0);
                hash = (hash * 397) ^ (Source != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(Source) : 0);
                return hash;
            }
        }
    }
}
