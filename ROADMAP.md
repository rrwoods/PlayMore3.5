# PlayMore35: Product Roadmap & Backlog Queue

This document serves as the canonical staging queue for PlayMore35 development. It consolidates all legacy backlog notes and planned features into an explicit 4-tier priority hierarchy.

When starting a new feature, select the highest-priority unblocked item from this roadmap and drive it through `/wayfinder`, `/to-spec`, or `/grill-with-docs` into GitHub issues.

---

## Tier 0: Important and Immediate Consequences of Recently-Implemented Features, and Basic Functionality Necessary To Play The Game

### References for current implementations
- Tag all content with source (book and page).  Every subsystem element, item, class, race, feat... everything.  (Providing UI for this comes later.)
- Reference the entirety of Rules Compendium (and create steering that this is always the primary source when it can be cited)
- Full references for all classes in classes.json, even when only part of the class is implemented.
- Full references for all other content with any implementation

### Extend element picker over non-subsystem choices
- taller default element picker.  like 3 more lines in the list of items, and quadruple the height of the details pane
- Feats
- Later, alternate class features

---

## Tier 1: Odric & Core UX Priorities

Features required for the author's primary active character, alongside immediate core dashboard and ledger UX improvements prioritized alongside them.

### Level 5: Maneuvers UI
- /verify-rules maneuvers against the extracted sourcebook
- Known maneuvers and stances is a build-time choice, and should be present in the level details view.  The player should be able to search for maneuvers there, and the text of what comes up should be sourced from docs/rules/content/maneuvers/\*.md.  While we're here, the "..." button that brings up the level details view should get a highlight of some sort whenever there's an unfilled choice for that level (like picking a maneuver known, or the weapon choice for Weapon Focus).
  - We will need to update the Martial Study and Martial Stance choices to use this UI as well.
- Readied maneuvers and current stance is a play-time choice, and we need a new Maneuvers tab (like the Vestiges tab for pact magic).  We need to think A LOT about how to lay this out and what the interaction paradigms are.  The "maneuvers as cards" analogy is very good and we should use it.

### Level 7: Traditional Spellcasting
- **Prerequisite**: Subsystem-projected effects/statistics shouldn't exist when they don't need to
  - Once we add spellcasting, if every spell in the catalog projected its stats and effects at all times, we would have thousands of stats (because there are thousands of spells).  We need some way of only loading a stat or effect into memory when it's relevant to do so.  (this is a /wayfinder task, I don't have a design idea)
- **[ODRIC 7] Vancian Spellcasting System**:
  - Support both prepared (Cleric, Wizard) and spontaneous (Sorcerer, Favored Soul) spellcasting.
  - Tracking spell slots per day by spell level, spells known/prepared, and casting consumption.
  - Derived caster level calculations and spell save DC stats.

### Level 9: Ability Substitutions
- **[ODRIC 9] Ability-Substitution Effects**:
  - Mechanism to tell a statistic: *"when computing value, if you would add the value of X, instead add the value of Y"*.
  - Implementation for Deepwarden 2's *Stonewarden* ("CON replaces DEX to AC"): (1) disable the baseline "DEX to AC" rule, and (2) add CON to AC.
  - When reporting explanation tooltips, reflect the substitution cleanly (e.g. *Weapon Finesse* substituting DEX for STR should omit STR from the explanation rather than showing `+STR -STR +DEX`).
  - StackingRules handling: in `StackModifiers`, if a bonus and a penalty share the same magnitude and source, clean them from the explanation.

### Level 12: Spell Casting-Time Alterations
- **[ODRIC 12] Casting Time Modifiers**:
  - Support feats and effects that modify action requirements for casting spells (e.g., *Battle Blessing* reducing standard/full-round Paladin/divine spells to swift/standard actions).

### Level 13: Subsystem-Advancing Prestige Classes
- **[ODRIC 13] Hybrid Prestige Class Progression**:
  - Support prestige classes that advance subsystem progression without advancing base class features (e.g., *Ruby Knight Vindicator* advancing both divine spellcasting and martial maneuvers).
  - Account for specialized advancement rules (maneuvers progressing by formula vs. spellcasting progressing at standard caster level rates).

### Level 18: Shield Bashing & Defense Tradeoffs
- **[ODRIC 18] Shield Bash AC Penalty & Usage Modes**:
  - Bashing with a shield loses its shield bonus to AC *"until your next action (usually until the next round)"*.
  - Support feats that waive this penalty (*Improved Shield Bash*).
  - Controller/UX state indicating whether a shield is currently wielded defensively or used offensively as an off-hand weapon (analogous to tower shield total cover vs. AC bonus).

### Low-Priority Odric / Binder Mechanics
- **[ODRIC] Vestige Special Requirements**:
  - Store and display special requirements for binding vestiges in the Pact Magic / vestiges interface (e.g. alignment constraints, environmental conditions, or situational binding penalties from *Tome of Magic*).
- **[ODRIC] Vestige Influence Penalties & Defiance Tracking**:
  - Model vestige influence requirements as directives rather than hard compulsions (*Tome of Magic* p. 10). Phrasing like "must" or "can't" functions as a law with penalties for violation; binders remain free-willed and can willingly choose to disobey ("cannot or will not").

---

## Tier 1.5: "Easy" UX and other intermediate tasks

Anything in this tier that turns out not to be easy should be deferred.

### Column 1 skill UX
  - If a column 1 skill (like Diplomacy) has subskill effects (like dream dwarf's bonus to checks against earth creatures):
    - Put an asterisk next to the skill modifier in column 1
    - Indicate in the tooltip that the player should look in the skills section for more details (or, possibly, just show the extra modifier in the tooltip)

### Action Highlighter:
  - Track the player's available actions (default to standard + move + swift at the start of each turn, if no effects are granting extra actions).
  - Highlight usable abilities and attacks (single attack if standard available, full attack if full-round available, etc.) without hard locking.  Gray out non-usable ones.
  - Provide a "use" menu item on the right click menu for every UI item that represents an action the player can take (including attacks) (some actions already have this).  Clicking this menu item consumes the associated action.
  - Instead of "next round", have an "end turn" and "begin turn" button (round counters tick down at start of turn).
  - Highlight immediate actions when it's not the player's turn.
  - Using an immediate action consumes the *upcoming turn's swift action*.

### Better out-of-combat indication:
  - It's easy to forget to start combat before using up abilities.
  - While not in combat, gray out every toggle and text effect that has an action resource.
  - Consider a background color change for when you're in combat?

### Timeline UX:
  - The date isn't lined up with the text on the buttons...

### Chain of calculation:
  - When hovering a stat on the stats page, highlight other stats it affects in one color and stats affecting it in another.  E.g., when hovering Wisdom modifier, highlight Listen and Spot in the first color and Wisdom score in the second color.

### Rounding out Pact Magic:
  - Rushed binding (Tome of Magic p10) (take -10 on the binding check to perform it as a single full-round action)
  - Vestige signs
  - The rest of the binder class (including Suppress Sign)
    - Include {{vestige save DC}} in relevant Vestige abilities
    - 24 hr pact irrevocability (adhering to enable-don't-enforce)
    - Fully document every vestige in the game, in docs/rules/content/vestiges.md (including Zceryll).
    - Fully document all of binder's statistics and class features.
    - Fully implement binder's class features.
      - For Pact Augmentation, invent a way to make game-time choices outside a subsystem.  Dropdown on the stats page in the appropriate area?
  - Eliminate the "EBL for max vestige level" stat.  Implement a way for effects to be conditional on what downstream thing is being computed.  When "max vestige level" is downstream, Improved Binding adds 2 to "effective binder level".

### Vestiges tab cleanup
- It's not clear that "Good" and "Poor" will bind the vestige.  Add a "Binding check:" label, and change the button text to "Succeed" and "Fail"
- Details panel could use some formatting love - probably separate labels for sign, influence, and granted abilities, with bold and all-caps headers
- The user should be able to find vestiges by ability text using the search box (e.g., searching for "swap" should find Savnok, since "swap" appears in the text of Savnok's Move Ally ability)

### Encumbrance and Carrying Capacity visibility:
  - Show the player what their weight limits are, and their current load category

### Miscellaneous:
  - Make "dmg" and "drn" headers for the ability damage and drain boxes

---

## Tier 2: Current Party Members

Features required to support the other players in the active gaming group.

### Dragonfire Adept Subsystems
- **Draconic Invocations**:
  - At-will spell-like abilities categorized by grade: *Least*, *Lesser*, *Greater*, *Dark*.
  - Sustained 24-hour self-buffs (*Endure Exposure*, *See the Unseen*, *Draconic Knowledge*) as active toggles.
  - At-will utility invocations (*Voracious Dispelling*, *Chilling Fog*).
- **Breath Weapon & Breath Effects**:
  - Breath weapon damage scaling and save DC calculation (10 + 1/2 level + CON modifier).
  - Breath effects modifying energy type or applying status conditions (e.g. *Frost Breath*, *Slow Breath*).

### Multi-Character Scoping & Isolation
- **Strict Local Isolation**:
  - By default, all effects apply to and read from only the character they are hosted on.
  - Cross-character interactions (such as Draconic Auras, Marshal minor/major auras, Bardic Music, or shared party buffs) must require explicit scoping to prevent cross-sheet state pollution.

### Encounter & Party Tracking (DM Short-Term Roadmap)
- **Synchronized Combat Clock**: Encounter runner tracking multiple combatants (player sheets and lightweight monster trackers) advancing against a shared `Timeline` clock.
- **Party Oversight Dashboard**: At-a-glance DM summary of party passive senses (Listen/Spot), active ACs, current HP/conditions, and remaining daily resources.

---

## Tier 3: Alth & High-Priority Usability

Features tagged specifically for the character Alth, alongside high-priority custom authoring tools.


#### In-App Effect Authoring (High Priority)
  - In-app UI system for defining new arbitrary effects (structured fields/dropdowns vs. natural language parser converting phrases like `"+1 morale to attack rolls"` into concrete effects).
  - Foundational effect generation layer that unblocks custom item, feat, class, and race authoring.

#### [ALTH] Class Feature Choices
  - Selections tied to class features (e.g., Swordsage discipline focus).

#### [ALTH] Skill Tricks
  - Dedicated "tricks" skill mechanism that creates a slot for skill tricks.
  - `tricks.json` catalog modeling skill trick prerequisites and effects.

#### [ALTH] Augment Crystals
  - Enable-don't-enforce crystal attachment: warn the user if attaching a crystal to an ineligible weapon/armor, but do not block the action.
  - Model crystals as `WeaponProperty Crystal` on `AttackMode`.
  - UI for attaching (drag crystal onto weapon) and detaching (right-click).

#### [ALTH] Weapon Tags in JSON Data
  - Introduce `tags.json` to categorize weapons (e.g. `special monk weapon`, *Tome of Battle* discipline weapons like `shadow hand`, pseudo-light like `rapier`, `tripping`, `disarming`).

---

## Tier 4: General Engine, Usability, & Maintenance

Universal engine improvements, usability enhancements, sanity refactors, and long-term compendium goals.

### 4.1 Core Rules Engine & Mechanics

#### First-class content tagging
- Almost every piece of content that exists (classes, feats, subsystem elements, races, items, weapons, armor, properties) needs a "tags" property defined in the json, to be able to be referred to by various player choice vectors.  For example, when Martial Study asks the player to choose one of their "adept" classes, "adept" should be a tag on each of the three martial adept classes in classes.json.

#### Feat Prerequisites & `GiveFeat` Engine
  - Deep prerequisite module, instead of parsing strings that represent prerequisites.
  - `GiveFeat` effect: adds to an active character feat set; feat framework uses this, and effects are conditional on prerequisites via `HasFeat` check.
  - Case-by-case prerequisite bypass: text-supported reasons (Monk bonus feats) via effect, or table-supported reasons (DM fiat).
  - Feats granting feats (e.g., *Vow of Poverty*).

#### Upgrade Effects
  - "Get [benefit]; if you already had [benefit], instead get [other benefit]" (e.g. Uncanny Dodge stacking into Improved Uncanny Dodge; Leraje vestige proficiency converting to +1 competence attack bonus if already proficient).

#### Variable Extra Dice
  - Variable extra damage dice scaling with level (Rogue sneak attack, Malphas sudden strike, scout skirmish).

#### Critical Characteristic Modifiers
  - Threat range increase (*Improved Critical*, *keen*), multiplier increase (*Deepwood Sniper*), confirmation roll bonus (*Bonecrusher* maneuver).

#### Natural Attacks & Unarmed Strikes
  - Effects generating natural attacks; races JSON specifying them.
  - Manufactured weapons vs. natural attacks (iteratives for manufactured; secondary natural attacks take -5, or -2 with Multiattack; held weapons precluding hand claws).
  - Unarmed strikes generated when at least one hand is free; option to hide/toggle unarmed attacks to avoid flooding sheet.

#### Tower Shield Attack Penalty
  - Enforce official rule: Tower shields impose -2 on ALL attack rolls, not just the shield hand.

#### Formalize DR, SR, and Energy Resistances
  - Convert text-only displays into structured rules pools where highest value applies.
  - SR pool (greatest number applies); Energy Resistance pools per energy type; DR pools keyed by overcoming condition (e.g., DR/magic, DR/evil, DR/--).

#### Creature Types, Subtypes, & Nonabilities
  - Formalize creature types and subtypes in the effect engine, referencing official core rules.
  - Support nonabilities (Construct/Undead CON, Vermin INT) with appropriate mathematical behaviors.

#### Multiform Support (Wild Shape, Polymorph, Hengeyokai)
  - Overwrite assigned ability scores (e.g., "Bear STR") via ability-substitution effects.
  - Handle forms ignoring level increments (RAW polymorph) by zeroing increment stats.
  - Base HP on normal CON using a separate "normal form CON" statistic.

#### Subsystems Framework Generalization
  - Unify resource-consuming subsystems (Psionics / Power Points, Mysteries, Truenaming) vs. effect-in-slot subsystems (Meldshaping / Soulmelds / Chakras / Essentia, Binders).
  - Warlock Subsystem (Deferred): Eldritch Blast attack routine modifications, Eldritch Essences (damage type and status riders), and Blast Shapes (range, burst, cone, chain).

#### Skills System Refinements
  - Support half-ranks for skills (`Character.skillRanks`, ranks text parsing).
  - Trained-only skills: grey out label when ranks equal zero.
  - Skill-like checks: Wild empathy, turning checks modeled as stats displayed under skills.
  - Skill point grant effects (Nymph's Kiss, Open Minded).
  - Cross-class cost alteration effects (Able Learner).
  - Class skill grant effects (Martial Study does this but locks it to feats -- prefactor, then implement e.g. Cleric domains).
  - Movement mode skill bonuses (Climb bonus from climb speed, Swim bonus from swim speed).
  - Rules-embedded situational bonuses (Hide bonuses while invisible).
  - Global skill effects: Effects affecting all skills (sickened, vestige defiance) should not trigger relevance for every skill; display centrally.
  - Space statistic (lookup table on creature size category).

#### Text-Appending Effects**: Support effects that append additional rules clauses or text to existing text effe

#### Epic Base Attack & Save Bonus Mechanics
  - D&D 3.5 epic progression rules (level > 20): Track "non-epic class levels" to determine which levels contribute toward standard base attack bonus and base save bonus progressions versus epic attack bonus and epic save bonus progressions.

#### Esoteric Rules & Mechanics (Monkey Grip & Control Shape)
  - *Monkey Grip*: Support wielding weapons one size category larger than normal with a -2 attack roll penalty.
  - *Control Shape Skill*: Model Control Shape skill checks for afflicted and natural lycanthropes.

### 4.2 Dashboard & Combat UX
*(Note: Section 0 restructuring is prioritized in Tier 1).*

#### Comprehensive Effects View
  - "All Effects" inspector displaying active, inactive, and conditional effects with toggle overrides.
  - Display uncategorized text under Special Qualities.
  - Show/hide controllable parameters based on toggles (matching controllable flags).

#### Combat Movement & Conditions
  - Over heavy load: 0 speed, but if less than double heavy load, add "full-rd at-will: move 5 feet" under movement.
  - Multiline evade display (Darkstalker, Hide in Plain Sight).
  - Variable ActionResource recharge times (prompt for roll result).
  - Unscheduling actions (e.g. Expel Vestige unscheduling unbind actions).
  - Dedicated Aura section (after Senses before Languages). (What is this actually for, again?  Look through official sourcebooks for "Aura" lines in stat blocks, and evaluate whether this is actually needed, or if all the stuff they put here should be in other places.)
  - There's an effect that says "can't run or charge", which is categorized as Movement.  But, the "charging" box under Atk Options is not grayed out.  Make a way for effects to "disable" toggles on the UI -- that is, gray them out; enable-don't-enforce.  Look for other effects that should probably do this.

#### Eliminate LevelDetailsDialog
- Move all its functionality into the Character tab directly
- To prevent the visual explosion of space, make collapsible columns for each choice category (including feats and skills)
- When choosing something that needs more space (like a feat), expand the row to make room for the picker

#### Choices in level details should be able to filter on almost anything  (/wayfinder)
- For example, the feat "Exotic Weapon Proficiency" currently defines its "choice" as "exotic weapon", and that choice list is hardcoded.  Instead, we should be able to say that its choice type is "weapon", and use a separate filter property on the choice to filter it down to weapons which have at least one AttackMode whose Proficiency is exotic.
- Best I can think of here is to store the raw json alongside every instance, and use that for filtering, but this seems very icky.

#### Character Building UX
  - Alternate Class Features (ACFs) support, replacing features or feature groups.
  - Use `TableLayoutPanel` and `FlowLayoutPanel` for class levels and feats layout.
  - Tab order and keyboard navigation across dynamic controls.
  - Lock/unlock ability score assignment (starts locked on load, clickable to unlock/respec).
  - Support removing class levels.

### 4.3 Inventory, Equipment, & Wealth

#### Interchangeable Ring Slots
  - Support items equipping to either Ring 1 or Ring 2 interchangeably; support *Hand of Glory* if needed.

#### Magic Item Merging
  - Combine two same-slot items with official 1.5× surcharge (e.g., *Torc of Displacement* + *Torc of Constitution*).

#### Weapon Properties Across Modes
  - Report weapon modes (melee, second melee, thrown, projectile) and allow properties on specific modes.
  - Allow adding weapon properties to shield bashes and armor spikes.

#### Item Usability & Descriptions
  - Add no-effect name-only items to inventory locations without requiring full catalog definitions.
  - View full item descriptions directly in the UI.

#### Ledger Presentation & Aesthetic Refinements
  - Re-investigate ledger presentation aesthetics (e.g., moving one or both timestamps to tooltips, adding show/hide toggles, or configurable date/time formatting options).

### 4.4 Content & Rules Data

#### Elven Weapon Proficiencies
  - Represent elf weapon proficiencies as effects rather than hardcoded specific feats (DCFS compliant).

#### Specific Feature Scaling
  - Savnok vestige DR scaling with Effective Binder Level.
  - Stunning Fist "per four levels" excluding non-class hit dice.

#### Downtime & Character Option Text Categories
  - Add "Downtime" or "Character Option" text effect category to support meta and out-of-combat feats (e.g. *Leadership*, *Improved Familiar*).

#### Naberius Faster Ability Healing Scheduled Actions
  - Convert Naberius's *Faster Ability Healing* from a passive text entry into scheduled sheet actions on the timeline.

#### Content Creation & House Rules Layer
  - In-app authoring for new items, classes, feats, and races (building upon Tier 3 in-app effect authoring).
  - Separate house rules from core rules; ability to save/load distinct house-rule sets per campaign.
  - Combine core and extended data definitions (load core $\to$ load extended $\to$ save sorted).
  - Contentious rules arbitration: allow users to configure campaign/DM preferences for ambiguous or contentious rules calls (e.g., vestige influence mandates vs. penalties).

#### [BUG] Ghost Touch Weapon Text**: Ensure ghost touch property text renders correctly in the attack/equipment summa

### 4.5 Project Architecture & Sanity

#### Deepen the Timeline & Sheet Action Module ([#74](https://github.com/rrwoods/PlayMore3.5/issues/74))
  - Decouple `Rounds` and `SheetAction` from `MainForm` into a headless, in-process module.
  - Enables headless unit testing and unblocks the synchronized combat clock for the Encounter Tracker.
  - Slices: [#78](https://github.com/rrwoods/PlayMore3.5/issues/78) (Test harness & context seam), [#79](https://github.com/rrwoods/PlayMore3.5/issues/79) (Timeline core & result contract), [#80](https://github.com/rrwoods/PlayMore3.5/issues/80) (SheetAction & generic slots), [#81](https://github.com/rrwoods/PlayMore3.5/issues/81) (Combat transitions & UI rewiring).

#### Modular Subsystem Engines & Delegation Refactor ([#75](https://github.com/rrwoods/PlayMore3.5/issues/75))
  - Restructure subsystems around independent engines with internal delegation rather than global slot/resource interfaces (see Tier 1 prerequisite to Level 5, `docs/adr/0006-unified-subsystems-and-resource-pools.md`, and `docs/agents/subsystems.md`).

#### Deepen the Attack Routine Evaluation Module ([#76](https://github.com/rrwoods/PlayMore3.5/issues/76))
  - Encapsulate combinatorial weapon-hand pairing, TWF penalties, and iterative attack progressions behind an evaluator interface.

#### Deepen the Character Engine Module ([#77](https://github.com/rrwoods/PlayMore3.5/issues/77))
  - Replace the pass-through `Controller` coordinator with a deep character session module emitting clean read-only state projections.
  - Slices: [#94](https://github.com/rrwoods/PlayMore3.5/issues/94) (Domain primitives & catalog extraction), [#95](https://github.com/rrwoods/PlayMore3.5/issues/95) (CharacterSession engine), [#96](https://github.com/rrwoods/PlayMore3.5/issues/96) (Projections & evaluation wrappers), [#97](https://github.com/rrwoods/PlayMore3.5/issues/97) (Presentation layer migration), [#98](https://github.com/rrwoods/PlayMore3.5/issues/98) (Controller purge & isolation verification).

#### UI Namespaces
  - Fix namespaces for controls and forms moved into the `UI/` folder.

#### Dynamic Control Recycling
  - Eliminate `ClearDynamicControls` and implement the recycle pattern across all dynamic panels.

#### `IJsonable` Deserialization ([ADR-0008](docs/adr/0008-explicit-constructor-deserialization-and-schema-stability.md))
  - Deliberately retained explicit constructor-based parsing (`new T(JObject)`) and static factory methods over parameterless mutating `ReadJson` methods or reflection frameworks to preserve domain encapsulation, construction invariants, and schema stability. Closed [#83](https://github.com/rrwoods/PlayMore3.5/issues/83) as wontfix.

#### Unique Effect Identifiers
  - Remove effects by unique ID rather than name (avoiding accidental removal of unrelated same-named effects when swapping vestiges/items).
