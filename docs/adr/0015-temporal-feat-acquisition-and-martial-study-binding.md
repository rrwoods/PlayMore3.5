# Temporal Feat Acquisition and Martial Study Binding

## Context
Under D&D 3.5e (*Tome of Battle: The Book of Nine Swords*, pp. 32, 52), acquiring martial maneuvers or stances via non-progression access (*Martial Study* and *Martial Stance*) depends strictly on whether the character possessed martial adept levels at the moment of feat acquisition:
- A character who has martial adept levels when taking *Martial Study* adds the learned maneuver to maneuvers known for one of their martial adept classes (increasing that class's maneuvers known by 1, with readied capacity unchanged, using that class's recovery routine).
- A character who takes *Martial Study* without martial adept levels gains the maneuver under the general feat pool (`"Feats"`, with readied capacity 1 and no recovery method). If they later gain martial adept levels, the previously learned maneuver remains permanently in the `"Feats"` pool and does not gain a recovery method.
- Furthermore, *ToB* p. 32 specifies that a maneuver learned through *Martial Study* cannot be exchanged or retrained during class level-up.
- RAW level-up sequencing (*PHB* pp. 58–59) places skill selection before feat selection, which would strictly prevent the discipline key skill grant from applying to skill points spent at the acquisition level (`dysfunction:skill-feats-same-level`).

PlayMore35 previously evaluated feats and character statistics in aggregate without tracking the acquisition level of individual feat slots, making it impossible to distinguish between these two situations or determine legal adept target classes at feat selection time.

## Decision
We resolve temporal ordering, target class binding, multi-choice persistence, and subsystem projection through the following cohesive architecture:

1. **Deterministic Acquisition Level Derivation**:
   - Rather than altering character serialization, acquisition level $L$ is deterministically derived by deepening `CharacterSession` with `GetFeatSlotLevel(string featSlot)` using the existing slot definitions in `GetFeatSlots()`.
   - Character class progression at that moment is evaluated from `character.classLevels.Take(L)`.

2. **Target Class Selection Rules**:
   - At acquisition level $L$:
     - If no martial adept classes exist in `classLevels.Take(L)`, the target source automatically resolves to `"Feats"` without presenting a redundant selection UI.
     - If exactly one martial adept class exists in `classLevels.Take(L)`, the target source automatically resolves to that class without presenting a redundant selection UI.
     - If two or more martial adept classes exist in `classLevels.Take(L)`, the player is prompted to choose between the eligible adept classes (and `"Feats"` is excluded).

3. **Composite Keys for Multiple Feat Choices**:
   - To preserve schema stability (ADR-0008) and backward compatibility without modifying `Character.cs`, multi-choice feats utilize composite keys within the existing `Dictionary<string, string> featChoices`:
     - `featChoices[$"{featSlot}:class"] = targetClass`
     - `featChoices[$"{featSlot}:maneuver"] = maneuverName`
   - Single-choice feats continue using `featChoices[featSlot]` unmodified.

4. **Data-Driven Modifiers via `ChoiceToggle`**:
   - Content logic remains declared in `feats.json` rather than hardcoded in C# `EffectPool`.
   - We deepen `IToggle` with `ChoiceToggle` (evaluating against the effect's parameter/choice, e.g. `{"choice": "Feats"}`) and compose with `UnToggle` for negation (`{"not": {"choice": "Feats"}}`).
   - In `feats.json`, `maneuvers known:$` grants $+1$ to `maneuvers known:<targetClass>`. The non-adept modifiers (`maneuvers readied:feats` and `initiator level:feats`) are conditioned on `{"choice": "Feats"}`, activating only when the target is `"Feats"`.

5. **Locked Slot Projection in Subsystem Engine**:
   - When a feat-sourced maneuver is chosen in `featChoices`, `MartialManeuverEngine` projects it directly into `Known[targetClass]` as a dedicated known slot.
   - The slot is marked as feat-bound and locked against retraining, upholding *ToB* p. 32 by construction and eliminating duplicate maneuver selection across dialogs.

6. **Dictionary-Based Property Path Resolution (`IPropertyProvider`) & Class Skill Grant (`dysfunction:skill-feats-same-level`)**:
   - Subsystem entities implement a lightweight `IPropertyProvider` interface exposing an `IReadOnlyDictionary<string, object> Properties { get; }` (using case-insensitive keys).
   - In `Maneuver`, `Properties["discipline"]` references the corresponding `MartialDiscipline` entity, which in turn exposes `Properties["keySkill"] = "Balance"`.
   - `feats.json` declares `"class skills": [ "${maneuver.discipline.keySkill}" ]`.
   - `IRulesCatalog.ResolveTemplate(template, choices)` evaluates path expressions without complex expression parsing:
     - The root choice `x` resolves via `choices[x]` (from `IReadOnlyDictionary<string, object> choices`). If no sub-properties exist (e.g. `${class}`), it directly formats the value with `.ToString()`.
     - For nested paths (`"${x.y.z}"`), `choices[x]` provides the pre-resolved root entity (e.g. `SubsystemElement` or any `IPropertyProvider`) without type guesswork, and template resolution walks the `Properties` dictionaries (`x.Properties["y"]` $\to$ `y.Properties["z"]`). If any choice is unset or key missing, it safely returns `""`.
   - `EffectPool` tracks granted class skills. `CharacterSession.IsClassSkill(string skill, int levelIndex)` checks both class definitions and granted skills active at that level.
   - Under human-approved correction `dysfunction:skill-feats-same-level`, the resolved key skill becomes a class skill starting at the feat's acquisition level (`levelIndex >= featAcquisitionLevel - 1`) and persists for all subsequent levels.

7. **Sublime Way Symmetry**:
   - *Martial Stance* follows the identical temporal binding, composite key persistence (`$"{featSlot}:stance"`), target source resolution, and `Stances[targetClass]` projection rules as *Martial Study*.

## Considered Options
- **Complex Expression Parser / Reflection**: Rejected. Inventing a string-to-property expression parser or using reflection adds unnecessary complexity and brittle type conversions. Having subsystem entities expose queryable `Properties` dictionaries allows property paths (`"${x.y.z}"`) to resolve through simple, uniform dictionary key lookups.
- **Procedural Subsystem Lookup in `CharacterSession`**: Rejected under Reversal Accounting. Having `CharacterSession` inspect `catalog.GetManeuver()` to extract the discipline and key skill leaked Sublime Way subsystem knowledge into the core character runtime. Declarative property paths (`"${maneuver.discipline.keySkill}"`) keep `CharacterSession` agnostic and drive resolution from `feats.json`.
- **Unified Progression / Level Ledger (`List<CharacterLevel>`)**: Rejected under *Deepen Before Splitting* and ADR-0008. Breaking existing character persistence schemas was unnecessary because `classLevels` and `GetFeatSlots()` already provide complete temporal ordering.
- **Hardcoding Content Branching in `EffectPool.cs`**: Rejected under Reversal Accounting. Putting custom feat logic in C# breaks the data-driven architecture and tightly couples the engine to specific sourcebook content.
- **Delimited Compound String (`"<class>|<maneuver>"`)**: Rejected in favor of composite keys (`$"{featSlot}:class"` and `$"{featSlot}:maneuver"`), which avoids brittle string parsing/serialization while remaining 100% backward-compatible with `Dictionary<string, string>`.

## Consequences
- Maintains absolute schema stability (ADR-0008) for character saves.
- Keeps game content rules entirely data-driven in `feats.json` via the reusable `ChoiceToggle`.
- Guarantees *Tome of Battle* rules fidelity for non-adepts, multiclass adepts, and late-adept progressions.
- Prepares clean seams for advisory Warning Badges under *Enable, Don't Enforce* (ADR-0001).
