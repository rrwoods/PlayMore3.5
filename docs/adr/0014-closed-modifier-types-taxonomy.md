# Closed Modifier Types Taxonomy and Catalog Auditing

## Context
D&D 3.5e mechanics enforce strict bonus stacking rules determined by modifier types (such as enhancement, morale, insight, or untyped). Undefined or ad-hoc modifier type strings bypass stacking logic, causing unpredictable calculation behavior and silent rules violations.

## Decision
1. **Closed Domain Taxonomy**:
   - Valid modifier types are restricted to a closed taxonomy in `PlayMore35.Effects.ModifierTypes`, comprising official D&D 3.5e bonus types alongside explicit internal engine categories (`internal`, `base`, `untyped`, `ability modifier`).
2. **Strict Catalog Invariant**:
   - Catalog entries across `classes.json`, `feats.json`, and item resources must use verified modifier types from this taxonomy. Ad-hoc strings are prohibited.
3. **Automated Audit Gates**:
   - Catalog integrity is enforced via automated test assertions (`RulesCatalogTests`) executed at build/test time, rejecting invalid modifier types without runtime fallback adapters.
4. **Human Arbitration for New Types**:
   - When a primary source or engine capability presents the need for a new modifier type, always ask the human for approval for the new type. A missing modifier type is an unarbitrated Domain Specification requiring human arbitration.

## Consequences
- Guarantees correct modifier stacking behavior across all character statistics.
- Catches catalog typographic errors and unauthorized bonus types early in test execution.
- Eliminates speculative runtime fallback shims.
