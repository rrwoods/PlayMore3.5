# Rules Template & Rubric

How to specify, arbitrate, and verify D&D 3.5e game rules in PlayMore35.

## The arbitration rule

Game rules are **Domain Specifications**, not **Environmental Facts** (see `GLOSSARY.md`). Game rules originate exclusively from human-verified primary sources. Treat model pretraining as blind to 3.5e mechanics; all rules awareness enters the engine through human arbitration.

A rules-bearing issue or task cannot be marked `ready-for-agent` without a verified primary source citation provided or confirmed by the human.

## Issue / Spec template

Use this template when creating GitHub issues or drafting specs for rules, statistics, subsystems, feats, classes, or modifiers:

```markdown
# [RULES] <Subsystem / Feat / Statistic / Effect Name>

## 1. Summary
Brief description of the rule or capability being modeled in PlayMore35.

---

## 2. Rules Provenance (Mandatory Gate for ready-for-agent)
> Complete and verify this section with the human arbiter before planning or implementing this issue.

- **Primary Source**: `<Sourcebook Name>` (e.g., *Player's Handbook 3.5*, *Complete Warrior*, *Rules Compendium*)
- **Page Number(s)**: `p. <Number>`
- **Verbatim Rule Text / Quote**:
  > "<Insert verbatim rule text, or markdown transcript of screenshot provided by human arbiter>"
- **Official Errata / FAQ (if applicable)**: `<Errata text or "None">`
- **Human Arbitration & Table Fiat**:
  - *Interpretation*: `<Clarifications on ambiguities, stacking, or interaction with existing subsystems>`
  - *House Rules / Table Fiat*: `<Overrides or custom table rulings per ADR-0001 Enable, Don't Enforce>`

---

## 3. Engine Target & Representation
- **Data Definitions (`rules.json`)**:
  - `stats`: `<New or modified Statistics>`
  - `effects`: `<Modifiers, toggles, stacking categories>`
- **Subsystem & C# Classes**:
  - `<Engine classes or interfaces affected, e.g., Subsystems/PactMagic, CharacterChoices/Feat.cs>`
- **Advisory Warnings**:
  - `<Prerequisites or illegal condition checks to surface via non-blocking Warning Badges>`

---

## 4. Test Specifications
- **Unit Test Class**: `<e.g. PlayMore35.Tests/FeatTests.cs>`
- **Test Citation**: Every test method must include the citation comment directly above the test:
  ```csharp
  // Source: <Sourcebook Name>, p. <Number>
  [TestMethod]
  public void FeatName_CalculatesCorrectBonus_WhenConditionMet() { ... }
  ```

---

## 5. Scope Boundaries
- **In Scope**: `<Explicit list of mechanics covered>`
- **Anti-goals / Out of Scope**: `<Conflated mechanics, higher-tier abilities, or deferred variants>`
```

## Verification rubric

Use this checklist during triage, planning, and code review:

| Stage | Check | Pass Criteria |
| :--- | :--- | :--- |
| **Triage** | **Source Provided** | Issue contains exact sourcebook, page number, and quote/screenshot transcript. If missing, label `needs-info`. |
| **Planning** | **Zero Extrapolation** | Implementation plan (`implementation_plan.md`) includes a `Rules Provenance` section reproducing the verified quote. No mechanics are assumed from memory. |
| **Coding** | **Fidelity & Citations** | Code strictly models the quoted text. Unit tests cite book and page in comments or names. |
| **Review** | **Spec Axis Review** | `/code-review` verifies the diff adheres strictly to the quoted text without unauthorized feature creep. |
