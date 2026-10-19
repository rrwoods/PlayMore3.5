# PlayMore35: Product Vision

## 1. Destination (North Star)

**PlayMore35** is an active, in-combat live character companion and DM dashboard for **Dungeons & Dragons 3.5 Edition**.

Unlike static PDF character sheets or offline character builders that focus on character creation, PlayMore35 solves the high-cognitive-load, fluid state tracking of complex D&D 3.5e characters during live tabletop play:
- Tracking dynamic combat stats, temporary buffs, status conditions, and action economy across real-time round progression.
- Modeling intricate, esoteric rules subsystems (Pact Magic, Tome of Battle maneuvers, Invocations, Vancian casting, and Alternate Class Features) with full mathematical transparency.
- Empowering players with a combat-first dashboard at the table, while supporting Dungeon Masters with synchronized encounter tracking, party oversight, and rule-compliant monster advancement.

The product's immediate priority is supporting the characters currently played by the author, expanding next to characters played by other members of the gaming group, and ultimately scaling toward universal coverage of published 3.5e content.

---

## 2. Product Invariants (Non-Negotiable Principles)

These core invariants govern every architecture decision, rule calculation, and UI component. They do not bend for convenience:

1. **RAW Fidelity with "Enable, Don't Enforce" (ADR-0001)**:
   - Calculations strictly reflect official D&D 3.5e Rules-as-Written (stacking rules, size scaling, effort categories, proficiencies, prerequisites).
   - Build validation logic informs rather than gates: unfulfilled prerequisites or illegal equipment loadouts surface non-obtrusive advisory warning badges (e.g. hoverable alert icons) but never prohibit choices or block tabletop DM fiat.
2. **Separation of Systems and Content (ADR-0002)**:
   - Systems code (core engine, subsystems, and UI presentation dialogs) and systems files (`rules.json`) operate with zero literacy of specific content names, IDs, or strings, preventing an explosion of special cases.
   - Global thresholds and systemic mechanics (such as the death threshold) are modeled directly as `Statistic` objects within `rules.json`, while content files (`feats.json`, `classes.json`, etc.) configure and parameterize systemic primitives.
3. **Unified Monster & Character Representation (ADR-0003)**:
   - Monsters and player characters share the unified `Character` data model.
   - Base creature statistics progress through Racial Hit Dice (governed by creature types); advancing a monster reuses the standard character leveling, feat selection, and ability increment pipeline.
4. **Dynamic Recomputation Without Caching (ADR-0004)**:
   - Derived statistics and effect results are not cached. Whenever character state, equipped gear, or conditional toggles change, all statistics recompute on demand directly from the effect pool.
   - Eliminates cache invalidation bugs and stale state anomalies while keeping the calculation engine purely deterministic and functional.
5. **Strict Local Isolation for Multi-Character State**:
   - Effects apply to and read from their host character instance exclusively by default.
   - Cross-character interactions (such as Draconic Auras, bardic music, or shared buffs) require explicit scoping to prevent unintended state bleeding across sheets.
6. **Advisory Action Economy**:
   - Action categories (standard, move, swift, immediate, full-round, free) guide the player via advisory highlighting rather than strict turn-locking state machines.

---

## 3. Core Capabilities (Thematic Horizons)

### Capability 1: Subsystems & Rules Engine Expansion
*Deepening the rules engine to handle complex supplemental subsystems, rule alterations, and feat mechanics.*
- **Subsystems Framework**: Generalized engine supporting resource-consuming systems (Vancian Spells, Tome of Battle Maneuvers, Psionics, Mysteries) and effect-in-slot systems (Pact Magic / Vestiges, Meldshaping / Chakras / Essentia, Multiform / Wild Shape).
- **Invocations Support**: Dedicated modeling for Dragonfire Adept draconic invocations by grade (Least, Lesser, Greater, Dark) and sustained 24-hour self-buffs, alongside breath weapon and breath effects. (*Eldritch Blast* and Warlock-specific shapes/essences are deferred).
- **Feat Prerequisites & `GiveFeat` Mechanics**: Formalize prerequisite checking (`HasFeat`, minimum stats) with advisory warnings; support `GiveFeat` effects that add to character feat sets (e.g., Vow of Poverty, racial bonus feats) and allow ignoring prerequisites via effect or DM fiat.
- **Rule Disabling & Text Appending**: Support effects that disable baseline rules (e.g., Deepwarden 2 disabling Dex-to-AC and adding Con-to-AC) and text-appending effects that expand baseline class features (e.g., feats that add clauses to Rage).
- **Combat Nuances**: Variable bonus dice (Sneak Attack, Sudden Strike), critical property modifiers (Keen, Improved Critical, Bonecrusher), shield bash offensive vs. defensive usage toggles, natural attack full routines, and weapon size scaling across all 9 size categories.

### Capability 2: The Live Combat & Exploration Dashboard
*Restructuring the combat interface around the natural cadence of tabletop play.*
- **Section-Based Layout Restructure**:
  - **Section 0 (Exploration & Social)**: Positioned above Section 1; displays character vitals (name, classes, levels, languages, alignment) and key exploration/social skills (e.g. Diplomacy, Gather Information, Survival).
  - **Section 1 (Before Combat)**: Spot/Listen, Initiative, Senses, Evade, Condition, and Auras.
  - **Section 2 (Enemy Turn)**: AC breakdown (touch, flat-footed), HP / temp HP / DR, Saves, Spell Resistance, Immunities, and Immediate reactions.
  - **Section 3 (My Turn)**: Speeds, Base Combat, Attacks, Attack Options, and Action Highlighter (checkboxes for available standard, move, swift actions).
- **Attack Routine Permutations**: Automatically compute valid attack permutations (single attack, full iterative routine, two-handed grip, TWF combinations) with a sensible initial baseline and collapse/expand controls.
- **"All Effects" Inspector**: A dedicated view listing all active, inactive, and conditional effects with toggle overrides.
- **Timeline & Duration Expiration**: Advancing combat rounds automatically ticks duration counters, reverting temporary buffs and queuing follow-up conditions (e.g., post-frenzy fatigue).

### Capability 3: DM Encounter Runner & Multi-Character Oversight
*Expanding the application to support Dungeon Masters running live combat.*
- **Encounter Tracker**: Run multiple combatants (player sheets and lightweight monster trackers) synchronized against a single, shared `Timeline` clock.
- **Party Oversight Dashboard**: At-a-glance view of the party's passive senses (Listen/Spot), active Armor Classes, current HP/conditions, and remaining daily resources.
- **Explicit Scoping Engine**: Ensure all effects and calculations remain strictly isolated to individual combatant instances unless explicitly broadcasted as an aura or external effect.

### Capability 4: Inventory, Equipment, & Wealth Tracking
*Managing physical gear, magic item combinations, and historical party wealth.*
- **Interchangeable Slots & Item Merging**: Support interchangeable ring slots (Ring 1 / Ring 2) and official *Magic Item Compendium* item merging (combining secondary properties at 1.5× cost).
- **Weapon Modes & Augment Crystals**: Assign properties across distinct weapon modes (melee, thrown, projectile), attach/detach augment crystals with advisory level warnings, and support armor spikes / shield bashes.
- **Timestamped Ledger**: Record gold transactions and XP session awards with dual timestamps: in-game timeline dates and real-world calendar timestamps.
- **Container Modeling**: Support named storage locations (backpacks, pouches), custom containers with weight-negation properties (*Bag of Holding*), and no-effect name-only carried items.

### Capability 5: Content Extensibility & Monster Compendium
*Enabling custom content authoring, house rules, and an expandable monster library.*
- **Layered Rules Architecture**: Clean separation between immutable Core Rules (`rules.json`), Campaign House Rules, and Character State.
- **Monster Compendium & Advancer**: Pre-loaded library of printed D&D 3.5e monsters built with Racial Hit Dice, supporting size increases, template additions, and class level advancement.
- **In-App Content Definition**: User interfaces and data schemas to define homebrew classes, feats, items, races, and Alternate Class Features without modifying engine source code.

---

## 4. Out of Scope (Explicit Anti-Goals / The "Fence")

To prevent feature bloat and preserve table-ready speed, the following are explicitly out of scope:

1. **Strict Character Creation Wizards & Build Blocking**:
   - PlayMore35 is an active combat companion, not a character creation wizard. It does not enforce legal class progressions, skill point spending limits, or prerequisite gating.
2. **Full VTT Battlemap & Canvas**:
   - PlayMore35 does not render 2D/3D tactical grids, map tokens, dynamic lighting, or line-of-sight. Map navigation belongs in dedicated virtual tabletops (Roll20, Foundry, Owlbear Rodeo).
3. **Turn Action State Machine Locking**:
   - The sheet never locks or disables ability clicks based on action economy expenditure. Tabletop combat involves interruptions, readied actions, and retcons; action tracking remains advisory.
4. **Mandatory Cloud Sync & Central Server Architecture**:
   - PlayMore35 is a fast, offline-first local desktop application. All character data and house rules live in transparent, user-editable local JSON files.

---

## 5. Integrating with Engineering Flows

When planning new development:
1. **Consult the Staging Queue**: Check [`ROADMAP.md`](./ROADMAP.md) for the active priority ordering (Odric level progression $\to$ current party $\to$ Alth $\to$ general engine).
2. **Map a Capability**: Select the top unblocked feature and invoke `/wayfinder` or `/grill-with-docs` to sharpen the specific design.
3. **Specify**: Run `/to-spec` to produce a buildable, test-backed design specification.
4. **Decompose & Implement**: Run `/to-tickets` to generate tracer-bullet tickets declaring blocking edges, followed by `/implement` (driving `/tdd` and `/code-review`).
