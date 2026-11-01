---
name: distill-rules
description: Distill verbatim primary source text (rules-md/) into structured, mechanics-only reference documentation in docs/rules/ via an interactive grilling session.
disable-model-invocation: true
---

# Distill Rules

Distill verified primary source text from the external markdown archive (`rules-md/`) into canonical, pure-mechanics reference documentation in the in-repo rules library (`docs/rules/`), using an interactive `/grilling` session to settle structural and content judgment calls, while adhering to the Rules Arbitration Invariant, Tabletop Domain Language, and the 300-line file cap.

## Preconditions & Invariants

1. **Source Exclusivity & Direct Read Access**: Input must originate from the persistent verbatim markdown archive:
   `C:/Users/rickr/Documents/D&D notes/resources/rules-md/<Book Name>/<Chapter or Section>.md`
   (If the section has not yet been extracted from the primary source PDF, run `/extract-sourcebook` first). Read archive files directly using `read_file` without intermediate copy scripts or sandbox bypass.
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

## Phase 2: Interactive Shaping & Grilling

Call the Skill tool with "grilling" to walk the design tree with the user before synthesizing documentation:
- **Destination & Partitioning**: Path under `docs/rules/` (`mechanics/`, `subsystems/`, or `content/`), and proactive subgrouping if projected to exceed the 300-line ceiling.
- **Schema Selection & Design**: Confirming established schemas (`Feats`, `Maneuvers`, `Vestiges`, or `General Rules Skeleton`) or negotiating a draft schema for novel entity types.
- **Content Scope & Fluff Pruning**: Boundaries between actionable tabletop mechanics and narrative fluff for descriptive prose, atmospheric sidebars, or table guidance.
- **Rules Dysfunctions & Table Rulings**: Contradictions in RAW or impossible prerequisites requiring human arbitration per ADR-0001.

Once the interview completes:
- Register any newly approved schema in `docs/agents/rules-template.md` under `### Rules reference schemas`.
- Update `GLOSSARY.md` if new domain terms or concepts were introduced.

---

## Phase 3: Mechanical Distillation

Read the section directly from `rules-md/` using `read_file`.

### 1. Sizing Strategy
- **Small to Medium Batches (1–15 entities)**: Inspect the archive markdown directly in-context via `read_file` and author/edit the distilled reference entries into `docs/rules/` using `edit_file` or `write_file`.
- **Large Batches (16–30+ entities)**: If automating extraction of high-volume catalogs via a temporary Python script in `scratch/` (e.g. `scratch/distill_<topic>.py`), invoke the script with `BypassSandbox: true` to access `C:\Users\rickr\Envs\dnd_pdf\Scripts\python.exe`. Have the script parse entities from `rules-md/`, strip flavor text, apply the schema, and output the formatted markdown block.

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
   If the target file unexpectedly exceeds 300 lines or `RulesLibraryStructureTests` fails:
   - Call the Skill tool with "grilling" to propose logical subgrouping options to the maintainer (e.g. splitting `feats.md` into `feats/general.md`, `feats/tactical.md`, or splitting by level/tier).
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
