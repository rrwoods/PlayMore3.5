using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json.Linq;
using PlayMore35.Rules;

namespace PlayMore35.Subsystems.Maneuvers {
    public class MartialDiscipline : IPropertyProvider {
        public string Name { get; }
        public string KeySkill { get; }
        public IReadOnlyList<string> Weapons { get; }

        public IReadOnlyDictionary<string, object> Properties {
            get {
                var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) {
                    ["name"] = Name,
                    ["keySkill"] = KeySkill,
                    ["weapons"] = Weapons
                };
                return new ReadOnlyDictionary<string, object>(dict);
            }
        }

        public MartialDiscipline(string name, string keySkill, IEnumerable<string> weapons) {
            Name = name ?? "";
            KeySkill = keySkill ?? "";
            Weapons = (weapons?.ToList() ?? new List<string>()).AsReadOnly();
        }

        public MartialDiscipline(string name, JObject json) {
            Name = name ?? "";
            KeySkill = (string)(json?["keySkill"] ?? "");
            var weapons = json?["weapons"] is JArray arr
                ? arr.Select(x => (string)x).ToList()
                : new List<string>();
            Weapons = weapons.AsReadOnly();
        }

        public JObject ToJson() {
            return new JObject(
                new JProperty("keySkill", KeySkill),
                new JProperty("weapons", new JArray(Weapons))
            );
        }

        public override string ToString() => Name;
    }
}
