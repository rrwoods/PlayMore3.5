# Dead Code & Zombie Island Report

*Generated at: 2026-11-05 19:38:53 UTC*

## Summary

| Metric | Count |
| :--- | :--- |
| **Total Methods Scanned** | 1809 |
| **Live Methods (Reachable from UI/Main)** | 1548 (85.6%) |
| **Dead Methods (Unreachable)** | 261 |
| **Zombie Islands Detected** | 239 |

## Zombie Islands Breakdown

A **Zombie Island** is an unreachable call tree that appears alive because it has callers or is kept on artificial life support by unit tests.

### Island 1: `PlayMore35.Items.Armor`

**Island Roots (No production callers):**
- `PlayMore35.Items.Armor..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\CharacterChoices\Armor.cs:72)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.NonNullCollectionInvariantTests.Armor_Constructors_EnsureCollectionsAreNonNull` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\NonNullCollectionInvariantTests.cs:102)

---

### Island 2: `PlayMore35.Items.ArmorProperty`

**Island Roots (No production callers):**
- `PlayMore35.Items.ArmorProperty..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\CharacterChoices\ArmorProperty.cs:55)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.ViewProjectionTests.Equipped_ContributeEffects_PushesItemEffectsAndFlagsRespectingDonnedArmorOverride` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\ViewProjectionTests.cs:262)

---

### Island 3: `PlayMore35.Items.Attack`

**Island Roots (No production callers):**
- `PlayMore35.Items.Attack.get_DamageType` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\CharacterChoices\Attack.cs:67)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 4: `PlayMore35.Items.Attack`

**Island Roots (No production callers):**
- `PlayMore35.Items.Attack.CreateForTesting` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\CharacterChoices\Attack.cs:72)

**Cascaded Methods (Downstream dead calls):**
- `PlayMore35.Items.Attack..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\CharacterChoices\Attack.cs:90)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.AtomicAttackTests.Attack_ComponentizesDamage_IntoBaseAndBonusDice` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\AtomicAttackTests.cs:20)
- 🧪 `PlayMore35.Tests.AtomicAttackTests.Attack_CriticalHits_MultiplyBaseDamageOnly_LeavingBonusDiceUnmultiplied` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\AtomicAttackTests.cs:41)
- 🧪 `PlayMore35.Tests.AtomicAttackTests.Attack_CriticalBonusDice_IncludesBurstDice_OnCriticalDamage` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\AtomicAttackTests.cs:62)
- 🧪 `PlayMore35.Tests.AtomicAttackTests.AttackRoutine_InstantiatesWithStructuredMetadata` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\AtomicAttackTests.cs:84)

---

### Island 5: `PlayMore35.Items.AttackRoutine`

**Island Roots (No production callers):**
- `PlayMore35.Items.AttackRoutine.get_Weapons` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\CharacterChoices\AttackRoutine.cs:24)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 6: `PlayMore35.CharacterChoices.Character`

**Island Roots (No production callers):**
- `PlayMore35.CharacterChoices.Character.get_rounds` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\CharacterChoices\Character.cs:48)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 7: `PlayMore35.CharacterChoices.Character`

**Island Roots (No production callers):**
- `PlayMore35.CharacterChoices.Character.set_rounds` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\CharacterChoices\Character.cs:49)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 8: `PlayMore35.CharacterChoices.Character`

**Island Roots (No production callers):**
- `PlayMore35.CharacterChoices.Character.get_Weapons` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\CharacterChoices\Character.cs:219)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 9: `PlayMore35.CharacterChoices.Character`

**Island Roots (No production callers):**
- `PlayMore35.CharacterChoices.Character.get_Armor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\CharacterChoices\Character.cs:225)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 10: `PlayMore35.CharacterChoices.Character`

**Island Roots (No production callers):**
- `PlayMore35.CharacterChoices.Character.get_Shields` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\CharacterChoices\Character.cs:231)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 11: `PlayMore35.CharacterChoices.CharacterClass`

**Island Roots (No production callers):**
- `PlayMore35.CharacterChoices.CharacterClass..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\CharacterChoices\CharacterClass.cs:49)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.DualPassPrerequisiteTests.ValidateProgressionChoice_FirstLevelClass_PrerequisitesEvaluatedAgainstLevel0State` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\DualPassPrerequisiteTests.cs:377)
- 🧪 `PlayMore35.Tests.DualPassPrerequisiteTests.ValidateProgressionChoice_ClassChoiceAtLevelL_EvaluatesAgainstLevelLMinus1_NoLeakageOfLevelLBenefits` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\DualPassPrerequisiteTests.cs:419)
- 🧪 `PlayMore35.Tests.DualPassPrerequisiteTests.ValidateProgressionChoice_ClassChoice_DualPassItemQualification_CategorizesAsItemDependent` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\DualPassPrerequisiteTests.cs:473)
- 🧪 `PlayMore35.Tests.DualPassPrerequisiteTests.ValidateProgressionChoice_ClassChoice_PartialItemImprovement_RemainsUnmet` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\DualPassPrerequisiteTests.cs:517)
- 🧪 `PlayMore35.Tests.FakeRulesCatalog.BuildMartialManeuversSubsystem` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\FakeRulesCatalog.cs:77)

---

### Island 12: `PlayMore35.CharacterChoices.SubsystemFeature`

**Island Roots (No production callers):**
- `PlayMore35.CharacterChoices.SubsystemFeature.set_Verbs` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\CharacterChoices\CharacterClass.cs:165)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 13: `PlayMore35.CharacterChoices.ChoiceDescriptor`

**Island Roots (No production callers):**
- `PlayMore35.CharacterChoices.ChoiceDescriptor..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\CharacterChoices\Feat.cs:18)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.CharacterSessionFeatTests.ChoiceDescriptor_JsonSerializationAndDeserialization_RoundTripsAllFields` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\CharacterSessionFeatTests.cs:338)
- 🧪 `PlayMore35.Tests.CharacterSessionFeatTests.ChoiceDescriptor_JsonSerializationAndDeserialization_PreservesMinimumPrerequisiteCountWhenSet_AndOmitsWhenZero` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\CharacterSessionFeatTests.cs:360)
- 🧪 `PlayMore35.Tests.CharacterSessionFeatTests.ValidateProgressionChoice_MartialStance_RequiresOneManeuverInDisciplineViaChoiceDescriptorMinimumPrereqCount` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\CharacterSessionFeatTests.cs:1319)
- 🧪 `PlayMore35.Tests.CharacterSessionTests.CharacterSession_ResolveSubsystemElementChoices_ResolvesViaDirectEngines` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\CharacterSessionTests.cs:915)
- 🧪 `PlayMore35.Tests.RulesCatalogTests.RulesCatalog_FindFeatByChoiceDescriptor_FindsMatchingFeat` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\RulesCatalogTests.cs:335)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_ValidateChoice_EmitsAdvisoryWarning_WhenElementExceedsMaxLevel` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:630)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_ValidateChoice_EmitsInvalidWarning_WhenElementDoesNotMatchFilter` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:682)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_ValidateChoice_WorksPurelyThroughDeclarativeActiveSlotState` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:1030)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.ChoiceDescriptor_SlotGroup_RoundTripSerialization` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:1442)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_ValidateChoice_EmitsCapacityWarning_WhenSlotGroupCapacityReached` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:1476)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_ValidateChoice_CapacityWarning_FormatsWithoutSource_WhenSourceNullOrEmpty` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:1502)

---

### Island 14: `PlayMore35.CharacterChoices.Feat`

**Island Roots (No production callers):**
- `PlayMore35.CharacterChoices.Feat..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\CharacterChoices\Feat.cs:91)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.NonNullCollectionInvariantTests.Feat_Constructors_EnsureCollectionsAreNonNull` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\NonNullCollectionInvariantTests.cs:66)

---

### Island 15: `PlayMore35.CharacterChoices.GpTransaction`

**Island Roots (No production callers):**
- `PlayMore35.CharacterChoices.GpTransaction.CompareTo` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\CharacterChoices\GpTransaction.cs:38)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 16: `PlayMore35.CharacterChoices.IReadOnlyCharacter`

**Island Roots (No production callers):**
- `PlayMore35.CharacterChoices.IReadOnlyCharacter.get_externalEffects` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\CharacterChoices\IReadOnlyCharacter.cs:25)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 17: `PlayMore35.CharacterChoices.IReadOnlyCharacter`

**Island Roots (No production callers):**
- `PlayMore35.CharacterChoices.IReadOnlyCharacter.get_Subsystems` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\CharacterChoices\IReadOnlyCharacter.cs:26)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 18: `PlayMore35.CharacterChoices.IReadOnlyCharacter`

**Island Roots (No production callers):**
- `PlayMore35.CharacterChoices.IReadOnlyCharacter.get_flagValues` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\CharacterChoices\IReadOnlyCharacter.cs:27)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 19: `PlayMore35.CharacterChoices.IReadOnlyCharacter`

**Island Roots (No production callers):**
- `PlayMore35.CharacterChoices.IReadOnlyCharacter.DetailClassLevels` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\CharacterChoices\IReadOnlyCharacter.cs:36)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 20: `PlayMore35.Items.Item`

**Island Roots (No production callers):**
- `PlayMore35.Items.Item..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\CharacterChoices\Item.cs:41)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.CharacterSessionFeatTests.ValidateProgressionChoice_InFlightOverlay_DualPassSatisfiedByGear_CategorizesAsItemDependent` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\CharacterSessionFeatTests.cs:1588)
- 🧪 `PlayMore35.Tests.DualPassPrerequisiteTests.DualPass_PrerequisiteSatisfiedByEquippedItem_ReturnsWarningKindItemDependent` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\DualPassPrerequisiteTests.cs:187)
- 🧪 `PlayMore35.Tests.DualPassPrerequisiteTests.DualPass_LevelGating_RemainsUnmetEvenIfEquippedGearProvidesStatBoosts` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\DualPassPrerequisiteTests.cs:230)
- 🧪 `PlayMore35.Tests.DualPassPrerequisiteTests.LevelDetailsDialog_WithItemDependentChoice_DisplaysItemDependentBadge` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\DualPassPrerequisiteTests.cs:313)
- 🧪 `PlayMore35.Tests.DualPassPrerequisiteTests.ValidateProgressionChoice_ClassChoice_DualPassItemQualification_CategorizesAsItemDependent` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\DualPassPrerequisiteTests.cs:473)
- 🧪 `PlayMore35.Tests.DualPassPrerequisiteTests.ValidateProgressionChoice_ClassChoice_PartialItemImprovement_RemainsUnmet` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\DualPassPrerequisiteTests.cs:517)
- 🧪 `PlayMore35.Tests.DualPassPrerequisiteTests.ValidateProgressionChoice_FeatSourcedSubsystem_DualPassPrerequisiteSatisfiedByGear_CategorizesAsItemDependent` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\DualPassPrerequisiteTests.cs:562)
- 🧪 `PlayMore35.Tests.DualPassPrerequisiteTests.ValidateProgressionChoice_FeatSourcedSubsystem_LevelGateExceeded_RemainsUnmetEvenWithStatBoostItem` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\DualPassPrerequisiteTests.cs:602)
- 🧪 `PlayMore35.Tests.DualPassPrerequisiteTests.ValidateProgressionChoice_ClassGrantedSubsystem_DualPassPrerequisiteSatisfiedByGear_CategorizesAsItemDependent` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\DualPassPrerequisiteTests.cs:642)
- 🧪 `PlayMore35.Tests.DualPassPrerequisiteTests.ValidateProgressionChoice_ClassGrantedSubsystem_LevelGateExceeded_RemainsUnmetEvenWithStatBoostItem` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\DualPassPrerequisiteTests.cs:681)
- 🧪 `PlayMore35.Tests.DualPassPrerequisiteTests.ValidateProgressionChoice_VestigeChoice_LevelGateRemainsUnmetEvenWithEquippedItem` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\DualPassPrerequisiteTests.cs:927)
- 🧪 `PlayMore35.Tests.DualPassPrerequisiteTests.ValidateProgressionChoice_OnSnapshotSessionWithOverlay_EvaluatesDualPassItemDependency` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\DualPassPrerequisiteTests.cs:988)
- 🧪 `PlayMore35.Tests.NonNullCollectionInvariantTests.Item_Constructors_EnsureCollectionsAreNonNull` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\NonNullCollectionInvariantTests.cs:87)
- 🧪 `PlayMore35.Tests.ViewProjectionTests.Equipped_ContributeEffects_PushesItemEffectsAndFlagsRespectingDonnedArmorOverride` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\ViewProjectionTests.cs:262)

---

### Island 21: `PlayMore35.CharacterChoices.ReadOnlyCharacter`

**Island Roots (No production callers):**
- `PlayMore35.CharacterChoices.ReadOnlyCharacter.get_externalEffects` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\CharacterChoices\ReadOnlyCharacter.cs:32)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 22: `PlayMore35.CharacterChoices.ReadOnlyCharacter`

**Island Roots (No production callers):**
- `PlayMore35.CharacterChoices.ReadOnlyCharacter.get_Subsystems` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\CharacterChoices\ReadOnlyCharacter.cs:33)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 23: `PlayMore35.CharacterChoices.ReadOnlyCharacter`

**Island Roots (No production callers):**
- `PlayMore35.CharacterChoices.ReadOnlyCharacter.get_flagValues` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\CharacterChoices\ReadOnlyCharacter.cs:34)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 24: `PlayMore35.CharacterChoices.ReadOnlyCharacter`

**Island Roots (No production callers):**
- `PlayMore35.CharacterChoices.ReadOnlyCharacter.DetailClassLevels` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\CharacterChoices\ReadOnlyCharacter.cs:43)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 25: `PlayMore35.Items.Shield`

**Island Roots (No production callers):**
- `PlayMore35.Items.Shield..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\CharacterChoices\Shield.cs:98)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.NonNullCollectionInvariantTests.Shield_Constructors_EnsureCollectionsAreNonNull` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\NonNullCollectionInvariantTests.cs:138)

---

### Island 26: `PlayMore35.Items.Weapon`

**Island Roots (No production callers):**
- `PlayMore35.Items.Weapon..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\CharacterChoices\Weapon.cs:100)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.NonNullCollectionInvariantTests.Weapon_Constructors_EnsureCollectionsAreNonNull` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\NonNullCollectionInvariantTests.cs:122)

---

### Island 27: `PlayMore35.Items.WeaponProperty`

**Island Roots (No production callers):**
- `PlayMore35.Items.WeaponProperty..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\CharacterChoices\WeaponProperty.cs:64)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 28: `PlayMore35.CharacterChoices.XpSessionEntry`

**Island Roots (No production callers):**
- `PlayMore35.CharacterChoices.XpSessionEntry.CompareTo` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\CharacterChoices\XpSessionEntry.cs:38)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 29: `PlayMore35.Effects.ActionResource`

**Island Roots (No production callers):**
- `PlayMore35.Effects.ActionResource.ComputeDuration` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\ActionResource.cs:69)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 30: `PlayMore35.Effects.AttackMatcher`

**Island Roots (No production callers):**
- `PlayMore35.Effects.AttackMatcher.Names` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\AttackMatcher.cs:69)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 31: `PlayMore35.Effects.AttackMatcher`

**Island Roots (No production callers):**
- `PlayMore35.Effects.AttackMatcher.Tag` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\AttackMatcher.cs:74)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 32: `PlayMore35.Effects.AttackMatcher`

**Island Roots (No production callers):**
- `PlayMore35.Effects.AttackMatcher.SizeCategory` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\AttackMatcher.cs:79)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 33: `PlayMore35.Effects.AttackMatcher`

**Island Roots (No production callers):**
- `PlayMore35.Effects.AttackMatcher.Simple` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\AttackMatcher.cs:84)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 34: `PlayMore35.Effects.AttackMatcher`

**Island Roots (No production callers):**
- `PlayMore35.Effects.AttackMatcher.Martial` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\AttackMatcher.cs:89)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 35: `PlayMore35.Effects.AttackMatcher`

**Island Roots (No production callers):**
- `PlayMore35.Effects.AttackMatcher.SimpleOrMartial` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\AttackMatcher.cs:94)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 36: `PlayMore35.Effects.AttackMatcher`

**Island Roots (No production callers):**
- `PlayMore35.Effects.AttackMatcher.Exotic` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\AttackMatcher.cs:102)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 37: `PlayMore35.Effects.AttackMatcher`

**Island Roots (No production callers):**
- `PlayMore35.Effects.AttackMatcher.Light` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\AttackMatcher.cs:107)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 38: `PlayMore35.Effects.AttackMatcher`

**Island Roots (No production callers):**
- `PlayMore35.Effects.AttackMatcher.OneHandedWeapon` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\AttackMatcher.cs:112)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 39: `PlayMore35.Effects.AttackMatcher`

**Island Roots (No production callers):**
- `PlayMore35.Effects.AttackMatcher.PrimaryHandAttack` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\AttackMatcher.cs:117)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 40: `PlayMore35.Effects.AttackMatcher`

**Island Roots (No production callers):**
- `PlayMore35.Effects.AttackMatcher.PrimaryHandTwfAttack` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\AttackMatcher.cs:125)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 41: `PlayMore35.Effects.AttackMatcher`

**Island Roots (No production callers):**
- `PlayMore35.Effects.AttackMatcher.OffHandAttack` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\AttackMatcher.cs:132)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 42: `PlayMore35.Effects.AttackMatcher`

**Island Roots (No production callers):**
- `PlayMore35.Effects.AttackMatcher.OneHandedAttack` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\AttackMatcher.cs:139)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 43: `PlayMore35.Effects.AttackMatcher`

**Island Roots (No production callers):**
- `PlayMore35.Effects.AttackMatcher.NonOffHandAttack` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\AttackMatcher.cs:148)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 44: `PlayMore35.Effects.AttackMatcher`

**Island Roots (No production callers):**
- `PlayMore35.Effects.AttackMatcher.TwoHandedAttack` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\AttackMatcher.cs:160)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 45: `PlayMore35.Effects.AttackMatcher`

**Island Roots (No production callers):**
- `PlayMore35.Effects.AttackMatcher.Melee` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\AttackMatcher.cs:165)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 46: `PlayMore35.Effects.AttackMatcher`

**Island Roots (No production callers):**
- `PlayMore35.Effects.AttackMatcher.Ranged` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\AttackMatcher.cs:173)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 47: `PlayMore35.Effects.AttackMatcher`

**Island Roots (No production callers):**
- `PlayMore35.Effects.AttackMatcher.Thrown` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\AttackMatcher.cs:182)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 48: `PlayMore35.Effects.AttackMatcher`

**Island Roots (No production callers):**
- `PlayMore35.Effects.AttackMatcher.Bow` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\AttackMatcher.cs:189)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 49: `PlayMore35.Effects.NameMatcher`

**Island Roots (No production callers):**
- `PlayMore35.Effects.NameMatcher..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\AttackMatcher.cs:213)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 50: `PlayMore35.Effects.TagMatcher`

**Island Roots (No production callers):**
- `PlayMore35.Effects.TagMatcher..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\AttackMatcher.cs:237)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 51: `PlayMore35.Effects.SizeMatcher`

**Island Roots (No production callers):**
- `PlayMore35.Effects.SizeMatcher..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\AttackMatcher.cs:261)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 52: `PlayMore35.Effects.ProficiencyMatcher`

**Island Roots (No production callers):**
- `PlayMore35.Effects.ProficiencyMatcher..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\AttackMatcher.cs:285)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 53: `PlayMore35.Effects.EffortMatcher`

**Island Roots (No production callers):**
- `PlayMore35.Effects.EffortMatcher..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\AttackMatcher.cs:318)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 54: `PlayMore35.Effects.HandMatcher`

**Island Roots (No production callers):**
- `PlayMore35.Effects.HandMatcher..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\AttackMatcher.cs:347)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 55: `PlayMore35.Effects.RangeMatcher`

**Island Roots (No production callers):**
- `PlayMore35.Effects.RangeMatcher..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\AttackMatcher.cs:368)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 56: `PlayMore35.Effects.ChoiceToggle`

**Island Roots (No production callers):**
- `PlayMore35.Effects.ChoiceToggle.get_Expected` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\ChoiceToggle.cs:10)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 57: `PlayMore35.Effects.ChoiceToggle`

**Island Roots (No production callers):**
- `PlayMore35.Effects.ChoiceToggle.get_Actual` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\ChoiceToggle.cs:11)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 58: `PlayMore35.Effects.ConditionalToggle`

**Island Roots (No production callers):**
- `PlayMore35.Effects.ConditionalToggle..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\ConditionalToggle.cs:24)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 59: `PlayMore35.Effects.ConditionalToggle`

**Island Roots (No production callers):**
- `PlayMore35.Effects.ConditionalToggle..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\ConditionalToggle.cs:30)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 60: `PlayMore35.Effects.ConditionalToggle`

**Island Roots (No production callers):**
- `PlayMore35.Effects.ConditionalToggle..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\ConditionalToggle.cs:35)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 61: `PlayMore35.Effects.ControllableFlag`

**Island Roots (No production callers):**
- `PlayMore35.Effects.ControllableFlag..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\ControllableFlag.cs:28)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.CombatTransitionTests.EndCombat_SwitchesCombatMode_ZeroesEncounterParameters_AndExecutesEncounterActions` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\CombatTransitionTests.cs:41)
- 🧪 `PlayMore35.Tests.SheetActionTests.CreateTestContext` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SheetActionTests.cs:17)
- 🧪 `PlayMore35.Tests.SheetActionTests.SheetAction_ChainedAction_FiresUponExpiration` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SheetActionTests.cs:180)
- 🧪 `PlayMore35.Tests.SkillTextSectionPanelTests.SkillTextSectionPanel_Update_HandlesActiveAndDisabledConditions` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SkillTextSectionPanelTests.cs:55)

---

### Island 62: `PlayMore35.Effects.Effect`

**Island Roots (No production callers):**
- `PlayMore35.Effects.Effect.AddSlot` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\Effect.cs:237)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.CharacterSessionFeatTests.ValidateProgressionChoice_InFlightOverlay_DualPassSatisfiedByGear_CategorizesAsItemDependent` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\CharacterSessionFeatTests.cs:1588)
- 🧪 `PlayMore35.Tests.DualPassPrerequisiteTests.DualPass_PrerequisiteSatisfiedByEquippedItem_ReturnsWarningKindItemDependent` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\DualPassPrerequisiteTests.cs:187)
- 🧪 `PlayMore35.Tests.DualPassPrerequisiteTests.LevelDetailsDialog_WithItemDependentChoice_DisplaysItemDependentBadge` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\DualPassPrerequisiteTests.cs:313)
- 🧪 `PlayMore35.Tests.DualPassPrerequisiteTests.ValidateProgressionChoice_FeatSourcedSubsystem_DualPassPrerequisiteSatisfiedByGear_CategorizesAsItemDependent` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\DualPassPrerequisiteTests.cs:562)
- 🧪 `PlayMore35.Tests.DualPassPrerequisiteTests.ValidateProgressionChoice_ClassGrantedSubsystem_DualPassPrerequisiteSatisfiedByGear_CategorizesAsItemDependent` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\DualPassPrerequisiteTests.cs:642)
- 🧪 `PlayMore35.Tests.DualPassPrerequisiteTests.ValidateProgressionChoice_OnSnapshotSessionWithOverlay_EvaluatesDualPassItemDependency` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\DualPassPrerequisiteTests.cs:988)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.Effect_Slots_SerializationAndDeserialization` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:72)

---

### Island 63: `PlayMore35.Effects.Effect`

**Island Roots (No production callers):**
- `PlayMore35.Effects.Effect.SetParameter` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\Effect.cs:242)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.TextEffectPanelTests.TextEffectPanel_EffectWithInlineParameterAndAssociatedActions_RendersInlineLabeledFieldAndContextMenu` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\TextEffectPanelTests.cs:53)
- 🧪 `PlayMore35.Tests.TextEffectPanelTests.TextEffectPanel_MultiCategoryEffectWithInlineParameter_SynchronizesInlineLabeledFieldsAcrossPanels` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\TextEffectPanelTests.cs:115)
- 🧪 `PlayMore35.Tests.TextEffectPanelTests.TextEffectPanel_RecycledEffectLabel_UpdatesContextMenuStripWhenAssociatedActionsChange` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\TextEffectPanelTests.cs:165)
- 🧪 `PlayMore35.Tests.ViewProjectionTests.CharacterSession_EffectWithInlineParameterAndAssociatedActions_RegistersParameterAndProjectsToTextEffects` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\ViewProjectionTests.cs:175)
- 🧪 `PlayMore35.Tests.ViewProjectionTests.EffectPool_AddEffect_AutomaticallyRegistersInlineParameterAndResourceUses` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\ViewProjectionTests.cs:225)

---

### Island 64: `PlayMore35.Effects.Effect`

**Island Roots (No production callers):**
- `PlayMore35.Effects.Effect.AddAction` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\Effect.cs:247)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.TextEffectPanelTests.TextEffectPanel_EffectWithInlineParameterAndAssociatedActions_RendersInlineLabeledFieldAndContextMenu` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\TextEffectPanelTests.cs:53)
- 🧪 `PlayMore35.Tests.TextEffectPanelTests.TextEffectPanel_RecycledEffectLabel_UpdatesContextMenuStripWhenAssociatedActionsChange` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\TextEffectPanelTests.cs:165)
- 🧪 `PlayMore35.Tests.ViewProjectionTests.CharacterSession_EffectWithInlineParameterAndAssociatedActions_RegistersParameterAndProjectsToTextEffects` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\ViewProjectionTests.cs:175)

---

### Island 65: `PlayMore35.Effects.Effect`

**Island Roots (No production callers):**
- `PlayMore35.Effects.Effect.SetToggle` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\Effect.cs:252)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 66: `PlayMore35.Effects.Effect`

**Island Roots (No production callers):**
- `PlayMore35.Effects.Effect.AddExtraAttack` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\Effect.cs:257)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 67: `PlayMore35.Effects.Effect`

**Island Roots (No production callers):**
- `PlayMore35.Effects.Effect.AddAttackModifier` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\Effect.cs:265)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.MartialManeuverSessionIntegrationTests.ActiveStance_AffectingEquippedWeaponAttacks_RecalculatesRoutines` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverSessionIntegrationTests.cs:160)

---

### Island 68: `PlayMore35.Effects.Effect`

**Island Roots (No production callers):**
- `PlayMore35.Effects.Effect.AddDamageModifier` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\Effect.cs:274)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 69: `PlayMore35.Effects.Effect`

**Island Roots (No production callers):**
- `PlayMore35.Effects.Effect.AddExtraDice` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\Effect.cs:283)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 70: `PlayMore35.Effects.Effect`

**Island Roots (No production callers):**
- `PlayMore35.Effects.Effect.AddMultiplier` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\Effect.cs:300)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 71: `PlayMore35.Effects.Effect`

**Island Roots (No production callers):**
- `PlayMore35.Effects.Effect.AddExoticArmorProficiency` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\Effect.cs:324)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 72: `PlayMore35.Effects.Effect`

**Island Roots (No production callers):**
- `PlayMore35.Effects.Effect.AddFlag` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\Effect.cs:339)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 73: `PlayMore35.Effects.EffectPool`

**Island Roots (No production callers):**
- `PlayMore35.Effects.EffectPool.set_SubTypes` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\EffectPool.cs:28)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 74: `PlayMore35.Effects.EffectPool`

**Island Roots (No production callers):**
- `PlayMore35.Effects.EffectPool.get_GrantedClassSkills` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\EffectPool.cs:30)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 75: `PlayMore35.Effects.EffectPool`

**Island Roots (No production callers):**
- `PlayMore35.Effects.EffectPool.get_Effects` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\EffectPool.cs:165)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 76: `PlayMore35.Effects.EffectPool`

**Island Roots (No production callers):**
- `PlayMore35.Effects.EffectPool.FlagsByCategory` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\EffectPool.cs:288)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 77: `PlayMore35.Effects.EffectPool`

**Island Roots (No production callers):**
- `PlayMore35.Effects.EffectPool.GenerateFeatEffects` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\EffectPool.cs:457)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.MartialAdeptProgressionTests.ActiveStances_EvaluatesCorrectly_AcrossNonAdept_Multiclass_AndFeat` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialAdeptProgressionTests.cs:419)
- 🧪 `PlayMore35.Tests.RulesCatalogTests.Catalog_TextEffectCategories_ExistInConstants` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\RulesCatalogTests.cs:164)

---

### Island 78: `PlayMore35.Effects.StringExtensions`

**Island Roots (No production callers):**
- `PlayMore35.Effects.StringExtensions.FillStatValues` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\EffectPool.cs:487)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 79: `PlayMore35.Effects.ExtraAttack`

**Island Roots (No production callers):**
- `PlayMore35.Effects.ExtraAttack..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\ExtraAttack.cs:30)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 80: `PlayMore35.Effects.FlagDefinition`

**Island Roots (No production callers):**
- `PlayMore35.Effects.FlagDefinition..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\FlagDefinition.cs:12)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 81: `PlayMore35.Effects.FlagDefinition`

**Island Roots (No production callers):**
- `PlayMore35.Effects.FlagDefinition..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\FlagDefinition.cs:26)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.ViewProjectionTests.Equipped_ContributeEffects_PushesItemEffectsAndFlagsRespectingDonnedArmorOverride` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\ViewProjectionTests.cs:262)

---

### Island 82: `PlayMore35.Effects.AlwaysOn`

**Island Roots (No production callers):**
- `PlayMore35.Effects.AlwaysOn..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\IToggle.cs:41)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 83: `PlayMore35.Effects.AlwaysOn`

**Island Roots (No production callers):**
- `PlayMore35.Effects.AlwaysOn..cctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\IToggle.cs:42)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 84: `PlayMore35.Effects.LargestAmount`

**Island Roots (No production callers):**
- `PlayMore35.Effects.LargestAmount..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\LargestAmount.cs:17)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 85: `PlayMore35.Effects.LargestAmount`

**Island Roots (No production callers):**
- `PlayMore35.Effects.LargestAmount..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\LargestAmount.cs:21)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 86: `PlayMore35.Effects.LookupAmount`

**Island Roots (No production callers):**
- `PlayMore35.Effects.LookupAmount..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\LookupAmount.cs:40)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.FakeRulesCatalog.BuildMartialManeuversSubsystem` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\FakeRulesCatalog.cs:77)
- 🧪 `PlayMore35.Tests.FakeRulesCatalog.BuildPactMagicSubsystem` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\FakeRulesCatalog.cs:231)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_ContributeEffects_RegistersSlotGroupMaxLevelAndContributesSlotGroupEffects` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:550)

---

### Island 87: `PlayMore35.Effects.Modifier`

**Island Roots (No production callers):**
- `PlayMore35.Effects.Modifier..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\Modifier.cs:25)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 88: `PlayMore35.Effects.Modifier`

**Island Roots (No production callers):**
- `PlayMore35.Effects.Modifier..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\Modifier.cs:39)
- `PlayMore35.Effects.Modifier.AddAmount` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\Modifier.cs:53)
- `PlayMore35.Effects.Modifier..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\Modifier.cs:44)
- `PlayMore35.Effects.Modifier.AddAmount` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\Modifier.cs:49)

**Cascaded Methods (Downstream dead calls):**
- `PlayMore35.Effects.MultiplyAmount..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\MultiplyAmount.cs:21)
- `PlayMore35.Effects.Modifier.AddAmount` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\Modifier.cs:57)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.FakeRulesCatalog.BuildMartialManeuversSubsystem` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\FakeRulesCatalog.cs:77)
- 🧪 `PlayMore35.Tests.FakeRulesCatalog.BuildPactMagicSubsystem` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\FakeRulesCatalog.cs:231)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_ContributeEffects_RegistersSlotGroupMaxLevelAndContributesSlotGroupEffects` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:550)

---

### Island 89: `PlayMore35.Effects.ModifierTypes`

**Island Roots (No production callers):**
- `PlayMore35.Effects.ModifierTypes.IsValid` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\ModifierTypes.cs:59)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.RulesCatalogTests.Catalog_AllModifiers_UseValidModifierTypes` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\RulesCatalogTests.cs:427)

---

### Island 90: `PlayMore35.Effects.MultiplyAmount`

**Island Roots (No production callers):**
- `PlayMore35.Effects.MultiplyAmount..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\MultiplyAmount.cs:26)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 91: `PlayMore35.Effects.ParameterDefinition`

**Island Roots (No production callers):**
- `PlayMore35.Effects.ParameterDefinition..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\ParameterDefinition.cs:10)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 92: `PlayMore35.Effects.ParameterDefinition`

**Island Roots (No production callers):**
- `PlayMore35.Effects.ParameterDefinition..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\ParameterDefinition.cs:18)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 93: `PlayMore35.Effects.SmallestAmount`

**Island Roots (No production callers):**
- `PlayMore35.Effects.SmallestAmount..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\SmallestAmount.cs:17)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 94: `PlayMore35.Effects.SmallestAmount`

**Island Roots (No production callers):**
- `PlayMore35.Effects.SmallestAmount..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\SmallestAmount.cs:21)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 95: `PlayMore35.Effects.Statistic`

**Island Roots (No production callers):**
- `PlayMore35.Effects.Statistic.Maximum` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\Statistic.cs:106)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 96: `PlayMore35.Effects.Statistic`

**Island Roots (No production callers):**
- `PlayMore35.Effects.Statistic.RoundTo` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\Statistic.cs:111)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 97: `PlayMore35.Effects.Statistic`

**Island Roots (No production callers):**
- `PlayMore35.Effects.Statistic.IgnoreModifierType` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\Statistic.cs:116)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 98: `PlayMore35.Effects.Statistic`

**Island Roots (No production callers):**
- `PlayMore35.Effects.Statistic.IgnoreModiferType` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Effects\Statistic.cs:121)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 99: `PlayMore35.Global.CharacterSession`

**Island Roots (No production callers):**
- `PlayMore35.Global.CharacterSession.get_IsSnapshot` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\CharacterSession.cs:28)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 100: `PlayMore35.Global.CharacterSession`

**Island Roots (No production callers):**
- `PlayMore35.Global.CharacterSession.get_RememberedFeats` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\CharacterSession.cs:31)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 101: `PlayMore35.Global.CharacterSession`

**Island Roots (No production callers):**
- `PlayMore35.Global.CharacterSession.get_RememberedFeatChoices` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\CharacterSession.cs:32)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 102: `PlayMore35.Global.CharacterSession`

**Island Roots (No production callers):**
- `PlayMore35.Global.CharacterSession.get_LightLoad` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\CharacterSession.cs:46)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 103: `PlayMore35.Global.CharacterSession`

**Island Roots (No production callers):**
- `PlayMore35.Global.CharacterSession.get_MediumLoad` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\CharacterSession.cs:47)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 104: `PlayMore35.Global.CharacterSession`

**Island Roots (No production callers):**
- `PlayMore35.Global.CharacterSession.get_HeavyLoad` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\CharacterSession.cs:48)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 105: `PlayMore35.Global.CharacterSession`

**Island Roots (No production callers):**
- `PlayMore35.Global.CharacterSession.PlayMore35.Global.ICharacterContext.GetAllRoutines` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\CharacterSession.cs:57)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 106: `PlayMore35.Global.CharacterSession`

**Island Roots (No production callers):**
- `PlayMore35.Global.CharacterSession.PlayMore35.Global.ICharacterContext.GetVisibleRoutines` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\CharacterSession.cs:58)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 107: `PlayMore35.Global.CharacterSession`

**Island Roots (No production callers):**
- `PlayMore35.Global.CharacterSession.RemoveEquippedWeapon` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\CharacterSession.cs:415)
- `PlayMore35.Global.CharacterSession.RemoveDonnedArmor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\CharacterSession.cs:430)
- `PlayMore35.Global.CharacterSession.RemoveEquippedShield` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\CharacterSession.cs:444)

**Cascaded Methods (Downstream dead calls):**
- `PlayMore35.Global.CharacterSession.Mutate` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\CharacterSession.cs:95)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.AttackRoutineEvaluatorTests.EvaluateRoutines_SoloPrimaryWeapon_GeneratesStandardAndIterativeFullAttacks` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\AttackRoutineEvaluatorTests.cs:40)
- 🧪 `PlayMore35.Tests.AttackRoutineEvaluatorTests.EvaluateRoutines_DoubleWeapon_GeneratesTwoHandedSingleAndTwfRoutines` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\AttackRoutineEvaluatorTests.cs:106)
- 🧪 `PlayMore35.Tests.AttackRoutinePreferenceTests.AttackRoutinePreferences_CategoryPreferences_SurviveEquippedWeaponSwaps` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\AttackRoutinePreferenceTests.cs:60)
- 🧪 `PlayMore35.Tests.CharacterSessionFeatTests.BonusFeat_SwapToClassWithoutBonusFeat_RetainsInMemory_DoesNotProject_DoesNotSerialize` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\CharacterSessionFeatTests.cs:866)
- 🧪 `PlayMore35.Tests.CharacterSessionFeatTests.BonusFeat_WithFeatChoices_SwapClass_RetainsChoicesInMemory_DoesNotSerialize_RestoresOnSwapBack` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\CharacterSessionFeatTests.cs:903)
- 🧪 `PlayMore35.Tests.CharacterSessionFeatTests.BonusFeat_ActiveClearing_ClearsMemorySoItDoesNotResurrect` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\CharacterSessionFeatTests.cs:939)
- 🧪 `PlayMore35.Tests.CharacterSessionFeatTests.RacialBonusFeat_SwapRace_RetainsInMemory_DoesNotProject_RestoresOnSwapBack` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\CharacterSessionFeatTests.cs:966)
- 🧪 `PlayMore35.Tests.CharacterSessionFeatTests.MartialStudy_InBonusFeatSlot_SwapClass_DeactivatesSubsystemAndRestores` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\CharacterSessionFeatTests.cs:992)
- 🧪 `PlayMore35.Tests.CharacterSessionTests.CharacterSession_Transactions_MutationsRecomputeTotalGold` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\CharacterSessionTests.cs:146)
- 🧪 `PlayMore35.Tests.CharacterSessionTests.CharacterSession_Sessions_MutationsRecomputeTotalExperience` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\CharacterSessionTests.cs:191)
- 🧪 `PlayMore35.Tests.CharacterSessionTests.GetSessionAtLevel_Mutation_InvalidatesCachedSnapshots` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\CharacterSessionTests.cs:642)
- 🧪 `PlayMore35.Tests.CharacterSessionTests.CharacterSession_MutateWithReturn_AppliesChangesAndReturnsValueAndRecomputes` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\CharacterSessionTests.cs:658)
- 🧪 `PlayMore35.Tests.CharacterSessionTests.CharacterSession_MutateWithReturn_ThrowsArgumentNullException_WhenMutateFuncIsNull` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\CharacterSessionTests.cs:683)
- 🧪 `PlayMore35.Tests.MainFormSubsystemIntegrationTests.MainForm_SubsystemTabLifecycle_AddsWhenGained_RemovesWhenLost` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MainFormSubsystemIntegrationTests.cs:114)
- 🧪 `PlayMore35.Tests.CharacterSessionTests.EquipmentMutations_EvictCachedSnapshotsInLevelSessionCache` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\CharacterSessionTests.cs:943)
- 🧪 `PlayMore35.Tests.CharacterSessionTests.InvalidEquipmentOperations_ReturnEarlyWithoutInvalidatingCacheOrRecalculating` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\CharacterSessionTests.cs:1143)

---

### Island 108: `PlayMore35.Global.CharacterSession`

**Island Roots (No production callers):**
- `PlayMore35.Global.CharacterSession.SetFeatChoice` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\CharacterSession.cs:1014)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.CharacterSessionTests.SetFeatChoice_DirectChoice_InvalidatesCachedSnapshotsAndRecalculatesDependentStats` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\CharacterSessionTests.cs:689)

---

### Island 109: `PlayMore35.Global.CharacterSession`

**Island Roots (No production callers):**
- `PlayMore35.Global.CharacterSession.SetFeatChoice` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\CharacterSession.cs:1020)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.CharacterSessionFeatTests.ResolveFeatTargetSource_TwoOrMoreAdeptClasses_ResolvesToChosenClass` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\CharacterSessionFeatTests.cs:71)
- 🧪 `PlayMore35.Tests.CharacterSessionFeatTests.CompositeKeys_PersistedInFeatChoices_WithFullSchemaStability` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\CharacterSessionFeatTests.cs:91)
- 🧪 `PlayMore35.Tests.CharacterSessionFeatTests.ResolveClassChoice_SyntheticFeat_MultipleQualifyingClasses_ResolvesToChosenClass` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\CharacterSessionFeatTests.cs:468)
- 🧪 `PlayMore35.Tests.CharacterSessionFeatTests.ResolveClassChoice_SyntheticFeat_TemporalGating_ResolvesBasedOnAcquisitionLevel` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\CharacterSessionFeatTests.cs:493)
- 🧪 `PlayMore35.Tests.CharacterSessionTests.SetFeatChoice_CompositeKeyChoice_InvalidatesCachedSnapshotsAndRecalculatesDependentStats` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\CharacterSessionTests.cs:730)
- 🧪 `PlayMore35.Tests.CharacterSessionTests.CharacterSession_ResolveSubsystemElementChoices_ResolvesViaDirectEngines` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\CharacterSessionTests.cs:915)
- 🧪 `PlayMore35.Tests.MartialManeuverSessionIntegrationTests.FeatChoice_UpdatedOrCleared_UpdatesProjectedSlot` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverSessionIntegrationTests.cs:597)
- 🧪 `PlayMore35.Tests.MartialManeuverSessionIntegrationTests.MartialStance_FeatChoice_UpdatedOrCleared_UpdatesProjectedSlot` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverSessionIntegrationTests.cs:813)

---

### Island 110: `PlayMore35.Global.CharacterSession`

**Island Roots (No production callers):**
- `PlayMore35.Global.CharacterSession.AdvanceTimeline` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\CharacterSession.cs:1428)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.CharacterSessionTests.CharacterSession_AdvanceTimeline_ExpiresActions` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\CharacterSessionTests.cs:130)
- 🧪 `PlayMore35.Tests.ControllerShrinkageAndIntegrationTests.Subsystem_FullBindingLifecycle_WithTimelineAdvanceAndReload` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\ControllerShrinkageAndIntegrationTests.cs:58)
- 🧪 `PlayMore35.Tests.MultiSessionIsolationTests.MultiSessionIsolation_TimelineProgressionIndependent` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MultiSessionIsolationTests.cs:96)
- 🧪 `PlayMore35.Tests.PactMagicEngineTests.PactMagic_DefianceResetsOnUnbindRebindGoodPactAndTimelineExpiration` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\PactMagicEngineTests.cs:279)
- 🧪 `PlayMore35.Tests.PactMagicEngineTests.OdricJson_LoadsScheduledUnbindAction_Correctly` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\PactMagicEngineTests.cs:440)

---

### Island 111: `PlayMore35.Global.ICharacterContext`

**Island Roots (No production callers):**
- `PlayMore35.Global.ICharacterContext.GetAllRoutines` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\ICharacterContext.cs:15)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 112: `PlayMore35.Global.ICharacterContext`

**Island Roots (No production callers):**
- `PlayMore35.Global.ICharacterContext.GetVisibleRoutines` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\ICharacterContext.cs:16)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 113: `PlayMore35.Global.ICharacterSession`

**Island Roots (No production callers):**
- `PlayMore35.Global.ICharacterSession.get_RememberedFeats` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\ICharacterSession.cs:12)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 114: `PlayMore35.Global.ICharacterSession`

**Island Roots (No production callers):**
- `PlayMore35.Global.ICharacterSession.get_RememberedFeatChoices` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\ICharacterSession.cs:13)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 115: `PlayMore35.Global.ICharacterSession`

**Island Roots (No production callers):**
- `PlayMore35.Global.ICharacterSession.get_LightLoad` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\ICharacterSession.cs:23)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 116: `PlayMore35.Global.ICharacterSession`

**Island Roots (No production callers):**
- `PlayMore35.Global.ICharacterSession.get_MediumLoad` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\ICharacterSession.cs:24)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 117: `PlayMore35.Global.ICharacterSession`

**Island Roots (No production callers):**
- `PlayMore35.Global.ICharacterSession.get_HeavyLoad` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\ICharacterSession.cs:25)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 118: `PlayMore35.Global.ICharacterSession`

**Island Roots (No production callers):**
- `PlayMore35.Global.ICharacterSession.RemoveEquippedWeapon` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\ICharacterSession.cs:37)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 119: `PlayMore35.Global.ICharacterSession`

**Island Roots (No production callers):**
- `PlayMore35.Global.ICharacterSession.RemoveDonnedArmor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\ICharacterSession.cs:38)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 120: `PlayMore35.Global.ICharacterSession`

**Island Roots (No production callers):**
- `PlayMore35.Global.ICharacterSession.RemoveEquippedShield` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\ICharacterSession.cs:39)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 121: `PlayMore35.Global.ICharacterSession`

**Island Roots (No production callers):**
- `PlayMore35.Global.ICharacterSession.RemoveItem` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\ICharacterSession.cs:52)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 122: `PlayMore35.Global.ICharacterSession`

**Island Roots (No production callers):**
- `PlayMore35.Global.ICharacterSession.GetAllRoutines` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\ICharacterSession.cs:67)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 123: `PlayMore35.Global.ICharacterSession`

**Island Roots (No production callers):**
- `PlayMore35.Global.ICharacterSession.AdvanceTimeline` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\ICharacterSession.cs:71)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 124: `PlayMore35.Global.ICharacterSession`

**Island Roots (No production callers):**
- `PlayMore35.Global.ICharacterSession.GetSizeCategory` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\ICharacterSession.cs:77)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 125: `PlayMore35.Global.ICharacterSession`

**Island Roots (No production callers):**
- `PlayMore35.Global.ICharacterSession.ResolveFeatChoice` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\ICharacterSession.cs:88)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 126: `PlayMore35.Global.ICharacterSession`

**Island Roots (No production callers):**
- `PlayMore35.Global.ICharacterSession.ResolveClassChoice` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\ICharacterSession.cs:89)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 127: `PlayMore35.Global.ICharacterSession`

**Island Roots (No production callers):**
- `PlayMore35.Global.ICharacterSession.IsClassEligible` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\ICharacterSession.cs:90)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 128: `PlayMore35.Global.ICharacterSession`

**Island Roots (No production callers):**
- `PlayMore35.Global.ICharacterSession.SetFeatChoice` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\ICharacterSession.cs:91)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 129: `PlayMore35.Global.ICharacterSession`

**Island Roots (No production callers):**
- `PlayMore35.Global.ICharacterSession.SetFeatChoice` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\ICharacterSession.cs:92)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 130: `PlayMore35.Global.ICharacterSession`

**Island Roots (No production callers):**
- `PlayMore35.Global.ICharacterSession.GetSessionAtLevel` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\ICharacterSession.cs:96)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 131: `PlayMore35.JObjectExtensions`

**Island Roots (No production callers):**
- `PlayMore35.JObjectExtensions.AddList` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\IJsonable.cs:11)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 132: `PlayMore35.JObjectExtensions`

**Island Roots (No production callers):**
- `PlayMore35.JObjectExtensions.AddList` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\IJsonable.cs:17)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 133: `PlayMore35.JObjectExtensions`

**Island Roots (No production callers):**
- `PlayMore35.JObjectExtensions.AddList` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\IJsonable.cs:23)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 134: `PlayMore35.JObjectExtensions`

**Island Roots (No production callers):**
- `PlayMore35.JObjectExtensions.ReadJson` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\IJsonable.cs:29)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 135: `PlayMore35.JObjectExtensions`

**Island Roots (No production callers):**
- `PlayMore35.JObjectExtensions.ReadJson` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\IJsonable.cs:35)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 136: `PlayMore35.Global.ProgressionChoiceContext`

**Island Roots (No production callers):**
- `PlayMore35.Global.ProgressionChoiceContext..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\ProgressionChoiceContext.cs:21)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 137: `PlayMore35.Global.SheetAction`

**Island Roots (No production callers):**
- `PlayMore35.Global.SheetAction.ClearFlag` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\SheetAction.cs:135)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 138: `PlayMore35.Global.Timeline`

**Island Roots (No production callers):**
- `PlayMore35.Global.Timeline.Unschedule` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\Timeline.cs:287)

**Cascaded Methods (Downstream dead calls):**
- `PlayMore35.Global.SheetAction.Matches` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\SheetAction.cs:235)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.BoostDurationSheetAction_SerializesAndRoundTripsViaFromJson` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:935)
- 🧪 `PlayMore35.Tests.SheetActionTests.DeclarativeSubsystemAction_SerializesAndPerforms_DirectlyOnSubsystemEngine` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SheetActionTests.cs:91)
- 🧪 `PlayMore35.Tests.SheetActionTests.DeclarativeSubsystemAction_MatchesAndDefaultName_BehavesCorrectly` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SheetActionTests.cs:126)
- 🧪 `PlayMore35.Tests.SheetActionTests.SheetAction_Name_RoundTripsThroughJsonAndMatches` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SheetActionTests.cs:210)
- 🧪 `PlayMore35.Tests.SheetActionTests.Effect_ParameterAndAssociatedActions_RoundTripThroughJsonAndApplyChoice` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SheetActionTests.cs:232)
- 🧪 `PlayMore35.Tests.SyntheticSubsystemTests.DeepenedSheetAction_JsonRoundTrip_SerializesAndDeserializesDeclaratively` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SyntheticSubsystemTests.cs:380)

---

### Island 139: `PlayMore35.Global.Timeline`

**Island Roots (No production callers):**
- `PlayMore35.Global.Timeline.Advance` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\Timeline.cs:75)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.CombatTransitionTests.StartCombat_SwitchesCombatMode_AndInitializesEncounterTiming` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\CombatTransitionTests.cs:22)
- 🧪 `PlayMore35.Tests.SheetActionTests.SheetAction_WithDuration_SchedulesReversionOnTimeline` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SheetActionTests.cs:151)
- 🧪 `PlayMore35.Tests.SheetActionTests.SheetAction_ChainedAction_FiresUponExpiration` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SheetActionTests.cs:180)
- 🧪 `PlayMore35.Tests.TimelineTests.Advance_IncrementsCurrentRound_AndProjectsCalendarDate` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\TimelineTests.cs:16)
- 🧪 `PlayMore35.Tests.TimelineTests.Advance_ZeroesPeriodicParameters_AcrossCadenceThresholds` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\TimelineTests.cs:70)

---

### Island 140: `PlayMore35.Global.Timeline`

**Island Roots (No production callers):**
- `PlayMore35.Global.Timeline.InCombat` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\Timeline.cs:254)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 141: `PlayMore35.Global.Timeline`

**Island Roots (No production callers):**
- `PlayMore35.Global.Timeline.Schedule` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\Timeline.cs:279)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 142: `PlayMore35.Global.TimelineAdvanceResult`

**Island Roots (No production callers):**
- `PlayMore35.Global.TimelineAdvanceResult.get_RoundsAdvanced` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\TimelineAdvanceResult.cs:12)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 143: `PlayMore35.Global.TimelineAdvanceResult`

**Island Roots (No production callers):**
- `PlayMore35.Global.TimelineAdvanceResult.get_PreviousTime` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\TimelineAdvanceResult.cs:13)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 144: `PlayMore35.Global.TimelineAdvanceResult`

**Island Roots (No production callers):**
- `PlayMore35.Global.TimelineAdvanceResult.get_CurrentTime` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\TimelineAdvanceResult.cs:14)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 145: `PlayMore35.Global.TimelineAdvanceResult`

**Island Roots (No production callers):**
- `PlayMore35.Global.TimelineAdvanceResult.set_ExpiredActions` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\TimelineAdvanceResult.cs:16)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 146: `PlayMore35.Global.TimelineAdvanceResult`

**Island Roots (No production callers):**
- `PlayMore35.Global.TimelineAdvanceResult.get_HasStateChanged` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\TimelineAdvanceResult.cs:18)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 147: `PlayMore35.Global.ToggleView`

**Island Roots (No production callers):**
- `PlayMore35.Global.ToggleView..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Global\ToggleView.cs:22)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.ViewProjectionTests.StatView_ToggleView_ParamView_PropertiesAreReadonlyAndPopulated` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\ViewProjectionTests.cs:24)

---

### Island 148: `System.Diagnostics.CodeAnalysis.MaybeNullWhenAttribute`

**Island Roots (No production callers):**
- `System.Diagnostics.CodeAnalysis.MaybeNullWhenAttribute..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Properties\NullableAttributes.cs:36)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 149: `System.Diagnostics.CodeAnalysis.MaybeNullWhenAttribute`

**Island Roots (No production callers):**
- `System.Diagnostics.CodeAnalysis.MaybeNullWhenAttribute.get_ReturnValue` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Properties\NullableAttributes.cs:37)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 150: `System.Diagnostics.CodeAnalysis.NotNullWhenAttribute`

**Island Roots (No production callers):**
- `System.Diagnostics.CodeAnalysis.NotNullWhenAttribute.get_ReturnValue` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Properties\NullableAttributes.cs:47)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 151: `System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute`

**Island Roots (No production callers):**
- `System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Properties\NullableAttributes.cs:56)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 152: `System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute`

**Island Roots (No production callers):**
- `System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute.get_ParameterName` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Properties\NullableAttributes.cs:57)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 153: `System.Diagnostics.CodeAnalysis.DoesNotReturnIfAttribute`

**Island Roots (No production callers):**
- `System.Diagnostics.CodeAnalysis.DoesNotReturnIfAttribute..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Properties\NullableAttributes.cs:72)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 154: `System.Diagnostics.CodeAnalysis.DoesNotReturnIfAttribute`

**Island Roots (No production callers):**
- `System.Diagnostics.CodeAnalysis.DoesNotReturnIfAttribute.get_ParameterValue` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Properties\NullableAttributes.cs:73)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 155: `System.Diagnostics.CodeAnalysis.MemberNotNullAttribute`

**Island Roots (No production callers):**
- `System.Diagnostics.CodeAnalysis.MemberNotNullAttribute..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Properties\NullableAttributes.cs:82)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 156: `System.Diagnostics.CodeAnalysis.MemberNotNullAttribute`

**Island Roots (No production callers):**
- `System.Diagnostics.CodeAnalysis.MemberNotNullAttribute..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Properties\NullableAttributes.cs:83)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 157: `System.Diagnostics.CodeAnalysis.MemberNotNullAttribute`

**Island Roots (No production callers):**
- `System.Diagnostics.CodeAnalysis.MemberNotNullAttribute.get_Members` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Properties\NullableAttributes.cs:84)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 158: `System.Diagnostics.CodeAnalysis.MemberNotNullWhenAttribute`

**Island Roots (No production callers):**
- `System.Diagnostics.CodeAnalysis.MemberNotNullWhenAttribute..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Properties\NullableAttributes.cs:93)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 159: `System.Diagnostics.CodeAnalysis.MemberNotNullWhenAttribute`

**Island Roots (No production callers):**
- `System.Diagnostics.CodeAnalysis.MemberNotNullWhenAttribute..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Properties\NullableAttributes.cs:99)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 160: `System.Diagnostics.CodeAnalysis.MemberNotNullWhenAttribute`

**Island Roots (No production callers):**
- `System.Diagnostics.CodeAnalysis.MemberNotNullWhenAttribute.get_ReturnValue` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Properties\NullableAttributes.cs:105)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 161: `System.Diagnostics.CodeAnalysis.MemberNotNullWhenAttribute`

**Island Roots (No production callers):**
- `System.Diagnostics.CodeAnalysis.MemberNotNullWhenAttribute.get_Members` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Properties\NullableAttributes.cs:106)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 162: `PlayMore35.Properties.Resources`

**Island Roots (No production callers):**
- `PlayMore35.Properties.Resources..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Properties\Resources.Designer.cs:32)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 163: `PlayMore35.Properties.Resources`

**Island Roots (No production callers):**
- `PlayMore35.Properties.Resources.get_Culture` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Properties\Resources.Designer.cs:55)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 164: `PlayMore35.Properties.Resources`

**Island Roots (No production callers):**
- `PlayMore35.Properties.Resources.set_Culture` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Properties\Resources.Designer.cs:58)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 165: `PlayMore35.Properties.Settings`

**Island Roots (No production callers):**
- `PlayMore35.Properties.Settings.get_Default` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Properties\Settings.Designer.cs:21)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 166: `PlayMore35.Rules.GameRulesCatalog`

**Island Roots (No production callers):**
- `PlayMore35.Rules.GameRulesCatalog..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Rules\GameRulesCatalog.cs:78)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 167: `PlayMore35.Rules.GameRulesCatalog`

**Island Roots (No production callers):**
- `PlayMore35.Rules.GameRulesCatalog.GetArmor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Rules\GameRulesCatalog.cs:282)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.RulesCatalogTests.GameRulesCatalog_LoadsCoreDefinitionsWithoutCharacter` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\RulesCatalogTests.cs:20)
- 🧪 `PlayMore35.Tests.RulesCatalogTests.IRulesCatalog_Lookups_ThrowKeyNotFoundException_WhenMissing` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\RulesCatalogTests.cs:594)

---

### Island 168: `PlayMore35.Rules.GameRulesCatalog`

**Island Roots (No production callers):**
- `PlayMore35.Rules.GameRulesCatalog.GetShield` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Rules\GameRulesCatalog.cs:292)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.RulesCatalogTests.GameRulesCatalog_LoadsCoreDefinitionsWithoutCharacter` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\RulesCatalogTests.cs:20)
- 🧪 `PlayMore35.Tests.RulesCatalogTests.IRulesCatalog_Lookups_ThrowKeyNotFoundException_WhenMissing` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\RulesCatalogTests.cs:594)

---

### Island 169: `PlayMore35.Rules.GameRulesCatalog`

**Island Roots (No production callers):**
- `PlayMore35.Rules.GameRulesCatalog.GetWeaponProperty` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Rules\GameRulesCatalog.cs:302)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.RulesCatalogTests.IRulesCatalog_Lookups_ThrowKeyNotFoundException_WhenMissing` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\RulesCatalogTests.cs:594)

---

### Island 170: `PlayMore35.Rules.GameRulesCatalog`

**Island Roots (No production callers):**
- `PlayMore35.Rules.GameRulesCatalog.GetArmorProperty` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Rules\GameRulesCatalog.cs:312)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.RulesCatalogTests.IRulesCatalog_Lookups_ThrowKeyNotFoundException_WhenMissing` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\RulesCatalogTests.cs:594)

---

### Island 171: `PlayMore35.Rules.GameRulesCatalog`

**Island Roots (No production callers):**
- `PlayMore35.Rules.GameRulesCatalog.GetSubsystem` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Rules\GameRulesCatalog.cs:348)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.CoreDecouplingTests.SubsystemAction_UnbindVestige_DispatchesThroughSubsystemEngine` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\CoreDecouplingTests.cs:89)
- 🧪 `PlayMore35.Tests.MartialAdeptProgressionTests.GameRulesCatalog_LoadsManeuversCatalogCleanly` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialAdeptProgressionTests.cs:365)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.ManeuversSlotGroup_DefinesFourCanonicalStates` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:56)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.SubsystemEngine_FullCatalogStanceActivation_ModifiesStatsAcrossClasses` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:950)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.SubsystemEngine_FullCatalogRecovery_OperatesAcrossCrusaderSwordsageAndWarblade` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:1016)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.ManeuversCatalog_ParsesTypeTaxonomyAndSlotGroupFiltersCorrectly` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:1090)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.SubsystemElement_MatchesFilter_GenericTaxonomyResolution` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:1140)
- 🧪 `PlayMore35.Tests.PactMagicEngineTests.PactMagic_SubsystemDefinition_DeclaresInfluenceEffectTemplate_AndElementsDeclareOnlyText` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\PactMagicEngineTests.cs:44)
- 🧪 `PlayMore35.Tests.PactMagicEngineTests.PactMagic_SlotGroup_ContainsExpectedStates` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\PactMagicEngineTests.cs:73)
- 🧪 `PlayMore35.Tests.PactMagicEngineTests.SubsystemElement_ImplementsIPropertyProvider_AndExposesGenericProperties` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\PactMagicEngineTests.cs:504)
- 🧪 `PlayMore35.Tests.PactMagicEngineTests.SubsystemDefinition_RequiredElementProperties_EnforcesPresenceAndRoundTrips` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\PactMagicEngineTests.cs:606)
- 🧪 `PlayMore35.Tests.PactMagicEngineTests.SubsystemDefinition_ElementStatistics_EnforcesPresenceAndRoundTrips` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\PactMagicEngineTests.cs:651)
- 🧪 `PlayMore35.Tests.PactMagicEngineTests.SubsystemDefinition_LevelStatistic_EnforcesPresenceAndRoundTrips` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\PactMagicEngineTests.cs:749)
- 🧪 `PlayMore35.Tests.PactMagicUITests.VestigeCardControl_CardHeightAndAbilities_ShowsAllAbilitiesWithoutMoreEllipsis` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\PactMagicUITests.cs:302)
- 🧪 `PlayMore35.Tests.PactMagicUITests.VestigeCardControl_PactState_DisplaysPactStateAndUnbindWhenBound` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\PactMagicUITests.cs:323)
- 🧪 `PlayMore35.Tests.PactMagicUITests.VestigeCardControl_CardStatusVisuals_UnmetLevelGate_DisplaysExceedsDarkGoldenrodAndTooltip` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\PactMagicUITests.cs:344)
- 🧪 `PlayMore35.Tests.PactMagicUITests.VestigeCardControl_CardStatusVisuals_UnmetOtherCategory_DisplaysIneligibleDarkGoldenrodAndTooltip` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\PactMagicUITests.cs:363)
- 🧪 `PlayMore35.Tests.PactMagicUITests.VestigeCardControl_CardStatusVisuals_ItemDependent_DisplaysItemDepDodgerBlueAndTooltip` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\PactMagicUITests.cs:382)
- 🧪 `PlayMore35.Tests.PactMagicUITests.VestigeCardControl_CardStatusVisuals_NoWarnings_DisplaysAvailableAndGreen` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\PactMagicUITests.cs:401)
- 🧪 `PlayMore35.Tests.PactMagicUITests.VestigeCardControl_IsInvalid_TrueOnlyWhenUnmetWarningsPresent` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\PactMagicUITests.cs:418)
- 🧪 `PlayMore35.Tests.RulesCatalogTests.Catalog_TextEffectCategories_ExistInConstants` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\RulesCatalogTests.cs:164)
- 🧪 `PlayMore35.Tests.RulesCatalogTests.VestigesCatalog_InfluenceCategories_MatchSection0Requirements` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\RulesCatalogTests.cs:242)
- 🧪 `PlayMore35.Tests.RulesCatalogTests.GameRulesCatalog_LoadsVestiges_FromResources` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\RulesCatalogTests.cs:351)
- 🧪 `PlayMore35.Tests.RulesCatalogTests.MartialManeuverEngine_ResolvesCatalogFromContext_OnInit` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\RulesCatalogTests.cs:375)
- 🧪 `PlayMore35.Tests.RulesCatalogTests.PactMagicEngine_ResolvesCatalogFromContext_OnInit` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\RulesCatalogTests.cs:395)
- 🧪 `PlayMore35.Tests.RulesCatalogTests.MartialDiscipline_CatalogAndProperties_CorrectlyConfigured` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\RulesCatalogTests.cs:464)
- 🧪 `PlayMore35.Tests.RulesCatalogTests.Maneuver_ImplementsIPropertyProvider_WithDisciplineObject` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\RulesCatalogTests.cs:487)
- 🧪 `PlayMore35.Tests.RulesCatalogTests.ResolveTemplate_DotSeparatedPath_TraversesPropertyProviders` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\RulesCatalogTests.cs:517)
- 🧪 `PlayMore35.Tests.RulesCatalogTests.ResolveTemplate_AllNineDisciplines_ResolveCorrectKeySkills` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\RulesCatalogTests.cs:532)
- 🧪 `PlayMore35.Tests.RulesCatalogTests.ResolveTemplate_PrefixedChoiceKey_ResolvesCorrectly` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\RulesCatalogTests.cs:557)
- 🧪 `PlayMore35.Tests.RulesCatalogTests.ResolveTemplate_UnsetOrMissingKey_SafelyReturnsEmptyString` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\RulesCatalogTests.cs:569)
- 🧪 `PlayMore35.Tests.RulesCatalogTests.IRulesCatalog_Lookups_ThrowKeyNotFoundException_WhenMissing` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\RulesCatalogTests.cs:594)
- 🧪 `PlayMore35.Tests.SheetActionTests.DeclarativeSubsystemAction_SerializesAndPerforms_DirectlyOnSubsystemEngine` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SheetActionTests.cs:91)
- 🧪 `PlayMore35.Tests.SheetActionTests.SheetAction_Perform_DispatchesViaGetSubsystem` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SheetActionTests.cs:315)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.GameRulesCatalog_CanLoadFullManeuverCatalog_AndResolveAllNineDisciplines` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:437)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_GrantSlot_And_PruneStaleGrantedSlots_OperateDeterministically` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:879)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_ValidateChoice_WorksPurelyThroughDeclarativeActiveSlotState` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:1030)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.ManeuversResource_DeclaresPrerequisiteProperty` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:1149)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_ValidateChoice_EmitsCapacityWarning_WhenSlotGroupCapacityReached` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:1476)

---

### Island 172: `PlayMore35.Rules.IRulesCatalog`

**Island Roots (No production callers):**
- `PlayMore35.Rules.IRulesCatalog.get_ExternalEffects` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Rules\IRulesCatalog.cs:20)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 173: `PlayMore35.Rules.IRulesCatalog`

**Island Roots (No production callers):**
- `PlayMore35.Rules.IRulesCatalog.GetArmor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Rules\IRulesCatalog.cs:27)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.ViewProjectionTests.Equipped_ContributeEffects_PushesItemEffectsAndFlagsRespectingDonnedArmorOverride` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\ViewProjectionTests.cs:262)

---

### Island 174: `PlayMore35.Rules.IRulesCatalog`

**Island Roots (No production callers):**
- `PlayMore35.Rules.IRulesCatalog.GetShield` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Rules\IRulesCatalog.cs:28)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 175: `PlayMore35.Rules.IRulesCatalog`

**Island Roots (No production callers):**
- `PlayMore35.Rules.IRulesCatalog.GetWeaponProperty` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Rules\IRulesCatalog.cs:29)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 176: `PlayMore35.Rules.IRulesCatalog`

**Island Roots (No production callers):**
- `PlayMore35.Rules.IRulesCatalog.GetArmorProperty` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Rules\IRulesCatalog.cs:30)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 177: `PlayMore35.Rules.IRulesCatalog`

**Island Roots (No production callers):**
- `PlayMore35.Rules.IRulesCatalog.GetSubsystem` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Rules\IRulesCatalog.cs:33)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.FakeRulesCatalog.BuildMartialManeuversSubsystem` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\FakeRulesCatalog.cs:77)
- 🧪 `PlayMore35.Tests.LevelDetailsDialogTests.MartialStudy_ExceedsMaxManeuverLevel_DisplaysAdvisoryWarningBadge` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\LevelDetailsDialogTests.cs:266)
- 🧪 `PlayMore35.Tests.MartialAdeptProgressionTests.Warblade_SingleClassProgression_EvaluatesStatisticsCorrectly` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialAdeptProgressionTests.cs:24)
- 🧪 `PlayMore35.Tests.MartialAdeptProgressionTests.Crusader_SingleClassProgression_EvaluatesStatisticsCorrectly` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialAdeptProgressionTests.cs:64)
- 🧪 `PlayMore35.Tests.MartialAdeptProgressionTests.Swordsage_SingleClassProgression_EvaluatesStatisticsCorrectly` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialAdeptProgressionTests.cs:105)
- 🧪 `PlayMore35.Tests.MartialAdeptProgressionTests.ManeuversCatalog_LoadsAllDisciplinesAndAllManeuverTypes` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialAdeptProgressionTests.cs:283)
- 🧪 `PlayMore35.Tests.MartialAdeptProgressionTests.ManeuversCatalog_AllEntriesDeserializeValidly_AndMatchSchema` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialAdeptProgressionTests.cs:315)

---

### Island 178: `PlayMore35.Rules.IRulesCatalog`

**Island Roots (No production callers):**
- `PlayMore35.Rules.IRulesCatalog.TryGetRace` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Rules\IRulesCatalog.cs:35)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.FakeRulesCatalog.TryGetRace` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\FakeRulesCatalog.cs:299)

---

### Island 179: `PlayMore35.Rules.IRulesCatalog`

**Island Roots (No production callers):**
- `PlayMore35.Rules.IRulesCatalog.TryGetExternalEffect` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Rules\IRulesCatalog.cs:44)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.FakeRulesCatalog.TryGetExternalEffect` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\FakeRulesCatalog.cs:478)

---

### Island 180: `PlayMore35.Rules.IRulesCatalog`

**Island Roots (No production callers):**
- `PlayMore35.Rules.IRulesCatalog.get_CoreFeatSlots` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Rules\IRulesCatalog.cs:53)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 181: `PlayMore35.Rules.IRulesCatalog`

**Island Roots (No production callers):**
- `PlayMore35.Rules.IRulesCatalog.get_CoreStats` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Rules\IRulesCatalog.cs:54)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 182: `PlayMore35.Rules.IRulesCatalog`

**Island Roots (No production callers):**
- `PlayMore35.Rules.IRulesCatalog.get_CoreEffects` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Rules\IRulesCatalog.cs:55)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 183: `PlayMore35.Rules.IRulesCatalog`

**Island Roots (No production callers):**
- `PlayMore35.Rules.IRulesCatalog.get_CoreFlags` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Rules\IRulesCatalog.cs:56)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 184: `PlayMore35.Rules.IRulesCatalog`

**Island Roots (No production callers):**
- `PlayMore35.Rules.IRulesCatalog.get_CoreParameters` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Rules\IRulesCatalog.cs:57)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 185: `PlayMore35.Subsystems.SubsystemStep`

**Island Roots (No production callers):**
- `PlayMore35.Subsystems.SubsystemStep..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Subsystems\SubsystemDefinition.cs:24)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.FakeRulesCatalog.BuildMartialManeuversSubsystem` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\FakeRulesCatalog.cs:77)
- 🧪 `PlayMore35.Tests.FakeRulesCatalog.BuildPactMagicSubsystem` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\FakeRulesCatalog.cs:231)
- 🧪 `PlayMore35.Tests.FakeRulesCatalog.CreateSyntheticAlchemySubsystem` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\FakeRulesCatalog.cs:522)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.ExpendVerb_SchedulesSheetActionForBoostAndCounter_AndExpiresOnTimelineAdvance` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:851)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_TransitionSlot_ActiveStateClamping_UsesStateCapacities` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:1399)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_TransitionSlot_DynamicStateClamping_CustomNonActiveState` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:1561)

---

### Island 186: `PlayMore35.Subsystems.MaxLevelDefinition`

**Island Roots (No production callers):**
- `PlayMore35.Subsystems.MaxLevelDefinition..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Subsystems\SubsystemDefinition.cs:67)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 187: `PlayMore35.Subsystems.MaxLevelDefinition`

**Island Roots (No production callers):**
- `PlayMore35.Subsystems.MaxLevelDefinition..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Subsystems\SubsystemDefinition.cs:69)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.FakeRulesCatalog.BuildMartialManeuversSubsystem` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\FakeRulesCatalog.cs:77)
- 🧪 `PlayMore35.Tests.FakeRulesCatalog.BuildPactMagicSubsystem` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\FakeRulesCatalog.cs:231)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_ContributeEffects_RegistersSlotGroupMaxLevelAndContributesSlotGroupEffects` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:550)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_IsSlotLevelValid_GatesDynamicallyBySlotGroupMaxLevel` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:579)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_ValidateChoice_EmitsAdvisoryWarning_WhenElementExceedsMaxLevel` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:630)

---

### Island 188: `PlayMore35.Subsystems.LevelGatingDefinition`

**Island Roots (No production callers):**
- `PlayMore35.Subsystems.LevelGatingDefinition..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Subsystems\SubsystemDefinition.cs:92)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 189: `PlayMore35.Subsystems.LevelGatingDefinition`

**Island Roots (No production callers):**
- `PlayMore35.Subsystems.LevelGatingDefinition..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Subsystems\SubsystemDefinition.cs:94)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.FakeRulesCatalog.BuildMartialManeuversSubsystem` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\FakeRulesCatalog.cs:77)
- 🧪 `PlayMore35.Tests.FakeRulesCatalog.BuildPactMagicSubsystem` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\FakeRulesCatalog.cs:231)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_IsSlotLevelValid_GatesDynamicallyBySlotGroupMaxLevel` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:579)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_ValidateChoice_EmitsAdvisoryWarning_WhenElementExceedsMaxLevel` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:630)

---

### Island 190: `PlayMore35.Subsystems.StateCapacityDefinition`

**Island Roots (No production callers):**
- `PlayMore35.Subsystems.StateCapacityDefinition..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Subsystems\SubsystemDefinition.cs:118)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.FakeRulesCatalog.BuildMartialManeuversSubsystem` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\FakeRulesCatalog.cs:77)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.StateCapacityDefinition_ExplicitConstructorAndJson_RoundTrips` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:1307)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SlotGroupDefinition_StateCapacities_JsonRoundTrip` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:1339)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_Validate_GenericStateCapacities_EmitsDynamicCapacityWarning` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:1365)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_TransitionSlot_ActiveStateClamping_UsesStateCapacities` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:1399)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_Validate_EmitsCharacterCapacityWarning_WhenDistinctSourcesNotOne` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:1527)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_TransitionSlot_DynamicStateClamping_CustomNonActiveState` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:1561)

---

### Island 191: `PlayMore35.Subsystems.SlotGroupDefinition`

**Island Roots (No production callers):**
- `PlayMore35.Subsystems.SlotGroupDefinition..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Subsystems\SubsystemDefinition.cs:162)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.FakeRulesCatalog.BuildMartialManeuversSubsystem` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\FakeRulesCatalog.cs:77)
- 🧪 `PlayMore35.Tests.FakeRulesCatalog.BuildPactMagicSubsystem` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\FakeRulesCatalog.cs:231)
- 🧪 `PlayMore35.Tests.FakeRulesCatalog.CreateSyntheticAlchemySubsystem` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\FakeRulesCatalog.cs:522)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_ContributeEffects_RegistersSlotGroupMaxLevelAndContributesSlotGroupEffects` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:550)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_IsSlotLevelValid_GatesDynamicallyBySlotGroupMaxLevel` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:579)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_ValidateChoice_EmitsAdvisoryWarning_WhenElementExceedsMaxLevel` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:630)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_CountSamePropertyKnown_PartitionsByConfiguredProperty` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:1158)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_ValidateChoice_DynamicPrerequisitePropertyAndWarningFormatting` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:1203)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_ValidateChoice_UnpartitionedPrerequisites_WhenPrerequisitePropertyNull` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:1242)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_Validate_EvaluatesDynamicPrerequisites` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:1279)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SlotGroupDefinition_StateCapacities_JsonRoundTrip` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:1339)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_Validate_GenericStateCapacities_EmitsDynamicCapacityWarning` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:1365)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_TransitionSlot_ActiveStateClamping_UsesStateCapacities` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:1399)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_ValidateChoice_CapacityWarning_FormatsWithoutSource_WhenSourceNullOrEmpty` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:1502)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_Validate_EmitsCharacterCapacityWarning_WhenDistinctSourcesNotOne` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:1527)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_TransitionSlot_DynamicStateClamping_CustomNonActiveState` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:1561)
- 🧪 `PlayMore35.Tests.UniversalSubsystemPersistenceTests.CharacterSession_InactiveCatalogEngine_DoesNotPopulateEmptySubsystemInCharacter` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\UniversalSubsystemPersistenceTests.cs:319)

---

### Island 192: `PlayMore35.Subsystems.PoolDefinition`

**Island Roots (No production callers):**
- `PlayMore35.Subsystems.PoolDefinition..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Subsystems\SubsystemDefinition.cs:231)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.FakeRulesCatalog.CreateSyntheticAlchemySubsystem` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\FakeRulesCatalog.cs:522)
- 🧪 `PlayMore35.Tests.UniversalSubsystemPersistenceTests.CharacterSession_InactiveCatalogEngine_DoesNotPopulateEmptySubsystemInCharacter` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\UniversalSubsystemPersistenceTests.cs:319)

---

### Island 193: `PlayMore35.Subsystems.EffectProjectionRule`

**Island Roots (No production callers):**
- `PlayMore35.Subsystems.EffectProjectionRule..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Subsystems\SubsystemDefinition.cs:257)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 194: `PlayMore35.Subsystems.EffectProjectionRule`

**Island Roots (No production callers):**
- `PlayMore35.Subsystems.EffectProjectionRule..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Subsystems\SubsystemDefinition.cs:259)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.FakeRulesCatalog.BuildMartialManeuversSubsystem` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\FakeRulesCatalog.cs:77)
- 🧪 `PlayMore35.Tests.FakeRulesCatalog.BuildPactMagicSubsystem` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\FakeRulesCatalog.cs:231)
- 🧪 `PlayMore35.Tests.FakeRulesCatalog.CreateSyntheticAlchemySubsystem` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\FakeRulesCatalog.cs:522)

---

### Island 195: `PlayMore35.Subsystems.TaxonomyEntry`

**Island Roots (No production callers):**
- `PlayMore35.Subsystems.TaxonomyEntry.GetProperty` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Subsystems\SubsystemDefinition.cs:295)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.ManeuversCatalog_ParsesTypeTaxonomyAndSlotGroupFiltersCorrectly` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:1090)
- 🧪 `PlayMore35.Tests.RulesCatalogTests.MartialDiscipline_CatalogAndProperties_CorrectlyConfigured` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\RulesCatalogTests.cs:464)
- 🧪 `PlayMore35.Tests.RulesCatalogTests.Maneuver_ImplementsIPropertyProvider_WithDisciplineObject` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\RulesCatalogTests.cs:487)

---

### Island 196: `PlayMore35.Subsystems.TaxonomyEntry`

**Island Roots (No production callers):**
- `PlayMore35.Subsystems.TaxonomyEntry.SetProperty` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Subsystems\SubsystemDefinition.cs:311)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.FakeRulesCatalog.BuildMartialManeuversSubsystem` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\FakeRulesCatalog.cs:77)

---

### Island 197: `PlayMore35.Subsystems.TaxonomyEntry`

**Island Roots (No production callers):**
- `PlayMore35.Subsystems.TaxonomyEntry..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Subsystems\SubsystemDefinition.cs:316)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.FakeRulesCatalog.BuildMartialManeuversSubsystem` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\FakeRulesCatalog.cs:77)

---

### Island 198: `PlayMore35.Subsystems.SubsystemDefinition`

**Island Roots (No production callers):**
- `PlayMore35.Subsystems.SubsystemDefinition..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Subsystems\SubsystemDefinition.cs:371)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.CharacterContextTests.FakeCharacterContext_GetSubsystem_ResolvesFromSubsystemsMap_CaseInsensitively` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\CharacterContextTests.cs:139)
- 🧪 `PlayMore35.Tests.FakeRulesCatalog.BuildMartialManeuversSubsystem` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\FakeRulesCatalog.cs:77)
- 🧪 `PlayMore35.Tests.FakeRulesCatalog.BuildPactMagicSubsystem` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\FakeRulesCatalog.cs:231)
- 🧪 `PlayMore35.Tests.FakeRulesCatalog.CreateSyntheticAlchemySubsystem` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\FakeRulesCatalog.cs:522)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.CreateTestEngine` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:16)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_Constructor_ThrowsArgumentNullException_OnNullDefinitionOrContext` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:24)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_ContributeEffects_RegistersSlotGroupMaxLevelAndContributesSlotGroupEffects` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:550)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_IsSlotLevelValid_GatesDynamicallyBySlotGroupMaxLevel` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:579)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_ValidateChoice_EmitsAdvisoryWarning_WhenElementExceedsMaxLevel` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:630)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_ValidateChoice_EmitsInvalidWarning_WhenElementDoesNotMatchFilter` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:682)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemDefinition_PrerequisiteProperty_SerializationRoundTrip` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:1119)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_CountSamePropertyKnown_PartitionsByConfiguredProperty` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:1158)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_ValidateChoice_DynamicPrerequisitePropertyAndWarningFormatting` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:1203)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_ValidateChoice_UnpartitionedPrerequisites_WhenPrerequisitePropertyNull` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:1242)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_Validate_EvaluatesDynamicPrerequisites` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:1279)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_Validate_GenericStateCapacities_EmitsDynamicCapacityWarning` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:1365)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_TransitionSlot_ActiveStateClamping_UsesStateCapacities` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:1399)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_ValidateChoice_CapacityWarning_FormatsWithoutSource_WhenSourceNullOrEmpty` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:1502)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_Validate_EmitsCharacterCapacityWarning_WhenDistinctSourcesNotOne` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:1527)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_TransitionSlot_DynamicStateClamping_CustomNonActiveState` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:1561)
- 🧪 `PlayMore35.Tests.UniversalSubsystemPersistenceTests.CharacterSession_InactiveCatalogEngine_DoesNotPopulateEmptySubsystemInCharacter` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\UniversalSubsystemPersistenceTests.cs:319)

---

### Island 199: `PlayMore35.Subsystems.SubsystemElement`

**Island Roots (No production callers):**
- `PlayMore35.Subsystems.SubsystemElement.GetStatistic` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Subsystems\SubsystemElement.cs:127)

**Cascaded Methods (Downstream dead calls):**
- `PlayMore35.Subsystems.SubsystemElement.GetStatistic` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Subsystems\SubsystemElement.cs:113)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.PactMagicEngineTests.PactMagic_ElementStatistics_ProjectedIntoEffectPool_AndModifiableByTagOrExactName` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\PactMagicEngineTests.cs:702)
- 🧪 `PlayMore35.Tests.PactMagicEngineTests.PactMagic_LevelStatistic_ProjectedIntoEffectPool_AndModifiable` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\PactMagicEngineTests.cs:817)

---

### Island 200: `PlayMore35.Subsystems.SubsystemElement`

**Island Roots (No production callers):**
- `PlayMore35.Subsystems.SubsystemElement.GetStatistic` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Subsystems\SubsystemElement.cs:130)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.PactMagicEngineTests.PactMagic_ElementStatistics_ProjectedIntoEffectPool_AndModifiableByTagOrExactName` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\PactMagicEngineTests.cs:702)

---

### Island 201: `PlayMore35.Subsystems.SubsystemElement`

**Island Roots (No production callers):**
- `PlayMore35.Subsystems.SubsystemElement.AddEffect` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Subsystems\SubsystemElement.cs:172)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.FakeRulesCatalog.CreateSyntheticAlchemySubsystem` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\FakeRulesCatalog.cs:522)
- 🧪 `PlayMore35.Tests.MartialAdeptProgressionTests.SubsystemElement_JsonSerializationRoundTrip_PreservesAllFields` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialAdeptProgressionTests.cs:389)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.ContributeEffects_ProjectsActiveStanceModifiers_AndIgnoresInactiveStances` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:816)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.ExpendVerb_SchedulesSheetActionForBoostAndCounter_AndExpiresOnTimelineAdvance` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:851)
- 🧪 `PlayMore35.Tests.MartialManeuverSessionIntegrationTests.ActiveStance_AffectingEquippedWeaponAttacks_RecalculatesRoutines` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverSessionIntegrationTests.cs:160)

---

### Island 202: `PlayMore35.Subsystems.SubsystemEngine`

**Island Roots (No production callers):**
- `PlayMore35.Subsystems.SubsystemEngine..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Subsystems\SubsystemEngine.cs:31)
- `PlayMore35.Subsystems.SubsystemEngine.SetRandom` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Subsystems\SubsystemEngine.cs:56)

**Cascaded Methods (Downstream dead calls):**
- `PlayMore35.Subsystems.SubsystemEngine.set_Rng` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Subsystems\SubsystemEngine.cs:29)
- `PlayMore35.Subsystems.SubsystemEngine.InitializeStorage` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Subsystems\SubsystemEngine.cs:60)
- `PlayMore35.Subsystems.SubsystemSlotGroup<TSlot>..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Subsystems\SubsystemSlotGroup.cs:12)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_Constructor_ThrowsArgumentNullException_OnNullDefinitionOrContext` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:24)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.DealCrusader_DealsGrantedManeuversAvailable_AndRemainderWithheld` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:465)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.AdvanceCrusaderTurn_GrantsOneWithheldManeuver_EachTurn` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:496)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.AdvanceCrusaderTurn_ReshufflesDeck_WhenWithheldPoolIsEmpty` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:536)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.ResetEncounter_RestoresReadiedManeuversAcrossAllSources_AndRedealsCrusader` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:563)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.SubsystemEngine_FullCatalogRecovery_OperatesAcrossCrusaderSwordsageAndWarblade` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:1016)
- 🧪 `PlayMore35.Tests.MartialManeuverSessionIntegrationTests.Crusader_EncounterDeck_DealAdvanceAndResetInSession` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverSessionIntegrationTests.cs:325)
- 🧪 `PlayMore35.Tests.SyntheticSubsystemTests.CreateSyntheticSession` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SyntheticSubsystemTests.cs:15)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemSlotGroup_AllowsDecoupledAccessAndCapacityTracking_Dynamically` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:96)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemSlotGroup_NegativeCapacity_ClampsToZero_AndAllowsSlotAccess` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:197)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemSlotGroup_CapacityTransitions_PreserveOccupiedSlotsAndPadEmptySlots` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:239)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemSlotGroup_NullCapacityProvider_ThrowsArgumentNullException` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:276)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemSlotGroup_DefaultMaxLevel_IsIntMaxValue_WhenNoProviderSupplied` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:316)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemSlotGroup_EvaluatesLevelProviderDynamically` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:375)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemSlotGroup_IsSlotLevelValid_EvaluatesCorrectly_AcrossSlotsBeyondCapacity` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:411)

---

### Island 203: `PlayMore35.Subsystems.SubsystemEngine`

**Island Roots (No production callers):**
- `PlayMore35.Subsystems.SubsystemEngine.GetMaxLevel` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Subsystems\SubsystemEngine.cs:234)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.SubsystemEngine_WiresCapacityAndLevelGating_ToCharacterStats` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:241)
- 🧪 `PlayMore35.Tests.MartialManeuverSessionIntegrationTests.SingleClass_Warblade_InitiatorLevelAndCapacities_CalculatedThroughSession` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverSessionIntegrationTests.cs:48)
- 🧪 `PlayMore35.Tests.MartialManeuverSessionIntegrationTests.Multiclass_InitiatorLevel_CalculatedSeparatelyForWarbladeAndCrusader` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverSessionIntegrationTests.cs:73)
- 🧪 `PlayMore35.Tests.MartialManeuverSessionIntegrationTests.NonAdept_MartialStudyFeat_CalculatesFeatInitiatorLevelInSession` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverSessionIntegrationTests.cs:101)

---

### Island 204: `PlayMore35.Subsystems.SubsystemEngine`

**Island Roots (No production callers):**
- `PlayMore35.Subsystems.SubsystemEngine.SetSlotState` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Subsystems\SubsystemEngine.cs:439)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.PactMagicEngineTests.PactMagic_BindingAndSlotClamping_UsesSubsystemSlotGroup` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\PactMagicEngineTests.cs:101)
- 🧪 `PlayMore35.Tests.PactMagicEngineTests.PactMagic_ContributeEffects_AddsAbilitiesInfluenceAndDefianceDebuffs` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\PactMagicEngineTests.cs:153)
- 🧪 `PlayMore35.Tests.PactMagicEngineTests.PactMagic_DefianceResetsOnUnbindRebindGoodPactAndTimelineExpiration` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\PactMagicEngineTests.cs:279)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_ContributeEffects_ProjectsTemplateEffectsAndResetsParametersGenerically` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:770)

---

### Island 205: `PlayMore35.Subsystems.SubsystemEngine`

**Island Roots (No production callers):**
- `PlayMore35.Subsystems.SubsystemEngine.GetSlotElement` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Subsystems\SubsystemEngine.cs:490)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.ControllerShrinkageAndIntegrationTests.Subsystem_FullBindingLifecycle_WithTimelineAdvanceAndReload` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\ControllerShrinkageAndIntegrationTests.cs:58)
- 🧪 `PlayMore35.Tests.ControllerShrinkageAndIntegrationTests.DeclarativePactMagic_UnbindVestige_ResetsDefianceAndSlots` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\ControllerShrinkageAndIntegrationTests.cs:122)
- 🧪 `PlayMore35.Tests.CoreDecouplingTests.SubsystemAction_UnbindVestige_DispatchesThroughSubsystemEngine` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\CoreDecouplingTests.cs:89)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.SubsystemEngine_ManagesKnownAndStanceSlots_AndTracksReadied` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:115)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.SubsystemState_PreservesSourceIsolation_AndRoundTripsJson` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:208)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.AssignSlot_AndClearSlot_Readied_MutateSlotsCorrectly` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:1060)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.AssignSlot_AndClearSlot_Stances_MutateSlotsCorrectly` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:1075)
- 🧪 `PlayMore35.Tests.MartialManeuverSessionIntegrationTests.CharacterSession_FullJsonSaveLoadRoundTrip_PreservesAllManeuverState` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverSessionIntegrationTests.cs:271)
- 🧪 `PlayMore35.Tests.MartialManeuverSessionIntegrationTests.MartialAdept_LevelUp_PreservesFeatBoundSlots_AndPreventsRetraining` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverSessionIntegrationTests.cs:493)
- 🧪 `PlayMore35.Tests.MartialManeuverSessionIntegrationTests.ProjectFeatSlots_SameManeuverAsClassProgression_DoesNotHijackClassSlot` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverSessionIntegrationTests.cs:837)
- 🧪 `PlayMore35.Tests.MultiSessionIsolationTests.OdricJson_BackwardsCompatibility_LoadsAndRecalculatesAccurately` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MultiSessionIsolationTests.cs:125)
- 🧪 `PlayMore35.Tests.RulesCatalogTests.PactMagicEngine_ResolvesCatalogFromContext_OnInit` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\RulesCatalogTests.cs:395)
- 🧪 `PlayMore35.Tests.SheetActionTests.DeclarativeSubsystemAction_SerializesAndPerforms_DirectlyOnSubsystemEngine` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SheetActionTests.cs:91)
- 🧪 `PlayMore35.Tests.SheetActionTests.SheetAction_Perform_DispatchesViaGetSubsystem` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SheetActionTests.cs:315)

---

### Island 206: `PlayMore35.Subsystems.SubsystemEngine`

**Island Roots (No production callers):**
- `PlayMore35.Subsystems.SubsystemEngine.GetSlotState` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Subsystems\SubsystemEngine.cs:495)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.ControllerShrinkageAndIntegrationTests.Subsystem_FullBindingLifecycle_WithTimelineAdvanceAndReload` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\ControllerShrinkageAndIntegrationTests.cs:58)
- 🧪 `PlayMore35.Tests.ControllerShrinkageAndIntegrationTests.DeclarativePactMagic_UnbindVestige_ResetsDefianceAndSlots` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\ControllerShrinkageAndIntegrationTests.cs:122)
- 🧪 `PlayMore35.Tests.CoreDecouplingTests.SubsystemAction_UnbindVestige_DispatchesThroughSubsystemEngine` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\CoreDecouplingTests.cs:89)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.SubsystemEngine_ManagesKnownAndStanceSlots_AndTracksReadied` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:115)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.ExpendVerb_TransitionsAvailableToExpended_AndRejectsNonAvailable` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:396)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.RecoverWarblade_RecoversAllExpendedManeuvers_PreservingUnreadied` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:426)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.RecoverSwordsage_RecoversSingleChosenManeuver` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:446)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.DealCrusader_DealsGrantedManeuversAvailable_AndRemainderWithheld` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:465)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.ResetEncounter_RestoresReadiedManeuversAcrossAllSources_AndRedealsCrusader` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:563)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.ReadyManeuver_AndUnreadyManeuver_TransitionsSlotLifecycle` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:612)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.AdoptStance_SetsActive_AndClearsOtherStances_WhenDualStanceNotPermitted` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:636)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.AdoptStance_PreservesBothActiveStances_WhenActiveStancesIsTwo` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:675)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.AdoptStance_CapsConcurrentActiveStancesAtTwo_UsingOldestFallback_WhenNoTargetSpecified` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:699)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.AdoptStance_WithExplicitReplacementTarget_ReplacesSpecifiedStance_WhenAtCapacity` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:739)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.AdoptStances_AdoptsBothStancesSimultaneously_ClearingPrevious` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:769)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.ExpendVerb_SchedulesSheetActionForBoostAndCounter_AndExpiresOnTimelineAdvance` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:851)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.SubsystemEngine_FullCatalogRecovery_OperatesAcrossCrusaderSwordsageAndWarblade` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:1016)
- 🧪 `PlayMore35.Tests.MartialManeuverSessionIntegrationTests.ActiveStance_ModifiesArmorClass_AndRecalculatesDynamically` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverSessionIntegrationTests.cs:126)
- 🧪 `PlayMore35.Tests.MartialManeuverSessionIntegrationTests.InCombat_Initiation_TimelineBoostExpiration_AndRecoveryLoop` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverSessionIntegrationTests.cs:212)
- 🧪 `PlayMore35.Tests.MartialManeuverSessionIntegrationTests.CharacterSession_FullJsonSaveLoadRoundTrip_PreservesAllManeuverState` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverSessionIntegrationTests.cs:271)
- 🧪 `PlayMore35.Tests.MartialManeuverSessionIntegrationTests.Crusader_EncounterDeck_DealAdvanceAndResetInSession` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverSessionIntegrationTests.cs:325)
- 🧪 `PlayMore35.Tests.MartialManeuverSessionIntegrationTests.FeatBoundSlot_ReadiedState_PreservedAcrossRecalculations` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverSessionIntegrationTests.cs:457)
- 🧪 `PlayMore35.Tests.MartialManeuverSessionIntegrationTests.MartialStance_FeatBoundSlot_ActiveState_PreservedAcrossRecalculations` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverSessionIntegrationTests.cs:707)
- 🧪 `PlayMore35.Tests.MultiSessionIsolationTests.OdricJson_BackwardsCompatibility_LoadsAndRecalculatesAccurately` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MultiSessionIsolationTests.cs:125)
- 🧪 `PlayMore35.Tests.SheetActionTests.DeclarativeSubsystemAction_SerializesAndPerforms_DirectlyOnSubsystemEngine` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SheetActionTests.cs:91)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_TransitionSlot_ActiveStateClamping_UsesStateCapacities` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:1399)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_TransitionSlot_DynamicStateClamping_CustomNonActiveState` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:1561)

---

### Island 207: `PlayMore35.Subsystems.SubsystemEngine`

**Island Roots (No production callers):**
- `PlayMore35.Subsystems.SubsystemEngine.IsSlotLevelValid` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Subsystems\SubsystemEngine.cs:500)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.SubsystemEngine_WiresCapacityAndLevelGating_ToCharacterStats` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:241)
- 🧪 `PlayMore35.Tests.PactMagicEngineTests.PactMagic_SlotLevelValidation_ReflectsVestigeLevelAndMaxVestigeLevel` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\PactMagicEngineTests.cs:464)
- 🧪 `PlayMore35.Tests.PactMagicEngineTests.PactMagic_LevelStatistic_ProjectedIntoEffectPool_AndModifiable` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\PactMagicEngineTests.cs:817)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_IsSlotLevelValid_GatesDynamicallyBySlotGroupMaxLevel` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:579)

---

### Island 208: `PlayMore35.Subsystems.SubsystemEngine`

**Island Roots (No production callers):**
- `PlayMore35.Subsystems.SubsystemEngine.ClearSlot` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Subsystems\SubsystemEngine.cs:581)
- `PlayMore35.Subsystems.SubsystemSlotGroup<TSlot>.IsSlotLevelValid` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Subsystems\SubsystemSlotGroup.cs:36)

**Cascaded Methods (Downstream dead calls):**
- `PlayMore35.Subsystems.SubsystemSlotGroup<TSlot>.ClearSlot` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Subsystems\SubsystemSlotGroup.cs:78)
- `PlayMore35.Subsystems.SubsystemSlotGroup<TSlot>.ValidateIndex` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Subsystems\SubsystemSlotGroup.cs:89)
- `PlayMore35.Subsystems.SubsystemSlotGroup<TSlot>.get_MaxLevel` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Subsystems\SubsystemSlotGroup.cs:34)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.AssignSlot_AndClearSlot_Readied_MutateSlotsCorrectly` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:1060)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.AssignSlot_AndClearSlot_Stances_MutateSlotsCorrectly` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:1075)
- 🧪 `PlayMore35.Tests.MartialManeuverSessionIntegrationTests.FeatBoundSlot_CannotBeOverwrittenOrCleared_ThrowsInvalidOperationException` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverSessionIntegrationTests.cs:423)
- 🧪 `PlayMore35.Tests.MartialManeuverSessionIntegrationTests.MartialAdept_LevelUp_PreservesFeatBoundSlots_AndPreventsRetraining` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverSessionIntegrationTests.cs:493)
- 🧪 `PlayMore35.Tests.MartialManeuverSessionIntegrationTests.MartialStance_FeatBoundSlot_CannotBeOverwrittenOrCleared_ThrowsInvalidOperationException` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverSessionIntegrationTests.cs:673)
- 🧪 `PlayMore35.Tests.UniversalSubsystemPersistenceTests.CharacterSession_TwoWaySync_EngineMutationsSyncToCharacterSubsystems` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\UniversalSubsystemPersistenceTests.cs:220)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemSlotGroup_Slots_AreGuaranteedNonNullInstances` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:35)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemSlotGroup_AllowsDecoupledAccessAndCapacityTracking_Dynamically` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:96)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemSlotGroup_ClearSlot_ResetsSpecificSlotToDefault` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:163)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemSlotGroup_IsSlotLevelValid_ReturnsTrue_WhenNoProviderOrSelectorSupplied` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:325)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemSlotGroup_IsSlotLevelValid_ValidatesLevelAgainstMaxLevel` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:333)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemSlotGroup_OutOfLevelAssignment_PreservesEnableDontEnforceWithoutThrowing` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:359)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemSlotGroup_EvaluatesLevelProviderDynamically` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:375)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemSlotGroup_IsSlotLevelValid_NegativeSlotIndex_ThrowsArgumentOutOfRangeException` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:400)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemSlotGroup_IsSlotLevelValid_EvaluatesCorrectly_AcrossSlotsBeyondCapacity` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:411)

---

### Island 209: `PlayMore35.Subsystems.SubsystemEngine`

**Island Roots (No production callers):**
- `PlayMore35.Subsystems.SubsystemEngine.InvokeVerb` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Subsystems\SubsystemEngine.cs:918)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.ExpendVerb_TransitionsAvailableToExpended_AndRejectsNonAvailable` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:396)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.RecoverWarblade_RecoversAllExpendedManeuvers_PreservingUnreadied` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:426)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.RecoverSwordsage_RecoversSingleChosenManeuver` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:446)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.DealCrusader_DealsGrantedManeuversAvailable_AndRemainderWithheld` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:465)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.AdvanceCrusaderTurn_GrantsOneWithheldManeuver_EachTurn` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:496)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.AdvanceCrusaderTurn_ReshufflesDeck_WhenWithheldPoolIsEmpty` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:536)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.ResetEncounter_RestoresReadiedManeuversAcrossAllSources_AndRedealsCrusader` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:563)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.ReadyManeuver_AndUnreadyManeuver_TransitionsSlotLifecycle` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:612)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.AdoptStance_SetsActive_AndClearsOtherStances_WhenDualStanceNotPermitted` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:636)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.AdoptStance_PreservesBothActiveStances_WhenActiveStancesIsTwo` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:675)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.AdoptStance_CapsConcurrentActiveStancesAtTwo_UsingOldestFallback_WhenNoTargetSpecified` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:699)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.AdoptStance_WithExplicitReplacementTarget_ReplacesSpecifiedStance_WhenAtCapacity` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:739)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.AdoptStances_AdoptsBothStancesSimultaneously_ClearingPrevious` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:769)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.ContributeEffects_ProjectsActiveStanceModifiers_AndIgnoresInactiveStances` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:816)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.ExpendVerb_SchedulesSheetActionForBoostAndCounter_AndExpiresOnTimelineAdvance` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:851)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.SubsystemEngine_FullCatalogStanceActivation_ModifiesStatsAcrossClasses` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:950)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.SubsystemEngine_FullCatalogRecovery_OperatesAcrossCrusaderSwordsageAndWarblade` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:1016)
- 🧪 `PlayMore35.Tests.MartialManeuverSessionIntegrationTests.ActiveStance_ModifiesArmorClass_AndRecalculatesDynamically` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverSessionIntegrationTests.cs:126)
- 🧪 `PlayMore35.Tests.MartialManeuverSessionIntegrationTests.ActiveStance_AffectingEquippedWeaponAttacks_RecalculatesRoutines` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverSessionIntegrationTests.cs:160)
- 🧪 `PlayMore35.Tests.MartialManeuverSessionIntegrationTests.InCombat_Initiation_TimelineBoostExpiration_AndRecoveryLoop` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverSessionIntegrationTests.cs:212)
- 🧪 `PlayMore35.Tests.MartialManeuverSessionIntegrationTests.CharacterSession_FullJsonSaveLoadRoundTrip_PreservesAllManeuverState` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverSessionIntegrationTests.cs:271)
- 🧪 `PlayMore35.Tests.MartialManeuverSessionIntegrationTests.Crusader_EncounterDeck_DealAdvanceAndResetInSession` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverSessionIntegrationTests.cs:325)
- 🧪 `PlayMore35.Tests.MartialManeuverSessionIntegrationTests.FeatBoundSlot_ReadiedState_PreservedAcrossRecalculations` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverSessionIntegrationTests.cs:457)
- 🧪 `PlayMore35.Tests.MartialManeuverSessionIntegrationTests.SerializationRoundTrip_PreservesFeatBoundSlotProperties` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverSessionIntegrationTests.cs:567)
- 🧪 `PlayMore35.Tests.MartialManeuverSessionIntegrationTests.MartialStance_FeatBoundSlot_ActiveState_PreservedAcrossRecalculations` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverSessionIntegrationTests.cs:707)
- 🧪 `PlayMore35.Tests.MartialManeuverSessionIntegrationTests.MartialStance_SerializationRoundTrip_PreservesFeatBoundSlotProperties` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverSessionIntegrationTests.cs:783)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_TransitionSlot_ActiveStateClamping_UsesStateCapacities` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:1399)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_TransitionSlot_DynamicStateClamping_CustomNonActiveState` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:1561)

---

### Island 210: `PlayMore35.Subsystems.SubsystemEngine`

**Island Roots (No production callers):**
- `PlayMore35.Subsystems.SubsystemEngine.ValidateChoice` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Subsystems\SubsystemEngine.cs:1396)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 211: `PlayMore35.Subsystems.SubsystemEngine`

**Island Roots (No production callers):**
- `PlayMore35.Subsystems.SubsystemEngine.Validate` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Subsystems\SubsystemEngine.cs:1512)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.Validate_WarnsWhenReadiedCountExceedsCapacity` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:345)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.Validate_WarnsWhenActiveStancesExceedsCapacity` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:797)
- 🧪 `PlayMore35.Tests.MartialManeuverEngineTests.SubsystemEngine_SlotGroupValidation_FlagsMismatchingFilterGenerically` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\MartialManeuverEngineTests.cs:1118)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_Validate_EvaluatesDynamicPrerequisites` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:1279)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_Validate_GenericStateCapacities_EmitsDynamicCapacityWarning` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:1365)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemEngine_Validate_EmitsCharacterCapacityWarning_WhenDistinctSourcesNotOne` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:1527)

---

### Island 212: `PlayMore35.Subsystems.SubsystemPoolOperation`

**Island Roots (No production callers):**
- `PlayMore35.Subsystems.SubsystemPoolOperation..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Subsystems\SubsystemSlot.cs:132)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 213: `PlayMore35.Subsystems.SubsystemTarget`

**Island Roots (No production callers):**
- `PlayMore35.Subsystems.SubsystemTarget..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Subsystems\SubsystemSlot.cs:163)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 214: `PlayMore35.Subsystems.SubsystemSlotGrant`

**Island Roots (No production callers):**
- `PlayMore35.Subsystems.SubsystemSlotGrant..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Subsystems\SubsystemSlot.cs:180)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 215: `PlayMore35.Subsystems.SubsystemSlotGroup<TSlot>`

**Island Roots (No production callers):**
- `PlayMore35.Subsystems.SubsystemSlotGroup<TSlot>..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Subsystems\SubsystemSlotGroup.cs:26)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemSlotGroup_Slots_AreGuaranteedNonNullInstances` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:35)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemSlotGroup_FixedCapacity_InitializesAndSetsSlots` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:142)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemSlotGroup_ClearSlot_ResetsSpecificSlotToDefault` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:163)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemSlotGroup_ClearAll_ResetsAllSlotsToDefault` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:182)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemSlotGroup_ZeroCapacity_AllowsPopulatingAndReadingSlots` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:219)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemSlotGroup_DefaultMaxLevel_IsIntMaxValue_WhenNoProviderSupplied` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:316)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemSlotGroup_IsSlotLevelValid_ReturnsTrue_WhenNoProviderOrSelectorSupplied` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:325)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemSlotGroup_IsSlotLevelValid_ValidatesLevelAgainstMaxLevel` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:333)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemSlotGroup_OutOfLevelAssignment_PreservesEnableDontEnforceWithoutThrowing` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:359)
- 🧪 `PlayMore35.Tests.SubsystemEngineTests.SubsystemSlotGroup_IsSlotLevelValid_NegativeSlotIndex_ThrowsArgumentOutOfRangeException` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemEngineTests.cs:400)

---

### Island 216: `PlayMore35.Subsystems.SubsystemSlotGroup<TSlot>`

**Island Roots (No production callers):**
- `PlayMore35.Subsystems.SubsystemSlotGroup<TSlot>.EnsureCapacity` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Subsystems\SubsystemSlotGroup.cs:95)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 217: `PlayMore35.Subsystems.SubsystemState`

**Island Roots (No production callers):**
- `PlayMore35.Subsystems.SubsystemState.set_Slots` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Subsystems\SubsystemState.cs:9)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 218: `PlayMore35.Subsystems.SubsystemState`

**Island Roots (No production callers):**
- `PlayMore35.Subsystems.SubsystemState.set_Pools` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\Subsystems\SubsystemState.cs:12)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 219: `PlayMore35.ConfigureAttackRoutinesDialog`

**Island Roots (No production callers):**
- `PlayMore35.ConfigureAttackRoutinesDialog.GetCategoryChecked` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\UI\ConfigureAttackRoutinesDialog.cs:19)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.ConfigureAttackRoutinesDialogTests.Dialog_InitializesWithCurrentPreferences` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\ConfigureAttackRoutinesDialogTests.cs:13)
- 🧪 `PlayMore35.Tests.ConfigureAttackRoutinesDialogTests.Dialog_ResetToDefaults_RestoresTabletopDefaults` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\ConfigureAttackRoutinesDialogTests.cs:39)

---

### Island 220: `PlayMore35.ConfigureAttackRoutinesDialog`

**Island Roots (No production callers):**
- `PlayMore35.ConfigureAttackRoutinesDialog.SetCategoryChecked` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\UI\ConfigureAttackRoutinesDialog.cs:23)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.ConfigureAttackRoutinesDialogTests.Dialog_SavePreferences_UpdatesModel` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\ConfigureAttackRoutinesDialogTests.cs:26)

---

### Island 221: `PlayMore35.Global.FormUtils`

**Island Roots (No production callers):**
- `PlayMore35.Global.FormUtils.ClearDynamicControls` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\UI\FormUtils.cs:32)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 222: `PlayMore35.Global.LabelExtensions`

**Island Roots (No production callers):**
- `PlayMore35.Global.LabelExtensions.Initialize` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\UI\FormUtils.cs:123)

**Cascaded Methods (Downstream dead calls):**
- `PlayMore35.Global.LabelExtensions.SendMessage` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\UI\FormUtils.cs:109)
- `PlayMore35.Global.LabelExtensions.ResizeTooltip` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\UI\FormUtils.cs:112)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 223: `PlayMore35.LevelDetailsDialog.ElementReceptorBinding`

**Island Roots (No production callers):**
- `PlayMore35.LevelDetailsDialog.ElementReceptorBinding.MatchesKey` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\UI\LevelDetailsDialog.cs:55)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 224: `PlayMore35.LevelDetailsDialog.ChoiceItem`

**Island Roots (No production callers):**
- `PlayMore35.LevelDetailsDialog.ChoiceItem..ctor` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\UI\LevelDetailsDialog.cs:77)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 225: `PlayMore35.MainForm`

**Island Roots (No production callers):**
- `PlayMore35.MainForm.StatsTab` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\UI\MainForm.cs:125)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 226: `PlayMore35.MainForm`

**Island Roots (No production callers):**
- `PlayMore35.MainForm.Parse` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\UI\MainForm.cs:378)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 227: `PlayMore35.UI.TabularLedgerPanel`

**Island Roots (No production callers):**
- `PlayMore35.UI.TabularLedgerPanel.GetRow` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\UI\TabularLedgerPanel.cs:43)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.TabularLedgerPanelTests.TabularLedgerPanel_SetEntries_CreatesRowsAndHeaders` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\TabularLedgerPanelTests.cs:13)
- 🧪 `PlayMore35.Tests.TabularLedgerPanelTests.TabularLedgerPanel_Selection_HighlightsRow` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\TabularLedgerPanelTests.cs:47)
- 🧪 `PlayMore35.Tests.TabularLedgerPanelTests.TabularLedgerPanel_GenericReuse_CleanlyAdaptsToArbitraryLedgerEntries` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\TabularLedgerPanelTests.cs:86)
- 🧪 `PlayMore35.Tests.TabularLedgerPanelTests.TabularLedgerPanel_ShrinkToOverflowThenExpand_RestoresRowPositions` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\TabularLedgerPanelTests.cs:214)
- 🧪 `PlayMore35.Tests.TabularLedgerPanelTests.TabularLedgerPanel_ShrinkToOverflow_RefreshEntriesWhileScrolled_ThenExpand_RestoresRowPositions` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\TabularLedgerPanelTests.cs:263)
- 🧪 `PlayMore35.Tests.TabularLedgerPanelTests.MainForm_ShrinkWindowToScrollThenExpand_LedgerRemainsVisible` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\TabularLedgerPanelTests.cs:322)
- 🧪 `PlayMore35.Tests.TabularLedgerPanelTests.TabularLedgerPanel_Columns_AlignWithBaselineCoordinates` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\TabularLedgerPanelTests.cs:393)
- 🧪 `PlayMore35.Tests.XpSessionEntryTests.MainForm_CharacterTab_InitializesLedgerXpAndDateOverridePicker` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\XpSessionEntryTests.cs:161)
- 🧪 `PlayMore35.Tests.XpSessionEntryTests.MainForm_AddSessionAndRightClickDelete_MutatesCharacterSessionsAndUpdatesLedger` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\XpSessionEntryTests.cs:192)

---

### Island 228: `PlayMore35.UI.TabularLedgerPanel`

**Island Roots (No production callers):**
- `PlayMore35.UI.TabularLedgerPanel.SelectRow` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\UI\TabularLedgerPanel.cs:273)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.TabularLedgerPanelTests.TabularLedgerPanel_Selection_HighlightsRow` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\TabularLedgerPanelTests.cs:47)

---

### Island 229: `PlayMore35.UI.Subsystems.ElementReceptorControl`

**Island Roots (No production callers):**
- `PlayMore35.UI.Subsystems.ElementReceptorControl.SetWarning` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\UI\Subsystems\ElementReceptorControl.cs:115)

**Cascaded Methods (Downstream dead calls):**
- `PlayMore35.UI.WarningBadge.SetWarning` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\UI\WarningBadge.cs:46)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 230: `PlayMore35.UI.Subsystems.DefaultElementPicker`

**Island Roots (No production callers):**
- `PlayMore35.UI.Subsystems.DefaultElementPicker.GetFilteredElementNames` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\UI\Subsystems\DefaultElementPicker.cs:101)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.LevelDetailsDialogTests.MartialStudy_ZeroAdeptClasses_AutoSelectsFeats_NoClassDropdown` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\LevelDetailsDialogTests.cs:163)
- 🧪 `PlayMore35.Tests.LevelDetailsDialogTests.MartialStance_ZeroAdeptClasses_AutoSelectsFeats_NoClassDropdown` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\LevelDetailsDialogTests.cs:375)
- 🧪 `PlayMore35.Tests.LevelDetailsDialogTests.SyntheticSubsystemFeat_DialogRendersDependentMultiChoiceControls_AndSavesChoices` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\LevelDetailsDialogTests.cs:771)
- 🧪 `PlayMore35.Tests.SubsystemUITests.DefaultElementPicker_SearchFilter_FiltersElementList` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemUITests.cs:183)
- 🧪 `PlayMore35.Tests.SubsystemUITests.DefaultElementPicker_HideInvalidChoices_FiltersByWarningProvider` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemUITests.cs:203)
- 🧪 `PlayMore35.Tests.SubsystemUITests.DefaultElementPicker_HideInvalidChoices_RetainsItemDependentWarnings_AndFiltersUnmet` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemUITests.cs:230)
- 🧪 `PlayMore35.Tests.SubsystemUITests.DefaultElementPicker_FilterManeuvers_IncludesStrikesBoostsCountersOther_AndExcludesStances` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemUITests.cs:428)
- 🧪 `PlayMore35.Tests.SubsystemUITests.DefaultElementPicker_FilterStances_IncludesOnlyStances` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemUITests.cs:454)

---

### Island 231: `PlayMore35.UI.Subsystems.DefaultElementPicker`

**Island Roots (No production callers):**
- `PlayMore35.UI.Subsystems.DefaultElementPicker.SelectElementByName` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\UI\Subsystems\DefaultElementPicker.cs:111)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.DualPassPrerequisiteTests.LevelDetailsDialog_WithItemDependentChoice_DisplaysItemDependentBadge` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\DualPassPrerequisiteTests.cs:313)
- 🧪 `PlayMore35.Tests.LevelDetailsDialogTests.MartialStudy_ZeroAdeptClasses_AutoSelectsFeats_NoClassDropdown` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\LevelDetailsDialogTests.cs:163)
- 🧪 `PlayMore35.Tests.LevelDetailsDialogTests.MartialStudy_SingleAdeptClass_AutoSelectsAdeptClass_NoClassDropdown` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\LevelDetailsDialogTests.cs:202)
- 🧪 `PlayMore35.Tests.LevelDetailsDialogTests.MartialStudy_TwoOrMoreAdeptClasses_RendersClassDropdown_ContainsOnlyEligibleAdeptClasses` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\LevelDetailsDialogTests.cs:231)
- 🧪 `PlayMore35.Tests.LevelDetailsDialogTests.MartialStudy_ExceedsMaxManeuverLevel_DisplaysAdvisoryWarningBadge` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\LevelDetailsDialogTests.cs:266)
- 🧪 `PlayMore35.Tests.LevelDetailsDialogTests.MartialStudy_UnmetDisciplinePrerequisite_DisplaysAdvisoryWarningBadge` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\LevelDetailsDialogTests.cs:299)
- 🧪 `PlayMore35.Tests.LevelDetailsDialogTests.MartialStudy_PrerequisiteMetByEarlierManeuver_NoWarningBadge` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\LevelDetailsDialogTests.cs:323)
- 🧪 `PlayMore35.Tests.LevelDetailsDialogTests.MartialStance_ZeroAdeptClasses_AutoSelectsFeats_NoClassDropdown` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\LevelDetailsDialogTests.cs:375)
- 🧪 `PlayMore35.Tests.LevelDetailsDialogTests.MartialStance_SingleAdeptClass_AutoSelectsAdeptClass_NoClassDropdown` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\LevelDetailsDialogTests.cs:410)
- 🧪 `PlayMore35.Tests.LevelDetailsDialogTests.MartialStance_TwoOrMoreAdeptClasses_RendersClassDropdown_ContainsOnlyEligibleAdeptClasses` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\LevelDetailsDialogTests.cs:439)
- 🧪 `PlayMore35.Tests.LevelDetailsDialogTests.MartialStance_ExceedsMaxManeuverLevel_DisplaysAdvisoryWarningBadge` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\LevelDetailsDialogTests.cs:474)
- 🧪 `PlayMore35.Tests.LevelDetailsDialogTests.MartialStance_NoKnownDisciplineManeuvers_DisplaysAdvisoryWarningBadge` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\LevelDetailsDialogTests.cs:499)
- 🧪 `PlayMore35.Tests.LevelDetailsDialogTests.MartialStance_PrerequisiteMetByEarlierManeuver_NoWarningBadge` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\LevelDetailsDialogTests.cs:523)
- 🧪 `PlayMore35.Tests.LevelDetailsDialogTests.DialogValidation_PendingChoicesAcrossSlotsAtSameLevel_SatisfiesPrerequisitesWithoutSpuriousWarning` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\LevelDetailsDialogTests.cs:572)
- 🧪 `PlayMore35.Tests.LevelDetailsDialogTests.SyntheticSubsystemFeat_DialogRendersDependentMultiChoiceControls_AndSavesChoices` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\LevelDetailsDialogTests.cs:771)
- 🧪 `PlayMore35.Tests.LevelDetailsDialogTests.SyntheticSubsystemFeat_DialogWithTwoAdeptClasses_RendersBothDropdowns_AndParameterizesLevelGating` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\LevelDetailsDialogTests.cs:816)
- 🧪 `PlayMore35.Tests.LevelDetailsDialogTests.ComposableElementSelection_FullLifecycle_ActivationDeliverySwitchingClearAndCollapse` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\LevelDetailsDialogTests.cs:849)
- 🧪 `PlayMore35.Tests.SubsystemUITests.DefaultElementPicker_DeliversChoiceImmediately_AndRetainsDisplay` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemUITests.cs:138)
- 🧪 `PlayMore35.Tests.SubsystemUITests.DefaultElementPicker_DetailsPane_FormatsWarningMessagesByKind` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemUITests.cs:341)

---

### Island 232: `PlayMore35.UI.Subsystems.DefaultElementPicker`

**Island Roots (No production callers):**
- `PlayMore35.UI.Subsystems.DefaultElementPicker.SelectNone` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\UI\Subsystems\DefaultElementPicker.cs:120)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.SubsystemUITests.DefaultElementPicker_SelectingNone_DeliversNull` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemUITests.cs:162)
- 🧪 `PlayMore35.Tests.SubsystemUITests.DefaultElementPicker_DetailsPane_FormatsWarningMessagesByKind` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemUITests.cs:341)

---

### Island 233: `PlayMore35.UI.Subsystems.DefaultElementPicker`

**Island Roots (No production callers):**
- `PlayMore35.UI.Subsystems.DefaultElementPicker.DrawItemForTest` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\UI\Subsystems\DefaultElementPicker.cs:204)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.SubsystemUITests.DefaultElementPicker_ItemBadges_WorstKindPrecedenceAndColors` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemUITests.cs:277)

---

### Island 234: `PlayMore35.UI.Subsystems.ElementPickerContext`

**Island Roots (No production callers):**
- `PlayMore35.UI.Subsystems.ElementPickerContext.get_MultiSelect` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\UI\Subsystems\ElementPickerContext.cs:11)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 235: `PlayMore35.UI.Subsystems.ElementPickerContext`

**Island Roots (No production callers):**
- `PlayMore35.UI.Subsystems.ElementPickerContext.set_MultiSelect` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\UI\Subsystems\ElementPickerContext.cs:11)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 236: `PlayMore35.UI.Subsystems.ElementPickerContext`

**Island Roots (No production callers):**
- `PlayMore35.UI.Subsystems.ElementPickerContext.get_InitialSelections` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\UI\Subsystems\ElementPickerContext.cs:12)

**Artificial Life Support (Tests referencing this island):**
- *None (Pure dead code - no tests reference this island)*

---

### Island 237: `PlayMore35.UI.Subsystems.ElementReceptorControl`

**Island Roots (No production callers):**
- `PlayMore35.UI.Subsystems.ElementReceptorControl.SetWarning` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\UI\Subsystems\ElementReceptorControl.cs:110)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.DualPassPrerequisiteTests.ElementReceptorControl_VisualModes_UnmetAndItemDependent` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\DualPassPrerequisiteTests.cs:128)
- 🧪 `PlayMore35.Tests.SubsystemUITests.ElementReceptorControl_WarningHandling_SetsAndClearsWarning` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemUITests.cs:125)

---

### Island 238: `PlayMore35.UI.Subsystems.SubsystemUIRegistry`

**Island Roots (No production callers):**
- `PlayMore35.UI.Subsystems.SubsystemUIRegistry.GetBySubsystemId` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\UI\Subsystems\SubsystemUIRegistry.cs:41)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.PactMagicUITests.PactMagicUI_Registration_RegistersPactMagicAndVestigeAlias` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\PactMagicUITests.cs:18)
- 🧪 `PlayMore35.Tests.SubsystemUITests.SubsystemUIRegistry_RegisterAndRetrieveByIdAndElementType` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\SubsystemUITests.cs:17)

---

### Island 239: `PlayMore35.UI.Subsystems.PactMagic.PactMagicElementPicker`

**Island Roots (No production callers):**
- `PlayMore35.UI.Subsystems.PactMagic.PactMagicElementPicker.DeliverVestigeByName` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35\UI\Subsystems\PactMagic\PactMagicElementPicker.cs:144)

**Artificial Life Support (Tests referencing this island):**
- 🧪 `PlayMore35.Tests.PactMagicUITests.PactMagicTabPage_SelectingCard_PopulatesLeftDetailsPanel` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\PactMagicUITests.cs:100)
- 🧪 `PlayMore35.Tests.PactMagicUITests.PactMagicTabPage_BindingAndPinnedRow_MovesBoundCardToTopRow` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\PactMagicUITests.cs:128)
- 🧪 `PlayMore35.Tests.PactMagicUITests.PactMagicTabPage_MaxCapacity_DisplaysAtMaxCapacityNotice` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\PactMagicUITests.cs:192)
- 🧪 `PlayMore35.Tests.PactMagicUITests.PactMagicElementPicker_ProgressionMode_SuppressesButtonsAndPinnedContainer` (C:\Users\rickr\programming-projects\PlayMore3.5\PlayMore35.Tests\PactMagicUITests.cs:492)

---

