using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Items;
using PlayMore35.Subsystems;

namespace PlayMore35.Tests {
    public static class TestFactory {
        public static Item CreateItem(
            Equipped.Slot slot,
            Effect? effect = null,
            decimal weight = 0,
            IEnumerable<FlagDefinition>? flags = null,
            IEnumerable<Statistic>? stats = null,
            string? name = null) {
            var itemEffect = effect ?? new Effect(name ?? "Test Item");
            var json = new JObject {
                ["slot"] = slot.ToString(),
                ["effect"] = itemEffect.ToJson(),
                ["weight"] = weight
            };
            if (flags != null) {
                json["flags"] = new JArray(flags.Select(f => f.ToJson()));
            }
            if (stats != null) {
                json["stats"] = new JArray(stats.Select(s => s.ToJson()));
            }
            return new Item(json);
        }

        public static CharacterClass CreateClass(
            string name,
            string prerequisites = "",
            int hitDie = 8,
            int skillPoints = 2,
            CharacterClass.BonusProgression bab = CharacterClass.BonusProgression.SLOW,
            CharacterClass.BonusProgression fort = CharacterClass.BonusProgression.SLOW,
            CharacterClass.BonusProgression refSave = CharacterClass.BonusProgression.SLOW,
            CharacterClass.BonusProgression will = CharacterClass.BonusProgression.SLOW,
            Dictionary<string, SubsystemFeature>? subsystems = null) {
            var json = new JObject {
                ["name"] = name,
                ["prerequisites"] = prerequisites,
                ["hit die"] = hitDie,
                ["class skills"] = new JArray(),
                ["skill points"] = skillPoints,
                ["base attack bonus"] = bab.ToString(),
                ["base fort save"] = fort.ToString(),
                ["base ref save"] = refSave.ToString(),
                ["base will save"] = will.ToString(),
                ["weapon proficiencies"] = new JArray(),
                ["armor proficiencies"] = new JArray(),
                ["shield proficiencies"] = new JArray(),
                ["exotic shield proficiencies"] = new JArray(),
                ["class features"] = new JArray()
            };
            if (subsystems != null && subsystems.Count > 0) {
                var subsObj = new JObject();
                foreach (var kvp in subsystems) {
                    var subJson = new JObject(
                        new JProperty("stats", new JArray(kvp.Value.Stats)),
                        new JProperty("effects", new JArray(kvp.Value.Effects.Select(x => x.ToJson())))
                    );
                    if (kvp.Value.Verbs != null && kvp.Value.Verbs.Count > 0) {
                        var verbsObj = new JObject();
                        foreach (var vKvp in kvp.Value.Verbs) {
                            verbsObj[vKvp.Key] = new JArray(vKvp.Value.Select(s => s.ToJson()));
                        }
                        subJson["verbs"] = verbsObj;
                    }
                    subsObj[kvp.Key] = subJson;
                }
                json["subsystems"] = subsObj;
            }
            return new CharacterClass(json);
        }

        public static Attack CreateAttack(
            string name,
            int bonus,
            DiceGroup baseDamage,
            IEnumerable<DiceGroup>? bonusDice = null,
            IEnumerable<DiceGroup>? criticalBonusDice = null,
            int criticalMultiplier = 2,
            int threatRange = 20,
            int rangeIncrement = 0) {
            var bDice = (bonusDice ?? Enumerable.Empty<DiceGroup>()).ToList();
            var cbDice = criticalBonusDice != null
                ? criticalBonusDice.ToList()
                : new List<DiceGroup>(bDice);

            var attack = (Attack)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Attack));
            var type = typeof(Attack);
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;

            type.GetField("WeaponName", flags)!.SetValue(attack, name);
            type.GetField("WeaponTags", flags)!.SetValue(attack, new List<string>());
            attack.AttackBonus = (bonus, new List<string> { $"+{bonus} base attack" });
            type.GetProperty("BaseDamage", flags)!.SetValue(attack, baseDamage);
            type.GetProperty("BonusDice", flags)!.SetValue(attack, bDice.AsReadOnly());
            type.GetProperty("CriticalBonusDice", flags)!.SetValue(attack, cbDice.AsReadOnly());
            type.GetProperty("CriticalMultiplier", flags)!.SetValue(attack, criticalMultiplier);
            type.GetProperty("ThreatRange", flags)!.SetValue(attack, threatRange);
            type.GetProperty("RangeIncrement", flags)!.SetValue(attack, rangeIncrement);
            type.GetProperty("DamageExplanation", flags)!.SetValue(attack, new List<string> { baseDamage?.ToString() ?? "" });
            type.GetProperty("CriticalDamageExplanation", flags)!.SetValue(attack, new List<string> { $"{baseDamage?.ToString()} ×{criticalMultiplier}" });

            return attack;
        }

        public static ArmorProperty CreateArmorProperty(
            string name,
            Effect? globalEffect = null,
            Modifier? acModifier = null,
            Modifier? maxDexModifier = null,
            Modifier? acpModifier = null,
            Modifier? asfModifier = null) {
            var json = new JObject(new JProperty("name", name));
            if (globalEffect != null) json["effect"] = globalEffect.ToJson();
            if (acModifier != null) json["AC modifier"] = acModifier.ToJson();
            if (maxDexModifier != null) json["max dex modifier"] = maxDexModifier.ToJson();
            if (acpModifier != null) json["armor check penalty modifier"] = acpModifier.ToJson();
            if (asfModifier != null) json["spell failure modifier"] = asfModifier.ToJson();
            return new ArmorProperty(json);
        }

        public static Weapon CreateWeapon(
            string name,
            int sizeCategory = 0,
            Weapon.Effort effort = Weapon.Effort.LIGHT,
            decimal weight = 1.0m) {
            var json = new JObject {
                ["name"] = name,
                ["weight"] = weight,
                ["effort"] = effort.ToString()
            };
            return new Weapon(json);
        }

        public static LookupAmount CreateLookupAmount(
            string basis,
            Dictionary<int, int> lookup,
            int missAdjust = 0,
            int missFactor = 1) {
            var json = new JObject(
                new JProperty("value", new VariableAmount(basis).ToJson()),
                new JProperty("table", new JObject(lookup.Select(x => new JProperty(x.Key.ToString(), x.Value))))
            );
            if (missAdjust != 0) {
                json["miss adjust"] = missAdjust;
                json["miss factor"] = missFactor;
            }
            return new LookupAmount(json);
        }

        public static Modifier CreateModifier(IAmount amount, string type) {
            var json = new JObject(
                new JProperty("type", type),
                new JProperty("amounts", new JArray(amount.ToJson()))
            );
            return new Modifier(json);
        }

        public static ControllableFlag CreateControllableFlag(
            string name,
            Character character,
            bool on = false,
            string category = "",
            string description = "") {
            var json = new JObject {
                ["name"] = name,
                ["category"] = category,
                ["description"] = description
            };
            var flag = new ControllableFlag(json, character);
            if (on) {
                flag.On = true;
            }
            return flag;
        }

        public static FlagDefinition CreateFlagDefinition(
            string name,
            string category = "",
            string description = "",
            IToggle? show = null,
            ActionResource? resource = null) {
            var json = new JObject(
                new JProperty("name", name ?? ""),
                new JProperty("category", category ?? ""),
                new JProperty("description", description ?? "")
            );
            if (show != null && show != AlwaysOn.Instance) {
                json.Add(new JProperty("show", show.ToJson()));
            }
            if (resource != null) {
                json.Add(new JProperty("resource", resource.ToJson()));
            }
            return new FlagDefinition(json);
        }

        public static SubsystemStep CreateSubsystemStep(
            string type,
            string slotGroup = "",
            string selector = "invoked",
            string? fromState = null,
            string toState = "",
            string countExpression = "1",
            string onEmptyVerb = "",
            string verb = "",
            SheetAction? action = null,
            string pool = "",
            string op = "consume",
            int amount = 0) {
            var json = new JObject(new JProperty("type", type));
            if (!string.IsNullOrEmpty(slotGroup)) json["slot_group"] = slotGroup;
            if (!string.IsNullOrEmpty(selector)) json["selector"] = selector;
            if (fromState != null) json["from"] = fromState;
            if (!string.IsNullOrEmpty(toState)) json["to"] = toState;
            if (!string.IsNullOrEmpty(countExpression)) json["count"] = countExpression;
            if (!string.IsNullOrEmpty(onEmptyVerb)) json["on_empty_verb"] = onEmptyVerb;
            if (!string.IsNullOrEmpty(verb)) json["verb"] = verb;
            if (action != null) json["action"] = action.ToJson();
            if (!string.IsNullOrEmpty(pool)) json["pool"] = pool;
            if (!string.IsNullOrEmpty(op)) json["op"] = op;
            if (amount != 0) json["amount"] = amount;
            return new SubsystemStep(json);
        }

        public static MaxLevelDefinition CreateMaxLevelDefinition(string stat, int baseValue = 0) {
            var json = new JObject { ["stat"] = stat, ["base"] = baseValue };
            return new MaxLevelDefinition(json);
        }

        public static LevelGatingDefinition CreateLevelGatingDefinition(string elementStatistic, string? maxLevelLabel = null) {
            var json = new JObject { ["element_statistic"] = elementStatistic };
            if (maxLevelLabel != null) json["max_level_label"] = maxLevelLabel;
            return new LevelGatingDefinition(json);
        }

        public static StateCapacityDefinition CreateStateCapacityDefinition(IEnumerable<string> states, string stat, string label) {
            var json = new JObject {
                ["states"] = new JArray(states ?? Enumerable.Empty<string>()),
                ["stat"] = stat,
                ["label"] = label
            };
            return new StateCapacityDefinition(json);
        }

        public static SlotGroupDefinition CreateSlotGroupDefinition(
            string name = "",
            string? filter = null,
            string? capacityStat = null,
            IEnumerable<StateCapacityDefinition>? stateCapacities = null,
            int fixedCapacity = 0,
            MaxLevelDefinition? maxLevel = null,
            IEnumerable<Effect>? effects = null,
            IEnumerable<string>? states = null,
            string? defaultState = null) {
            var json = new JObject();
            if (!string.IsNullOrEmpty(name)) json["name"] = name;
            if (!string.IsNullOrEmpty(filter)) json["filter"] = filter;
            if (!string.IsNullOrEmpty(capacityStat)) json["capacity_stat"] = capacityStat;
            if (stateCapacities != null) {
                json["state_capacities"] = new JArray(stateCapacities.Select(sc => sc.ToJson()));
            }
            if (fixedCapacity > 0) json["fixed_capacity"] = fixedCapacity;
            if (maxLevel != null) json["max_level"] = maxLevel.ToJson();
            if (effects != null) {
                json["effects"] = new JArray(effects.Select(e => e.ToJson()));
            }
            if (states != null) {
                json["states"] = new JArray(states);
            }
            if (!string.IsNullOrEmpty(defaultState)) json["default_state"] = defaultState;
            return new SlotGroupDefinition(json);
        }

        public static PoolDefinition CreatePoolDefinition(
            string id,
            string? name = null,
            string? capacityStat = null,
            int defaultCapacity = 0) {
            var json = new JObject(
                new JProperty("id", id),
                new JProperty("name", name ?? id)
            );
            if (!string.IsNullOrEmpty(capacityStat)) json["capacity_stat"] = capacityStat;
            if (defaultCapacity > 0) json["default_capacity"] = defaultCapacity;
            return new PoolDefinition(json);
        }

        public static EffectProjectionRule CreateEffectProjectionRule(string slotGroup, string state, params string[] effects) {
            var json = new JObject(
                new JProperty("slot_group", slotGroup ?? ""),
                new JProperty("state", state ?? ""),
                new JProperty("effects", new JArray(effects ?? Array.Empty<string>()))
            );
            return new EffectProjectionRule(json);
        }

        public static TaxonomyEntry CreateTaxonomyEntry(string name, IDictionary<string, object>? properties = null) {
            var json = new JObject();
            if (properties != null) {
                foreach (var kvp in properties) {
                    json[kvp.Key] = JToken.FromObject(kvp.Value);
                }
            }
            return new TaxonomyEntry(name, json);
        }

        public static SubsystemDefinition CreateSubsystemDefinition(
            string id = "test_subsystem",
            string? name = null,
            string? prerequisiteUnit = null,
            string? prerequisiteProperty = null,
            LevelGatingDefinition? levelGating = null,
            IDictionary<string, SlotGroupDefinition>? slotGroups = null,
            IDictionary<string, List<SubsystemStep>>? verbs = null,
            IDictionary<string, List<SubsystemStep>>? defaultElementVerbs = null,
            IDictionary<string, PoolDefinition>? pools = null,
            IDictionary<string, JObject>? effectTemplates = null,
            IEnumerable<EffectProjectionRule>? projections = null,
            IEnumerable<string>? requiredElementProperties = null,
            IDictionary<string, List<string>>? elementStatistics = null,
            IDictionary<string, IDictionary<string, TaxonomyEntry>>? taxonomies = null,
            IEnumerable<SubsystemElement>? elements = null,
            IEnumerable<SheetAction>? actions = null) {
            var subJson = new JObject(
                new JProperty("id", id),
                new JProperty("name", name ?? id)
            );
            if (!string.IsNullOrEmpty(prerequisiteUnit)) {
                subJson["prerequisite_unit"] = prerequisiteUnit;
            }
            if (!string.IsNullOrEmpty(prerequisiteProperty)) {
                subJson["prerequisite_property"] = prerequisiteProperty;
            }

            var elemStats = elementStatistics != null
                ? new Dictionary<string, List<string>>(elementStatistics, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            if (levelGating != null) {
                subJson["level_gating"] = levelGating.ToJson();
                if (!string.IsNullOrEmpty(levelGating.ElementStatistic) && !elemStats.ContainsKey(levelGating.ElementStatistic)) {
                    elemStats[levelGating.ElementStatistic] = new List<string>();
                }
            }

            if (elemStats.Count > 0) {
                var statsObj = new JObject();
                foreach (var kvp in elemStats) {
                    statsObj[kvp.Key] = new JArray(kvp.Value);
                }
                subJson["element_statistics"] = statsObj;
            }

            if (slotGroups != null && slotGroups.Count > 0) {
                var slotsObj = new JObject();
                foreach (var kvp in slotGroups) slotsObj[kvp.Key] = kvp.Value.ToJson();
                subJson["slot_groups"] = slotsObj;
            }

            if (verbs != null && verbs.Count > 0) {
                var verbsObj = new JObject();
                foreach (var kvp in verbs) verbsObj[kvp.Key] = new JArray(kvp.Value.Select(x => x.ToJson()));
                subJson["verbs"] = verbsObj;
            }

            if (defaultElementVerbs != null && defaultElementVerbs.Count > 0) {
                var defVerbsObj = new JObject();
                foreach (var kvp in defaultElementVerbs) defVerbsObj[kvp.Key] = new JArray(kvp.Value.Select(x => x.ToJson()));
                subJson["default_element_verbs"] = defVerbsObj;
            }

            if (pools != null && pools.Count > 0) {
                var poolsObj = new JObject();
                foreach (var kvp in pools) poolsObj[kvp.Key] = kvp.Value.ToJson();
                subJson["pools"] = poolsObj;
            }

            if (effectTemplates != null && effectTemplates.Count > 0) {
                var templatesObj = new JObject();
                foreach (var kvp in effectTemplates) templatesObj[kvp.Key] = (JObject)kvp.Value.DeepClone();
                subJson["effect_templates"] = templatesObj;
            }

            if (projections != null) {
                subJson["projections"] = new JArray(projections.Select(x => x.ToJson()));
            }

            if (requiredElementProperties != null) {
                subJson["required_element_properties"] = new JArray(requiredElementProperties);
            }

            if (taxonomies != null && taxonomies.Count > 0) {
                var taxObj = new JObject();
                foreach (var taxKvp in taxonomies) {
                    var entriesObj = new JObject();
                    foreach (var entryKvp in taxKvp.Value) {
                        entriesObj[entryKvp.Key] = entryKvp.Value.ToJson();
                    }
                    taxObj[taxKvp.Key] = entriesObj;
                }
                subJson["taxonomies"] = taxObj;
            }

            if (actions != null) {
                subJson["actions"] = new JArray(actions.Select(x => x.ToJson()));
            }

            var root = new JObject(new JProperty("subsystem", subJson));
            if (elements != null) {
                root["elements"] = new JArray(elements.Select(x => x.ToJson()));
            }
            return new SubsystemDefinition(root);
        }
    }
}
