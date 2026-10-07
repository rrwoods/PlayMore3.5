using System;
using System.Collections.Generic;
using System.Linq;
using PlayMore35.Effects;
using PlayMore35.Items;
using static PlayMore35.Effects.Constants;

namespace PlayMore35.Items {
    public static class AttackRoutineEvaluator {
        private static int AdjustSize(int starting, int change) {
            int adjusted = starting + change;
            if (adjusted < -4) {
                adjusted = -4;
            } else if (adjusted > +4) {
                adjusted = +4;
            }
            return adjusted;
        }

        private static List<int> GetExtraAttackPenalties(EffectPool rules, Equipped items, Attack attack) {
            if (attack == null) {
                return new List<int>();
            }
            var extras = rules.GetExtraAttacks(items, attack);
            var stacked = StackingRules.StackExtraAttacks(extras);
            stacked.Sort((a, b) => b.CompareTo(a)); // sort descending: 0, -5, -10, ...
            return stacked;
        }

        private static List<Attack> BuildProgressionTierAttacks(
            EffectPool rules,
            Equipped items,
            Attack primaryAttack,
            Attack offHandAttack = null
        ) {
            var primaryPenalties = GetExtraAttackPenalties(rules, items, primaryAttack);
            var offHandPenalties = offHandAttack != null
                ? GetExtraAttackPenalties(rules, items, offHandAttack)
                : new List<int>();

            var allPenalties = new HashSet<int> { 0 };
            foreach (var p in primaryPenalties) allPenalties.Add(p);
            foreach (var p in offHandPenalties) allPenalties.Add(p);

            var sortedTiers = allPenalties.OrderByDescending(p => p).ToList();
            var result = new List<Attack>();

            foreach (var tierPenalty in sortedTiers) {
                if (tierPenalty == 0) {
                    result.Add(primaryAttack);
                    foreach (var p in primaryPenalties.Where(pen => pen == 0)) {
                        result.Add(primaryAttack.WithIterativePenalty(0, "Bonus attack"));
                    }
                    if (offHandAttack != null) {
                        result.Add(offHandAttack);
                        foreach (var p in offHandPenalties.Where(pen => pen == 0)) {
                            result.Add(offHandAttack.WithIterativePenalty(0, "Bonus attack"));
                        }
                    }
                } else {
                    int priCount = primaryPenalties.Count(pen => pen == tierPenalty);
                    for (int i = 0; i < priCount; i++) {
                        result.Add(primaryAttack.WithIterativePenalty(tierPenalty));
                    }
                    if (offHandAttack != null) {
                        int offCount = offHandPenalties.Count(pen => pen == tierPenalty);
                        for (int i = 0; i < offCount; i++) {
                            result.Add(offHandAttack.WithIterativePenalty(tierPenalty));
                        }
                    }
                }
            }

            return result;
        }

        public static List<AttackRoutine> EvaluateRoutines(EffectPool rules, Equipped items, int sizeDiscrepancy = 0) {
            var routines = new List<AttackRoutine>();
            if (rules == null || items == null) {
                return routines;
            }

            Armor armor = items.DonnedArmor;
            Shield shield = items.OffHandShield;
            Weapon primary = items.PrimaryHandWeapon;
            Weapon offHand = items.OffHandWeapon;

            bool shieldPresent = shield != null;
            if (shieldPresent && shield.AttackRollPenalty == null) {
                offHand = null;
            }

            int playerSizeCategory = rules.GetStat(SIZE_CATEGORY).GetValue(rules, items).value;
            int shSizeCategory = shieldPresent ? AdjustSize(shield.SizeCategory, sizeDiscrepancy) : 0;
            int shSizeDiscrepancy = shieldPresent ? shSizeCategory - playerSizeCategory : 0;
            bool shieldBashAvailable = shield?.ShieldBash != null;
            bool spikesAvailable = armor?.ArmorSpikes != null;
            int armSizeCategory = spikesAvailable ? AdjustSize(armor.SizeCategory, sizeDiscrepancy) : 0;

            // 1. Standalone secondary attacks (solo armor spikes, solo shield bash)
            if (spikesAvailable) {
                Attack spikesStandard = Attack.ArmorSpikes(rules, items, Weapon.Hand.ONE_HANDED, armSizeCategory);
                if (spikesStandard != null) {
                    routines.Add(new AttackRoutine(
                        RoutineActionType.Standard,
                        RoutineCategory.ArmorSpikesSolo,
                        "Armor Spikes Standard Attack",
                        Enumerable.Empty<Weapon>(),
                        new[] { spikesStandard }
                    ));
                    var spikesFull = BuildProgressionTierAttacks(rules, items, spikesStandard);
                    if (spikesFull.Count > 1) {
                        routines.Add(new AttackRoutine(
                            RoutineActionType.FullAttack,
                            RoutineCategory.ArmorSpikesSolo,
                            "Armor Spikes Full Attack",
                            Enumerable.Empty<Weapon>(),
                            spikesFull
                        ));
                    }
                }
            }

            if (shieldBashAvailable) {
                Attack shieldStandard = Attack.ShieldBash(rules, items, Weapon.Hand.ONE_HANDED, shSizeCategory, shSizeDiscrepancy);
                if (shieldStandard != null) {
                    routines.Add(new AttackRoutine(
                        RoutineActionType.Standard,
                        RoutineCategory.ShieldBashSolo,
                        "Shield Bash Standard Attack",
                        Enumerable.Empty<Weapon>(),
                        new[] { shieldStandard }
                    ));
                    var shieldFull = BuildProgressionTierAttacks(rules, items, shieldStandard);
                    if (shieldFull.Count > 1) {
                        routines.Add(new AttackRoutine(
                            RoutineActionType.FullAttack,
                            RoutineCategory.ShieldBashSolo,
                            "Shield Bash Full Attack",
                            Enumerable.Empty<Weapon>(),
                            shieldFull
                        ));
                    }
                }
            }

            if (primary == null && offHand == null) {
                return routines;
            }

            // 2. Primary weapon routines
            if (primary != null) {
                int priSizeCategory = AdjustSize(primary.SizeCategory, sizeDiscrepancy);
                int priSizeDiscrepancy = priSizeCategory - playerSizeCategory;
                Weapon.Effort priEffort = primary.EffortCategory + priSizeDiscrepancy;

                if (priEffort >= Weapon.Effort.LIGHT && priEffort <= Weapon.Effort.TWO_HANDED) {
                    bool singleInHand = !shieldPresent && offHand == null;

                    if (singleInHand) {
                        // Single weapon held in primary hand -> two-hand if possible
                        if (primary.SecondHead != null) {
                            // Double weapon
                            if (primary.Melee != null) {
                                Attack head1TwoHand = Attack.Melee(rules, items, primary, Weapon.Hand.TWO_HANDED, priSizeCategory, priSizeDiscrepancy, 0, true);
                                if (head1TwoHand != null) {
                                    routines.Add(new AttackRoutine(
                                        RoutineActionType.Standard,
                                        RoutineCategory.DoubleWeapon,
                                        $"{primary.Name} Standard Attack",
                                        new[] { primary },
                                        new[] { head1TwoHand }
                                    ));
                                    var head1Full = BuildProgressionTierAttacks(rules, items, head1TwoHand);
                                    if (head1Full.Count > 1) {
                                        routines.Add(new AttackRoutine(
                                            RoutineActionType.FullAttack,
                                            RoutineCategory.DoubleWeapon,
                                            $"{primary.Name} (Two-Handed) Full Attack",
                                            new[] { primary },
                                            head1Full
                                        ));
                                    }
                                }

                                Attack head2TwoHand = Attack.Melee(rules, items, primary, Weapon.Hand.TWO_HANDED, priSizeCategory, priSizeDiscrepancy, 0, false);
                                if (head2TwoHand != null) {
                                    routines.Add(new AttackRoutine(
                                        RoutineActionType.Standard,
                                        RoutineCategory.DoubleWeapon,
                                        $"{primary.Name} (Second Head) Standard Attack",
                                        new[] { primary },
                                        new[] { head2TwoHand }
                                    ));
                                }

                                // Double weapon TWF routine
                                Attack head1Twf = Attack.Melee(rules, items, primary, Weapon.Hand.PRIMARY_HAND_TWF, priSizeCategory, priSizeDiscrepancy, -4, true, Weapon.Effort.ONE_HANDED);
                                Attack head2Twf = Attack.Melee(rules, items, primary, Weapon.Hand.OFF_HAND_TWF, priSizeCategory, priSizeDiscrepancy, -8, false, Weapon.Effort.LIGHT);
                                if (head1Twf != null && head2Twf != null) {
                                    var doubleTwfAttacks = BuildProgressionTierAttacks(rules, items, head1Twf, head2Twf);
                                    routines.Add(new AttackRoutine(
                                        RoutineActionType.FullAttack,
                                        RoutineCategory.DoubleWeapon,
                                        $"{primary.Name} (Two-Weapon) Full Attack",
                                        new[] { primary },
                                        doubleTwfAttacks
                                    ));
                                }
                            }
                        } else {
                            // Standard single weapon (not double weapon)
                            if (primary.Melee != null) {
                                Weapon.Hand hand = priEffort == Weapon.Effort.LIGHT ? Weapon.Hand.ONE_HANDED : Weapon.Hand.TWO_HANDED;
                                Attack meleeAtk = Attack.Melee(rules, items, primary, hand, priSizeCategory, priSizeDiscrepancy);
                                if (meleeAtk != null) {
                                    routines.Add(new AttackRoutine(
                                        RoutineActionType.Standard,
                                        RoutineCategory.PrimarySolo,
                                        $"{primary.Name} Standard Attack",
                                        new[] { primary },
                                        new[] { meleeAtk }
                                    ));
                                    var fullAtks = BuildProgressionTierAttacks(rules, items, meleeAtk);
                                    if (fullAtks.Count > 1) {
                                        routines.Add(new AttackRoutine(
                                            RoutineActionType.FullAttack,
                                            RoutineCategory.PrimarySolo,
                                            $"{primary.Name} Full Attack",
                                            new[] { primary },
                                            fullAtks
                                        ));
                                    }
                                }
                            }

                            if (primary.Projectile != null) {
                                Attack projAtk = Attack.Projectile(rules, items, primary, Weapon.Hand.TWO_HANDED, priSizeCategory, priSizeDiscrepancy);
                                if (projAtk != null) {
                                    routines.Add(new AttackRoutine(
                                        RoutineActionType.Standard,
                                        RoutineCategory.PrimarySolo,
                                        $"{primary.Name} Standard Attack",
                                        new[] { primary },
                                        new[] { projAtk }
                                    ));
                                    var fullAtks = BuildProgressionTierAttacks(rules, items, projAtk);
                                    if (fullAtks.Count > 1) {
                                        routines.Add(new AttackRoutine(
                                            RoutineActionType.FullAttack,
                                            RoutineCategory.PrimarySolo,
                                            $"{primary.Name} Full Attack",
                                            new[] { primary },
                                            fullAtks
                                        ));
                                    }
                                }
                            }

                            if (primary.CanBeThrown()) {
                                Attack thrownAtk = Attack.Thrown(rules, items, primary, Weapon.Hand.ONE_HANDED, priSizeCategory, priSizeDiscrepancy);
                                if (thrownAtk != null) {
                                    routines.Add(new AttackRoutine(
                                        RoutineActionType.Standard,
                                        RoutineCategory.PrimarySolo,
                                        $"{primary.Name} Thrown Standard Attack",
                                        new[] { primary },
                                        new[] { thrownAtk }
                                    ));
                                    var fullAtks = BuildProgressionTierAttacks(rules, items, thrownAtk);
                                    if (fullAtks.Count > 1) {
                                        routines.Add(new AttackRoutine(
                                            RoutineActionType.FullAttack,
                                            RoutineCategory.PrimarySolo,
                                            $"{primary.Name} Thrown Full Attack",
                                            new[] { primary },
                                            fullAtks
                                        ));
                                    }
                                }
                            }
                        }

                        // Armor spikes TWF with two-handed/single weapon
                        if (spikesAvailable) {
                            Attack primaryForSpikes = null;
                            if (primary.Melee != null) {
                                primaryForSpikes = Attack.Melee(rules, items, primary, Weapon.Hand.TWO_HANDED_TWF, priSizeCategory, priSizeDiscrepancy, -4);
                            } else if (primary.Projectile != null) {
                                primaryForSpikes = Attack.Projectile(rules, items, primary, Weapon.Hand.TWO_HANDED_TWF, priSizeCategory, priSizeDiscrepancy, -4);
                            } else if (primary.CanBeThrown()) {
                                primaryForSpikes = Attack.Thrown(rules, items, primary, Weapon.Hand.PRIMARY_HAND_TWF, priSizeCategory, priSizeDiscrepancy, -4);
                            }

                            if (primaryForSpikes != null) {
                                Attack spikesTwf = Attack.ArmorSpikes(rules, items, Weapon.Hand.OFF_HAND_TWF, armSizeCategory, -8);
                                var twfWithSpikes = BuildProgressionTierAttacks(rules, items, primaryForSpikes, spikesTwf);
                                routines.Add(new AttackRoutine(
                                    RoutineActionType.FullAttack,
                                    RoutineCategory.ArmorSpikesTWF,
                                    $"{primary.Name} + Armor Spikes Full Attack",
                                    new[] { primary },
                                    twfWithSpikes
                                ));
                            }
                        }
                    } else if (priEffort < Weapon.Effort.TWO_HANDED) {
                        // One-handed primary attacks (shield present or off-hand weapon equipped)
                        if (primary.Melee != null) {
                            Attack meleeAtk = Attack.Melee(rules, items, primary, Weapon.Hand.ONE_HANDED, priSizeCategory, priSizeDiscrepancy);
                            if (meleeAtk != null) {
                                routines.Add(new AttackRoutine(
                                    RoutineActionType.Standard,
                                    RoutineCategory.PrimarySolo,
                                    $"{primary.Name} Standard Attack",
                                    new[] { primary },
                                    new[] { meleeAtk }
                                ));
                                var fullAtks = BuildProgressionTierAttacks(rules, items, meleeAtk);
                                if (fullAtks.Count > 1) {
                                    routines.Add(new AttackRoutine(
                                        RoutineActionType.FullAttack,
                                        RoutineCategory.PrimarySolo,
                                        $"{primary.Name} Full Attack",
                                        new[] { primary },
                                        fullAtks
                                    ));
                                }
                            }
                        }

                        if (primary.Projectile != null) {
                            Attack projAtk = Attack.Projectile(rules, items, primary, Weapon.Hand.ONE_HANDED, priSizeCategory, priSizeDiscrepancy);
                            if (projAtk != null) {
                                routines.Add(new AttackRoutine(
                                    RoutineActionType.Standard,
                                    RoutineCategory.PrimarySolo,
                                    $"{primary.Name} Standard Attack",
                                    new[] { primary },
                                    new[] { projAtk }
                                ));
                                var fullAtks = BuildProgressionTierAttacks(rules, items, projAtk);
                                if (fullAtks.Count > 1) {
                                    routines.Add(new AttackRoutine(
                                        RoutineActionType.FullAttack,
                                        RoutineCategory.PrimarySolo,
                                        $"{primary.Name} Full Attack",
                                        new[] { primary },
                                        fullAtks
                                    ));
                                }
                            }
                        }

                        if (primary.CanBeThrown()) {
                            Attack thrownAtk = Attack.Thrown(rules, items, primary, Weapon.Hand.ONE_HANDED, priSizeCategory, priSizeDiscrepancy);
                            if (thrownAtk != null) {
                                routines.Add(new AttackRoutine(
                                    RoutineActionType.Standard,
                                    RoutineCategory.PrimarySolo,
                                    $"{primary.Name} Thrown Standard Attack",
                                    new[] { primary },
                                    new[] { thrownAtk }
                                ));
                                var fullAtks = BuildProgressionTierAttacks(rules, items, thrownAtk);
                                if (fullAtks.Count > 1) {
                                    routines.Add(new AttackRoutine(
                                        RoutineActionType.FullAttack,
                                        RoutineCategory.PrimarySolo,
                                        $"{primary.Name} Thrown Full Attack",
                                        new[] { primary },
                                        fullAtks
                                    ));
                                }
                            }
                        }

                        // TWF with off-hand weapon
                        if (offHand != null) {
                            int offSizeCategory = AdjustSize(offHand.SizeCategory, sizeDiscrepancy);
                            int offSizeDiscrepancy = offSizeCategory - playerSizeCategory;
                            Weapon.Effort offEffort = offHand.EffortCategory + offSizeDiscrepancy;

                            if (offEffort == Weapon.Effort.LIGHT || offEffort == Weapon.Effort.ONE_HANDED) {
                                int twfPri = offEffort == Weapon.Effort.LIGHT ? -4 : -6;
                                int twfOff = offEffort == Weapon.Effort.LIGHT ? -8 : -10;

                                Attack priTwf = primary.Melee != null
                                    ? Attack.Melee(rules, items, primary, Weapon.Hand.PRIMARY_HAND_TWF, priSizeCategory, priSizeDiscrepancy, twfPri)
                                    : (primary.Projectile != null
                                        ? Attack.Projectile(rules, items, primary, Weapon.Hand.PRIMARY_HAND_TWF, priSizeCategory, priSizeDiscrepancy, twfPri)
                                        : Attack.Thrown(rules, items, primary, Weapon.Hand.PRIMARY_HAND_TWF, priSizeCategory, priSizeDiscrepancy, twfPri));

                                Attack offTwf = offHand.Melee != null
                                    ? Attack.Melee(rules, items, offHand, Weapon.Hand.OFF_HAND_TWF, offSizeCategory, offSizeDiscrepancy, twfOff)
                                    : (offHand.Projectile != null
                                        ? Attack.Projectile(rules, items, offHand, Weapon.Hand.OFF_HAND_TWF, offSizeCategory, offSizeDiscrepancy, twfOff)
                                        : Attack.Thrown(rules, items, offHand, Weapon.Hand.OFF_HAND_TWF, offSizeCategory, offSizeDiscrepancy, twfOff));

                                if (priTwf != null && offTwf != null) {
                                    var twfAttacks = BuildProgressionTierAttacks(rules, items, priTwf, offTwf);
                                    routines.Add(new AttackRoutine(
                                        RoutineActionType.FullAttack,
                                        RoutineCategory.TwoWeaponFighting,
                                        $"{primary.Name} / {offHand.Name} Full Attack",
                                        new[] { primary, offHand },
                                        twfAttacks
                                    ));
                                }
                            }
                        }

                        // TWF with Shield Bash
                        if (shieldBashAvailable) {
                            Weapon.Effort offEffort = (Weapon.Effort)shield.ShieldBashEffort;
                            int twfPri = offEffort == Weapon.Effort.LIGHT ? -4 : -6;
                            int twfOff = offEffort == Weapon.Effort.LIGHT ? -8 : -10;

                            Attack priTwf = primary.Melee != null
                                ? Attack.Melee(rules, items, primary, Weapon.Hand.PRIMARY_HAND_TWF, priSizeCategory, priSizeDiscrepancy, twfPri)
                                : (primary.Projectile != null
                                    ? Attack.Projectile(rules, items, primary, Weapon.Hand.PRIMARY_HAND_TWF, priSizeCategory, priSizeDiscrepancy, twfPri)
                                    : Attack.Thrown(rules, items, primary, Weapon.Hand.PRIMARY_HAND_TWF, priSizeCategory, priSizeDiscrepancy, twfPri));

                            Attack shieldTwf = Attack.ShieldBash(rules, items, Weapon.Hand.OFF_HAND_TWF, shSizeCategory, shSizeDiscrepancy, twfOff);
                            if (priTwf != null && shieldTwf != null) {
                                var shieldTwfAttacks = BuildProgressionTierAttacks(rules, items, priTwf, shieldTwf);
                                routines.Add(new AttackRoutine(
                                    RoutineActionType.FullAttack,
                                    RoutineCategory.OffHandShieldBash,
                                    $"{primary.Name} / Shield Bash Full Attack",
                                    new[] { primary },
                                    shieldTwfAttacks
                                ));
                            }
                        }

                        // TWF with Armor Spikes
                        if (spikesAvailable) {
                            Attack priTwf = primary.Melee != null
                                ? Attack.Melee(rules, items, primary, Weapon.Hand.PRIMARY_HAND_TWF, priSizeCategory, priSizeDiscrepancy, -4)
                                : (primary.Projectile != null
                                    ? Attack.Projectile(rules, items, primary, Weapon.Hand.PRIMARY_HAND_TWF, priSizeCategory, priSizeDiscrepancy, -4)
                                    : Attack.Thrown(rules, items, primary, Weapon.Hand.PRIMARY_HAND_TWF, priSizeCategory, priSizeDiscrepancy, -4));

                            Attack spikesTwf = Attack.ArmorSpikes(rules, items, Weapon.Hand.OFF_HAND_TWF, armSizeCategory, -8);
                            if (priTwf != null && spikesTwf != null) {
                                var spikesTwfAttacks = BuildProgressionTierAttacks(rules, items, priTwf, spikesTwf);
                                routines.Add(new AttackRoutine(
                                    RoutineActionType.FullAttack,
                                    RoutineCategory.ArmorSpikesTWF,
                                    $"{primary.Name} / Armor Spikes Full Attack",
                                    new[] { primary },
                                    spikesTwfAttacks
                                ));
                            }
                        }
                    }
                }
            }

            // 3. Off-hand weapon standalone attack (if off-hand equipped)
            if (offHand != null) {
                int offSizeCategory = AdjustSize(offHand.SizeCategory, sizeDiscrepancy);
                int offSizeDiscrepancy = offSizeCategory - playerSizeCategory;
                if (offHand.Melee != null) {
                    Attack offAtk = Attack.Melee(rules, items, offHand, Weapon.Hand.ONE_HANDED, offSizeCategory, offSizeDiscrepancy);
                    if (offAtk != null) {
                        routines.Add(new AttackRoutine(
                            RoutineActionType.Standard,
                            RoutineCategory.PrimarySolo,
                            $"{offHand.Name} Standard Attack",
                            new[] { offHand },
                            new[] { offAtk }
                        ));
                    }
                }
                if (offHand.Projectile != null) {
                    Attack offProj = Attack.Projectile(rules, items, offHand, Weapon.Hand.ONE_HANDED, offSizeCategory, offSizeDiscrepancy);
                    if (offProj != null) {
                        routines.Add(new AttackRoutine(
                            RoutineActionType.Standard,
                            RoutineCategory.PrimarySolo,
                            $"{offHand.Name} Standard Attack",
                            new[] { offHand },
                            new[] { offProj }
                        ));
                    }
                }
                if (offHand.CanBeThrown()) {
                    Attack offThr = Attack.Thrown(rules, items, offHand, Weapon.Hand.ONE_HANDED, offSizeCategory, offSizeDiscrepancy);
                    if (offThr != null) {
                        routines.Add(new AttackRoutine(
                            RoutineActionType.Standard,
                            RoutineCategory.PrimarySolo,
                            $"{offHand.Name} Thrown Standard Attack",
                            new[] { offHand },
                            new[] { offThr }
                        ));
                    }
                }
            }

            return routines;
        }
    }
}
