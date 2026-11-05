---
status: accepted
---

# Content-Agnostic Subsystem Engine and Declarative State Capacities

## Context
`SubsystemEngine` and `SlotGroupDefinition` retained direct literacy leaks of Sublime Way martial concepts (`discipline`, `maneuver`, `stance`, `ReadiedCapacityStat`, and `MaxActiveStat`), violating Content Agnosticism (`GLOSSARY.md`) and Zero Class/Subsystem Literacy in C# engine code (`CODING_STANDARDS.md`):
- `CountSameDisciplineKnown` and `ValidateChoice` assumed intra-subsystem prerequisites always partition by the `"discipline"` property.
- `GetAccessibleElements` formatted elements with bracketed discipline prefixes (`$"[{discipline}] {elem.Name}"`) for `CharacterSession.GetAccessibleSubsystemElements`, which had zero production callers across the repository.
- `SlotGroupDefinition` declared bespoke properties `ReadiedCapacityStat` and `MaxActiveStat` to track non-default and active slot counts.
- `ValidateChoice` conflated candidate choice validation with sheet-level state inspection, evaluating readied and active slot counts inside an `if (descriptor == null)` test-induced branch while skipping capacity checks for real progression choices (`descriptor != null`).

## Decision

1. **Declarative Prerequisite Property (`prerequisite_property`)**:
   - Add optional `PrerequisiteProperty` to `SubsystemDefinition` (e.g., `"prerequisite_property": "discipline"` in `maneuvers.json`).
   - Refactor `CountSameDisciplineKnown` in `SubsystemEngine` to generic `CountSamePropertyKnown`. If `PrerequisiteProperty` is null or omitted, prerequisite counts evaluate against all known elements in the subsystem.
   - Format prerequisite warnings using `Definition.PrerequisiteUnit ?? "element"` rather than hardcoding `"maneuver"`.

2. **Deletion of Dead Discovery Seams**:
   - Apply the Falsifiable Deletion Test to `GetAccessibleElements` on `SubsystemEngine` and `GetAccessibleSubsystemElements` on `ICharacterSession` and `CharacterSession`. Delete both methods and their dead test fixtures, removing bracket prefixing and discipline string-inspection entirely.

3. **Structured `StateCapacityDefinition` on Slot Groups**:
   - Replace `ReadiedCapacityStat` and `MaxActiveStat` on `SlotGroupDefinition` with a list of declarative state capacity rules:
     ```json
     "state_capacities": [
       {
         "states": ["available", "expended", "withheld"],
         "stat": "maneuvers readied:{source}",
         "label": "readied"
       }
     ]
     ```
   - For stances, define:
     ```json
     "state_capacities": [
       {
         "states": ["active"],
         "stat": "active stances",
         "label": "active"
       }
     ]
     ```
   - In `SubsystemEngine.Validate()`, evaluate `state_capacities` generically and format warning messages as:
     `$"{src} has {count} {cap.Label} {groupDef.Name}, exceeding maximum of {max}."`

4. **Deepened `ChoiceDescriptor` for Progression Choice Capacity**:
   - Add `SlotGroup` to `ChoiceDescriptor` (e.g., `"slotGroup": "maneuvers"` or `"slotGroup": "stances"` in `feats.json`).
   - In `ValidateChoice`, evaluate candidate choices against the target slot group's `CapacityStat` partitioned by `effectiveSource`.
   - Exclude the slot currently being edited (`context.SlotKey`) from the assigned count so re-selecting or modifying a choice does not self-conflict.
   - Purge the ambient `if (descriptor == null)` readied and active capacity checks from `ValidateChoice`.

## Considered Options

- **Retain `ReadiedCapacityStat` with Sanitized Warning Strings**: Rejected. Preserves subsystem-specific naming on generic engine schema.
- **Dictionary with Sentinel Tokens (`"!default"`) for State Capacities**: Rejected. Structured objects with explicit `states`, `stat`, and `label` are self-documenting and avoid magical strings.
- **Retain `GetAccessibleElements` Returning Bare Element Names**: Rejected under the Falsifiable Deletion Test. The method had zero callers outside of unit tests written for itself.

## Consequences

- Purges all remaining Sublime Way terminology (`discipline`, `maneuver`, `stance`, `readied`) from `SubsystemEngine.cs` and `SubsystemDefinition.cs`.
- Unifies slot group state limits under a single content-blind mechanism (`StateCapacities`).
- Restores a clean boundary between choice validation (`ValidateChoice`) and sheet state validation (`Validate()`).
- Partially supersedes ADR-0006 regarding slot group schema and ADR-0018 regarding capacity warning evaluation in `ValidateChoice`.
