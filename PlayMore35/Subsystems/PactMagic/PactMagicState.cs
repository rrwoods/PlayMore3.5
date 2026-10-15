using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using PlayMore35.Global;

namespace PlayMore35.Subsystems.PactMagic {
    public class PactMagicState : IJsonable {
        public List<PactSlot> Slots { get; set; } = new List<PactSlot>();

        public JToken ToJson() {
            return new JObject(
                new JProperty("slots", new JArray(Slots.Select(s => new JObject(
                    new JProperty("name", s.VestigeName),
                    new JProperty("state", s.State.ToString())
                ))))
            );
        }

        public static PactMagicState FromJson(JObject json) {
            var state = new PactMagicState();
            if (json?["slots"] is JArray array) {
                foreach (var item in array) {
                    string name = (string)item["name"] ?? "";
                    string stateStr = (string)item["state"];
                    PactState pactState = PactState.GOOD_PACT;
                    if (!string.IsNullOrEmpty(stateStr) && Enum.TryParse(stateStr, true, out PactState parsed)) {
                        pactState = parsed;
                    }
                    state.Slots.Add(new PactSlot(name, pactState));
                }
            }
            return state;
        }
    }
}
