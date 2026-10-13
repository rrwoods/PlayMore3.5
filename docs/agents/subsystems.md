# Subsystem Design and Implementation Guidelines

Guidelines and architectural principles for implementing D&D 3.5e magic and martial subsystems in PlayMore35.

## 1. Architectural Invariants

Every subsystem implementation must adhere to these five architectural invariants:

1. **Minimal Host Seam (`ISubsystemEngine`)**:
   - `ISubsystemEngine` provides universal host lifecycle, engine identification, and dynamic effect contribution (`Id`, `IsActive`, `Initialize`, `ContributeEffects`).
   - `CharacterSession` hosts active engines uniformly in `SubsystemRegistry` and dispatches `ContributeEffects` during dynamic recomputation without subsystem-specific coupling.
   - Sub-interfaces beneath `ISubsystemEngine` (such as `ISlottedSubsystem`) are prohibited. Subsystem capabilities belong to composed delegates, not engine interface inheritance.

2. **Delegation Over Inheritance**:
   - Subsystem engines manage internal mechanics (such as slots, resource pools, and access validations) by delegating to composed objects, never through class inheritance or engine interface inheritance.
   - Timed expirations on the `Timeline` route through engine-specific or delegate operations rather than a global slotted interface.

3. **Rule-of-Two Extraction (Just-in-Time Delegation)**:
   - Prefactor or extract a shared delegate only when an already-implemented subsystem and the subsystem currently being implemented both require the identical mechanic.
   - Build each new subsystem with cohesive, subsystem-specific domain models tailored to its specific rules. When a second implemented subsystem demonstrates a genuinely shared mechanic, extract the shared delegate at that time.

4. **Native Domain Vocabulary (User / Verb / Element)**:
   - Subsystem engines speak their canonical D&D 3.5e vocabulary across their public APIs, state models, and presentation controls:
     - Spellcaster / casts / spell
     - Initiator / initiates / maneuver
     - Manifester / manifests / power
     - Binder / binds / vestige
     - Meldshaper / shapes / soulmeld
     - Truenamer / speaks / utterance
     - Shadowcaster / casts / mystery
     - Invoker / invokes / invocation
   - Avoid generic names like `UseThing()` or `SubsystemItem`. Keep domain code expressive and faithful to primary source terminology.

5. **Shared Content in Rules Catalog (`IRulesCatalog`)**:
   - Subsystem definition catalogs (`vestiges.json`, `maneuvers.json`, `spells.json`, etc.) load through `IRulesCatalog` so multi-character sessions and DM encounter tracking share immutable rules data without redundant JSON parsing.

6. **Enable, Don't Enforce with Advisory Warning Badges (ADR-0001)**:
   - Selection interfaces filter to eligible elements by default to keep large catalogs navigable, while providing a clear toggle to view all elements.
   - If an element is chosen or slotted that exceeds standard progression access or slot criteria, the system retains the selection and displays a non-blocking Warning Badge.
   - Table exceptions and campaign house rules are declared via the house rules layer or custom feature grants to tell the software that table fiat applies.

---

## 2. Known Subsystem Landscape

PlayMore35 encompasses diverse magic and martial systems across published 3.5e supplements:

| Subsystem | Sourcebook | User | Verb | Subsystem Element | Distinctive Mechanics |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Pact Magic** | *Tome of Magic* | Binder | Binds | Vestige | Binding check DC, good/poor pact state, vestige influence defiance penalties, 24-hour duration. |
| **Martial Maneuvers** | *Tome of Battle* | Initiator | Initiates | Maneuver / Stance | Two-stage slotting (known vs. readied), martial disciplines, maneuver recovery routines, persistent stance toggles. |
| **Vancian Spellcasting** | *Player's Handbook* | Spellcaster | Casts | Spell | Prepared vs. spontaneous casting, per-level spell slot matrices, caster levels, schools/domains, spell components. |
| **Psionics** | *Expanded Psionics Handbook* | Manifester | Manifests | Power | Power point pools, power augmentation scaling, psionic focus expenditure, discipline lists. |
| **Truenaming** | *Tome of Magic* | Truenamer | Speaks | Utterance | Truespeak skill check DCs, Law of Resistance (increasing DC per utterance per day), Law of Sequence. |
| **Shadow Magic** | *Tome of Magic* | Shadowcaster | Casts | Mystery | Fundamental/apprentice/initiator/master mystery paths, progression from arcane spells $\to$ SLAs $\to$ supernatural abilities. |
| **Incarnum** | *Magic of Incarnum* | Meldshaper | Shapes | Soulmeld | Body slot mapping, chakra binds, essentia capacity caps, dynamic essentia investment and reallocation. |
| **Invocations** | *Complete Arcane* / *Dragon Magic* | Invoker | Invokes | Invocation | Grade categorization (least, lesser, greater, dark), at-will execution, 24-hour sustained self-buff toggles. |

*(Note: Other subsystems, such as Words of Creation or Tainted Magic, follow these same guidelines if introduced).*

---

## 3. Open Watch List of Commonality Patterns

The following patterns represent known areas of mechanical overlap observed across 3.5e subsystems. **This is an open watch list, not an exhaustive taxonomy or rigid framework.**

Prefactor or extract a shared delegate only when an already-implemented subsystem and the subsystem currently being implemented both require the identical mechanic:

### 3.1 Slotting & Acceptance Criteria
- **Unrestricted slots**: Binders put vestiges into unrestricted slots up to a capacity derived from character statistics (`number of vestiges`).
- **Level-restricted slots**: Prepared spellcasters assign spells to slots matching or exceeding the spell's level.
- **Body-slot & Chakra mapping**: Meldshapers shape soulmelds to body slots (each soulmeld valid for 1–3 specific slots) and bind them to unlocked chakras.
- **Two-stage slotting**: Initiators populate a "Maneuvers Known" collection, and subsequently ready a subset of those into "Readied Maneuver" combat slots.

### 3.2 Resource Expenditure
- **Expended slotted elements**: Prepared casters and initiators expend a specific assigned slot upon casting or initiating; recovery replenishes the slot.
- **Consumable resource pools**: Spontaneous casters expend daily spell slots; psionic manifesters spend power points from a shared pool.
- **Dynamic resource investment**: Meldshapers allocate and reallocate essentia among shaped soulmelds and feats without consuming the points permanently.
- **At-will / Non-expending**: Invocations and passive vestige abilities execute at-will without depleting a resource pool.

### 3.3 Gated Progression Access
- **Level-capped access**: Caster level, initiator level, and binder level govern the maximum level/grade of elements a character can learn, ready, or bind.
- **Class / List restrictions**: Class spell lists, martial disciplines, and class soulmeld lists restrict what elements can be selected through standard class progression.

### 3.4 Non-Progression Access
- **Feat grants**: Feats grant subsystem access independently of class progression—for example, *Martial Study* grants a maneuver of any discipline within initiator level limits; *Shape Soulmeld* grants access to any class's soulmeld; and *Wild Talent* grants a power point pool without manifester class levels.
- **Class exception features**: Features like Beguiler's *Advanced Learning* grant access to out-of-list spells meeting specific school and level constraints.

---

## 4. Implementation Path: Aligning Code with ADR-0006

Because PlayMore35 currently implements one subsystem (Pact Magic), the following roadmap item brings the codebase into alignment with ADR-0006 before subsequent subsystems are built:

1. **Retire `ISlottedSubsystem`**:
   - `ISlottedSubsystem` imposes a 1D flat slot interface (`SlotCount`, `ClearSlot(int slotIndex)`) onto `ISubsystemEngine`.
   - Reverse this in code by having `PactMagicEngine` manage its slots via an internal delegate. Timeline slot expiration (`SheetAction`) should target engine-specific or delegate operations rather than relying on a global interface.
2. **Retire Unused `ResourcePool`**:
   - `PlayMore35.Subsystems.ResourcePool` was created speculatively and currently has zero production callers.
   - When a resource-spending subsystem (such as Psionics or Spontaneous Casting) is implemented alongside a second consumer, extract a shared resource delegate just-in-time.
3. **Migrate Content Provider into `IRulesCatalog`**:
   - Replace `ISubsystemContentProvider` (which hardcodes vestige lookups) by registering vestige definitions directly into `IRulesCatalog` alongside weapons, feats, and spells.
