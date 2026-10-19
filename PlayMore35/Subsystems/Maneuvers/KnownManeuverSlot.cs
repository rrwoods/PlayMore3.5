using System;
using Newtonsoft.Json.Linq;
using PlayMore35.Global;

namespace PlayMore35.Subsystems.Maneuvers {
    public class KnownManeuverSlot : IJsonable {
        public string ManeuverName { get; set; } = "";
        public ManeuverState State { get; set; } = ManeuverState.Unreadied;
        public bool IsFeatBound { get; set; } = false;
        public string FeatSlot { get; set; } = null;

        public bool IsReadied => State != ManeuverState.Unreadied;

        public KnownManeuverSlot(string maneuverName = "", ManeuverState state = ManeuverState.Unreadied, bool isFeatBound = false, string featSlot = null) {
            ManeuverName = maneuverName ?? "";
            State = state;
            IsFeatBound = isFeatBound;
            FeatSlot = featSlot;
        }

        public KnownManeuverSlot(JObject json) {
            if (json == null) throw new ArgumentNullException(nameof(json));
            ManeuverName = (string)(json["name"] ?? "");
            string stateStr = (string)json["state"];
            if (!string.IsNullOrEmpty(stateStr) && Enum.TryParse<ManeuverState>(stateStr, true, out var parsedState)) {
                State = parsedState;
            } else {
                State = ManeuverState.Unreadied;
            }
            IsFeatBound = (bool)(json["isFeatBound"] ?? false);
            FeatSlot = (string)json["featSlot"];
        }

        public JToken ToJson() {
            var obj = new JObject(
                new JProperty("name", ManeuverName),
                new JProperty("state", State.ToString())
            );
            if (IsFeatBound) {
                obj["isFeatBound"] = true;
            }
            if (!string.IsNullOrEmpty(FeatSlot)) {
                obj["featSlot"] = FeatSlot;
            }
            return obj;
        }

        public override string ToString() => IsFeatBound
            ? $"{ManeuverName} ({State}, Feat-Bound: {FeatSlot})"
            : $"{ManeuverName} ({State})";
    }
}
