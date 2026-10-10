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

### Line endings

All text files in this repository (including `*.md`, `*.cs`, `*.csv`, and `*.ps1`) must use Windows CRLF (`\r\n`) line endings as configured in `.gitattributes`. The Git `pre-commit` hook (`scripts/install-hooks.ps1`) automatically runs `scripts/check-crlf.ps1 -Fix` and `dotnet test` on `git commit`, so you do not need to run `scripts/check-crlf.ps1 -Fix` manually before committing.

To install or update the Git hooks (`pre-commit` and `commit-msg`):
```powershell
powershell -ExecutionPolicy Bypass -File scripts/install-hooks.ps1
```

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

Game rules (mechanics, formulas, stacking, prerequisites, modifiers, subsystem interactions, and `rules.json` entries) are strictly human-arbitrated. This invariant binds **all chat turns, grilling rounds, recommendations, planning, and code**:
- **Never extrapolate from model memory**: The agent is strictly prohibited from inventing, assuming, remembering, or citing D&D 3.5e mechanics to justify an option, answer a question, or explain a design.
- **Rules are not environmental facts**: The grilling principle "finding facts is your job" applies exclusively to the local environment and codebase (files, git history, tests), never to D&D 3.5e game rules.
- **Conversational & grilling protocol**: When framing questions or recommendations in `/grilling`, `/wayfinder`, or general chat:
  - Never assert unverified rules mechanics to justify a recommendation.
  - If a decision or recommendation hinges on unverified rules mechanics, the recommended answer must either adopt a zero-assumption presentation baseline or explicitly state: `➡️ Recommendation: Defer until human arbitration provides the primary source citation for [Rule].`
- **Human arbitration required**: The agent must halt and require the user to provide an authoritative primary source (sourcebook title, page number, and a verbatim quote or screenshot extract of the rule text) before designing or writing code touching rules.
- **Plan provenance**: Every planned rules change must include a `Rules Provenance` table documenting the book, page, exact quote, and user confirmation before implementation can begin. See `docs/agents/rules-template.md`.

## Planning and pre-commit gates

- **Rules provenance and scope gates**: Before committing or marking a PR ready, verify rules provenance and anti-shim scope compliance per `CODING_STANDARDS.md`.
- **Documentation gate**: When authoring an `implementation_plan.md` or before running `git commit`, update repository documentation following the [Documentation checklist](#documentation-checklist).

## Documentation checklist

Update repository documentation as appropriate:
- `README.md`: When user-facing features, dashboard workflows, status matrix, or setup instructions change.
- `ROADMAP.md`: When backlog items, progression tiers, or feature priorities are started, completed, or reordered. When backlog items are completed, remove them from `ROADMAP.md` rather than marking them as completed in the text.
- `GLOSSARY.md`: When domain language, rules concepts, or terminology are introduced, modified, or retired.
- `VISION.md`: When product destination, architectural invariants, capabilities, or anti-goals shift.
