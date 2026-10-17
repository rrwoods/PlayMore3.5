using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PlayMore35.Global;

namespace PlayMore35.Subsystems.Maneuvers {
    public class MartialManeuverState : IJsonable {
        public Dictionary<string, List<KnownManeuverSlot>> Known { get; } =
            new Dictionary<string, List<KnownManeuverSlot>>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, List<StanceSlot>> Stances { get; } =
            new Dictionary<string, List<StanceSlot>>(StringComparer.OrdinalIgnoreCase);

        public MartialManeuverState() { }

        public MartialManeuverState(JObject json) {
            if (json == null) return;
            if (json["sources"] is JObject sourcesObj) {
                foreach (var prop in sourcesObj.Properties()) {
                    string sourceName = prop.Name;
                    if (prop.Value is JObject sourceJson) {
                        if (sourceJson["known"] is JArray knownArr) {
                            var knownList = new List<KnownManeuverSlot>();
                            foreach (var token in knownArr) {
                                if (token is JObject obj) {
                                    knownList.Add(new KnownManeuverSlot(obj));
                                } else {
                                    knownList.Add(null);
                                }
                            }
                            Known[sourceName] = knownList;
                        }
                        if (sourceJson["stances"] is JArray stanceArr) {
                            var stanceList = new List<StanceSlot>();
                            foreach (var token in stanceArr) {
                                if (token is JObject obj) {
                                    stanceList.Add(new StanceSlot(obj));
                                } else {
                                    stanceList.Add(null);
                                }
                            }
                            Stances[sourceName] = stanceList;
                        }
                    }
                }
            }
        }

        public static MartialManeuverState FromJson(JObject json) => new MartialManeuverState(json);

        public List<KnownManeuverSlot> GetOrCreateKnown(string source) {
            if (string.IsNullOrWhiteSpace(source)) {
                throw new ArgumentException("Source name cannot be null or empty.", nameof(source));
            }
            if (!Known.TryGetValue(source, out var list)) {
                list = new List<KnownManeuverSlot>();
                Known[source] = list;
            }
            return list;
        }

        public List<StanceSlot> GetOrCreateStances(string source) {
            if (string.IsNullOrWhiteSpace(source)) {
                throw new ArgumentException("Source name cannot be null or empty.", nameof(source));
            }
            if (!Stances.TryGetValue(source, out var list)) {
                list = new List<StanceSlot>();
                Stances[source] = list;
            }
            return list;
        }

        public IEnumerable<string> Sources {
            get {
                var set = new HashSet<string>(Known.Keys, StringComparer.OrdinalIgnoreCase);
                set.UnionWith(Stances.Keys);
                return set;
            }
        }

        public JToken ToJson() {
            var sourcesObj = new JObject();
            var allSources = Sources;
            foreach (var source in allSources) {
                var sourceObj = new JObject();

                var knownArr = new JArray();
                if (Known.TryGetValue(source, out var knownSlots) && knownSlots != null) {
                    foreach (var slot in knownSlots) {
                        if (slot != null) {
                            knownArr.Add(slot.ToJson());
                        } else {
                            knownArr.Add(new KnownManeuverSlot().ToJson());
                        }
                    }
                }
                sourceObj["known"] = knownArr;

                var stanceArr = new JArray();
                if (Stances.TryGetValue(source, out var stanceSlots) && stanceSlots != null) {
                    foreach (var slot in stanceSlots) {
                        if (slot != null) {
                            stanceArr.Add(slot.ToJson());
                        } else {
                            stanceArr.Add(new StanceSlot().ToJson());
                        }
                    }
                }
                sourceObj["stances"] = stanceArr;

                sourcesObj[source] = sourceObj;
            }

            return new JObject(
                new JProperty("sources", sourcesObj)
            );
        }
    }
}
