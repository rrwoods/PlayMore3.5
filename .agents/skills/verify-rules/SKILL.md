---
name: verify-rules
description: Sanity-check and verify in-repo distilled rules documentation (docs/rules/) against the verbatim primary source archive (rules-md/).
disable-model-invocation: true
---

# Verify Rules

Sanity-check and crosscheck distilled, mechanics-only reference documentation in `docs/rules/` against the authoritative verbatim mirror in the external archive (`rules-md/`), ensuring zero mechanical omission, accurate primary source citations, schema adherence, and strict exclusion of narrative fluff.

## Preconditions & References

1. **Authoritative Verbatim Mirror & Direct Read Access**: The external archive at:
   `C:/Users/rickr/Documents/D&D notes/resources/rules-md/<Book Name>/<Chapter or Section>.md`
   is the single point of truth for verbatim text. Antigravity permissions grant direct read access across `resources/rules-md/`, so inspect verbatim archive text directly with `read_file`. Scratch scripts and sandbox bypass are not required for reading archive text.
2. **Write Access for Remediation**: Antigravity permissions grant write access to `C:\Users\rickr\Documents\D&D notes\resources\rules-md`. Verbatim mirror formatting defects or typos discovered during verification can be remediated directly in-place via `edit_file` or `write_file`.
3. **In-Repo Target**: A file or partitioned directory under:
   `docs/rules/mechanics/`, `docs/rules/subsystems/`, or `docs/rules/content/`.
4. **Reference Schemas**: The canonical schemas registered in `docs/agents/rules-template.md`.

---

## Phase 1: Intake & Scope Definition

Identify the scope of the verification:
- **Distilled In-Repo Path**: Target file (e.g. `docs/rules/content/feats/complete-warrior.md`) or partitioned directory (e.g. `docs/rules/content/vestiges/`).
- **Verbatim Archive Path**: e.g. `C:/Users/rickr/Documents/D&D notes/resources/rules-md/Complete Warrior/Chapter 2 - Feats.md`.
- **Scope**: Whole file, partitioned directory, a designated section/subgroup, or a specific list of entities.
- **Partitioned Directories**: When the in-repo target is a directory containing multiple partition files (e.g. `vestiges-a-d.md`, `vestiges-e-l.md`, `vestiges-m-z.md` split to satisfy the 300-line ceiling), aggregate all markdown files under that directory into a single combined entity inventory before checking coverage.

Confirm that both paths exist and are accessible before proceeding.

---

## Phase 2: Catalog & Coverage Crosscheck

### 1. Execution Strategy
- **Direct Inspection (Small to Medium Batches, 1–15 entities)**: Read the verbatim archive file directly using `read_file`, compare against the in-repo file in `docs/rules/`, and evaluate coverage and accuracy in-context. No scratch scripts or shell commands required.
- **Automated Batch Verification (Large Batches or Whole Chapters, 16+ entities)**: Author a temporary verification script in `scratch/` (e.g. `scratch/verify_<topic>.py`). If executed with the dedicated `dnd_pdf` python environment (`C:\Users\rickr\Envs\dnd_pdf\Scripts\python.exe`), set `BypassSandbox: true` because the Python virtual environment is outside the sandbox boundary. The script parses both `rules-md/` and `docs/rules/` (aggregating partitioned directory files), cross-checks entity inventories, physical page citations, prerequisites, formulas, and schema fields, and outputs the verification report directly.

### 2. Entity Extraction
- Extract all entity names and physical page citations from `rules-md/` (pattern: `### <Name> (p. <Page>)` or `## <Name> (p. <Page>)`).
- Extract all entity names and `- **Source**:` citations from `docs/rules/` (across all files if partitioned).

### 3. Coverage Assertions
- **Omission Check**: Identify any entities present in `rules-md/` within the declared scope that are missing from `docs/rules/`.
- **Orphan Check**: Identify any entities in `docs/rules/` that claim this sourcebook/section but do not exist in `rules-md/`.
- **Page Citation Accuracy**: Confirm that `- **Source**: *<Sourcebook>*, p. <Page>` in `docs/rules/` exactly matches the physical page recorded in `rules-md/`.

---

## Phase 3: Semantic & Mechanical Fidelity Audit

For every entity in scope, perform an itemized crosscheck against the verbatim text in `rules-md/`:

### 1. Prerequisites & Gating
- Verify that all stat minimums, Base Attack Bonuses, skills, feats, caster levels, or alignment restrictions from `rules-md/` are faithfully transcribed.
- Ensure no prerequisite was dropped, altered, or assumed.

### 2. Operative Mechanics & Mathematical Fidelity
- **Action Economy**: Verify action types (Standard, Move, Swift, Immediate, Full-Round, Attack Action, Free).
- **Targeting & Geometry**: Verify range, target, area of effect, and shape (e.g. line, cone, burst).
- **Duration & Dismissal**: Verify duration and concentration/dismissal conditions.
- **Saving Throws & DCs**: Check save type (Fortitude, Reflex, Will), effect on success (half, negates, partial), and mathematical DC formulas (e.g. `10 + 1/2 EBL + Cha modifier`).
- **Bonus Types & Stacking**: Confirm bonus types are explicitly stated using official 3.5e types (competence, morale, insight, sacred, profane, enhancement).
- **Scaling Formulas**: Confirm scaling rules and progression tables are explicit (e.g. `1d6 per 2 levels`). Ensure no informal shortcuts or approximations were substituted for explicit formulas.
- **Branching Benefits**: Confirm multi-part abilities (e.g. adept vs. non-adept, tactical options, conditional clauses) are deconstructed into distinct sub-bullets.

### 3. Fluff Exclusion
- Confirm that introductory atmospheric descriptions, physical appearances, and developer commentary are completely stripped from `Benefit`, `Mechanics`, or `Granted Abilities`.

### 4. Dysfunctions & Contradictions
- If the verbatim text contains an inherent rules contradiction, impossible condition, or known rules dysfunction:
  - Verify that `- **Rules Dysfunction** (<id>)` is recorded.
  - Verify that `- **Human-Approved Correction**` contains the human table ruling.
  - If unarbitrated, flag for human review per ADR-0001.

---

## Phase 4: Schema & Line Cap Validation

1. **Schema Compliance**: Check that every entry strictly matches the canonical schema registered in `docs/agents/rules-template.md`.
2. **Task Checklist Ban**: Ensure no checklist syntax (`- [ ]`) is present.
3. **300-Line Growth Ceiling**:
   Run the automated structure test suite:
   ```powershell
   dotnet test --filter RulesLibraryStructureTests --blame-hang-timeout 15s
   ```
   (If executing unsandboxed is needed for the MSBuild task host, use `BypassSandbox: true`).

---

## Phase 5: Verification Report & Remediation

Produce a structured markdown audit report:

```markdown
# Verification Report: <Target File> vs. <Source Section>

## 1. Summary
- **Target File**: `docs/rules/<path>.md`
- **Primary Source Archive**: `rules-md/<Book>/<Section>.md`
- **Total Entities Audited**: N
- **Audit Status**: PASS | DEFECTS FOUND

## 2. Check Results
| Check | Status | Notes |
| :--- | :---: | :--- |
| **Entity Coverage** | PASS / FAIL | N/N entities present (X missing) |
| **Folio Page Citations** | PASS / FAIL | Exact folio match across all entries |
| **Prerequisites Fidelity** | PASS / FAIL | All gating conditions verified |
| **Mechanical Fidelity** | PASS / FAIL | Formulas, action costs, bonuses verified |
| **Fluff Stripping** | PASS / FAIL | Pure mechanics; zero narrative text |
| **Schema Compliance** | PASS / FAIL | Conforms to rules-template.md schema |
| **300-Line Limit** | PASS / FAIL | Total lines: X / 300 |

## 3. Discrepancies & Remediation Diffs
<If defects were found, provide exact edit_file target and replacement blocks to resolve each issue>
```

If defects are found:
- **In-repo rules defects**: Present and execute targeted `edit_file` fixes in `docs/rules/`.
- **Verbatim archive defects**: If the `rules-md/` mirror itself has transcription typos or formatting defects, update it directly using `edit_file` or `write_file` thanks to configured write permissions on `rules-md/`.
- If ambiguous or involving conflicting RAW, request human arbitration per ADR-0001.
- Re-run `RulesLibraryStructureTests` until clean.
