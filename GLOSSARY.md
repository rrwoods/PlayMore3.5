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

**Character Session**:
An isolated, autonomous runtime host managing an active character's live effect evaluation, timeline progression, inventory, and dynamic recalculations during play.
_Avoid_: Controller, character coordinator, character engine

**Enable, Don't Enforce**:
The design principle that rules and constraints are advisory and calculative, never hard-blocking table fiat or house rules.
_Avoid_: Strict validation, hard enforcement, rule blocking

**Warning Badge**:
A non-blocking visual indicator alerting the player that a prerequisite or rule constraint is currently unmet without preventing the choice.
_Avoid_: Error badge, validation failure, blocker

### Rules & State

**Rules Catalog**:
An immutable repository of core and house rule definitions (races, classes, feats, items) shared read-only across character sessions.
_Avoid_: Rules cache, item database, game data

**Core Rules**:
The official 3.5e game rules and content definitions for stats, feats, classes, and items.
_Avoid_: Base system, engine defaults

**House Rules**:
Table-specific or campaign-specific modifications and homebrew definitions that override or extend core rules.
_Avoid_: Mod, patch, variant hack

**Rule Parameter**:
A configurable systemic game constant or calculation formula (such as the death threshold) modeled as a first-class Statistic that may be customized by house rules.
_Avoid_: Engine constant, setting, hardcoding

**Environmental Fact**:
Observable ground truth discovered through local inspection of the repository, including committed source code, tests, rules data, and previously arbitrated citations recorded in the rules library. Finding environmental facts is exclusively the agent's job.
_Avoid_: Model pretraining memory, world knowledge, unverified assumption, unpopulated TODO stub

**Domain Specification**:
Definitive 3.5e game rules mechanics, content definitions, formulas, stacking constraints, and table fiat not yet recorded with a verified citation in the repository, arbitrated exclusively by the human maintainer via authoritative primary sources.
_Avoid_: Environmental fact, agent fact, recalled rule

**Rules Skeleton**:
A structured placeholder entry in `docs/rules/` defining the required metadata fields for an unarbitrated domain specification before human verification.
_Avoid_: Checklist stub, TODO checkbox, rules stub

**Reference Entry**:
A fully populated, verified mechanical entry in `docs/rules/` providing complete environmental facts without task tracking syntax.
_Avoid_: Checked item, populated stub, rule entry

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

**Controllable Parameter**:
A user-editable numeric counter or input on the character sheet (such as damage taken, Power Attack investment, ability uses expended, or vestige defiance count) that feeds dynamically into effect and modifier evaluations.
_Avoid_: Rule parameter, stat input, counter

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

**Subsystem Element**:
A discrete catalogable unit within a subsystem (such as a spell, martial maneuver, psionic power, vestige, soulmeld, invocation, mystery, or utterance) that a character can learn, prepare, ready, bind, shape, or activate. May be shortened to *Element* when the subsystem context is clear.
_Avoid_: Subsystem thing, subsystem ability, subsystem option, subsystem feature

**Progression Access**:
Eligibility for subsystem elements acquired through a character's class progression and governed by level caps or class and discipline lists.
_Avoid_: Class access, standard access, normal progression

**Non-Progression Access**:
Eligibility for specific subsystem elements granted outside normal class progression (such as via feats or special class features) that bypasses or modifies standard class or discipline restrictions.
_Avoid_: Exception grant, direct grant, bonus access

**Subsystem Slot**:
A discrete assignment receptacle within a subsystem (such as a vestige bind slot or readied maneuver slot) holding an assigned subsystem element.
_Avoid_: Bind slot, maneuver slot, slot

**Resource Pool**:
A quantitative allocation of consumable or replenishable subsystem units (such as spell slots per day, power points, or essentia).
_Avoid_: Resource, pool, spell pool

**Vestige Influence**:
A personality quirk and behavioral requirement imposed by a bound vestige during a poor pact, whose defiance incurs cumulative penalties until the pact ends.
_Avoid_: Bad pact effect, vestige penalty, pact flaw

**Sublime Way**:
The martial combat subsystem encompassing maneuvers and stances, classified into nine disciplines and accessed through initiator level.
_Avoid_: Blade magic, combat magic, sword spells

**Maneuver**:
A discrete extraordinary or supernatural combat action (boost, counter, or strike) initiated by a martial adept and temporarily expended until recovered.
_Avoid_: Spell, combat trick, special attack

**Stance**:
A continuous martial combat posture initiated as a swift action that provides ongoing benefits until replaced or rendered helpless; counts as a maneuver solely for prerequisites and level access.
_Avoid_: Maneuver, mode, aura, sustained spell

**Maneuvers Known**:
The permanent repertoire of Sublime Way martial maneuvers learned through class progression or feats, eligible for preparation into readied slots.
_Avoid_: Known slots, maneuver deck, spellbook

**Readied Maneuver**:
A known maneuver assigned to a dedicated subsystem slot during out-of-combat preparation, available for tactical initiation in combat.
_Avoid_: Prepared spell, memorized maneuver

**Granted Maneuver**:
A readied Crusader maneuver randomly dealt from the withheld deck into the active combat hand, available for immediate initiation.
_Avoid_: Drawn card, active maneuver, readied slot

**Withheld Maneuver**:
A readied Crusader maneuver currently remaining in the draw deck that cannot be initiated until dealt into the granted hand.
_Avoid_: Deck card, unready maneuver

**Expended Maneuver**:
A readied maneuver that has been initiated in combat and rendered unavailable until refreshed through a recovery routine or encounter reset.
_Avoid_: Spent card, tapped maneuver, exhausted slot

**Initiator Source**:
The class or feat origin of a character's Sublime Way capabilities (such as Warblade, Crusader, Swordsage, or Feats), governing progression access, initiator level, and recovery routines.
_Avoid_: Adept class, class partition, maneuver group

**Maneuver State**:
The current in-combat lifecycle condition of a known martial maneuver: Unreadied, Withheld, Available, or Expended.
_Avoid_: Maneuver status, card state

**Maneuver Type**:
The mechanical category of a Sublime Way martial power: Strike, Boost, Counter, Stance, or Other.
_Avoid_: Action type, maneuver category

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

### Ledgers & Progression

**GP Transaction**:
A recorded adjustment to a character's gold piece wealth, tracking amount, description, in-game timeline time, and real-world date.
_Avoid_: Gold transaction, coin entry, money transfer

**XP Session Log**:
A recorded experience point grant or deduction associated with a play session, tracking amount, description, in-game timeline time, and real-world date.
_Avoid_: XP transaction, session entry, XP grant

**In-Game Time**:
The fictional campaign date and time on a character's active timeline.
_Avoid_: Clock time, game round time

**Real-World Date**:
The calendar date when a tabletop session or transaction took place in reality.
_Avoid_: Wall clock time, session timestamp, real timestamp
