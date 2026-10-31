---
status: accepted
---

# Level Snapshots and Item-Dependent Prerequisites

## Context
Under D&D 3.5e, character progression choices (such as feats, skills, and subsystem elements like martial maneuvers or spells) are bound to the acquisition level at which they were selected. Gaining subsequent levels, higher base attack bonus, or higher initiator levels does not retroactively permit higher-tier selections for earlier-level choices (e.g. choosing *Martial Study* at level 1 cannot select a 2nd- or 3rd-level maneuver even if the character is currently level 10).

Furthermore, while 3.5e rules (*DMG* p. 214, *Complete Warrior* p. 16) allow ability score enhancements from magic items to qualify a character for feats and prestige classes, PlayMore35 is an active **Live Companion** (ADR-0001, ADR-0004) that does not track historical equipment inventories per level. Continuously recomputing historical character states across every combat round tick, timeline advance, or general recalculation would severely degrade combat dashboard responsiveness.

## Decision

1. **Evaluative Level Snapshot (`ICharacterSession`)**:
   - `CharacterSession` provides `GetSessionAtLevel(int level, bool includeEquipped = false)`, constructing an evaluative `CharacterSession` from a sliced projection of the character's progression.
   - The slice includes data up to acquisition level $L$:
     - Class progression: `classLevels.Take(L)`
     - Natural ability scores: base assignments + racial traits
     - Ability score increments: `abilityScoreIncrements.Where(kvp => kvp.Key <= L)`
     - Hit dice: `rolledHitDice.Take(L)`
     - Skill ranks: `skillRanks.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Take(L))`
     - Feats and choices: all slots where `GetFeatSlotLevel(slot) <= L`
   - Under *Deepen Before Splitting*, the snapshot session reuses 100% of the existing rules engine, modifier stacking, and subsystem level-gating without parallel calculation models.

2. **Intra-Level Co-Visibility**:
   - All feats acquired at level $L$ are co-visible to each other regardless of slot order, matching 3.5e level-up rules where choices at the same level can be made in any sequence (e.g. *Point Blank Shot* in `Human` and *Precise Shot* in `Fighter 1` mutually satisfy prerequisites).

3. **Calling Boundaries and Lazy Mutation Invalidation**:
   - Level Snapshot creation is strictly excluded from live combat updates, timeline advances, full `CharacterSession.Recalculate()`, and batch rendering of Character tab level rows (`DisplayLevels()`).
   - Sliced sessions are cached lazily per level (`_levelSessionCache`) on `CharacterSession`. The cache is cleared and marked dirty whenever any build choice or equipped item mutates in `Character`.

4. **Dual-Pass Prerequisite Validation & Two-State Warning Badges**:
   - Instead of tracking historical equipment ledgers, the snapshot evaluates an intrinsic baseline (`includeEquipped = false`, excluding all gear, inventory, combat toggles, and external effects).
   - If prerequisite warnings occur, validation evaluates the check against the snapshot with currently equipped items applied (`includeEquipped = true`).
   - Warnings are categorized via a structured `ValidationWarning` model with an extensible `WarningKind`:
     - **Completely Unmet (`WarningKind.Unmet`, `⚠`, `Color.DarkGoldenrod`)**: Unmet intrinsically at level $L$ and still unmet when applying current equipped gear to the level $L$ snapshot (or structurally invalid).
     - **Item-Dependent (`WarningKind.ItemDependent`, `🛈`, `Color.DodgerBlue`)**: Unmet intrinsically in the level $L$ snapshot, but satisfied by applying current equipped items to that level $L$ snapshot (without leaking subsequent character levels).
   - **Level Gating Invariant**: Subsystem element tier gating (e.g. maximum maneuver level based on initiator level) is strictly intrinsic to character level at acquisition time and can never qualify for the item-dependent badge.

5. **Advisory "Enable, Don't Enforce" (ADR-0001)**:
   - Warning badges remain non-blocking visual indicators. Table fiat is preserved by allowing players to select or retain choices regardless of badge status.
   - Subsystem element pickers default to filtering by the acquisition level's gate while providing a toggle to "Show Ineligible Elements" with advisory badges.

6. **Centralized Engine Seam**:
   - Orchestration is centralized on `ICharacterSession`: `ValidateFeatChoice(...)` automatically resolves acquisition level and conducts two-pass snapshot evaluation.
   - No custom logic is added to `LevelDetailsDialog`, preserving clean engine seams ahead of its planned replacement.

## Considered Options

- **Historical Item Ledger (`AcquisitionLevel` on items)**: Rejected. Tracking acquisition levels or purchase dates on every inventory item adds massive data-entry friction and schema complexity for an in-combat companion.
- **Validating Against the Live Character Session**: Rejected. Testing item reliance directly against the live session erroneously leaks subsequent character levels (such as higher BAB, class abilities, or later ability increments) into early-level prerequisite checks.
- **Dedicated Snapshot DTO / Pipeline**: Rejected under *Deepen Before Splitting*. Slicing the `Character` POCO and reusing `CharacterSession` guarantees historical evaluation and live engine math never diverge.
- **Sequential Slot Dependency at Level $L$**: Rejected. 3.5e treats all choices at the same level as simultaneous, so slot order should not artificially block valid combinations.

## Consequences

- Solves temporal prerequisite checking and level gating across all subsystems without degrading live combat performance.
- Accurately identifies item-reliant qualifications while preventing level leakage from later progression.
- Keeps character persistence schema completely stable (ADR-0008).
- Prepares a clean, high-level API on `ICharacterSession` for upcoming build UI designs.
