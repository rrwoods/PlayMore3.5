# Rules Template & Rubric

How to specify, arbitrate, and verify D&D 3.5e game rules in PlayMore35.

## The arbitration rule & progressive disclosure library (`docs/rules/`)

Unrecorded D&D 3.5e game rules are **Domain Specifications**, while verified citations recorded in `docs/rules/` are **Environmental Facts** (see `GLOSSARY.md`). Game rules originate exclusively from human-verified primary sources. Treat model pretraining as blind to 3.5e mechanics; all rules awareness enters the engine through human arbitration and is persisted in `docs/rules/`.

### Directory layout

Do not bulk-load `docs/rules/` upfront. Query the filesystem for the specific file matching your task:

- `docs/rules/mechanics/`: Systemic 3.5e rules logic (e.g. `combat.md`, `stacking-and-modifiers.md`, `skills.md`).
- `docs/rules/subsystems/`: Dedicated subsystem arbitration documents (e.g. `maneuvers.md`, `pact-magic.md`).
- `docs/rules/content/`: 1:1 mirror of `PlayMore35/Resources/<stem>.json`. Stored as `<stem>.md` (or a `<stem>/` subfolder if split under the 300-line ceiling).

### Two-state lookup & write-back workflow

When inspecting a target file in `docs/rules/`:
1. **Populated citation (Reference Entry)**: The entry contains a verified sourcebook, page number, and mechanical facts without checklist syntax. Treat it as an **Environmental Fact** and use it directly without re-asking the human.
2. **Rules Skeleton (unarbitrated placeholder) or absent entry**: The entry is an unarbitrated **Domain Specification** awaiting human arbitration (formatted as a compact unarbitrated stub schema or a maneuver schema skeleton with `TODO` values). Request human arbitration using the template below. Once the human provides or confirms the citation, **immediately replace the Rules Skeleton (or add the entry) in `docs/rules/` as a fully populated Reference Entry** so future sessions never have to ask again.

### 300-line growth threshold & splitting protocol

Every `.md` file under `docs/rules/` is capped at **300 lines** (enforced by `RulesLibraryStructureTests` in `PlayMore35.Tests`).
- When recording a citation pushes a file over 300 lines (or `RulesLibraryStructureTests` fails), **pause and initiate a `/grilling` session** with the human maintainer to decide the subgrouping(s).
- Once the subgrouping is agreed upon, replace `<stem>.md` with the directory `<stem>/` containing the agreed sub-files.

### Explicit modifier formulas over induced shortcuts

When an ability modifies an underlying system mechanic via an explicit formula (such as adjusting an effective level, bonus, or progression parameter), execute the full mechanical pipeline for every evaluation. Never replace the rule's explicit calculation with an informal pattern or mental shortcut induced from a few sample cases.

### Rules library authoring style

When writing or updating reference documents in `docs/rules/`:
- **Positive reference entry format**: Author entries as positive, structured reference documentation (**Reference Entries** and **Rules Skeletons** per `GLOSSARY.md` and ADR-0011). State active rules, formulas, and data properties directly, citing the primary sourcebook and page.
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

#### 2. Compact Unarbitrated Stub Schema (General Rules Skeleton)

For general content catalogs awaiting arbitration (such as feats, items, equipment, and classes), use the compact unarbitrated stub schema:

```markdown
### <Name>
- **Status**: Unarbitrated Domain Specification
- **Source**: TODO
```

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
- **TDD Gate (`/tdd`)**: Every unit test verifying a game rule must cite the primary source in a comment directly above the test method (`// Source: <Sourcebook Name>, p. <Number>`). If the citation is missing from `docs/rules/`, pause and request the source from the user before proceeding.
- **Review & Pre-commit Gate (`/code-review`, `git commit`)**: Audit all modified rules logic and `PlayMore35/Resources/*.json` entries strictly against the verified primary source quote in `docs/rules/` or the issue. Confirm that unit tests cite the primary source, that newly arbitrated citations were written to `docs/rules/`, and that no unarbitrated mechanical assumptions crept into the implementation.
