---
status: accepted
---

# Dynamic Recomputation Without Caching

## Context
D&D 3.5e statistics feature extensive, interconnected dependencies. An adjustment to a single ability score, toggle, or condition cascades across attack bonuses, armor class, skill checks, carrying capacity, save DCs, and duration timers. In complex rule systems, caching derived values often introduces subtle cache-invalidation bugs, stale data anomalies, and cache synchronization overhead.

## Decision
We deliberately choose not to cache the results of any effects or derived statistics, nor do we mutate `EffectPool` incrementally at runtime:
1. Whenever character state, equipped gear, toggles, or conditions change, `CharacterSession.Recalculate()` rebuilds the `EffectPool` from scratch from the catalog rules, character choices, and active items, and dynamically recomputes each derived statistic from the pool on demand.
2. Runtime state lives exclusively in `Controllables` (boolean flags and integer parameters) and resource pools. Timed actions and reversions (`SheetAction`) never add or remove `Effect` instances imperatively; instead, they mutate controllable flags or parameters (`"set"`, `"clear"`, `"adjust"`), which gate or scale the declarative effects in the freshly rebuilt `EffectPool`.

## Consequences
- Completely eliminates cache-invalidation bugs, stale cached values, and cache-synchronization complexity.
- Eliminates the need for imperative runtime effect removal or unique effect-instance identifiers: applying, stacking, or expiring a temporary condition is simply a mutation to `Controllables` followed by recalculation.
- Circular dependencies in the rules/calculation graph (e.g., mutually dependent statistics) still exist and must be prevented or handled during calculation, but they are not compounded by cache invalidation cycles.
- The rules engine remains strictly deterministic: character data stores only inputs and controllable state, while the effect pool and all displayed statistics are fresh functional projections.
- Full recomputation across a character's statistics executes virtually instantaneously on modern hardware, making caching an unnecessary complication for our use cases.
