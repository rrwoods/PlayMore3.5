using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Global;
using PlayMore35.Items;

using static PlayMore35.Effects.Constants;

namespace PlayMore35.Effects {
    public class EffectPool : IJsonable {
        // { slot description => level }
        // e.g. "Level 6" => 6, or maybe "Flaw 1" => 1 and "Flaw 1 (Feat)" => 1
        // (you'd implement the flaws themselves as feats)
        public readonly Dictionary<string, int> featSlots = new Dictionary<string, int>();

        public readonly Dictionary<string, Statistic> stats = new Dictionary<string, Statistic>();
        public readonly Dictionary<string, ControllableFlag> controllableFlags = new Dictionary<string, ControllableFlag>();
        public readonly Dictionary<string, ControllableParameter> controllableParameters = new Dictionary<string, ControllableParameter>();
        public readonly Dictionary<string, List<ControllableParameter>> resettableParameters;
        private readonly List<Effect> effects = new List<Effect>();
        private readonly Character character;
        private readonly Effect baseUses = new Effect("base uses");

        public string CreatureType { get; private set; } = "";
        public List<string> SubTypes { get; } = new List<string>();
        private readonly List<GrantedClassSkill> grantedClassSkills = new List<GrantedClassSkill>();

        public JToken ToJson() {
            return new JObject(
                new JProperty("feats", new JObject(featSlots.Select(x => new JProperty(x.Key, x.Value)))),
                new JProperty("stats", new JArray(stats.Values.Select(x => x.ToJson()))),
                new JProperty("flags", new JArray(controllableFlags.Values.Select(x => x.ToJson()))),
                new JProperty("parameters", new JArray(controllableParameters.Values.Select(x => x.ToJson()))),
                new JProperty("effects", new JArray(effects.Select(x => x.ToJson())))
            );
        }

        public void ReadJson(JToken json) {
            if (json["feats"] is JObject featsObj) {
                foreach (JProperty featSlot in featsObj.Properties()) {
                    featSlots[featSlot.Name] = (int)featSlot.Value;
                }
            }
            if (json["stats"] is JArray statsArr) {
                foreach (JObject token in statsArr) {
                    Statistic s = new Statistic(token);
                    stats[s.Name] = s;
                }
            }
            if (json["effects"] is JArray effectsArr) {
                foreach (JObject token in effectsArr) {
                    AddEffect(new Effect(token));
                }
            }
            if (json["flags"] is JArray flagsArr) {
                foreach (JObject token in flagsArr) {
                    AddFlag(new ControllableFlag(token, character));
                }
            }
            if (json["parameters"] is JArray paramsArr) {
                foreach (JObject token in paramsArr) {
                    ControllableParameter parameter = new ControllableParameter(token, character);
                    controllableParameters[parameter.Name] = parameter;
                }
            }
        }

        public EffectPool(Character character) {
            this.character = character ?? throw new ArgumentNullException(nameof(character));
            resettableParameters = LengthOfTime.UNIT_ROUNDS.Keys.ToDictionary(k => k, k => new List<ControllableParameter>());
            resettableParameters["encounter"] = new List<ControllableParameter>();
            effects.Add(baseUses);
        }

        public void AddStat(Statistic stat) {
            if (stats.ContainsKey(stat.Name)) {
                stats[stat.Name].EnsureTags(stat);
            } else {
                stats[stat.Name] = stat;
            }
        }

        public void AddStats(IEnumerable<Statistic> stats) {
            foreach (Statistic stat in stats) {
                AddStat(stat);
            }
        }

        public Statistic GetStat(string name) {
            return stats[name];
        }

        public StatView GetStatistic(string? name, Equipped items) {
            if (name != null && stats.TryGetValue(name, out var stat)) {
                var (val, expl) = stat.GetValue(this, items ?? new Equipped());
                return new StatView(name, val, expl.AsReadOnly());
            }
            return new StatView(name ?? "", 0);
        }

        public int GetStatValue(string? name, Equipped items, int defaultValue = 0) {
            if (name != null && stats.TryGetValue(name, out var stat)) {
                return stat.GetValue(this, items ?? new Equipped()).value;
            }
            return defaultValue;
        }

        public IEnumerable<Statistic> RelevantStatsWithTag(string tag, Equipped items) {
            return stats.Values.Where(x => x.HasTag(tag) && x.Relevant(this, items));
        }

        private void RegisterEffectParameter(Effect effect) {
            if (!string.IsNullOrEmpty(effect.ParameterName)) {
                string paramName = effect.ParameterName;
                if (!controllableParameters.ContainsKey(paramName)) {
                    controllableParameters[paramName] = new ControllableParameter(paramName, character) {
                        Category = "",
                        Description = "",
                    };
                }
            }
        }

        private bool RegisterResourceUses(ActionResource? resource, string name) {
            string statName = $"{name} uses";
            if (stats.ContainsKey(statName)) {
                return true;
            }
            if (resource == null || resource.RechargeTime.unit == AT_WILL) {
                return false;
            }
            
            if (resource.StaticUses != 0) {
                baseUses.AddModifier(statName, new Modifier(resource.StaticUses, ModifierTypes.BASE));
            }
            AddStat(new Statistic(statName));

            string usedName = $"{name} used";
            ControllableParameter usedParam = new ControllableParameter(usedName, character) {
                Category = "",
                Description = "",
            };
            controllableParameters[usedName] = usedParam;
            if (resource.RechargeTime.unit != "ever" && resource.RechargeTime.quantity.IsSingleOne()) {
                resettableParameters[resource.RechargeTime.unit].Add(usedParam);
            }
            return true;
        }

        public void AddEffect(Effect effect) {
            if (effect == null) return;
            effects.Add(effect);
            RegisterEffectParameter(effect);
            foreach (Text text in effect.Text) {
                if (RegisterResourceUses(text.Resource, effect.Name)) {
                    break;
                }
            }
        }

        public void RemoveEffect(string effect) {
            var removedParams = effects
                .Where(x => x.Name == effect && !string.IsNullOrEmpty(x.ParameterName))
                .Select(x => x.ParameterName)
                .Distinct()
                .ToList();
            effects.RemoveAll(x => x.Name == effect);
            foreach (string paramName in removedParams) {
                if (!effects.Any(x => x.ParameterName == paramName)) {
                    controllableParameters.Remove(paramName);
                }
            }
        }

        public void ReplaceEffect(Effect effect) {
            RemoveEffect(effect.Name);
            AddEffect(effect);
        }

        public void AddFlag(ControllableFlag flag) {
            if (flag == null) return;
            controllableFlags[flag.Name] = flag;
            RegisterResourceUses(flag.Resource, flag.Name);
        }

        public void AddFlag(FlagDefinition def) {
            if (def == null) return;
            AddFlag(def as ControllableFlag ?? new ControllableFlag(def, character));
        }

        public IEnumerable<ExtraAttack> GetExtraAttacks(Equipped items, Attack affecting) {
            return effects.SelectMany(x => x.GetExtraAttacks(this, items, affecting));
        }

        public IEnumerable<Modifier> GetAttackModifiers(Equipped items, Attack affecting) {
            return effects.SelectMany(x => x.GetAttackModifiers(this, items, affecting));
        }

        public IEnumerable<Modifier> GetDamageModifiers(Equipped items, Attack affecting) {
            return effects.SelectMany(x => x.GetDamageModifiers(this, items, affecting));
        }

        public IEnumerable<Modifier> GetModifiers(Equipped items, Statistic affecting) {
            return effects.SelectMany(x => x.GetModifiers(this, items, affecting));
        }

        public IEnumerable<Multiplier> GetMultipliers(Equipped items, Statistic affecting) {
            return effects.SelectMany(x => x.GetMultipliers(this, items, affecting));
        }

        public IEnumerable<DiceGroup> GetExtraDice(Equipped items, Attack affecting) {
            return effects.SelectMany(x => x.GetExtraDice(this, items, affecting));
        }

        public IEnumerable<Modifier> GetThreatModifiers(Equipped items, Attack affecting) {
            return effects.SelectMany(x => x.GetThreatModifiers(this, items, affecting));
        }

        public IEnumerable<Multiplier> GetRangeMultipliers(Equipped items, Attack affecting) {
            return effects.SelectMany(x => x.GetRangeMultipliers(this, items, affecting));
        }

        public bool Familiar(Equipped items, string weapon) {
            return effects
                .SelectMany(x => x.WeaponFamiliarity)
                .Any(x => x == weapon);
        }

        public bool Proficient(Equipped items, Attack attack) {
            return effects.Select(x => x.Proficient(this, items, attack)).Contains(true);
        }

        public bool ProficientWithArmor(Equipped items) {
            return effects.Select(x => x.ProficientWithArmor(this, items)).Contains(true);
        }

        public bool ProficientWithShield(Equipped items) {
            return effects.Select(x => x.ProficientWithShield(this, items)).Contains(true);
        }

        public bool HasFlag(string flag, Equipped items) {
            if (controllableFlags.ContainsKey(flag) && controllableFlags[flag].On) {
                return true;
            }

            return effects
                .Any(x => x.HasFlag(flag) && x.Toggle.On(this, items));
        }

        public Dictionary<string, List<Text>> GetTextEffects(Equipped items) {
            var allText = TEXT_CATEGORIES.ToDictionary(x => x, x => new List<Text>());

            foreach (Effect e in effects.Where(x => x.Toggle.On(this, items))) {
                foreach (Text text in e.Text) {
                    Text filled = text.FillStatValues(this, items, e.Name);
                    if (!string.IsNullOrEmpty(e.ParameterName)) {
                        filled.ParameterName = e.ParameterName;
                        filled.ParameterLabel = e.Parameter;
                    }
                    if (e.associatedAction.Any()) {
                        filled.associatedAction = new List<SheetAction>(e.associatedAction);
                    }
                    string category = allText.ContainsKey(text.Category) ? text.Category : UNCATEGORIZED;
                    allText[category].Add(filled);
                }
            }

            foreach (ControllableFlag f in controllableFlags.Values) {
                f.Resource?.ComputeUses(f.Name, this, items);
            }

            return allText;
        }

        public void AssignAbilityScores(Dictionary<string, int> scores) {
            foreach (var ability in scores) {
                string name = ability.Key;
                int score = ability.Value;
                ReplaceEffect(new Effect($"Assigned {name}")
                    .AddModifier(name + " score", new Modifier(score, ModifierTypes.BASE))
                );
            }
        }

        // Set up effects inherent to the character only.  Effects from items regenerated moment to moment.
        public void GenerateCharacterEffects() {
            AssignAbilityScores(character.assignedAbilityScores);
            foreach (var ability in character.abilityScoreIncrements) {
                AddEffect(new Effect("Level")
                    .AddModifier(ability.Value + " score", new Modifier(+1, ModifierTypes.ABILITY_MODIFIER))
                );
            }
            int numHd = character.rolledHitDice.Count();
            AddEffect(new Effect("Hit dice")
                .AddModifier(TOTAL_HD, new Modifier(numHd, ModifierTypes.BASE))
            );
            for (int i = 0; i < numHd; i++) {
                // This might seem roundabout compared to the alternative, which is to
                // sum all the rolled hit dice and add HD*CON.  However, *each* individual
                // hit die is minimum 1.
                string hdName = $"Level {i+1} hit die";
                AddStat(new Statistic(hdName, stats[CON_MODIFIER]).Minimum(1));
                AddEffect(new Effect(hdName)
                    .AddModifier(hdName, new Modifier(character.rolledHitDice[i], ModifierTypes.BASE))
                );
                AddEffect(new Effect(hdName)
                    .AddModifier(MAX_HP, new Modifier(hdName, ModifierTypes.BASE))
                );
            }
            foreach (var skill in character.skillRanks) {
                AddEffect(new Effect(skill.Key + RANKS)
                    .AddModifier(skill.Key + RANKS, new Modifier(skill.Value.Sum(), ModifierTypes.BASE))
                );
            }

            foreach (string language in character.bonusLanguages) {
                AddEffect(new Effect("Bonus language").AddText(LANGUAGES, language));
            }
        }

        public void GenerateExternalEffect(ExternalEffect effect) {
            effect.Flags.ForEach(x => AddFlag(x));
            effect.Params.ForEach(x => controllableParameters[x.Name] = new ControllableParameter(x, character));
            AddEffect(effect.Effect);
        }

        public void GenerateRaceEffects(Race race) {
            CreatureType = race.CreatureType;
            SubTypes.AddRange(race.SubTypes);
            Effect effect = new Effect(race.Name + " racial traits")
                .AddModifier(SIZE_CATEGORY, new Modifier(race.SizeCategory, ModifierTypes.BASE))
                .AddModifier(BASE_SPEED, new Modifier(race.BaseSpeed, ModifierTypes.BASE))
                .AddModifier(STR_SCORE, new Modifier(race.Str, ModifierTypes.RACIAL))
                .AddModifier(DEX_SCORE, new Modifier(race.Dex, ModifierTypes.RACIAL))
                .AddModifier(CON_SCORE, new Modifier(race.Con, ModifierTypes.RACIAL))
                .AddModifier(INT_SCORE, new Modifier(race.Int, ModifierTypes.RACIAL))
                .AddModifier(WIS_SCORE, new Modifier(race.Wis, ModifierTypes.RACIAL))
                .AddModifier(CHA_SCORE, new Modifier(race.Cha, ModifierTypes.RACIAL));
            foreach (string language in race.AutomaticLanguages) {
                effect.AddText(LANGUAGES, language);
            }
            foreach (string weapon in race.FamiliarWeapons) {
                effect.AddFamiliarity(weapon);
            }
            AddEffect(effect);
            race.Effects.ForEach(x => AddEffect(x));
            race.Flags.ForEach(x => AddFlag(x));
            AddStats(race.NewStats);
        }

        private int BaseSaveBonus(CharacterClass.BonusProgression prog, int level) {
            switch (prog) {
                case CharacterClass.BonusProgression.FAST:
                    return (int)(level/2.0 + 2);
                case CharacterClass.BonusProgression.SLOW:
                    return (int)(level/3.0 + 0.01); // (int) rounds down, so +0.34 properly accounts for rounding error
                default:
                    return 0;
            }
        }

        // Base attack and saves, proficiencies, and subsystem-related features.
        // Not other class features.
        public void GenerateClassEffects(CharacterClass @class, int level) {
            AddStat(new Statistic($"{@class.Name} level"));
            AddEffect(new Effect($"{@class.Name} level")
                .AddModifier($"{@class.Name} level", new Modifier(level, ModifierTypes.BASE))
            );
            AddStat(new Statistic(CHARACTER_LEVEL));
            AddEffect(new Effect($"{@class.Name} character level contribution")
                .AddModifier(CHARACTER_LEVEL, new Modifier(level, ModifierTypes.UNTYPED))
            );
            int baseAttackBonus = 0;
            switch (@class.BaseAttackBonus) {
                case CharacterClass.BonusProgression.NONE:
                    baseAttackBonus = 0;
                    break;
                case CharacterClass.BonusProgression.SLOW:
                    baseAttackBonus = level / 2;
                    break;
                case CharacterClass.BonusProgression.MEDIUM:
                    baseAttackBonus = (int)(level * 0.75);
                    break;
                case CharacterClass.BonusProgression.FAST:
                    baseAttackBonus = level;
                    break;
            }
            AddEffect(new Effect($"{@class.Name} base attack bonus")
                .AddModifier(BASE_ATTACK_BONUS, new Modifier(baseAttackBonus, ModifierTypes.BASE))
            );

            AddEffect(new Effect($"{@class.Name} base Fort save")
                .AddModifier(FORT_SAVES, new Modifier(BaseSaveBonus(@class.BaseFortSave, level), ModifierTypes.BASE))
            );
            AddEffect(new Effect($"{@class.Name} base Ref save")
                .AddModifier(REF_SAVES, new Modifier(BaseSaveBonus(@class.BaseRefSave, level), ModifierTypes.BASE))
            );
            AddEffect(new Effect($"{@class.Name} base Will save")
                .AddModifier(WILL_SAVES, new Modifier(BaseSaveBonus(@class.BaseWillSave, level), ModifierTypes.BASE))
            );

            Effect proficiencies = new Effect($"{@class.Name} proficiencies");
            foreach (AttackMatcher proficiency in @class.WeaponProficiencies) {
                proficiencies.AddProficiency(proficiency);
            }
            foreach (Armor.WeightCategory proficiency in @class.ArmorProficiencies) {
                proficiencies.AddProficiency(proficiency);
            }
            foreach (bool proficiency in @class.ShieldProficiencies) {
                proficiencies.AddShieldProficiency(proficiency);
            }
            foreach (string proficiency in @class.ExoticShieldProficiencies) {
                proficiencies.AddExoticShieldProficiency(proficiency);
            }
            AddEffect(proficiencies);

            foreach (var feature in @class.Subsystems.Values) {
                AddStats(feature.Stats.Select(x => new Statistic(x)));
                foreach (Effect e in feature.Effects) {
                    AddEffect(e);
                }
            }
        }

        public void GenerateFeatEffects(Feat feat, IEnumerable<string> choices) {
            Effect? benefit = feat.Benefit;
            if (benefit != null && choices != null) {
                foreach (var choice in choices.Where(c => !string.IsNullOrEmpty(c))) {
                    benefit = benefit.ApplyChoice(choice);
                }
            }
            if (benefit != null) {
                AddEffect(benefit);
            }
            feat.Flags.ForEach(x => AddFlag(x));
            feat.Parameters.ForEach(x => controllableParameters[x.Name] = new ControllableParameter(x, character));
            AddStats(feat.NewStats);
        }

        public void GenerateFeatEffects(Feat feat, string choice) {
            GenerateFeatEffects(feat, string.IsNullOrEmpty(choice) ? Enumerable.Empty<string>() : new[] { choice });
        }

        public void GrantClassSkill(string skill, int minimumLevel) {
            if (!string.IsNullOrWhiteSpace(skill)) {
                grantedClassSkills.Add(new GrantedClassSkill(skill, minimumLevel));
            }
        }

        public bool HasGrantedClassSkill(string skill, int levelIndex) {
            if (string.IsNullOrWhiteSpace(skill) || levelIndex < 0) return false;
            return grantedClassSkills.Any(g =>
                string.Equals(g.Skill, skill, StringComparison.OrdinalIgnoreCase) &&
                levelIndex >= g.MinimumLevel - 1);
        }
    }

    public class GrantedClassSkill {
        public string Skill { get; }
        public int MinimumLevel { get; }

        public GrantedClassSkill(string skill, int minimumLevel) {
            Skill = skill ?? "";
            MinimumLevel = minimumLevel;
        }
    }

    public static class StringExtensions {
        private static List<string> statAlerts = new List<string>();
        public static string FillStatValues(this string description, EffectPool pool, Equipped items) {
            string newDescription = description;
            while (newDescription.Contains("{{")) {
                int start = newDescription.IndexOf("{{") + 2;
                int end = newDescription.IndexOf("}}", start);
                string statName = newDescription.Substring(start, end - start);
                try { 
                    int statValue = pool.GetStat(statName).GetValue(pool, items).value;
                    newDescription = newDescription.Replace("{{" + statName + "}}", "" + statValue);
                } catch (KeyNotFoundException) {
                    if (!statAlerts.Contains(statName)) {
                        statAlerts.Add(statName);
                        // TODO : don't messagebox here, use some sort of error-registration system i guess?
                        MessageBox.Show($"Couldn't find stat '{statName}' during processing of effect");
                    }
                    return newDescription;
                }
            }
            return newDescription;
        }
    }
}
