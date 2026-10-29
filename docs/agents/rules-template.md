# Rules Template & Rubric

How to specify, arbitrate, and verify D&D 3.5e game rules in PlayMore35.

## The arbitration rule & progressive disclosure library (`docs/rules/`)

Unrecorded D&D 3.5e game rules are **Domain Specifications**, while verified citations recorded in `docs/rules/` are **Environmental Facts** (see `GLOSSARY.md`). Game rules originate exclusively from human-verified primary sources:
- **Primary Source Exclusivity**: Primary source PDFs in the reference library are the sole authoritative source of truth for all game rules and mechanics. When a requested sourcebook PDF is inaccessible, uncalibrated, or missing, immediately stop and surface the access issue to the user. All rules documentation and engine mechanics must derive directly from the primary source text or human arbitration.
- **Model Blindness**: Treat model pretraining as blind to 3.5e mechanics; all rules awareness enters the engine through human arbitration and is persisted in `docs/rules/`.

### Directory layout

Do not bulk-load `docs/rules/` upfront. Query the filesystem for the specific file matching your task:

- `docs/rules/mechanics/`: Systemic 3.5e rules logic (e.g. `combat.md`, `stacking-and-modifiers.md`, `skills.md`).
- `docs/rules/subsystems/`: Dedicated subsystem arbitration documents (e.g. `maneuvers.md`, `pact-magic.md`).
- `docs/rules/content/`: 1:1 mirror of `PlayMore35/Resources/<stem>.json`. Stored as `<stem>.md` (or a `<stem>/` subfolder if split under the 300-line ceiling).

### Rules lookup hierarchy & write-back workflow

When resolving 3.5e game rules, mechanics, or content citations, follow this three-step lookup sequence:

1. **In-Repo Rules Library (`docs/rules/`)**:
   - Query the relevant file in `docs/rules/mechanics/`, `docs/rules/subsystems/`, or `docs/rules/content/`.
   - **Populated citation (Reference Entry)**: If the entry contains a verified sourcebook, page number, and actionable mechanics without placeholder syntax, treat it as an **Environmental Fact** and use it directly.
2. **External Markdown Archive (`rules-md/`)**:
   - If the entry in `docs/rules/` is an unarbitrated Rules Skeleton or absent, inspect the external verbatim markdown mirror:  
     `C:/Users/rickr/Documents/D&D notes/resources/rules-md/<Book Name>/<Chapter or Section>.md` (see `docs/agents/sourcebooks.md`).
   - If the section exists in `rules-md/`, invoke the `/distill-rules` skill to resolve or register the schema, strip narrative fluff, and synthesize the canonical Reference Entry directly into `docs/rules/`. To crosscheck and sanity-check synthesized entries against `rules-md/`, invoke the `/verify-rules` skill. The verbatim markdown mirror serves as the complete point of truth for both distillation and verification.
3. **Primary Source PDF Extraction**:
   - If the section does not yet exist in `rules-md/`, execute the Three-Phase Conversion Procedure documented in `docs/agents/sourcebooks.md`:
     1. Calibrate page offset and detect text layer vs. scanned image.
     2. Run `tools/extract_sourcebook_pages.py` in `dnd_pdf` (`BypassSandbox: true`) to dump cleaned text blocks or render page PNGs to `scratch/`.
     3. Transcribe and author the clean, verbatim markdown file into `rules-md/<Book Name>/<Chapter or Section>.md`.
     4. Synthesize the canonical Reference Entry into `docs/rules/` (and clean up `scratch/`).
   - **Human Arbitration Boundary**: Human arbitration is reserved for rules dysfunctions, contradictory rules-as-written, genuine ambiguities, and table fiat per ADR-0001. When human arbitration provides a ruling or clarification, write both the citation and human-approved correction into `docs/rules/` immediately.

### 300-line growth threshold & splitting protocol

Every `.md` file under `docs/rules/` is capped at **300 lines** (enforced by `RulesLibraryStructureTests` in `PlayMore35.Tests`).
- When recording a citation pushes a file over 300 lines (or `RulesLibraryStructureTests` fails), **pause and initiate a `/grilling` session** with the human maintainer to decide the subgrouping(s).
- Once the subgrouping is agreed upon, replace `<stem>.md` with the directory `<stem>/` containing the agreed sub-files.

### Explicit modifier formulas over induced shortcuts

When an ability modifies an underlying system mechanic via an explicit formula (such as adjusting an effective level, bonus, or progression parameter), execute the full mechanical pipeline for every evaluation. Never replace the rule's explicit calculation with an informal pattern or mental shortcut induced from a few sample cases.

### Rules library authoring style

When writing or updating reference documents in `docs/rules/`:
- **Positive reference entry format**: Author entries as positive, structured reference documentation (**Reference Entries** and **Rules Skeletons** per `GLOSSARY.md` and ADR-0011). State active rules, formulas, and data properties directly, citing the primary sourcebook and page.
- **Tabletop vocabulary domain**: Write `docs/rules/` exclusively in the language of the sourcebooks and tabletop play. Describe how rules operate for characters and players (in-game choices, action economy, eligibility, and official 3.5e bonus types). Reserve software constructs—such as engine stat keys, C# types, and internal engine modifier categories—for resource catalogs and source code.
- **Deconstruct branching benefits**: Structure multi-part or conditional mechanics as dedicated sub-bullets under `- **Benefit**:`, isolating distinct prerequisites, conditions (such as adept vs. non-adept), and secondary outcomes (such as skill grants or recovery limits).
- **Complete operative mechanics**: Detail the full mechanical gameplay rules so the entry is a self-contained rules reference, capturing all operational consequences rather than relying on brief summaries.
- **Paired dysfunction templating**: When documenting rules dysfunctions, provide both the `- **Rules Dysfunction** (<tag>)` field detailing the contradiction in rules-as-written and the `- **Human-Approved Correction**` field detailing the human table ruling.
- **Retirement of task-list checklist syntax**: Task-list checklist syntax (`- [ ]`) is strictly retired across `docs/rules/`. Rules documentation serves purely as reference facts and schemas, not as an in-file task tracker. Use markdown section headings (`### <Name>`) and structured field lists instead of checkbox items.
- **Pure mechanics**: Restrict content to actionable mechanics: data properties, formulas, action types, prerequisite trees, associated skills or weapons, and stacking rules. Omit narrative flavor summaries to keep entries concise and files strictly within the 300-line cap.
- **Subsystem-specific scope**: Only document universal mechanics when a subsystem explicitly modifies or overrides baseline behavior. Rely on canonical core mechanics files (`docs/rules/mechanics/`) for standard action economy and combat rules.

### Rules reference schemas

#### 1. 6-field Maneuver Reference Schema

Martial maneuvers and stances in `docs/rules/content/maneuvers/` follow a dedicated 6-field reference entry:

```markdown
### <Name>
- **Source**: *Tome of Battle*, p. <Page>
- **Type**: <Strike | Boost | Counter | Stance | Other> [<Descriptor>]
- **Level**: <Class> <Level>
- **Initiation Action**: <Action>
- **Prerequisites**: <Prerequisites>
- **Mechanics**: <Range, target, duration, and mechanical gameplay rules text>
```

Field specifications:
- **Source**: Primary source citation (*Tome of Battle*, page number).
- **Type**: Maneuver type in parentheses, and optional descriptor in square brackets.
- **Level**: Initiator class or classes and level (e.g. `Crusader 1, Warblade 1`).
- **Initiation Action**: Action economy cost (e.g. `1 standard action`, `1 swift action`, `1 immediate action`).
- **Prerequisites**: Minimum maneuvers known or prerequisite conditions (e.g. `None`, `1 Desert Wind maneuver`).
- **Mechanics**: Range, target, duration, and mechanical gameplay rules text are integrated directly into `Mechanics`. Narrative flavor text is strictly excluded to keep entries concise and within file length caps.

#### 2. Feat Reference Schema

Feats in `docs/rules/content/feats/` follow this structured reference format:

```markdown
### <Name>
- **Status**: Verified Reference Entry
- **Source**: *<Sourcebook>*, p. <Page>
- **Prerequisites**: <Prerequisites, or omit if None>
- **Benefit**:
  - <Operative mechanical rules in tabletop language>
  - <If conditional or multi-part (e.g. adept vs. non-adept, secondary grants), use structured sub-bullets>
```

When a feat involves a rules dysfunction, append the two-part dysfunction fields directly beneath `Benefit`:

```markdown
- **Rules Dysfunction** (`dysfunction:<id>`): <State the contradiction, omission, or deadlock in rules-as-written.>
- **Human-Approved Correction**: <State the human-approved table ruling in plain tabletop language.>
```

#### 3. Vestige Reference Schema

Vestiges in `docs/rules/content/vestiges.md` (or `docs/rules/content/vestiges/` if split under the 300-line ceiling) follow this structured reference format:

```markdown
### <Name>
- **Source**: *Tome of Magic*, p. <Page>
- **Level**: <Vestige Level (1–8)>
- **Binding DC**: <Check DC>
- **Special Requirement**: <Specific requirement to draw seal/summon, or None>
- **Sign**: <Physical manifestation on the binder's body>
- **Influence**: <Mandatory action or prohibited behavior triggering the defiance penalty under a Poor Pact>
- **Granted Abilities**:
  - **<Ability Name>**:
    - **Action**: <Action & frequency: Standard (every 5 rounds) | Swift | Immediate | Passive | Standard (1/day) | etc.>
    - **Mechanics**: <Operative mechanical text: targeting, range, duration, save type and DC formula, scaling, and whether showing the Sign is required>
```

When a vestige involves a rules dysfunction, append the two-part dysfunction fields directly beneath `Granted Abilities`:

```markdown
- **Rules Dysfunction** (`dysfunction:<id>`): <State the contradiction, omission, or deadlock in rules-as-written.>
- **Human-Approved Correction**: <State the human-approved table ruling in plain tabletop language.>
```

Field specifications:
- **Source**: Primary source citation (*Tome of Magic*, page number).
- **Level**: Vestige level (1st through 8th level), gating access by Effective Binder Level.
- **Binding DC**: Target DC for the 1d20 + Effective Binder Level + Cha mod binding check.
- **Special Requirement**: Environmental, item, or personal prerequisite to draw the seal or summon. Must explicitly state `None` if absent to confirm verification.
- **Sign**: Observable physical transmutation on the binder's body while bound. Suppressible with Suppress Sign on a Good Pact; unsuppressible on a Poor Pact.
- **Influence**: The actionable mandatory requirement or prohibition under a Poor Pact. Omits personality flavor text and universal defiance penalty boilerplate per the pure-mechanics and subsystem-scoped rules, relying on the baseline rules in `docs/rules/subsystems/pact-magic.md`.
- **Granted Abilities**: Structured sub-bullet list for each granted power detailing action cost and frequency/cooldown collapsed together (e.g. `Standard (every 5 rounds)`, `Passive`, `Swift`), followed by complete operative mechanics (including save types, DC formulas, and dependencies on showing the Sign). Note that all powers granted by vestiges are supernatural in origin (*Tome of Magic*, p. 19), so ability type tags (`(Su)`, `(Ex)`, `(Sp)`) are omitted.

Example: Canonical Reference Entry (Amon)

```markdown
### Amon
- **Source**: *Tome of Magic*, p. 21
- **Level**: 1
- **Binding DC**: 20
- **Special Requirement**: Amon refuses your call if you have hosted Chupoclops, Eurynome, Karsus, or Leraje within the last 24 hours (and vice versa).
- **Sign**: You grow a pair of curling ram's horns.
- **Influence**: You must attempt a saving throw against beneficial spells cast by worshipers of fire, the sun, or law deities.
- **Granted Abilities**:
  - **Darkvision**:
    - **Action**: Passive
    - **Mechanics**: You gain darkvision out to 60 ft.
  - **Fire Breath**:
    - **Action**: Standard (every 5 rounds)
    - **Mechanics**: You breathe a line of fire extending 10 feet per effective binder level (maximum 50 feet). The breath deals 1d6 fire damage per effective binder level; creatures in the area can attempt a Reflex save (DC 10 + 1/2 EBL + Cha modifier) for half damage.
  - **Ram Attack**:
    - **Action**: Attack action (natural weapon)
    - **Mechanics**: Requires showing Amon's sign. You gain a natural ram attack dealing 1d6 damage plus 1-1/2 times your Strength modifier. When charging, your ram attack deals an extra 1d8 damage.
```

Unarbitrated Vestige Skeleton Schema:

```markdown
### <Name>
- **Status**: Unarbitrated Domain Specification
- **Source**: TODO
- **Level**: TODO
- **Binding DC**: TODO
- **Special Requirement**: TODO
- **Sign**: TODO
- **Influence**: TODO
- **Granted Abilities**:
  - TODO
```

#### 4. Compact Unarbitrated Stub Schema (General Rules Skeleton)

For general content catalogs awaiting arbitration (such as feats, items, equipment, and classes), use the compact unarbitrated stub schema:

```markdown
### <Name>
- **Status**: Unarbitrated Domain Specification
- **Source**: TODO
```

An unarbitrated entry retains `- **Status**: Unarbitrated Domain Specification` even if its primary source and page number are already identified (`- **Source**: <Sourcebook>, p. <Page>`). It only transitions to a full Reference Entry (omitting the Status line) once the actionable Environmental Facts (formulas, table progressions, or mechanics) are fully recorded.

When human arbitration confirms the primary source and mechanics, replace the Rules Skeleton with the full structured Reference Entry for that content type (citing the primary sourcebook and page, and detailing actionable Environmental Facts).

### Positive grilling & conversational protocol

When framing frontier questions and recommendations in `/grilling`, `/wayfinder`, or general chat:
- Focus questions on **engine representation and software architecture** (data modeling in `rules.json`, class hierarchies, seams, non-blocking warning badges).
- If a decision or recommendation hinges on a game rule that is still an unarbitrated Rules Skeleton or absent in `docs/rules/`, use this explicit positive recommendation token:
  `➡️ Recommendation: Defer until human arbitration provides the primary source citation for [Rule].`

## Issue / Spec template

Use this template when creating GitHub issues or drafting specs for rules, statistics, subsystems, feats, classes, or modifiers:

```markdown
# [RULES] <Subsystem / Feat / Statistic / Effect Name>

## 1. Summary
Brief description of the rule or capability being modeled in PlayMore35.

---

## 2. Rules Provenance (Mandatory Gate for ready-for-agent)
> Check `docs/rules/` first. If already populated, copy or link the verified citation here. If marked with an unarbitrated Rules Skeleton or absent, verify this section with the human arbiter and write the citation into `docs/rules/`.

- **Rules Library Target**: `<e.g., docs/rules/mechanics/combat.md or docs/rules/content/feats.md>`
- **Primary Source**: `<Sourcebook Name>` (e.g., *Player's Handbook 3.5*, *Complete Warrior*, *Rules Compendium*)
- **Page Number(s)**: `p. <Number>`
- **Verbatim Rule Text / Quote**:
  > "<Insert verbatim rule text, or markdown transcript of screenshot provided by human arbiter>"
- **Official Errata / FAQ (if applicable)**: `<Errata text or "None">`
- **Human Arbitration & Table Fiat**:
  - *Interpretation*: `<Clarifications on ambiguities, stacking, or interaction with existing subsystems>`
  - *House Rules / Table Fiat*: `<Overrides or custom table rulings per ADR-0001 Enable, Don't Enforce>`

---

## 3. Engine Target & Representation
- **Data Definitions (`rules.json`)**:
  - `stats`: `<New or modified Statistics>`
  - `effects`: `<Modifiers, toggles, stacking categories>`
- **Subsystem & C# Classes**:
  - `<Engine classes or interfaces affected, e.g., Subsystems/PactMagic, CharacterChoices/Feat.cs>`
- **Advisory Warnings**:
  - `<Prerequisites or illegal condition checks to surface via non-blocking Warning Badges>`

---

## 4. Test Specifications
- **Unit Test Class**: `<e.g. PlayMore35.Tests/FeatTests.cs>`
- **Test Citation**: Every test method must include the citation comment directly above the test:
  ```csharp
  // Source: <Sourcebook Name>, p. <Number>
  [TestMethod]
  public void FeatName_CalculatesCorrectBonus_WhenConditionMet() { ... }
  ```

---

## 5. Scope Boundaries
- **In Scope**: `<Explicit list of mechanics covered>`
- **Anti-goals / Out of Scope**: `<Conflated mechanics, higher-tier abilities, or deferred variants>`
```

## Verification rubric

Use this checklist during triage, planning, and code review:

| Stage | Check | Pass Criteria |
| :--- | :--- | :--- |
| **Lookup / Triage** | **Source Verified & Recorded** | Check `docs/rules/` first. If an unarbitrated Rules Skeleton or missing, obtain exact sourcebook, page number, and quote from the human (or label `needs-info`) and record it in `docs/rules/`. |
| **Planning** | **Zero Extrapolation** | Implementation plan (`implementation_plan.md`) includes a `Rules Provenance` section reproducing the verified quote from `docs/rules/`. No mechanics are assumed from memory. Apply explicit modifier formulas; never substitute shortcuts induced from sample cases. |
| **Coding** | **Fidelity, Citations & Line Cap** | Code strictly models the quoted text. Unit tests cite book and page in comments. Updated `docs/rules/` files pass `RulesLibraryStructureTests` ($\le 300$ lines). |
| **Review** | **Spec Axis Review** | `/code-review` verifies the diff adheres strictly to the quoted text without unauthorized feature creep and that any newly arbitrated citation is persisted in `docs/rules/`. |

### Phase boundary enforcement

Enforce rules provenance at phase gates rather than relying on in-flight self-interception:
- **Spec Gate (`/to-spec`)**: A rules-bearing task cannot be marked `ready-for-agent` without Section 2 (Rules Provenance) completed with sourcebook, page number, and verbatim quote (either retrieved from a populated `docs/rules/` entry or newly confirmed by the maintainer and written to `docs/rules/`).
- **Modifier Types Integrity Gate**: When implementing an ability or catalog entry whose primary source specifies a bonus type missing from `ModifierTypes.All`, pause and request human arbitration: present the primary source citation, its verbatim stacking text, and propose adding the type to `PlayMore35.Effects.ModifierTypes` and `docs/rules/mechanics/stacking-and-modifiers.md`.
- **TDD Gate (`/tdd`)**: Every unit test verifying a game rule must cite the primary source in a comment directly above the test method (`// Source: <Sourcebook Name>, p. <Number>`). If the citation is missing from `docs/rules/`, pause and request the source from the user before proceeding.
- **Review & Pre-commit Gate (`/code-review`, `git commit`)**: Audit all modified rules logic and `PlayMore35/Resources/*.json` entries strictly against the verified primary source quote in `docs/rules/` or the issue. Confirm that unit tests cite the primary source, that newly arbitrated citations were written to `docs/rules/`, and that no unarbitrated mechanical assumptions crept into the implementation.
