# Dynamic Recomputation Without Caching

## Context
D&D 3.5e statistics feature extensive, interconnected dependencies. An adjustment to a single ability score, toggle, or condition cascades across attack bonuses, armor class, skill checks, carrying capacity, save DCs, and duration timers. In complex rule systems, caching derived values often introduces subtle cache-invalidation bugs, stale data anomalies, and cache synchronization overhead.

## Decision
We deliberately choose not to cache the results of any effects or derived statistics. Whenever character state, equipped gear, toggles, or conditions change, the program dynamically recomputes each derived statistic from the effect pool on demand.

## Consequences
- Completely eliminates cache-invalidation bugs, stale cached values, and cache-synchronization complexity.
- Circular dependencies in the rules/calculation graph (e.g., mutually dependent statistics) still exist and must be prevented or handled during calculation, but they are not compounded by cache invalidation cycles.
- The rules engine remains strictly deterministic: character data stores only inputs and state, while all displayed statistics are fresh functional projections.
- Full recomputation across a character's statistics executes virtually instantaneously on modern hardware, making caching an unnecessary complication for our use cases.
