using System;
using System.Collections.Generic;
using PlayMore35.Effects;

using static PlayMore35.GameRules.Constants;

namespace PlayMore35.GameRules {
    public class GameRules {
        public EffectPool Rules { get; private set; } = new EffectPool();

        private readonly Dictionary<string, Statistic> stats = new Dictionary<string, Statistic>();
        private readonly Dictionary<string, Statistic> abilityModifiers = new Dictionary<string, Statistic>();

        public void AddStat(Statistic stat) {
            stats[stat.Name] = stat;
        }

        public Statistic GetStat(string name) {
            return stats[name];
        }

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

            AddStat(new Statistic(CURRENT_HP));
            Rules.AddEffect(new Effect("Normal hit point total")
                .AddModifier(CURRENT_HP, new Modifier(stats[MAX_HP], BASE))
            );

            AddStat(new Statistic(LETHAL_DAMAGE));
            Rules.AddEffect(new Effect("Subtract damage from hit points")
                // this is using ability modifier just to ensure it shows up after BASE.
                .AddModifier(CURRENT_HP, new Modifier(-1, stats[LETHAL_DAMAGE], ABILITY_MODIFIER))
            );

            Rules.AddEffect(new Effect(CANT_ACT)
                .SetToggle(new FlagToggle(CANT_ACT))
                .AddText(ACTIONS, CANT_ACT)
            );

            // The UI should automatically clear this flag when you damage yourself to the negatives or heal out of them.
            ControllableToggle stable = new ControllableToggle("stable");
            Rules.AddEffect(new Effect("disabled")
                .SetToggle(new AnyToggle(
                    new ConditionalToggle(stats[CURRENT_HP], Relation.EQUAL, 0),
                    new AllToggle(
                        new ConditionalToggle(stats[CURRENT_HP], Relation.LESS_THAN, 0),
                        stable
                    )
                ))
                .AddText(ACTIONS, "One move or standard each round (not both, no full-rounds)")
                .AddMultiplier(ALL_SPEEDS, new Multiplier(0.5m))
                .AddText(ACTIONS, "Any standard or strenuous action deals 1 to you")
            );

            Rules.AddEffect(new Effect("dying")
                .SetToggle(new AllToggle(
                    new ConditionalToggle(stats[CURRENT_HP], Relation.LESS_THAN, 0),
                    new UnToggle(stable)
                ))
                .AddFlag(CANT_ACT)
                .AddText(CONDITION, "You take 1 damage each round")
                .AddText(CONDITION, "Any healing stabilizes you; 10% chance each round to self-stabilize")
            );
                    
            AddStat(new Statistic(NONLETHAL_DAMAGE));

            SkillModifier(APPRAISE, INT);
            SkillModifier(AUTOHYPNOSIS, WIS);
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
            SkillModifier(IAIJUTSU_FOCUS, CHA);
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
            SkillModifier(KNOWLEDGE_PSIONICS, INT);
            SkillModifier(KNOWLEDGE_RELIGION, INT);
            SkillModifier(KNOWLEDGE_THE_PLANES, INT);
            SkillModifier(MARTIAL_LORE, INT);
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
            SkillModifier(PSICRAFT, INT);
            SkillModifier(RIDE, DEX);
            SkillModifier(SEARCH, INT);
            SkillModifier(SENSE_MOTIVE, WIS);
            SkillModifier(SLEIGHT_OF_HAND, DEX);
            SkillModifier(SPELLCRAFT, INT);
            SkillModifier(SPOT, WIS);
            SkillModifier(SURVIVAL, WIS);
            SkillModifier(SWIM, STR);
            SkillModifier(TRUESPEAK, INT);
            SkillModifier(TUMBLE, DEX);
            SkillModifier(USE_MAGIC_DEVICE, CHA);
            SkillModifier(USE_PSIONIC_DEVICE, CHA);
            SkillModifier(USE_ROPE, DEX);
            AddStat(new Statistic(CLIMB_ROPE,                     stats[CLIMB]));
            AddStat(new Statistic(DISGUISE_ACT_IN_CHARACTER,      stats[DISGUISE]));
            AddStat(new Statistic(ESCAPE_ARTIST_ROPE,             stats[ESCAPE_ARTIST]));
            AddStat(new Statistic(PSICRAFT_ADDRESS_POWER_STONE,   stats[PSICRAFT]));
            AddStat(new Statistic(SEARCH_SECRET_DOORS,            stats[SEARCH]));
            AddStat(new Statistic(SPELLCRAFT_DECIPHER_SCROLL,     stats[SPELLCRAFT]));
            AddStat(new Statistic(SURVIVAL_ABOVEGROUND,           stats[SURVIVAL]));
            AddStat(new Statistic(SURVIVAL_FOLLOW_TRACKS,         stats[SURVIVAL]));
            AddStat(new Statistic(SURVIVAL_LOST,                  stats[SURVIVAL]));
            AddStat(new Statistic(SURVIVAL_PLANAR,                stats[SURVIVAL]));
            AddStat(new Statistic(SURVIVAL_UNDERGROUND,           stats[SURVIVAL]));
            AddStat(new Statistic(USE_MAGIC_DEVICE_SCROLL,        stats[USE_MAGIC_DEVICE]));
            AddStat(new Statistic(USE_PSIONIC_DEVICE_POWER_STONE, stats[USE_PSIONIC_DEVICE]));
            AddStat(new Statistic(USE_ROPE_BINDINGS,              stats[USE_ROPE]));

            SkillSynergy(BLUFF, DIPLOMACY, DISGUISE_ACT_IN_CHARACTER, INTIMIDATE, SLEIGHT_OF_HAND);
            SkillSynergy(DECIPHER_SCRIPT, USE_MAGIC_DEVICE_SCROLL);
            SkillSynergy(ESCAPE_ARTIST, USE_ROPE_BINDINGS);
            SkillSynergy(HANDLE_ANIMAL, RIDE);
            SkillSynergy(JUMP, TUMBLE);
            SkillSynergy(KNOWLEDGE_ARCANA, SPELLCRAFT);
            SkillSynergy(KNOWLEDGE_ARCHITECTURE_AND_ENGINEERING, SEARCH_SECRET_DOORS);
            SkillSynergy(KNOWLEDGE_DUNGEONEERING, SURVIVAL_UNDERGROUND);
            SkillSynergy(KNOWLEDGE_GEOGRAPHY, SURVIVAL_LOST);
            SkillSynergy(KNOWLEDGE_LOCAL, GATHER_INFORMATION);
            SkillSynergy(KNOWLEDGE_NATURE, SURVIVAL_ABOVEGROUND);
            SkillSynergy(KNOWLEDGE_NOBILITY_AND_ROYALTY, DIPLOMACY);
            SkillSynergy(KNOWLEDGE_THE_PLANES, SURVIVAL_PLANAR);
            SkillSynergy(SEARCH, SURVIVAL_FOLLOW_TRACKS);
            SkillSynergy(SENSE_MOTIVE, DIPLOMACY);
            SkillSynergy(SPELLCRAFT, USE_MAGIC_DEVICE_SCROLL);
            SkillSynergy(SURVIVAL, KNOWLEDGE_NATURE);
            SkillSynergy(TUMBLE, BALANCE, JUMP);
            SkillSynergy(USE_MAGIC_DEVICE, SPELLCRAFT_DECIPHER_SCROLL);
            SkillSynergy(USE_ROPE, CLIMB_ROPE, ESCAPE_ARTIST_ROPE);

            SkillSynergy(AUTOHYPNOSIS, KNOWLEDGE_PSIONICS);
            SkillSynergy(CONCENTRATION, AUTOHYPNOSIS);
            SkillSynergy(KNOWLEDGE_PSIONICS, PSICRAFT);
            SkillSynergy(PSICRAFT, USE_PSIONIC_DEVICE_POWER_STONE);
            SkillSynergy(USE_PSIONIC_DEVICE, PSICRAFT_ADDRESS_POWER_STONE);

            // given how utterly insanely hard the truename research use of the knowledge skill is,
            // i'm assuming no one will ever do it, and therefore not including the intricate web of
            // synergies associated with it.

            AddStat(new Statistic(ARMOR_CHECK_PENALTY));
            Effect acp = new Effect(ARMOR_CHECK_PENALTY);
            foreach (string skill in new string[] { BALANCE, CLIMB, ESCAPE_ARTIST, HIDE, JUMP, MOVE_SILENTLY, SLEIGHT_OF_HAND }) {
                acp.AddModifier(skill, new Modifier(stats[ARMOR_CHECK_PENALTY], UNTYPED));
            }
            acp.AddModifier(SWIM, new Modifier(2, stats[ARMOR_CHECK_PENALTY], UNTYPED));
            Rules.AddEffect(acp);

            Statistic ac = new Statistic(AC);
            Statistic touchAC = new Statistic(TOUCH_AC, ac)
                .IgnoreModifierType(ARMOR)
                .IgnoreModifierType(SHIELD)
                .IgnoreModifierType(NATURAL_ARMOR);
            AddStat(ac);
            AddStat(touchAC);

            Rules.AddEffect(new Effect("Base AC")
                .AddModifier(AC, new Modifier(10, BASE))
            );

            AddStat(new Statistic(NATURAL_ARMOR)); // this is the same name as the bonus type. eh.
            Rules.AddEffect(new Effect(NATURAL_ARMOR)
                .AddModifier(AC, new Modifier(stats[NATURAL_ARMOR], NATURAL_ARMOR))
            );


            AddStat(new Statistic(LAND_SPEED).AddTag(ALL_SPEEDS).RoundTo(5));
            AddStat(new Statistic(BURROW_SPEED).AddTag(ALL_SPEEDS).RoundTo(5));
            AddStat(new Statistic(CLIMB_SPEED).AddTag(ALL_SPEEDS).RoundTo(5));
            AddStat(new Statistic(SWIM_SPEED).AddTag(ALL_SPEEDS).RoundTo(5));
            AddStat(new Statistic(FLY_SPEED).AddTag(ALL_SPEEDS).RoundTo(5));
            AddStat(new Statistic(MANEUVERABILITY));

            Rules.AddEffect(new Effect("Encumbrance speed penalty")
                .Controllable()
                .AddMultiplier(ALL_SPEEDS, new Multiplier(0.666m))
            );

            // ... yeah, this jumps (heh) through some hoops.  nothing to see here, move along.
            // (i'm purposefully abusing integer math to get rounding on these amounts, and doing amount stacking via
            // statistic chaining to do divide-subtract-multiply.  sue me :P )
            string excessSpeed = "speed over 30 ft, in units of 10 ft";
            AddStat(new Statistic(excessSpeed));
            Rules.AddEffect(new Effect(excessSpeed)
                .AddModifier(excessSpeed, new Modifier(UNTYPED)
                    .AddAmount(0.1m, stats[LAND_SPEED])
                    .AddAmount(-3)
                )
            );
            string lackingSpeed = "speed under 30 ft, in units of 10 ft";
            AddStat(new Statistic(lackingSpeed));
            Rules.AddEffect(new Effect(lackingSpeed)
                .AddModifier(lackingSpeed, new Modifier(UNTYPED)
                    .AddAmount(-0.1m, stats[LAND_SPEED])
                    .AddAmount(+3)
                )
            );

            Rules.AddEffect(new Effect("Speed penalty")
                // the rule says "less than 30", but it doesn't actually do anything unless you're at 20 or under.
                .SetToggle(new ConditionalToggle(stats[LAND_SPEED], Relation.LESS_THAN_OR_EQUAL, 20))
                .AddModifier(JUMP, new Modifier(-6, stats[lackingSpeed], UNTYPED))
            );
            Rules.AddEffect(new Effect("Speed bonus")
                // the rule stays "more than 30", but it doesn't actually do anything unless you're at 40 or over.
                .SetToggle(new ConditionalToggle(stats[LAND_SPEED], Relation.GREATER_THAN_OR_EQUAL, 40))
                .AddModifier(JUMP, new Modifier(+4, stats[excessSpeed], UNTYPED))
            );

            // Medium at 0 makes a lot of calculations more smooth
            AddStat(new Statistic(SIZE_CATEGORY).Minimum(-4).Maximum(+4));

            AddStat(new Statistic(SIZE_MODIFIER));
            Rules.AddEffect(new Effect(SIZE_MODIFIER + " calculation")
                .AddModifier(SIZE_MODIFIER, new Modifier(
                    new LookupAmount(
                        stats[SIZE_CATEGORY],
                        new Dictionary<int, int> {
                            [-4] = +8,
                            [-3] = +4,
                            [-2] = +2,
                            [-1] = +1,
                            [0] = +0,
                            [+1] = -1,
                            [+2] = -2,
                            [+3] = -4,
                            [+4] = -8,
                        }
                    ),
                    BASE
                ))
            );
            Rules.AddEffect(new Effect(SIZE_MODIFIER)
                .AddAttackModifier(AttackClass.ALL, new Modifier(stats[SIZE_MODIFIER], SIZE))
                .AddModifier(AC, new Modifier(stats[SIZE_MODIFIER], SIZE))
            );

            // for now (or maybe forever) i'm not dealing with the fact that there's a long/tall distinction, or the fact
            // that "some exceptions exist" to these values.  for player characters i'm assuming tall, and i'm assuming that
            // space never changes, and i'm assuming that if reach changes it's via an effect not a base reach difference.
            // if this is not the case... i'll just let the player say what these values are and remove these effects.

            AddStat(new Statistic(REACH).Minimum(0));
            Rules.AddEffect(new Effect("Reach based on size")
                .AddModifier(REACH, new Modifier(
                    new LookupAmount(stats[SIZE_CATEGORY],
                        new Dictionary<int, int> {
                            [-4] = 0,
                            [-3] = 0,
                            [-2] = 0,
                            [-1] = 5,
                            [0]  = 5,
                            [+1] = 10,
                            [+2] = 15,
                            [+3] = 20,
                            [+4] = 30,
                        }
                    ),
                    BASE
                ))
            );

            AddStat(new Statistic(BASE_ATTACK_BONUS));
            AddStat(new Statistic(GRAPPLE));
            Rules.AddEffect(new Effect("STR to grapple")
                .AddModifier(GRAPPLE, new Modifier(stats[STR_MODIFIER], ABILITY_MODIFIER))
            );

            Rules.AddEffect(new Effect(BASE_ATTACK_BONUS)
                .AddAttackModifier(AttackClass.ALL, new Modifier(stats[BASE_ATTACK_BONUS], BASE))
                .AddModifier(GRAPPLE, new Modifier(stats[BASE_ATTACK_BONUS], BASE))
            );

            AddStat(new Statistic(SPECIAL_SIZE_MODIFIER));
            Rules.AddEffect(new Effect(SPECIAL_SIZE_MODIFIER + " calculation")
                .AddModifier(SPECIAL_SIZE_MODIFIER, new Modifier(4, stats[SIZE_CATEGORY], BASE))
            );
            Rules.AddEffect(new Effect(SPECIAL_SIZE_MODIFIER)
                .AddModifier(HIDE, new Modifier(-1, stats[SPECIAL_SIZE_MODIFIER], SIZE))
                .AddModifier(GRAPPLE, new Modifier(stats[SPECIAL_SIZE_MODIFIER], SIZE))
            );

            Rules.AddEffect(new Effect("DEX to ranged attacks")
                .AddAttackModifier(new AttackClass().Ranged(), new Modifier(stats[DEX_MODIFIER], ABILITY_MODIFIER))
            );
            // Weapon Finesse will use ability-substitution technology
            Rules.AddEffect(new Effect("STR to melee attacks")
                .AddAttackModifier(new AttackClass().Melee(), new Modifier(stats[STR_MODIFIER], ABILITY_MODIFIER))
            );

            Rules.AddEffect(new Effect("STR bonus ×1.5 to two-handed weapon damage")
                .AddDamageModifier(
                    new AttackClass().Melee().TwoHandedAttack(),
                    new Modifier(1.5m, stats[STR_BONUS], ABILITY_MODIFIER)
                )
            );
            Rules.AddEffect(new Effect("STR bonus to primary-hand weapon damage")
                .AddDamageModifier(new AttackClass().Melee().PrimaryHandAttack(), new Modifier(stats[STR_BONUS], ABILITY_MODIFIER))
            );
            Rules.AddEffect(new Effect("STR bonus ×0.5 to off-hand weapon damage")
                .AddDamageModifier(
                    new AttackClass().Melee().OffHandAttack(),
                    new Modifier(0.5m, stats[STR_BONUS], ABILITY_MODIFIER)
                )
            );

            Rules.AddEffect(new Effect("STR penalty to bow damage")
                .AddDamageModifier(new AttackClass().Bow(), new Modifier(stats[STR_PENALTY], ABILITY_MODIFIER))
            );
            Rules.AddEffect(new Effect("STR to thrown weapon damage")
                .AddDamageModifier(new AttackClass().Thrown(), new Modifier(stats[STR_MODIFIER], ABILITY_MODIFIER))
            );
            Rules.AddEffect(new Effect("STR to sling damage")
                .AddDamageModifier(new AttackClass().Name("Sling"), new Modifier(stats[STR_MODIFIER], ABILITY_MODIFIER))
            );     

            AddStat(new Statistic(FEAR_LEVEL).Minimum(0).Maximum(3));
            Rules.AddEffect(new Effect(SHAKEN)
                .SetToggle(new ConditionalToggle(stats[FEAR_LEVEL], Relation.GREATER_THAN_OR_EQUAL, 1))
                .AddAttackModifier(AttackClass.ALL, new Modifier(-2, UNTYPED))
                .AddModifier(ALL_SAVES, new Modifier(-2, UNTYPED))
                .AddModifier(ALL_SKILLS, new Modifier(-2, UNTYPED))
                .AddModifier(ABILITY_CHECKS, new Modifier(-2, UNTYPED))
            );
            Rules.AddEffect(new Effect(FRIGHTENED)
                .SetToggle(new ConditionalToggle(stats[FEAR_LEVEL], Relation.EQUAL, 2))
                .AddText(ACTIONS, "Must flee from source of fear")
            );
            Rules.AddEffect(new Effect(PANICKED)
                .SetToggle(new ConditionalToggle(stats[FEAR_LEVEL], Relation.EQUAL, 3))
                .AddText(ACTIONS, "Must drop all held items and flee at top speed along a random path; cower if cornered")
            );


            Rules.AddEffect(new Effect("blinded")
                .Controllable()
                .AddModifier(AC, new Modifier(-2, UNTYPED))
                .AddFlag(DENIED_DEX)
                .AddMultiplier(ALL_SPEEDS, new Multiplier(0.5m))
                .AddText(SKILLS, "All vision activities and checks (like Spot) auto-fail")
                .AddText(ATK_OPTIONS, "Opponents have total concealment")
                .AddModifier(SEARCH, new Modifier(-4, UNTYPED))
                .AddModifier(STR_SKILLS, new Modifier(-4, UNTYPED))
                .AddModifier(DEX_SKILLS, new Modifier(-4, UNTYPED))
            );

            Rules.AddEffect(new Effect("charging")
                .Controllable()
                .AddModifier(AC, new Modifier(-2, UNTYPED))
                .AddAttackModifier(AttackClass.ALL, new Modifier(+2, UNTYPED))
                .AddModifier(STR_CHECKS, new Modifier(+2, UNTYPED)) // technically this is only for a bull rush as part of this charge
            );

            // not bothering with confused status -- you have to look it up anyway, and it's prone to DM fiat

            Rules.AddEffect(new Effect("cowering")
                .Controllable()
                .AddFlag(CANT_ACT)
                .AddFlag(DENIED_DEX)
                .AddModifier(AC, new Modifier(-2, UNTYPED))
            );

            Rules.AddEffect(new Effect("dazed")
                .Controllable()
                .AddFlag(CANT_ACT)
            );

            Rules.AddEffect(new Effect("dazzled")
                .Controllable()
                .AddAttackModifier(AttackClass.ALL, new Modifier(-1, UNTYPED))
                .AddModifier(SEARCH, new Modifier(-1, UNTYPED))
                .AddModifier(SPOT, new Modifier(-1, UNTYPED))
            );

            Rules.AddEffect(new Effect("deafened")
                .Controllable()
                .AddModifier(INITIATIVE, new Modifier(-4, UNTYPED))
                .AddText(SKILLS, "Auto-fail Listen checks")
                .AddText(MISC_CASTING, "20% spell failure on spells with verbal components")
            );

            AddStat(new Statistic(NEGATIVE_LEVELS));
            IAmount negativeLevels = new MultiplyAmount(-1, stats[NEGATIVE_LEVELS]);
            Rules.AddEffect(new Effect("energy drained")
                // A toggle isn't strictly necessary here since all the values will be zero if the character has no negative levels
                // It's included anyway so that it's easier to exclude it from the effects list in the frontend
                .SetToggle(new ConditionalToggle(stats[NEGATIVE_LEVELS], Relation.GREATER_THAN, 0))
                .AddAttackModifier(AttackClass.ALL, new Modifier(negativeLevels, UNTYPED))
                .AddModifier(ALL_SAVES, new Modifier(negativeLevels, UNTYPED))
                .AddModifier(ALL_SKILLS, new Modifier(negativeLevels, UNTYPED))
                .AddModifier(ABILITY_CHECKS, new Modifier(negativeLevels, UNTYPED))
                .AddModifier(MAX_HP, new Modifier(-5, stats[NEGATIVE_LEVELS], UNTYPED))
                .AddText(MISC_CASTING, "-1 effective spellcasting level, lose a highest-level spell slot")
            );

            Rules.AddEffect(new Effect(CANT_RUN_OR_CHARGE)
                .SetToggle(new FlagToggle(CANT_RUN_OR_CHARGE))
                .AddText(MOVEMENT, CANT_RUN_OR_CHARGE)
            );

            Rules.AddEffect(new Effect("entangled")
                .Controllable()
                .AddMultiplier(ALL_SPEEDS, new Multiplier(0.5m))
                .AddFlag(CANT_RUN_OR_CHARGE)
                .AddAttackModifier(AttackClass.ALL, new Modifier(-2, UNTYPED))
                .AddModifier(DEX_SCORE, new Modifier(-4, UNTYPED))
                .AddText(MISC_CASTING, "Spellcasting requires DC 15 concentration, or lose spell")
            );

            Rules.AddEffect(new Effect("exhausted")
                .Controllable()
                .AddMultiplier(ALL_SPEEDS, new Multiplier(0.5m))
                .AddFlag(CANT_RUN_OR_CHARGE) // exhausted doesn't actually say this but fatigued does.
                .AddModifier(STR_SCORE, new Modifier(-6, UNTYPED))
                .AddModifier(DEX_SCORE, new Modifier(-6, UNTYPED))
                .AddText(CONDITION, "Rest one hour to be fatigued instead of exhausted")
            );

            Rules.AddEffect(new Effect("fascinated")
                .Controllable()
                .AddFlag(CANT_ACT)
                .AddModifier(LISTEN, new Modifier(-4, UNTYPED))
                .AddModifier(SPOT, new Modifier(-4, UNTYPED))
            );

            Rules.AddEffect(new Effect("fatigued")
                .Controllable()
                .AddFlag(CANT_RUN_OR_CHARGE)
                .AddModifier(STR_SCORE, new Modifier(-2, UNTYPED))
                .AddModifier(DEX_SCORE, new Modifier(-2, UNTYPED))
            );

            Rules.AddEffect(new Effect("flat-footed")
                .Controllable()
                .AddFlag(FLAT_FOOTED)
                .AddText(ACTIONS, "Can't take immediate actions or attacks of opportunity")
            );

            Rules.AddEffect(new Effect("invisible")
                .Controllable()
                .AddAttackModifier(AttackClass.ALL, new Modifier(+2, UNTYPED))
                .AddText(ATK_OPTIONS, "Deny opponents' DEX to AC")
            );

            Rules.AddEffect(new Effect("nauseated")
                .Controllable()
                .AddText(ATK_OPTIONS, "Can't attack")
                .AddText(MISC_CASTING, "Can't cast or concentrate on spells")
                .AddText(ACTIONS, "Can't do anything requiring attention")
                .AddText(ACTIONS, "Only one move action each turn")
            );

            Rules.AddEffect(new Effect("prone")
                .Controllable()
                .AddAttackModifier(AttackClass.ALL, new Modifier(-4, UNTYPED))
                .AddText(ATK_OPTIONS, "Can't ranged weapons other than crossbows")
                .AddText(AC, "+4 vs ranged; -4 vs melee")
            );

            Rules.AddEffect(new Effect("sickened")
                .Controllable()
                .AddAttackModifier(AttackClass.ALL, new Modifier(-2, UNTYPED))
                .AddDamageModifier(AttackClass.ALL, new Modifier(-2, UNTYPED))
                .AddModifier(ALL_SAVES, new Modifier(-2, UNTYPED))
                .AddModifier(ALL_SKILLS, new Modifier(-2, UNTYPED))
                .AddModifier(ABILITY_CHECKS, new Modifier(-2, UNTYPED))
            );

            Rules.AddEffect(new Effect("staggered")
                .SetToggle(new ConditionalToggle(
                    new VariableAmount(stats[NONLETHAL_DAMAGE]),
                    Relation.EQUAL,
                    new VariableAmount(stats[CURRENT_HP])
                ))
                .AddText(ACTIONS, "One move or standard each round (not both, no full-rounds)")
            );

            Rules.AddEffect(new Effect("stunned")
                .Controllable()
                .AddModifier(AC, new Modifier(-2, UNTYPED))
                .AddFlag(DENIED_DEX)
                .AddFlag(CANT_ACT)
                .AddText(ACTIONS, "Must drop all held items")
            );

            IToggle unconscious = new AnyToggle(
                new ConditionalToggle(stats[CURRENT_HP], Relation.LESS_THAN, 0),
                new ConditionalToggle(
                    new VariableAmount(stats[NONLETHAL_DAMAGE]),
                    Relation.GREATER_THAN,
                    new VariableAmount(stats[CURRENT_HP])
                )
            );
            Rules.AddEffect(new Effect("unconscious").SetToggle(unconscious));
            Rules.AddEffect(new Effect(HELPLESS_AND_PARALYZED)
                // There's no real effect here; setting STR and DEX to zero in the same effect where
                // we check STR and DEX causes an infinite stack overflow.
                .SetToggle(new AnyToggle(
                    new ControllableToggle(HELPLESS_AND_PARALYZED),
                    new ConditionalToggle(stats[STR_SCORE], Relation.EQUAL, 0),
                    new ConditionalToggle(stats[DEX_SCORE], Relation.EQUAL, 0),
                    unconscious
                ))
                .AddFlag(CANT_ACT)
                .AddFlag(HELPLESS_AND_PARALYZED)
            );

            Rules.AddEffect(new Effect("DEX to AC")
                .AddModifier(AC, new Modifier(stats[DEX_MODIFIER], DODGE))
                .SetToggle(new AnyToggle(
                    // DEX always applies if it's negative
                    new ConditionalToggle(stats[DEX_MODIFIER], Relation.LESS_THAN, 0),
                    // Otherwise check if we're denied our dex bonus -- flat-footed without uncanny, or hard denied
                    new UnToggle(new AnyToggle(
                        new FlagToggle(DENIED_DEX),
                        new AllToggle(
                            new FlagToggle(FLAT_FOOTED),
                            new UnToggle(new FlagToggle(UNCANNY_DODGE))
                        )
                    ))
                ))
            );
        }

        private void AbilityScore(string ability) {
            Statistic abilityScore = new Statistic(ability + " score").Minimum(0);
            AddStat(abilityScore);

            Statistic abilityDamage = new Statistic(ability + " damage");
            AddStat(abilityDamage);
            Rules.AddEffect(
                new Effect(ability + " damage")
                    .AddModifier(ability + " score", new Modifier(-1, stats[ability + " damage"], ABILITY_MODIFIER))
            );
            Statistic abilityDrain = new Statistic(ability + " drain");
            AddStat(abilityDrain);
            Rules.AddEffect(
                new Effect(ability + " drain")
                    .AddModifier(ability + " score", new Modifier(-1, stats[ability + " drain"], ABILITY_MODIFIER))
            );

            string abilityModifierName = ability + " modifier";
            Rules.AddEffect(
                new Effect(ability + " modifier")
                    .AddModifier(abilityModifierName, new Modifier(BASE)
                        .AddAmount(-5)
                        .AddAmount(0.5m, abilityScore)
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
                    .AddModifier(abilityBonus.Name, new Modifier(
                        new GreaterAmount(new FixedAmount(0), new VariableAmount(abilityModifier)),
                        BASE
                    ))
            );

            Statistic abilityPenalty = new Statistic(ability + " penalty");
            AddStat(abilityPenalty);
            Rules.AddEffect(
                new Effect(ability + " penalty")
                    .AddModifier(abilityPenalty.Name, new Modifier(
                        new LesserAmount(new FixedAmount(0), new VariableAmount(abilityModifier)),
                        BASE
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
                new Effect(skillRanksName)
                    .AddModifier(skill, new Modifier(skillRanks, BASE))
            );
            Rules.AddEffect(
                new Effect(ability + " to " + skill)
                    .AddModifier(skill, new Modifier(abilityModifiers[ability], ABILITY_MODIFIER))
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
                    .AddModifier(saveName, new Modifier(UNTYPED)
                        .AddAmount(new VariableAmount(baseSave))
                        .AddAmount(new VariableAmount(abilityModifiers[ability]))
                    )
            );
        }

        private void SkillSynergy(string fiveRanksIn, params string[] givesPlusTwoOn) {
            Statistic rankSkill = stats[fiveRanksIn + RANKS];
            Effect synergy = new Effect(fiveRanksIn + " synergy")
                .SetToggle(new ConditionalToggle(rankSkill, Relation.GREATER_THAN_OR_EQUAL, 5));
            foreach (string skill in givesPlusTwoOn) {
                synergy.AddModifier(skill, new Modifier(+2, UNTYPED));
            }
            Rules.AddEffect(synergy);
        }
    }
}
