# Domain Docs

How the engineering skills should consume this repo's domain documentation when exploring the codebase.

## Before exploring, read these

- **`GLOSSARY.md`** at the repo root: project and engine domain concepts.
- **`docs/adr/`**: read ADRs relevant to the area you are about to touch.
- **`docs/rules/`**: progressive-disclosure 3.5e mechanics (`docs/rules/mechanics/`) and content citations (`docs/rules/content/`). See `docs/agents/rules-template.md`.

## Use the glossary's vocabulary (and keep 3.5e rules in `docs/rules/`)

When your output names a project or engine domain concept (in an issue title, a refactor proposal, a hypothesis, a test name), use the term as defined in `GLOSSARY.md`. Don't drift to synonyms the glossary explicitly avoids.

Keep `GLOSSARY.md` bounded to **project and engine domain concepts** (*Live Companion*, *Statistic*, *Effect*, *Modifier*, *Subsystem*, *Character Session*). Specific D&D 3.5e game rules and content citations belong in `docs/rules/` (`docs/rules/mechanics/` and `docs/rules/content/`) and are loaded via **progressive disclosure** only when a task touches that mechanic or content item (see `docs/agents/rules-template.md`).

If an engine/project concept you need isn't in `GLOSSARY.md` yet, that's a signal: either you're inventing language the project doesn't use (reconsider) or there's a real gap (note it for `/domain-modeling`).

## Flag ADR conflicts

If your output contradicts an existing ADR, surface it explicitly rather than silently overriding:

> _Contradicts ADR-0001 (enable don't enforce), but worth reopening because…_
