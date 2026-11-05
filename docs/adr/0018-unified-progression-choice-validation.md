---
status: accepted
---

# Unified Progression Choice Validation

## Context
Character progression validation on `ICharacterSession` was previously modeled as a feat-slot-specific helper (`ValidateFeatChoice`), requiring all progression choices to masquerade as feat slots. Furthermore, `SubsystemEngine.ValidateChoice` returned untyped `List<string>`, forcing `CharacterSession` to loop through all engines with `engine.IsElementWithinLevelGate(...)` to reverse-engineer whether an unmet warning string represented an intrinsic element level gate versus an item-dependent prerequisite.

In addition, 3.5e level advancement (*Player's Handbook* p. 58, *Complete Warrior* p. 16) treats class entry differently from feats and subsystem elements: class selection occurs in Step 1 of advancement, requiring qualifications to be satisfied prior to gaining that level ($L - 1$, or 0-level base state for $L = 1$), whereas feats and subsystem elements gained at level $L$ evaluate co-visibly with level $L$ benefits and stat increments.

## Decision

1. **Unified Progression Choice Context & Seam (`ICharacterSession`)**:
   - Introduce `ProgressionChoiceContext` and expose `IReadOnlyList<ValidationWarning> ValidateProgressionChoice(ProgressionChoiceContext context)` on `ICharacterSession`.
   - Strongly type domains using the `ProgressionChoiceDomain` enum (`Feat`, `Subsystem`, `Class`).
   - Support both direct `AcquisitionLevel` and automatic fallback resolution from `SlotKey` via `session.GetFeatSlotLevel(slotKey)`.

2. **Structured Warning Taxonomy on `SubsystemEngine`**:
   > **Partially Superseded by [ADR-0019](0019-content-agnostic-subsystem-engine-and-declarative-state-capacities.md)**: Ambient readied and active slot state inspection was purged from `ValidateChoice` to `Validate()`. In `ValidateChoice`, `ValidationCategory.Capacity` evaluates candidate choices against `ChoiceDescriptor.SlotGroup` capacity.
   - Under *Deepen Before Splitting*, deepen `ValidationWarning` with a `ValidationCategory` enum:
     - `LevelGate`: Element tier exceeds level gate; strictly intrinsic and never qualifies for item dependence.
     - `Prerequisite`: Discipline, feat, or ability prerequisites; eligible for item dependence under dual-pass validation (*Complete Warrior* p. 16).
     - `Capacity`: Slot group capacity exceeded for the target source, or state capacity exceeded on the character sheet.
     - `SourceConflict`: Element already learned under another initiator source.
     - `Invalid`: Element not found or filtered out.
   - Refactor `SubsystemEngine.ValidateChoice` to return `IReadOnlyList<ValidationWarning>`, eliminating the reverse-engineering loop in `CharacterSession`.

3. **Pre-Advancement Evaluation ($L - 1$) and 0-Level Snapshots**:
   - Deepen `GetSessionAtLevel(int level, bool includeEquipped = false)` to permit `level = 0`. At `level = 0`, the evaluative slice contains base assigned ability scores and racial modifiers with 0 class levels, 0 hit dice, 0 skill ranks, and 0 feats.
   - When validating `ProgressionChoiceDomain.Class` for character level $L$, evaluate against acquisition level $L - 1$ (or $L = 0$ for level 1).
   - When validating `ProgressionChoiceDomain.Feat` or `Subsystem`, evaluate against acquisition level $L$, preserving intra-level co-visibility.

4. **Retirement of `ValidateFeatChoice`**:
   - Retire `ValidateFeatChoice` cleanly from `ICharacterSession` and `CharacterSession`.
   - Update all UI callers (`LevelDetailsDialog`) and test suites to invoke `ValidateProgressionChoice`.

## Considered Options

- **Separate Engine Validation DTO (`SubsystemValidationResult`)**: Rejected under *Deepen Before Splitting*. `ValidationWarning` is already the unified model across session and UI. Adding `ValidationCategory` avoids translation boilerplate and adapter shims.
- **Forwarding Shim for `ValidateFeatChoice`**: Rejected under repo coding standards. Updating callers directly maintains clean seams and zero dead code.
- **Ad-Hoc 0-Level Branching in `ValidateProgressionChoice`**: Rejected. Enabling `GetSessionAtLevel(0)` allows the rules engine to project 0-level state consistently for any future race, racial HD, or initial qualification checks.

## Consequences

- Generalizes progression choice validation across feats, class maneuvers, spells, and class qualification through a single engine seam.
- Eliminates string reverse-engineering, making prerequisite vs level gate distinction explicit and deterministic.
- Accurately enforces 3.5e advancement timing: class qualification evaluates strictly prior to level entry ($L - 1$), while feats and maneuvers evaluate with level $L$ gains co-visible.
- Retains full compatibility with ADR-0001 (Enable, Don't Enforce) and ADR-0017 (Level Snapshots & Item-Dependent Prerequisites).
