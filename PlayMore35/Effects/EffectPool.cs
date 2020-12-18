using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Items;

using static PlayMore35.Effects.Constants;

namespace PlayMore35.Effects {
    public class EffectPool : IJsonable {
        public readonly Dictionary<string, Statistic> stats = new Dictionary<string, Statistic>();
        private readonly List<Effect> effects = new List<Effect>();

        public string CreatureType { get; private set; } = "";
        public List<string> SubTypes { get; private set; } = new List<string>();

        public JToken ToJson() {
            return new JObject(
                new JProperty("stats", new JArray(stats.Values.Select(x => x.ToJson()))),
                new JProperty("effects", new JArray(effects.Select(x => x.ToJson())))
            );
        }

        public void ReadJson(JToken json) {
            foreach(JObject token in json["stats"]) {
                Statistic s = new Statistic(token);
                stats[s.Name] = s;
            }
            foreach(JObject token in json["effects"]) {
                effects.Add(new Effect(token));
            }
        }

        public void AddStat(Statistic stat) {
            stats[stat.Name] = stat;
        }

        public Statistic GetStat(string name) {
            return stats[name];
        }

        public void AddEffect(Effect effect) {
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

        public bool Proficient(Equipped items, Attack attack) {
            return AllEffects(items).Select(x => x.Proficient(this, items, attack)).Contains(true);
        }

        public bool ProficientWithArmor(Equipped items) {
            return AllEffects(items).Select(x => x.ProficienctWithArmor(this, items)).Contains(true);
        }

        public bool ProficientWithShield(Equipped items) {
            return AllEffects(items).Select(x => x.ProficienctWithShield(this, items)).Contains(true);
        }

        public bool HasFlag(string flag, Equipped items) {
            return effects
                .Any(x => x.HasFlag(flag) && x.Toggle.On(this, items));
        }

        public Dictionary<string, List<string>> GetTextEffects(Equipped items) {
            var allText = TEXT_CATEGORIES.ToDictionary(x => x, x => new List<string>());

            foreach (Effect e in AllEffects(items).Where(x => x.Toggle.On(this, items))) {
                foreach ((string category, string text) in e.Text) {
                    if (allText.ContainsKey(category)) {
                        allText[category].Add(text);
                    } else {
                        allText[UNCATEGORIZED].Add("[" + category + "] " + text);
                    }
                }
            }

            return allText;
        }

        // Set up effects inherent to the character only.  Effects from items regenerated moment to moment.
        public void GenerateCharacterEffects(Character choices) {
            foreach (var ability in choices.assignedAbilityScores) {
                string name = ability.Key;
                int score = ability.Value;
                AddEffect(new Effect("Assigned " + name)
                    .AddModifier(name + " score", new Modifier(score, BASE))
                );
            }
            foreach (string ability in choices.abilityScoreIncrements) {
                AddEffect(new Effect("Level")
                    .AddModifier(ability + " score", new Modifier(+1, ABILITY_MODIFIER))
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
                string hdName = "Level " + (i + 1) + " hit die";
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
            AddEffect(effect);
            effects.AddRange(race.Effects);
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

        // NOT class features.  Just base attack and saves, and proficiencies.
        public void GenerateClassEffects(CharacterClass @class, int level) {
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
            AddEffect(new Effect(@class.Name + " base attack bonus")
                .AddModifier(BASE_ATTACK_BONUS, new Modifier(baseAttackBonus, BASE))
            );

            AddEffect(new Effect(@class.Name + " base Fort save")
                .AddModifier(FORT_SAVES, new Modifier(BaseSaveBonus(@class.BaseFortSave, level), BASE))
            );
            AddEffect(new Effect(@class.Name + " base Ref save")
                .AddModifier(REF_SAVES, new Modifier(BaseSaveBonus(@class.BaseRefSave, level), BASE))
            );
            AddEffect(new Effect(@class.Name + " base Will save")
                .AddModifier(WILL_SAVES, new Modifier(BaseSaveBonus(@class.BaseWillSave, level), BASE))
            );

            Effect proficiencies = new Effect(@class.Name + " proficiencies");
            foreach (AttackMatcher proficiency in @class.WeaponProficiencies) {
                proficiencies.AddProficiency(proficiency);
            }
            foreach (Armor.WeightCategory proficiency in @class.ArmorProficiencies) {
                proficiencies.AddProficiency(proficiency);
            }
            foreach (bool proficiency in @class.ShieldProficiencies) {
                proficiencies.AddShieldProficiency(proficiency);
            }
            AddEffect(proficiencies);
        }
    }
}
