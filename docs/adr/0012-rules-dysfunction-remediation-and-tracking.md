---
status: accepted
---

# Rules Dysfunction Remediation and Tracking

## Context
D&D 3.5e rules-as-written (RAW) occasionally contain mechanical contradictions or unplayable edge cases ("rules dysfunctions"), such as *Martial Study* granting an Initiator Level of $\lfloor\text{Character Level} / 2\rfloor$, which evaluates to 0 for a 1st-level character and prevents them from selecting or initiating any 1st-level maneuver granted by the feat. Resolving dysfunctions requires explicit provenance distinguishing official text from human-approved corrections, while preserving future toggleability for table-specific house rules.

## Decision
1. **Explicit Rules Dysfunction Documentation**:
   - Rules dysfunctions and their human-approved corrections are documented inline directly beside the rules citations they modify in `docs/rules/`, explicitly marked as human-approved corrections.
2. **Metadata Tagging**:
   - Catalog modifiers that implement a dysfunction fix are tagged with metadata `"dysfunction": "<id>"` (e.g. `"dysfunction": "martial-study-minimum-il"`).
3. **Default Activation & Overlapping**:
   - The runtime applies human-approved corrections by default. Where multiple grants adjust the same underlying floor (such as *Martial Study* and *Martial Stance* each setting a minimum Initiator Level of 1), modifiers use non-stacking modifier type `internal` (`ModifierTypes.INTERNAL`).
   - House rule toggle machinery will read `"dysfunction"` metadata when configured without introducing speculative runtime branching prematurely.

## Consequences
- Maintains transparent rules provenance between RAW sourcebook text and human-approved corrections.
- Lays the architectural groundwork for a future per-dysfunction house rule toggle system without speculative runtime branching.
