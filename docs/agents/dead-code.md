# Dead Code & Zombie Island Scanner

Deterministic Roslyn call-graph analysis tool to detect unreachable production code and test-sustained zombie islands.

## Core Invariant: Strict Separation of Findings and Resolution

> [!IMPORTANT]
> **Findings and resolution are separate phases.** The dead code scanner is an advisory diagnostic. Agents must confirm findings with a human in the loop before taking any remediation actions.

When an agent is asked to "check for dead code", the task is **discovery, analysis, and reporting**.

---

## What "Check for Dead Code" Means

When instructed to "check for dead code", follow this procedure:

1. **Run the survey scan**:
   ```powershell
   dotnet run --project tools/DeadCodeScanner
   ```
2. **Read the generated report**: Inspect `reports/dead-code-report.md`.
3. **Report findings neutrally**: Present the detected islands, their entry roots, downstream cascades, and artificial life-support tests with file links.
4. **Classify candidates**: For any island under discussion, assess which category best explains its reachability:
   - **True Dead Code**: Obsolete, superseded logic ready for pruning.
   - **Unhooked Seam**: Real feature logic waiting for UI or session integration.
   - **Scanner Blindspot**: Invoked via reflection, JSON serialization, or dynamic events.
   - **Test Seam**: Intentional helper for test harness construction.
5. **Ask the user to confirm the intended resolution**: Present findings and classifications, then wait for human direction before editing or pruning any code.

---

## Core Concepts & Vocabulary

- **Production Roots**: Code entry points (`Program.Main`, public `Form` constructors, and WinForms/Component lifecycle overrides like `OnLoad`, `Dispose`).
- **Live Methods**: Transitive call-graph closure reachable from Production Roots (including interface implementations and virtual dispatch).
- **Dead Methods**: Any production method with zero reachable paths from Production Roots.
- **Zombie Island**: A cluster of dead methods that appear active because members call each other (cascades) or unit tests call into them.
- **Artificial Life Support**: Unit tests that target unreachable production methods. While useful during test-first development, they can mask abandoned features if the code is never wired to production.
- **Isolated Dead Methods**: Standalone dead methods with zero callers and zero test references.

---

## Operating Modes

| Mode | Command | Use Case | Behavior |
| :--- | :--- | :--- | :--- |
| **Survey (Default)** | `dotnet run --project tools/DeadCodeScanner` | Exploration, health checks, diagnostic review | Generates `reports/dead-code-report.md`. Always returns exit code `0`. |
| **Gate (`--check`)** | `dotnet run --project tools/DeadCodeScanner -- --check` | Pre-commit check or CI validation | Returns exit code `1` if any zombie islands exist. |

> [!NOTE]
> Compiling Roslyn trees against WinForms/net48 targets may spawn out-of-process build tasks. In restricted sandbox environments, run with sandbox bypass if project compilation blocks.

---

## Resolution Pathways (Human-Approved Only)

Once the human confirms the desired resolution:

- **Prune**: Delete the dead production methods and remove their artificial life-support tests together in one atomic change.
- **Wire Up**: Connect the island root to its intended UI event handler or production workflow.
- **Expand Roots**: Deepen `ReachabilityAnalyzer.IsProductionRoot` to recognize the dynamic entry point.
