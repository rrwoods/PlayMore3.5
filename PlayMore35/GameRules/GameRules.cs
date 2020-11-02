using System;
using System.Collections.Generic;
using PlayMore35.Effects;

using static PlayMore35.GameRules.Constants;

namespace PlayMore35.GameRules {
    public class GameRules {
        public EffectPool Rules { get; private set; } = new EffectPool();

        // TODO : Stats should be private, with a GetStat(name) method, to ensure no one directly inserts into this dict
        public Dictionary<string, Statistic> Stats { get; } = new Dictionary<string, Statistic>();
        private readonly Dictionary<string, Statistic> abilityModifiers = new Dictionary<string, Statistic>();

        public Items.Weapon PrimaryHandWeapon { get; private set; } = null;
        public Items.Weapon OffHandWeapon { get; private set; } = null;

        public void AddStat(Statistic stat) {
            Stats[stat.Name] = stat;
        }

        public void NoWeapons() {
            PrimaryHandWeapon = null;
            OffHandWeapon = null;
        }

        public void OneHandOneWeapon(Items.Weapon w) {
            PrimaryHandWeapon = w;
            OffHandWeapon = null;
        }

        public void TwoHandOneWeapon(Items.Weapon w) {
            PrimaryHandWeapon = w;
            OffHandWeapon = w;
        }

        public void TwoWeapons(Items.Weapon primary, Items.Weapon off) {
            PrimaryHandWeapon = primary;
            OffHandWeapon = off;
        }

        // The default constructor sets up 3.5's official rules.
        public GameRules() {
            AbilityScore(STR);
            AbilityScore(DEX);
            AbilityScore(CON);
            AbilityScore(INT);
            AbilityScore(WIS);
            AbilityScore(CHA);
            AddStat(new Statistic(INITIATIVE, Stats[DEX_CHECKS]));

            AddStat(new Statistic(MAX_HP));

            AddStat(new Statistic(CURRENT_HP));
            Rules.AddEffect(new Effect("Normal hit point total")
                .AddModifier(CURRENT_HP, new Modifier(Stats[MAX_HP], BASE))
            );

            AddStat(new Statistic(LETHAL_DAMAGE));
            Rules.AddEffect(new Effect("Subtract damage from hit points")
                .AddModifier(CURRENT_HP, new Modifier(-1, Stats[LETHAL_DAMAGE], BASE))
            );

            Rules.AddEffect(new Effect(CANT_ACT)
                .SetToggle(new FlagToggle(CANT_ACT))
                .AddText(ACTIONS, CANT_ACT)
            );

            // The UI should automatically clear this flag when you damage yourself to the negatives or heal out of them.
            ControllableToggle stable = new ControllableToggle("stable");
            Rules.AddEffect(new Effect("disabled")
                .SetToggle(new AnyToggle(
                    new ConditionalToggle(Stats[CURRENT_HP], Relation.EQUAL, 0),
                    new AllToggle(
                        new ConditionalToggle(Stats[CURRENT_HP], Relation.LESS_THAN, 0),
                        stable
                    )
                ))
                .AddText(ACTIONS, "One move or standard each round (not both, no full-rounds)")
                .SetMultiplier(new Multiplier(0.5m, ALL_SPEEDS))
                .AddText(ACTIONS, "Any standard or strenuous action deals 1 to you")
            );

            Rules.AddEffect(new Effect("dying")
                .SetToggle(new AllToggle(
                    new ConditionalToggle(Stats[CURRENT_HP], Relation.LESS_THAN, 0),
                    new UnToggle(stable)
                ))
                .SetFlag(CANT_ACT)
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
            SkillModifier(TUMBLE, DEX);
            SkillModifier(USE_MAGIC_DEVICE, CHA);
            SkillModifier(USE_PSIONIC_DEVICE, CHA);
            SkillModifier(USE_ROPE, DEX);
            AddStat(new Statistic(DISGUISE_ACT_IN_CHARACTER,      Stats[DISGUISE]));
            AddStat(new Statistic(ESCAPE_ARTIST_ROPE,             Stats[ESCAPE_ARTIST]));
            AddStat(new Statistic(PSICRAFT_ADDRESS_POWER_STONE,   Stats[PSICRAFT]));
            AddStat(new Statistic(SEARCH_SECRET_DOORS,            Stats[SEARCH]));
            AddStat(new Statistic(SPELLCRAFT_DECIPHER_SCROLL,     Stats[SPELLCRAFT]));
            AddStat(new Statistic(SURVIVAL_LOST,                  Stats[SURVIVAL]));
            AddStat(new Statistic(SURVIVAL_ABOVEGROUND,           Stats[SURVIVAL]));
            AddStat(new Statistic(SURVIVAL_PLANAR,                Stats[SURVIVAL]));
            AddStat(new Statistic(SURVIVAL_FOLLOW_TRACKS,         Stats[SURVIVAL]));
            AddStat(new Statistic(USE_MAGIC_DEVICE_SCROLL,        Stats[USE_MAGIC_DEVICE]));
            AddStat(new Statistic(USE_PSIONIC_DEVICE_POWER_STONE, Stats[USE_PSIONIC_DEVICE]));
            AddStat(new Statistic(USE_ROPE_BINDINGS,              Stats[USE_ROPE]));

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
                    .AddModifier(SWIM, new Modifier(2, Stats[ARMOR_CHECK_PENALTY], UNTYPED))
            );
            // TODO : (after toggle-effects are in)
            // apply ACP these specific skills only if the character is proficient with their armor
            // apply ACP to all STR and DEX skills if the character is not proficient with their armor

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
                .AddModifier(AC, new Modifier(Stats[NATURAL_ARMOR], NATURAL_ARMOR))
            );


            AddStat(new Statistic(LAND_SPEED).AddTag(ALL_SPEEDS).RoundTo(5));
            AddStat(new Statistic(BURROW_SPEED).AddTag(ALL_SPEEDS).RoundTo(5));
            AddStat(new Statistic(CLIMB_SPEED).AddTag(ALL_SPEEDS).RoundTo(5));
            AddStat(new Statistic(SWIM_SPEED).AddTag(ALL_SPEEDS).RoundTo(5));
            AddStat(new Statistic(FLY_SPEED).AddTag(ALL_SPEEDS).RoundTo(5));
            AddStat(new Statistic(MANEUVERABILITY));

            Rules.AddEffect(new Effect("Encumbrance speed penalty")
                .Controllable() // TODO : automatically determine based on armor/inventory ... ugh, armor.
                .SetMultiplier(new Multiplier(0.666m, ALL_SPEEDS))
            );

            // ... yeah, this jumps (heh) through some hoops.  nothing to see here, move along.
            // (i'm purposefully abusing integer math to get rounding on these amounts, and doing amount stacking via
            // statistic chaining to do divide-subtract-multiply.  sue me :P )
            string excessSpeed = "speed over 30 ft, in units of 10 ft";
            AddStat(new Statistic(excessSpeed));
            Rules.AddEffect(new Effect(excessSpeed)
                .AddModifier(excessSpeed, new Modifier(UNTYPED)
                    .AddAmount(0.1m, Stats[LAND_SPEED])
                    .AddAmount(-3)
                )
            );
            string lackingSpeed = "speed under 30 ft, in units of 10 ft";
            AddStat(new Statistic(lackingSpeed));
            Rules.AddEffect(new Effect(lackingSpeed)
                .AddModifier(lackingSpeed, new Modifier(UNTYPED)
                    .AddAmount(-0.1m, Stats[LAND_SPEED])
                    .AddAmount(+3)
                )
            );

            Rules.AddEffect(new Effect("Speed penalty")
                // the rule says "less than 30", but it doesn't actually do anything unless you're at 20 or under.
                .SetToggle(new ConditionalToggle(Stats[LAND_SPEED], Relation.LESS_THAN_OR_EQUAL, 20))
                .AddModifier(JUMP, new Modifier(-6, Stats[lackingSpeed], UNTYPED))
            );
            Rules.AddEffect(new Effect("Speed bonus")
                // the rule stays "more than 30", but it doesn't actually do anything unless you're at 40 or over.
                .SetToggle(new ConditionalToggle(Stats[LAND_SPEED], Relation.GREATER_THAN_OR_EQUAL, 40))
                .AddModifier(JUMP, new Modifier(+4, Stats[excessSpeed], UNTYPED))
            );

            // Medium at 0 makes a lot of calculations more smooth
            AddStat(new Statistic(SIZE_CATEGORY).Minimum(-4).Maximum(+4));

            AddStat(new Statistic(SIZE_MODIFIER));
            Rules.AddEffect(new Effect(SIZE_MODIFIER + " calculation")
                .AddModifier(SIZE_MODIFIER, new Modifier(
                    new LookupAmount(
                        Stats[SIZE_CATEGORY],
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
                .AddAttackModifier(WeaponClass.ALL, new Modifier(Stats[SIZE_MODIFIER], SIZE))
                .AddModifier(AC, new Modifier(Stats[SIZE_MODIFIER], SIZE))
            );

            // for now (or maybe forever) i'm not dealing with the fact that there's a long/tall distinction, or the fact
            // that "some exceptions exist" to these values.  for player characters i'm assuming tall, and i'm assuming that
            // space never changes, and i'm assuming that if reach changes it's via an effect not a base reach difference.
            // if this is not the case... i'll just let the player say what these values are and remove these effects.

            // TODO : Space (it's a LookupText, basically)
            AddStat(new Statistic(REACH).Minimum(0));
            Rules.AddEffect(new Effect("Reach based on size")
                .AddModifier(REACH, new Modifier(
                    new LookupAmount(Stats[SIZE_CATEGORY],
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
                .AddModifier(GRAPPLE, new Modifier(Stats[STR_MODIFIER], BASE))
            );

            Rules.AddEffect(new Effect(BASE_ATTACK_BONUS)
                .AddAttackModifier(WeaponClass.ALL, new Modifier(Stats[BASE_ATTACK_BONUS], BASE))
                .AddModifier(GRAPPLE, new Modifier(Stats[BASE_ATTACK_BONUS], BASE))
            );

            AddStat(new Statistic(SPECIAL_SIZE_MODIFIER));
            Rules.AddEffect(new Effect(SPECIAL_SIZE_MODIFIER + " calculation")
                .AddModifier(SPECIAL_SIZE_MODIFIER, new Modifier(4, Stats[SIZE_CATEGORY], BASE))
            );
            Rules.AddEffect(new Effect(SPECIAL_SIZE_MODIFIER)
                .AddModifier(HIDE, new Modifier(-1, Stats[SPECIAL_SIZE_MODIFIER], SIZE))
                .AddModifier(GRAPPLE, new Modifier(Stats[SPECIAL_SIZE_MODIFIER], SIZE))
            );

            Rules.AddEffect(new Effect("DEX to ranged attacks")
                .AddAttackModifier(new WeaponClass().Ranged(), new Modifier(Stats[DEX_MODIFIER], BASE))
            );
            // Weapon Finesse will use ability-substitution technology
            Rules.AddEffect(new Effect("(STR) to melee attacks")
                .AddAttackModifier(new WeaponClass().Melee(), new Modifier(Stats[STR_MODIFIER], BASE))
            );

            Rules.AddEffect(new Effect("(STR ×1.5) to two-handed weapon damage")
                .AddDamageModifier(
                    new WeaponClass().Melee().TwoHandedAttack(),
                    new Modifier(1.5m, Stats[STR_BONUS], BASE)
                )
            );
            Rules.AddEffect(new Effect("(STR) to primary-hand weapon damage")
                .AddDamageModifier(new WeaponClass().Melee().PrimaryHandAttack(), new Modifier(Stats[STR_BONUS], BASE))
            );
            Rules.AddEffect(new Effect("(STR ×0.5) to off-hand weapon damage")
                .AddDamageModifier(
                    new WeaponClass().Melee().OffHandAttack(),
                    new Modifier(0.5m, Stats[STR_BONUS], BASE)
                )
            );

            Rules.AddEffect(new Effect("STR penalty to bow damage")
                .AddDamageModifier(new WeaponClass().Bow(), new Modifier(Stats[STR_PENALTY], BASE))
            );
            Rules.AddEffect(new Effect("STR to thrown weapon damage")
                .AddDamageModifier(new WeaponClass().Thrown(), new Modifier(Stats[STR_MODIFIER], BASE))
            );
            Rules.AddEffect(new Effect("STR to sling damage")
                .AddDamageModifier(new WeaponClass().Name("Sling"), new Modifier(Stats[STR_MODIFIER], BASE))
            );

            AddStat(new Statistic(FEAR_LEVEL).Minimum(0).Maximum(3));
            Rules.AddEffect(new Effect(SHAKEN)
                .SetToggle(new ConditionalToggle(Stats[FEAR_LEVEL], Relation.GREATER_THAN_OR_EQUAL, 1))
                .AddAttackModifier(WeaponClass.ALL, new Modifier(-2, UNTYPED))
                .AddModifier(ALL_SAVES, new Modifier(-2, UNTYPED))
                .AddModifier(ALL_SKILLS, new Modifier(-2, UNTYPED))
                .AddModifier(ABILITY_CHECKS, new Modifier(-2, UNTYPED))
            );
            Rules.AddEffect(new Effect(FRIGHTENED)
                .SetToggle(new ConditionalToggle(Stats[FEAR_LEVEL], Relation.EQUAL, 2))
                .AddText(ACTIONS, "Must flee from source of fear")
            );
            Rules.AddEffect(new Effect(PANICKED)
                .SetToggle(new ConditionalToggle(Stats[FEAR_LEVEL], Relation.EQUAL, 3))
                .AddText(ACTIONS, "Must drop all held items and flee at top speed along a random path; cower if cornered")
            );


            Rules.AddEffect(new Effect("blinded")
                .Controllable()
                .AddModifier(AC, new Modifier(-2, UNTYPED))
                .SetFlag(DENIED_DEX)
                .SetMultiplier(new Multiplier(0.5m, ALL_SPEEDS))
                .AddText(SKILLS, "All vision activities and checks (like Spot) auto-fail")
                .AddText(ATK_OPTIONS, "Opponents have total concealment")
                .AddModifier(SEARCH, new Modifier(-4, UNTYPED))
                .AddModifier(STR_SKILLS, new Modifier(-4, UNTYPED))
                .AddModifier(DEX_SKILLS, new Modifier(-4, UNTYPED))
            );

            Rules.AddEffect(new Effect("charging")
                .Controllable()
                .AddModifier(AC, new Modifier(-2, UNTYPED))
                .AddAttackModifier(WeaponClass.ALL, new Modifier(+2, UNTYPED))
                .AddModifier(STR_CHECKS, new Modifier(+2, UNTYPED)) // technically this is only for a bull rush as part of this charge
            );

            // not bothering with confused status -- you have to look it up anyway, and it's prone to DM fiat

            Rules.AddEffect(new Effect("cowering")
                .Controllable()
                .SetFlag(CANT_ACT)
                .SetFlag(DENIED_DEX)
                .AddModifier(AC, new Modifier(-2, UNTYPED))
            );

            Rules.AddEffect(new Effect("dazed")
                .Controllable()
                .SetFlag(CANT_ACT)
            );

            Rules.AddEffect(new Effect("dazzled")
                .Controllable()
                .AddAttackModifier(WeaponClass.ALL, new Modifier(-1, UNTYPED))
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
            IAmount negativeLevels = new MultiplyAmount(-1, Stats[NEGATIVE_LEVELS]);
            Rules.AddEffect(new Effect("energy drained")
                // A toggle isn't strictly necessary here since all the values will be zero if the character has no negative levels
                // It's included anyway so that it's easier to exclude it from the effects list in the frontend
                .SetToggle(new ConditionalToggle(Stats[NEGATIVE_LEVELS], Relation.GREATER_THAN, 0))
                .AddAttackModifier(WeaponClass.ALL, new Modifier(negativeLevels, UNTYPED))
                .AddModifier(ALL_SAVES, new Modifier(negativeLevels, UNTYPED))
                .AddModifier(ALL_SKILLS, new Modifier(negativeLevels, UNTYPED))
                .AddModifier(ABILITY_CHECKS, new Modifier(negativeLevels, UNTYPED))
                .AddModifier(MAX_HP, new Modifier(-5, Stats[NEGATIVE_LEVELS], UNTYPED))
                .AddText(MISC_CASTING, "-1 effective spellcasting level, lose a highest-level spell slot")
                // TODO : handle spellcasting effects in spellcasting subsystem
            );

            Rules.AddEffect(new Effect(CANT_RUN_OR_CHARGE)
                .SetToggle(new FlagToggle(CANT_RUN_OR_CHARGE))
                .AddText(MOVEMENT, CANT_RUN_OR_CHARGE)
            );

            Rules.AddEffect(new Effect("entangled")
                .Controllable()
                .SetMultiplier(new Multiplier(0.5m, ALL_SPEEDS))
                .SetFlag(CANT_RUN_OR_CHARGE)
                .AddAttackModifier(WeaponClass.ALL, new Modifier(-2, UNTYPED))
                .AddModifier(DEX_SCORE, new Modifier(-4, UNTYPED))
                .AddText(MISC_CASTING, "Spellcasting requires DC 15 concentration, or lose spell")
            );

            Rules.AddEffect(new Effect("exhausted")
                .Controllable()
                .SetMultiplier(new Multiplier(0.5m, ALL_SPEEDS))
                .SetFlag(CANT_RUN_OR_CHARGE) // exhausted doesn't actually say this but fatigued does.
                .AddModifier(STR_SCORE, new Modifier(-6, UNTYPED))
                .AddModifier(DEX_SCORE, new Modifier(-6, UNTYPED))
                .AddText(CONDITION, "Rest one hour to be fatigued instead of exhausted")
            );

            Rules.AddEffect(new Effect("fascinated")
                .Controllable()
                .SetFlag(CANT_ACT)
                .AddModifier(LISTEN, new Modifier(-4, UNTYPED))
                .AddModifier(SPOT, new Modifier(-4, UNTYPED))
            );

            Rules.AddEffect(new Effect("fatigued")
                .Controllable()
                .SetFlag(CANT_RUN_OR_CHARGE)
                .AddModifier(STR_SCORE, new Modifier(-2, UNTYPED))
                .AddModifier(DEX_SCORE, new Modifier(-2, UNTYPED))
            );

            Rules.AddEffect(new Effect("flat-footed")
                .Controllable()
                .SetFlag(FLAT_FOOTED)
                .AddText(ACTIONS, "Can't take immediate actions or attacks of opportunity")
            );

            Rules.AddEffect(new Effect("invisible")
                .Controllable()
                .AddAttackModifier(WeaponClass.ALL, new Modifier(+2, UNTYPED))
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
                .AddAttackModifier(WeaponClass.ALL, new Modifier(-4, UNTYPED))
                .AddText(ATK_OPTIONS, "Can't ranged weapons other than crossbows")
                .AddText(AC, "+4 vs ranged; -4 vs melee")
            );

            Rules.AddEffect(new Effect("sickened")
                .Controllable()
                .AddAttackModifier(WeaponClass.ALL, new Modifier(-2, UNTYPED))
                .AddDamageModifier(WeaponClass.ALL, new Modifier(-2, UNTYPED))
                .AddModifier(ALL_SAVES, new Modifier(-2, UNTYPED))
                .AddModifier(ALL_SKILLS, new Modifier(-2, UNTYPED))
                .AddModifier(ABILITY_CHECKS, new Modifier(-2, UNTYPED))
            );

            Rules.AddEffect(new Effect("staggered")
                .SetToggle(new ConditionalToggle(
                    new VariableAmount(Stats[NONLETHAL_DAMAGE]),
                    Relation.EQUAL,
                    new VariableAmount(Stats[CURRENT_HP])
                ))
                .AddText(ACTIONS, "One move or standard each round (not both, no full-rounds)")
            );

            Rules.AddEffect(new Effect("stunned")
                .Controllable()
                .AddModifier(AC, new Modifier(-2, UNTYPED))
                .SetFlag(DENIED_DEX)
                .SetFlag(CANT_ACT) // TODO : this doesn't work; needs to be AddFlag instead.
                .AddText(ACTIONS, "Must drop all held items")
            );

            IToggle unconscious = new AnyToggle(
                new ConditionalToggle(Stats[CURRENT_HP], Relation.LESS_THAN, 0),
                new ConditionalToggle(
                    new VariableAmount(Stats[NONLETHAL_DAMAGE]),
                    Relation.GREATER_THAN,
                    new VariableAmount(Stats[CURRENT_HP])
                )
            );
            Rules.AddEffect(new Effect("unconscious").SetToggle(unconscious));
            Rules.AddEffect(new Effect(HELPLESS_AND_PARALYZED)
                // There's no real effect here; setting STR and DEX to zero in the same effect where
                // we check STR and DEX causes an infinite stack overflow.
                .SetToggle(new AnyToggle(
                    new ControllableToggle(HELPLESS_AND_PARALYZED),
                    new ConditionalToggle(Stats[STR_SCORE], Relation.EQUAL, 0),
                    new ConditionalToggle(Stats[DEX_SCORE], Relation.EQUAL, 0),
                    unconscious
                ))
                .SetFlag(CANT_ACT)
                .SetFlag(HELPLESS_AND_PARALYZED) // TODO : this doesn't work; needs to be AddFlag instead.
            );

            Rules.AddEffect(new Effect("DEX to AC")
                .AddModifier(AC, new Modifier(Stats[DEX_MODIFIER], DODGE))
                .SetToggle(new AnyToggle(
                    // DEX always applies if it's negative
                    new ConditionalToggle(Stats[DEX_MODIFIER], Relation.LESS_THAN, 0),
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
                    .AddModifier(ability + " score", new Modifier(-1, Stats[ability + " damage"], BASE))
            );
            Statistic abilityDrain = new Statistic(ability + " drain");
            AddStat(abilityDrain);
            Rules.AddEffect(
                new Effect(ability + " drain")
                    .AddModifier(ability + " score", new Modifier(-1, Stats[ability + " drain"], BASE))
            );

            string abilityModifierName = ability + " modifier";
            Rules.AddEffect(
                new Effect("Base " + ability + " modifier")
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
                new Effect("Ranks in " + skill) // so-named so it doesn't collide with effects I make in testing.
                // TODO : that's a bad reason to name things this way.
                    .AddModifier(skill, new Modifier(skillRanks, BASE))
            );
            Rules.AddEffect(
                new Effect(ability + " to " + skill)
                    .AddModifier(skill, new Modifier(abilityModifiers[ability], BASE))
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

        private void SkillSynergy(Statistic fiveRanksIn, string givesPlusTwoOn) {
            Rules.AddEffect(
                new Effect("Synergy: " + fiveRanksIn.Name + " -> " + givesPlusTwoOn)
                    .AddModifier(givesPlusTwoOn, new Modifier(+2, UNTYPED))
                    .SetToggle(new ConditionalToggle(new VariableAmount(fiveRanksIn), Relation.GREATER_THAN_OR_EQUAL, 5))
            );
        }

        private void ArmorCheckPenalty(string skill) {
            Rules.AddEffect(
                new Effect(ARMOR_CHECK_PENALTY + " to " + skill)
                    .AddModifier(skill, new Modifier(Stats[ARMOR_CHECK_PENALTY], UNTYPED))
            );
        }
    }
}
