using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PlayMore35.Global;

namespace PlayMore35.Subsystems.Maneuvers {
    public class MartialManeuverState : IJsonable {
        public Dictionary<string, ManeuverSourceState> Sources { get; } =
            new Dictionary<string, ManeuverSourceState>(StringComparer.OrdinalIgnoreCase);

        public MartialManeuverState() { }

        public MartialManeuverState(JObject json) {
            if (json == null) return;
            if (json["sources"] is JObject sourcesObj) {
                foreach (var prop in sourcesObj.Properties()) {
                    if (prop.Value is JObject sourceJson) {
                        Sources[prop.Name] = new ManeuverSourceState(sourceJson);
                    }
                }
            }
        }

        public static MartialManeuverState FromJson(JObject json) => new MartialManeuverState(json);

        public ManeuverSourceState GetOrCreateSource(string source) {
            if (string.IsNullOrWhiteSpace(source)) {
                throw new ArgumentException("Source name cannot be null or empty.", nameof(source));
            }
            if (!Sources.TryGetValue(source, out var state)) {
                state = new ManeuverSourceState();
                Sources[source] = state;
            }
            return state;
        }

        public JToken ToJson() {
            var sourcesObj = new JObject();
            foreach (var kvp in Sources) {
                if (kvp.Value != null) {
                    sourcesObj[kvp.Key] = kvp.Value.ToJson();
                }
            }
            return new JObject(
                new JProperty("sources", sourcesObj)
            );
        }
    }
}
