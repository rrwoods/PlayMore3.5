---
status: accepted
---

# Unified SubsystemStep and Character Invariant Hardening

## Context
The subsystem execution pipeline previously maintained parallel, duplicate data models:
- `SubsystemStep` defined declarative JSON steps in catalogs (`maneuvers.json`, `classes.json`, `vestiges.json`).
- `SubsystemTransition` and `SubsystemPoolOperation` defined duplicate DTO models in `PlayMore35/Subsystems/SubsystemSlot.cs`.
- `SubsystemVerbPipeline.ExecuteStep` manually copied fields from `SubsystemStep` into temporary `SubsystemTransition` or `SubsystemPoolOperation` instances just to dispatch execution internally.
- `SheetAction` exposed dual properties (`Transition`, `PoolOperation`) serializing separate JSON keys.

Furthermore, several dead code seams lingered:
- `IsSlotLevelValid`: an obsolete method on `SubsystemEngine` and `SubsystemSlotGroup` superseded by unified choice validation (`ValidateChoice`), having zero production callers and sustained solely by 15 unit tests.
- `ExecuteOperation`: a one-line pass-through forwarding alias for `ExecutePoolOperation`.
- `EnsureCapacity`: an uncalled private method in `SubsystemSlotGroup.cs`.

Additionally, `Character.cs` declared collection fields without initializers, permitting `new Character()` to instantiate null collections. This forced defensive null guards (`!= null`, `??`) across `CharacterSession` and `SubsystemEngine`. Simultaneously, `Timeline.ScheduledActions` invoked `.ToDictionary(...)`, allocating a new dictionary on every derived effect recalculation in `SubsystemEngine`.

## Decision

We unify the subsystem execution model, harden domain entity invariants, and prune obsolete seams:

1. **Canonical `SubsystemStep` Execution Model**:
   - `SubsystemStep` is the single canonical execution model across `SubsystemDefinition`, `SubsystemVerbPipeline`, `SubsystemEngine`, and `SheetAction`.
   - Remove duplicate adapter models `SubsystemTransition` and `SubsystemPoolOperation` in `SubsystemSlot.cs`.
   - Expose static factory helpers on `SubsystemStep` (`Transition`, `Pool`, `ElementVerb`, `Assign`) while preserving immutable properties and the canonical positional constructor per ADR-0008.
   - `SheetAction` replaces `Transition` and `PoolOperation` with a single `public SubsystemStep? Step { get; set; }` property, serializing directly to `"step"`.

2. **Clean Separation of Rule Definition and Runtime Targeting**:
   - `SubsystemStep` remains strictly declarative, defining the action to perform without runtime targeting fields.
   - `SubsystemTarget` remains the exclusive carrier of runtime execution targeting (`SlotGroup`, `SlotIndex`, `Element`, `Source`, `SelectedIndices`).

3. **Consolidation of Execution Primitives on `ExecuteStep`**:
   - `SubsystemEngine` and `SubsystemVerbPipeline` expose `ExecuteStep(SubsystemStep step, SubsystemTarget? target = null)` as the sole execution entry point for subsystem steps.
   - Remove `ExecuteTransition`, `ExecutePoolOperation`, and `ExecuteOperation` completely.

4. **Pruning Obsolete and Zombie Seams**:
   - Remove `IsSlotLevelValid` from `SubsystemSlotGroup.cs` and `SubsystemEngine.cs`.
   - Remove `ExecuteOperation` from `SubsystemEngine.cs` and `SubsystemVerbPipeline.cs`.
   - Remove uncalled `EnsureCapacity` from `SubsystemSlotGroup.cs`.
   - Delete the 16 artificial life-support tests sustaining these dead seams.

5. **Guaranteed Non-Null Collection Invariants on `Character`**:
   - All collection properties on `Character` (`assignedAbilityScores`, `bonusLanguages`, `classLevels`, `abilityScoreIncrements`, `rolledHitDice`, `skillRanks`, `feats`, `featChoices`, `transactions`, `inventory`, `equipped`, `sessions`, `externalEffects`, `Subsystems`, `flagValues`, `paramValues`) are initialized to non-null instances at declaration and construction.
   - Purge redundant defensive null guards and fallback coalescing across `CharacterSession` and `SubsystemEngine`.

6. **Null Object Pattern for `LevelGatingDefinition`**:
   - Introduce `LevelGatingDefinition.None` with `IsEnabled => !string.IsNullOrEmpty(ElementStatistic)`.
   - Default `SubsystemDefinition.LevelGating` to `LevelGatingDefinition.None`, eliminating null checks and null-forgiving assertions (`!.`).

7. **Allocation-Free `Timeline` Inspection**:
   - Delete the allocating `Timeline.ScheduledActions` property.
   - Expose allocation-free domain queries: `ActiveActions` (yielding all active actions across scheduled actions and encounter actions), `ScheduledActionCount`, and `GetScheduledActions(long round)`.

## Considered Options

- **Retaining `ExecuteTransition` / `ExecutePoolOperation` Alongside `ExecuteStep`**: Rejected under the Falsifiable Deletion Test. Maintaining duplicate typed entry points creates unnecessary method sprawl when `ExecuteStep` already acts as the universal dispatcher.
- **Blending `SlotIndex` and `SelectedIndices` into `SubsystemStep`**: Rejected. Conflates declarative catalog rule specifications with runtime execution targeting, reinflating adapter duplication.
- **Putting `Invert()` on `SubsystemStep`**: Rejected. A `SubsystemStep` is a declarative catalog instruction. In live companion play, reversions are a timeline lifecycle concern of `SheetAction` (expiring buff flags and restoring cooldown charges), not a mathematical property of static catalog steps.

## Consequences

- Completely eliminates adapter allocation and field-copying overhead in `SubsystemVerbPipeline`.
- Enforces strict construction invariants on `Character`, eliminating defensive null checks and potential `NullReferenceException` bugs.
- Eliminates heap allocations during timeline effect derivations in `SubsystemEngine`.
- Deletes 16 zombie tests, keeping test suites focused on live behavioral verification.
