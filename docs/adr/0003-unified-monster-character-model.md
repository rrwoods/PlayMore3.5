---
status: accepted
---

# Unified Monster and Character Representation

## Context
D&D 3.5e monsters share the underlying rules framework of player characters: they possess ability scores, size categories, natural attacks, feats, skill points, and hit dice. DMs frequently advance monsters by adding racial hit dice, templates, or player class levels. Building a separate, simplified data model for monsters would require duplicating leveling, attack generation, and effect calculation code.

## Decision
Monsters are represented using the unified `Character` model. A monster's base physical progression is modeled as Racial Hit Dice (analogous to class levels associated with creature types). Advancing a monster reuses the standard character leveling, feat selection, and ability increment pipeline.

## Consequences
- Eliminates duplicate engine logic for monsters versus player characters.
- Monsters automatically benefit from existing effect pooling, weapon size scaling, stacking rules, and in-combat tracking.
- Preparing monster statblocks requires populating racial hit dice definitions and creature type progressions in `classes.json` / `races.json`.
