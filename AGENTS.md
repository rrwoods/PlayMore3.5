## Agent skills

### Issue tracker

Issues are tracked in GitHub Issues using the `gh` CLI. See `docs/agents/issue-tracker.md`.

### Triage labels

Canonical triage roles mapped to matching labels (`needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`). See `docs/agents/triage-labels.md`.

### Domain docs

Single-context layout (`GLOSSARY.md` and `docs/adr/` at the repo root). See `docs/agents/domain.md`.

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

D&D 3.5e game rules are **Domain Specifications**, not **Environmental Facts** (see `GLOSSARY.md`). This invariant binds **all chat turns, grilling rounds, recommendations, planning, and code**:
- **Environmental Facts vs. Domain Specifications**: Finding environmental facts (codebase, test suites, git history, committed `rules.json`, directory structure) is exclusively the agent's job using read tools. Domain specifications (mechanics, formulas, stacking, prerequisites, modifiers, subsystem interactions) are external requirements arbitrated exclusively by the human maintainer via primary source citations. Treat model pretraining as blind to 3.5e mechanics: derive rules exclusively from verified repository sources.
- **Positive grilling & conversational protocol**: When framing frontier questions and recommendations in `/grilling`, `/wayfinder`, or general chat:
  - Focus questions on **engine representation and software architecture** (data modeling in `rules.json`, class hierarchies, seams, non-blocking warning badges).
  - If a decision or recommendation hinges on game mechanics not yet cited in the repository, use this explicit positive recommendation token:
    `➡️ Recommendation: Defer until human arbitration provides the primary source citation for [Rule].`
  - When researching rules, search local committed repository files (`rules.json`, committed tests, `docs/`). If absent from the environment, request human arbitration using `docs/agents/rules-template.md`.
- **Phase boundary enforcement**: Enforce rules provenance at phase gates rather than relying on in-flight self-interception:
  - **Spec Gate (`/to-spec`)**: A rules-bearing task cannot be marked `ready-for-agent` without Section 2 (Rules Provenance) completed with sourcebook, page number, and verbatim quote confirmed by the maintainer.
  - **TDD Gate (`/tdd`)**: Every unit test verifying a game rule must cite the primary source in a comment directly above the test method (`// Source: <Sourcebook Name>, p. <Number>`).
  - **Review Gate (`/code-review`)**: The Spec axis review audits modified rules logic and `rules.json` entries strictly against the user-supplied primary source quote. Any unverified mechanics or extrapolation are flagged as a hard Spec failure.

## Planning and pre-commit gates

- **Rules provenance and scope gates**: Before committing or marking a PR ready, verify rules provenance and anti-shim scope compliance per `CODING_STANDARDS.md`.
- **Documentation gate**: When authoring an `implementation_plan.md` or before running `git commit`, update repository documentation following the [Documentation checklist](#documentation-checklist).

### Documentation gate

When authoring an implementation plan (`implementation_plan.md`), include planned repository documentation updates under `Proposed Changes` following the [Documentation checklist](#documentation-checklist).

## Pre-commit gates

### Rules provenance gate

Before running `git commit` or marking a PR ready for changes touching game rules:
- **Applies to mechanics only**: Binds actual game rules logic (math formulas, stacking, prerequisites, modifiers, subsystem interactions, and `rules.json` entries). Does not apply to application presentation schema or UI layout categorization (e.g. Column 0 `Exploration` or `Social` section groupings).
- **Primary source provenance**: Citations come exclusively from user-supplied primary sources in the plan or issue. When a citation is needed for test comments or review gates, transcribe the exact book, page, and quote verified by the maintainer. If a citation is missing, pause and request the source from the user before proceeding.
- Audit all modified rules logic and `rules.json` entries against the user-supplied source quote in the plan or issue.
- Verify that unit tests validating the rule cite the user-supplied primary source (book and page) in test comments or names.
- Confirm that no unarbitrated mechanical assumptions or extrapolation crept into the implementation.

### Documentation gate

Before running `git commit`, inspect staged changes and update repository documentation following the [Documentation checklist](#documentation-checklist).

### Scope gate

Before running `git commit` or marking a PR ready, audit changes against the originating issue or specification:
- Check the diff against the issue's **Out of Scope** and **Anti-goals**. If any JSON fallback, adapter shim, or deprecated forwarding constructor was introduced, flag it as a hard violation.
- Verify that no shims, compatibility layers, or features explicitly marked **Out of Scope** or **Anti-goals** were introduced.

## Documentation checklist

Update repository documentation as appropriate:
- `README.md`: When user-facing features, dashboard workflows, status matrix, or setup instructions change.
- `ROADMAP.md`: When backlog items, progression tiers, or feature priorities are started, completed, or reordered. When backlog items are completed, remove them from `ROADMAP.md` rather than marking them as completed in the text.
- `GLOSSARY.md`: When domain language, rules concepts, or terminology are introduced, modified, or retired.
- `VISION.md`: When product destination, architectural invariants, capabilities, or anti-goals shift.
