# Dynamic Subsystem Level Gating

## Context
Subsystems with tiered elements (such as martial maneuvers, spells, psionic powers, or vestiges) govern progression and non-progression access via formulaic level ceilings (such as $\min(9, \lceil\text{IL} / 2\rceil)$ for Sublime Way maneuvers). Defining these formulas statically across class and feat catalog entries introduces redundancy and leaks subsystem-specific progression math into static resource definitions.

## Decision
1. **Subsystem Engine Projection**:
   - Subsystem engines dynamically project level gating statistics (`max <element> level:<source>`) via `ISubsystemEngine.ContributeEffects` for each source with an active progression statistic (e.g. `initiator level:<source> > 0`).
2. **Catalog Decoupling**:
   - Class and feat catalog entries omit static level gating tables, defining only their direct progression grants (such as class levels or feat benefits).
3. **Dynamic Activation**:
   - Subsystem engines evaluate active status dynamically by verifying whether any progression source provides an active level (`context.GetStatValue(ProgressionStat(source), 0) > 0`).

Sublime Way martial maneuvers serve as the reference implementation via `MartialManeuverEngine.ContributeEffects` projecting `max maneuver level:<source>` from `initiator level:<source>`.

## Consequences
- Encapsulates subsystem progression mathematics inside their respective subsystem engines.
- Guarantees any new class or feat granting progression automatically receives level gating without catalog table duplication.
