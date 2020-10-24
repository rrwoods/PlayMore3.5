using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayMore35.Effects;

namespace PlayMore35.GameRules {
    public class GameRules {
        public EffectPool Rules { get; private set; } = new EffectPool();

        public Dictionary<string, Statistic> Stats { get; private set; } = new Dictionary<string, Statistic>();

        // TODO : Non-default constructor(s) to load houserules or custom rules from files.
        // Or, should we always load the official rules, then modify those rules with houserules?

        private readonly Dictionary<string, Statistic> abilityModifiers = new Dictionary<string, Statistic>();

        // The default constructor sets up 3.5's official rules.
        public GameRules() {
            AbilityScore("STR");
            AbilityScore("DEX");
            AbilityScore("CON");
            AbilityScore("INT");
            AbilityScore("WIS");
            AbilityScore("CHA");
            AddStat(new Statistic("Initiative", Stats["DEX checks"]));

            SkillModifier("Appraise", "INT");
            SkillModifier("Balance", "DEX");
            Statistic bluffRanks = SkillModifier("Bluff", "CHA");
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
            Statistic handleAnimalRanks = SkillModifier("Handle Animal", "CHA");
            SkillModifier("Heal", "WIS");
            SkillModifier("Hide", "DEX");
            SkillModifier("Intimidate", "CHA");
            Statistic jumpRanks = SkillModifier("Jump", "STR");
            Statistic knowledgeArcanaRanks = SkillModifier("Knowledge (arcana)", "INT");
            Statistic knowledgeArchitectureRanks = SkillModifier("Knowledge (architecture and engineering)", "INT");
            Statistic knowledgeDungeoneeringRanks = SkillModifier("Knowledge (dungeoneering)", "INT");
            Statistic knowledgeGeographyRanks = SkillModifier("Knowledge (geography)", "INT");
            Statistic knowledgeHistoryRanks = SkillModifier("Knowledge (history)", "INT");
            Statistic knowledgeLocalRanks = SkillModifier("Knowledge (local)", "INT");
            Statistic knowledgeNatureRanks = SkillModifier("Knowledge (nature)", "INT");
            Statistic knowledgeNobilityRanks = SkillModifier("Knowledge (nobility and royalty)", "INT");
            Statistic knowledgeReligionRanks = SkillModifier("Knowledge (religion)", "INT");
            Statistic knowledgePlanesRanks = SkillModifier("Knowledge (the planes)", "INT");
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
            Statistic senseMotiveRanks = SkillModifier("Sense Motive", "WIS");
            SkillModifier("Sleight Of Hand", "DEX");
            SkillModifier("Spellcraft", "INT");
            SkillModifier("Spot", "WIS");
            Statistic survivalRanks = SkillModifier("Survival", "WIS");
            SkillModifier("Swim", "STR");
            Statistic tumbleRanks = SkillModifier("Tumble", "DEX");
            SkillModifier("Use Magic Device", "CHA");
            SkillModifier("Use Rope", "DEX");
            // TODO : expanded skills -- psionics, iaijutsu, martial lore, etc

            SkillSynergy(bluffRanks, "Diplomacy");
            SkillSynergy(bluffRanks, "Intimidate");
            SkillSynergy(bluffRanks, "Sleight of Hand");
            SkillSynergy(handleAnimalRanks, "Ride");
            SkillSynergy(jumpRanks, "Tumble");
            SkillSynergy(knowledgeArcanaRanks, "Spellcraft");
            SkillSynergy(knowledgeLocalRanks, "Gather Information");
            SkillSynergy(knowledgeNobilityRanks, "Diplomacy");
            SkillSynergy(senseMotiveRanks, "Diplomacy");
            SkillSynergy(survivalRanks, "Knowledge (nature)");
            SkillSynergy(tumbleRanks, "Balance");
            SkillSynergy(tumbleRanks, "Jump");
            // TODO : 5 ranks in bluff gives +2 Disguise to act in character, etc

            AddStat(new Statistic("Armor check penalty"));
            ArmorCheckPenalty("Balance");
            ArmorCheckPenalty("Climb");
            ArmorCheckPenalty("Balance");
            ArmorCheckPenalty("Escape Artist");
            ArmorCheckPenalty("Hide");
            ArmorCheckPenalty("Jump");
            ArmorCheckPenalty("Move Silently");
            ArmorCheckPenalty("Sleight of Hand");
            Rules.AddEffect(
                new Effect("Armor check penalty to Swim")
                    .AddModifier(new Modifier(new VariableAmount(Stats["Armor check penalty"]).Double(), "untyped", "Swim"))
            );
            // TODO : (after toggle-effects are in)
            // apply ACP these specific skills only if the character is proficient with their armor
            // apply ACP to all STR and DEX skills if the character is not proficient with their armor

            Statistic ac = new Statistic("AC");
            Statistic touchAC = new Statistic("Touch AC", ac)
                .ignoreModifierType("armor")
                .ignoreModifierType("shield")
                .ignoreModifierType("natural armor");
            Statistic flatFootedAC = new Statistic("Flat-footed AC", ac)
                .ignoreModifierType("dodge");
            Statistic flatFootedTouchAC = new Statistic("Flat-footed touch AC", touchAC)
                .ignoreModifierType("dodge");
            AddStat(ac);
            AddStat(touchAC);
            AddStat(flatFootedAC);
            AddStat(flatFootedTouchAC);

            Rules.AddEffect(new Effect("DEX bonus to AC")
                .AddModifier(new Modifier(Stats["DEX modifier"], "dodge", "AC"))
            );

            Rules.AddEffect(new Effect("Base AC")
                .AddModifier(new Modifier(10, "untyped", "AC"))
            );
        }

        private void AddStat(Statistic stat) {
            Stats[stat.Name] = stat;
        }

        // returns the check stat, so i can make initiative later.
        private void AbilityScore(string ability) {
            Statistic abilityScore = new Statistic(ability + " score");
            AddStat(abilityScore);
            string abilityModifierName = ability + " modifier";
            Rules.AddEffect(
                new Effect("Base " + ability + " modifier")
                    .AddModifier(new Modifier("untyped", abilityModifierName)
                        .AddAmount(-5)
                        .AddAmount(new VariableAmount(abilityScore).Halve())
                    )
            );

            Statistic abilityModifier = new Statistic(abilityModifierName);
            AddStat(abilityModifier);
            Statistic checks = new Statistic(ability + " checks", abilityModifier)
                .AddTag("Ability checks");
            AddStat(checks);

            abilityModifiers[ability] = abilityModifier;
        }

        private Statistic SkillModifier(string skill, string ability) {
            string skillRanksName = skill + " ranks";
            Statistic skillRanks = new Statistic(skillRanksName);
            AddStat(skillRanks);

            AddStat(new Statistic(skill)
                .AddTag("All skills")
                .AddTag(ability + " skills")
            );

            Rules.AddEffect(
                new Effect("Base " + skill + " modifier")
                    .AddModifier(new Modifier("untyped", skill)
                        .AddAmount(new VariableAmount(skillRanks))
                        .AddAmount(new VariableAmount(abilityModifiers[ability]))
                    )
            );

            return skillRanks;
        }

        private void SaveModifier(string save, string ability) {
            string saveName = save + " saves";
            string baseSaveName = "Base " + save;
            Statistic baseSave = new Statistic(baseSaveName);
            AddStat(baseSave);

            AddStat(new Statistic(saveName));
            Rules.AddEffect(
                new Effect("Total " + save + " modifier")
                    .AddModifier(new Modifier("untyped", saveName)
                        .AddAmount(new VariableAmount(baseSave))
                        .AddAmount(new VariableAmount(abilityModifiers[ability]))
                    )
            );
        }

        private void SkillSynergy(Statistic fiveRanksIn, string givesPlusTwoOn) {
            Rules.AddEffect(
                new Effect("Synergy: " + fiveRanksIn.Name + " -> " + givesPlusTwoOn)
                    .AddModifier(new Modifier(2, "untyped", givesPlusTwoOn))
                    .SetToggle(new ConditionalToggle(new VariableAmount(fiveRanksIn), Relation.GREATER_THAN_OR_EQUAL, 5))
            );
        }

        private void ArmorCheckPenalty(string skill) {
            Rules.AddEffect(
                new Effect("Armor check penalty to " + skill)
                    .AddModifier(new Modifier(Stats["Armor check penalty"], "untyped", skill))
            );
        }
    }
}
