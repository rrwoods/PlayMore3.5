using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Items;
using PlayMore35.Rules;
using PlayMore35.Subsystems;

namespace PlayMore35.Tests {
    public class FakeRulesCatalog : IRulesCatalog {
        public Dictionary<string, Race> RacesMap { get; } = new Dictionary<string, Race>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, CharacterClass> ClassesMap { get; } = new Dictionary<string, CharacterClass>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, Feat> FeatsMap { get; } = new Dictionary<string, Feat>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, Weapon> WeaponsMap { get; } = new Dictionary<string, Weapon>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, Armor> ArmorsMap { get; } = new Dictionary<string, Armor>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, Shield> ShieldsMap { get; } = new Dictionary<string, Shield>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, WeaponProperty> WeaponPropertiesMap { get; } = new Dictionary<string, WeaponProperty>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, ArmorProperty> ArmorPropertiesMap { get; } = new Dictionary<string, ArmorProperty>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, Item> ItemsMap { get; } = new Dictionary<string, Item>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, ExternalEffect> ExternalEffectsMap { get; } = new Dictionary<string, ExternalEffect>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, SubsystemDefinition> SubsystemsMap { get; } = new Dictionary<string, SubsystemDefinition>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<string, Race> Races => RacesMap;
        public IReadOnlyDictionary<string, CharacterClass> Classes => ClassesMap;
        public IReadOnlyDictionary<string, Feat> Feats {
            get {
                if (inner == null) return FeatsMap;
                var combined = new Dictionary<string, Feat>(StringComparer.OrdinalIgnoreCase);
                foreach (var kvp in inner.Feats) combined[kvp.Key] = kvp.Value;
                foreach (var kvp in FeatsMap) combined[kvp.Key] = kvp.Value;
                return combined;
            }
        }
        public IReadOnlyDictionary<string, Weapon> Weapons => WeaponsMap;
        public IReadOnlyDictionary<string, Armor> Armors => ArmorsMap;
        public IReadOnlyDictionary<string, Shield> Shields => ShieldsMap;
        public IReadOnlyDictionary<string, WeaponProperty> WeaponProperties => WeaponPropertiesMap;
        public IReadOnlyDictionary<string, ArmorProperty> ArmorProperties => ArmorPropertiesMap;
        public IReadOnlyDictionary<string, Item> Items => ItemsMap;
        public IReadOnlyDictionary<string, ExternalEffect> ExternalEffects => ExternalEffectsMap;
        public IReadOnlyDictionary<string, SubsystemDefinition> Subsystems => SubsystemsMap;

        private readonly IRulesCatalog? inner;

        public IReadOnlyDictionary<string, int> CoreFeatSlots => inner?.CoreFeatSlots ?? new ReadOnlyDictionary<string, int>(new Dictionary<string, int>());
        public IReadOnlyList<Statistic> CoreStats => inner?.CoreStats ?? new List<Statistic>().AsReadOnly();
        public IReadOnlyList<Effect> CoreEffects => inner?.CoreEffects ?? new List<Effect>().AsReadOnly();
        public IReadOnlyDictionary<string, FlagDefinition> CoreFlags => inner?.CoreFlags ?? new ReadOnlyDictionary<string, FlagDefinition>(new Dictionary<string, FlagDefinition>());
        public IReadOnlyDictionary<string, ParameterDefinition> CoreParameters => inner?.CoreParameters ?? new ReadOnlyDictionary<string, ParameterDefinition>(new Dictionary<string, ParameterDefinition>());
        public JToken CoreRulesJson => inner?.CoreRulesJson ?? new JObject();

        public FakeRulesCatalog(params SubsystemElement[] maneuvers) : this(maneuvers, null, null) { }
        public FakeRulesCatalog(IEnumerable<SubsystemElement> maneuvers, IRulesCatalog? inner) : this(maneuvers, null, inner) { }

        public FakeRulesCatalog(IEnumerable<SubsystemElement>? maneuvers = null, IEnumerable<SubsystemElement>? vestiges = null, IRulesCatalog? inner = null) {
            this.inner = inner;
            if (inner != null) {
                if (inner.Subsystems != null) {
                    foreach (var kvp in inner.Subsystems) {
                        SubsystemsMap[kvp.Key] = kvp.Value;
                    }
                }
            }
            if (!SubsystemsMap.ContainsKey("pact_magic") || vestiges != null) {
                BuildPactMagicSubsystem(vestiges);
            }
            if (!SubsystemsMap.ContainsKey("martial_maneuvers") || maneuvers != null) {
                BuildMartialManeuversSubsystem(maneuvers);
            }
        }

        public void BuildMartialManeuversSubsystem(IEnumerable<SubsystemElement> maneuvers = null) {
            var def = new SubsystemDefinition {
                Id = "martial_maneuvers",
                Name = "Martial Maneuvers",
                PrerequisiteUnit = "maneuver",
                PrerequisiteProperty = "discipline",
                LevelGating = new LevelGatingDefinition("level", "maximum maneuver level")
            };
            if (inner?.GetSubsystem("martial_maneuvers")?.Taxonomies != null) {
                foreach (var taxKvp in inner.GetSubsystem("martial_maneuvers").Taxonomies) {
                    var map = new Dictionary<string, TaxonomyEntry>(StringComparer.OrdinalIgnoreCase);
                    foreach (var entryKvp in taxKvp.Value) {
                        map[entryKvp.Key] = entryKvp.Value;
                    }
                    def.Taxonomies[taxKvp.Key] = map;
                }
            }
            if (!def.Taxonomies.ContainsKey("type")) {
                var typeMap = new Dictionary<string, TaxonomyEntry>(StringComparer.OrdinalIgnoreCase);
                foreach (var mType in new[] { "Strike", "Boost", "Counter", "Other" }) {
                    var entry = new TaxonomyEntry { Name = mType };
                    entry.SetProperty("category", "maneuver");
                    typeMap[mType] = entry;
                }
                var stanceEntry = new TaxonomyEntry { Name = "Stance" };
                stanceEntry.SetProperty("category", "stance");
                typeMap["Stance"] = stanceEntry;
                def.Taxonomies["type"] = typeMap;
            }
            def.ElementStatistics["level"] = new List<string>();
            var maneuverGroup = new SlotGroupDefinition {
                Name = "maneuvers",
                Filter = "maneuver",
                CapacityStat = "maneuvers known:{source}",
                MaxLevel = new MaxLevelDefinition("max maneuver level:{source}", 0),
                DefaultState = "unreadied"
            };
            maneuverGroup.StateCapacities.Add(new StateCapacityDefinition(
                new[] { "available", "expended", "withheld" },
                "maneuvers readied:{source}",
                "readied"
            ));
            maneuverGroup.States.AddRange(new[] { "unreadied", "available", "expended", "withheld" });
            var maxManeuverTable = new Dictionary<int, int> {
                { 0, 0 }, { 1, 1 }, { 3, 2 }, { 5, 3 }, { 7, 4 },
                { 9, 5 }, { 11, 6 }, { 13, 7 }, { 15, 8 }, { 17, 9 }
            };
            maneuverGroup.Effects.Add(new Effect("max maneuver level:{source}")
                .AddModifier("max maneuver level:{source}", new Modifier(new LookupAmount("initiator level:{source}", maxManeuverTable, -1, 1), ModifierTypes.INTERNAL)));
            def.SlotGroups["maneuvers"] = maneuverGroup;

            var stances = new SlotGroupDefinition {
                Name = "stances",
                Filter = "stance",
                CapacityStat = "stances known:{source}",
                MaxLevel = new MaxLevelDefinition("max maneuver level:{source}", 0),
                DefaultState = "inactive"
            };
            stances.StateCapacities.Add(new StateCapacityDefinition(
                new[] { "active" },
                "active stances",
                "active"
            ));
            stances.States.AddRange(new[] { "inactive", "active" });
            def.SlotGroups["stances"] = stances;

            def.Projections.Add(new EffectProjectionRule("stances", "active", "benefits"));
            def.DefaultElementVerbs["execute"] = new List<SubsystemStep>();
            def.Verbs["ready"] = new List<SubsystemStep> {
                new SubsystemStep { Type = "transition", SlotGroup = "maneuvers", Selector = "invoked", FromState = "unreadied", ToState = "available" }
            };
            def.Verbs["unready"] = new List<SubsystemStep> {
                new SubsystemStep { Type = "transition", SlotGroup = "maneuvers", Selector = "invoked", FromState = "!unreadied", ToState = "unreadied" }
            };
            def.Verbs["expend"] = new List<SubsystemStep> {
                new SubsystemStep { Type = "transition", SlotGroup = "maneuvers", Selector = "invoked", FromState = "available", ToState = "expended" },
                new SubsystemStep { Type = "invoke_element_verb", Verb = "execute", Selector = "invoked" }
            };
            def.Verbs["adopt_stance"] = new List<SubsystemStep> {
                new SubsystemStep { Type = "transition", SlotGroup = "stances", Selector = "invoked", FromState = "*", ToState = "active" }
            };
            def.Verbs["exit_stance"] = new List<SubsystemStep> {
                new SubsystemStep { Type = "transition", SlotGroup = "stances", Selector = "invoked", FromState = "active", ToState = "inactive" }
            };
            def.Verbs["exit_all_stances"] = new List<SubsystemStep> {
                new SubsystemStep { Type = "transition", SlotGroup = "stances", Selector = "all", FromState = "active", ToState = "inactive" }
            };
            def.Verbs["recover_all"] = new List<SubsystemStep> {
                new SubsystemStep { Type = "transition", SlotGroup = "maneuvers", Selector = "all", FromState = "expended", ToState = "available" }
            };
            def.Verbs["recover_select"] = new List<SubsystemStep> {
                new SubsystemStep { Type = "transition", SlotGroup = "maneuvers", Selector = "select", CountExpression = "1", FromState = "expended", ToState = "available" }
            };
            def.Verbs["deal"] = new List<SubsystemStep> {
                new SubsystemStep { Type = "transition", SlotGroup = "maneuvers", Selector = "all", FromState = "!unreadied", ToState = "withheld" },
                new SubsystemStep { Type = "transition", SlotGroup = "maneuvers", Selector = "random", CountExpression = "maneuvers granted:{source}", FromState = "withheld", ToState = "available" }
            };
            def.Verbs["advance_turn"] = new List<SubsystemStep> {
                new SubsystemStep { Type = "transition", SlotGroup = "maneuvers", Selector = "random", CountExpression = "1", FromState = "withheld", ToState = "available", OnEmptyVerb = "deal" }
            };
            def.Verbs["reset_encounter"] = new List<SubsystemStep> {
                new SubsystemStep { Type = "transition", SlotGroup = "maneuvers", Selector = "all", FromState = "expended", ToState = "available" }
            };
            SubsystemsMap["martial_maneuvers"] = def;

            if (!TryGetClass("Warblade", out _)) {
                var wb = new CharacterClass { Name = "Warblade" };
                var wbSub = new SubsystemFeature();
                wbSub.Verbs["ready"] = def.Verbs["ready"];
                wbSub.Verbs["recover"] = def.Verbs["recover_all"];
                wbSub.Verbs["reset_encounter"] = def.Verbs["reset_encounter"];
                wb.Subsystems["martial_maneuvers"] = wbSub;
                ClassesMap["Warblade"] = wb;
            }
            if (!TryGetClass("Swordsage", out _)) {
                var ss = new CharacterClass { Name = "Swordsage" };
                var ssSub = new SubsystemFeature();
                ssSub.Verbs["ready"] = def.Verbs["ready"];
                ssSub.Verbs["recover"] = def.Verbs["recover_select"];
                ssSub.Verbs["reset_encounter"] = def.Verbs["reset_encounter"];
                ss.Subsystems["martial_maneuvers"] = ssSub;
                ClassesMap["Swordsage"] = ss;
            }
            if (!TryGetClass("Crusader", out _)) {
                var cr = new CharacterClass { Name = "Crusader" };
                var crSub = new SubsystemFeature();
                crSub.Verbs["ready"] = new List<SubsystemStep> {
                    new SubsystemStep { Type = "transition", SlotGroup = "maneuvers", Selector = "invoked", FromState = "unreadied", ToState = "withheld" }
                };
                crSub.Verbs["deal"] = def.Verbs["deal"];
                crSub.Verbs["advance_turn"] = def.Verbs["advance_turn"];
                crSub.Verbs["reset_encounter"] = def.Verbs["deal"];
                cr.Subsystems["martial_maneuvers"] = crSub;
                ClassesMap["Crusader"] = cr;
            }

            if (maneuvers != null) {
                foreach (var elem in maneuvers) {
                    if (elem == null || string.IsNullOrEmpty(elem.Name)) continue;
                    if (def.Taxonomies.Count > 0) {
                        foreach (var taxKvp in def.Taxonomies) {
                            if (elem.Properties.TryGetValue(taxKvp.Key, out var propVal) && propVal != null) {
                                var entryName = propVal.ToString();
                                if (taxKvp.Value.TryGetValue(entryName, out var entryDef)) {
                                    elem.SetProperty(taxKvp.Key, entryDef);
                                }
                            }
                        }
                    }
                    def.Elements[elem.Name] = elem;
                }
            }
        }

        public void BuildPactMagicSubsystem(IEnumerable<SubsystemElement> vestiges = null) {
            var def = new SubsystemDefinition {
                Id = "pact_magic",
                Name = "Pact Magic"
            };
            var vestigesSlotGroup = new SlotGroupDefinition {
                Name = "vestiges",
                CapacityStat = "number of vestiges",
                MaxLevel = new MaxLevelDefinition("maximum vestige level", 0),
                DefaultState = "empty"
            };
            vestigesSlotGroup.States.AddRange(new[] { "empty", "good_pact", "poor_pact" });
            var maxVestigeTable = new Dictionary<int, int> {
                { 1, 1 }, { 3, 2 }, { 5, 3 }, { 7, 4 },
                { 10, 5 }, { 12, 6 }, { 15, 7 }, { 17, 8 }
            };
            vestigesSlotGroup.Effects.Add(new Effect("maximum vestige level")
                .AddModifier("maximum vestige level", new Modifier(new LookupAmount("EBL for max vestige level", maxVestigeTable, -1, 1), ModifierTypes.UNTYPED)));
            def.SlotGroups["vestiges"] = vestigesSlotGroup;
            def.LevelGating = new LevelGatingDefinition("level");
            def.RequiredElementProperties.Add("binding DC");
            def.RequiredElementProperties.Add("level");
            def.ElementStatistics["binding DC"] = new List<string> { "binding DCs" };
            def.ElementStatistics["level"] = new List<string>();

            def.Verbs["bind_good_pact"] = new List<SubsystemStep> {
                new SubsystemStep {
                    Type = "assign",
                    SlotGroup = "vestiges",
                    ToState = "good_pact"
                }
            };
            def.Verbs["bind_poor_pact"] = new List<SubsystemStep> {
                new SubsystemStep {
                    Type = "assign",
                    SlotGroup = "vestiges",
                    ToState = "poor_pact"
                }
            };
            def.Verbs["unbind"] = new List<SubsystemStep> {
                new SubsystemStep {
                    Type = "transition",
                    SlotGroup = "vestiges",
                    Selector = "invoked",
                    FromState = "*",
                    ToState = "empty"
                }
            };

            def.Projections.Add(new EffectProjectionRule("vestiges", "good_pact", "abilities"));
            def.Projections.Add(new EffectProjectionRule("vestiges", "poor_pact", "abilities", "influence"));

            if (inner != null && inner.TryGetSubsystem("pact_magic", out var innerPact)) {
                foreach (var kvp in innerPact.EffectTemplates) {
                    def.EffectTemplates[kvp.Key] = (JObject)kvp.Value.DeepClone();
                }
            }

            SubsystemsMap["pact_magic"] = def;

            if (vestiges != null) {
                foreach (var elem in vestiges) {
                    if (elem == null || string.IsNullOrEmpty(elem.Name)) continue;
                    def.Elements[elem.Name] = elem;
                }
            }
        }

        public bool TryGetRace(string name, [NotNullWhen(true)] out Race? result) {
            if (name != null && RacesMap.TryGetValue(name, out var val)) {
                result = val;
                return true;
            }
            if (inner != null) {
                return inner.TryGetRace(name, out result);
            }
            result = null;
            return false;
        }

        public Race GetRace(string name) {
            if (TryGetRace(name, out var race)) {
                return race;
            }
            throw new KeyNotFoundException($"Race '{name}' was not found in the rules catalog.");
        }

        public bool TryGetClass(string name, [NotNullWhen(true)] out CharacterClass? result) {
            if (name != null && ClassesMap.TryGetValue(name, out var val)) {
                result = val;
                return true;
            }
            if (inner != null) {
                return inner.TryGetClass(name, out result);
            }
            result = null;
            return false;
        }

        public CharacterClass GetClass(string name) {
            if (TryGetClass(name, out var cls)) {
                return cls;
            }
            throw new KeyNotFoundException($"Class '{name}' was not found in the rules catalog.");
        }

        public bool TryGetFeat(string name, [NotNullWhen(true)] out Feat? result) {
            if (name != null && FeatsMap.TryGetValue(name, out var val)) {
                result = val;
                return true;
            }
            if (inner != null) {
                return inner.TryGetFeat(name, out result);
            }
            result = null;
            return false;
        }

        public Feat GetFeat(string name) {
            if (TryGetFeat(name, out var feat)) {
                return feat;
            }
            throw new KeyNotFoundException($"Feat '{name}' was not found in the rules catalog.");
        }

        public bool TryGetWeapon(string name, [NotNullWhen(true)] out Weapon? result) {
            if (name != null && WeaponsMap.TryGetValue(name, out var val)) {
                result = val;
                return true;
            }
            if (inner != null) {
                return inner.TryGetWeapon(name, out result);
            }
            result = null;
            return false;
        }

        public Weapon GetWeapon(string name) {
            if (TryGetWeapon(name, out var weapon)) {
                return weapon;
            }
            throw new KeyNotFoundException($"Weapon '{name}' was not found in the rules catalog.");
        }

        public bool TryGetArmor(string name, [NotNullWhen(true)] out Armor? result) {
            if (name != null && ArmorsMap.TryGetValue(name, out var val)) {
                result = val;
                return true;
            }
            if (inner != null) {
                return inner.TryGetArmor(name, out result);
            }
            result = null;
            return false;
        }

        public Armor GetArmor(string name) {
            if (TryGetArmor(name, out var armor)) {
                return armor;
            }
            throw new KeyNotFoundException($"Armor '{name}' was not found in the rules catalog.");
        }

        public bool TryGetShield(string name, [NotNullWhen(true)] out Shield? result) {
            if (name != null && ShieldsMap.TryGetValue(name, out var val)) {
                result = val;
                return true;
            }
            if (inner != null) {
                return inner.TryGetShield(name, out result);
            }
            result = null;
            return false;
        }

        public Shield GetShield(string name) {
            if (TryGetShield(name, out var shield)) {
                return shield;
            }
            throw new KeyNotFoundException($"Shield '{name}' was not found in the rules catalog.");
        }

        public bool TryGetWeaponProperty(string name, [NotNullWhen(true)] out WeaponProperty? result) {
            if (name != null && WeaponPropertiesMap.TryGetValue(name, out var val)) {
                result = val;
                return true;
            }
            if (inner != null) {
                return inner.TryGetWeaponProperty(name, out result);
            }
            result = null;
            return false;
        }

        public WeaponProperty GetWeaponProperty(string name) {
            if (TryGetWeaponProperty(name, out var prop)) {
                return prop;
            }
            throw new KeyNotFoundException($"WeaponProperty '{name}' was not found in the rules catalog.");
        }

        public bool TryGetArmorProperty(string name, [NotNullWhen(true)] out ArmorProperty? result) {
            if (name != null && ArmorPropertiesMap.TryGetValue(name, out var val)) {
                result = val;
                return true;
            }
            if (inner != null) {
                return inner.TryGetArmorProperty(name, out result);
            }
            result = null;
            return false;
        }

        public ArmorProperty GetArmorProperty(string name) {
            if (TryGetArmorProperty(name, out var prop)) {
                return prop;
            }
            throw new KeyNotFoundException($"ArmorProperty '{name}' was not found in the rules catalog.");
        }

        public bool TryGetItem(string name, [NotNullWhen(true)] out Item? result) {
            if (name != null && ItemsMap.TryGetValue(name, out var val)) {
                result = val;
                return true;
            }
            if (inner != null) {
                return inner.TryGetItem(name, out result);
            }
            result = null;
            return false;
        }

        public Item GetItem(string name) {
            if (TryGetItem(name, out var item)) {
                return item;
            }
            throw new KeyNotFoundException($"Item '{name}' was not found in the rules catalog.");
        }

        public bool TryGetExternalEffect(string name, [NotNullWhen(true)] out ExternalEffect? result) {
            if (name != null && ExternalEffectsMap.TryGetValue(name, out var val)) {
                result = val;
                return true;
            }
            if (inner != null) {
                return inner.TryGetExternalEffect(name, out result);
            }
            result = null;
            return false;
        }

        public ExternalEffect GetExternalEffect(string name) {
            if (TryGetExternalEffect(name, out var ext)) {
                return ext;
            }
            throw new KeyNotFoundException($"ExternalEffect '{name}' was not found in the rules catalog.");
        }

        public bool TryGetSubsystem(string name, [NotNullWhen(true)] out SubsystemDefinition? result) {
            if (name != null && SubsystemsMap.TryGetValue(name, out var val)) {
                result = val;
                return true;
            }
            if (inner != null) {
                return inner.TryGetSubsystem(name, out result);
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

        public void RegisterSubsystem(SubsystemDefinition def) {
            if (def != null && !string.IsNullOrEmpty(def.Id)) {
                SubsystemsMap[def.Id] = def;
            }
        }

        public static SubsystemDefinition CreateSyntheticAlchemySubsystem() {
            var def = new SubsystemDefinition {
                Id = "synthetic_alchemy",
                Name = "Synthetic Alchemy"
            };

            var flasks = new SlotGroupDefinition {
                Name = "flasks",
                CapacityStat = "max_flasks",
                FixedCapacity = 3,
                DefaultState = "empty"
            };
            flasks.States.AddRange(new[] { "empty", "prepared", "expended" });
            def.SlotGroups["flasks"] = flasks;

            var infusions = new SlotGroupDefinition {
                Name = "infusions",
                CapacityStat = "max_infusions",
                FixedCapacity = 2,
                DefaultState = "inactive"
            };
            infusions.States.AddRange(new[] { "inactive", "active" });
            def.SlotGroups["infusions"] = infusions;

            def.Pools["catalyst"] = new PoolDefinition {
                Id = "catalyst",
                Name = "Catalyst Points",
                CapacityStat = "catalyst_capacity",
                DefaultCapacity = 10
            };

            def.Pools["heat"] = new PoolDefinition {
                Id = "heat",
                Name = "Heat Points",
                CapacityStat = "heat_capacity",
                DefaultCapacity = 5
            };

            def.Verbs["throw"] = new List<SubsystemStep> {
                new SubsystemStep {
                    Type = "transition",
                    SlotGroup = "flasks",
                    Selector = "invoked",
                    FromState = "prepared",
                    ToState = "expended"
                },
                new SubsystemStep {
                    Type = "invoke_element_verb",
                    Verb = "execute",
                    Selector = "invoked"
                }
            };

            def.Verbs["prepare_all"] = new List<SubsystemStep> {
                new SubsystemStep {
                    Type = "transition",
                    SlotGroup = "flasks",
                    Selector = "all",
                    FromState = "expended",
                    ToState = "prepared"
                }
            };

            def.Verbs["prepare_select"] = new List<SubsystemStep> {
                new SubsystemStep {
                    Type = "transition",
                    SlotGroup = "flasks",
                    Selector = "select",
                    FromState = "expended",
                    ToState = "prepared"
                }
            };

            def.Verbs["deal_random"] = new List<SubsystemStep> {
                new SubsystemStep {
                    Type = "transition",
                    SlotGroup = "flasks",
                    Selector = "random",
                    FromState = "empty",
                    ToState = "prepared",
                    CountExpression = "random_flasks_granted"
                }
            };

            def.DefaultElementVerbs["execute"] = new List<SubsystemStep>();

            def.Projections.Add(new EffectProjectionRule("infusions", "active", "effects"));

            var acidFlask = new SubsystemElement {
                Name = "Acid Flask",
                Type = "Flask"
            };
            acidFlask.Verbs["execute"] = new List<SubsystemStep> {
                new SubsystemStep {
                    Type = "action",
                    Action = new SheetAction().AdjustParam("acid_splash_count", 1)
                }
            };
            def.Elements[acidFlask.Name] = acidFlask;

            var fireFlask = new SubsystemElement {
                Name = "Fire Flask",
                Type = "Flask"
            };
            def.Elements[fireFlask.Name] = fireFlask;

            var speedInfusion = new SubsystemElement {
                Name = "Speed Infusion",
                Type = "Infusion"
            };
            var speedEffect = new Effect("Speed Infusion Effect");
            speedEffect.AddModifier("DEX score", new Modifier(4, ModifierTypes.UNTYPED));
            speedInfusion.AddEffect("effects", speedEffect);
            def.Elements[speedInfusion.Name] = speedInfusion;

            var strengthInfusion = new SubsystemElement {
                Name = "Strength Infusion",
                Type = "Infusion"
            };
            var strEffect = new Effect("Strength Infusion Effect");
            strEffect.AddModifier("STR score", new Modifier(2, ModifierTypes.ENHANCEMENT));
            strengthInfusion.AddEffect("effects", strEffect);
            def.Elements[strengthInfusion.Name] = strengthInfusion;

            return def;
        }

        public string ResolveTemplate(string template, IReadOnlyDictionary<string, object> choices) =>
            GameRulesCatalog.ResolveTemplateInternal(template, choices);

        public IEnumerable<string> ChoicesForKey(string choiceDescription) => inner?.ChoicesForKey(choiceDescription) ?? Enumerable.Empty<string>();
    }
}
