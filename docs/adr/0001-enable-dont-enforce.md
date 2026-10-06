# Enable, Don't Enforce Rule Validity

## Context
Dungeons & Dragons 3.5e features intricate prerequisites, equipment restrictions, and stacking constraints. Traditional digital tools enforce these constraints strictly by prohibiting illegal options. At actual gaming tables, Dungeon Masters frequently exercise fiat to waive prerequisites, allow custom items, or modify subsystem interactions.

## Decision
The engine and user interface will inform the player of invalid choices (such as unmet feat prerequisites or non-proficient equipment) via non-obtrusive advisory indicators, while never prohibiting those choices or blocking character state progression. Mathematical penalties mandated by the rules (such as armor check penalties for non-proficiency) are calculated accurately.

## Consequences
- The application supports house rules, homebrew, and table fiat seamlessly without requiring complex rule-breaking configuration.
- Build validation logic informs rather than gates player action.
- UI elements must display non-blocking advisory indicators (such as hoverable warning badges) without obstructing primary character information.
