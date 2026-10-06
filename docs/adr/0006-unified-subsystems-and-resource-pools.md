# Unified Subsystems and Resource Pools Architecture

## Context
Non-core magic and martial disciplines in Dungeons & Dragons 3.5e (Pact Magic, Tome of Battle Maneuvers, Vancian Spellcasting, Invocations, Meldshaping) share common systemic patterns—specifically effect-in-slot configurations and bounded resource pools—while each having idiosyncratic domain rules (binding checks, pact influence, defiance penalties, recovery maneuvers, preparation matrices).

Previously, Pact Magic (Vestiges) was hardcoded with dedicated properties and bespoke methods across five separate modules (`Character`, `Controller`, `EffectPool`, `SheetAction`, and `MainForm`). Repeating this pattern for planned subsystems on the roadmap would multiply boilerplate, balloon `Character` and `Controller`, leak subsystem-specific enums (such as `PactState`), and prevent uniform headless testing.

## Decision
We deepen subsystem mechanics into an encapsulated, composite subsystem architecture:

1. **Role Interface Segregation (`ISubsystemEngine` & `ISlottedSubsystem`)**:
   - `ISubsystemEngine` provides universal lifecycle, identification, and effect contribution (`Id`, `IsActive`, `Initialize`, `ContributeEffects`).
   - `ISlottedSubsystem : ISubsystemEngine` exposes discrete slot clearance (`SlotCount`, `ClearSlot(slotIndex)`), enabling headless expiration on the `Timeline` and `SheetAction` without forcing non-slotted subsystems to implement unused slot members.

2. **Composite Hybrid Model**:
   - Subsystems are domain-specific engines (`PactMagicEngine`, future `ManeuverEngine`) encapsulating their specific game rules, binding checks, and UI queries.
   - Engines compose reusable domain primitives:
     - `SubsystemSlotGroup<TSlot>`: Manages indexed slots up to a capacity derived from character statistics.
     - `ResourcePool`: Manages consumable and replenishable numeric quantities (spend, restore, reset) bound to statistic caps.

3. **Statistic-Driven Activation**:
   - Subsystem availability is determined dynamically by evaluating whether capacity statistics exceed zero (e.g., `number of vestiges > 0` for Pact Magic; `readied maneuvers > 0` for Martial Maneuvers). This adheres to ADR-0002 (*Systemic Rules as Statistics*) and supports feats (e.g., *Bind Vestige*, *Martial Study*) and hybrid prestige classes without hardcoding class name checks.

4. **Push-Based Effect Contribution**:
   - `EffectPool` is decoupled from subsystems. During recomputation (`Controller.Reload()`), each active engine pushes its active effects, defiance penalties, stances, and flags into `EffectPool` via `ContributeEffects()`.

5. **Decoupled Character State Persistence**:
   - `Character` stores strongly typed domain states in a `Dictionary<string, ISubsystemState> Subsystems` serialized under a `"subsystems"` JSON container.
   - Subsystem domain models and enums (`PactMagicState`, `PactState`) reside entirely within their respective subsystem modules under `PlayMore35.Subsystems.*`.

6. **Engine Registry on `ICharacterContext` & `Controller`**:
   - `ICharacterContext` and `Controller` expose a `SubsystemRegistry` (`GetSubsystem<T>()` / `GetSubsystem(string id)`).
   - Bespoke per-subsystem fields and forwarding methods are purged from `Character` and `Controller`. Presentation layers and tests interact directly with the specific engine instance.

7. **Scoped Content Provider Seam**:
   - Subsystem catalog definitions (e.g., `vestiges.json`) are resolved through an `ISubsystemContentProvider` seam, abstracting filesystem paths and embedded assembly resources away from game engine logic.

## Consequences
- Shrinks the public API of `Character` and `Controller`, concentrating subsystem mechanics behind modular seams.
- Provides headless, testable units for individual subsystems without instantiating Windows Forms.
- Establishes a uniform template for upcoming subsystems (Tome of Battle Maneuvers, Vancian Spellcasting, Invocations, and Incarnum).
- Maintains backward-compatible `SheetAction.ClearSlot(subsystem, slotIndex)` operations via `ISlottedSubsystem`.
