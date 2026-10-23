# Declarative Subsystems and Primitive-Verb Engine

## Context
Dungeons & Dragons 3.5e contains a closed, known taxonomy of magic and martial subsystems (Pact Magic, Martial Maneuvers, Vancian Spellcasting, Psionics, Incarnum, Invocations, Shadow Magic, Truenaming). Under previous architecture (the deleted ADR-0006 "Rule of Two"), subsystems were implemented as bespoke, siloed C# classes (`MartialManeuverEngine`, `PactMagicEngine`). This approach accumulated severe class literacy leaks, hardcoding class names (`RecoverWarblade`, `RecoverSwordsage`, `DealCrusader`), static discipline tables in C# source, and asymmetric class schemas (`"vestiges"` vs `"subsystems"`), directly violating ADR-0002 (Systems and Content Separation).

Attempts to generalize by creating subsystem-level recovery verbs (e.g., `recover_all`, `recover_one` inside `maneuvers.json`) simply migrated class-specific silos from C# into subsystem catalogs. Furthermore, polymorphic `SheetAction` subclasses (`UnbindVestigeAction`, `ManeuverSheetAction`) bypassed serialization and treated the timeline as an ambient state store.

## Decision
We replace all bespoke subsystem engines, interface shims, and class-specific recovery methods with a unified, four-tier architecture driven by declarative configuration over domain-agnostic C# primitives:

1. **Single Unified Concrete Engine (`SubsystemEngine`)**:
   - Delete `MartialManeuverEngine`, `PactMagicEngine`, and the 1:1 pass-through interface `ISubsystemEngine`.
   - All subsystems are instantiated as instances of a single, content-blind `SubsystemEngine`.
   - The engine provides four fundamental C# primitives:
     - **Slot State Transitions**: Generic state changes across slot groups supporting targeting selectors: `invoked` (the triggered slot), `all` (all slots matching `from`), `select` (user-chosen slot), and `random` (stochastic transfer).
     - **Element Verb Invocation**: Explicit invocation of an element-level verb (e.g., `execute`) on an assigned element, falling back to subsystem element verb defaults if not declared on the element.
     - **Resource Pool Operations**: Clamped numeric pool operations (`consume`, `recharge`, `set`) against dynamic capacity statistics.
     - **Effect Projection**: Queries assigned elements for specific named categories of effects when a slot matches a required state.

2. **Authoritative Subsystem Catalogs (`maneuvers.json`, `vestiges.json`)**:
   - Subsystem files package both their systemic definition (`"subsystem"`) and their catalog items (`"elements"`).
   - The subsystem definition declares slot groups (and valid states), generic subsystem verbs, general actions, and effect projection rules.
   - The maneuvers subsystem defines the generic `expend` verb as two discrete declarative steps: 1) transition the invoked slot to `"expended"`, and 2) explicitly invoke the maneuver's `"execute"` verb. Subsystems declare default element verbs (such as a default `"execute"` verb in `maneuvers.json` that does nothing).
   - Effect projection rules map slot states to element effect properties (e.g., Stance `"active"` projects `"benefits"`; Vestige `"good_pact"` projects `"abilities"`; Vestige `"poor_pact"` projects `"abilities"` and `"influence"`).

3. **Class-Owned Mechanics and Source Verbs (`classes.json`)**:
   - Subsystem recovery and draw cycles belong to class features, not subsystem catalogs.
   - Each adept class defines a `"ready"` verb that directly invokes the engine's slot state change verb (Warblade/Swordsage transition selected maneuvers to `"available"`; Crusader transitions selected maneuvers to `"withheld"`).
   - Recovery mechanics are standard class features that generate `SheetAction`s calling the engine's slot state change verb (Warblade swift recovery transitions `all` expended $\to$ `available`; Swordsage meditation transitions `select` expended $\to$ `available`; Crusader features manage stochastic dealing and draw cycles).

4. **Unified Element and Slot Persistence**:
   - All slots use a universal representation: `ElementId`, `State`, and `Source`.
   - `Character.Subsystems` serializes uniformly as a map of subsystem IDs to slot groups and numeric pools. All hardcoded subsystem string branches are removed from `Character.cs`.
   - Elements are pure declarative data; their effects are categorized by category under `effects` (`benefits`, `abilities`, `influence`), with optional element verbs (such as an `execute` SheetAction for duration-based boosts and counters) falling back to subsystem element verb defaults.

5. **Deepened `SheetAction`**:
   - `SheetAction` is the single mutation model for both instantaneous mutations and duration-based reversions, eliminating all polymorphic subsystem action subclasses.

## Considered Options
- **Bespoke C# Subsystem Engines (ADR-0010 model)**: Rejected. Introduces class literacy leaks, hardcodes mechanics in C#, and requires engine recompilation for new subsystems.
- **Pass-Through Interface (`ISubsystemEngine`)**: Rejected under the Falsifiable Deletion Test. With a single concrete `SubsystemEngine` handling all subsystems via data, the interface has zero alternate implementations and adds pure indirection.
- **Subsystem-Level Recovery Verbs (`recover_all`, `recover_one` in `maneuvers.json`)**: Rejected. Subsystems should not know how specific classes recover maneuvers; classes own their recovery mechanics by calling engine primitives.
- **Polymorphic Action Subclasses (`UnbindVestigeAction`)**: Rejected. Imperative subclasses break JSON transparency and bypass the declarative action pipeline.
- **Ambient Subsystem Reset Cadences (`reset_cadence` on pools)**: Rejected. Conflated timeline clock parameter decay with subsystem lifecycles. Subsystems in 3.5e have heterogeneous, action-driven lifecycles (vestige 24-hour timeline unbinding, maneuver discrete recovery actions, Vancian rest & study actions). `SubsystemEngine` does not perform ambient resets; pools and slots are reset exclusively via discrete player `SheetAction`s, scheduled timeline reversions, or persistent sheet actions.

## Consequences
- Completely eliminates class literacy leaks from C# engines; core engine code knows zero class, discipline, or subsystem element names.
- Subsystems and class participation are 100% data-driven, allowing homebrew and future subsystems (Psionics, Incarnum) without C# engine changes.
- Upholds Enable, Don't Enforce (ADR-0001) and Systems & Content Separation (ADR-0002).
- Supersedes the recovery and engine structure of ADR-0010 while preserving its slot lifecycle states and dynamic level gating rules.
