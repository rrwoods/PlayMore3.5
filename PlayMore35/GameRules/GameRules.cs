using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayMore35.Effects;

using static PlayMore35.GameRules.Constants;
using static PlayMore35.Items.AttackNames;

namespace PlayMore35.GameRules {
    public class GameRules {
        public EffectPool Rules { get; private set; } = new EffectPool();

        private readonly List<Items.Weapon> weapons = new List<Items.Weapon>();
        private readonly Dictionary<string, Statistic> stats = new Dictionary<string, Statistic>();

        public void AddStat(Statistic stat) {
            stats[stat.Name] = stat;
        }

        public void AddWeapon(Items.Weapon weapon) {
            weapons.Add(weapon);
        }

        public Dictionary<string, Statistic> Stats() {
            Dictionary<string, Statistic> snapshot = new Dictionary<string, Statistic>(stats);

            foreach (Items.Weapon weapon in weapons) {
                string name = weapon.Name;

                foreach (Items.Attack attack in weapon.Attacks) {
                    string range = attack.RangeCategory.Name();
                    Statistic attackStat = new Statistic(range + ATTACK_ROLLS + WITH + name)
                        .AddTag(ATTACK_ROLLS)
                        .AddTag(range + ATTACK_ROLLS)
                        .AddTag(ATTACK_ROLLS + WITH + name);
                    Statistic damageStat = new Statistic(range + DAMAGE_ROLLS + WITH + name)
                        .AddTag(DAMAGE_ROLLS)
                        .AddTag(range + DAMAGE_ROLLS)
                        .AddTag(DAMAGE_ROLLS + WITH + name);

                    List<string> tags = new List<string>(weapon.Tags) {
                        attack.AttackType.Name(),
                        attack.ProficencyCategory.Name(),
                        attack.EffortCategory.Name(),
                        attack.RangeCategory.FullName()
                    };
                    if (attack.EffortCategory != Items.Attack.Effort.TWO_HANDED) {
                        tags.Add(attack.OffHand ? OFF_HAND : PRIMARY_HAND);
                    }
                    if (attack.RangeCategory == Items.Attack.Range.PROJECTILE && attack.StrengthRating >= 0) {
                        tags.Add("+" + attack.StrengthRating + " STR");
                    }
                    foreach (string tag in tags) {
                        attackStat
                            .AddTag(ATTACK_ROLLS + WITH + tag + WEAPONS)
                            .AddTag(range + ATTACK_ROLLS + WITH + tag + WEAPONS);
                        damageStat
                            .AddTag(DAMAGE_ROLLS + WITH + tag + WEAPONS)
                            .AddTag(range + DAMAGE_ROLLS + WITH + tag + WEAPONS);
                    }

                    snapshot[attackStat.Name] = attackStat;
                    snapshot[damageStat.Name] = damageStat;
                }
            }

            return snapshot;
        }

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
            AddStat(new Statistic(INITIATIVE, stats[DEX_CHECKS]));

            AddStat(new Statistic(MAX_HP));

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

            SkillSynergy(stats[BLUFF + RANKS], DIPLOMACY);
            SkillSynergy(stats[BLUFF + RANKS], INTIMIDATE);
            SkillSynergy(stats[BLUFF + RANKS], SLEIGHT_OF_HAND);
            SkillSynergy(stats[HANDLE_ANIMAL + RANKS], RIDE);
            SkillSynergy(stats[JUMP + RANKS], TUMBLE);
            SkillSynergy(stats[KNOWLEDGE_ARCANA + RANKS], SPELLCRAFT);
            SkillSynergy(stats[KNOWLEDGE_LOCAL + RANKS], GATHER_INFORMATION);
            SkillSynergy(stats[KNOWLEDGE_NOBILITY_AND_ROYALTY + RANKS], DIPLOMACY);
            SkillSynergy(stats[SENSE_MOTIVE + RANKS], DIPLOMACY);
            SkillSynergy(stats[SURVIVAL + RANKS], KNOWLEDGE_NATURE);
            SkillSynergy(stats[TUMBLE + RANKS], BALANCE);
            SkillSynergy(stats[TUMBLE + RANKS], JUMP);
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
                    .AddModifier(new Modifier(new VariableAmount(stats[ARMOR_CHECK_PENALTY], 2f), UNTYPED, SWIM))
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

            Rules.AddEffect(new Effect("DEX to AC")
                .AddModifier(new Modifier(stats[DEX_MODIFIER], DODGE, AC))
            );

            Rules.AddEffect(new Effect("Base AC")
                .AddModifier(new Modifier(10, UNTYPED, AC))
            );

            AddStat(new Statistic(NATURAL_ARMOR)); // this is the same name as the bonus type. eh.
            Rules.AddEffect(new Effect(NATURAL_ARMOR)
                .AddModifier(new Modifier(stats[NATURAL_ARMOR], NATURAL_ARMOR, AC))
            );

            AddStat(new Statistic(LAND_SPEED).AddTag(ALL_SPEEDS));
            AddStat(new Statistic(BURROW_SPEED).AddTag(ALL_SPEEDS));
            AddStat(new Statistic(CLIMB_SPEED).AddTag(ALL_SPEEDS));
            AddStat(new Statistic(SWIM_SPEED).AddTag(ALL_SPEEDS));
            AddStat(new Statistic(FLY_SPEED).AddTag(ALL_SPEEDS));
            AddStat(new Statistic(MANEUVERABILITY));

            AddStat(new Statistic(SIZE_CATEGORY)); // TODO : "derived" stats for Space and Reach, via a (new) LookupAmount

            AddStat(new Statistic(SPECIAL_SIZE_MODIFIER)); // TODO : actually calculate this lol
            Rules.AddEffect(new Effect("Hide special size modifier")
                .AddModifier(new Modifier(new VariableAmount(stats[SPECIAL_SIZE_MODIFIER], -1), UNTYPED, HIDE))
            );

            AddStat(new Statistic(BASE_ATTACK_BONUS));
            Rules.AddEffect(new Effect("Base grapple")
                .AddModifier(new Modifier(stats[BASE_ATTACK_BONUS], UNTYPED, GRAPPLE))
                .AddModifier(new Modifier(stats[STR_MODIFIER], UNTYPED, GRAPPLE))
                .AddModifier(new Modifier(stats[SPECIAL_SIZE_MODIFIER], UNTYPED, GRAPPLE))
            );

            Rules.AddEffect(new Effect(BASE_ATTACK_BONUS)
                .AddModifier(new Modifier(stats[BASE_ATTACK_BONUS], UNTYPED, ATTACK_ROLLS))
            );

            Rules.AddEffect(new Effect("DEX to ranged attacks")
                .AddModifier(new Modifier(stats[DEX_MODIFIER], UNTYPED, RANGED + ATTACK_ROLLS))
            );
            // Weapon Finesse will use ability-substitution technology
            Rules.AddEffect(new Effect("STR to melee attacks")
                .AddModifier(new Modifier(stats[STR_MODIFIER], UNTYPED, MELEE + ATTACK_ROLLS))
            );

            Rules.AddEffect(new Effect("1½ STR bonus to two-handed weapon damage")
                .AddModifier(new Modifier(
                    new VariableAmount(stats[STR_BONUS], 1.5f),
                    UNTYPED,
                    MELEE + DAMAGE_ROLLS + WITH + TWO_HANDED + WEAPONS
                ))
            );
            Rules.AddEffect(new Effect("STR bonus to primary-hand weapon damage")
                .AddModifier(new Modifier(stats[STR_BONUS], UNTYPED, MELEE + DAMAGE_ROLLS + WITH + PRIMARY_HAND + WEAPONS))
            );
            Rules.AddEffect(new Effect("½ STR bonus to off-hand weapon damage")
                .AddModifier(new Modifier(
                    new VariableAmount(stats[STR_BONUS], 0.5f),
                    UNTYPED,
                    MELEE + DAMAGE_ROLLS + WITH + OFF_HAND + WEAPONS
                ))
            );

            Rules.AddEffect(new Effect("STR penalty to projectile weapon damage")
                .AddModifier(new Modifier(stats[STR_PENALTY], UNTYPED, RANGED + DAMAGE_ROLLS + WITH + PROJECTILE + WEAPONS))
            );
            Rules.AddEffect(new Effect("STR to thrown weapon damage")
                .AddModifier(new Modifier(stats[STR_MODIFIER], UNTYPED, RANGED + DAMAGE_ROLLS + WITH + THROWN + WEAPONS))
            );

            // XXX : I hate this.  I hate this so much.
            // But I currently see no other way of dealing with composite bows.
            for (int strengthRating = 0; strengthRating <= 10; strengthRating++) {
                string strText = "+" + strengthRating + " STR";
                Rules.AddEffect(new Effect("Insufficient STR for " + strText + " composite bow")
                    .SetToggle(new ConditionalToggle(stats[STR_MODIFIER], Relation.LESS_THAN, strengthRating))
                    .AddModifier(new Modifier(-2, UNTYPED, RANGED + ATTACK_ROLLS + WITH + strText + WEAPONS))
                );
                Rules.AddEffect(new Effect(strText + " composite bow damage")
                    .AddModifier(new Modifier(
                        new LesserAmount(
                            new VariableAmount(stats[STR_BONUS]), // if there's a penalty, we already applied it above.
                            new FixedAmount(strengthRating)
                        ),
                        UNTYPED,
                        RANGED + DAMAGE_ROLLS + WITH + strText + WEAPONS
                    ))
                );
            }
        }

        private void AbilityScore(string ability) {
            Statistic abilityScore = new Statistic(ability + " score");
            AddStat(abilityScore);
            string abilityModifierName = ability + " modifier";
            Rules.AddEffect(
                new Effect("Base " + ability + " modifier")
                    .AddModifier(new Modifier("untyped", abilityModifierName)
                        .AddAmount(-5)
                        .AddAmount(new VariableAmount(abilityScore, 0.5f))
                    )
            );

            Statistic abilityModifier = new Statistic(abilityModifierName);
            AddStat(abilityModifier);
            Statistic checks = new Statistic(ability + " checks", abilityModifier)
                .AddTag(ABILITY_CHECKS);
            AddStat(checks);

            abilityModifiers[ability] = abilityModifier;

            Statistic abilityBonus = new Statistic(ability + " bonus");
            AddStat(abilityBonus);
            Rules.AddEffect(
                new Effect(ability + " bonus")
                    .AddModifier(new Modifier(
                        new GreaterAmount(new FixedAmount(0), new VariableAmount(abilityModifier)),
                        UNTYPED,
                        abilityBonus.Name
                    ))
            );

            Statistic abilityPenalty = new Statistic(ability + " penalty");
            AddStat(abilityPenalty);
            Rules.AddEffect(
                new Effect(ability + " penalty")
                    .AddModifier(new Modifier(
                        new LesserAmount(new FixedAmount(0), new VariableAmount(abilityModifier)),
                        UNTYPED,
                        abilityPenalty.Name
                    ))
            );
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
                    .AddModifier(new Modifier(stats[ARMOR_CHECK_PENALTY], UNTYPED, skill))
            );
        }
    }
}
