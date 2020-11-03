using System.Collections.Generic;
using PlayMore35.Effects;

using static PlayMore35.GameRules.Constants;

namespace PlayMore35.GameRules {
    // This entire file will go away once serialization is in a solidly usable state --
    // it's only here to construct the core rules of 3.5, which should be the job
    // of the JSON loading system once that's done.
    public static class GameRules {
        public static EffectPool MakeTheRules() {
            EffectPool Rules = new EffectPool();

            AbilityScore(Rules, STR);
            AbilityScore(Rules, DEX);
            AbilityScore(Rules, CON);
            AbilityScore(Rules, INT);
            AbilityScore(Rules, WIS);
            AbilityScore(Rules, CHA);
            Rules.AddStat(new Statistic(INITIATIVE, Rules.GetStat(DEX_CHECKS)));

            Rules.AddStat(new Statistic(MAX_HP));

            Rules.AddStat(new Statistic(CURRENT_HP));
            Rules.AddEffect(new Effect("Normal hit point total")
                .AddModifier(CURRENT_HP, new Modifier((MAX_HP), BASE))
            );

            Rules.AddStat(new Statistic(LETHAL_DAMAGE));
            Rules.AddEffect(new Effect("Subtract damage from hit points")
                // this is using ability modifier just to ensure it shows up after BASE.
                .AddModifier(CURRENT_HP, new Modifier(-1, (LETHAL_DAMAGE), ABILITY_MODIFIER))
            );

            Rules.AddEffect(new Effect(CANT_ACT)
                .SetToggle(new FlagToggle(CANT_ACT))
                .AddText(ACTIONS, CANT_ACT)
            );

            // The UI should automatically clear this flag when you damage yourself to the negatives or heal out of them.
            ControllableToggle stable = new ControllableToggle("stable");
            Rules.AddEffect(new Effect("disabled")
                .SetToggle(new AnyToggle(
                    new ConditionalToggle((CURRENT_HP), Relation.EQUAL, 0),
                    new AllToggle(
                        new ConditionalToggle((CURRENT_HP), Relation.LESS_THAN, 0),
                        stable
                    )
                ))
                .AddText(ACTIONS, "One move or standard each round (not both, no full-rounds)")
                .AddMultiplier(ALL_SPEEDS, new Multiplier(0.5m))
                .AddText(ACTIONS, "Any standard or strenuous action deals 1 to you")
            );

            Rules.AddEffect(new Effect("dying")
                .SetToggle(new AllToggle(
                    new ConditionalToggle((CURRENT_HP), Relation.LESS_THAN, 0),
                    new UnToggle(stable)
                ))
                .AddFlag(CANT_ACT)
                .AddText(CONDITION, "You take 1 damage each round")
                .AddText(CONDITION, "Any healing stabilizes you; 10% chance each round to self-stabilize")
            );

            Rules.AddStat(new Statistic(NONLETHAL_DAMAGE));

            SkillModifier(Rules, APPRAISE, INT);
            SkillModifier(Rules, AUTOHYPNOSIS, WIS);
            SkillModifier(Rules, BALANCE, DEX);
            SkillModifier(Rules, BLUFF, CHA);
            SkillModifier(Rules, CLIMB, STR);
            SkillModifier(Rules, CONCENTRATION, CON);
            SkillModifier(Rules, CRAFT, INT);
            SkillModifier(Rules, DECIPHER_SCRIPT, INT);
            SkillModifier(Rules, DIPLOMACY, CHA);
            SkillModifier(Rules, DISABLE_DEVICE, INT);
            SkillModifier(Rules, DISGUISE, CHA);
            SkillModifier(Rules, ESCAPE_ARTIST, DEX);
            SkillModifier(Rules, FORGERY, INT);
            SkillModifier(Rules, GATHER_INFORMATION, CHA);
            SkillModifier(Rules, HANDLE_ANIMAL, CHA);
            SkillModifier(Rules, HEAL, WIS);
            SkillModifier(Rules, HIDE, DEX);
            SkillModifier(Rules, IAIJUTSU_FOCUS, CHA);
            SkillModifier(Rules, INTIMIDATE, CHA);
            SkillModifier(Rules, JUMP, STR);
            SkillModifier(Rules, KNOWLEDGE_ARCANA, INT);
            SkillModifier(Rules, KNOWLEDGE_ARCHITECTURE_AND_ENGINEERING, INT);
            SkillModifier(Rules, KNOWLEDGE_DUNGEONEERING, INT);
            SkillModifier(Rules, KNOWLEDGE_GEOGRAPHY, INT);
            SkillModifier(Rules, KNOWLEDGE_HISTORY, INT);
            SkillModifier(Rules, KNOWLEDGE_LOCAL, INT);
            SkillModifier(Rules, KNOWLEDGE_NATURE, INT);
            SkillModifier(Rules, KNOWLEDGE_NOBILITY_AND_ROYALTY, INT);
            SkillModifier(Rules, KNOWLEDGE_PSIONICS, INT);
            SkillModifier(Rules, KNOWLEDGE_RELIGION, INT);
            SkillModifier(Rules, KNOWLEDGE_THE_PLANES, INT);
            SkillModifier(Rules, MARTIAL_LORE, INT);
            SkillModifier(Rules, LISTEN, WIS);
            SkillModifier(Rules, MOVE_SILENTLY, DEX);
            SkillModifier(Rules, OPEN_LOCK, DEX);
            SkillModifier(Rules, PERFORM_ACT, CHA);
            SkillModifier(Rules, PERFORM_COMEDY, CHA);
            SkillModifier(Rules, PERFORM_DANCE, CHA);
            SkillModifier(Rules, PERFORM_KEYBOARD_INSTRUMENTS, CHA);
            SkillModifier(Rules, PERFORM_ORATORY, CHA);
            SkillModifier(Rules, PERFORM_PERCUSSION_INSTRUMENTS, CHA);
            SkillModifier(Rules, PERFORM_STRING_INSTRUMENTS, CHA);
            SkillModifier(Rules, PERFORM_WIND_INSTRUMENTS, CHA);
            SkillModifier(Rules, PERFORM_SING, CHA);
            SkillModifier(Rules, PROFESSION, WIS);
            SkillModifier(Rules, PSICRAFT, INT);
            SkillModifier(Rules, RIDE, DEX);
            SkillModifier(Rules, SEARCH, INT);
            SkillModifier(Rules, SENSE_MOTIVE, WIS);
            SkillModifier(Rules, SLEIGHT_OF_HAND, DEX);
            SkillModifier(Rules, SPELLCRAFT, INT);
            SkillModifier(Rules, SPOT, WIS);
            SkillModifier(Rules, SURVIVAL, WIS);
            SkillModifier(Rules, SWIM, STR);
            SkillModifier(Rules, TRUESPEAK, INT);
            SkillModifier(Rules, TUMBLE, DEX);
            SkillModifier(Rules, USE_MAGIC_DEVICE, CHA);
            SkillModifier(Rules, USE_PSIONIC_DEVICE, CHA);
            SkillModifier(Rules, USE_ROPE, DEX);
            Rules.AddStat(new Statistic(CLIMB_ROPE,                     Rules.GetStat(CLIMB)));
            Rules.AddStat(new Statistic(DISGUISE_ACT_IN_CHARACTER,      Rules.GetStat(DISGUISE)));
            Rules.AddStat(new Statistic(ESCAPE_ARTIST_ROPE,             Rules.GetStat(ESCAPE_ARTIST)));
            Rules.AddStat(new Statistic(PSICRAFT_ADDRESS_POWER_STONE,   Rules.GetStat(PSICRAFT)));
            Rules.AddStat(new Statistic(SEARCH_SECRET_DOORS,            Rules.GetStat(SEARCH)));
            Rules.AddStat(new Statistic(SPELLCRAFT_DECIPHER_SCROLL,     Rules.GetStat(SPELLCRAFT)));
            Rules.AddStat(new Statistic(SURVIVAL_ABOVEGROUND,           Rules.GetStat(SURVIVAL)));
            Rules.AddStat(new Statistic(SURVIVAL_FOLLOW_TRACKS,         Rules.GetStat(SURVIVAL)));
            Rules.AddStat(new Statistic(SURVIVAL_LOST,                  Rules.GetStat(SURVIVAL)));
            Rules.AddStat(new Statistic(SURVIVAL_PLANAR,                Rules.GetStat(SURVIVAL)));
            Rules.AddStat(new Statistic(SURVIVAL_UNDERGROUND,           Rules.GetStat(SURVIVAL)));
            Rules.AddStat(new Statistic(USE_MAGIC_DEVICE_SCROLL,        Rules.GetStat(USE_MAGIC_DEVICE)));
            Rules.AddStat(new Statistic(USE_PSIONIC_DEVICE_POWER_STONE, Rules.GetStat(USE_PSIONIC_DEVICE)));
            Rules.AddStat(new Statistic(USE_ROPE_BINDINGS,              Rules.GetStat(USE_ROPE)));

            SkillSynergy(Rules, BLUFF, DIPLOMACY, DISGUISE_ACT_IN_CHARACTER, INTIMIDATE, SLEIGHT_OF_HAND);
            SkillSynergy(Rules, DECIPHER_SCRIPT, USE_MAGIC_DEVICE_SCROLL);
            SkillSynergy(Rules, ESCAPE_ARTIST, USE_ROPE_BINDINGS);
            SkillSynergy(Rules, HANDLE_ANIMAL, RIDE);
            SkillSynergy(Rules, JUMP, TUMBLE);
            SkillSynergy(Rules, KNOWLEDGE_ARCANA, SPELLCRAFT);
            SkillSynergy(Rules, KNOWLEDGE_ARCHITECTURE_AND_ENGINEERING, SEARCH_SECRET_DOORS);
            SkillSynergy(Rules, KNOWLEDGE_DUNGEONEERING, SURVIVAL_UNDERGROUND);
            SkillSynergy(Rules, KNOWLEDGE_GEOGRAPHY, SURVIVAL_LOST);
            SkillSynergy(Rules, KNOWLEDGE_LOCAL, GATHER_INFORMATION);
            SkillSynergy(Rules, KNOWLEDGE_NATURE, SURVIVAL_ABOVEGROUND);
            SkillSynergy(Rules, KNOWLEDGE_NOBILITY_AND_ROYALTY, DIPLOMACY);
            SkillSynergy(Rules, KNOWLEDGE_THE_PLANES, SURVIVAL_PLANAR);
            SkillSynergy(Rules, SEARCH, SURVIVAL_FOLLOW_TRACKS);
            SkillSynergy(Rules, SENSE_MOTIVE, DIPLOMACY);
            SkillSynergy(Rules, SPELLCRAFT, USE_MAGIC_DEVICE_SCROLL);
            SkillSynergy(Rules, SURVIVAL, KNOWLEDGE_NATURE);
            SkillSynergy(Rules, TUMBLE, BALANCE, JUMP);
            SkillSynergy(Rules, USE_MAGIC_DEVICE, SPELLCRAFT_DECIPHER_SCROLL);
            SkillSynergy(Rules, USE_ROPE, CLIMB_ROPE, ESCAPE_ARTIST_ROPE);

            SkillSynergy(Rules, AUTOHYPNOSIS, KNOWLEDGE_PSIONICS);
            SkillSynergy(Rules, CONCENTRATION, AUTOHYPNOSIS);
            SkillSynergy(Rules, KNOWLEDGE_PSIONICS, PSICRAFT);
            SkillSynergy(Rules, PSICRAFT, USE_PSIONIC_DEVICE_POWER_STONE);
            SkillSynergy(Rules, USE_PSIONIC_DEVICE, PSICRAFT_ADDRESS_POWER_STONE);

            // given how utterly insanely hard the truename research use of the knowledge skill is,
            // i'm assuming no one will ever do it, and therefore not including the intricate web of
            // synergies associated with it.

            Rules.AddStat(new Statistic(ARMOR_CHECK_PENALTY));
            Effect acp = new Effect(ARMOR_CHECK_PENALTY);
            foreach (string skill in new string[] { BALANCE, CLIMB, ESCAPE_ARTIST, HIDE, JUMP, MOVE_SILENTLY, SLEIGHT_OF_HAND }) {
                acp.AddModifier(skill, new Modifier((ARMOR_CHECK_PENALTY), UNTYPED));
            }
            acp.AddModifier(SWIM, new Modifier(2, (ARMOR_CHECK_PENALTY), UNTYPED));
            Rules.AddEffect(acp);

            Statistic ac = new Statistic(AC);
            Statistic touchAC = new Statistic(TOUCH_AC, ac)
                .IgnoreModifierType(ARMOR)
                .IgnoreModifierType(SHIELD)
                .IgnoreModifierType(NATURAL_ARMOR);
            Rules.AddStat(ac);
            Rules.AddStat(touchAC);

            Rules.AddEffect(new Effect("Base AC")
                .AddModifier(AC, new Modifier(10, BASE))
            );

            Rules.AddStat(new Statistic(NATURAL_ARMOR)); // this is the same name as the bonus type. eh.
            Rules.AddEffect(new Effect(NATURAL_ARMOR)
                .AddModifier(AC, new Modifier((NATURAL_ARMOR), NATURAL_ARMOR))
            );


            Rules.AddStat(new Statistic(LAND_SPEED).AddTag(ALL_SPEEDS).RoundTo(5));
            Rules.AddStat(new Statistic(BURROW_SPEED).AddTag(ALL_SPEEDS).RoundTo(5));
            Rules.AddStat(new Statistic(CLIMB_SPEED).AddTag(ALL_SPEEDS).RoundTo(5));
            Rules.AddStat(new Statistic(SWIM_SPEED).AddTag(ALL_SPEEDS).RoundTo(5));
            Rules.AddStat(new Statistic(FLY_SPEED).AddTag(ALL_SPEEDS).RoundTo(5));
            Rules.AddStat(new Statistic(MANEUVERABILITY));

            Rules.AddEffect(new Effect("Encumbrance speed penalty")
                .Controllable()
                .AddMultiplier(ALL_SPEEDS, new Multiplier(0.666m))
            );

            // ... yeah, this jumps (heh) through some hoops.  nothing to see here, move along.
            // (i'm purposefully abusing integer math to get rounding on these amounts, and doing amount stacking via
            // statistic chaining to do divide-subtract-multiply.  sue me :P )
            string excessSpeed = "speed over 30 ft, in units of 10 ft";
            Rules.AddStat(new Statistic(excessSpeed));
            Rules.AddEffect(new Effect(excessSpeed)
                .AddModifier(excessSpeed, new Modifier(UNTYPED)
                    .AddAmount(0.1m, (LAND_SPEED))
                    .AddAmount(-3)
                )
            );
            string lackingSpeed = "speed under 30 ft, in units of 10 ft";
            Rules.AddStat(new Statistic(lackingSpeed));
            Rules.AddEffect(new Effect(lackingSpeed)
                .AddModifier(lackingSpeed, new Modifier(UNTYPED)
                    .AddAmount(-0.1m, (LAND_SPEED))
                    .AddAmount(+3)
                )
            );

            Rules.AddEffect(new Effect("Speed penalty")
                // the rule says "less than 30", but it doesn't actually do anything unless you're at 20 or under.
                .SetToggle(new ConditionalToggle((LAND_SPEED), Relation.LESS_THAN_OR_EQUAL, 20))
                .AddModifier(JUMP, new Modifier(-6, (lackingSpeed), UNTYPED))
            );
            Rules.AddEffect(new Effect("Speed bonus")
                // the rule stays "more than 30", but it doesn't actually do anything unless you're at 40 or over.
                .SetToggle(new ConditionalToggle((LAND_SPEED), Relation.GREATER_THAN_OR_EQUAL, 40))
                .AddModifier(JUMP, new Modifier(+4, (excessSpeed), UNTYPED))
            );

            // Medium at 0 makes a lot of calculations more smooth
            Rules.AddStat(new Statistic(SIZE_CATEGORY).Minimum(-4).Maximum(+4));

            Rules.AddStat(new Statistic(SIZE_MODIFIER));
            Rules.AddEffect(new Effect(SIZE_MODIFIER + " calculation")
                .AddModifier(SIZE_MODIFIER, new Modifier(
                    new LookupAmount(
                        (SIZE_CATEGORY),
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
                .AddAttackModifier(AttackClass.ALL, new Modifier((SIZE_MODIFIER), SIZE))
                .AddModifier(AC, new Modifier((SIZE_MODIFIER), SIZE))
            );

            // for now (or maybe forever) i'm not dealing with the fact that there's a long/tall distinction, or the fact
            // that "some exceptions exist" to these values.  for player characters i'm assuming tall, and i'm assuming that
            // space never changes, and i'm assuming that if reach changes it's via an effect not a base reach difference.
            // if this is not the case... i'll just let the player say what these values are and remove these effects.

            Rules.AddStat(new Statistic(REACH).Minimum(0));
            Rules.AddEffect(new Effect("Reach based on size")
                .AddModifier(REACH, new Modifier(
                    new LookupAmount((SIZE_CATEGORY),
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

            Rules.AddStat(new Statistic(BASE_ATTACK_BONUS));
            Rules.AddStat(new Statistic(GRAPPLE));
            Rules.AddEffect(new Effect("STR to grapple")
                .AddModifier(GRAPPLE, new Modifier((STR_MODIFIER), ABILITY_MODIFIER))
            );

            Rules.AddEffect(new Effect(BASE_ATTACK_BONUS)
                .AddAttackModifier(AttackClass.ALL, new Modifier((BASE_ATTACK_BONUS), BASE))
                .AddModifier(GRAPPLE, new Modifier((BASE_ATTACK_BONUS), BASE))
            );

            Rules.AddStat(new Statistic(SPECIAL_SIZE_MODIFIER));
            Rules.AddEffect(new Effect(SPECIAL_SIZE_MODIFIER + " calculation")
                .AddModifier(SPECIAL_SIZE_MODIFIER, new Modifier(4, (SIZE_CATEGORY), BASE))
            );
            Rules.AddEffect(new Effect(SPECIAL_SIZE_MODIFIER)
                .AddModifier(HIDE, new Modifier(-1, (SPECIAL_SIZE_MODIFIER), SIZE))
                .AddModifier(GRAPPLE, new Modifier((SPECIAL_SIZE_MODIFIER), SIZE))
            );

            Rules.AddEffect(new Effect("DEX to ranged attacks")
                .AddAttackModifier(new AttackClass().Ranged(), new Modifier((DEX_MODIFIER), ABILITY_MODIFIER))
            );
            // Weapon Finesse will use ability-substitution technology
            Rules.AddEffect(new Effect("STR to melee attacks")
                .AddAttackModifier(new AttackClass().Melee(), new Modifier((STR_MODIFIER), ABILITY_MODIFIER))
            );

            Rules.AddEffect(new Effect("STR bonus ×1.5 to two-handed weapon damage")
                .AddDamageModifier(
                    new AttackClass().Melee().TwoHandedAttack(),
                    new Modifier(1.5m, (STR_BONUS), ABILITY_MODIFIER)
                )
            );
            Rules.AddEffect(new Effect("STR bonus to primary-hand weapon damage")
                .AddDamageModifier(new AttackClass().Melee().PrimaryHandAttack(), new Modifier((STR_BONUS), ABILITY_MODIFIER))
            );
            Rules.AddEffect(new Effect("STR bonus ×0.5 to off-hand weapon damage")
                .AddDamageModifier(
                    new AttackClass().Melee().OffHandAttack(),
                    new Modifier(0.5m, (STR_BONUS), ABILITY_MODIFIER)
                )
            );

            Rules.AddEffect(new Effect("STR penalty to bow damage")
                .AddDamageModifier(new AttackClass().Bow(), new Modifier((STR_PENALTY), ABILITY_MODIFIER))
            );
            Rules.AddEffect(new Effect("STR to thrown weapon damage")
                .AddDamageModifier(new AttackClass().Thrown(), new Modifier((STR_MODIFIER), ABILITY_MODIFIER))
            );
            Rules.AddEffect(new Effect("STR to sling damage")
                .AddDamageModifier(new AttackClass().Name("Sling"), new Modifier((STR_MODIFIER), ABILITY_MODIFIER))
            );

            Rules.AddStat(new Statistic(FEAR_LEVEL).Minimum(0).Maximum(3));
            Rules.AddEffect(new Effect(SHAKEN)
                .SetToggle(new ConditionalToggle((FEAR_LEVEL), Relation.GREATER_THAN_OR_EQUAL, 1))
                .AddAttackModifier(AttackClass.ALL, new Modifier(-2, UNTYPED))
                .AddModifier(ALL_SAVES, new Modifier(-2, UNTYPED))
                .AddModifier(ALL_SKILLS, new Modifier(-2, UNTYPED))
                .AddModifier(ABILITY_CHECKS, new Modifier(-2, UNTYPED))
            );
            Rules.AddEffect(new Effect(FRIGHTENED)
                .SetToggle(new ConditionalToggle((FEAR_LEVEL), Relation.EQUAL, 2))
                .AddText(ACTIONS, "Must flee from source of fear")
            );
            Rules.AddEffect(new Effect(PANICKED)
                .SetToggle(new ConditionalToggle((FEAR_LEVEL), Relation.EQUAL, 3))
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

            Rules.AddStat(new Statistic(NEGATIVE_LEVELS));
            IAmount negativeLevels = new MultiplyAmount(-1, (NEGATIVE_LEVELS));
            Rules.AddEffect(new Effect("energy drained")
                // A toggle isn't strictly necessary here since all the values will be zero if the character has no negative levels
                // It's included anyway so that it's easier to exclude it from the effects list in the frontend
                .SetToggle(new ConditionalToggle((NEGATIVE_LEVELS), Relation.GREATER_THAN, 0))
                .AddAttackModifier(AttackClass.ALL, new Modifier(negativeLevels, UNTYPED))
                .AddModifier(ALL_SAVES, new Modifier(negativeLevels, UNTYPED))
                .AddModifier(ALL_SKILLS, new Modifier(negativeLevels, UNTYPED))
                .AddModifier(ABILITY_CHECKS, new Modifier(negativeLevels, UNTYPED))
                .AddModifier(MAX_HP, new Modifier(-5, (NEGATIVE_LEVELS), UNTYPED))
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
                    new VariableAmount((NONLETHAL_DAMAGE)),
                    Relation.EQUAL,
                    new VariableAmount((CURRENT_HP))
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
                new ConditionalToggle((CURRENT_HP), Relation.LESS_THAN, 0),
                new ConditionalToggle(
                    new VariableAmount((NONLETHAL_DAMAGE)),
                    Relation.GREATER_THAN,
                    new VariableAmount((CURRENT_HP))
                )
            );
            Rules.AddEffect(new Effect("unconscious").SetToggle(unconscious));
            Rules.AddEffect(new Effect(HELPLESS_AND_PARALYZED)
                // There's no real effect here; setting STR and DEX to zero in the same effect where
                // we check STR and DEX causes an infinite stack overflow.
                .SetToggle(new AnyToggle(
                    new ControllableToggle(HELPLESS_AND_PARALYZED),
                    new ConditionalToggle((STR_SCORE), Relation.EQUAL, 0),
                    new ConditionalToggle((DEX_SCORE), Relation.EQUAL, 0),
                    unconscious
                ))
                .AddFlag(CANT_ACT)
                .AddFlag(HELPLESS_AND_PARALYZED)
            );

            Rules.AddEffect(new Effect("DEX to AC")
                .AddModifier(AC, new Modifier((DEX_MODIFIER), DODGE))
                .SetToggle(new AnyToggle(
                    // DEX always applies if it's negative
                    new ConditionalToggle((DEX_MODIFIER), Relation.LESS_THAN, 0),
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

            return Rules;
        }

        private static void AbilityScore(EffectPool Rules, string ability) {
            Statistic abilityScore = new Statistic(ability + " score").Minimum(0);
            Rules.AddStat(abilityScore);

            Statistic abilityDamage = new Statistic(ability + " damage");
            Rules.AddStat(abilityDamage);
            Rules.AddEffect(
                new Effect(ability + " damage")
                    .AddModifier(ability + " score", new Modifier(-1, (ability + " damage"), ABILITY_MODIFIER))
            );
            Statistic abilityDrain = new Statistic(ability + " drain");
            Rules.AddStat(abilityDrain);
            Rules.AddEffect(
                new Effect(ability + " drain")
                    .AddModifier(ability + " score", new Modifier(-1, (ability + " drain"), ABILITY_MODIFIER))
            );

            string abilityModifierName = ability + " modifier";
            Rules.AddEffect(
                new Effect(ability + " modifier")
                    .AddModifier(abilityModifierName, new Modifier(BASE)
                        .AddAmount(-5)
                        .AddAmount(0.5m, abilityScore.Name)
                    )
            );

            Statistic abilityModifier = new Statistic(abilityModifierName);
            Rules.AddStat(abilityModifier);
            Statistic checks = new Statistic(ability + " checks", abilityModifier)
                .AddTag(ABILITY_CHECKS);
            Rules.AddStat(checks);

            Statistic abilityBonus = new Statistic(ability + " bonus");
            Rules.AddStat(abilityBonus);
            Rules.AddEffect(
                new Effect(ability + " bonus")
                    .AddModifier(abilityBonus.Name, new Modifier(
                        new GreaterAmount(new FixedAmount(0), new VariableAmount(abilityModifierName)),
                        BASE
                    ))
            );

            Statistic abilityPenalty = new Statistic(ability + " penalty");
            Rules.AddStat(abilityPenalty);
            Rules.AddEffect(
                new Effect(ability + " penalty")
                    .AddModifier(abilityPenalty.Name, new Modifier(
                        new LesserAmount(new FixedAmount(0), new VariableAmount(abilityModifierName)),
                        BASE
                    ))
            );
        }

        private static void SkillModifier(EffectPool Rules, string skill, string ability) {
            string skillRanksName = skill + " ranks";
            Statistic skillRanks = new Statistic(skillRanksName);
            Rules.AddStat(skillRanks);

            Rules.AddStat(new Statistic(skill)
                .AddTag(ALL_SKILLS)
                .AddTag(ability + " skills")
            );

            Rules.AddEffect(
                new Effect(skillRanksName)
                    .AddModifier(skill, new Modifier(skillRanksName, BASE))
            );
            Rules.AddEffect(
                new Effect(ability + " to " + skill)
                    .AddModifier(skill, new Modifier((ability + " modifier"), ABILITY_MODIFIER))
            );
        }

        private static void SaveModifier(EffectPool Rules, string save, string ability) {
            string saveName = save + " saves";
            string baseSaveName = "Base " + save;
            Statistic baseSave = new Statistic(baseSaveName);
            Rules.AddStat(baseSave);

            Rules.AddStat(new Statistic(saveName));
            Rules.AddEffect(
                new Effect("Total " + save + " modifier")
                    .AddModifier(saveName, new Modifier(UNTYPED)
                        .AddAmount(new VariableAmount(baseSaveName))
                        .AddAmount(new VariableAmount((ability + " modifier")))
                    )
            );
        }

        private static void SkillSynergy(EffectPool Rules, string fiveRanksIn, params string[] givesPlusTwoOn) {
            Statistic rankSkill = Rules.GetStat(fiveRanksIn + RANKS);
            Effect synergy = new Effect(fiveRanksIn + " synergy")
                .SetToggle(new ConditionalToggle(rankSkill.Name, Relation.GREATER_THAN_OR_EQUAL, 5));
            foreach (string skill in givesPlusTwoOn) {
                synergy.AddModifier(skill, new Modifier(+2, UNTYPED));
            }
            Rules.AddEffect(synergy);
        }
    }
}
