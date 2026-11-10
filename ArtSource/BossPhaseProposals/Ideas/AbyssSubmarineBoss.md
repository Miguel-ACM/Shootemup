# Abyss submarine: proposed cards

Code: `Assets/Scripts/Enemies/Bosses/AbyssSubmarineBoss.Proposed.cs`. The only shipping edit is the review-switch line at the top of `Attack()`.

The submarine is Miguel's named exception to one concept per phase: its dive cycle stays the structure. `MoveBoss` still:
- dives
- marks the emergence spot
- erupts with the 360 crowns
- charges with its non-aimed foam wake and depth charges
- keeps the hull undamageable while submerged

The book fills only the surfaced stretch of each cycle, with **one concept per phase** and a new variation of it on every surfacing. `Wait()` holds through every dive. A verse that a dive interrupts is dropped, not finished later.

Fire leaves the prow, the two launch banks and the shoulder launch channels; the upper channel lane serves the top. Every big release is preceded by a charge on those ports. Rounds: torpedoes and beads (circles), arrows, and Underwater droplets (within the warmed reserve).

Pressure per surfacing on Hard:

| Phase | Proposed | Current | Difference |
| --- | --- | --- | --- |
| One | ~156 rounds | ~160 | -3% |
| Two | ~460 | ~530 | -13% |
| Three | ~435 | ~480 | -9% |

On Normal and Easy the longer rests let a dive cut the end of a verse, just as they do the shipping beats.

## Phase one: Torpedo Herringbone

**Current:** torpedo racks that wind up, rain plumes thrown up in three speed layers, and mirrored current arrows (three alternating ideas).

Each surfacing fires three salvos, twelve torpedoes in all. Each dark torpedo launches with a bright line of droplets at graded speeds. The droplets hang where the torpedo passed, then peel off to alternate sides, the farthest first, so each track opens into a widening V as the torpedo accelerates away. The surfacings rotate through three variations:
1. *Spread*: three tubes aimed, four unaimed and mirrored, then five aimed.
2. *Crossing*: the shoulder tracks cut inward under the outward bank tracks, so the wakes form an X.
3. *Arrowheads*: the wakes peel forward instead of back, so each track trails a chevron.

Alternative single concepts (not implemented):
- *Caustic Net*: two counter-swaying arrow strands per shoulder braid into lens chains with beads and travelling gaps. It varies by splay and by the gap rhythm. It was implemented in an earlier draft.
- *Sonar Sweep*: an arrow fan that sweeps across the arena with a dark ping on each beat. Surfacings change the sweep direction and width.
- *Ballast Blow*: droplets vented upward that rise past the hull and fall back as bubbles bursting into short forward cones.

## Phase two: Countercurrent

**Current:** dark rain plumes, a bright three-arm mirrored pressure lace, and dark aimed arrow wavefronts.

Each surfacing fires eleven fans from both shoulders. Every round is caught by a current flowing back toward the centre and down, scaled so each loop turns short of its own wall wherever the hull has drifted. The loops return across the middle, so the two sides cross into a sinking lattice. Fast bright arrows loop wide around slow dark beads, and every other fan is offset by half a lane. A short dark droplet jet from the prow keeps a still pilot moving. The surfacings rotate:
1. *Wide*: level fans, crossing mid-arena.
2. *Fountain*: raised fans whose steep lanes climb to the top edge and fall back.
3. *Tight*: low fans in a stronger current, with small loops that cross close under the hull and then flash across the arena.

Alternative single concepts (not implemented):
- *Kelp Forest*: droplet stalks planted from the banks and channels that hang, then sink rippling, with arrow shoals between. It varies by lean and stalk length. It was implemented in an earlier draft.
- *Undertow*: rounds dropped straight down that a sideways current drags into a slanting sheet, reversing on every surfacing.
- *Pressure Fronts*: aimed arcs that slow, stop together and then advance again as one straight front, from more ports each surfacing.

## Crush Depth (Hard, phase three): Hadal Trench

**Current:** rain, torpedo racks under a bright lace, and dark wavefronts on every surfacing.

On every surfacing the shoulder channels hang dark circular cliffs in columns along both walls, from above the hull down to mid-arena. The cliffs slide inward together, so open water narrows into a trench. Four torpedo herringbones run down it, each preceded by a charge on the tubes. A bright droplet curtain closes the surface. Alternate surfacings close the cliffs diagonally (sinking) or almost level (pinching), and swap the half-row offset.

Alternative single concepts (not implemented):
- *Implosion*: the cliffs stop again mid-arena and collapse to the centre line as the charge begins.
- *Abyssal Lantern*: a slow lure round drifts toward the pilot; every salvo aims at the lure, not the ship.
- *Crush Rings of Arcs*: arcs (never full circles) from both shoulders that close in like a vice, tighter each surfacing.
