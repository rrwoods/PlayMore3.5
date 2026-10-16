# Rules Library

Progressively disclosed D&D 3.5e game rules and citations.

## Primary Sources & Extraction

- **Sourcebooks & PDF Paths**: Primary source PDFs (Player's Handbook, Tome of Battle, etc.) are located outside the workspace at `C:\Users\rickr\Documents\D&D notes\resources\books\`. For extraction tooling, PyMuPDF environments, and page calibration, see [`docs/agents/sourcebooks.md`](../agents/sourcebooks.md).
- **Rules Templates & Schema**: For the 6-field reference schema and the 300-line progressive disclosure ceiling, see [`docs/agents/rules-template.md`](../agents/rules-template.md).
- **Domain Structure**: For mechanics vs content separation and ADR conventions, see [`docs/agents/domain.md`](../agents/domain.md).

## Library Layout

- `mechanics/`: Systemic D&D 3.5e rules logic (modifiers, stacking, subsystem interactions).
- `content/`: Specific content reference entries mirroring `PlayMore35/Resources/*.json` (feats, maneuvers, items, spells).
- `subsystems/`: Dedicated subsystem arbitration files.
