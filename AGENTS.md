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
- When a task graph has a single active frontier (strictly sequential dependencies), work directly on the branch without creating extra worktree indirection.

## Planning gates

### Rules arbitration gate

When authoring an implementation plan (`implementation_plan.md`) or preparing to touch game rules (mechanics, formulas, stacking, prerequisites, modifiers, subsystems, or `rules.json`):
- **Never extrapolate from model memory**: The agent is strictly prohibited from inventing, assuming, or remembering D&D 3.5e mechanics.
- **Human arbitration required**: The agent must halt and require the user to provide an authoritative primary source (sourcebook title, page number, and a verbatim quote or screenshot extract of the rule text) before designing or writing code.
- **Plan provenance**: Every planned rules change must include a `Rules Provenance` table documenting the book, page, exact quote, and user confirmation before implementation can begin. See `docs/agents/rules-template.md`.

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
- Verify that no shims, compatibility layers, or features explicitly marked **Out of Scope** or **Anti-goals** were introduced.

## Documentation checklist

Update repository documentation as appropriate:
- `README.md`: When user-facing features, dashboard workflows, status matrix, or setup instructions change.
- `ROADMAP.md`: When backlog items, progression tiers, or feature priorities are started, completed, or reordered.
- `GLOSSARY.md`: When domain language, rules concepts, or terminology are introduced, modified, or retired.
- `VISION.md`: When product destination, architectural invariants, capabilities, or anti-goals shift.
