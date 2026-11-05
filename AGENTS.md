## Agent skills

### Issue tracker

Issues are tracked in GitHub Issues using the `gh` CLI. See `docs/agents/issue-tracker.md`.

### Triage labels

Canonical triage roles mapped to matching labels (`needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`). See `docs/agents/triage-labels.md`.

### Domain docs

Single-context layout (`GLOSSARY.md` and `docs/adr/` at the repo root) for project and engine vocabulary, plus progressively disclosed 3.5e rules and content citations in `docs/rules/`. See `docs/agents/domain.md`, `docs/agents/rules-template.md`, and `docs/agents/subsystems.md`.

### Sourcebooks, PDF extraction, and markdown archive

Consulting rules in primary source PDFs and the external verbatim markdown archive (`rules-md`), 3-phase extraction procedure, and extraction tooling: see `docs/agents/sourcebooks.md`. To extract and archive new sourcebook chapters, invoke the `/extract-sourcebook` skill. To distill verbatim text from `rules-md` into canonical game mechanics in `docs/rules`, invoke the `/distill-rules` skill. To crosscheck and sanity-check distilled rules against `rules-md`, invoke the `/verify-rules` skill.

### Product vision

Product destination, architectural invariants, capability frontiers, and anti-goals: see `VISION.md`.

### Coding standards

Sole source of truth for code review: `CODING_STANDARDS.md` (C# architecture, WinForms layout and `Designer.cs` conventions, `.csproj` `<SubType>` rules, and testing practices).

### Planning and pre-commit gates

Rules provenance, scope audits, prototype promotion, dead code audits, and documentation gates before committing or marking a PR ready: see `docs/agents/gates.md`.

### Dead code scanner

Roslyn call-graph reachability scanner for inspecting unreachable methods and zombie islands (`dotnet run --project tools/DeadCodeScanner`). Strictly separates diagnostic findings from human-confirmed resolution. See `docs/agents/dead-code.md` and `docs/agents/gates.md`.

## Workflow and git conventions

### Git conventions

Prefer linear git history:
- Line endings: Automated pre-commit hook enforces LF (`scripts/check-lf.ps1`).
- Rebase working branches onto the target base branch (`git rebase <base>`) rather than creating merge commits.
- Integrate branch commits via fast-forward merges (`git merge --ff-only <branch>`). Avoid `--no-ff` merge commits where possible.
- When a task graph has a single active frontier (strictly sequential dependencies), work directly on the branch without creating extra worktree indirection. Still dispatch an implementer subagent per ticket (inheriting the workspace) to keep the orchestrator's context window lean.
- **Synchronous commit execution**: Set `WaitMsBeforeAsync: 10000` when executing `git commit` so pre-commit test hooks complete synchronously.
- **Reactive wakeup on background tasks**: When a command runs as an asynchronous background task or a subagent is launched, immediately yield the turn without querying `manage_task status` or calling `schedule`. Rely exclusively on the system's reactive wakeup message to resume execution.

### Test execution and diagnostics

- **Sandbox bypass for WinForms test runs**: Compiling Windows Forms `.resx` resources in `.NET Framework 4.8` spawns an out-of-process 32-bit `MSBuild.exe` task host (`GenerateResource`). Run `dotnet test` and `dotnet build` with `BypassSandbox: true` to prevent `MSB4216` sandbox process creation errors.
- **Bound test runs with timeouts**: Include a hang-timeout flag (such as `--blame-hang-timeout 15s`) when invoking `dotnet test` so test suites return deterministic results quickly.
- **Target specific fixtures during iteration**: When reproducing a bug or implementing a single ticket, focus execution on the relevant test method or class using `--filter <TargetTest>` to keep iteration loops fast.
- **Treat prolonged test runs as re-entrant loops**: When a unit test takes longer than 15 seconds on a small test assembly, stop the process, inspect the control's event handlers for circular notification cascades (such as mutually triggering radio button or check state updates), and wrap programmatic state mutations in an update guard flag.

### State exploration and search discipline

- **Treat first-search negative results as proof of absence**: When searching for an element, identifier, or control named in a ticket specification, treat the first empty search result as confirmation that the element does not yet exist. Immediately proceed to write the red test and implement it.
- **Chain searches exclusively to newly discovered references**: Restrict every follow-up query to a specific symbol, type, or file path discovered in the immediately preceding tool output.
- **Pivot or escalate on negative discovery**: Exploration serves solely to locate active seams. When two consecutive inspection queries return no new references, or when a ticket refers to a concept absent from the codebase, immediately choose between two actions: proceed to implement the new capability test-first, or halt tool execution and ask the user to clarify the intended design seam.
- **Inspect exclusively after state transitions**: Schedule verification and inspection tool calls (`git status`, `git grep`, file reads) after performing a mutating action (`edit_file`, `write_file`, or command execution).

### Pull requests

When authoring pull request descriptions with Mermaid diagrams:
- Always wrap Mermaid edge labels in double quotes (e.g. `-->|"Read/Write Mutation"|`). Unquoted slashes, brackets, or parentheses break GitHub's Mermaid parser.

## Rules arbitration invariant

Unrecorded D&D 3.5e game rules are **Domain Specifications**, while verified citations recorded in `docs/rules/` are **Environmental Facts** (see `GLOSSARY.md`). This invariant binds **all chat turns, grilling rounds, recommendations, planning, and code**:
- **Primary source exclusivity & extraction boundary**: Primary source PDFs in the reference library are the authoritative source of truth for extracting verbatim text into the markdown archive (`rules-md/`) via `/extract-sourcebook`. Once archived, the verbatim markdown mirror is the complete point of truth for distillation (`/distill-rules`) and verification (`/verify-rules`). When a requested sourcebook PDF is inaccessible, uncalibrated, or missing during extraction, immediately stop and surface the access issue to the user. All rules documentation and engine mechanics derive directly from verified verbatim sources or human arbitration.
- **Applies to mechanics and game content only**: Binds actual game rules logic and content definitions (math formulas, stacking, prerequisites, modifiers, subsystem interactions, and `PlayMore35/Resources/*.json` entries). Does not apply to application presentation schema or UI layout categorization (e.g. Column 0 `Exploration` or `Social` section groupings).
- **Model blindness**: Treat model pretraining as blind to 3.5e mechanics. Game rules originate exclusively from human-verified primary sources.
- **Two-state resolution & write-back**: Populated citations in `docs/rules/` are Environmental Facts to use directly. `TODO` stubs and absent entries are unarbitrated Domain Specifications requiring human arbitration; write citations back to `docs/rules/` immediately upon arbitration.
- **Procedures and gates**: Progressive disclosure lookup paths (`docs/rules/mechanics/`, `docs/rules/subsystems/`, `docs/rules/content/`), 300-line file caps, positive grilling recommendation tokens, and phase gate requirements (`/to-spec`, `/tdd`, `/code-review`) are documented in `docs/agents/rules-template.md`.
