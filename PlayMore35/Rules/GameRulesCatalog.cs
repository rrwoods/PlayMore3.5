using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Items;
using PlayMore35.Subsystems;
using static PlayMore35.Effects.Constants;

namespace PlayMore35.Rules {
    public class GameRulesCatalog : IRulesCatalog {
        private readonly string? saveDir;

        public IReadOnlyDictionary<string, Race> Races { get; private set; } = null!;
        public IReadOnlyDictionary<string, CharacterClass> Classes { get; private set; } = null!;
        public IReadOnlyDictionary<string, Feat> Feats { get; private set; } = null!;
        public IReadOnlyDictionary<string, Weapon> Weapons { get; private set; } = null!;
        public IReadOnlyDictionary<string, Armor> Armors { get; private set; } = null!;
        public IReadOnlyDictionary<string, Shield> Shields { get; private set; } = null!;
        public IReadOnlyDictionary<string, WeaponProperty> WeaponProperties { get; private set; } = null!;
        public IReadOnlyDictionary<string, ArmorProperty> ArmorProperties { get; private set; } = null!;
        public IReadOnlyDictionary<string, Item> Items { get; private set; } = null!;
        public IReadOnlyDictionary<string, ExternalEffect> ExternalEffects { get; private set; } = null!;
        public IReadOnlyDictionary<string, SubsystemDefinition> Subsystems { get; private set; } = null!;

        public IReadOnlyDictionary<string, int> CoreFeatSlots { get; private set; } = null!;
        public IReadOnlyList<Statistic> CoreStats { get; private set; } = null!;
        public IReadOnlyList<Effect> CoreEffects { get; private set; } = null!;
        public IReadOnlyDictionary<string, FlagDefinition> CoreFlags { get; private set; } = null!;
        public IReadOnlyDictionary<string, ParameterDefinition> CoreParameters { get; private set; } = null!;
        public JToken CoreRulesJson { get; private set; } = null!;

        private IReadOnlyList<string> weaponNames = null!;
        private IReadOnlyList<string> martialWeaponNames = null!;
        private IReadOnlyList<string> exoticWeaponNames = null!;
        private static readonly IReadOnlyList<string> crossbows = new string[] { "hand crossbow", "light crossbow" };
        private static readonly Lazy<GameRulesCatalog> DefaultCatalog = new Lazy<GameRulesCatalog>(() => new GameRulesCatalog(null, true));

        public GameRulesCatalog(string? customSaveDir = null) {
            saveDir = customSaveDir;

            if (customSaveDir == null) {
                var def = DefaultCatalog.Value;
                CoreRulesJson = def.CoreRulesJson;
                CoreFeatSlots = def.CoreFeatSlots;
                CoreStats = def.CoreStats;
                CoreEffects = def.CoreEffects;
                CoreFlags = def.CoreFlags;
                CoreParameters = def.CoreParameters;
                Races = def.Races;
                Classes = def.Classes;
                Feats = def.Feats;
                Weapons = def.Weapons;
                weaponNames = def.weaponNames;
                martialWeaponNames = def.martialWeaponNames;
                exoticWeaponNames = def.exoticWeaponNames;
                Armors = def.Armors;
                Shields = def.Shields;
                WeaponProperties = def.WeaponProperties;
                ArmorProperties = def.ArmorProperties;
                Items = def.Items;
                ExternalEffects = def.ExternalEffects;
                Subsystems = def.Subsystems;
                return;
            }

            if (!Directory.Exists(saveDir)) {
                Directory.CreateDirectory(saveDir);
            }
            LoadFromResourcesOrSaveDir();
        }

        private GameRulesCatalog(string? customSaveDir, bool forceLoad) {
            saveDir = customSaveDir;
            LoadFromResourcesOrSaveDir();
        }

        private void LoadFromResourcesOrSaveDir() {
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

            // 10. Subsystems
            var subsystemsMap = new Dictionary<string, SubsystemDefinition>(StringComparer.OrdinalIgnoreCase);
            var maneuversToken = GetResource(Properties.Resources.maneuvers, "maneuvers.json");

            if (maneuversToken is JObject maneuversObj) {
                var martialDef = new SubsystemDefinition(maneuversObj);
                subsystemsMap[martialDef.Id] = martialDef;
            }

            var vestigesToken = GetResource(Properties.Resources.vestiges, "vestiges.json");

            if (vestigesToken is JObject vestigesObj) {
                var pactDef = new SubsystemDefinition(vestigesObj);
                subsystemsMap[pactDef.Id] = pactDef;
            }

            Subsystems = new ReadOnlyDictionary<string, SubsystemDefinition>(subsystemsMap);
        }

        private JToken GetResource(byte[] resource, string altFilename) {
            string json;
            if (saveDir != null) {
                string savePath = Path.Combine(saveDir, altFilename);
                if (File.Exists(savePath)) {
                    json = File.ReadAllText(savePath);
                } else {
                    json = Encoding.UTF8.GetString(resource).TrimStart('\uFEFF');
                    File.WriteAllText(savePath, json);
                }
            } else {
                json = Encoding.UTF8.GetString(resource).TrimStart('\uFEFF');
            }
            return JToken.Parse(json);
        }

        private static bool TryFindEntity<T>(IReadOnlyDictionary<string, T> dictionary, string name, Func<T, string> nameSelector, [NotNullWhen(true)] out T? entity) where T : class {
            if (string.IsNullOrEmpty(name)) {
                entity = null;
                return false;
            }
            if (dictionary.TryGetValue(name, out var exact)) {
                entity = exact;
                return true;
            }
            var matched = dictionary.Values.FirstOrDefault(x => string.Equals(nameSelector(x), name, StringComparison.OrdinalIgnoreCase));
            if (matched != null) {
                entity = matched;
                return true;
            }
            entity = null;
            return false;
        }

        public bool TryGetRace(string name, [NotNullWhen(true)] out Race? result) {
            if (string.IsNullOrEmpty(name)) {
                result = Races[""];
                return true;
            }
            return TryFindEntity(Races, name, x => x.Name, out result);
        }

        public Race GetRace(string name) {
            if (TryGetRace(name, out var race)) {
                return race;
            }
            throw new KeyNotFoundException($"Race '{name}' was not found in the rules catalog.");
        }

        public bool TryGetClass(string name, [NotNullWhen(true)] out CharacterClass? result) =>
            TryFindEntity(Classes, name, x => x.Name, out result);

        public CharacterClass GetClass(string name) {
            if (TryGetClass(name, out var cls)) {
                return cls;
            }
            throw new KeyNotFoundException($"Class '{name}' was not found in the rules catalog.");
        }

        public bool TryGetFeat(string name, [NotNullWhen(true)] out Feat? result) =>
            TryFindEntity(Feats, name, x => x.Benefit?.Name ?? "", out result);

        public Feat GetFeat(string name) {
            if (TryGetFeat(name, out var feat)) {
                return feat;
            }
            throw new KeyNotFoundException($"Feat '{name}' was not found in the rules catalog.");
        }

        public Feat? FindFeatByChoiceDescriptor(ChoiceDescriptor descriptor) {
            if (descriptor == null) return null;
            return Feats.Values.FirstOrDefault(f => f.Choices.Any(c => c == descriptor || (c.Id == descriptor.Id && string.Equals(c.Domain, descriptor.Domain, StringComparison.OrdinalIgnoreCase))));
        }

        public bool TryGetWeapon(string name, [NotNullWhen(true)] out Weapon? result) =>
            TryFindEntity(Weapons, name, x => x.Name, out result);

        public Weapon GetWeapon(string name) {
            if (TryGetWeapon(name, out var weapon)) {
                return weapon;
            }
            throw new KeyNotFoundException($"Weapon '{name}' was not found in the rules catalog.");
        }

        public bool TryGetArmor(string name, [NotNullWhen(true)] out Armor? result) =>
            TryFindEntity(Armors, name, x => x.Name, out result);

        public Armor GetArmor(string name) {
            if (TryGetArmor(name, out var armor)) {
                return armor;
            }
            throw new KeyNotFoundException($"Armor '{name}' was not found in the rules catalog.");
        }

        public bool TryGetShield(string name, [NotNullWhen(true)] out Shield? result) =>
            TryFindEntity(Shields, name, x => x.Name, out result);

        public Shield GetShield(string name) {
            if (TryGetShield(name, out var shield)) {
                return shield;
            }
            throw new KeyNotFoundException($"Shield '{name}' was not found in the rules catalog.");
        }

        public bool TryGetWeaponProperty(string name, [NotNullWhen(true)] out WeaponProperty? result) =>
            TryFindEntity(WeaponProperties, name, x => x.Name, out result);

        public WeaponProperty GetWeaponProperty(string name) {
            if (TryGetWeaponProperty(name, out var prop)) {
                return prop;
            }
            throw new KeyNotFoundException($"WeaponProperty '{name}' was not found in the rules catalog.");
        }

        public bool TryGetArmorProperty(string name, [NotNullWhen(true)] out ArmorProperty? result) =>
            TryFindEntity(ArmorProperties, name, x => x.Name, out result);

        public ArmorProperty GetArmorProperty(string name) {
            if (TryGetArmorProperty(name, out var prop)) {
                return prop;
            }
            throw new KeyNotFoundException($"ArmorProperty '{name}' was not found in the rules catalog.");
        }

        public bool TryGetItem(string name, [NotNullWhen(true)] out Item? result) =>
            TryFindEntity(Items, name, x => x.ItemEffect?.Name ?? "", out result);

        public Item GetItem(string name) {
            if (TryGetItem(name, out var item)) {
                return item;
            }
            throw new KeyNotFoundException($"Item '{name}' was not found in the rules catalog.");
        }

        public bool TryGetExternalEffect(string name, [NotNullWhen(true)] out ExternalEffect? result) =>
            TryFindEntity(ExternalEffects, name, x => x.Effect?.Name ?? "", out result);

        public ExternalEffect GetExternalEffect(string name) {
            if (TryGetExternalEffect(name, out var ext)) {
                return ext;
            }
            throw new KeyNotFoundException($"ExternalEffect '{name}' was not found in the rules catalog.");
        }

        public bool TryGetSubsystem(string name, [NotNullWhen(true)] out SubsystemDefinition? result) {
            if (!string.IsNullOrEmpty(name) && Subsystems.TryGetValue(name, out var s)) {
                result = s;
                return true;
            }
            result = null;
            return false;
        }

        public SubsystemDefinition GetSubsystem(string name) {
            if (TryGetSubsystem(name, out var s)) {
                return s;
            }
            throw new KeyNotFoundException($"Subsystem '{name}' was not found in the rules catalog.");
        }

        public string ResolveTemplate(string template, IReadOnlyDictionary<string, object> choices) =>
            ResolveTemplateInternal(template, choices);

        internal static string ResolveTemplateInternal(string template, IReadOnlyDictionary<string, object> choices) {
            if (string.IsNullOrEmpty(template)) return "";
            if (!template.Contains("${")) return template;

            object GetChoice(string key) {
                if (choices == null) return null;
                if (choices.TryGetValue(key, out var val)) return val;
                var kvp = choices.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
                if (kvp.Value != null) return kvp.Value;
                var prefixedKvp = choices.FirstOrDefault(x => x.Key.EndsWith(":" + key, StringComparison.OrdinalIgnoreCase));
                if (prefixedKvp.Value != null) return prefixedKvp.Value;
                return null;
            }

            return System.Text.RegularExpressions.Regex.Replace(template, @"\$\{([^}]+)\}", match => {
                string path = match.Groups[1].Value.Trim();
                if (string.IsNullOrEmpty(path)) return "";

                string[] parts = path.Split('.');
                object current = GetChoice(parts[0]);
                if (current == null) return "";

                for (int i = 1; i < parts.Length; i++) {
                    if (current is IPropertyProvider provider) {
                        if (provider.Properties != null && provider.Properties.TryGetValue(parts[i], out var nextVal)) {
                            current = nextVal;
                        } else {
                            return "";
                        }
                    } else {
                        return "";
                    }
                }

                return current?.ToString() ?? "";
            });
        }

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
