# PlayMore35: Product Roadmap & Backlog Queue

This document serves as the canonical staging queue for PlayMore35 development. It consolidates all legacy backlog notes and planned features into an explicit 4-tier priority hierarchy.

When starting a new feature, select the highest-priority unblocked item from this roadmap and drive it through `/wayfinder`, `/to-spec`, or `/grill-with-docs` into GitHub issues.

---

## Tier 1: Odric & Core UX Priorities

Features required for the author's primary active character, alongside immediate core dashboard and ledger UX improvements prioritized alongside them.

### Immediate Core UX Priorities
- **Section 0 Restructure (Exploration & Social)**:
  - Create Section 0 positioned above Section 1 for Exploration and Social.
  - Move character vitals (name, gender, alignment, race, classes, levels, languages) to Section 0.
  - Pick 1–3 relevant skills to display per section (e.g., Spot and Listen in Senses, Diplomacy/Gather Information in Section 0).
  - Overall flow: Section 0 (Exploration/Social) $\to$ Section 1 (Before Combat / Senses / Evade / Initiative / Auras) $\to$ Section 2 (Enemy Turn: AC / HP / Saves / SR / Immunities / Reactions) $\to$ Section 3 (My Turn: Speeds / Base Combat / Attacks / Attack Options / Action Highlighter).
- **Timestamped GP & XP Ledgers**:
  - Gold transactions and XP session logs record dual timestamps: in-game timeline dates and real-world calendar timestamps.

### Odric Level Progression

#### Level 1–4: Baseline Mechanics & Bug Fixes
- **Text-Appending Effects**: Support effects that append additional rules clauses or text to existing class features (e.g. feats/features that add clauses to Barbarian Rage / Whirling Frenzy).
- **[BUG] Feat Choices Persistence**: Fix bug where feat choices persist across levels when clicking the `...` button (e.g. clicking level 4 first shows the weapon focus choice on all other levels; clicking level 1–3 blanks the weapon choice for level 4).
- **[BUG] Level-Up Experience Threshold**: Fix UI display showing `"0 XP to level 4"`.
- **[BUG] Ghost Touch Weapon Text**: Ensure ghost touch property text renders correctly in the attack/equipment summary.

### Level 5: Martial Maneuvers
- **[ODRIC 5] Tome of Battle Maneuver System**:
  - Support martial maneuver progression (Swordsage, Crusader, Warblade).
  - Maneuver preparation and readied/expended tracking: initiating a maneuver expends it until recovered.
  - Active stance effects that project bonuses and capabilities while toggled on.
  - Subsystem interaction with character actions and initiator level calculations.

### Level 7: Traditional Spellcasting
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
  - *(Note: Warlock-specific Eldritch Blast shapes and essences are deferred to Tier 4).*

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

- **In-App Effect Authoring (High Priority)**:
  - In-app UI system for defining new arbitrary effects (structured fields/dropdowns vs. natural language parser converting phrases like `"+1 morale to attack rolls"` into concrete effects).
  - Foundational effect generation layer that unblocks custom item, feat, class, and race authoring.
- **[ALTH] Class Feature Choices**:
  - Selections tied to class features (e.g., Swordsage discipline focus).
- **[ALTH] Skill Tricks**:
  - Dedicated "tricks" skill mechanism that creates a slot for skill tricks.
  - `tricks.json` catalog modeling skill trick prerequisites and effects.
- **[ALTH] Augment Crystals**:
  - Enable-don't-enforce crystal attachment: warn the user if attaching a crystal to an ineligible weapon/armor, but do not block the action.
  - Model crystals as `WeaponProperty Crystal` on `AttackMode`.
  - UI for attaching (drag crystal onto weapon) and detaching (right-click).
- **[ALTH] Weapon Tags in JSON Data**:
  - Introduce `tags.json` to categorize weapons (e.g. `special monk weapon`, *Tome of Battle* discipline weapons like `shadow hand`, pseudo-light like `rapier`, `tripping`, `disarming`).

---

## Tier 4: General Engine, Usability, & Maintenance

Universal engine improvements, usability enhancements, sanity refactors, and long-term compendium goals.

### 4.1 Core Rules Engine & Mechanics
- **Feat Prerequisites & `GiveFeat` Engine**:
  - Prerequisite checking: if prerequisite is not satisfied, flag with non-blocking advisory warning badge.
  - `GiveFeat` effect: adds to an active character feat set; feat framework uses this, and effects are conditional on prerequisites via `HasFeat` check.
  - Case-by-case prerequisite bypass: text-supported reasons (Monk bonus feats) via effect, or table-supported reasons (DM fiat).
  - Feats granting feats (e.g., *Vow of Poverty*).
- **Upgrade Effects**:
  - "Get [benefit]; if you already had [benefit], instead get [other benefit]" (e.g. Uncanny Dodge stacking into Improved Uncanny Dodge; Leraje vestige proficiency converting to +1 competence attack bonus if already proficient).
- **Variable Extra Dice**:
  - Variable extra damage dice scaling with level (Rogue sneak attack, Malphas sudden strike, scout skirmish).
- **Critical Characteristic Modifiers**:
  - Threat range increase (*Improved Critical*, *keen*), multiplier increase (*Deepwood Sniper*), confirmation roll bonus (*Bonecrusher* maneuver).
- **Natural Attacks & Unarmed Strikes**:
  - Effects generating natural attacks; races JSON specifying them.
  - Manufactured weapons vs. natural attacks (iteratives for manufactured; secondary natural attacks take -5, or -2 with Multiattack; held weapons precluding hand claws).
  - Unarmed strikes generated when at least one hand is free; option to hide/toggle unarmed attacks to avoid flooding sheet.
- **Tower Shield Attack Penalty**:
  - Enforce official rule: Tower shields impose -2 on ALL attack rolls, not just the shield hand.
- **Formalize DR, SR, and Energy Resistances**:
  - Convert text-only displays into structured rules pools where highest value applies.
  - SR pool (greatest number applies); Energy Resistance pools per energy type; DR pools keyed by overcoming condition (e.g., DR/magic, DR/evil, DR/--).
- **Creature Types, Subtypes, & Nonabilities**:
  - Formalize creature types and subtypes in the effect engine, referencing official core rules.
  - Support nonabilities (Construct/Undead CON, Vermin INT) with appropriate mathematical behaviors.
- **Multiform Support (Wild Shape, Polymorph, Hengeyokai)**:
  - Overwrite assigned ability scores (e.g., "Bear STR") via ability-substitution effects.
  - Handle forms ignoring level increments (RAW polymorph) by zeroing increment stats.
  - Base HP on normal CON using a separate "normal form CON" statistic.
- **Subsystems Framework Generalization**:
  - Unify resource-consuming subsystems (Psionics / Power Points, Mysteries, Truenaming) vs. effect-in-slot subsystems (Meldshaping / Soulmelds / Chakras / Essentia, Binders).
  - Warlock Subsystem (Deferred): Eldritch Blast attack routine modifications, Eldritch Essences (damage type and status riders), and Blast Shapes (range, burst, cone, chain).
- **Skills System Refinements**:
  - Support half-ranks for skills (`Character.skillRanks`, ranks text parsing).
  - Trained-only skills: grey out label when ranks equal zero.
  - Skill-like checks: Wild empathy, turning checks modeled as stats displayed under skills.
  - Skill point grant effects (Nymph's Kiss, Open Minded).
  - Cross-class cost alteration effects (Able Learner).
  - Class skill grant effects (Martial Study).
  - Movement mode skill bonuses (Climb bonus from climb speed, Swim bonus from swim speed).
  - Rules-embedded situational bonuses (Hide bonuses while invisible).
  - Global skill effects: Effects affecting all skills (sickened, vestige defiance) should not trigger relevance for every skill; display centrally.
  - Space statistic (lookup table on creature size category).
- **Epic Base Attack & Save Bonus Mechanics**:
  - D&D 3.5 epic progression rules (level > 20): Track "non-epic class levels" to determine which levels contribute toward standard base attack bonus and base save bonus progressions versus epic attack bonus and epic save bonus progressions.
- **Esoteric Rules & Mechanics (Monkey Grip & Control Shape)**:
  - *Monkey Grip*: Support wielding weapons one size category larger than normal with a -2 attack roll penalty.
  - *Control Shape Skill*: Model Control Shape skill checks for afflicted and natural lycanthropes.

### 4.2 Dashboard & Combat UX
*(Note: Section 0 restructuring is prioritized in Tier 1).*
- **Action Highlighter**:
  - Provide checkboxes for remaining turn actions (standard, move, swift).
  - Highlight usable abilities and attacks (single attack if standard available, full attack if full-round available, etc.) without hard locking.
- **Automated Attack Permutations**:
  - Automatically calculate valid attack permutations (single attack, full iterative routine, two-handed grip, TWF combinations) with a sensible baseline initially visible.
  - Combined attack and damage workflow: show roll damage button after attack roll (deferred with RollServer).
- **Roll Button Feedback**:
  - Provide feedback if the roll button is clicked and no incoming Roll20 request is listening on `RollServer`. Alternatively, avoid presenting or enable/disable the roll button based on active incoming listener availability.
- **Comprehensive Effects View**:
  - "All Effects" inspector displaying active, inactive, and conditional effects with toggle overrides.
  - Display uncategorized text under Special Qualities.
  - Show/hide controllable parameters based on toggles (matching controllable flags).
- **Combat Movement & Conditions**:
  - Over heavy load: 0 speed, but if less than double heavy load, add "full-rd at-will: move 5 feet" under movement.
  - Multiline evade display (Darkstalker, Hide in Plain Sight).
  - Variable ActionResource recharge times (prompt for roll result).
  - Unscheduling actions (e.g. Expel Vestige unscheduling unbind actions).
  - Dedicated Aura section (after Senses before Languages).
- **Character Building UX**:
  - Alternate Class Features (ACFs) support, replacing features or feature groups.
  - Use `TableLayoutPanel` and `FlowLayoutPanel` for class levels and feats layout.
  - Tab order and keyboard navigation across dynamic controls.
  - Lock/unlock ability score assignment (starts locked on load, clickable to unlock/respec).
  - Support removing class levels.

### 4.3 Inventory, Equipment, & Wealth
- **Interchangeable Ring Slots**:
  - Support items equipping to either Ring 1 or Ring 2 interchangeably; support *Hand of Glory* if needed.
- **Magic Item Merging**:
  - Combine two same-slot items with official 1.5× surcharge (e.g., *Torc of Displacement* + *Torc of Constitution*).
- **Weapon Properties Across Modes**:
  - Report weapon modes (melee, second melee, thrown, projectile) and allow properties on specific modes.
  - Allow adding weapon properties to shield bashes and armor spikes.
- **Item Usability & Descriptions**:
  - Add no-effect name-only items to inventory locations without requiring full catalog definitions.
  - View full item descriptions directly in the UI.
- *(Note: Timestamped GP and XP ledgers are prioritized in Tier 1).*

### 4.4 Content & Rules Data
- **Elven Weapon Proficiencies**:
  - Represent elf weapon proficiencies as effects rather than hardcoded specific feats (DCFS compliant).
- **Specific Feature Scaling**:
  - Savnok vestige DR scaling with Effective Binder Level.
  - Stunning Fist "per four levels" excluding non-class hit dice.
- **Downtime & Character Option Text Categories**:
  - Add "Downtime" or "Character Option" text effect category to support meta and out-of-combat feats (e.g. *Leadership*, *Improved Familiar*).
- **Naberius Faster Ability Healing Scheduled Actions**:
  - Convert Naberius's *Faster Ability Healing* from a passive text entry into scheduled sheet actions on the timeline.
- **Content Creation & House Rules Layer**:
  - In-app authoring for new items, classes, feats, and races (building upon Tier 3 in-app effect authoring).
  - Separate house rules from core rules; ability to save/load distinct house-rule sets per campaign.
  - Combine core and extended data definitions (load core $\to$ load extended $\to$ save sorted).
  - Contentious rules arbitration: allow users to configure campaign/DM preferences for ambiguous or contentious rules calls (e.g., vestige influence mandates vs. penalties).

### 4.5 Project Architecture & Sanity
- **Deepen the Timeline & Sheet Action Module ([#74](https://github.com/rrwoods/PlayMore3.5/issues/74))**:
  - Decouple `Rounds` and `SheetAction` from `MainForm` into a headless, in-process module.
  - Enables headless unit testing and unblocks the synchronized combat clock for the Encounter Tracker.
  - Slices: [#78](https://github.com/rrwoods/PlayMore3.5/issues/78) (Test harness & context seam), [#79](https://github.com/rrwoods/PlayMore3.5/issues/79) (Timeline core & result contract), [#80](https://github.com/rrwoods/PlayMore3.5/issues/80) (SheetAction & generic slots), [#81](https://github.com/rrwoods/PlayMore3.5/issues/81) (Combat transitions & UI rewiring).
- **Deepen the Subsystems & Resource Pools Module ([#75](https://github.com/rrwoods/PlayMore3.5/issues/75))**:
  - Consolidate Pact Magic, Tome of Battle Maneuvers, Spellcasting, and Invocations behind a uniform effect-in-slot and resource pool interface.
  - Slices: [#84](https://github.com/rrwoods/PlayMore3.5/issues/84) (Core abstractions & domain primitives), [#85](https://github.com/rrwoods/PlayMore3.5/issues/85) (Pact Magic engine & persistence), [#86](https://github.com/rrwoods/PlayMore3.5/issues/86) (Core models decoupling), [#87](https://github.com/rrwoods/PlayMore3.5/issues/87) (UI rewiring & Controller shrinkage).
- **Deepen the Attack Routine Evaluation Module ([#76](https://github.com/rrwoods/PlayMore3.5/issues/76))**:
  - Encapsulate combinatorial weapon-hand pairing, TWF penalties, and iterative attack progressions behind an evaluator interface.
- **Deepen the Character Engine Module ([#77](https://github.com/rrwoods/PlayMore3.5/issues/77))**:
  - Replace the pass-through `Controller` coordinator with a deep character session module emitting clean read-only state projections.
  - Slices: [#94](https://github.com/rrwoods/PlayMore3.5/issues/94) (Domain primitives & catalog extraction), [#95](https://github.com/rrwoods/PlayMore3.5/issues/95) (CharacterSession engine), [#96](https://github.com/rrwoods/PlayMore3.5/issues/96) (Projections & evaluation wrappers), [#97](https://github.com/rrwoods/PlayMore3.5/issues/97) (Presentation layer migration), [#98](https://github.com/rrwoods/PlayMore3.5/issues/98) (Controller purge & isolation verification).
- **UI Namespaces**:
  - Fix namespaces for controls and forms moved into the `UI/` folder.
- **Dynamic Control Recycling**:
  - Eliminate `ClearDynamicControls` and implement the recycle pattern across all dynamic panels.
- **`IJsonable` Deserialization ([ADR-0008](docs/adr/0008-explicit-constructor-deserialization-and-schema-stability.md))**:
  - Deliberately retained explicit constructor-based parsing (`new T(JObject)`) and static factory methods over parameterless mutating `ReadJson` methods or reflection frameworks to preserve domain encapsulation, construction invariants, and schema stability. Closed [#83](https://github.com/rrwoods/PlayMore3.5/issues/83) as wontfix.
- **Unique Effect Identifiers**:
  - Remove effects by unique ID rather than name (avoiding accidental removal of unrelated same-named effects when swapping vestiges/items).
