using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayMore35.Effects;

namespace PlayMore35.GameRules {
    class GameRules {
        public EffectPool Rules { get; private set; } = new EffectPool();

        public List<Statistic> Stats { get; private set; } = new List<Statistic>();

        // TODO : Non-default constructor(s) to load houserules or custom rules from files.
        // Or, should we always load the official rules, then modify those rules with houserules?

        private readonly Dictionary<string, Statistic> abilityModifiers = new Dictionary<string, Statistic>();

        // The default constructor sets up 3.5's official rules.
        public GameRules() {
            AbilityScore("STR");
            Stats.Add(new Statistic("Initiative", AbilityScore("DEX")));
            AbilityScore("CON");
            AbilityScore("INT");
            AbilityScore("WIS");
            AbilityScore("CHA");

            SkillModifier("Appraise", "INT");
            SkillModifier("Balance", "DEX");
            SkillModifier("Bluff", "CHA");
            SkillModifier("Climb", "STR");
            SkillModifier("Concentration", "CON");
            SkillModifier("Craft", "INT"); // TODO : need subskills
            SkillModifier("Decipher Script", "INT");
            SkillModifier("Diplomacy", "CHA");
            SkillModifier("Disable Device", "INT");
            SkillModifier("Disguise", "CHA");
            SkillModifier("Escape Artist", "DEX");
            SkillModifier("Forgery", "INT");
            SkillModifier("Gather Information", "CHA");
            SkillModifier("Handle Animal", "CHA");
            SkillModifier("Heal", "WIS");
            SkillModifier("Hide", "DEX");
            SkillModifier("Intimidate", "CHA");
            SkillModifier("Jump", "STR");
            SkillModifier("Knowledge (arcana)", "INT");
            SkillModifier("Knowledge (architecture and engineering)", "INT");
            SkillModifier("Knowledge (dungeoneering)", "INT");
            SkillModifier("Knowledge (geography)", "INT");
            SkillModifier("Knowledge (history)", "INT");
            SkillModifier("Knowledge (local)", "INT");
            SkillModifier("Knowledge (nature)", "INT");
            SkillModifier("Knowledge (nobility and royalty)", "INT");
            SkillModifier("Knowledge (religion)", "INT");
            SkillModifier("Knowledge (the planes)", "INT");
            SkillModifier("Listen", "WIS");
            SkillModifier("Move Silently", "DEX");
            SkillModifier("Open Lock", "DEX");
            SkillModifier("Perform (act)", "CHA");
            SkillModifier("Perform (comedy)", "CHA");
            SkillModifier("Perform (dance)", "CHA");
            SkillModifier("Perform (keyboard instruments)", "CHA");
            SkillModifier("Perform (oratory)", "CHA");
            SkillModifier("Perform (percussion instruments)", "CHA");
            SkillModifier("Perform (string instruments)", "CHA");
            SkillModifier("Perform (wind instruments)", "CHA");
            SkillModifier("Perform (sing)", "CHA");
            SkillModifier("Profession", "WIS"); // TODO : need subskills
            SkillModifier("Ride", "DEX");
            SkillModifier("Search", "INT");
            SkillModifier("Sense Motive", "WIS");
            SkillModifier("Sleight Of Hand", "DEX");
            SkillModifier("Spellcraft", "INT");
            SkillModifier("Spot", "WIS");
            SkillModifier("Survival", "WIS");
            SkillModifier("Swim", "STR");
            SkillModifier("Tumble", "DEX");
            SkillModifier("Use Magic Device", "CHA");
            SkillModifier("Use Rope", "DEX");
            // TODO : expanded skills -- psionics, iaijutsu, martial lore, etc

        }

        // returns the check stat, so i can make initiative later.
        public Statistic AbilityScore(string ability) {
            Statistic abilityScore = new Statistic(ability + " score");
            Stats.Add(abilityScore);
            string abilityModifierName = ability + " modifier";
            Rules.AddEffect(
                new Effect("Base " + ability + " modifier")
                    .AddPart(new EffectPart("untyped", abilityModifierName)
                        .AddAmount(-5)
                        .AddAmount(new VariableAmount(abilityScore).Halve())
                    )
            );

            Statistic abilityModifier = new Statistic(abilityModifierName);
            Stats.Add(abilityModifier);
            Statistic checks = new Statistic(ability + " checks", abilityModifier)
                .AddTag("ability checks");
            Stats.Add(checks);

            abilityModifiers[ability] = abilityModifier;
            return checks;
        }

        public void SkillModifier(string skill, string ability) {
            string skillRanksName = skill + " ranks";
            Statistic skillRanks = new Statistic(skillRanksName);
            Stats.Add(skillRanks);

            string skillChecks = skill + " checks";
            Stats.Add(new Statistic(skillChecks)
                .AddTag("All skills")
                .AddTag(ability + " skills")
            );

            Rules.AddEffect(
                new Effect("Base " + skill + " modifier")
                    .AddPart(new EffectPart("untyped", skillChecks)
                        .AddAmount(new VariableAmount(skillRanks))
                        .AddAmount(new VariableAmount(abilityModifiers[ability]))
                    )
            );
        }

        public void SaveModifier(string save, string ability) {
            string saveName = save + " saves";
            string baseSaveName = "Base " + save;
            Statistic baseSave = new Statistic(baseSaveName);
            Stats.Add(baseSave);

            Stats.Add(new Statistic(saveName));
            Rules.AddEffect(
                new Effect("Total " + save + " modifier")
                    .AddPart(new EffectPart("untyped", saveName)
                        .AddAmount(new VariableAmount(baseSave))
                        .AddAmount(new VariableAmount(abilityModifiers[ability]))
                    )
            );
        }
    }
}
