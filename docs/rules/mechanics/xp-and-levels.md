# Experience, Levels, and Multiclassing

## 1. Experience and Level Advancement
- **Primary Source**: *Player's Handbook*, pp. 22, 58
- **Experience Points (XP)**: Cumulative measure of character learning and advancement awarded by the Dungeon Master based on accomplishments and defeated encounters.
- **Advancing a Level**:
  - A character attains a new character level when their total accumulated XP equals or exceeds the minimum XP threshold for that level.
  - **XP Threshold Formula**:
    $$\text{XP}_{\text{required}}(L) = \frac{(L - 1) \times L}{2} \times 1{,}000$$
  - **Single Level Advancement Limit**: A character can advance only one level at a time per advancement event.
  - **Excess XP Cap on Multi-Level Awards**: If an XP reward would advance a character two or more levels at once, the character advances exactly one level, and their XP total is capped at $1\text{ XP}$ short of the next level threshold:
    $$\text{Max XP} = \text{XP}_{\text{required}}(L + 2) - 1$$
    Any excess experience points beyond this cap are not retained.
  - **Training and Practice**: Inability to train, study, or practice skills between adventures permits the Dungeon Master to reduce XP awards or impose XP loss.

---

## 2. Step-by-Step Level Advancement Procedure
- **Primary Source**: *Player's Handbook*, pp. 22, 58
- When a character advances a level, apply adjustments in the following mandatory sequence:
  1. **Choose Class**: Select the class that advances by one level (or take 1st level in a new class). Other classes remain at their current level.
  2. **Base Attack Bonus (BAB)**: Check the class progression table for the advancing class. High BAB progressions advance by $+1$ each level; medium progressions advance at $+3/4$ per level; poor progressions advance at $+1/2$ per level.
  3. **Base Save Bonuses**: Check Fortitude, Reflex, and Will save progressions for the advancing class. Good saves increase at every even-numbered level ($+1/2$ per level after base $+2$); poor saves increase at levels divisible by three ($+1/3$ per level).
  4. **Ability Score Increase**:
     - Gained at 4th, 8th, 12th, 16th, and 20th **character level** (every four character levels, regardless of individual class levels).
     - Increase one chosen ability score by $+1$ (scores may exceed 18).
     - **Retroactive Hit Points**: In PlayMore35, maximum hit points is a dynamically computed statistic; increasing Constitution retroactively increases hit points across all character levels:
       $$\Delta\text{HP} = \Delta\text{CON mod} \times \text{Character Level}$$
  5. **Hit Points**:
     - Roll one Hit Die of the advancing class type, add the current Constitution modifier, and add the result to the character's total hit points.
     - **Minimum 1 HP Rule**: A character always gains at least 1 hit point on leveling up, even if a Constitution penalty and low die roll would result in 0 or negative hit points.
  6. **Skill Points**:
     - Gain skill points based on the advancing class: $(\text{Class Skill Points} + \text{INT mod})$.
     - **Intelligence Score Tracking**: Determined using current Intelligence including permanent adjustments (inherent bonuses, ability score increases from Step 4, ability drain), but strictly excluding temporary changes (enhancement bonuses from spells or magic items, ability damage).
     - **Class Skills**: Skills on the advancing class's list cost 1 point per rank. Maximum rank is $\text{Character Level} + 3$.
     - **Cross-Class Skills**: Skills not on the advancing class's list cost 1 point per $1/2$ rank. Maximum rank is $\frac{\text{Character Level} + 3}{2}$ (exact half; not rounded).
     - **Skill Maxing**: Spending 1 point per level maintains maximum ranks in a maxed-out skill ($1/2$ rank if cross-class).
     - **Level-Up Skill List**: Skills are purchased strictly according to the class being advanced at that level; only skills on that class's skill list qualify for 1 point per 1 rank cost.
  7. **Feats**:
     - Gained at 1st level, 3rd level, and every three character levels thereafter (6th, 9th, 12th, 15th, 18th).
     - Gated by overall **character level**, not class level.
     - Feat prerequisites must be satisfied at the time the feat is selected.
  8. **Spells**: Spellcasting classes gain additional spells per day and/or spells known according to their class progression table and class level.
  9. **Class Features**: Gain new special abilities, attacks, or powers granted by the advancing class at the new level.

---

## 3. Multiclass Characters: Core Mechanics
- **Primary Source**: *Player's Handbook*, pp. 22, 59
- **General Principle**: Multiclass abilities are the cumulative sum of each individual class's independent progression.
- **Character Level vs. Class Level**:
  - **Character Level**: Total number of levels across all classes ($\text{Character Level} = \sum \text{Class Levels}$). Governs general feat acquisition intervals, ability score increases, and maximum skill rank caps.
  - **Class Level**: Level in a particular class. Governs Hit Dice, class features, class-specific spells, caster level, base attack bonus, and base save increments.
- **Hit Points**: Sum of hit points rolled and added from each class level.
- **Base Attack Bonus & Iterative Attacks**:
  - Total BAB equals the direct sum of base attack bonuses from each class:
    $$\text{Total BAB} = \sum \text{Class BAB}$$
  - **Iterative Attacks**: When $\text{Total BAB} \ge +6$, the character gains multiple attacks during a full attack action at 5-point decrements:
    - $\text{BAB } +6 \text{ to } +10$: 2 attacks ($\text{BAB} / \text{BAB}-5$)
    - $\text{BAB } +11 \text{ to } +15$: 3 attacks ($\text{BAB} / \text{BAB}-5 / \text{BAB}-10$)
    - $\text{BAB } +16 \text{ to } +20$: 4 attacks ($\text{BAB} / \text{BAB}-5 / \text{BAB}-10 / \text{BAB}-15$)
    - Additional attacks unlock based on combined total BAB, regardless of whether individual class BABs would grant extra attacks.
- **Saving Throws**:
  - Total base save bonus equals the direct sum of base save bonuses from each class for each save category (Fortitude, Reflex, Will):
    $$\text{Total Base Save} = \sum \text{Class Base Save}$$
- **Skills for Multiclass Characters**:
  - **Maximum Skill Rank Ceiling**: If a skill is a class skill for *any* of the character's classes, the maximum rank is determined by the class skill cap using overall Character Level:
    $$\text{Max Rank} = \text{Character Level} + 3$$
  - If a skill is not a class skill for any of the character's classes, the cap is:
    $$\text{Max Rank} = \frac{\text{Character Level} + 3}{2}$$
  - **Point Cost at Advancement**: When advancing a level in Class A, skills on Class A's list cost 1 point per rank. Skills not on Class A's list cost 1 point per $1/2$ rank, even if they are class skills for another class the character possesses.

---

## 4. Multiclass Class Features and Stacking Rules
- **Primary Source**: *Player's Handbook*, p. 59
- **Cumulative Features and Penalties**: A multiclass character gains all class features of each class and suffers all class restrictions and penalties (e.g. arcane spell failure from armor applies to arcane spells, even when proficient with armor through another class). Exception: Gaining barbarian levels does not impose illiteracy.
- **Turn Undead Stacking**:
  - If a character possesses levels in Cleric and at least 4 levels in Paladin, effective turning level stacks:
    $$\text{Effective Turning Level} = \text{Cleric Level} + \text{Paladin Level} - 3 \quad (\text{Paladin Level} \ge 4)$$
- **Uncanny Dodge Stacking**:
  - When a character with Uncanny Dodge would gain Uncanny Dodge a second time from another class (e.g. Barbarian and Rogue), they automatically gain **Improved Uncanny Dodge** instead.
  - Barbarian and Rogue levels stack to determine the attacker rogue level required to flank the character:
    $$\text{Minimum Flanker Rogue Level} = \text{Barbarian Level} + \text{Rogue Level} + 4$$
- **Familiar Stacking**:
  - Sorcerer and Wizard levels stack directly to determine a familiar's natural armor bonus, Intelligence score, and special abilities.
- **Spellcasting**:
  - Spell slots, spells prepared, and spells known are tracked independently for each class.
  - Spell effects, save DCs, and caster level depend on the specific class level used to cast the spell.

---

## 5. Adding a New Class
- **Primary Source**: *Player's Handbook*, pp. 59–60
- **1st Level in New Class Grants**:
  - 1st-level base attack bonus and base save bonuses of the new class.
  - Class skills and weapon, armor, and shield proficiencies of the new class.
  - 1st-level class features and spellcasting capabilities of the new class.
  - 1 Hit Die of the appropriate type (rolled + Constitution modifier, minimum 1 HP).
  - Single-level skill points for the new class: $(\text{Class Skill Points} + \text{INT mod}) \times 1$.
- **Excluded Starting Bonuses**:
  - When acquiring a new class after 1st character level, the character does **not** receive:
    1. Maximum hit points from the first Hit Die.
    2. Quadruple skill points ($\times 4$ skill points apply strictly at 1st character level).
    3. Starting equipment.
    4. Starting gold.
- **Single 1st-Level Entry**: A character cannot take 1st level in the same class more than once (e.g. to choose alternative domains or specializations).

---

## 6. Level-Dependent Progression Reference Table
- **Primary Source**: *Player's Handbook*, pp. 22, 58 (Table 3–2: Experience and Level-Dependent Benefits)

| Character Level | Total XP Required | Feats Gained | Ability Score Increases | Class Skill Max Rank | Cross-Class Skill Max Rank |
| :---: | :---: | :---: | :---: | :---: | :---: |
| 1st | 0 | 1st | — | 4 | 2 |
| 2nd | 1,000 | — | — | 5 | 2.5 |
| 3rd | 3,000 | 2nd | — | 6 | 3 |
| 4th | 6,000 | — | 1st (+1) | 7 | 3.5 |
| 5th | 10,000 | — | — | 8 | 4 |
| 6th | 15,000 | 3rd | — | 9 | 4.5 |
| 7th | 21,000 | — | — | 10 | 5 |
| 8th | 28,000 | — | 2nd (+1) | 11 | 5.5 |
| 9th | 36,000 | 4th | — | 12 | 6 |
| 10th | 45,000 | — | — | 13 | 6.5 |
| 11th | 55,000 | — | — | 14 | 7 |
| 12th | 66,000 | 5th | 3rd (+1) | 15 | 7.5 |
| 13th | 78,000 | — | — | 16 | 8 |
| 14th | 91,000 | — | — | 17 | 8.5 |
| 15th | 105,000 | 6th | — | 18 | 9 |
| 16th | 120,000 | — | 4th (+1) | 19 | 9.5 |
| 17th | 136,000 | — | — | 20 | 10 |
| 18th | 153,000 | 7th | — | 21 | 10.5 |
| 19th | 171,000 | — | — | 22 | 11 |
| 20th | 190,000 | — | 5th (+1) | 23 | 11.5 |
