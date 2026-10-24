#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using PlayMore35.Global;

namespace PlayMore35.Subsystems {
    public class SubsystemState : IJsonable {
        public Dictionary<string, List<SubsystemSlot>> Slots { get; set; } =
            new Dictionary<string, List<SubsystemSlot>>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, int> Pools { get; set; } =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public SubsystemState() { }

        public SubsystemState(JObject json) {
            if (json == null) return;

            if (json["slots"] is JObject slotsObj) {
                foreach (var prop in slotsObj.Properties()) {
                    if (prop.Value is JArray slotArray) {
                        var list = new List<SubsystemSlot>();
                        foreach (var item in slotArray) {
                            if (item is JObject slotObj) {
                                list.Add(new SubsystemSlot(slotObj));
                            }
                        }
                        Slots[prop.Name] = list;
                    }
                }
            }

            if (json["pools"] is JObject poolsObj) {
                foreach (var prop in poolsObj.Properties()) {
                    if (prop.Value != null && prop.Value.Type == JTokenType.Integer) {
                        Pools[prop.Name] = (int)prop.Value;
                    }
                }
            }
        }

        public JToken ToJson() {
            var slotsObj = new JObject();
            foreach (var kvp in Slots) {
                slotsObj[kvp.Key] = new JArray(kvp.Value.Select(s => s != null ? s.ToJson() : new SubsystemSlot().ToJson()));
            }

            var poolsObj = new JObject();
            foreach (var kvp in Pools) {
                poolsObj[kvp.Key] = kvp.Value;
            }

            return new JObject(
                new JProperty("slots", slotsObj),
                new JProperty("pools", poolsObj)
            );
        }
    }
}
