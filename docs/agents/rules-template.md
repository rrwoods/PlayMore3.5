# Rules Template & Rubric

How to specify, arbitrate, and verify D&D 3.5e game rules in PlayMore35.

## The arbitration rule & progressive disclosure library (`docs/rules/`)

Unrecorded D&D 3.5e game rules are **Domain Specifications**, while verified citations recorded in `docs/rules/` are **Environmental Facts** (see `GLOSSARY.md`). Game rules originate exclusively from human-verified primary sources. Treat model pretraining as blind to 3.5e mechanics; all rules awareness enters the engine through human arbitration and is persisted in `docs/rules/`.

### Directory layout

Do not bulk-load `docs/rules/` upfront. Open only the specific file(s) matching the mechanic or content item your task touches:

```
docs/rules/
├── mechanics/                          ← Systemic 3.5e rules (rules.json & C# subsystems)
│   ├── abilities-and-saves.md
│   ├── combat.md
│   ├── equipment-and-encumbrance.md
│   ├── health-and-conditions.md
│   ├── progression.md
│   ├── skills.md
│   └── stacking-and-modifiers.md
├── subsystems/                         ← Subsystem rules (e.g. spellcasting, manifesting, etc)
│   ├── spellcasting.md (not yet present)
│   ├── manifesting.md (not yet present)
│   ├── maneuvers.md
│   ├── pact-magic.md
│   ├── truenaming.md (not yet present)
│   ├── mysteries.md (not yet present)
│   ├── meldshaping.md (not yet present)
│   ├── invocations.md (not yet present)
└── content/                            ← 1:1 mirror of PlayMore35/Resources/<stem>.json (excluding external.json)
    ├── armor.md
    ├── armorProperties.md
    ├── classes/
    │   ├── barbarian.md
    │   ├── binder.md
    │   └── fighter.md
    ├── feats.md
    ├── items.md
    ├── races.md
    ├── shields.md
    ├── vestiges.md
    ├── weaponProperties.md
    └── weapons.md
```

### Two-state lookup & write-back workflow

When inspecting a target file in `docs/rules/`:
1. **Populated citation**: The entry contains a verified sourcebook, page number, and rule text/ruling. Treat it as an **Environmental Fact** and use it directly without re-asking the human.
2. **`TODO` stub (`- [ ] **<Name>**: TODO`) or absent entry**: The entry is still an unarbitrated **Domain Specification**. Request human arbitration using the template below. Once the human provides or confirms the citation, **immediately replace the `TODO` stub (or add the entry) in `docs/rules/`** so future sessions never have to ask again.

### 300-line growth threshold & splitting protocol

Every `.md` file under `docs/rules/` is capped at **300 lines** (enforced by `RulesLibraryStructureTests` in `PlayMore35.Tests`).
- When recording a citation pushes a file over 300 lines (or `RulesLibraryStructureTests` fails), **pause and initiate a `/grilling` session** with the human maintainer to decide the subgrouping(s).
- Once the subgrouping is agreed upon, replace `<stem>.md` with the directory `<stem>/` containing the agreed sub-files.

## Issue / Spec template

Use this template when creating GitHub issues or drafting specs for rules, statistics, subsystems, feats, classes, or modifiers:

```markdown
# [RULES] <Subsystem / Feat / Statistic / Effect Name>

## 1. Summary
Brief description of the rule or capability being modeled in PlayMore35.

---

## 2. Rules Provenance (Mandatory Gate for ready-for-agent)
> Check `docs/rules/` first. If already populated, copy or link the verified citation here. If marked `TODO` or absent, verify this section with the human arbiter and write the citation into `docs/rules/`.

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
| **Lookup / Triage** | **Source Verified & Recorded** | Check `docs/rules/` first. If `TODO` or missing, obtain exact sourcebook, page number, and quote from the human (or label `needs-info`) and record it in `docs/rules/`. |
| **Planning** | **Zero Extrapolation** | Implementation plan (`implementation_plan.md`) includes a `Rules Provenance` section reproducing the verified quote from `docs/rules/`. No mechanics are assumed from memory. |
| **Coding** | **Fidelity, Citations & Line Cap** | Code strictly models the quoted text. Unit tests cite book and page in comments. Updated `docs/rules/` files pass `RulesLibraryStructureTests` ($\le 300$ lines). |
| **Review** | **Spec Axis Review** | `/code-review` verifies the diff adheres strictly to the quoted text without unauthorized feature creep and that any newly arbitrated citation is persisted in `docs/rules/`. |
