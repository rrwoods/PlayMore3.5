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

        private List<string> TagsForAttack(Items.Attack attack) {
            List<string> tags = new List<string>(attack.W.Tags) {
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
            tags.Add(SIZE_CATEGORIES[attack.W.SizeCategory + 4]);
            return tags;
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

                    List<string> tags = TagsForAttack(attack);
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

        public Dictionary<string, DiceStatistic> ExtraDice() {
            Dictionary<string, DiceStatistic> snapshot = new Dictionary<string, DiceStatistic>();

            foreach (Items.Weapon weapon in weapons) {
                string name = weapon.Name;

                foreach (Items.Attack attack in weapon.Attacks) {
                    string range = attack.RangeCategory.Name();
                    DiceStatistic stat = new DiceStatistic(range + EXTRA_DAMAGE_DICE + WITH + name)
                        .AddTag(EXTRA_DAMAGE_DICE)
                        .AddTag(range + EXTRA_DAMAGE_DICE)
                        .AddTag(range + EXTRA_DAMAGE_DICE + WITH + name);

                    List<string> tags = TagsForAttack(attack);
                    foreach (string tag in tags) {
                        stat
                            .AddTag(EXTRA_DAMAGE_DICE + WITH + tag + WEAPONS)
                            .AddTag(range + EXTRA_DAMAGE_DICE + WITH + tag + WEAPONS);
                    }

                    snapshot[stat.Name] = stat;
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

            AddStat(new Statistic(CURRENT_HP));
            Rules.AddEffect(new Effect(MAX_HP)
                .AddModifier(new Modifier(stats[MAX_HP], BASE, CURRENT_HP))
            );

            AddStat(new Statistic(LETHAL_DAMAGE));
            Rules.AddEffect(new Effect("Subtract damage from hit points")
                .AddModifier(new Modifier(new VariableAmount(stats[LETHAL_DAMAGE], -1), BASE, CURRENT_HP))
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
                // TODO : text about what disabled means
            );

            Rules.AddEffect(new Effect("dying")
                .SetToggle(new AllToggle(
                    new ConditionalToggle(stats[CURRENT_HP], Relation.LESS_THAN, 0),
                    new UnToggle(stable)
                ))
                // TODO : text "can't act"
                // TODO : text "1 lethal damage per round"
                // TODO : text "healing stabilizes; 10%/round to auto-stabilize"
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
            AddStat(new Statistic(DISGUISE_ACT_IN_CHARACTER,      stats[DISGUISE]));
            AddStat(new Statistic(ESCAPE_ARTIST_ROPE,             stats[ESCAPE_ARTIST]));
            AddStat(new Statistic(PSICRAFT_ADDRESS_POWER_STONE,   stats[PSICRAFT]));
            AddStat(new Statistic(SEARCH_SECRET_DOORS,            stats[SEARCH]));
            AddStat(new Statistic(SPELLCRAFT_DECIPHER_SCROLL,     stats[SPELLCRAFT]));
            AddStat(new Statistic(SURVIVAL_LOST,                  stats[SURVIVAL]));
            AddStat(new Statistic(SURVIVAL_ABOVEGROUND,           stats[SURVIVAL]));
            AddStat(new Statistic(SURVIVAL_PLANAR,                stats[SURVIVAL]));
            AddStat(new Statistic(SURVIVAL_FOLLOW_TRACKS,         stats[SURVIVAL]));
            AddStat(new Statistic(USE_MAGIC_DEVICE_SCROLL,        stats[USE_MAGIC_DEVICE]));
            AddStat(new Statistic(USE_PSIONIC_DEVICE_POWER_STONE, stats[USE_PSIONIC_DEVICE]));
            AddStat(new Statistic(USE_ROPE_BINDINGS,              stats[USE_ROPE]));

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
                    .AddModifier(new Modifier(new VariableAmount(stats[ARMOR_CHECK_PENALTY], 2), UNTYPED, SWIM))
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
                .AddModifier(new Modifier(10, BASE, AC))
            );

            AddStat(new Statistic(NATURAL_ARMOR)); // this is the same name as the bonus type. eh.
            Rules.AddEffect(new Effect(NATURAL_ARMOR)
                .AddModifier(new Modifier(stats[NATURAL_ARMOR], NATURAL_ARMOR, AC))
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
                .AddModifier(new Modifier(UNTYPED, excessSpeed)
                    .AddAmount(new VariableAmount(stats[LAND_SPEED], 0.1m))
                    .AddAmount(-3)
                )
            );
            string lackingSpeed = "speed under 30 ft, in units of 10 ft";
            AddStat(new Statistic(lackingSpeed));
            Rules.AddEffect(new Effect(lackingSpeed)
                .AddModifier(new Modifier(UNTYPED, lackingSpeed)
                    .AddAmount(new VariableAmount(stats[LAND_SPEED], -0.1m))
                    .AddAmount(+3)
                )
            );

            Rules.AddEffect(new Effect("Speed penalty")
                // the rule says "less than 30", but it doesn't actually do anything unless you're at 20 or under.
                .SetToggle(new ConditionalToggle(stats[LAND_SPEED], Relation.LESS_THAN_OR_EQUAL, 20))
                .AddModifier(new Modifier(new VariableAmount(stats[lackingSpeed], -6), UNTYPED, JUMP))
            );
            Rules.AddEffect(new Effect("Speed bonus")
                // the rule stays "more than 30", but it doesn't actually do anything unless you're at 40 or over.
                .SetToggle(new ConditionalToggle(stats[LAND_SPEED], Relation.GREATER_THAN_OR_EQUAL, 40))
                .AddModifier(new Modifier(new VariableAmount(stats[excessSpeed], +4), UNTYPED, JUMP))
            );

            // Medium at 0 makes a lot of calculations more smooth
            AddStat(new Statistic(SIZE_CATEGORY).Minimum(-4).Maximum(+4));

            AddStat(new Statistic(SIZE_MODIFIER));
            Rules.AddEffect(new Effect(SIZE_MODIFIER + " calculation")
                .AddModifier(new Modifier(
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
                    BASE,
                    SIZE_MODIFIER
                ))
            );
            Rules.AddEffect(new Effect(SIZE_MODIFIER)
                .AddModifier(new Modifier(stats[SIZE_MODIFIER], SIZE, ATTACK_ROLLS))
                .AddModifier(new Modifier(stats[SIZE_MODIFIER], SIZE, AC))
            );

            // for now (or maybe forever) i'm not dealing with the fact that there's a long/tall distinction, or the fact
            // that "some exceptions exist" to these values.  for player characters i'm assuming tall, and i'm assuming that
            // space never changes, and i'm assuming that if reach changes it's via an effect not a base reach difference.
            // if this is not the case... i'll just let the player say what these values are and remove these effects.

            // TODO : Space (it's a LookupText, basically)
            AddStat(new Statistic(REACH).Minimum(0));
            Rules.AddEffect(new Effect("Reach based on size")
                .AddModifier(new Modifier(
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
                    BASE,
                    REACH
                ))
            );

            AddStat(new Statistic(BASE_ATTACK_BONUS));
            AddStat(new Statistic(GRAPPLE));
            Rules.AddEffect(new Effect("STR to grapple")
                .AddModifier(new Modifier(stats[STR_MODIFIER], BASE, GRAPPLE))
            );

            Rules.AddEffect(new Effect(BASE_ATTACK_BONUS)
                .AddModifier(new Modifier(stats[BASE_ATTACK_BONUS], BASE, ATTACK_ROLLS))
                .AddModifier(new Modifier(stats[BASE_ATTACK_BONUS], BASE, GRAPPLE))
            );

            AddStat(new Statistic(SPECIAL_SIZE_MODIFIER));
            Rules.AddEffect(new Effect(SPECIAL_SIZE_MODIFIER + " calculation")
                .AddModifier(new Modifier(new VariableAmount(stats[SIZE_CATEGORY], 4), BASE, SPECIAL_SIZE_MODIFIER))
            );
            Rules.AddEffect(new Effect(SPECIAL_SIZE_MODIFIER)
                .AddModifier(new Modifier(new VariableAmount(stats[SPECIAL_SIZE_MODIFIER], -1), SIZE, HIDE))
                .AddModifier(new Modifier(stats[SPECIAL_SIZE_MODIFIER], SIZE, GRAPPLE))
            );

            Rules.AddEffect(new Effect("DEX to ranged attacks")
                .AddModifier(new Modifier(stats[DEX_MODIFIER], BASE, RANGED + ATTACK_ROLLS))
            );
            // Weapon Finesse will use ability-substitution technology
            Rules.AddEffect(new Effect("STR to melee attacks")
                .AddModifier(new Modifier(stats[STR_MODIFIER], BASE, MELEE + ATTACK_ROLLS))
            );

            Rules.AddEffect(new Effect("1½ STR bonus to two-handed weapon damage")
                .AddModifier(new Modifier(
                    new VariableAmount(stats[STR_BONUS], 1.5m),
                    BASE,
                    MELEE + DAMAGE_ROLLS + WITH + TWO_HANDED + WEAPONS
                ))
            );
            Rules.AddEffect(new Effect("STR bonus to primary-hand weapon damage")
                .AddModifier(new Modifier(stats[STR_BONUS], BASE, MELEE + DAMAGE_ROLLS + WITH + PRIMARY_HAND + WEAPONS))
            );
            Rules.AddEffect(new Effect("½ STR bonus to off-hand weapon damage")
                .AddModifier(new Modifier(
                    new VariableAmount(stats[STR_BONUS], 0.5m),
                    BASE,
                    MELEE + DAMAGE_ROLLS + WITH + OFF_HAND + WEAPONS
                ))
            );

            Rules.AddEffect(new Effect("STR penalty to projectile weapon damage")
                .AddModifier(new Modifier(stats[STR_PENALTY], BASE, RANGED + DAMAGE_ROLLS + WITH + PROJECTILE + WEAPONS))
            );
            Rules.AddEffect(new Effect("STR to thrown weapon damage")
                .AddModifier(new Modifier(stats[STR_MODIFIER], BASE, RANGED + DAMAGE_ROLLS + WITH + THROWN + WEAPONS))
            );

            // XXX : I hate this.  I hate this so much.
            // But I currently see no other way of dealing with composite bows.
            for (int strengthRating = 0; strengthRating <= 10; strengthRating++) {
                string strText = "+" + strengthRating + " STR";
                Rules.AddEffect(new Effect("Insufficient STR for " + strText + " composite bow")
                    .SetToggle(new ConditionalToggle(stats[STR_MODIFIER], Relation.LESS_THAN, strengthRating))
                    .AddModifier(new Modifier(-2, BASE, RANGED + ATTACK_ROLLS + WITH + strText + WEAPONS))
                );
                Rules.AddEffect(new Effect(strText + " composite bow damage")
                    .AddModifier(new Modifier(
                        new LesserAmount(
                            new VariableAmount(stats[STR_BONUS]), // if there's a penalty, we already applied it above.
                            new FixedAmount(strengthRating)
                        ),
                        BASE,
                        RANGED + DAMAGE_ROLLS + WITH + strText + WEAPONS
                    ))
                );
            }

            // XXX : more hate but again i'm not seeing another way.
            for (int sizeCategory = -4; sizeCategory <= +4; sizeCategory++) {
                string sizeText = SIZE_CATEGORIES[sizeCategory + 4];
                Rules.AddEffect(new Effect("Attack penalty for using " + sizeText + " weapon (too large)")
                    .SetToggle(new ConditionalToggle(stats[SIZE_CATEGORY], Relation.GREATER_THAN, sizeCategory))
                    .AddModifier(new Modifier(BASE, ATTACK_ROLLS + WITH + sizeText + WEAPONS)
                        .AddAmount(new VariableAmount(stats[SIZE_CATEGORY], -2))
                        .AddAmount(sizeCategory * 2)
                    )
                );
                Rules.AddEffect(new Effect("Attack penalty for using " + sizeText + " weapon (too small)")
                    .SetToggle(new ConditionalToggle(stats[SIZE_CATEGORY], Relation.LESS_THAN, sizeCategory))
                    .AddModifier(new Modifier(BASE, ATTACK_ROLLS + WITH + sizeText + WEAPONS)
                        .AddAmount(new VariableAmount(stats[SIZE_CATEGORY], 2))
                        .AddAmount(-sizeCategory * 2)
                    )
                );
            }

            AddStat(new Statistic(FEAR_LEVEL).Minimum(0).Maximum(3));
            Rules.AddEffect(new Effect(SHAKEN)
                .SetToggle(new ConditionalToggle(stats[FEAR_LEVEL], Relation.GREATER_THAN_OR_EQUAL, 1))
                .AddModifier(new Modifier(-2, UNTYPED, ATTACK_ROLLS))
                .AddModifier(new Modifier(-2, UNTYPED, ALL_SAVES))
                .AddModifier(new Modifier(-2, UNTYPED, ALL_SKILLS))
                .AddModifier(new Modifier(-2, UNTYPED, ABILITY_CHECKS))
            );
            Rules.AddEffect(new Effect(FRIGHTENED)
                .SetToggle(new ConditionalToggle(stats[FEAR_LEVEL], Relation.EQUAL, 2))
                // TODO : text "must flee from source of fear"
            );
            Rules.AddEffect(new Effect(PANICKED)
                .SetToggle(new ConditionalToggle(stats[FEAR_LEVEL], Relation.EQUAL, 3))
                // TODO : text "must drop all held items and flee at top speed along a random path, cowering if cornered"
            );


            Rules.AddEffect(new Effect("blinded")
                .Controllable()
                .AddModifier(new Modifier(-2, UNTYPED, AC))
                .SetFlag(DENIED_DEX)
                .SetMultiplier(new Multiplier(0.5m, ALL_SPEEDS))
                // TODO : text "all vision activities and checks (like Spot) auto-fail"
                // TODO : text "opponents have total concealment"
                .AddModifier(new Modifier(-4, UNTYPED, SEARCH))
                .AddModifier(new Modifier(-4, UNTYPED, STR_SKILLS))
                .AddModifier(new Modifier(-4, UNTYPED, DEX_SKILLS))
            );

            Rules.AddEffect(new Effect("charging")
                .Controllable()
                .AddModifier(new Modifier(-2, UNTYPED, AC))
                .AddModifier(new Modifier(+2, UNTYPED, ATTACK_ROLLS))
                .AddModifier(new Modifier(+2, UNTYPED, STR_CHECKS)) // technically this is only for a bull rush as part of this charge
            );

            // not bothering with confused status -- you have to look it up anyway, and it's prone to DM fiat

            Rules.AddEffect(new Effect("cowering")
                .Controllable()
                // TODO : text "can't act"
                .SetFlag(DENIED_DEX)
                .AddModifier(new Modifier(-2, UNTYPED, AC))
            );

            // even though this is an entirely textual entirely controllable effect (meaning you turn it "dazed" on to basically
            // just see "you are dazed"), i'm including it anyway.  there are many things that have a "can't act" effect which i
            // am strongly considering making its own thing.
            Rules.AddEffect(new Effect("dazed")
                .Controllable()
                // TODO : text "can't act"
            );

            Rules.AddEffect(new Effect("dazzled")
                .Controllable()
                .AddModifier(new Modifier(-1, UNTYPED, ATTACK_ROLLS))
                .AddModifier(new Modifier(-1, UNTYPED, SEARCH))
                .AddModifier(new Modifier(-1, UNTYPED, SPOT))
            );

            Rules.AddEffect(new Effect("deafened")
                .Controllable()
                .AddModifier(new Modifier(-4, UNTYPED, INITIATIVE))
                // TODO : text "auto-fail Listen checks"
                // TODO : text 20% spell failure on spells with verbal components
            );

            AddStat(new Statistic(NEGATIVE_LEVELS));
            Rules.AddEffect(new Effect("energy drained")
                // A toggle isn't strictly necessary here since all the values will be zero if the character has no negative levels
                // It's included anyway so that it's easier to exclude it from the effects list in the frontend
                .SetToggle(new ConditionalToggle(stats[NEGATIVE_LEVELS], Relation.GREATER_THAN, 0))
                .AddModifier(new Modifier(new VariableAmount(stats[NEGATIVE_LEVELS], -1), UNTYPED, ATTACK_ROLLS))
                .AddModifier(new Modifier(new VariableAmount(stats[NEGATIVE_LEVELS], -1), UNTYPED, ALL_SAVES))
                .AddModifier(new Modifier(new VariableAmount(stats[NEGATIVE_LEVELS], -1), UNTYPED, ALL_SKILLS))
                .AddModifier(new Modifier(new VariableAmount(stats[NEGATIVE_LEVELS], -1), UNTYPED, ABILITY_CHECKS))
                .AddModifier(new Modifier(new VariableAmount(stats[NEGATIVE_LEVELS], -5), UNTYPED, MAX_HP))
                // TODO : -1 effective level for spellcasting; lose a highest-level spell slot
            );

            Rules.AddEffect(new Effect("entangled")
                .Controllable()
                .SetMultiplier(new Multiplier(0.5m, ALL_SPEEDS))
                // TODO : "can't run or charge"
                .AddModifier(new Modifier(-2, UNTYPED, ATTACK_ROLLS))
                .AddModifier(new Modifier(-4, UNTYPED, DEX_SCORE))
                // TODO : text "spellcasting requires DC 15 Concentration, or lose spell"
            );

            Rules.AddEffect(new Effect("exhausted")
                .Controllable()
                .SetMultiplier(new Multiplier(0.5m, ALL_SPEEDS))
                // TODO : text "can't run or charge" -- exhausted doesn't actually say this but fatigued does.
                .AddModifier(new Modifier(-6, UNTYPED, STR_SCORE))
                .AddModifier(new Modifier(-6, UNTYPED, DEX_SCORE))
                // TODO : text "rest one hour to be fatigued instead of exhausted"
            );

            Rules.AddEffect(new Effect("fascinated")
                .Controllable()
                // TODO : text "can't act"
                .AddModifier(new Modifier(-4, UNTYPED, LISTEN))
                .AddModifier(new Modifier(-4, UNTYPED, SPOT))
            );

            Rules.AddEffect(new Effect("fatigued")
                .Controllable()
                // TODO : text "can't run or charge"
                .AddModifier(new Modifier(-2, UNTYPED, STR_SCORE))
                .AddModifier(new Modifier(-2, UNTYPED, DEX_SCORE))
            );

            Rules.AddEffect(new Effect("flat-footed")
                .Controllable()
                .SetFlag(FLAT_FOOTED)
                // TODO : text "can't take immediate actions or attacks of opportunity"
            );

            Rules.AddEffect(new Effect("invisible")
                .Controllable()
                .AddModifier(new Modifier(+2, UNTYPED, ATTACK_ROLLS))
                // TODO : text "opponents lose DEX to AC"
            );

            Rules.AddEffect(new Effect("nauseated")
                .Controllable()
                // TODO : text "can't attack"
                // TODO : text "can't cast spells or concentrate on spells"
                // TODO : text "can't concentrate on anything"
                // TODO : text "can only take one move action each turn"
            );

            Rules.AddEffect(new Effect("prone")
                .Controllable()
                .AddModifier(new Modifier(-4, UNTYPED, MELEE + ATTACK_ROLLS))
                // TODO : text "can't use non-crossbow ranged weapons"
                // TODO : text "+4 AC vs ranged"
                // TODO : text "-4 AC vs melee"
            );

            Rules.AddEffect(new Effect("sickened")
                .Controllable()
                .AddModifier(new Modifier(-2, UNTYPED, ATTACK_ROLLS))
                .AddModifier(new Modifier(-2, UNTYPED, DAMAGE_ROLLS))
                .AddModifier(new Modifier(-2, UNTYPED, ALL_SAVES))
                .AddModifier(new Modifier(-2, UNTYPED, ALL_SKILLS))
                .AddModifier(new Modifier(-2, UNTYPED, ABILITY_CHECKS))
            );

            Rules.AddEffect(new Effect("staggered")
                .SetToggle(new ConditionalToggle(
                    new VariableAmount(stats[NONLETHAL_DAMAGE]),
                    Relation.EQUAL,
                    new VariableAmount(stats[CURRENT_HP])
                ))
                // TODO : text "one standard or one move action only"
            );

            Rules.AddEffect(new Effect("stunned")
                .Controllable()
                .AddModifier(new Modifier(-2, UNTYPED, AC))
                .SetFlag(DENIED_DEX)
                // text "can't act"
                // text "drop all held items"
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
                // TODO : text "can't act"
                // TODO : text "helpless and paralyzed", probably in all sections
                // TODO : text "effective STR 0 and DEX 0"
            );

            Rules.AddEffect(new Effect("DEX to AC")
                .AddModifier(new Modifier(stats[DEX_MODIFIER], DODGE, AC))
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
                    .AddModifier(new Modifier(new VariableAmount(stats[ability + " damage"], -1), BASE, ability + " score"))
            );
            Statistic abilityDrain = new Statistic(ability + " drain");
            AddStat(abilityDrain);
            Rules.AddEffect(
                new Effect(ability + " drain")
                    .AddModifier(new Modifier(new VariableAmount(stats[ability + " drain"], -1), BASE, ability + " drain"))
            );

            string abilityModifierName = ability + " modifier";
            Rules.AddEffect(
                new Effect("Base " + ability + " modifier")
                    .AddModifier(new Modifier(BASE, abilityModifierName)
                        .AddAmount(-5)
                        .AddAmount(new VariableAmount(abilityScore, 0.5m))
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
                        BASE,
                        abilityBonus.Name
                    ))
            );

            Statistic abilityPenalty = new Statistic(ability + " penalty");
            AddStat(abilityPenalty);
            Rules.AddEffect(
                new Effect(ability + " penalty")
                    .AddModifier(new Modifier(
                        new LesserAmount(new FixedAmount(0), new VariableAmount(abilityModifier)),
                        BASE,
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
                new Effect("Ranks in " + skill) // so-named so it doesn't collide with effects I make in testing.
                // TODO : that's a bad reason to name things this way.

                    .AddModifier(new Modifier(skillRanks, BASE, skill))
            );
            Rules.AddEffect(
                new Effect(ability + " to " + skill)
                    .AddModifier(new Modifier(abilityModifiers[ability], BASE, skill))
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
                    .AddModifier(new Modifier(2, UNTYPED, givesPlusTwoOn))
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
