## Agent skills

### Issue tracker

Issues are tracked in GitHub Issues using the `gh` CLI. See `docs/agents/issue-tracker.md`.

### Triage labels

Canonical triage roles mapped to matching labels (`needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`). See `docs/agents/triage-labels.md`.

### Domain docs

Single-context layout (`GLOSSARY.md` and `docs/adr/` at the repo root) for project and engine vocabulary, plus progressively disclosed 3.5e rules and content citations in `docs/rules/`. See `docs/agents/domain.md`, `docs/agents/rules-template.md`, and `docs/agents/subsystems.md`.

### Product vision

Product destination, architectural invariants, capability frontiers, and anti-goals: see `VISION.md`.

### Coding standards

C# architecture, WinForms layout and `Designer.cs` conventions, `.csproj` `<SubType>` rules, and testing practices: see `CODING_STANDARDS.md`.

## Formatting and conventions

### Git conventions

Prefer linear git history:
- Rebase working branches onto the target base branch (`git rebase <base>`) rather than creating merge commits.
- Integrate branch commits via fast-forward merges (`git merge --ff-only <branch>`). Avoid `--no-ff` merge commits where possible.
- When a task graph has a single active frontier (strictly sequential dependencies), work directly on the branch without creating extra worktree indirection. Still dispatch an implementer subagent per ticket (inheriting the workspace) to keep the orchestrator's context window lean.

### Pull requests

When authoring pull request descriptions with Mermaid diagrams:
- Always wrap Mermaid edge labels in double quotes (e.g. `-->|"Read/Write Mutation"|`). Unquoted slashes, brackets, or parentheses break GitHub's Mermaid parser.

### Code architecture and test seams

- See `CODING_STANDARDS.md` for UI encapsulation, WinForms layout, `.csproj` `<SubType>`, and test performance rules.
- **Prototype clean references**: When promoting code from a prototype exploring multiple design variants, never reference discarded variants in tickets or specifications. Prune all unselected options from descriptions and pointers upstream so the agent only encounters the chosen variant.

## Rules arbitration invariant

Unrecorded D&D 3.5e game rules are **Domain Specifications**, while verified citations recorded in `docs/rules/` are **Environmental Facts** (see `GLOSSARY.md`). This invariant binds **all chat turns, grilling rounds, recommendations, planning, and code**:
- **Applies to mechanics and game content only**: Binds actual game rules logic and content definitions (math formulas, stacking, prerequisites, modifiers, subsystem interactions, and `PlayMore35/Resources/*.json` entries). Does not apply to application presentation schema or UI layout categorization (e.g. Column 0 `Exploration` or `Social` section groupings).
- **Progressive disclosure lookup (`docs/rules/`)**: Load only the specific file(s) relevant to the mechanic or content under discussion:
  - **Systemic mechanics** (`rules.json` and engine subsystems): `docs/rules/mechanics/<topic>.md` (`abilities-and-saves.md`, `skills.md`, `combat.md`, `health-and-conditions.md`, `equipment-and-encumbrance.md`, `progression.md`, `stacking-and-modifiers.md`, `pact-magic.md`).
  - **Specific content** (1:1 mirror of `PlayMore35/Resources/<stem>.json`): `docs/rules/content/<stem>.md` or, once split, `docs/rules/content/<stem>/` (such as `docs/rules/content/classes/<class>.md`).
- **Two-state resolution & write-back**:
  - **Populated citation in `docs/rules/`**: Counts as an **Environmental Fact**. Use it directly without re-asking the human.
  - **`TODO` stub (`- [ ] **<Name>**: TODO`) or absent entry**: Remains an unarbitrated **Domain Specification**. Treat model pretraining as blind to 3.5e mechanics; request human arbitration per `docs/agents/rules-template.md`, and **replace the `TODO` (or add the entry) in `docs/rules/` immediately** when the human provides the citation.
- **300-line growth & split gate**: Every `docs/rules/**/*.md` file must remain $\le 300$ lines (enforced by `RulesLibraryStructureTests`). When an edit pushes a file over 300 lines, run a `/grilling` round with the human maintainer to choose the subgrouping(s) and split `<stem>.md` into `<stem>/`.
- **Positive grilling & conversational protocol**: When framing frontier questions and recommendations in `/grilling`, `/wayfinder`, or general chat:
  - Focus questions on **engine representation and software architecture** (data modeling in `rules.json`, class hierarchies, seams, non-blocking warning badges).
  - If a decision or recommendation hinges on a game rule that is still a `TODO` or absent in `docs/rules/`, use this explicit positive recommendation token:
    `➡️ Recommendation: Defer until human arbitration provides the primary source citation for [Rule].`
- **Phase boundary enforcement**: Enforce rules provenance at phase gates rather than relying on in-flight self-interception:
  - **Spec Gate (`/to-spec`)**: A rules-bearing task cannot be marked `ready-for-agent` without Section 2 (Rules Provenance) completed with sourcebook, page number, and verbatim quote (either retrieved from a populated `docs/rules/` entry or newly confirmed by the maintainer and written to `docs/rules/`).
  - **TDD Gate (`/tdd`)**: Every unit test verifying a game rule must cite the primary source in a comment directly above the test method (`// Source: <Sourcebook Name>, p. <Number>`). If the citation is missing from `docs/rules/`, pause and request the source from the user before proceeding.
  - **Review & Pre-commit Gate (`/code-review`, `git commit`)**: Audit all modified rules logic and `PlayMore35/Resources/*.json` entries strictly against the verified primary source quote in `docs/rules/` or the issue. Confirm that unit tests cite the primary source, that newly arbitrated citations were written to `docs/rules/`, and that no unarbitrated mechanical assumptions crept into the implementation.

## Planning and pre-commit gates

Before committing or marking a PR ready:
- **Rules provenance gate**: Verify rules provenance for any changes touching game mechanics per [Rules arbitration invariant](#rules-arbitration-invariant).
- **Scope gate**: Audit the diff against the originating issue's **Out of Scope** and **Anti-goals** and `CODING_STANDARDS.md`. Flag any JSON fallback, adapter shim, compatibility layer, or deprecated forwarding constructor as a hard violation.
- **Documentation gate**: When authoring an `implementation_plan.md` (include under `Proposed Changes`) or before running `git commit`, inspect changes and update repository documentation as appropriate:
  - `docs/rules/`: When a game rule or content item is arbitrated, replace its `TODO` stub or add the citation in `docs/rules/mechanics/` or `docs/rules/content/`. When new mechanics or content entries are added without immediate citation, add a `- [ ] **<Name>**: TODO` stub to the matching file.
  - `README.md`: When user-facing features, dashboard workflows, status matrix, or setup instructions change.
  - `ROADMAP.md`: When backlog items, progression tiers, or feature priorities are started, completed, or reordered. When backlog items are completed, remove them from `ROADMAP.md` rather than marking them as completed in the text.
  - `GLOSSARY.md`: When project/engine domain language or architectural concepts are introduced, modified, or retired (keep specific 3.5e rules and game content in `docs/rules/`, never `GLOSSARY.md`).
  - `VISION.md`: When product destination, architectural invariants, capabilities, or anti-goals shift.
