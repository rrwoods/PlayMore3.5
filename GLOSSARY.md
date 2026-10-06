# PlayMore35

A dynamic character sheet and combat companion for running Dungeons & Dragons 3.5 Edition characters during live play.

## Language

### Philosophy & Roles

**Live Companion**:
An active, in-combat dashboard that tracks and calculates a character's fluid combat state during a live session.
_Avoid_: Character builder, offline planner, statblock viewer

**Encounter Tracker**:
A live session interface for managing multiple combatants, initiative order, rounds, and shared conditions simultaneously.
_Avoid_: Combat runner, battle manager

**Enable, Don't Enforce**:
The design principle that rules and constraints are advisory and calculative, never hard-blocking table fiat or house rules.
_Avoid_: Strict validation, hard enforcement, rule blocking

**Warning Badge**:
A non-blocking visual indicator alerting the player that a prerequisite or rule constraint is currently unmet without preventing the choice.
_Avoid_: Error badge, validation failure, blocker

### Rules & State

**Core Rules**:
The official 3.5e game rules and content definitions for stats, feats, classes, and items.
_Avoid_: Base system, engine defaults

**House Rules**:
Table-specific or campaign-specific modifications and homebrew definitions that override or extend core rules.
_Avoid_: Mod, patch, variant hack

**Rule Parameter**:
A configurable systemic game constant or calculation formula (such as the death threshold) modeled as a first-class Statistic that may be customized by house rules.
_Avoid_: Engine constant, setting, hardcoding

**Character State**:
The personal saved data of a player character, including permanent choices, current inventory, and transient combat effects.
_Avoid_: Character build, profile, sheet state

**Monster**:
A creature entity modeled using the unified Character representation, starting with Racial Hit Dice and capable of standard progression and advancement.
_Avoid_: NPC statblock, mob, monster definition

**Racial Hit Dice**:
Progression levels granted by a creature's monster type rather than a character class, determining base attack bonus, saves, and hit points.
_Avoid_: Monster levels, racial HD, creature levels

### Effects & Mechanics

**Statistic**:
A numeric or derived game value (such as an ability score, saving throw, armor class, or attack bonus) evaluated dynamically by the rules engine.
_Avoid_: Attribute, value

**Effect**:
A modular rules element that modifies one or more statistics, grants extra attacks, or conveys special capabilities.
_Avoid_: Buff, ability, feature

**Modifier**:
A discrete numeric adjustment to a statistic associated with a specific modifier type and source.
_Avoid_: Bonus, penalty, adjustment

**Modifier Type**:
The formal 3.5e categorization of a modifier (such as enhancement, morale, insight, or size) determining whether it stacks or overlaps.
_Avoid_: Bonus type, category, bonus category

**Toggle**:
A conditional state or switch that determines whether an effect or modifier is currently active.
_Avoid_: Switch, flag, condition

**Timeline**:
The sequential progression of combat rounds and game time that advances the duration of active effects and conditions.
_Avoid_: Clock, round manager

**Sheet Action**:
A scheduled modification that alters character state over a specific duration and automatically reverts upon expiration.
_Avoid_: Timed buff, event, scheduled job

**Invocation**:
An at-will spell-like ability chosen from a fixed grade (least, lesser, greater, dark) that functions as an essence, shape, or sustained effect.
_Avoid_: Spell, SLA, magic trick

**Attack**:
A discrete offensive action evaluated with a single attack roll bonus, threat range, damage dice, and critical multiplier.
_Avoid_: Strike, swing, blow, attack step, attack roll

**Attack Routine**:
An automatically generated combat sequence (such as a single attack, full iterative progression, or two-weapon fighting sequence) evaluated from currently equipped weapons.
_Avoid_: Attack macro, custom attack, attack combo

**Action Economy**:
The categorization and advisory presentation of abilities by action cost (standard, move, swift, immediate, full-round, free) without turn-expenditure locks.
_Avoid_: Action tracker, action lock, turn budget

### Subsystems & Resources

**Subsystem**:
An encapsulated magic or martial discipline (such as Pact Magic, Martial Maneuvers, Vancian Spellcasting, or Meldshaping) governed by specialized preparation, selection, or recovery mechanics.
_Avoid_: System, subsystem engine, magic system

**Subsystem Slot**:
A discrete assignment receptacle within a subsystem (such as a vestige bind slot or readied maneuver slot) holding an active configuration or ability.
_Avoid_: Bind slot, maneuver slot, slot

**Resource Pool**:
A quantitative allocation of consumable or replenishable subsystem units (such as spell slots per day, power points, or readied martial maneuvers).
_Avoid_: Resource, pool, spell pool

### Inventory & Equipment

**Equipped Item**:
An item assigned to a body slot or held in hand that actively projects its effects, modifiers, and properties onto the character.
_Avoid_: Worn gear, active item

**Carried Item**:
An item stored in an inventory location that contributes to total encumbrance weight and wealth without projecting active effects.
_Avoid_: Stored item, unequipped gear, packed item

**Inventory Location**:
A designated storage container or partition (such as a backpack, belt pouch, or saddlebag) holding carried items.
_Avoid_: Container, bag, storage bin
