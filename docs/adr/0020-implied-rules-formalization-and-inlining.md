---
status: accepted
---

# Implied Rules Formalization and Inlining

## Context

D&D 3.5e sourcebooks occasionally "talk around" a game rule they never explicitly write down in a single operative passage, instead enumerating or handling a series of consequences of that unwritten rule. In PlayMore35, planning and implementing features test-first requires citing authoritative game rules. Without a formal process for codifying these unwritten rules, feature specifications risk either treating them as unverified table assumptions (violating the *Rules Arbitration Invariant* and *Model Blindness*) or fragmenting them across scattered notes.

## Decision

1. **Domain Taxonomy & Epistemological Status**:
   - Codified in `GLOSSARY.md` as an **Implied Rule**: a systemic 3.5e game principle or constraint not explicitly codified in a single operative passage of the core rulebooks, but whose existence is logically presupposed by explicit rules, exceptions, or consequences.
   - Prior to human arbitration, any candidate implied rule is a **Domain Specification**. Once vetted with supporting primary source evidence and confirmed by the human maintainer, it is written to `docs/rules/` and functions as an authoritative **Environmental Fact**.
2. **Evidentiary Standard & Falsifiability**:
   - Requires *presuppositional proof*: candidate rules must be supported by primary source text whose printed rules only make sense if the unwritten rule is true.
   - Standard bar: at least two distinct primary source citations (or one primary citation plus an official errata/FAQ gloss) demonstrating consequences of the rule. A single explicit carve-out exception ("Unlike normal, you can...") is an acceptable temporary state.
   - The human maintainer explicitly supplies the supporting evidence citations and quotes rather than the agent attempting to discover or infer them, preserving Model Blindness.
   - Unwritten rules lacking textual presupposition in primary sources are classified as **Table Fiat** or **House Rules** per ADR-0001, not Implied Rules.
3. **Inlining Format and Siting**:
   - Implied rules are inlined directly into relevant domain mechanics files in `docs/rules/mechanics/` or `docs/rules/subsystems/` alongside the rules they interact with, honoring the *Deepen Before Splitting* design principle.
   - They are authored to look just like any other rule in standard tabletop domain language, carrying a parenthetical citation:
     `- <Rule text> (implied by *<Sourcebook 1>*, p. <Page>, and *<Sourcebook 2>*, p. <Page>).`
4. **Lifecycle & Distillation Touchpoint**:
   - Implied rules can be discovered during feature planning, issue authoring, or during interactive `/distill-rules` sessions.
   - In `/distill-rules`, when the human maintainer identifies an unwritten premise talked around by the source text, the supporting citations are recorded and the inlined formulation is synthesized directly into the target rules file during the session.
5. **Code and Test Citations**:
   - Unit tests and specifications cite the rule title/topic and the rules library section anchor:
     `// Source: Implied Rule: <Rule Name> (see docs/rules/mechanics/<file>.md#<section>)`
6. **Reversal Accounting & Evolution**:
   - If maintainer understanding evolves or reverses on an implied rule, the inlined text in `docs/rules/` and affected tests/code are updated or removed in-place.
   - The git commit message and PR description document the *Reversal Accounting*, explicitly explaining what was flawed in the prior reasoning.
   - If a table wishes to retain the ruling as a campaign preference rather than an unstated core rule, it transitions to a House Rule / Table Fiat per ADR-0001.
7. **Engine Modeling**:
   - Implied rules are modeled in the C# engine as standard core mechanics without special runtime tags, metadata, or speculative toggle plumbing.

## Consequences

- Provides a legitimate, verifiable mechanism to cite unwritten core rules during planning, issue authoring, and test-driven development.
- Deepens existing domain rules documentation without creating siloed catalogs or fractured lookups.
- Preserves the project's strict invariants: Model Blindness, Primary Source Exclusivity, and Enable Don't Enforce.
- Ensures all test citations remain navigable and traceable to human-verified primary source consequences.
