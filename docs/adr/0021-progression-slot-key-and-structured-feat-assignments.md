---
status: accepted
---

# Progression Slot Key and Structured Feat Assignments

## Context

Progression slot identity was previously stringly typed across the character session, subsystem engine, and UI dialogs:
- Advancement feats (`"Level 1"`), racial bonus feats (`"Human"`), and class bonus feats (`"Fighter 1"`) were stored as raw string keys in `Character.feats`.
- Multi-choice feats (*Martial Study*, *Weapon Focus*) concatenated composite subkeys with a colon delimiter into a separate dictionary `Character.featChoices` (`"Level 1:class"`, `"Level 1:maneuver"`).
- Gear-granted subsystem slots encoded provenance via magic prefixes (`FeatSlot = "Equipped:" + item.ItemEffect.Name`).
- Subsystem slots held this string to track provenance and exclude the editing slot during capacity validation.

This stringly-typed design forced ad-hoc string parsing (`Substring`, `IndexOf(':')`), fragile orphan cleanup in `CharacterSession.ReconcileFeats()`, and conflicting directional prefix matching (`MatchesFeatSlot` vs `MatchesSlotKey`). Attempting to retain dictionary string keys in JSON storage (`{"Human": "Power Attack"}`) would have forced any value module to implement string parsing (`Parse`) and heuristic guessing of provenance, perpetuating the brittleness.

## Decision

1. **`ProgressionSlotKey` Value Module**:
   - Codified as an immutable value object with four fields:
     - `Kind`: enum `SlotKind { Advancement, Race, Class, Item }`
     - `Source`: string (e.g. `"Advancement"`, `"Human"`, `"Fighter"`, or item name)
     - `Level`: integer representing advancement level (1, 3, 6...), class level (1, 2, 4...), race level (1), or `-1` for item grants
   - **No String Parsing (`Parse`)**: Slot keys contain real character progression state and are never parsed from strings. They instantiate exclusively via typed factory methods (`Advancement(level)`, `Race(name, level)`, `Class(name, classLevel)`, `Item(name)`) or explicit JSON constructor `new ProgressionSlotKey(JObject)`.
   - **Stable Intrinsic Identity**: A class slot key stores its intrinsic class level (e.g. Fighter level 1), not its temporal character acquisition level. The character acquisition level (e.g. character level 2 in a Rogue 1 / Fighter 1 build) is resolved dynamically by `CharacterSession.GetFeatSlotLevel(slotKey)` based on build history, keeping slot identity stable under build reordering.
   - **Exact Equality**: Implements `IEquatable<ProgressionSlotKey>`. Two keys are equal if and only if their `Kind`, `Source` (ordinal case-insensitive), and `Level` match. Sibling choices and composite subkeys on slot keys are eliminated.

2. **Structured `FeatAssignment` and Inlined Choices**:
   - `Character.feats` transitions from `Dictionary<string, string>` to `List<FeatAssignment>`.
   - `FeatAssignment` encapsulates:
     - `ProgressionSlotKey Slot`
     - `string Feat`
     - `Dictionary<string, string> Choices` (inlined choices for the feat, e.g. `{"class": "Warblade", "maneuver": "Burning Blade"}`)
   - `Character.featChoices` is removed entirely. Inlining choices makes orphan choices structurally impossible: clearing or changing a feat inherently clears its choices.
   - Delimiter splitting (`":"`) and directional prefix matching (`MatchesFeatSlot`) are eliminated.

3. **Subsystem Slot Grant Integration**:
   - `SubsystemSlot` replaces `string? FeatSlot` with `ProgressionSlotKey? GrantSlot`, serialized as `"slot"` in JSON.
   - `IsGrantedByEquippedItem` derives directly from `GrantSlot?.Kind == SlotKind.Item`.
   - The magic string prefix `"Equipped:"` is eliminated.
   - In `SubsystemEngine` capacity validation, slot exclusion matching collapses to exact value equality (`s.GrantSlot.Equals(excludeSlot)`).

4. **Clean Cutover Without Backward-Compatibility Shims**:
   - In strict compliance with ADR-0008, no dual-format fallback deserializers or adapter shims are introduced.
   - `Odric.json` and all unit test fixtures are cleanly migrated to the structured array schema:
     ```json
     "feats": [
       {
         "slot": { "kind": "Advancement", "source": "Advancement", "level": 1 },
         "feat": "Improved Binding"
       },
       {
         "slot": { "kind": "Class", "source": "Fighter", "level": 2 },
         "feat": "Weapon Focus",
         "choices": { "weapon": "dagger" }
       }
     ]
     ```

## Consequences

- Replaces ad-hoc string manipulation across `CharacterSession`, `SubsystemEngine`, and `LevelDetailsDialog` with a strongly typed, immutable value module.
- Eliminates orphan choice tracking in `ReconcileFeats()` and composite subkey string splitting.
- Eliminates magic `"Equipped:"` prefix checking in subsystem slot lifecycle and capacity checks.
- Changes `Character.feats` schema from an object dictionary to a structured JSON array, requiring a one-time migration of `Odric.json` and test fixtures.
