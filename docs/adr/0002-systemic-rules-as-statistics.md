# Systemic Game Rules As Statistics

## Context
D&D 3.5e includes global thresholds and systemic rules, such as character death at -10 HP or massive damage thresholds. Tables frequently modify these rules (for example, death at `-CON score` instead of -10). The engine needed a consistent way to represent these systemic rules without hardcoding constants into the source code or introducing a separate configuration engine.

## Decision
Systemic game rules and thresholds are modeled as first-class `Statistic` objects within `rules.json`. House rules modify these thresholds using the existing `Modifier`, `Effect`, and `Alterer` mechanisms.

## Consequences
- The calculation engine, modifier stacking, and mathematical explanation tooltips apply to global rules identically to character ability scores and saving throws.
- House rules can override or adjust rules dynamically without modifying core application code.
- Tooltips can display the full provenance and formula for house-ruled thresholds (such as showing `-14 (–CON score)`).
