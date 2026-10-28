---
status: accepted
---

# Structured Rules Reference Schemas and Primary Source Extraction

All D&D 3.5e game content in `docs/rules/` is authored as structured reference documentation, and martial maneuvers are extracted directly from primary source text into discipline-partitioned reference files.

## Context

The rules library (`docs/rules/`) provides the authoritative Environmental Facts consumed by character sessions, rules catalogs, and test suites. Content documentation must deliver complete mechanical fidelity—including formulas, prerequisites, action economy, and stacking rules—while respecting the strict 300-line ceiling per markdown file enforced by `RulesLibraryStructureTests`.

## Decision

1. **Reference Documentation Format**: Rules entries in `docs/rules/` are structured reference documentation. Populated entries record verified mechanical **Environmental Facts**; unverified entries are explicit **Domain Specifications** awaiting human arbitration.
2. **Maneuvers Reference Schema**: Martial maneuvers and stances in `docs/rules/content/maneuvers/` follow a dedicated 6-field reference entry:
   ```markdown
   ### <Name>
   - **Source**: *Tome of Battle*, p. <Page>
   - **Discipline**: <Discipline> (<Strike | Boost | Counter | Stance | Other>) [<Descriptor>]
   - **Level**: <Class> <Level>
   - **Initiation Action**: <Action>
   - **Prerequisites**: <Prerequisites>
   - **Mechanics**: <Range, target, duration, and mechanical gameplay rules text>
   ```
   Range, target, and duration are integrated directly into `Mechanics`. Entries focus exclusively on actionable gameplay rules, formulas, and modifiers, omitting narrative flavor text to keep discipline files bounded. Unarbitrated maneuver stubs preserve this full schema with `TODO` values.
3. **General Content Skeletons**: Non-maneuver content skeletons (such as classes, feats, items, and equipment) use compact unarbitrated stubs:
   ```markdown
   ### <Name>
   - **Status**: Unarbitrated Domain Specification
   - **Source**: TODO
   ```
   Catalogs with large item counts (such as feats) are partitioned across balanced files in a dedicated sub-directory (`docs/rules/content/feats/`) to maintain file sizes well below 300 lines.
4. **Primary Source Extraction Tooling**: The full ~190 maneuver and stance catalog is extracted directly from the primary source PDF (*Tome of Battle*, Chapter 4) using a committed, reproducible extraction script (`tools/extract_maneuvers.py`) executed within the dedicated `dnd_pdf` PyMuPDF environment.
5. **Canon Standard**: Extraction strictly preserves printed primary source Rules as Written (RAW) from *Tome of Battle*.
6. **Engine Catalog Representation**: All ~190 maneuvers are recorded in `PlayMore35/Resources/maneuvers.json`. Stances and continuous boosts populate active engine modifiers in `effects`, while instantaneous strikes and tactical maneuvers record complete mechanical rules in `description`.

## Consequences

- Implementers and subagents consume verified primary source environmental facts from `docs/rules/content/maneuvers/` with full mechanical fidelity.
- All rules files remain well within the 300-line ceiling while providing complete mechanical coverage.
- The committed extraction script establishes an auditable, reproducible extraction pipeline for future primary source subsystem extractions (spells, psionics, soulmelds).
