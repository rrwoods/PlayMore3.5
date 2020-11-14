using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using PlayMore35.Items;

using static PlayMore35.Effects.Constants;

namespace PlayMore35.Effects {
    public class EffectPool : IJsonable {
        private readonly Dictionary<string, Statistic> stats = new Dictionary<string, Statistic>();
        private readonly List<Effect> effects = new List<Effect>();

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

            foreach (Effect e in effects.Where(x => x.Toggle.On(this, items))) {
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
        public void GenerateCharacterEffects(Character.CharacterChoices choices) {
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
                // This might seem roundabout, compared to the alternative, which is to
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
            // feat effects go here!
        }
    }
}
