using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayMore35.Effects;

using static PlayMore35.GameRules.Constants;

namespace PlayMore35.GameRules {
    public class GameRules {
        public EffectPool Rules { get; private set; } = new EffectPool();

        public Dictionary<string, Statistic> Stats { get; private set; } = new Dictionary<string, Statistic>();

        // TODO : Non-default constructor(s) to load houserules or custom rules from files.
        // Or, should we always load the official rules, then modify those rules with houserules?

        private readonly Dictionary<string, Statistic> abilityModifiers = new Dictionary<string, Statistic>();

        // The default constructor sets up 3.5's official rules.
        public GameRules() {
            AbilityScore(STR);
            AbilityScore(DEX);
            AbilityScore(CON);
            AbilityScore(INT);
            AbilityScore(WIS);
            AbilityScore(CHA);
            AddStat(new Statistic(INITIATIVE, Stats[DEX_CHECKS]));

            SkillModifier(APPRAISE, INT);
            SkillModifier(BALANCE, DEX);
            SkillModifier(BLUFF, CHA);
            SkillModifier(CLIMB, STR);
            SkillModifier(CONCENTRATION, CON);
            SkillModifier(CRAFT, INT);
            SkillModifier(DECIPHER_SCRIPT, INT);
            SkillModifier(DIPLOMACY, CHA);
            SkillModifier(DISABLE_DEVICE, INT);
            SkillModifier(DISGUISE, CHA);
            SkillModifier(ESCAPE_ARTIST, DEX);
            SkillModifier(FORGERY, INT);
            SkillModifier(GATHER_INFORMATION, CHA);
            SkillModifier(HANDLE_ANIMAL, CHA);
            SkillModifier(HEAL, WIS);
            SkillModifier(HIDE, DEX);
            SkillModifier(INTIMIDATE, CHA);
            SkillModifier(JUMP, STR);
            SkillModifier(KNOWLEDGE_ARCANA, INT);
            SkillModifier(KNOWLEDGE_ARCHITECTURE_AND_ENGINEERING, INT);
            SkillModifier(KNOWLEDGE_DUNGEONEERING, INT);
            SkillModifier(KNOWLEDGE_GEOGRAPHY, INT);
            SkillModifier(KNOWLEDGE_HISTORY, INT);
            SkillModifier(KNOWLEDGE_LOCAL, INT);
            SkillModifier(KNOWLEDGE_NATURE, INT);
            SkillModifier(KNOWLEDGE_NOBILITY_AND_ROYALTY, INT);
            SkillModifier(KNOWLEDGE_RELIGION, INT);
            SkillModifier(KNOWLEDGE_THE_PLANES, INT);
            SkillModifier(LISTEN, WIS);
            SkillModifier(MOVE_SILENTLY, DEX);
            SkillModifier(OPEN_LOCK, DEX);
            SkillModifier(PERFORM_ACT, CHA);
            SkillModifier(PERFORM_COMEDY, CHA);
            SkillModifier(PERFORM_DANCE, CHA);
            SkillModifier(PERFORM_KEYBOARD_INSTRUMENTS, CHA);
            SkillModifier(PERFORM_ORATORY, CHA);
            SkillModifier(PERFORM_PERCUSSION_INSTRUMENTS, CHA);
            SkillModifier(PERFORM_STRING_INSTRUMENTS, CHA);
            SkillModifier(PERFORM_WIND_INSTRUMENTS, CHA);
            SkillModifier(PERFORM_SING, CHA);
            SkillModifier(PROFESSION, WIS);
            SkillModifier(RIDE, DEX);
            SkillModifier(SEARCH, INT);
            SkillModifier(SENSE_MOTIVE, WIS);
            SkillModifier(SLEIGHT_OF_HAND, DEX);
            SkillModifier(SPELLCRAFT, INT);
            SkillModifier(SPOT, WIS);
            SkillModifier(SURVIVAL, WIS);
            SkillModifier(SWIM, STR);
            SkillModifier(TUMBLE, DEX);
            SkillModifier(USE_MAGIC_DEVICE, CHA);
            SkillModifier(USE_ROPE, DEX);
            // TODO : expanded skills -- psionics, iaijutsu, martial lore, etc

            SkillSynergy(Stats[BLUFF + RANKS], DIPLOMACY);
            SkillSynergy(Stats[BLUFF + RANKS], INTIMIDATE);
            SkillSynergy(Stats[BLUFF + RANKS], SLEIGHT_OF_HAND);
            SkillSynergy(Stats[HANDLE_ANIMAL + RANKS], RIDE);
            SkillSynergy(Stats[JUMP + RANKS], TUMBLE);
            SkillSynergy(Stats[KNOWLEDGE_ARCANA + RANKS], SPELLCRAFT);
            SkillSynergy(Stats[KNOWLEDGE_LOCAL + RANKS], GATHER_INFORMATION);
            SkillSynergy(Stats[KNOWLEDGE_NOBILITY_AND_ROYALTY + RANKS], DIPLOMACY);
            SkillSynergy(Stats[SENSE_MOTIVE + RANKS], DIPLOMACY);
            SkillSynergy(Stats[SURVIVAL + RANKS], KNOWLEDGE_NATURE);
            SkillSynergy(Stats[TUMBLE + RANKS], BALANCE);
            SkillSynergy(Stats[TUMBLE + RANKS], JUMP);
            // TODO : 5 ranks in bluff gives +2 Disguise to act in character, etc

            AddStat(new Statistic(ARMOR_CHECK_PENALTY));
            ArmorCheckPenalty(BALANCE);
            ArmorCheckPenalty(CLIMB);
            ArmorCheckPenalty(BALANCE);
            ArmorCheckPenalty(ESCAPE_ARTIST);
            ArmorCheckPenalty(HIDE);
            ArmorCheckPenalty(JUMP);
            ArmorCheckPenalty(MOVE_SILENTLY);
            ArmorCheckPenalty(SLEIGHT_OF_HAND);
            Rules.AddEffect(
                new Effect(ARMOR_CHECK_PENALTY + " to " + SWIM)
                    .AddModifier(new Modifier(new VariableAmount(Stats[ARMOR_CHECK_PENALTY]).Double(), UNTYPED, SWIM))
            );
            // TODO : (after toggle-effects are in)
            // apply ACP these specific skills only if the character is proficient with their armor
            // apply ACP to all STR and DEX skills if the character is not proficient with their armor

            Statistic ac = new Statistic(AC);
            Statistic touchAC = new Statistic(TOUCH_AC, ac)
                .ignoreModifierType(ARMOR)
                .ignoreModifierType(SHIELD)
                .ignoreModifierType(NATURAL_ARMOR);
            Statistic flatFootedAC = new Statistic(FLAT_FOOTED_AC, ac)
                .ignoreModifierType(DODGE);
            Statistic flatFootedTouchAC = new Statistic(FLAT_FOOTED_TOUCH_AC, touchAC)
                .ignoreModifierType(DODGE);
            AddStat(ac);
            AddStat(touchAC);
            AddStat(flatFootedAC);
            AddStat(flatFootedTouchAC);

            Rules.AddEffect(new Effect("DEX bonus to AC")
                .AddModifier(new Modifier(Stats[DEX_MODIFIER], DODGE, AC))
            );

            Rules.AddEffect(new Effect("Base AC")
                .AddModifier(new Modifier(10, UNTYPED, AC))
            );


            AddStat(new Statistic(SPECIAL_SIZE_MODIFIER)); // TODO : actually calculate this lol
            Rules.AddEffect(new Effect("Hide special size modifier")
                .AddModifier(new Modifier(new VariableAmount(Stats[SPECIAL_SIZE_MODIFIER]).Negate(), UNTYPED, HIDE))
            );

            AddStat(new Statistic(BASE_ATTACK_BONUS));
            Rules.AddEffect(new Effect("Base grapple")
                .AddModifier(new Modifier(Stats[BASE_ATTACK_BONUS], UNTYPED, GRAPPLE))
                .AddModifier(new Modifier(Stats[STR_MODIFIER], UNTYPED, GRAPPLE))
                .AddModifier(new Modifier(Stats[SPECIAL_SIZE_MODIFIER], UNTYPED, GRAPPLE))
            );

        }

        public void AddStat(Statistic stat) {
            Stats[stat.Name] = stat;
        }

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
                .AddTag(ABILITY_CHECKS);
            AddStat(checks);

            abilityModifiers[ability] = abilityModifier;
        }

        private void SkillModifier(string skill, string ability) {
            string skillRanksName = skill + " ranks";
            Statistic skillRanks = new Statistic(skillRanksName);
            AddStat(skillRanks);

            AddStat(new Statistic(skill)
                .AddTag(ALL_SKILLS)
                .AddTag(ability + " skills")
            );

            Rules.AddEffect(
                new Effect("Base " + skill + " modifier")
                    .AddModifier(new Modifier(UNTYPED, skill)
                        .AddAmount(new VariableAmount(skillRanks))
                        .AddAmount(new VariableAmount(abilityModifiers[ability]))
                    )
            );
        }

        private void SaveModifier(string save, string ability) {
            string saveName = save + " saves";
            string baseSaveName = "Base " + save;
            Statistic baseSave = new Statistic(baseSaveName);
            AddStat(baseSave);

            AddStat(new Statistic(saveName));
            Rules.AddEffect(
                new Effect("Total " + save + " modifier")
                    .AddModifier(new Modifier(UNTYPED, saveName)
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
                new Effect(ARMOR_CHECK_PENALTY + " to " + skill)
                    .AddModifier(new Modifier(Stats[ARMOR_CHECK_PENALTY], UNTYPED, skill))
            );
        }
    }
}
