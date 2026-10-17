# Maneuvers and the Sublime Way

## 1. Overview and Core Concepts
- **Primary Source**: *Tome of Battle: The Book of Nine Swords*, pp. 5, 37–38
- **Sublime Way**: A martial combat system consisting of discrete martial powers organized into stances and maneuvers.
- **Martial Adepts**: Characters with levels in the three standard martial adept classes: Crusader, Swordsage, or Warblade (*Tome of Battle*, p. 5). Non-adepts can gain limited access to maneuvers and stances via the Martial Study and Martial Stance feats (*Tome of Battle*, pp. 38, 52).
- **Extraordinary vs. Supernatural**: Maneuvers and stances are Extraordinary ($\text{Ex}$) abilities by default unless their individual entry specifies Supernatural ($\text{Su}$) (*Tome of Battle*, p. 40).


---

## 2. The Nine Disciplines
- **Primary Source**: *Tome of Battle*, pp. 5, 41–43
- Each maneuver and stance belongs to one of nine martial disciplines. Each discipline is associated with a key skill and a suite of preferred weapons:
  - **Desert Wind**: Key skill **Tumble**. Associated weapons: *scimitar, light mace, light pick, falchion, spear*.
  - **Devoted Spirit**: Key skill **Intimidate**. Associated weapons: *falchion, greatclub, longsword, maul*.
  - **Diamond Mind**: Key skill **Concentration**. Associated weapons: *rapier, shortspear, bastard sword, trident*.
  - **Iron Heart**: Key skill **Balance**. Associated weapons: *bastard sword, dwarven waraxe, longsword, two-bladed sword*.
  - **Setting Sun**: Key skill **Sense Motive**. Associated weapons: *short sword, quarterstaff, nunchaku, unarmed strike*.
  - **Shadow Hand**: Key skill **Hide**. Associated weapons: *dagger, short sword, sai, siangham, unarmed strike, spiked chain*.
  - **Stone Dragon**: Key skill **Balance**. Associated weapons: *greatsword, greataxe, heavy mace, unarmed strike*.
  - **Tiger Claw**: Key skill **Jump**. Associated weapons: *kukri, kama, claw, handaxe, greataxe, unarmed strike*.
  - **White Raven**: Key skill **Diplomacy**. Associated weapons: *longsword, battleaxe, warhammer, greatsword, halberd*.

---

## 3. Categories of Martial Powers
- **Primary Source**: *Tome of Battle*, pp. 5, 42–44
- **Boost**:
  - Initiated as a **swift action** (*Tome of Battle*, p. 42).
  - Grants a temporary bonus (usually to attacks, damage, speed, or skill checks) lasting until the **end of your turn** (*Tome of Battle*, p. 42).
  - Applies to all attacks made during the turn, even if switching weapons (*Tome of Battle*, p. 42).
- **Counter**:
  - Initiated as an **immediate action**, usable outside your turn in response to an enemy action or condition (*Tome of Battle*, pp. 43, 44–45).
  - Foils an attack or mitigates a hazard (*Tome of Battle*, p. 43).
- **Strike**:
  - Initiated as a **standard action** or **full-round action** (*Tome of Battle*, p. 43).
  - A special attack routine delivering unique effects or bonus damage on a hit (*Tome of Battle*, p. 43).
  - Extra damage from a strike does **not** multiply on a critical hit (*Tome of Battle*, p. 43).
  - A strike is its own dedicated attack action and cannot be substituted for an individual attack within a full attack routine (*Tome of Battle*, p. 43). Strikes cannot benefit from *haste* or speed weapons to make additional attacks unless the strike description explicitly calls for a full attack.
- **Stance**:
  - Adopted or switched as a **swift action** (*Tome of Battle*, pp. 5, 38, 43).
  - Provides a continuous, ongoing combat benefit as long as it is maintained (*Tome of Battle*, p. 43).
  - Duration is indefinite; ends immediately if the character is rendered **helpless** (*Tome of Battle*, p. 43).
  - Only one stance can be active at a time, unless modified by a class feature such as Warblade 20 **Stance Mastery (Ex)** which allows two stances active simultaneously and permits initiating or changing one or both stances with a single swift action (*Tome of Battle*, pp. 23, 40).
  - A stance **counts as a maneuver** solely for the purpose of meeting prerequisites (e.g. "two Iron Heart maneuvers") and level gating for acquisition (*Tome of Battle*, pp. 43–44). For all other mechanical purposes and contexts, a stance is **not** a maneuver (it is never readied, never expended, and cannot be initiated as a strike/boost/counter).

---

## 4. Initiator Level and Maneuver Access Formulas
- **Primary Source**: *Tome of Battle*, pp. 39–40, 44
- **Initiator Level ($\text{IL}$)**:
  - **Single-class martial adept**:
    $$\text{IL} = \text{Martial Adept Class Level}$$
  - **Non-adept classes**:
    $$\text{IL} = \left\lfloor\frac{\text{Character Level}}{2}\right\rfloor$$
    - **Rules Dysfunction & Human-Approved Correction** (`dysfunction:martial-study-minimum-il`):
      Under strict rules-as-written, a 1st-level non-adept taking *Martial Study* receives $\lfloor 1 / 2 \rfloor = 0$ initiator level, leaving them with $\text{Max Maneuver Level} = 0$ and unable to select or initiate any 1st-level maneuver. PlayMore35 applies a human-approved correction setting $\text{IL}_{\text{feats}} = \max(1, \lfloor\text{Character Level} / 2\rfloor)$ using non-stacking modifier type `internal` on both *Martial Study* and *Martial Stance*.
  - **Multiclass martial adept** (calculated independently for each martial adept class):
    $$\text{IL}_{\text{class}} = \text{Class Level} + \left\lfloor\frac{1}{2} \sum \text{Other Class Levels}\right\rfloor + \text{Prestige Class Levels}$$
- **Maneuver & Stance Level Gating**:
  - *Tome of Battle*, p. 39:
    $$\text{Max Maneuver Level} = \min\left(9, \left\lceil\frac{\text{Initiator Level}}{2}\right\rceil\right)$$
  - Gating applies identically to learning maneuvers and stances.

---

## 5. Maneuvers Known, Readied, and Preparation
- **Primary Source**: *Tome of Battle*, pp. 5, 38
- **Maneuvers Known**: The permanent repertoire of maneuvers learned through class levels or the Martial Study feat.
- **Maneuvers Readied**: The subset of maneuvers known that are prepared and available for initiation.
  - Number readied is strictly determined by class progression (plus prestige classes) (*Tome of Battle*, p. 38).
  - Non-adepts with Martial Study ready all maneuvers known from the feat (*Tome of Battle*, p. 38).
  - Adepts who take Martial Study treat the maneuver as an additional maneuver known, subject to standard readied capacity (*Tome of Battle*, p. 38).
  - Slots cannot be left open or unfilled (*Tome of Battle*, p. 38).
- **Readying Ritual**:
  - Requires **5 minutes** of uninterrupted practice, exercise, meditation, or prayer (*Tome of Battle*, p. 38).
  - Initiator does not need to be rested, but must be able to stand and move freely (*Tome of Battle*, p. 38).
- **Stances**: Stances are never readied; all known stances are available at all times (*Tome of Battle*, pp. 5, 38).

---

## 6. In-Combat and Encounter Recovery Mechanics
- **Primary Source**: *Tome of Battle*, pp. 38, 40
- **Encounter Start**: Adepts begin every encounter with all readied maneuvers unexpended (*Tome of Battle*, pp. 38, 40).
- **Initiation and Expending**: Initiating a maneuver temporarily expends it until recovered (*Tome of Battle*, p. 38). Stances are never expended (*Tome of Battle*, p. 38).
- **In-Combat Recovery by Class**:
  - **Crusader (Granted vs. Withheld)**:
    - Crusaders begin encounters with a subset of readied maneuvers randomly granted; the remainder are withheld (*Tome of Battle*, pp. 38, 40).
    - At the end of each turn, one withheld maneuver is granted (*Tome of Battle*, p. 38).
    - When attempting to draw/grant a maneuver card with none remaining in the withheld pool, all expended maneuvers are recovered and reshuffled into the deck, drawing a fresh hand of granted maneuvers for the next round (*Tome of Battle*, p. 38).
  - **Swordsage**:
    - Can recover **one** expended maneuver of choice as a **full-round action** (*Tome of Battle*, p. 40).
  - **Warblade**:
    - Can recover **all** expended maneuvers as a **swift action**, immediately followed by a melee attack or a standard action to do nothing (*Tome of Battle*, p. 40).
    - Cannot recover maneuvers if the warblade initiated a maneuver or changed stance in that same round (*Tome of Battle*, p. 40).
  - **Martial Study Feat**:
    - Characters without a martial adept class do not have a recovery mechanism (*Tome of Battle*, p. 40).
  - **Multiclass Adepts**:
    - Maneuvers known, readied, expended, and recovery actions are tracked completely separately for each adept class (*Tome of Battle*, p. 40).
- **Out of Combat / Encounter End**:
  - All readied maneuvers automatically recover at the end of an encounter (*Tome of Battle*, p. 40).
  - Out of combat or in protracted situations, taking no attacks, initiating no maneuvers, and receiving no enemy attacks for **1 full minute (10 consecutive rounds)** automatically recovers all expended maneuvers (*Tome of Battle*, p. 40).

---

## 7. Initiation Rules and Restrictions
- **Primary Source**: *Tome of Battle*, pp. 38–40, 44–46
- **Physical Requirements**: Initiating requires physical mobility (must be able to move). Speech / vocalization is **not** required (*Tome of Battle*, p. 38).
- **Concentration**: Initiating does not require a Concentration check. Taking damage while initiating does not disrupt or lose the maneuver (*Tome of Battle*, p. 39).
- **Attacks of Opportunity**: Initiating a maneuver or stance does not provoke attacks of opportunity unless explicitly stated in its description (*Tome of Battle*, p. 39). However, movement or charge actions executed as part of a maneuver provoke attacks of opportunity as normal (*Tome of Battle*, p. 39).
- **Targeting & Line of Effect**:
  - Requires line of effect (*Tome of Battle*, p. 45).
  - An opening of at least 1 square foot in an otherwise solid barrier permits line of effect (*Tome of Battle*, p. 45).
  - Unconscious targets are automatically considered willing; conscious but helpless/immobilized targets are not automatically willing (*Tome of Battle*, p. 45).
- **Saving Throw DC**:
  - DC formula is specified per maneuver, standard formula:
    $$\text{Save DC} = 10 + \text{Maneuver Level} + \text{Key Ability Modifier}$$
