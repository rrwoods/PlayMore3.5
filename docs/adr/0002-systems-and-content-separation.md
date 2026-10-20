# Separation of Systems and Content

## Context
Dungeons & Dragons 3.5e comprises immense breadth across dozens of subsystems and hundreds of sourcebooks. Systems govern character creation, progression, calculations, and gameplay mechanics (e.g., feats, skills, class levels, attacking, saving throws, spellcasting, Sublime Way maneuvers). Content encompasses the thousands of specific options cataloged within those systems (e.g., *Power Attack*, *Martial Study*, *Warblade*, *Fireball*).

When code or system configuration files directly reference specific content names or IDs, the engine rapidly accumulates special cases, hardcoded branch logic, and tight coupling across sourcebooks. PlayMore35 requires a strict architectural invariant that separates systems from content, keeping all code and systemic files content-agnostic while enabling content to drive rich, recurring mechanical archetypes through generalized primitives.

## Decision
We establish the separation of systems and content as a foundational architectural invariant:

1. **Zero Content Literacy Across Systems**:
   - System code—including core engine modules (`PlayMore35/Global/`, `PlayMore35/Effects/`), subsystems (`PlayMore35/Subsystems/`), and UI presentation dialogs (`PlayMore35/UI/`)—must remain strictly ignorant of specific content names, IDs, or strings (e.g., no `"Martial Study"`, `"Warblade"`, `"Power Attack"`).
   - Systems evaluate and present content purely through generic data contracts and schema metadata.
   - Integration unit tests alone may reference specific content names to verify that systems load and execute content properly.

2. **The Generalized Primitive Rule**:
   - In D&D 3.5e, mechanical patterns are recurring archetypes rather than one-offs. What initially appears unique to a single piece of content (such as a feat requiring a class selection or granting a cross-subsystem capability) is an instance of a mechanical pattern shared across dozens of feats, classes, or spells in other sourcebooks.
   - When a unit of content requires a mechanic not yet supported by current systems, the system must be deepened with a generalized, reusable primitive (e.g., generic class choice descriptors, cross-subsystem grant specs) rather than an ad-hoc content branch. The content record only supplies parameters.

3. **File Taxonomy (Systems Files vs. Content Files)**:
   - **Systems Files**: `rules.json` defines systemic statistics (the 6 ability scores, 3 saves, HP, AC, BAB, death threshold), formulas, modifier types, and global rule parameters. It contains zero references to specific content. Documentation in `docs/rules/mechanics/` and `docs/rules/subsystems/` similarly documents systemic rules.
   - **Content Files**: `feats.json`, `classes.json`, `races.json`, `skills.json`, `maneuvers.json`, `spells.json`, `weapons.json`, `armor.json`, `items.json`, and `vestiges.json` represent content catalogs that parameterize systems. Documentation in `docs/rules/content/` provides reference entries for content units.

4. **Declarative Choice Schemas**:
   - Content requiring player choices (such as choosing a class, weapon, skill, or subsystem element) declares structured choice descriptors in schema (e.g., specifying identifier, prompt label, domain source, and eligibility criteria).
   - UI presentation dialogs render pickers dynamically from choice metadata. `CharacterSession` persists selections generically via composite keys (`$"{slot}:{choiceId}"`).

5. **Cross-System Interoperability via Generic Seams**:
   - When content bridges multiple systems (e.g., a feat granting a maneuver and a class skill), systems communicate through generic interfaces (`ISubsystemEngine`, `ICharacterContext`, `IPropertyProvider`) and declarative property path templates (e.g., `"${maneuver.discipline.keySkill}"`). Neither system couples to the other's content entities.

6. **Systemic Rules as First-Class Statistics (Absorbed Decision)**:
   - Global game rules and thresholds (such as character death at -10 HP or massive damage thresholds) are modeled as first-class `Statistic` objects within `rules.json`. House rules adjust these thresholds using standard `Modifier` and `Effect` primitives, completely eliminating hardcoded C# constants while keeping `rules.json` strictly a systems file.

## Considered Options
- **Hardcoded Content Branching (`if (featName == "...")`)**: Rejected. Leads to exponential special-case sprawl, fragile code, and tight coupling to specific sourcebook content.
- **Content-Aware UI Dialogs**: Rejected. Presentation dialogs are part of the system layer and must drive UI controls dynamically from schema descriptors rather than hardcoding content IDs.
- **Scripting Engine / Expression Hooks**: Deferred. Declarative property paths and typed choice descriptors provide necessary power without security, performance, or debugging overhead.

## Consequences
- Eliminates special-case explosion across engine, subsystem, and UI presentation code.
- Ensures new sourcebook content can be introduced purely through JSON data whenever the underlying systemic primitive already exists.
- Clarifies the role of `rules.json` as a systems file, distinct from content catalogs.
