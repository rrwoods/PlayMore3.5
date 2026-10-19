using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

        public static readonly IReadOnlyDictionary<string, MartialDiscipline> All =
            new ReadOnlyDictionary<string, MartialDiscipline>(
                new List<MartialDiscipline> {
                    new MartialDiscipline("Desert Wind", "Tumble", new [] { "scimitar", "light mace", "light pick", "falchion", "spear" }),
                    new MartialDiscipline("Devoted Spirit", "Intimidate", new [] { "falchion", "greatclub", "longsword", "maul" }),
                    new MartialDiscipline("Diamond Mind", "Concentration", new [] { "rapier", "shortspear", "bastard sword", "trident" }),
                    new MartialDiscipline("Iron Heart", "Balance", new [] { "bastard sword", "dwarven waraxe", "longsword", "two-bladed sword" }),
                    new MartialDiscipline("Setting Sun", "Sense Motive", new [] { "short sword", "quarterstaff", "nunchaku", "unarmed strike" }),
                    new MartialDiscipline("Shadow Hand", "Hide", new [] { "dagger", "short sword", "sai", "siangham", "unarmed strike", "spiked chain" }),
                    new MartialDiscipline("Stone Dragon", "Balance", new [] { "greatsword", "greataxe", "heavy mace", "unarmed strike" }),
                    new MartialDiscipline("Tiger Claw", "Jump", new [] { "kukri", "kama", "claw", "handaxe", "greataxe", "unarmed strike" }),
                    new MartialDiscipline("White Raven", "Diplomacy", new [] { "longsword", "battleaxe", "warhammer", "greatsword", "halberd" })
                }.ToDictionary(d => d.Name, d => d, StringComparer.OrdinalIgnoreCase)
            );

        public static MartialDiscipline Get(string name) {
            if (string.IsNullOrEmpty(name)) return null;
            if (All.TryGetValue(name, out var discipline)) return discipline;
            return All.Values.FirstOrDefault(d => string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
