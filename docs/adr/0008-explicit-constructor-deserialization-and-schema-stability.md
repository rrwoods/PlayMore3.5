---
status: partially superseded by ADR-0006
---

# Explicit Constructor Deserialization and Schema Stability

## Context
Ticket #83 investigated standardizing polymorphic deserialization across `IJsonable` domain models and `ISubsystemState` using Json.NET `TypeNameHandling`, custom `JsonConverter` implementations, external JSON schemas, or typed discriminators. The intent was to eliminate bespoke property-by-property extraction in `Character` and static factory methods like `Amount.FromJson` and `Toggle.FromJson`.

While automated and declarative serialization looks appealing, rich domain models in a tabletop RPG rules engine have distinct structural requirements:
- Objects must protect invariants at construction time rather than living in uninitialized mutable states.
- Domain entities (`Character`, `EquippedChoices`, `Timeline`, `InventoryLocation`) encapsulate domain logic and calculated state, unlike flat Data Transfer Objects (DTOs).
- Existing persistent character files (`Odric.json`) and core rules definitions (`rules.json`) use human-readable DSL representations without embedded CLR metadata or synthetic discriminator wrappers.

## Decision
We explicitly standardize on **explicit constructor-based deserialization and static factory methods** (`new T(JObject json)`, `T.FromJson(JToken json)`) for all domain entities and rule DSL expressions:

1. **Constructor & Static Factory Encapsulation**:
   - Domain entities deserialize via parameterized constructors `new T(JObject)` or static factory methods `T.FromJson(JToken)`.
   - Domain objects must enforce invariants at construction time and retain `readonly` fields. Parameterless constructors that require two-phase initialization or mutable `ReadJson` methods are prohibited.

2. **Rejection of `TypeNameHandling` and Reflection Routing**:
   - Json.NET `TypeNameHandling` is strictly prohibited. Saved files and rules must never embed CLR type names, assembly names, or C# namespaces.
   - Global reflection-driven polymorphic deserializers are rejected in favor of explicit, context-driven parsing.

3. **Schema Stability and Human-Readable DSLs**:
   - Existing JSON serialization formats (including `Odric.json`, `rules.json`, and `feats.json`) are permanent contracts.
   - The concise DSL structure (e.g., `{"fixed": 5}`, `{"controllable": "Power Attack"}`) is retained without adding mandatory `$type` or `type` wrapper properties.
   - Backward-compatibility shims and JSON fallbacks remain strictly forbidden under repo guardrails.

4. **Engine-Owned Subsystem State Deserialization**:
   > [!WARNING]
   > **Partially Superseded by [ADR-0006](0006-declarative-subsystems-and-primitive-verbs.md)**: Subsystem state is no longer parsed by bespoke engine classes (`PactMagicState.FromJson`). All subsystem state serializes uniformly as a map of subsystem IDs to universal slot groups and numeric pools in `Character.Subsystems`, while preserving explicit constructor deserialization.

   - Subsystem state persistence remains modular per ADR-0006: each `ISubsystemEngine` provides its own explicit static factory method (e.g., `PactMagicState.FromJson(JObject)`).
   - `Character` dispatches subsystem state deserialization explicitly by subsystem identifier without reflection.

## Considered Options

- **Json.NET `TypeNameHandling`**: Rejected. Embedding CLR metadata (`"$type": "PlayMore35.Subsystems..."`) couples persistent character files to C# namespace hierarchies and assembly names, breaking files upon class refactoring and introducing security risks.
- **External JSON Schema & DTO Code Generation**: Rejected. Generating anemic DTOs from external schemas requires maintaining an extensive adapter and mapping layer across all domain models, violating the repo's anti-shim invariants and creating dual sources of truth.
- **Parameterless Constructors with Mutable `ReadJson`**: Rejected. Eliminating parameterized constructors breaks domain encapsulation, forces `readonly` collections to become mutable, and permits objects to exist in invalid, half-initialized states.
- **Uniform In-Object Typed Discriminators**: Rejected. Enforcing an explicit discriminator property on all polymorphic objects would break existing saves and rule assets or require forbidden compatibility shims.

## Consequences
- Domain entities retain parameterized constructors, guaranteeing valid invariants and immutability upon construction.
- Existing save files and rules definitions remain 100% stable with zero migrations or shims.
- Subsystem state deserialization remains cleanly co-located within each subsystem's module.
- Ticket #83 is closed as `wontfix`.

## Addendum: Canonical Domain Constructors and Adapter Deserialization (Ticket #247)

Following Ticket #247, domain entities and subsystem configuration models expose **public canonical C# constructors** with optional default parameters rather than relying solely on JSON deserialization constructors:

1. **Canonical C# Constructors as the Primary Seam**:
   - Domain entities (`Character`, `Timeline`, `EquippedChoices`, `InventoryLocation`) and subsystem configurations (`SubsystemConfig`, `SlotGroupConfig`, `PoolConfig`) expose public canonical C# constructors taking strongly typed domain parameters.
   - Optional parameters with sensible defaults (e.g., empty collections, default enums, 0/1 counts) provide expressive, readable instantiation in production code, subsystem engines, and unit tests without combinatorial constructor overloads.

2. **JSON Deserialization Constructors as Delegating Adapters**:
   - Deserialization constructors (`new T(JObject json)`) act as thin serialization adapters that parse JSON properties and forward extracted values to the canonical constructor via constructor chaining (`: this(...)`).
   - Invariants and validation live exclusively in the canonical constructor, ensuring consistent domain state regardless of whether an instance is hydrated from disk or constructed in memory.

3. **Prohibition of Synthetic JSON Test Factories**:
   - Constructing synthetic JSON payloads in unit tests to instantiate domain objects (such as the former `TestFactory`) is strictly prohibited.
   - Tests instantiate domain entities directly using their canonical C# constructors, avoiding JSON serialization overhead, string key fragility, and artificial coupling to serialization representations in test suites.
