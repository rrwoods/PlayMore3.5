using System;
using Newtonsoft.Json.Linq;
using PlayMore35.Global;

namespace PlayMore35.Subsystems.Maneuvers {
    public class ManeuverSheetAction : SheetAction {
        public string ManeuverName { get; }
        public string Source { get; }

        public ManeuverSheetAction(string maneuverName, string source, string duration = "1 round") {
            ManeuverName = maneuverName ?? "";
            Source = source ?? "";
            Name = !string.IsNullOrEmpty(Source) ? $"{ManeuverName} ({Source})" : ManeuverName;
            Duration(duration);
        }

        public ManeuverSheetAction(JObject json) : base(json) {
            if (json == null) throw new ArgumentNullException(nameof(json));
            ManeuverName = (string)(json["maneuver_action"] ?? "");
            Source = (string)(json["source"] ?? "");
            if (string.IsNullOrEmpty(Name)) {
                Name = !string.IsNullOrEmpty(Source) ? $"{ManeuverName} ({Source})" : ManeuverName;
            }
        }

        public static SheetAction TryParse(JObject json) {
            if (json == null) return null;
            return json["maneuver_action"] != null
                ? new ManeuverSheetAction(json)
                : null;
        }

        public override TimelineRefreshRequirement Perform(ICharacterContext context) {
            return TimelineRefreshRequirement.RecomputeDerived;
        }

        public override JToken ToJson() {
            var json = (JObject)base.ToJson();
            json["maneuver_action"] = ManeuverName;
            if (!string.IsNullOrEmpty(Source)) {
                json["source"] = Source;
            }
            return json;
        }

        public override bool Matches(SheetAction action) {
            return base.Matches(action) &&
                   action is ManeuverSheetAction m &&
                   string.Equals(m.ManeuverName, ManeuverName, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(m.Source, Source, StringComparison.OrdinalIgnoreCase);
        }
    }
}
