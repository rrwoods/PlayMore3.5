using System;
using Newtonsoft.Json.Linq;
using PlayMore35.Global;

namespace PlayMore35.Subsystems.Maneuvers {
    public class KnownManeuverSlot : IJsonable {
        public string ManeuverName { get; set; } = "";
        public ManeuverState State { get; set; } = ManeuverState.Unreadied;

        public bool IsReadied => State != ManeuverState.Unreadied;

        public KnownManeuverSlot(string maneuverName = "", ManeuverState state = ManeuverState.Unreadied) {
            ManeuverName = maneuverName ?? "";
            State = state;
        }

        public KnownManeuverSlot(JObject json) {
            if (json == null) throw new ArgumentNullException(nameof(json));
            ManeuverName = (string)(json["name"] ?? json["maneuverName"] ?? "");
            string stateStr = (string)json["state"];
            if (!string.IsNullOrEmpty(stateStr) && Enum.TryParse<ManeuverState>(stateStr, true, out var parsedState)) {
                State = parsedState;
            } else {
                State = ManeuverState.Unreadied;
            }
        }

        public JToken ToJson() {
            return new JObject(
                new JProperty("name", ManeuverName),
                new JProperty("state", State.ToString())
            );
        }

        public override string ToString() => $"{ManeuverName} ({State})";
    }
}
