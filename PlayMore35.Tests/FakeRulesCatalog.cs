using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Items;
using PlayMore35.Rules;
using PlayMore35.Subsystems;
using PlayMore35.Subsystems.Maneuvers;

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
        public Dictionary<string, Maneuver> ManeuversMap { get; } = new Dictionary<string, Maneuver>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, Vestige> VestigesMap { get; } = new Dictionary<string, Vestige>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, MartialDiscipline> DisciplinesMap { get; } = new Dictionary<string, MartialDiscipline>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, SubsystemDefinition> SubsystemsMap { get; } = new Dictionary<string, SubsystemDefinition>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<string, Race> Races => RacesMap;
        public IReadOnlyDictionary<string, CharacterClass> Classes => ClassesMap;
        public IReadOnlyDictionary<string, Feat> Feats => FeatsMap;
        public IReadOnlyDictionary<string, Weapon> Weapons => WeaponsMap;
        public IReadOnlyDictionary<string, Armor> Armors => ArmorsMap;
        public IReadOnlyDictionary<string, Shield> Shields => ShieldsMap;
        public IReadOnlyDictionary<string, WeaponProperty> WeaponProperties => WeaponPropertiesMap;
        public IReadOnlyDictionary<string, ArmorProperty> ArmorProperties => ArmorPropertiesMap;
        public IReadOnlyDictionary<string, Item> Items => ItemsMap;
        public IReadOnlyDictionary<string, ExternalEffect> ExternalEffects => ExternalEffectsMap;
        public IReadOnlyDictionary<string, Maneuver> Maneuvers => ManeuversMap;
        public IReadOnlyDictionary<string, Vestige> Vestiges => VestigesMap;
        public IReadOnlyDictionary<string, MartialDiscipline> Disciplines => DisciplinesMap.Count > 0 ? DisciplinesMap : (inner?.Disciplines ?? MartialDiscipline.All);
        public IReadOnlyDictionary<string, SubsystemDefinition> Subsystems => SubsystemsMap;

        private readonly IRulesCatalog inner;

        public IReadOnlyDictionary<string, int> CoreFeatSlots => inner?.CoreFeatSlots ?? new ReadOnlyDictionary<string, int>(new Dictionary<string, int>());
        public IReadOnlyList<Statistic> CoreStats => inner?.CoreStats ?? new List<Statistic>().AsReadOnly();
        public IReadOnlyList<Effect> CoreEffects => inner?.CoreEffects ?? new List<Effect>().AsReadOnly();
        public IReadOnlyDictionary<string, FlagDefinition> CoreFlags => inner?.CoreFlags ?? new ReadOnlyDictionary<string, FlagDefinition>(new Dictionary<string, FlagDefinition>());
        public IReadOnlyDictionary<string, ParameterDefinition> CoreParameters => inner?.CoreParameters ?? new ReadOnlyDictionary<string, ParameterDefinition>(new Dictionary<string, ParameterDefinition>());
        public JToken CoreRulesJson => inner?.CoreRulesJson ?? new JObject();

        public FakeRulesCatalog(params Maneuver[] maneuvers) : this(maneuvers, null, null) { }
        public FakeRulesCatalog(params Vestige[] vestiges) : this(null, vestiges, null) { }
        public FakeRulesCatalog(IEnumerable<Maneuver> maneuvers, IRulesCatalog inner) : this(maneuvers, null, inner) { }
        public FakeRulesCatalog(IEnumerable<Vestige> vestiges, IRulesCatalog inner) : this(null, vestiges, inner) { }

        public FakeRulesCatalog(IEnumerable<Maneuver> maneuvers = null, IEnumerable<Vestige> vestiges = null, IRulesCatalog inner = null) {
            this.inner = inner;
            if (inner != null) {
                if (inner.Vestiges != null) {
                    foreach (var kvp in inner.Vestiges) {
                        VestigesMap[kvp.Key] = kvp.Value;
                    }
                }
                if (inner.Maneuvers != null) {
                    foreach (var kvp in inner.Maneuvers) {
                        ManeuversMap[kvp.Key] = kvp.Value;
                    }
                }
                if (inner.Subsystems != null) {
                    foreach (var kvp in inner.Subsystems) {
                        SubsystemsMap[kvp.Key] = kvp.Value;
                    }
                }
            }
            if (maneuvers != null) {
                foreach (var m in maneuvers) {
                    if (m?.Name != null) ManeuversMap[m.Name] = m;
                }
            }
            if (vestiges != null) {
                foreach (var v in vestiges) {
                    if (v?.Name != null) VestigesMap[v.Name] = v;
                }
            }
            if (VestigesMap.Count > 0 || inner?.GetSubsystem("pact_magic") != null) {
                BuildPactMagicSubsystem();
            }
        }

        public void BuildPactMagicSubsystem() {
            SubsystemDefinition def;
            if (!SubsystemsMap.TryGetValue("pact_magic", out def) || def == null) {
                def = new SubsystemDefinition {
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

                def.Projections.Add(new KindProjectionRule("vestiges", "good_pact", "abilities"));
                def.Projections.Add(new KindProjectionRule("vestiges", "poor_pact", "abilities", "influence"));
                SubsystemsMap["pact_magic"] = def;
            }

            foreach (var v in VestigesMap.Values) {
                if (v == null || string.IsNullOrEmpty(v.Name)) continue;
                var elem = new SubsystemElement {
                    Name = v.Name
                };
                elem.SetProperty("level", v.Level);
                elem.SetProperty("binding DC", v.BindingDc);
                if (v.Abilities != null) {
                    foreach (var a in v.Abilities) elem.AddKindEffect("abilities", a);
                }
                if (v.Influence != null) {
                    elem.AddKindEffect("influence", v.Influence);
                }
                def.Elements[elem.Name] = elem;
            }
        }

        public Race GetRace(string name) => (name != null && RacesMap.TryGetValue(name, out var val)) ? val : inner?.GetRace(name);
        public CharacterClass GetClass(string name) => (name != null && ClassesMap.TryGetValue(name, out var val)) ? val : inner?.GetClass(name);
        public Feat GetFeat(string name) => (name != null && FeatsMap.TryGetValue(name, out var val)) ? val : inner?.GetFeat(name);
        public Weapon GetWeapon(string name) => (name != null && WeaponsMap.TryGetValue(name, out var val)) ? val : inner?.GetWeapon(name);
        public Armor GetArmor(string name) => (name != null && ArmorsMap.TryGetValue(name, out var val)) ? val : inner?.GetArmor(name);
        public Shield GetShield(string name) => (name != null && ShieldsMap.TryGetValue(name, out var val)) ? val : inner?.GetShield(name);
        public WeaponProperty GetWeaponProperty(string name) => (name != null && WeaponPropertiesMap.TryGetValue(name, out var val)) ? val : inner?.GetWeaponProperty(name);
        public ArmorProperty GetArmorProperty(string name) => (name != null && ArmorPropertiesMap.TryGetValue(name, out var val)) ? val : inner?.GetArmorProperty(name);
        public Item GetItem(string name) => (name != null && ItemsMap.TryGetValue(name, out var val)) ? val : inner?.GetItem(name);
        public ExternalEffect GetExternalEffect(string name) => (name != null && ExternalEffectsMap.TryGetValue(name, out var val)) ? val : inner?.GetExternalEffect(name);
        public Maneuver GetManeuver(string name) => (name != null && ManeuversMap.TryGetValue(name, out var val)) ? val : inner?.GetManeuver(name);
        public Vestige GetVestige(string name) => (name != null && VestigesMap.TryGetValue(name, out var val)) ? val : inner?.GetVestige(name);
        public MartialDiscipline GetDiscipline(string name) => (name != null && DisciplinesMap.TryGetValue(name, out var val)) ? val : (inner?.GetDiscipline(name) ?? MartialDiscipline.Get(name));
        public SubsystemDefinition GetSubsystem(string name) => (name != null && SubsystemsMap.TryGetValue(name, out var val)) ? val : inner?.GetSubsystem(name);

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

            def.Projections.Add(new KindProjectionRule("infusions", "active", "effects"));

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
            speedInfusion.AddKindEffect("effects", speedEffect);
            def.Elements[speedInfusion.Name] = speedInfusion;

            var strengthInfusion = new SubsystemElement {
                Name = "Strength Infusion",
                Type = "Infusion"
            };
            var strEffect = new Effect("Strength Infusion Effect");
            strEffect.AddModifier("STR score", new Modifier(2, ModifierTypes.ENHANCEMENT));
            strengthInfusion.AddKindEffect("effects", strEffect);
            def.Elements[strengthInfusion.Name] = strengthInfusion;

            return def;
        }

        public object GetEntity(string type, string name) {
            if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(name)) return null;
            switch (type.ToLowerInvariant()) {
                case "maneuver":
                    return GetManeuver(name);
                case "discipline":
                    return GetDiscipline(name);
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
                default:
                    return null;
            }
        }

        public string ResolveTemplate(string template, IReadOnlyDictionary<string, string> choices) =>
            GameRulesCatalog.ResolveTemplateInternal(this, template, choices);

        public IEnumerable<string> ChoicesForKey(string choiceDescription) => inner?.ChoicesForKey(choiceDescription) ?? Enumerable.Empty<string>();
    }
}
