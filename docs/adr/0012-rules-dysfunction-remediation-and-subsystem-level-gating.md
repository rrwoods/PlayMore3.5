# Rules Dysfunction Remediation and Subsystem Level Gating

## Context
D&D 3.5e rules-as-written (RAW) occasionally contain mechanical contradictions or unplayable edge cases ("rules dysfunctions"). Specifically, *Martial Study* grants an Initiator Level of $\lfloor\text{Character Level} / 2\rfloor$, which evaluates to 0 for a 1st-level character, preventing them from selecting or initiating any 1st-level maneuver granted by the feat. Furthermore, the universal formula for maximum maneuver level access across all martial adept classes and feat grants ($\min(9, \lceil\text{IL} / 2\rceil)$) was previously copy-pasted as duplicate lookup tables across catalog entries (`classes.json` and `feats.json`), using non-existent modifier types such as `"feat"`.

## Decision
1. **Explicit Rules Dysfunction Tracking**:
   - Rules dysfunctions and their human-approved corrections are documented inline directly beside the rules citations they modify in `docs/rules/`, explicitly marked as human-approved corrections.
   - Catalog modifiers that implement a dysfunction fix are tagged with metadata `"dysfunction": "<id>"` (e.g. `"dysfunction": "martial-study-minimum-il"`), allowing future house rule toggles to individually enable or disable each correction.
   - *Martial Study* and *Martial Stance* leverage `LargestAmount(1, floor(level * 0.5))` with modifier type `ModifierTypes.INTERNAL` to guarantee an Initiator Level of at least 1, preventing double-stacking when both feats are taken.

2. **Subsystem-Generated Level Gating (`max maneuver level`)**:
   - `max maneuver level:<source>` is completely removed from `classes.json` and `feats.json`.
   - `MartialManeuverEngine.ContributeEffects` dynamically generates `max maneuver level:<source>` for each source with an active initiator level using an `Effect` with a shared `LookupAmount` on `initiator level:<source>`.
   - The engine's `IsActive` check evaluates strictly whether `context.GetStatValue(InitiatorLevelStat(source), 0) > 0` for any source, requiring a non-empty `source` parameter on `InitiatorLevelStat`.

3. **Strict Modifier Types Delineation**:
   - Valid modifier types are isolated into `PlayMore35.Effects.ModifierTypes`. Ad-hoc strings such as `"feat"` are prohibited and enforced by automated catalog audit tests.

## Consequences
- 1st-level characters taking *Martial Study* can legally initiate 1st-level maneuvers without disabling the subsystem.
- Removes four identical lookup tables from resource catalogs and guarantees any future martial initiator class or feat automatically receives level gating.
- Lays the architectural groundwork for a future per-dysfunction house rule toggle system without introducing speculative runtime branching prematurely.
