using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Global;

namespace PlayMore35.Subsystems {
    public class SubsystemSlot : IJsonable, IEquatable<SubsystemSlot> {
        public string Element { get; set; } = "";
        public string State { get; set; } = "";
        public string Source { get; set; } = "";
        public ProgressionSlotKey? GrantSlot { get; set; } = null;
        public bool IsFeatBound => GrantSlot != null;
        public bool IsGrantedByEquippedItem => GrantSlot?.Kind == SlotKind.Item;

        public SubsystemSlot() { }

        public SubsystemSlot(string? element, string? state, string? source = null, ProgressionSlotKey? grantSlot = null) {
            Element = element ?? "";
            State = state ?? "";
            Source = source ?? "";
            GrantSlot = grantSlot;
        }

        public SubsystemSlot(JObject? json) {
            if (json == null) return;
            Element = (string?)(json["element"] ?? "") ?? "";
            State = (string?)(json["state"] ?? "") ?? "";
            Source = (string?)(json["source"] ?? "") ?? "";
            if (json["slot"] is JObject slotObj) {
                GrantSlot = new ProgressionSlotKey(slotObj);
            }
        }

        public JToken ToJson() {
            var json = new JObject(
                new JProperty("element", Element),
                new JProperty("state", State),
                new JProperty("source", Source)
            );
            if (GrantSlot != null) {
                json["slot"] = GrantSlot.ToJson();
            }
            return json;
        }

        public bool Equals(SubsystemSlot? other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Element, other.Element, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(State, other.State, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(Source, other.Source, StringComparison.OrdinalIgnoreCase) &&
                   Equals(GrantSlot, other.GrantSlot);
        }

        public override bool Equals(object? obj) {
            return Equals(obj as SubsystemSlot);
        }

        public override int GetHashCode() {
            unchecked {
                int hash = (Element != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(Element) : 0);
                hash = (hash * 397) ^ (State != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(State) : 0);
                hash = (hash * 397) ^ (Source != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(Source) : 0);
                hash = (hash * 397) ^ (GrantSlot != null ? GrantSlot.GetHashCode() : 0);
                return hash;
            }
        }
    }

    public class SubsystemTarget {
        public string? SlotGroup { get; set; }
        public int? SlotIndex { get; set; }
        public string? Element { get; set; }
        public string? Source { get; set; }
        public List<int>? SelectedIndices { get; set; }

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
