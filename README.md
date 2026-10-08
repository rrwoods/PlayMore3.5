# PlayMore35

A live character companion and combat tracker for **Dungeons & Dragons 3.5 Edition**.

---

## Overview

Dungeons & Dragons 3.5e is a rich and tactically deep system, but running characters at the table creates high cognitive overhead. Tracking overlapping bonus types, situational buffs, temporary ability score adjustments, and round-based effect durations quickly bogs down combat.

Static character sheets and offline character builders excel at planning a build, but offer little assistance once dice start rolling. **PlayMore35** is designed specifically for live tabletop play: an active, in-combat dashboard that dynamically tracks and calculates your character's fluid combat state from round to round.

---

## Core Philosophy

PlayMore35 is built around three foundational design principles:

- **Enable, Don't Enforce**: Tabletop gaming thrives on DM fiat, situational rulings, and house rules. The application never locks choices or prevents actions based on rigid rule checks. If a character does not meet a prerequisite or equips incompatible gear, the sheet presents an advisory **warning badge** without blocking the selection.
- **Mathematical Transparency**: No black-box numbers. Every calculated statistic—from Armor Class and saving throws to attack routines and skill totals—provides a detailed breakdown tooltip displaying every contributing modifier, its category (enhancement, morale, insight, luck, etc.), and its source according to official 3.5e stacking rules.
- **Dynamic Recomputation**: Statistics are evaluated dynamically on demand. When an item is equipped, a stance activated, or a buff toggled, all affected values recalculate immediately across the entire sheet without caching or synchronization lag.

---

## Live Combat Dashboard Tour

The application provides a combat-first interface organized around the natural flow of tabletop play:

### 1. Defenses, Vitals, & Combat Rolls
The main **Stats** tab functions as your active combat sheet, organized into structured sections across its columns:
- **Section 0 (Exploration & Social)**: Positioned prominently at the top of Column 0, displaying core character vitals alongside focused skill checks and relevant abilities: **Exploration** (Survival, Search) and **Social** (Diplomacy, Bluff).
- **Section 1 (Before Combat & Senses)**: Situational readiness panels including refactored **Senses** (Listen and Spot checks, replacing the retired `ListenSpotPanel`), initiative, evade capabilities, and conditions.
- **Armor Class & Defenses**: At-a-glance visibility into full Armor Class, Touch AC, and Flat-Footed AC, alongside Hit Points, damage reduction, and saving throws.
- **Attack Routines**: Automatically computed standard and full-attack routines partitioned by action economy, with category-based visibility filtering (two-weapon fighting, double weapons, shield bashes, armor spikes) and interactive one-click rolls.
- **Dynamic Panels**: Real-time panels for initiative, movement speeds, Senses, and active combat capabilities.

### 2. Situational Toggles & Attack Options
Activate combat abilities and conditional buffs with a single click:
- Switch on abilities such as **Whirling Frenzy**, **Power Attack**, or temporary class features.
- Derived bonuses immediately cascade across attack rolls, defense values, and saving throws.

### 3. The Combat Timeline
Combat moves round by round, and so does PlayMore35:
- Advance combat rounds with a dedicated round advancement control.
- Active buffs, spells, and temporary effects automatically decrement their remaining durations.
- Expired effects revert cleanly, and scheduled transition states (such as post-rage or post-frenzy fatigue) queue and trigger automatically.

### 4. Esoteric Subsystems (Pact Magic & Vestiges)
When playing characters that rely on non-core subsystems, specialized tabs unlock dynamically:
- **Soul Binding**: Bind vestiges from *Tome of Magic*, manage binding check DCs, track pact influence, and activate granted supernatural and extraordinary abilities.

### 5. Equipment, Containers, & Wealth
The **Inventory** tab tracks physical gear and logistical wealth:
- **Equipped Slots**: Gear assigned to body slots actively projects its properties, armor check penalties, and magical enhancements onto combat stats.
- **Carried Containers**: Organize gear across named storage locations (backpacks, belt pouches, saddlebags) with automatic encumbrance calculations.
- **Transaction Ledger**: Maintain a timestamped historical record of gold transactions and session experience point awards.

---

## Current State & Content Coverage

PlayMore35 is currently in active alpha development. To keep development grounded and immediately useful at the table, content implementation is driven first by the active needs of the author's tabletop campaign before expanding across the full library of published 3.5e supplements.

### Feature Status Matrix

| Feature Area | Status | Description |
| :--- | :--- | :--- |
| **Rules Calculation Engine** | Available | Official 3.5e modifier stacking, type overlap, and on-demand stat recomputation |
| **Live Combat Dashboard** | Available | Real-time AC, saves, attack routines, and situational combat toggles |
| **Combat Timeline & Durations** | Available | Round progression with automatic duration countdowns and expiration triggers |
| **Pact Magic / Vestiges** | Available | Dedicated Binder subsystem tab with soul binding, pact checks, and granted abilities |
| **Inventory & Encumbrance** | Available | Slotted gear projection, carried container weights, and gold/XP ledgers |
| **Advisory Prerequisite Badges** | Available | Non-blocking warning indicators for unmet feats or gear prerequisites |
| **Invocations (Dragonfire Adept)** | In Progress | Draconic invocations by grade, breath weapon effects, and sustained 24-hr buffs |
| **Tome of Battle Subsystem** | Planned | Maneuver tracking, readying, stances, and recovery mechanics |
| **DM Encounter Tracker** | Planned | Multi-combatant initiative order, synchronized timeline, and monster tracking |
| **Universal 3.5e Compendium** | Expanding | Currently includes Barbarian (with ACFs), Fighter, Binder, core races, and select items/feats |

---

## Getting Started

### System Requirements
- **Operating System**: Windows (Windows 10 / 11 recommended)
- **Runtime**: [.NET Framework 4.8](https://dotnet.microsoft.com/download/dotnet-framework/net48) runtime

### Running the Application
Launch the compiled binary:
```text
PlayMore35.exe
```

### Building from Source
To build PlayMore35 locally:

1. Clone the repository:
   ```shell
   git clone https://github.com/rrwoods/PlayMore3.5.git
   ```
2. Ensure you have the [.NET SDK](https://dotnet.microsoft.com/) (version 6.0 or later supporting MSBuild) or **Visual Studio 2022** with the **.NET desktop development** workload installed.
3. Build the solution from the project root:
   ```shell
   dotnet build PlayMore35.sln
   ```
4. The output binary will be located in:
   ```text
   PlayMore35\bin\Debug\PlayMore35.exe
   ```

### Character Data & Saves
- Characters are saved and loaded as transparent, human-readable `.json` files via the in-app **Save Character** and **Load Character** buttons.
- All character sheets and campaign data remain fully offline and stored locally on your machine.
