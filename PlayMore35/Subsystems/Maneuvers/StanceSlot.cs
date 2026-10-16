using System;
using Newtonsoft.Json.Linq;
using PlayMore35.Global;

namespace PlayMore35.Subsystems.Maneuvers {
    public class StanceSlot : IJsonable {
        public string StanceName { get; set; } = "";
        public bool IsActive { get; set; } = false;

        public StanceSlot(string stanceName = "", bool isActive = false) {
            StanceName = stanceName ?? "";
            IsActive = isActive;
        }

        public StanceSlot(JObject json) {
            if (json == null) throw new ArgumentNullException(nameof(json));
            StanceName = (string)(json["name"] ?? "");
            IsActive = (bool)(json["isActive"] ?? false);
        }

        public JToken ToJson() {
            return new JObject(
                new JProperty("name", StanceName),
                new JProperty("isActive", IsActive)
            );
        }

        public override string ToString() => $"{StanceName} (Active: {IsActive})";
    }
}
