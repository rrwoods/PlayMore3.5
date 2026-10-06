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

### Documentation gate

When authoring an implementation plan (`implementation_plan.md`), include planned updates to repository documentation under `Proposed Changes` as appropriate:
- `README.md`: Include when user-facing features, dashboard workflows, status matrix, or setup instructions change.
- `ROADMAP.md`: Include when backlog items, progression tiers, or feature priorities are started, completed, or reordered.
- `GLOSSARY.md`: Include when domain language, rules concepts, or terminology are introduced, modified, or retired.
- `VISION.md`: Include when product destination, architectural invariants, capabilities, or anti-goals shift.

## Pre-commit gates

### Documentation gate

Before running `git commit`, inspect staged changes and update repository documentation as appropriate:
- `README.md`: Update when user-facing features, dashboard workflows, status matrix, or setup instructions change.
- `ROADMAP.md`: Update when backlog items, progression tiers, or feature priorities are started, completed, or reordered.
- `GLOSSARY.md`: Update when domain language, rules concepts, or terminology are introduced, modified, or retired.
- `VISION.md`: Update when product destination, architectural invariants, capabilities, or anti-goals shift.

### Scope gate

Before running `git commit` or marking a PR ready, audit changes against the originating issue or specification:
- Verify that no shims, compatibility layers, or features explicitly marked **Out of Scope** or **Anti-goals** were introduced.
