# Modular Subsystem Engines and Delegation Architecture

## Context
Magic and martial disciplines in Dungeons & Dragons 3.5e (Pact Magic, Tome of Battle Maneuvers, Vancian Spellcasting, Psionics, Truenaming, Shadow Magic, Meldshaping, Invocations) share certain mechanical patterns—such as slotting, resource expenditure, progression access, and non-progression grants—while each having rich, idiosyncratic domain rules. Subsystems require clean modular encapsulation, testable headless units, and decoupled state persistence without creating speculative frameworks or rigid class hierarchies.

## Decision
We structure subsystems around independent engines that plug into the character session lifecycle and compose internal capabilities through delegation:

1. **Minimal Host Seam (`ISubsystemEngine`)**:
   - `ISubsystemEngine` provides universal session lifecycle, identification, and effect contribution (`Id`, `IsActive`, `Initialize`, `ContributeEffects`).
   - `CharacterSession` hosts active engines uniformly via `SubsystemRegistry` and queries `ContributeEffects` during dynamic recomputation without subsystem-specific coupling.
   - Mechanical sub-interfaces beneath `ISubsystemEngine` (such as `ISlottedSubsystem`) are rejected; mechanical capabilities belong to composable delegates, not polymorphic engine interfaces.

2. **Delegation Over Inheritance**:
   - Subsystem engines manage internal mechanics (such as slots, resource pools, and access validations) by delegating to composed objects, never via class inheritance or engine interface inheritance.
   - Timed slot expirations on the `Timeline` route through engine-specific or delegate operations rather than a global 1D slotted interface.

3. **Rule-of-Two Extraction**:
   - Prefactor or extract a shared delegate only when an already-implemented subsystem and the subsystem currently being implemented both require the identical mechanic.
   - Each subsystem begins with cohesive domain models tailored to its rules; commonalities are factored into shared delegates just-in-time when a second concrete consumer proves the contract.

4. **Native Domain Vocabulary**:
   - Subsystem engines speak their canonical 3.5e vocabulary for the *User*, the *Verb*, and the *Subsystem Element* (e.g. `BindVestige`, `InitiateManeuver`, `ReadyManeuver`, `ShapeSoulmeld`, `CastSpell`) across their public APIs, internal models, and presentation controls.

5. **Shared Content in Rules Catalog (`IRulesCatalog`)**:
   - Immutable subsystem content definitions (`vestiges.json`, `maneuvers.json`, `spells.json`) load via `IRulesCatalog`, sharing thread-safe catalog data across concurrent character sessions and encounter tracking without redundant file parsing.

6. **Decoupled Character State Persistence**:
   - `Character` persists subsystem state modularly in `Dictionary<string, ISubsystemState> Subsystems` serialized under a `"subsystems"` JSON container.
   - Each subsystem engine owns its strongly typed state document implementing `ISubsystemState` and `IJsonable` with explicit constructor deserialization.

7. **Statistic-Driven Activation and Advisory Validation**:
   - Subsystem availability is derived dynamically from character statistics (such as `number of vestiges > 0` or `readied maneuvers > 0`).
   - Selection lists default to eligible elements to keep large catalogs navigable while respecting Enable, Don't Enforce (ADR-0001). Choices that exceed standard progression access or slot acceptance criteria surface non-blocking Warning Badges. Table exceptions and house rules are declared via the house rules layer or custom grants.

## Consequences
- Keeps `ISubsystemEngine` minimal, lightweight, and focused purely on session lifecycle and effect contribution.
- Prevents premature abstraction by ensuring shared delegates are created only when two implemented subsystems share a concrete need.
- Retains native D&D 3.5e terminology across all subsystem modules.
- Existing single-subsystem abstractions (`ISlottedSubsystem`, speculative `ResourcePool`) are slated for retirement as upcoming subsystems are introduced.
