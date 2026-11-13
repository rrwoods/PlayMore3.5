---
status: accepted
---

# Single-Anchor Round Zero and Pre-1900 Timeline Dates

## Context

PlayMore35 tracks fictional campaign progression via `Timeline` (`RoundZero` + `CurrentRound`) and dual-timestamped GP/XP ledgers ([ADR-0009](0009-dual-timestamped-gp-and-xp-ledgers.md)). Three limitations prevented setting and running campaigns in historical or fantasy-numbered pre-1900 years:

1. **Win32 Control Floor (`1753-01-01`)**: `.NET` `System.DateTime` natively supports proleptic Gregorian dates across Years `0001–9999`, but `PickTime` relied on WinForms `DateTimePicker` (wrapping Win32 `SysDateTimePick32`), which throws `ArgumentOutOfRangeException` on any date prior to `1753-01-01`.
2. **Conflation of Advancing Play Time and Setting the Campaign Clock**: `Timeline` initialized `RoundZero` to `DateTime.Now` and only exposed forward round advancement (`SetTime`, which computed a round delta and called `Advance`). Players had no UI mechanism to set a campaign's calendar setting without hand-editing JSON or triggering duration expirations and periodic resets.
3. **Stale Derived `DateTime` Copies**: Persisting absolute `DateTime` strings for `GpTransaction.GameTime` (`"game_time"`), `XpSessionEntry.GameTime` (`"game_time"`), and `Timeline.EncounterStartTime` (`"encounter start time"`) duplicated `RoundZero` across historical records. Adjusting a campaign's setting date after logging starting gold or entering encounters would leave prior timestamps stranded in the wrong century or corrupt existing histories on overshoot corrections.

## Decision

1. **Retain `System.DateTime` (`0001–9999`) with Structured Picker Controls**:
   - In-game calendar dates continue to use `.NET` `System.DateTime` (proleptic Gregorian calendar, Years `1–9999`) with exact 64-bit integer `Ticks` round arithmetic (`6 * TimeSpan.TicksPerSecond` per round).
   - WinForms `DateTimePicker` is replaced in `PickTime` with structured date and 12-hour time controls (`Month`, `Day` clamped to month/leap-year length, `Year` `1–9999`, `Hour`, `Minute`, 6-second `Second`, `AM`/`PM`) and non-fighting validation.
   - `Real-World Date` (`DateOverridePicker`) retains `MonthCalendar`, as real-world tabletop sessions occur in modern years.

2. **Single Persisted In-Game Timestamp Anchor (`RoundZero`)**:
   - `"round zero"` (ISO-8601 `"O"`) on `Timeline` is the sole persisted `DateTime` for in-game time.
   - `GpTransaction` and `XpSessionEntry` replace `DateTime GameTime` (`"game_time"`) with `long GameRound` (`"game_round"`), recording the 64-bit timeline round index (~1.75 trillion years of range) at which the entry occurred and projecting display timestamps via `Timeline.GetTimeAtRound(long round)`. Chronological ledger ordering sorts primarily by `GameRound`, then `RealDate`.
   - `Timeline.EncounterStartTime` becomes a computed projection `GetTimeAtRound(EncounterStartRound)`, removing `"encounter start time"` from `Timeline` JSON.
   - Per [ADR-0008](0008-explicit-constructor-deserialization-and-schema-stability.md), the schema cutover is performed cleanly with zero legacy `"game_time"` fallback shims.

3. **Separate Elapsed Time Advancement from Campaign Setting Adjustment**:
   - **Advancing Play Time (`AdvanceTo`)**: Triggered via `[Advance to...]` out of combat; advances `CurrentRound` forward, expires scheduled `SheetAction`s, and resets periodic parameters.
   - **Campaign Setting Adjustment (`RoundZero` Rebase)**: Triggered out of combat by clicking the timestamp `LinkLabel` on `TimelinePanel` in a dedicated campaign-setting adjustment mode; edits the displayed current campaign date/time by shifting only `RoundZero` (`chosenTime - CurrentRound * 6s`) while leaving `CurrentRound`, `EncounterStartRound`, `scheduledActions`, and all ledger `GameRound` entries untouched.

## Considered Options

- **Custom `InGameTime` Struct or Configurable Fantasy Calendars**: Rejected for now. Proleptic Gregorian Years `1–9999` cover target campaign timelines while preserving `.NET` calendar math and `"round zero"` ISO-8601 serialization without custom calendar maintenance.
- **Keeping `DateTime` on Ledger Entries with an Optional Shift Checkbox on Rebase**: Rejected. Storing relative `long` round counters makes `RoundZero` the single source of truth so all historical ledger entries project accurately whenever the campaign setting date is adjusted, with zero mutation or overshoot-corruption risk.

## Consequences

- Partially supersedes [ADR-0009](0009-dual-timestamped-gp-and-xp-ledgers.md) (`game_round` `long` replaces `game_time` `DateTime` on `GpTransaction` and `XpSessionEntry`).
- Eliminates all Win32 `1753-01-01` UI crashes for in-game dates across Years `1–9999`.
- Adjusting a campaign's setting date/time dynamically updates both the timeline readout and all projected ledger timestamps without disturbing active durations or round history.
