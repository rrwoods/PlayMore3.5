# Domain Docs

How the engineering skills should consume this repo's domain documentation when exploring the codebase.

## Before exploring, read these

- **`GLOSSARY.md`** at the repo root, or
- **`GLOSSARY-MAP.md`** at the repo root if it exists: it points at one `GLOSSARY.md` per context. Read each one relevant to the topic.
- **`docs/adr/`**: read ADRs that touch the area you're about to work in. In multi-context repos, also check `src/<context>/docs/adr/` for context-scoped decisions.

If any of these files don't exist, **proceed silently**. Don't flag their absence; don't suggest creating them upfront. The `/domain-modeling` skill (reached via `/grill-with-docs` and `/improve-codebase-architecture`) creates them lazily when terms or decisions actually get resolved.

## File structure

Single-context repo (most repos):

```
/
├── GLOSSARY.md
├── docs/
│   ├── adr/
│   │   ├── 0001-enable-dont-enforce.md
│   │   └── ...
│   └── rules/
│       ├── mechanics/                  ← systemic 3.5e rules (disclosed on demand)
│       └── content/                    ← specific 3.5e content mirroring Resources/*.json
└── PlayMore35/
```

Multi-context repo (presence of `GLOSSARY-MAP.md` at the root):

```
/
├── GLOSSARY-MAP.md
├── docs/adr/                          ← system-wide decisions
└── src/
    ├── ordering/
    │   ├── GLOSSARY.md
    │   └── docs/adr/                  ← context-specific decisions
    └── billing/
        ├── GLOSSARY.md
        └── docs/adr/
```

## Use the glossary's vocabulary (and keep 3.5e rules in `docs/rules/`)

When your output names a project or engine domain concept (in an issue title, a refactor proposal, a hypothesis, a test name), use the term as defined in `GLOSSARY.md`. Don't drift to synonyms the glossary explicitly avoids.

Keep `GLOSSARY.md` bounded to **project and engine domain concepts** (*Live Companion*, *Statistic*, *Effect*, *Modifier*, *Subsystem*, *Character Session*). Specific D&D 3.5e game rules and content citations belong in `docs/rules/` (`docs/rules/mechanics/` and `docs/rules/content/`) and are loaded via **progressive disclosure** only when a task touches that mechanic or content item (see `docs/agents/rules-template.md`).

If an engine/project concept you need isn't in `GLOSSARY.md` yet, that's a signal: either you're inventing language the project doesn't use (reconsider) or there's a real gap (note it for `/domain-modeling`).

## Flag ADR conflicts

If your output contradicts an existing ADR, surface it explicitly rather than silently overriding:

> _Contradicts ADR-0007 (event-sourced orders), but worth reopening because…_
