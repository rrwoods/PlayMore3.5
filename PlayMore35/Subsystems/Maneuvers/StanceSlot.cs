using System;
using Newtonsoft.Json.Linq;
using PlayMore35.Global;

namespace PlayMore35.Subsystems.Maneuvers {
    public class StanceSlot : IJsonable {
        public string StanceName { get; set; } = "";
        public bool IsActive { get; set; } = false;
        public bool IsFeatBound { get; set; } = false;
        public string FeatSlot { get; set; } = null;

        public StanceSlot(string stanceName = "", bool isActive = false, bool isFeatBound = false, string featSlot = null) {
            StanceName = stanceName ?? "";
            IsActive = isActive;
            IsFeatBound = isFeatBound;
            FeatSlot = featSlot;
        }

        public StanceSlot(JObject json) {
            if (json == null) throw new ArgumentNullException(nameof(json));
            StanceName = (string)(json["name"] ?? "");
            IsActive = (bool)(json["isActive"] ?? false);
            IsFeatBound = (bool)(json["isFeatBound"] ?? false);
            FeatSlot = (string)json["featSlot"];
        }

        public JToken ToJson() {
            var obj = new JObject(
                new JProperty("name", StanceName),
                new JProperty("isActive", IsActive)
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
            ? $"{StanceName} (Active: {IsActive}, Feat-Bound: {FeatSlot})"
            : $"{StanceName} (Active: {IsActive})";
    }
}
