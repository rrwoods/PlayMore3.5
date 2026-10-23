using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json.Linq;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Rules;

namespace PlayMore35.Subsystems.Maneuvers {
    public enum ManeuverType {
        Strike,
        Boost,
        Counter,
        Stance,
        Other
    }

    public class Maneuver : IJsonable, IPropertyProvider {
        public string Name { get; set; } = "";
        public string Discipline { get; set; } = "";
        public int Level { get; set; } = 1;
        public ManeuverType Type { get; set; } = ManeuverType.Strike;
        public string ActionType { get; set; } = "";
        public string Prerequisites { get; set; } = "";
        public int PrerequisiteCount { get; set; } = 0;
        public string Description { get; set; } = "";
        public List<Effect> Effects { get; set; } = new List<Effect>();
        public MartialDiscipline DisciplineDefinition { get; set; }

        public IReadOnlyDictionary<string, object> Properties {
            get {
                var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) {
                    ["name"] = Name,
                    ["level"] = Level,
                    ["discipline"] = DisciplineDefinition ?? (object)Discipline
                };
                return new ReadOnlyDictionary<string, object>(dict);
            }
        }

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
            if (json["effects"] is JObject effectsObj && effectsObj["benefits"] is JArray arr) {
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
                new JProperty("prerequisiteCount", PrerequisiteCount),
                new JProperty("description", Description)
            );
            if (Effects.Count > 0) {
                json["effects"] = new JObject(
                    new JProperty("benefits", new JArray(Effects.Select(x => x.ToJson())))
                );
            }
            return json;
        }

        public override string ToString() => Name;
    }
}
