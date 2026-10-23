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
using PlayMore35.Subsystems;
using static PlayMore35.Effects.Constants;

namespace PlayMore35.Rules {
    public class GameRulesCatalog : IRulesCatalog {
        private readonly string saveDir;

        public IReadOnlyDictionary<string, Race> Races { get; private set; }
        public IReadOnlyDictionary<string, CharacterClass> Classes { get; private set; }
        public IReadOnlyDictionary<string, Feat> Feats { get; private set; }
        public IReadOnlyDictionary<string, Weapon> Weapons { get; private set; }
        public IReadOnlyDictionary<string, Armor> Armors { get; private set; }
        public IReadOnlyDictionary<string, Shield> Shields { get; private set; }
        public IReadOnlyDictionary<string, WeaponProperty> WeaponProperties { get; private set; }
        public IReadOnlyDictionary<string, ArmorProperty> ArmorProperties { get; private set; }
        public IReadOnlyDictionary<string, Item> Items { get; private set; }
        public IReadOnlyDictionary<string, ExternalEffect> ExternalEffects { get; private set; }
        public IReadOnlyDictionary<string, Vestige> Vestiges { get; private set; }
        public IReadOnlyDictionary<string, SubsystemDefinition> Subsystems { get; private set; }

        public IReadOnlyDictionary<string, int> CoreFeatSlots { get; private set; }
        public IReadOnlyList<Statistic> CoreStats { get; private set; }
        public IReadOnlyList<Effect> CoreEffects { get; private set; }
        public IReadOnlyDictionary<string, FlagDefinition> CoreFlags { get; private set; }
        public IReadOnlyDictionary<string, ParameterDefinition> CoreParameters { get; private set; }
        public JToken CoreRulesJson { get; private set; }

        private IReadOnlyList<string> weaponNames;
        private IReadOnlyList<string> martialWeaponNames;
        private IReadOnlyList<string> exoticWeaponNames;
        private static readonly IReadOnlyList<string> crossbows = new string[] { "hand crossbow", "light crossbow" };
        private static readonly Lazy<GameRulesCatalog> DefaultCatalog = new Lazy<GameRulesCatalog>(() => new GameRulesCatalog(null, true));

        public GameRulesCatalog(string customSaveDir = null) {
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
                Vestiges = def.Vestiges;
                return;
            }

            if (!Directory.Exists(saveDir)) {
                Directory.CreateDirectory(saveDir);
            }
            LoadFromResourcesOrSaveDir();
        }

        private GameRulesCatalog(string customSaveDir, bool forceLoad) {
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

            // 10. Subsystems and Vestiges
            var subsystemsMap = new Dictionary<string, SubsystemDefinition>(StringComparer.OrdinalIgnoreCase);
            var maneuversToken = GetResource(Properties.Resources.maneuvers, "maneuvers.json");

            if (maneuversToken is JObject maneuversObj) {
                var martialDef = new SubsystemDefinition(maneuversObj);
                subsystemsMap[martialDef.Id] = martialDef;
            }

            var vestiges = new Dictionary<string, Vestige>(StringComparer.OrdinalIgnoreCase);
            var vestigesToken = GetResource(Properties.Resources.vestiges, "vestiges.json");

            if (vestigesToken is JObject vestigesObj) {
                var pactDef = new SubsystemDefinition(vestigesObj);
                subsystemsMap[pactDef.Id] = pactDef;

                if (vestigesObj["elements"] is JArray elemArr) {
                    foreach (var elemToken in elemArr) {
                        if (elemToken is JObject elemObj) {
                            var vestige = new Vestige(elemObj);
                            vestiges[vestige.Name] = vestige;
                        }
                    }
                }
            }

            Subsystems = new ReadOnlyDictionary<string, SubsystemDefinition>(subsystemsMap);
            Vestiges = new ReadOnlyDictionary<string, Vestige>(vestiges);
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

        public Vestige GetVestige(string name) => FindEntity(Vestiges, name, x => x.Name);

        public SubsystemDefinition GetSubsystem(string name) => (name != null && Subsystems.TryGetValue(name, out var s)) ? s : null;

        public object GetEntity(string type, string name) {
            if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(name)) return null;
            switch (type.ToLowerInvariant()) {
                case "class":
                    return GetClass(name);
                case "race":
                    return GetRace(name);
                case "feat":
                    return GetFeat(name);
                case "weapon":
                    return GetWeapon(name);
                case "armor":
                    return GetArmor(name);
                case "shield":
                    return GetShield(name);
                case "item":
                    return GetItem(name);
                case "weaponproperty":
                    return GetWeaponProperty(name);
                case "armorproperty":
                    return GetArmorProperty(name);
                case "vestige":
                    return GetVestige(name);
                case "externaleffect":
                    return GetExternalEffect(name);
            }

            if (Subsystems != null) {
                foreach (var sub in Subsystems.Values) {
                    if (sub.Taxonomies != null && sub.Taxonomies.TryGetValue(type, out var taxMap)) {
                        if (taxMap.TryGetValue(name, out var entry)) return entry;
                    }
                    if (sub.Elements != null && sub.Elements.TryGetValue(name, out var elem)) {
                        return elem;
                    }
                }
            }

            return null;
        }

        public string ResolveTemplate(string template, IReadOnlyDictionary<string, string> choices) =>
            ResolveTemplateInternal(this, template, choices);

        internal static string ResolveTemplateInternal(IRulesCatalog catalog, string template, IReadOnlyDictionary<string, string> choices) {
            if (string.IsNullOrEmpty(template)) return "";
            if (!template.Contains("${")) return template;

            string GetChoice(string key) {
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
                if (parts.Length == 1) {
                    return GetChoice(parts[0]) ?? "";
                }

                string rootType = parts[0];
                string rootName = GetChoice(rootType);
                if (string.IsNullOrEmpty(rootName)) return "";

                object current = catalog?.GetEntity(rootType, rootName);
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
