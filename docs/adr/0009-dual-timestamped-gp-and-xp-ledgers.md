---
status: partially superseded by ADR-0023
---

# Dual-Timestamped GP and XP Ledgers

## Context
GP transactions and session XP logs in PlayMore35 were previously tracked as raw tuples `(decimal gp, string description)` and `(int xp, string description)` without temporal context. Players need to record when wealth changes and XP grants occurred along two distinct temporal dimensions: fictional narrative progression within the campaign world, and real-world session occurrences to correlate with physical tabletop session notes.

## Decision
We standardize on strongly-typed, immutable domain models `GpTransaction` and `XpSessionEntry` implementing `IJsonable` with explicit constructor deserialization per [ADR-0008](0008-explicit-constructor-deserialization-and-schema-stability.md), recording dual timestamps:

1. **In-Game Timeline Time (`game_time`)**:
   > [!WARNING]
   > **Superseded by [ADR-0023](0023-single-anchor-round-zero-and-pre-1900-timeline-dates.md)**: `GpTransaction` and `XpSessionEntry` now persist `long GameRound` (`"game_round"`) rather than an absolute `DateTime` (`"game_time"`), projecting in-game timestamps dynamically from `Timeline.RoundZero`.

   - Sourced from `session.Character.Timeline.CurrentTime` (`DateTime`).
   - Represents the active fictional campaign date and time advancing at round-level resolution.
   - Serialized in ISO-8601 roundtrip (`"O"`) format.
   - Serves as the primary sort key (chronological ascending order).

2. **Real-World Date (`real_date`)**:
   - Represents the calendar date (`yyyy-MM-dd`) of the physical play session.
   - Defaults to the local system date (`DateTime.Today`), with manual date override support in the entry workflow for retroactive downtime and past session record-keeping.
   - Excludes wall-clock time of day, as session-level date resolution is sufficient for real-world correlation.

3. **Domain Model & JSON Contract**:
   - `GpTransaction`: `amount` (`decimal`), `description` (`string`), `game_time` (`DateTime`), `real_date` (`DateTime`).
   - `XpSessionEntry`: `xp` (`int`), `description` (`string`), `game_time` (`DateTime`), `real_date` (`DateTime`).
   - Raw tuples in `Character`, `IReadOnlyCharacter`, and `ReadOnlyCharacter` are replaced by `IReadOnlyList<GpTransaction>` and `IReadOnlyList<XpSessionEntry>`.

4. **Presentation & Interactivity**:
   - Both ledgers are rendered in scrollable tabular layouts matching the clean "raw text on canvas" aesthetic (borderless, matching form background) while providing subtle row hover/selection feedback and right-click deletion context menus.
   - The artificial 100-entry cutoff (`MAX_TRANSACTIONS`) is eliminated in favor of standard vertical scrolling.

## Considered Options

- **Expanded Tuples**: Rejected. 4-tuples `(decimal, string, DateTime, DateTime)` leak raw primitives across interfaces, lack serialization encapsulation, and complicate UI binding.
- **Single Real-World Timestamp with Wall-Clock Seconds**: Rejected. Sub-day precision for real-world time adds visual noise without tabletop value; calendar date resolution with chronological entry ordering suffices for session identification.
- **Strictly Automatic Timestamps Without Overrides**: Rejected. Tabletop players regularly record retroactive transactions during character maintenance between sessions or after narrative downtime.

## Consequences
- Character save schema explicitly incorporates `game_time` and `real_date` properties on `transactions` and `sessions`.
- Campaign timeline state is deeply integrated into character financial and experiential bookkeeping.
- UI presentation retains the minimal text aesthetic while supporting full-history scrolling and interactive selection.
