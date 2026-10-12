using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Items;
using static PlayMore35.Effects.Constants;

namespace PlayMore35.Rules {
    public class GameRulesCatalog : IRulesCatalog {
        private readonly string saveDir;

        public IReadOnlyDictionary<string, Race> Races { get; }
        public IReadOnlyDictionary<string, CharacterClass> Classes { get; }
        public IReadOnlyDictionary<string, Feat> Feats { get; }
        public IReadOnlyDictionary<string, Weapon> Weapons { get; }
        public IReadOnlyDictionary<string, Armor> Armors { get; }
        public IReadOnlyDictionary<string, Shield> Shields { get; }
        public IReadOnlyDictionary<string, WeaponProperty> WeaponProperties { get; }
        public IReadOnlyDictionary<string, ArmorProperty> ArmorProperties { get; }
        public IReadOnlyDictionary<string, Item> Items { get; }
        public IReadOnlyDictionary<string, ExternalEffect> ExternalEffects { get; }

        public IReadOnlyDictionary<string, int> CoreFeatSlots { get; }
        public IReadOnlyList<Statistic> CoreStats { get; }
        public IReadOnlyList<Effect> CoreEffects { get; }
        public IReadOnlyDictionary<string, FlagDefinition> CoreFlags { get; }
        public IReadOnlyDictionary<string, ParameterDefinition> CoreParameters { get; }
        public JToken CoreRulesJson { get; }

        private readonly IReadOnlyList<string> weaponNames;
        private readonly IReadOnlyList<string> martialWeaponNames;
        private readonly IReadOnlyList<string> exoticWeaponNames;
        private static readonly IReadOnlyList<string> crossbows = new string[] { "hand crossbow", "light crossbow" };

        public GameRulesCatalog(string customSaveDir = null) {
            saveDir = customSaveDir;

            if (saveDir != null && !Directory.Exists(saveDir)) {
                Directory.CreateDirectory(saveDir);
            }

            // 1. Core Rules
            var rulesJson = GetResource(Properties.Resources.rules, "rules.json");
            CoreRulesJson = rulesJson;

            var featSlots = new Dictionary<string, int>();
            foreach (JProperty featSlot in rulesJson["feats"]) {
                featSlots[featSlot.Name] = (int)featSlot.Value;
            }
            CoreFeatSlots = new ReadOnlyDictionary<string, int>(featSlots);

            CoreStats = rulesJson["stats"].Select(x => new Statistic((JObject)x)).ToList().AsReadOnly();
            CoreEffects = rulesJson["effects"].Select(x => new Effect((JObject)x)).ToList().AsReadOnly();

            var flags = rulesJson["flags"].Select(x => new FlagDefinition((JObject)x)).ToDictionary(x => x.Name);
            CoreFlags = new ReadOnlyDictionary<string, FlagDefinition>(flags);

            var parameters = rulesJson["parameters"].Select(x => new ParameterDefinition((JObject)x)).ToDictionary(x => x.Name);
            CoreParameters = new ReadOnlyDictionary<string, ParameterDefinition>(parameters);

            // 2. Races
            var races = GetResource(Properties.Resources.races, "races.json")
                .Select(x => new Race((JObject)x))
                .ToDictionary(x => x.Name);
            races[""] = new Race();
            Races = new ReadOnlyDictionary<string, Race>(races);

            // 3. Classes
            var classes = GetResource(Properties.Resources.classes, "classes.json")
                .Select(x => new CharacterClass((JObject)x))
                .ToDictionary(x => x.Name);
            Classes = new ReadOnlyDictionary<string, CharacterClass>(classes);

            // 4. Feats
            var feats = GetResource(Properties.Resources.feats, "feats.json")
                .Select(x => new Feat((JObject)x))
                .ToDictionary(x => x.Benefit.Name);
            Feats = new ReadOnlyDictionary<string, Feat>(feats);

            // 5. Weapons
            var weapons = GetResource(Properties.Resources.weapons, "weapons.json")
                .Select(x => new Weapon((JObject)x))
                .ToDictionary(x => x.Name);
            Weapons = new ReadOnlyDictionary<string, Weapon>(weapons);

            weaponNames = Weapons.Keys.OrderBy(x => x).ToList().AsReadOnly();
            martialWeaponNames = Weapons
                .Where(x => x.Value.GetProficiency() == Weapon.Proficiency.MARTIAL)
                .Select(x => x.Key)
                .OrderBy(x => x)
                .ToList()
                .AsReadOnly();
            exoticWeaponNames = Weapons
                .Where(x => x.Value.GetProficiency() == Weapon.Proficiency.EXOTIC)
                .Select(x => x.Key)
                .OrderBy(x => x)
                .ToList()
                .AsReadOnly();

            // 6. Armor & Shields
            var armor = GetResource(Properties.Resources.armor, "armor.json")
                .Select(x => new Armor((JObject)x))
                .ToDictionary(x => x.Name);
            Armors = new ReadOnlyDictionary<string, Armor>(armor);

            var shields = GetResource(Properties.Resources.shields, "shields.json")
                .Select(x => new Shield((JObject)x))
                .ToDictionary(x => x.Name);
            Shields = new ReadOnlyDictionary<string, Shield>(shields);

            // 7. Weapon & Armor Properties
            var weaponProps = GetResource(Properties.Resources.weaponProperties, "weaponProperties.json")
                .Select(x => new WeaponProperty((JObject)x))
                .ToDictionary(x => x.Name);
            WeaponProperties = new ReadOnlyDictionary<string, WeaponProperty>(weaponProps);

            var armorProps = GetResource(Properties.Resources.armorProperties, "armorProperties.json")
                .Select(x => new ArmorProperty((JObject)x))
                .ToDictionary(x => x.Name);
            ArmorProperties = new ReadOnlyDictionary<string, ArmorProperty>(armorProps);

            // 8. Items
            var items = GetResource(Properties.Resources.items, "items.json")
                .Select(x => new Item((JObject)x))
                .ToDictionary(x => x.ItemEffect.Name);
            Items = new ReadOnlyDictionary<string, Item>(items);

            // 9. External Effects
            var external = GetResource(Properties.Resources.external, "external.json")
                .Select(x => new ExternalEffect((JObject)x))
                .ToDictionary(x => x.Effect.Name);
            ExternalEffects = new ReadOnlyDictionary<string, ExternalEffect>(external);
        }

        private JToken GetResource(byte[] resource, string altFilename) {
            string json;
            if (saveDir != null) {
                string savePath = Path.Combine(saveDir, altFilename);
                if (File.Exists(savePath)) {
                    json = File.ReadAllText(savePath);
                } else {
                    json = Encoding.UTF8.GetString(resource);
                    File.WriteAllText(savePath, json);
                }
            } else {
                json = Encoding.UTF8.GetString(resource);
            }
            return JToken.Parse(json);
        }

        private static T FindEntity<T>(IReadOnlyDictionary<string, T> dictionary, string name, Func<T, string> nameSelector) where T : class {
            if (string.IsNullOrEmpty(name)) return null;
            if (dictionary.TryGetValue(name, out var entity)) return entity;
            return dictionary.Values.FirstOrDefault(x => string.Equals(nameSelector(x), name, StringComparison.OrdinalIgnoreCase));
        }

        public Race GetRace(string name) {
            if (string.IsNullOrEmpty(name)) {
                return Races[""];
            }
            return FindEntity(Races, name, x => x.Name);
        }

        public CharacterClass GetClass(string name) => FindEntity(Classes, name, x => x.Name);

        public Feat GetFeat(string name) => FindEntity(Feats, name, x => x.Benefit?.Name);

        public Weapon GetWeapon(string name) => FindEntity(Weapons, name, x => x.Name);

        public Armor GetArmor(string name) => FindEntity(Armors, name, x => x.Name);

        public Shield GetShield(string name) => FindEntity(Shields, name, x => x.Name);

        public WeaponProperty GetWeaponProperty(string name) => FindEntity(WeaponProperties, name, x => x.Name);

        public ArmorProperty GetArmorProperty(string name) => FindEntity(ArmorProperties, name, x => x.Name);

        public Item GetItem(string name) => FindEntity(Items, name, x => x.ItemEffect?.Name);

        public ExternalEffect GetExternalEffect(string name) => FindEntity(ExternalEffects, name, x => x.Effect?.Name);

        public IEnumerable<string> ChoicesForKey(string choiceDescription) {
            switch (choiceDescription) {
                case "hand or light crossbow":
                    return crossbows;
                case "school of magic":
                    return SCHOOLS_OF_MAGIC;
                case "skill":
                    return SKILLS_LIST;
                case "weapon":
                    return weaponNames;
                case "martial weapon":
                    return martialWeaponNames;
                case "exotic weapon":
                    return exoticWeaponNames;
                default:
                    return Enumerable.Empty<string>();
            }
        }
    }
}
