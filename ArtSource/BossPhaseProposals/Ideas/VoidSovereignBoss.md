# Void Sovereign: proposed forms

**Final choice (Miguel, November 2026).** Void keeps its shipping cards, teleports and interludes, and the `.Proposed.cs` file is gone. In `VoidSovereignBoss.Cards.cs`:

- **No aimed rounds.** Each aimed reply is now a mirrored moon fan from both eyes (`SweepingReply`). The fans cross beneath the body on a heading that sweeps slowly with the beat, and are larger than the replies they replace, so the pressure does not drop.
  - Funeral Psalm's aimed pearls now halt and resume down crossing diagonals.
  - Cut the Firmament's middle lane turns straight down.
  - The final form's crossfire (`CrossedGaze`) charges both side tips, then crosses kunai, with a moon at the heart of each chord, through a fixed point above the star that steps left, centre and right in turn.
- **More moons, kunai and giant circles.** Circular lanes are now moons and arrow lanes kunai (chevrons where a card asks for them). Crowns come more often: Witnesses and World Without Edges every third beat, Severing every fourth. They remain lone giants with their neighbouring lanes left empty.

The proposal text below is kept for reference.

Code: `Assets/Scripts/Enemies/Bosses/VoidSovereignBoss.Proposed.cs`. No shipping file changed.

Each form is one concept that runs for the whole form. It develops in movements:

- **Movement.** Teleport, prime, one movement of about 4.5 s, then a rest. The teleports and the fifteen-second one-sided interludes are unchanged: they are the form's built-in structure.
- **Variation and escalation.** The movement number picks the variation, and each completed cycle escalates. Later cycles run up to 15% faster and have narrower slits or fewer rests.
- **Over 60 s.** A recording holds about eight movements, so each variation plays twice.

`CardIndex` alternates by movement for the review tools. Each form leads with one tone (mask dark, mantis bright, maw dark, star bright) and flips it each movement. Giants and aimed replies take the other tone.

Every round leaves the form's visible eyes, and every concept reaches the top edge. Giant orbs are lone accents with their neighbouring lanes left empty. Rounds used: kunai, crescents, gems, chevrons, needles, ricochets, pearls, giants and Void rays.

Rough average pressure on Hard, in rounds per second over a whole loop including teleports and rests:

| Form | Proposed | Shipping form | Difference |
| --- | --- | --- | --- |
| One | ~48 | ~46 | +4% |
| Two | ~24 | ~26 | -8% |
| Three | ~43 | ~49 | -12% |
| Four | ~17, plus rays | ~19 | -10% |

## Form one (horned mask): Weeping Mask

**Current:** Witnesses (butterfly seams) and Funeral Psalm (a pearl pall that folds) alternate.

**Weeping Mask:** both eyes weep fountains of kunai that rise toward the top edge and arc down under gravity. Inner lanes cross over the mask, and outer lanes fold once off the side walls. A slit moves through every fountain, and gem pairs answer a resting pilot.

| Movement | Development |
| --- | --- |
| 1 (welling) | Inner lanes only, crossing over the mask. |
| 2 (weeping) | Full fountains, with lone giant tears down the empty middle. |
| 3 (sobbing) | The eyes take turns in doubled pulses: a fast layer and a slow, counter-breathing layer. |

Each later cycle swings the breath wider and narrows the slit.

Other single concepts:
- *Ringed Horns*: one stream per eye sweeps from straight up, over the outside, to straight down and curls into a horn ringed with crescents. More rings each cycle.
- *Mask's Smile*: a crescent arc hung beneath the mask tilts and drops in pieces, and each movement hangs a wider smile.

## Form two (mantis): Praying Mantis

**Current:** Severing (cutting ribbons with ricochets) and Cut the Firmament (rays over pleats that pause and turn) alternate.

**Praying Mantis:** the mantis hunts. It stalks with its forelegs held down as rays, then strikes with hooked kunai blades that halt, swing 40° and carry on as the same curve. Crescents answer a rest.

| Movement | Development |
| --- | --- |
| 1 (stalk) | Forelegs open and fold without closing the aisle between them. The wings run the ceiling into the corners and down the walls, and pearls drip between the forelegs. |
| 2 (strike) | Alternate arms strike across the arena. |
| 3 (strike from above) | Ricochet blades thrown off the ceiling between the low cuts. |
| 4 (feed) | Forelegs, blades crossed from both arms, and the wings together. |

Each later cycle cuts faster.

Other single concepts:
- *Compound Eye*: a hexagonal grid of gems stops in place and blinks out lane by lane, with a wider grid each movement.
- *Molting*: blades that split into forward shrapnel cones when they halt, shedding more skin each cycle.

## Form three (octagonal maw): Gnashing Maw

**Current:** Hunger (nested parabolic teeth with plasma) and False Stars (splitting comets) alternate.

**Gnashing Maw:** the mandibles bite and set teeth. A jaw of kunai is thrown out, halts, and snaps back inward across the other jaw. Rows of teeth are flung to exact places, hang, and fall jagged, each row with its own moving gap. Crescents answer a rest.

| Movement | Development |
| --- | --- |
| 1 | Low bites below the maw. |
| 2 | High bites that open over the maw and snap down from near the top. |
| 3 | Three rows of teeth across the upper arena. |
| 4 (gnashing) | Rows and bites in turn. |

Each later cycle rests less.

Other single concepts:
- *Overbite*: two rows of teeth slide sideways in opposite directions before they fall, faster each movement.
- *Swallow*: thrown ricochets curve back into the maw. This borrows Space's gravity grammar, so it is held back.

## Form four (crystal star): Prism Lattice

**Current:** World Without Edges (opposed fracture pairs stitching a lattice) and Last Equation (ricochets with giant tips) alternate, both with charged crossfire.

**Prism Lattice:** the crystal star refracts and shatters. Four rays turn as a slow cross. Crystal bars of kunai hang along the axes, then shatter: alternate teeth turn 90° each way, and each bar opens into two dotted walls that travel apart. Charged crossfire from the side tips closes the body's shadow throughout.

| Movement | Development |
| --- | --- |
| 1 | The cross turns while the points refract gem fans into the quadrants, bending them with the turn. |
| 2 | The points lay bars that shatter. |
| 3 | The cross turns back and sheds bars from beneath its own rays. |
| 4 | The whole star shatters at once into an expanding square of walls. |

Each later cycle quickens.

Other single concepts:
- *Refraction Chain*: one aimed gem splits into a three-way fan at each pause, twice; each movement adds a generation.
- *Snowblind Facets*: crescents halt on a ring and leave on the tangent. That is a moving circle, which is Space's grammar, so it is held back.

## Concerns

- Weeping Mask kunai keep accelerating under gravity, reaching about 5 to 6 units/s near the floor before they fade.
- Gnashing Maw rows and Prism Lattice bars use `WorldSpeed` so the teeth stop on exact places. The far teeth launch at up to about 8 units/s before braking, always outward from the body.
- Form four is the lightest in rounds, as it is in shipping. Its pressure comes from the rays, the walls and the crossfire.
