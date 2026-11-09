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
   - **True Dead Code**: Obsolete, superseded logic ready for pruning (including uncalled parameterless constructors and obsolete event handlers).
   - **Test Seam**: Logic that exists solely to be called by tests (in-memory constructors, test inspection helpers, test automation hooks). Production assemblies must not carry test-only scaffolding; flag these as candidates for relocation to `PlayMore35.Tests`.
   - **Unhooked Seam**: Real feature logic with test coverage waiting for UI or session integration.
   - **Scanner Blindspot**: Unindexed syntax forms (such as static field declarations). Note that constructor initializer chaining (`: this(...)`) is fully indexed by Roslyn's `ConstructorInitializerSyntax` and is reachable from production roots when the calling constructor is live.
5. **Ask the user to confirm the intended resolution**: Present findings and classifications, then wait for human direction before editing or pruning any code.

---

## Explicit Construction & Constructor Trust

All domain hydration in this codebase is explicit C# code via canonical constructors and delegating `new T(JObject json)` constructors or static factory methods (see `CODING_STANDARDS.md` and `docs/adr/0008-explicit-constructor-deserialization-and-schema-stability.md`). Production never uses reflection-based activation or implicit deserialization.

Domain entities and subsystem definitions expose public canonical C# constructors with optional parameters. JSON deserialization constructors delegate into these canonical constructors via constructor chaining (`: this(...)`). Because Roslyn's `ConstructorInitializerSyntax` is explicitly indexed by the scanner, constructor-chained canonical constructors are recognized as live and reachable whenever their JSON adapter constructors are reachable from production roots.

Furthermore, unit tests construct domain models directly using their canonical C# constructors; ad-hoc synthetic JSON test factories (like the former `TestFactory`) are prohibited.

Because all production construction paths exist explicitly in the Roslyn syntax tree, **trust the scanner's reachability findings on constructors**:

- **Canonical & chained constructors**: When a JSON deserialization constructor (`new T(JObject)`) is reachable from production roots, its constructor-chained target (`: this(...)`) is indexed via `ConstructorInitializerSyntax` and marked live. Canonical constructors called from production or chained from live deserializers are first-class production code.
- **Parameterless constructors** with zero callers are **True Dead Code** (or **Test Seams** if called exclusively by `FakeRulesCatalog` object initializers). They are never hidden JSON deserialization points.
- **Positional constructors** unreached by production roots (and not chained from production deserializers) are candidates to be pruned if obsolete or moved to test helpers. Note that synthetic JSON test factories (which built fake `JObject` payloads to invoke deserialization constructors in tests) are prohibited in favor of direct canonical constructor calls.

---

## Core Concepts & Vocabulary

- **Production Roots**: Code entry points (`Program.Main`, public `Form` constructors, WinForms/Component lifecycle overrides like `OnLoad`, `Dispose`, public members on UI forms and controls, public presentation models ending in `ViewModel`, and public members of core domain engine contracts and their implementations: `ICharacterSession`, `IRulesCatalog`, `ISubsystemEngine`, `CharacterSession`, `GameRulesCatalog`, `SubsystemEngine`, `Timeline`, `Effect`, `EffectPool`, `ModifierTypes`, `SubsystemDefinition`, `TaxonomyEntry`, `SubsystemElement`, `SubsystemSlotGroup`, `SubsystemUIRegistry`).
- **Live Methods**: Transitive call-graph closure reachable from Production Roots (including interface implementations and virtual dispatch).
- **Dead Methods**: Any production method with zero reachable paths from Production Roots.
- **Zombie Island**: A cluster of dead methods that appear active because members call each other (cascades) or unit tests call into them.
- **Artificial Life Support**: Unit tests that target unreachable production methods. Production assemblies must not retain methods solely to satisfy tests. Flag test-only seams as candidates to move into the test package whenever feasible.
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
- **Relocate to Tests**: Move test-only convenience constructors, builder methods, or inspection helpers out of the production project (`PlayMore35`) and into the test project (`PlayMore35.Tests`) as test extension methods or test-assembly helpers, keeping the production surface clean (synthetic JSON test factories remain prohibited).
- **Wire Up**: Connect the island root to its intended UI event handler or production workflow.
- **Expand Roots**: Deepen `ReachabilityAnalyzer.IsProductionRoot` to recognize the dynamic entry point.

### Remediation Ticket Authoring

When creating or authoring tickets to remediate dead code:
- **Phased execution note**: Always place this callout banner at the top of the ticket body:
  > [!NOTE]
  > Phased execution:
  > 1. Directly edit or prune the enumerated targets and run unit tests.
  > 2. Run the dead code scanner gate (`--check`) only after code edits and unit tests pass.
- **Explicit target lists**: Explicitly enumerate the dead production members, entry roots, and artificial life-support tests with file paths so the implementing agent can navigate directly to the seams. **Never cite ordinal island numbers** (e.g. `Island 16`), which are ephemeral and re-index across runs; cite only fully qualified symbols and file paths.
