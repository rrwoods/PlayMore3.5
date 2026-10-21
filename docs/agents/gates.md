# Planning and Pre-commit Gates

Checklists and verification gates enforced before planning, committing code, or marking pull requests ready.

## Gates

Before committing or marking a PR ready:

### 1. Rules provenance gate

Verify rules provenance for any changes touching game mechanics per the Rules Arbitration Invariant (`AGENTS.md` and `docs/agents/rules-template.md`).
- Every modified game rule or `PlayMore35/Resources/*.json` entry must have an authoritative citation in `docs/rules/`.
- Every unit test verifying a game rule must cite the primary source in a comment directly above the test method (`// Source: <Sourcebook Name>, p. <Number>`).

### 2. Scope gate

Audit the diff against the originating issue's **Out of Scope** and **Anti-goals** and `CODING_STANDARDS.md`. Flag any JSON fallback, adapter shim, compatibility layer, or deprecated forwarding constructor as a hard violation.

### 3. Documentation gate

When authoring an `implementation_plan.md` (include under `Proposed Changes`) or before running `git commit`, inspect changes and update repository documentation as appropriate:
- **`docs/rules/`**: When a game rule or content item is arbitrated, replace its Rules Skeleton or add the citation in `docs/rules/mechanics/`, `docs/rules/subsystems/`, or `docs/rules/content/`. When new mechanics or content entries are added without immediate citation, add an unarbitrated Rules Skeleton per the schemas in `docs/agents/rules-template.md` to the matching file.
- **`README.md`**: When user-facing features, dashboard workflows, status matrix, or setup instructions change.
- **`ROADMAP.md`**: When backlog items, progression tiers, or feature priorities are started, completed, or reordered. When backlog items are completed, remove them from `ROADMAP.md` rather than marking them as completed in the text.
- **`GLOSSARY.md`**: When project/engine domain language or architectural concepts are introduced, modified, or retired (keep specific 3.5e rules and game content in `docs/rules/`, never `GLOSSARY.md`).
- **`VISION.md`**: When product destination, architectural invariants, capabilities, or anti-goals shift.

### 4. Prototype promotion gate

When promoting code from a prototype, specify exclusively the chosen design in tickets and specifications. Upstream descriptions and context pointers present only the selected variant so the agent works against a single definitive architecture.
