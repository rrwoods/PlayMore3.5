using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using PlayMore35.Effects;
using PlayMore35.Global;

namespace PlayMore35.Subsystems.Maneuvers {
    public enum ManeuverType {
        Strike,
        Boost,
        Counter,
        Stance,
        Other
    }

    public class Maneuver : IJsonable {
        public string Name { get; set; } = "";
        public string Discipline { get; set; } = "";
        public int Level { get; set; } = 1;
        public ManeuverType Type { get; set; } = ManeuverType.Strike;
        public string ActionType { get; set; } = "";
        public string Prerequisites { get; set; } = "";
        public int PrerequisiteCount { get; set; } = 0;
        public string Description { get; set; } = "";
        public List<Effect> Effects { get; set; } = new List<Effect>();

        public Maneuver() { }

        public Maneuver(JObject json) {
            Name = (string)(json["name"] ?? "");
            Discipline = (string)(json["discipline"] ?? "");
            Level = (int)(json["level"] ?? 1);
            if (json["type"] != null) {
                Type = (ManeuverType)Enum.Parse(typeof(ManeuverType), (string)json["type"], true);
            }
            ActionType = (string)(json["actionType"] ?? "");
            Prerequisites = (string)(json["prerequisites"] ?? "");
            if (json["prerequisiteCount"] != null) {
                PrerequisiteCount = (int)json["prerequisiteCount"];
            }
            Description = (string)(json["description"] ?? "");
            if (json["effects"] is JArray arr) {
                Effects = arr.Select(x => new Effect((JObject)x)).ToList();
            }
        }

        public JToken ToJson() {
            var json = new JObject(
                new JProperty("name", Name),
                new JProperty("discipline", Discipline),
                new JProperty("level", Level),
                new JProperty("type", Type.ToString()),
                new JProperty("actionType", ActionType),
                new JProperty("prerequisites", Prerequisites),
                new JProperty("description", Description),
                new JProperty("effects", new JArray(Effects.Select(x => x.ToJson())))
            );
            if (PrerequisiteCount > 0) {
                json["prerequisiteCount"] = PrerequisiteCount;
            }
            return json;
        }

        public override string ToString() => Name;
    }
}
