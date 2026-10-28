---
status: partially superseded by ADR-0006
---

# Dynamic Subsystem Level Gating

## Context
Subsystems with tiered elements (such as martial maneuvers, spells, psionic powers, or vestiges) govern progression and non-progression access via formulaic level ceilings (such as $\min(9, \lceil\text{IL} / 2\rceil)$ for Sublime Way maneuvers). Defining these formulas statically across class and feat catalog entries introduces redundancy and leaks subsystem-specific progression math into static resource definitions.

## Decision
1. **Slot Group Declaration & Ownership**:
   - Each slot group in a declarative subsystem configuration declares and owns its maximum element level stat (via `"max_level": { "stat": "<stat_name>", "base": <int> }`).
   - The subsystem engine contributes the maximum stat with a `BASE` modifier to the character's `EffectPool`.
   - Effects from classes, feats, or items can modify the maximum stat. Subsystem-specific progression formulas (such as lookup tables mapping progression stats to maximum element levels) are declared as effects directly on the slot group configuration itself.
   - Note: The imperative projection in `MartialManeuverEngine` (`max maneuver level:<source>`) remains as legacy until retired in #166.
     > [!WARNING]
     > **Superseded by [ADR-0006](0006-declarative-subsystems-and-primitive-verbs.md)**: `MartialManeuverEngine` was deleted; slot groups in `SubsystemEngine` own their maximum element level statistics directly.
2. **Subsystem Level Gating Definition**:
   - A subsystem configuration declares its element-level statistic via `"level_gating": { "element_statistic": "<property_name>" }` (e.g. `"level"`).
   - An element is accessible if its level statistic does not exceed the maximum level of at least one configured slot group.
3. **Catalog Decoupling**:
   - Class and feat catalog entries omit static level gating tables, defining only their direct progression grants (such as class levels or feat benefits). Subsystem progression tables live cohesively within the subsystem resource configuration.

## Consequences
- Encapsulates subsystem progression mathematics inside declarative subsystem slot group definitions.
- Keeps `SubsystemEngine` content-blind while allowing slot groups to dynamically own and compute maximum element levels.
- Guarantees any new class or feat granting progression automatically receives level gating without catalog table duplication.
