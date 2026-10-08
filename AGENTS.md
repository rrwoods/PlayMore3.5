## Agent skills

### Issue tracker

Issues are tracked in GitHub Issues using the `gh` CLI. See `docs/agents/issue-tracker.md`.

### Triage labels

Canonical triage roles mapped to matching labels (`needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`). See `docs/agents/triage-labels.md`.

### Domain docs

Single-context layout (`GLOSSARY.md` and `docs/adr/` at the repo root). See `docs/agents/domain.md`.

### Product vision

Product destination, architectural invariants, capability frontiers, and anti-goals: see `VISION.md`.

## Formatting and conventions

### Line endings

All text files in this repository (including `*.md`, `*.cs`, `*.csv`, and `*.ps1`) must use Windows CRLF (`\r\n`) line endings as configured in `.gitattributes`. When creating or editing files, always ensure CRLF line endings are used.

To verify line endings across the repository:
```powershell
powershell -ExecutionPolicy Bypass -File scripts/check-crlf.ps1
```

To automatically fix any non-CRLF line endings:
```powershell
powershell -ExecutionPolicy Bypass -File scripts/check-crlf.ps1 -Fix
```

### Git conventions

Prefer linear git history:
- Rebase working branches onto the target base branch (`git rebase <base>`) rather than creating merge commits.
- Integrate branch commits via fast-forward merges (`git merge --ff-only <branch>`). Avoid `--no-ff` merge commits where possible.
- When a task graph has a single active frontier (strictly sequential dependencies), work directly on the branch without creating extra worktree indirection. Still dispatch an implementer subagent per ticket (inheriting the workspace) to keep the orchestrator's context window lean.

### Pull requests

When authoring pull request descriptions with Mermaid diagrams:
- Always wrap Mermaid edge labels in double quotes (e.g. `-->|"Read/Write Mutation"|`). Unquoted slashes, brackets, or parentheses break GitHub's Mermaid parser.

## Rules arbitration invariant

Game rules (mechanics, formulas, stacking, prerequisites, modifiers, subsystem interactions, and `rules.json` entries) are strictly human-arbitrated. This invariant binds **all chat turns, grilling rounds, recommendations, planning, and code**:
- **Never extrapolate from model memory**: The agent is strictly prohibited from inventing, assuming, remembering, or citing D&D 3.5e mechanics to justify an option, answer a question, or explain a design.
- **Rules are not environmental facts**: The grilling principle "finding facts is your job" applies exclusively to the local environment and codebase (files, git history, tests), never to D&D 3.5e game rules.
- **Conversational & grilling protocol**: When framing questions or recommendations in `/grilling`, `/wayfinder`, or general chat:
  - Never assert unverified rules mechanics to justify a recommendation.
  - If a decision or recommendation hinges on unverified rules mechanics, the recommended answer must either adopt a zero-assumption presentation baseline or explicitly state: `➡️ Recommendation: Defer until human arbitration provides the primary source citation for [Rule].`
- **Human arbitration required**: The agent must halt and require the user to provide an authoritative primary source (sourcebook title, page number, and a verbatim quote or screenshot extract of the rule text) before designing or writing code touching rules.
- **Plan provenance**: Every planned rules change must include a `Rules Provenance` table documenting the book, page, exact quote, and user confirmation before implementation can begin. See `docs/agents/rules-template.md`.

## Planning gates

### Documentation gate

When authoring an implementation plan (`implementation_plan.md`), include planned repository documentation updates under `Proposed Changes` following the [Documentation checklist](#documentation-checklist).

## Pre-commit gates

### Rules provenance gate

Before running `git commit` or marking a PR ready for changes touching game rules:
- Audit all modified rules logic and `rules.json` entries against the user-supplied source quote in the plan or issue.
- Verify that unit tests validating the rule cite the primary source (book and page) in test comments or names.
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
- `ROADMAP.md`: When backlog items, progression tiers, or feature priorities are started, completed, or reordered.
- `GLOSSARY.md`: When domain language, rules concepts, or terminology are introduced, modified, or retired.
- `VISION.md`: When product destination, architectural invariants, capabilities, or anti-goals shift.
