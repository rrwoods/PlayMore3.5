using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
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
        private readonly List<Effect> effects = new List<Effect>();

        public string CreatureType { get; private set; } = "";
        public List<string> SubTypes { get; private set; } = new List<string>();

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
            foreach (JProperty featSlot in json["feats"]) {
                featSlots[featSlot.Name] = (int)featSlot.Value;
            }
            foreach (JObject token in json["stats"]) {
                Statistic s = new Statistic(token);
                stats[s.Name] = s;
            }
            foreach (JObject token in json["effects"]) {
                AddEffect(new Effect(token));
            }
            foreach (JObject token in json["flags"]) {
                ControllableFlag flag = new ControllableFlag(token);
                controllableFlags[flag.Name] = flag;
            }
            foreach (JObject token in json["parameters"]) {
                ControllableParameter parameter = new ControllableParameter(token);
                controllableParameters[parameter.Name] = parameter;
            }
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

        public IEnumerable<Statistic> RelevantStatsWithTag(string tag, Equipped items) {
            return stats.Values.Where(x => x.HasTag(tag) && x.Relevant(this, items));
        }

        public void AddEffect(Effect effect) {
            effects.Add(effect);
        }

        public void RemoveEffect(string effect) {
            effects.RemoveAll(x => x.Name == effect);
        }

        public void ReplaceEffect(Effect effect) {
            RemoveEffect(effect.Name);
            effects.Add(effect);
        }

        private IEnumerable<Effect> AllEffects(Equipped items) {
            return effects.Concat(items.GetGlobalEffects());
        }

        public IEnumerable<ExtraAttack> GetExtraAttacks(Equipped items, Attack affecting) {
            return AllEffects(items).SelectMany(x => x.GetExtraAttacks(this, items, affecting));
        }

        public IEnumerable<Modifier> GetAttackModifiers(Equipped items, Attack affecting) {
            return AllEffects(items).SelectMany(x => x.GetAttackModifiers(this, items, affecting));
        }

        public IEnumerable<Modifier> GetDamageModifiers(Equipped items, Attack affecting) {
            return AllEffects(items).SelectMany(x => x.GetDamageModifiers(this, items, affecting));
        }

        public IEnumerable<Modifier> GetModifiers(Equipped items, Statistic affecting) {
            return AllEffects(items).SelectMany(x => x.GetModifiers(this, items, affecting));
        }

        public IEnumerable<Multiplier> GetMultipliers(Equipped items, Statistic affecting) {
            return AllEffects(items).SelectMany(x => x.GetMultipliers(this, items, affecting));
        }

        public IEnumerable<DiceGroup> GetExtraDice(Equipped items, Attack affecting) {
            return AllEffects(items).SelectMany(x => x.GetExtraDice(this, items, affecting));
        }

        public IEnumerable<Modifier> GetThreatModifiers(Equipped items, Attack affecting) {
            return AllEffects(items).SelectMany(x => x.GetThreatModifiers(this, items, affecting));
        }

        public IEnumerable<Multiplier> GetRangeMultipliers(Equipped items, Attack affecting) {
            return AllEffects(items).SelectMany(x => x.GetRangeMultipliers(this, items, affecting));
        }

        public bool Familiar(Equipped items, string weapon) {
            return AllEffects(items)
                .SelectMany(x => x.WeaponFamiliarity)
                .Any(x => x == weapon);
        }

        public bool Proficient(Equipped items, Attack attack) {
            return AllEffects(items).Select(x => x.Proficient(this, items, attack)).Contains(true);
        }

        public bool ProficientWithArmor(Equipped items) {
            return AllEffects(items).Select(x => x.ProficientWithArmor(this, items)).Contains(true);
        }

        public bool ProficientWithShield(Equipped items) {
            return AllEffects(items).Select(x => x.ProficientWithShield(this, items)).Contains(true);
        }

        public bool HasFlag(string flag, Equipped items) {
            if (controllableFlags.ContainsKey(flag) && controllableFlags[flag].On) {
                return true;
            }

            return effects
                .Any(x => x.HasFlag(flag) && x.Toggle.On(this, items));
        }

        // Returns { category => [ (name, description) ] }
        public Dictionary<string, List<Text>> GetTextEffects(Equipped items) {
            var allText = TEXT_CATEGORIES.ToDictionary(x => x, x => new List<Text>());

            foreach (Effect e in AllEffects(items).Where(x => x.Toggle.On(this, items))) {
                foreach (Text text in e.Text) {
                    Text filled = text.FillStatValues(this, items, e.Name);
                    string category = allText.ContainsKey(text.Category) ? text.Category : UNCATEGORIZED;
                    allText[category].Add(filled);
                }
            }

            return allText;
        }

        // usage:
        // foreach (var grouping in FlagsByCategory()) {
        //     string flagName = grouping.Key;
        //     foreach (ControllableFlag flag in grouping) {
        //         // ...
        //     }
        // }
        public ILookup<string, ControllableFlag> FlagsByCategory() {
            return controllableFlags.Values.ToLookup(x => x.Category);
        }

        public void AssignAbilityScores(Dictionary<string, int> scores) {
            foreach (var ability in scores) {
                string name = ability.Key;
                int score = ability.Value;
                ReplaceEffect(new Effect($"Assigned {name}")
                    .AddModifier(name + " score", new Modifier(score, BASE))
                );
            }
        }

        // Set up effects inherent to the character only.  Effects from items regenerated moment to moment.
        public void GenerateCharacterEffects(Character choices) {
            AssignAbilityScores(choices.assignedAbilityScores);
            foreach (var ability in choices.abilityScoreIncrements) {
                AddEffect(new Effect("Level")
                    .AddModifier(ability.Value + " score", new Modifier(+1, ABILITY_MODIFIER))
                );
            }
            int numHd = choices.rolledHitDice.Count();
            AddEffect(new Effect("Hit dice")
                .AddModifier(TOTAL_HD, new Modifier(numHd, BASE))
            );
            for (int i = 0; i < numHd; i++) {
                // This might seem roundabout compared to the alternative, which is to
                // sum all the rolled hit dice and add HD*CON.  However, *each* individual
                // hit die is minimum 1.
                string hdName = $"Level {i+1} hit die";
                AddStat(new Statistic(hdName, stats[CON_MODIFIER]).Minimum(1));
                AddEffect(new Effect(hdName)
                    .AddModifier(hdName, new Modifier(choices.rolledHitDice[i], BASE))
                );
                AddEffect(new Effect(hdName)
                    .AddModifier(MAX_HP, new Modifier(hdName, BASE))
                );
            }
            foreach (var skill in choices.skillRanks) {
                AddEffect(new Effect(skill.Key + RANKS)
                    .AddModifier(skill.Key + RANKS, new Modifier(skill.Value.Sum(), BASE))
                );
            }

            foreach (string language in choices.bonusLanguages) {
                AddEffect(new Effect("Bonus language").AddText(LANGUAGES, language));
            }
        }

        public void GenerateExternalEffect(ExternalEffect effect) {
            effect.Flags.ForEach(x => controllableFlags[x.Name] = x);
            effect.Params.ForEach(x => controllableParameters[x.Name] = x);
            AddEffect(effect.Effect);
        }

        public void GenerateRaceEffects(Race race) {
            CreatureType = race.CreatureType;
            SubTypes.AddRange(race.SubTypes);
            Effect effect = new Effect(race.Name + " racial traits")
                .AddModifier(SIZE_CATEGORY, new Modifier(race.SizeCategory, BASE))
                .AddModifier(BASE_SPEED, new Modifier(race.BaseSpeed, BASE))
                .AddModifier(STR_SCORE, new Modifier(race.Str, RACIAL))
                .AddModifier(DEX_SCORE, new Modifier(race.Dex, RACIAL))
                .AddModifier(CON_SCORE, new Modifier(race.Con, RACIAL))
                .AddModifier(INT_SCORE, new Modifier(race.Int, RACIAL))
                .AddModifier(WIS_SCORE, new Modifier(race.Wis, RACIAL))
                .AddModifier(CHA_SCORE, new Modifier(race.Cha, RACIAL));
            foreach (string language in race.AutomaticLanguages) {
                effect.AddText(LANGUAGES, language);
            }
            foreach (string weapon in race.FamiliarWeapons) {
                effect.AddFamiliarity(weapon);
            }
            AddEffect(effect);
            race.Effects.ForEach(x => AddEffect(x));
            race.Flags.ForEach(x => controllableFlags[x.Name] = x);
            AddStats(race.NewStats);
        }

        public void GenerateUses(Equipped items) {
            foreach (Effect e in AllEffects(items)) {
                Statistic stat = e.Uses();
                if (stat != null) {
                    AddStat(stat);
                    controllableParameters[$"{e.Name} used"] = new ControllableParameter {
                        Name = $"{e.Name} used",
                        Category = "",
                        Description = "",
                        Value = 0
                    };
                }
            }
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
                .AddModifier($"{@class.Name} level", new Modifier(level, BASE))
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
                .AddModifier(BASE_ATTACK_BONUS, new Modifier(baseAttackBonus, BASE))
            );

            AddEffect(new Effect($"{@class.Name} base Fort save")
                .AddModifier(FORT_SAVES, new Modifier(BaseSaveBonus(@class.BaseFortSave, level), BASE))
            );
            AddEffect(new Effect($"{@class.Name} base Ref save")
                .AddModifier(REF_SAVES, new Modifier(BaseSaveBonus(@class.BaseRefSave, level), BASE))
            );
            AddEffect(new Effect($"{@class.Name} base Will save")
                .AddModifier(WILL_SAVES, new Modifier(BaseSaveBonus(@class.BaseWillSave, level), BASE))
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

            if (@class.Vestiges != null) {
                AddStats(@class.Vestiges?.stats.Select(x => new Statistic(x)));
                foreach (Effect e in @class.Vestiges?.effects) {
                    AddEffect(e);
                }
            }
        }

        public void GenerateFeatEffects(Feat feat, string choice) {
            Effect benefit = feat.Benefit;
            if (choice.Any()) {
                benefit = benefit.ApplyChoice(choice);
            }
            AddEffect(benefit);
            feat.Flags.ForEach(x => controllableFlags[x.Name] = x);
            feat.Parameters.ForEach(x => controllableParameters[x.Name] = x);
            AddStats(feat.NewStats);
        }

        public void GenerateVestigeEffects(Vestige vestige, Character.PactState state) {
            if (state == Character.PactState.POOR_PACT) {
                AddEffect(vestige.Influence);
            }
            if (state == Character.PactState.DEFIED) {
                AddEffect(new Effect($"Defied {vestige.Name}")
                    .AddAttackModifier(AttackMatcher.ALL, new Modifier(-1, UNTYPED))
                    .AddModifier(ALL_SAVES, new Modifier(-1, UNTYPED))
                    .AddModifier(ABILITY_CHECKS, new Modifier(-1, UNTYPED))
                    .AddModifier(ALL_SKILLS, new Modifier(-1, UNTYPED))
                );
            }
            foreach (Effect ability in vestige.Abilities) {
                AddEffect(ability);
            }
        }
    }
}
