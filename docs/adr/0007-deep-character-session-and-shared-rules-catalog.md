---
status: partially superseded by ADR-0006
---

# Deep Character Session and Shared Rules Catalog Architecture

## Context
In previous iterations, `Controller` operated as a shallow coordinator exposing raw mutable fields (`Effects`, `Character`, `EquippedItems`, item definitions dictionaries). Presentation panels (`DynamicStatPanel`, `AcPanel`, `AttacksPanel`, `TextEffectPanel`) and dialogs bypassed controller methods to mutate internal dictionaries, state parameters, and effect flags directly. Furthermore, `Controller.Reload()` deserialized and re-parsed JSON definition files from disk and resources on every instance initialization, tightly coupling definition objects (`Race`, `CharacterClass`, `Item`, `ExternalEffect`) to individual `Character` instances via stateful `ControllableFlag` and `ControllableParameter` references.

This shallow design broke locality, produced stale-data anomalies across derived statistics, and made it impossible to host multiple characters concurrently within the Encounter Tracker or party dashboards without leaking cross-character state.

## Decision
We replace the shallow coordinator with a deep character session architecture:

1. **Autonomous Runtime Host (`CharacterSession`)**:
   - `CharacterSession` acts as the living runtime host for an active character during play, combining a `Character` state document with the game rules engine.
   - It maintains the core architectural invariant of dynamic recomputation without caching (ADR-0004), ensuring that all stat projections, weight limits, and condition modifiers remain strictly consistent.

2. **Immutable Shared Rules Catalog (`IRulesCatalog`)**:
   - Game definitions (`rules.json`, `races.json`, `classes.json`, `feats.json`, `weapons.json`, `armor.json`, `items.json`) are decoupled from `Character` instances and loaded once into an immutable, thread-safe `IRulesCatalog`.
   - Flag and parameter definitions represent static metadata. Runtime state (`On`, `Value`) is stored exclusively in session-scoped state maps. Multiple `CharacterSession` instances share the same catalog without cross-contamination.

3. **Two Distinct Seams**:
   - **External Seam (`ICharacterSession`)**: Exposes read-only query projections (`StatView`, `ToggleView`, `ParamView`, `AttackRoutine`) and intent-revealing combat operations (`EquipWeapon`, `DonArmor`, `SetFlag`, `SetParameter`, `AdvanceTimeline`, `ClearSlot`) to UI panels and the Encounter Tracker. Raw collections and `EffectPool` are strictly encapsulated.
   - **Internal Seam (`ICharacterContext`)**: Retained as the internal collaborator interface implemented by `CharacterSession` for subsystems (`ISubsystemEngine`, ADR-0006) and timeline expiration (`SheetAction`, ADR-0005) to push effects and clear slots during recalculation.
     > [!WARNING]
     > **Partially Superseded by [ADR-0006](0006-declarative-subsystems-and-primitive-verbs.md)**: Collaborators communicate with the unified concrete `SubsystemEngine` rather than the defunct `ISubsystemEngine`. Polymorphic actions under ADR-0005 were superseded by declarative `SheetAction` primitives.

4. **Transactional Document Mutation Boundary (`session.Mutate(Action<Character>)`)**:
   - Sheet data modifications (biography, assigned ability scores, class levels, skill ranks, feats) execute inside transactional mutation lambdas.
   - Callers mutate character data without requiring dozens of one-line pass-through setter methods on the session interface. When the mutation block exits, `CharacterSession` automatically synchronizes effect pools, recomputes weights, and invalidates derived projections atomically.

## Consequences
- Enforces strict local isolation, enabling the Encounter Tracker to host multiple PC and monster sessions concurrently with zero state leakage.
- Shrinks the public surface of the character engine: UI consumers interact solely with read-only projections and clean command methods.
- Eliminates redundant file I/O and JSON deserialization overhead across sessions by sharing the immutable rules catalog.
- Preserves headless, testable execution without instantiating Windows Forms or mocking internal pool collections.
- Maintains full backward compatibility with existing character serialization formats (including `Odric.json`).