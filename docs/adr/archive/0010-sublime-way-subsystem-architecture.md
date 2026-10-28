---
status: superseded by ADR-0006
---

# Sublime Way Subsystem Architecture

> [!WARNING]
> **Superseded by [ADR-0006](../0006-declarative-subsystems-and-primitive-verbs.md)**: The bespoke `MartialManeuverEngine` architecture, class recovery routines, and `ManeuverSheetAction` were superseded by the unified declarative `SubsystemEngine` and primitive-verb engine.

## Context
The Sublime Way martial system (*Tome of Battle: The Book of Nine Swords*) introduces martial maneuvers and stances across nine disciplines. Characters acquire maneuvers and stances through martial adept classes (Warblade, Crusader, Swordsage) or feats (Martial Study, Martial Stance). The subsystem requires modeling two-stage preparation (known vs. readied), class-isolated pools, divergent in-combat recovery mechanics (Crusader card decks, Warblade swift flourishes, Swordsage full-round meditation), and seamless integration with the headless `Timeline` and `EffectPool` without introducing speculative frameworks or violating Enable, Don't Enforce (ADR-0001).

## Decision
We structure the Sublime Way around a single cohesive engine `MartialManeuverEngine` (`Id = "martial_maneuvers"`) composing deepened `SubsystemSlotGroup<TSlot>` delegates partitioned by initiator source:

1. **Deepened Slot Groups with Level Gating (`SubsystemSlotGroup<TSlot>`)**:
   - `SubsystemSlotGroup<TSlot>` is deepened with optional `maxLevelProvider` and `levelSelector` delegates to evaluate level-based access gating across both Pact Magic and Martial Maneuvers per the Rule of Two (ADR-0006).
   - The slot group exposes `IsSlotLevelValid(int slotIndex)` for non-blocking advisory Warning Badges (ADR-0001) without rejecting out-of-level assignments.

2. **Direct Initiator Partitions and Unified Caller Seam**:
   - `MartialManeuverState` organizes data directly by initiator source without intermediate wrapper states: `Dictionary<string, List<KnownManeuverSlot>> Known` and `Dictionary<string, List<StanceSlot>> Stances`.
   - Keys include adept classes (`"Warblade"`, `"Crusader"`, `"Swordsage"`) and non-adept feat grants (`"Feats"`).
   - `MartialManeuverEngine` acts as the single cohesive caller seam for all slot mutations, queries, capacity checks, and lifecycle actions (`SetKnownSlot`, `GetKnownSlot`, `ClearKnownSlot`, `SetStanceSlot`, `GetStanceSlot`, `ClearStanceSlot`, `AdoptStance`, `InitiateManeuver`, recovery routines).
   - `SubsystemSlotGroup<TSlot>` binds directly to the state's underlying lists via an optional `storage` constructor parameter, eliminating shadow arrays, synchronization loops, and dynamic wrapper allocations.

3. **Four-State Lifecycle & Initiation Seam**:
   - `KnownManeuverSlot` tracks `ManeuverName` and an explicit 4-state enum `ManeuverState { Unreadied, Withheld, Available, Expended }`.
   - Readied status is evaluated as `State != ManeuverState.Unreadied`. Readied capacity is checked against `maneuvers readied:<source>` via advisory Warning Badges.
   - Combat initiation is uniform across all classes: any maneuver with `State == ManeuverState.Available` may be initiated, transitioning to `ManeuverState.Expended`.
   - Crusader encounters deal $K$ readied maneuvers into `Available` (from `maneuvers granted:crusader`) with the remainder `Withheld`.
   - `StanceSlot` tracks `StanceName` and `bool IsActive`. `ContributeEffects` adds all active stances to `EffectPool`.

4. **Dedicated Class Recovery Routines**:
   - The engine provides explicit tabletop recovery operations: `RecoverWarblade(source)`, `RecoverSwordsage(source, slotIndex)`, `AdvanceCrusaderTurn(source)`, and `ResetEncounter()`.
   - The `"Feats"` source provides no in-combat recovery routine and replenishes only upon `ResetEncounter()`.

5. **Transient Timeline Execution (`ManeuverSheetAction`)**:
   - Initiating turn-length Boosts and Counters schedules a `ManeuverSheetAction` on the `Timeline`.
   - The action injects temporary modifiers into `EffectPool` during its active duration and un-applies them automatically upon expiration, leaving the maneuver slot in the `Expended` state.

6. **Unified Catalog & Categorization (`ManeuverType`)**:
   - All maneuvers and stances load through `IRulesCatalog` from a single `PlayMore35/Resources/maneuvers.json`.
   - Entries are categorized by `ManeuverType { Strike, Boost, Counter, Stance, Other }`, capturing standard martial disciplines and special maneuvers without an explicit type tag.

## Considered Options

- **Separate Maneuver and Stance Engines**: Rejected under Deepen Before Splitting. Stances share disciplines, prerequisites, and initiator level with maneuvers.
- **Second `SubsystemSlotGroup` for Readied Maneuvers**: Rejected. Maneuvers known, readied, and granted are isomorphic set inclusions; tracking readied status as a state on the known slot guarantees uniqueness by construction and eliminates synchronization complexity.
- **Boolean Flags (`IsReadied`, `IsExpended`, `IsGranted`)**: Rejected in favor of the 4-state enum `ManeuverState`, eliminating illegal states and unifying the initiation check (`State == Available`).
- **Class-Locked Partitions**: Rejected in favor of unified `Sources` accommodating both adept classes and non-adept feat grants (`"Feats"`).

## Consequences
- Integrates seamlessly with branch 122 prefactorings (`SubsystemSlotGroup`, `ICharacterContext.GetStatValue`, `SubsystemRegistry.CreateDefault()`).
- Upholds Enable, Don't Enforce (ADR-0001) by presenting advisory Warning Badges for level caps, missing discipline counts, and cross-class duplicate selections.
- Guarantees schema stability and explicit constructor deserialization (ADR-0008).
