# Headless Timeline and Sheet Actions

## Context
In previous iterations, tracking time progression (`Rounds`) and executing timed duration reversions (`SheetAction`) were directly coupled to the Windows Forms graphical interface (`MainForm`). Action expiration and round ticks called UI refresh and reload methods directly, requiring `Character` to take `MainForm` in its constructor. This prevented headless automated testing (leading to the deletion of unit tests) and made it impossible to run multiple character timelines concurrently within an Encounter Tracker. An earlier attempt using an observer pattern (`RoundObserver`) had been discarded due to polymorphic serialization complexity and conflating view repaints with domain state scheduling.

## Decision
We decouple the `Timeline` and `SheetAction` engine completely from the presentation layer:
1. `Timeline` operates as a headless domain module managing combat rounds, timestamps, cadence threshold resets, and scheduled action queues.
2. `Timeline.Advance()`, `StartCombat()`, and `EndCombat()` return a deterministic `TimelineAdvanceResult` indicating whether state changed and the degree of recalculation required (`TimelineRefreshRequirement`: `None`, `RecomputeDerived`, or `FullReload`).
3. `SheetAction.Perform()` executes against an `ICharacterContext` seam without reference to UI classes.
4. `SheetAction` uses generic slot clearance (`ClearSlot(subsystem, slotIndex)`), with transparent backward-compatible deserialization for legacy `"unbind"` vestige data.

## Consequences
- Enables automated headless unit testing for time advancement, duration expiration, and condition reversion without instantiating Windows Forms.
- Preserves the deterministic, cache-free recalculation model (ADR-0004) while eliminating memory leaks from lingering UI event subscriptions.
- Maintains dual-mode operation: solo characters retain their own serialized timeline state, while the DM Encounter Tracker can advance multiple character timelines against a synchronized clock.
- Ensures full backward compatibility with existing character JSON files.
