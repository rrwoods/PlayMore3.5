## Agent skills

### Issue tracker

Issues are tracked in GitHub Issues using the `gh` CLI. See `docs/agents/issue-tracker.md`.

### Triage labels

Canonical triage roles mapped to matching labels (`needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`). See `docs/agents/triage-labels.md`.

### Domain docs

Single-context layout (`GLOSSARY.md` and `docs/adr/` at the repo root) for project and engine vocabulary, plus progressively disclosed 3.5e rules and content citations in `docs/rules/`. See `docs/agents/domain.md`, `docs/agents/rules-template.md`, and `docs/agents/subsystems.md`.

### Sourcebooks and PDF extraction

Consulting rules in primary source PDFs (environment, PyMuPDF, image rendering, and scratch scripts): see `docs/agents/sourcebooks.md`.

### Product vision

Product destination, architectural invariants, capability frontiers, and anti-goals: see `VISION.md`.

### Coding standards

Sole source of truth for code review: `CODING_STANDARDS.md` (C# architecture, WinForms layout and `Designer.cs` conventions, `.csproj` `<SubType>` rules, and testing practices).

### Planning and pre-commit gates

Rules provenance, scope audits, prototype promotion, and documentation gates before committing or marking a PR ready: see `docs/agents/gates.md`.

## Workflow and git conventions

### Git conventions

Prefer linear git history:
- Line endings: Automated pre-commit hook enforces LF (`scripts/check-lf.ps1`).
- Rebase working branches onto the target base branch (`git rebase <base>`) rather than creating merge commits.
- Integrate branch commits via fast-forward merges (`git merge --ff-only <branch>`). Avoid `--no-ff` merge commits where possible.
- When a task graph has a single active frontier (strictly sequential dependencies), work directly on the branch without creating extra worktree indirection. Still dispatch an implementer subagent per ticket (inheriting the workspace) to keep the orchestrator's context window lean.
- **Synchronous commit execution**: Set `WaitMsBeforeAsync: 10000` when executing `git commit` so pre-commit test hooks complete synchronously.
- **Reactive wakeup on background tasks**: When a command runs as an asynchronous background task or a subagent is launched, immediately yield the turn without querying `manage_task status` or calling `schedule`. Rely exclusively on the system's reactive wakeup message to resume execution.

### Pull requests

When authoring pull request descriptions with Mermaid diagrams:
- Always wrap Mermaid edge labels in double quotes (e.g. `-->|"Read/Write Mutation"|`). Unquoted slashes, brackets, or parentheses break GitHub's Mermaid parser.

## Rules arbitration invariant

Unrecorded D&D 3.5e game rules are **Domain Specifications**, while verified citations recorded in `docs/rules/` are **Environmental Facts** (see `GLOSSARY.md`). This invariant binds **all chat turns, grilling rounds, recommendations, planning, and code**:
- **Applies to mechanics and game content only**: Binds actual game rules logic and content definitions (math formulas, stacking, prerequisites, modifiers, subsystem interactions, and `PlayMore35/Resources/*.json` entries). Does not apply to application presentation schema or UI layout categorization (e.g. Column 0 `Exploration` or `Social` section groupings).
- **Model blindness**: Treat model pretraining as blind to 3.5e mechanics. Game rules originate exclusively from human-verified primary sources.
- **Two-state resolution & write-back**: Populated citations in `docs/rules/` are Environmental Facts to use directly. `TODO` stubs and absent entries are unarbitrated Domain Specifications requiring human arbitration; write citations back to `docs/rules/` immediately upon arbitration.
- **Procedures and gates**: Progressive disclosure lookup paths (`docs/rules/mechanics/`, `docs/rules/subsystems/`, `docs/rules/content/`), 300-line file caps, positive grilling recommendation tokens, and phase gate requirements (`/to-spec`, `/tdd`, `/code-review`) are documented in `docs/agents/rules-template.md`.
