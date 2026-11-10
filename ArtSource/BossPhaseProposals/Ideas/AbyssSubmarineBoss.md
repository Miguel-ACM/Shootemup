# Abyss submarine: proposed cards

Code: `Assets/Scripts/Enemies/Bosses/AbyssSubmarineBoss.Proposed.cs`. The only shipping edit is the review-switch line at the top of `Attack()`.

The dive cycle is still the spell sequence. `MoveBoss` still dives, marks the emergence spot, erupts with the 360 crowns, charges with its non-aimed foam wake and depth charges, and keeps the hull undamageable while submerged. The book fills only the surfaced stretch of each cycle. It plays one verse per surfacing and takes the phase's two cards in turn. `Wait()` already holds through every dive. A verse that a dive interrupts is dropped, not finished on the next surfacing.

Fire leaves the prow, the two launch banks and the shoulder launch channels; the upper channel lane serves the top. Every big release is preceded by a charge on those ports. Rounds: torpedoes and beads (circles), arrows, and Underwater droplets.

Pressure per surfacing on Hard:

| Phase | Proposed | Current | Difference |
| --- | --- | --- | --- |
| One | ~150 rounds | ~160 | -6% |
| Two: Countercurrent | ~460 | ~530 | -13% |
| Two: Kelp Forest | ~470 | ~530 | -12% |
| Three: Hadal Trench | ~435 | ~480 | -9% |

Most of the shipping phase-two count comes from the pressure lace, about 180 rounds in 1.8 s. Kelp Forest keeps its live droplets within the district's warmed reserve of 256. On Normal and Easy the longer rests let a dive cut the end of a verse, just as they do the shipping beats.

## Phase one

**Current:** torpedo racks that wind up, rain plumes thrown up in three speed layers, and mirrored current arrows that curl outward.

- **Torpedo Herringbone.** Three salvos: three tubes (aimed), then four (unaimed, mirrored about straight down), then all five (aimed). Each dark torpedo launches together with a bright line of droplets at graded speeds. The droplets stop where the torpedo passed, then peel off to alternate sides, the farthest first, so each track opens into a widening V as the torpedo accelerates away. Wakes fade after a few seconds. The current card is mainly falling arcs. This one is straight tracks that open into feathered Vs.
- **Caustic Net.** Each shoulder channel pours two dark arrow strands that sway in opposite phase. They braid into chains of lens cells that ripple as the hull drifts and as the splay breathes. Bright pearls run down each chain's axis. Every chain breaks for two beats in eight, which gives the pilot a gap that moves. A bright spout hangs droplets in a line along the top edge, inner ones first, then sinks as a swaying curtain. It does this twice, so the surface is no refuge.

Further ideas (not implemented):
- *Sonar Sweep*: an arrow fan from the prow that sweeps left to right with a dark ping on each beat.
- *Ballast Blow*: droplets vented upward from the stern that rise past the hull and fall back as bubbles bursting into short forward cones.
- *Periscope*: a single slow bright round raised to the top edge that marks the next salvo's aim before the tubes fire.

## Phase two

**Current:** dark rain plumes, a bright three-arm mirrored pressure lace, and dark aimed arrow wavefronts.

- **Countercurrent.** Eleven level fans from both shoulders. Every round is caught by a current flowing back toward the centre and down, scaled so each loop turns just short of its own wall wherever the hull has drifted. The loops return across the middle, so the two sides cross into a lattice that sinks and widens. Fast bright arrows loop wide around slow dark beads. Every other fan is offset by half a lane, and the upward lanes reach the top edge. A short dark droplet jet from the prow keeps a still pilot moving. Nothing in the current book curls back.
- **Kelp Forest.** Six plantings. The launch banks plant dark droplet stalks down into the arena, and the upper channels plant them out along the top. Each stalk is one lane at graded speeds, so it stands as a line. It hangs, then sinks swaying. Each droplet sways at a slightly different rate, so the stalk ripples like kelp, and the clearings move as the lean alternates narrow and wide. Mirrored bright arrow shoals dart at the pilot on both half-beats between plantings: small diamond schools whose rows are made by speed.

Further ideas (not implemented):
- *Undertow*: rounds dropped straight down that a sideways current drags into a slanting sheet, reversing every surfacing.
- *Pressure Fronts*: aimed arcs that slow, stop together and then advance again as one straight front.
- *Riptide*: two crossing currents (left and right Fall) applied to alternate rows, so one fan shears into a plaid.

## Crush Depth (Hard, phase three)

**Current:** rain, torpedo racks under a bright lace, and dark wavefronts on every surfacing.

- **Hadal Trench.** On every surfacing the shoulder channels hang dark circular cliffs in columns along both walls, from above the hull down to mid-arena. Alternate walls are offset by half a row. The cliffs slide down and inward together, so open water narrows into a sinking trench. Four torpedo herringbones run down it, each preceded by a charge on the tubes. A bright droplet curtain closes the surface. It combines phase one's herringbone with a moving wall shape that no current card uses.

Further ideas (not implemented):
- *Implosion*: the cliffs stop again mid-arena and collapse to the centre line as the charge begins.
- *Abyssal Lantern*: a slow lure round that drifts toward the pilot; the next herringbone salvo aims at the lure, not the ship.
