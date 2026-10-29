---
name: distill-rules
description: Distill verbatim primary source text (rules-md/) into structured, mechanics-only reference documentation in docs/rules/.
disable-model-invocation: true
---

# Distill Rules

Distill verified primary source text from the external markdown archive (`rules-md/`) into canonical, pure-mechanics reference documentation in the in-repo rules library (`docs/rules/`), adhering to the Rules Arbitration Invariant, Tabletop Domain Language, and the 300-line file cap.

## Preconditions & Invariants

1. **Source Exclusivity**: Input must originate from the persistent verbatim markdown archive:
   `C:/Users/rickr/Documents/D&D notes/resources/rules-md/<Book Name>/<Chapter or Section>.md`
   (If the section has not yet been extracted from the primary source PDF, run `/extract-sourcebook` first).
2. **Pure Mechanics Invariant**: Rules documentation serves purely as reference facts and schemas. Strictly strip narrative flavor text, atmospheric fiction, and developer commentary. Retain 100% of the operative tabletop rules, formulas, prerequisites, and action economy.
3. **Tabletop Domain Vocabulary**: Write exclusively in the vocabulary of 3.5e sourcebooks and tabletop play. Reserve C# engine types, stat keys, and software abstractions for engine code and resource files.
4. **300-Line Growth Ceiling**: Every markdown file in `docs/rules/` must not exceed 300 lines (enforced by `PlayMore35.Tests.RulesLibraryStructureTests`).
5. **No Checkbox Syntax**: Task-list checklist syntax (`- [ ]`) is retired across `docs/rules/`. Use structured headings and field lists.

---

## Phase 1: Intake & Target Identification

### 1. Identify Source Section
Locate the source markdown file in the external archive:
`C:/Users/rickr/Documents/D&D notes/resources/rules-md/<Book>/<Section>.md`

Confirm the file contains verified heading citations (e.g. `### <Entity Name> (p. <Page>)`).

### 2. Determine Rules Library Destination
Route the output to the appropriate in-repo directory per `docs/agents/rules-template.md`:
- `docs/rules/mechanics/`: Systemic core rules logic (e.g. `combat.md`, `stacking-and-modifiers.md`, `skills.md`).
- `docs/rules/subsystems/`: Dedicated subsystem operational rules (e.g. `maneuvers.md`, `pact-magic.md`).
- `docs/rules/content/`: Discrete entity catalogs mirroring `PlayMore35/Resources/<stem>.json` (e.g. `feats.md` / `feats/`, `vestiges.md` / `vestiges/`, `spells/`).

---

## Phase 2: Schema Resolution & Registration

Inspect `docs/agents/rules-template.md` under `### Rules reference schemas`.

### Branch: Does a canonical reference schema already exist for this entity type?

#### Option A: Canonical Schema Exists
Adopt the established schema directly:
- **Feats**: 4-part schema (`Status: Verified Reference Entry`, `Source`, `Prerequisites`, `Benefit`).
- **Maneuvers / Stances**: 6-field schema (`Source`, `Type`, `Level`, `Initiation Action`, `Prerequisites`, `Mechanics`).
- **Vestiges**: 7-field schema (`Source`, `Level`, `Binding DC`, `Special Requirement`, `Sign`, `Influence`, `Granted Abilities`).
- **General Rules Skeleton**: Compact unarbitrated stub.

#### Option B: Schema Missing (New Entity or Subsystem Type)
If distilling an entity type without an established schema (e.g. soulmelds, invocations, prestige classes, items, spells):
1. **Analyze Source Anatomy**: Identify the invariant mechanical properties across all instances in the primary source text (e.g. action economy, prerequisites, level/grade, saving throws, duration, scaling formulas).
2. **Draft Reference Schema**:
   - Define a concise, structured markdown format.
   - Enforce explicit field names and bullet hierarchies.
   - Isolate multi-part or branching mechanics into structured sub-bullets.
   - Exclude narrative descriptions and fluff.
3. **Align with Maintainer**: If field boundaries or representations are ambiguous, review the proposed schema with the human maintainer.
4. **Register in `docs/agents/rules-template.md`**:
   - Append the new schema definition, field specifications, and a canonical populated example entry under `### Rules reference schemas` in `docs/agents/rules-template.md`.
   - Update `GLOSSARY.md` if new domain terms or concepts were introduced.

---

## Phase 3: Mechanical Distillation

Read the section from `rules-md/`.

### 1. Sizing Strategy
- **Small Batches (1–5 entities)**: Distill and edit directly into the target file in `docs/rules/` using `edit_file` or `write_file`.
- **Large Batches (6–30+ entities)**: Write a temporary Python script in `scratch/` (e.g. `scratch/distill_<topic>.py`) to parse entities from `rules-md/`, strip flavor text, apply the schema, and output the formatted markdown block.

### 2. Distillation Invariants
- **Strip Fluff**: Omit all introductory narrative, physical descriptions, and atmospheric quotes.
- **Folio Page Citation**: Extract the page number from the heading citation (`(p. <Page>)`) and record it in the entry's `- **Source**: *<Sourcebook>*, p. <Page>` field.
- **Deconstruct Branching Benefits**: For complex or multi-part effects, split distinct prerequisites, conditional branches (e.g. adept vs. non-adept), and secondary benefits into individual sub-bullets.
- **Explicit Formulas**: Record mathematical formulas and scaling progressions explicitly (e.g. `1d6 fire damage per effective binder level (maximum 50 feet)`). Never substitute mental shortcuts or informal patterns.
- **Rules Dysfunctions**: If contradictory rules-as-written, impossible prerequisites, or deadlocks are encountered:
  - Add `- **Rules Dysfunction** (<dysfunction:id>): <State the contradiction in RAW>`
  - Add `- **Human-Approved Correction**: <State the table ruling>`
  - If no human ruling has been recorded yet, pause and request human arbitration per ADR-0001.

---

## Phase 4: Integration & Line Cap Gate

1. **Insert / Update Target File**: Write the distilled reference entries into the destination file in `docs/rules/`.
2. **Enforce 300-Line Limit**:
   Run the structural verification test:
   ```powershell
   dotnet test --filter RulesLibraryStructureTests --blame-hang-timeout 15s
   ```
3. **Subgroup Splitting Protocol**:
   If the target file exceeds 300 lines or `RulesLibraryStructureTests` fails:
   - Pause and propose a logical subgrouping with the human maintainer (e.g. splitting `feats.md` into `feats/general.md`, `feats/tactical.md`, or splitting by level/tier).
   - Once agreed, convert `<stem>.md` into a directory `<stem>/` containing the subdivided `.md` files.
   - Re-run `dotnet test --filter RulesLibraryStructureTests` until all files pass.

---

## Phase 5: Verification & Completion Criteria

1. **Clean Scratch Workspace**: Remove any temporary parsing scripts from `scratch/`.
2. **Audit Schema Compliance**: Confirm that:
   - Every entity uses the registered reference schema.
   - Heading-level citations and sourcebook titles match primary sources.
   - No narrative fluff or task-list checklist syntax (`- [ ]`) is present.
   - All files under `docs/rules/` are $\le 300$ lines and pass `RulesLibraryStructureTests`.
3. **Report Summary**: Output:
   - Target file path(s) and line count(s).
   - Number of entities distilled.
   - Any newly registered schemas added to `docs/agents/rules-template.md`.
   - Any rules dysfunctions identified or arbitrated.
