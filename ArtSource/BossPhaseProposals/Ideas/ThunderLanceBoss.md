# Thunder lance: proposed cards

Code: `Assets/Scripts/Enemies/Bosses/ThunderLanceBoss.Proposed.cs`. The only change to existing code is the first line of `Attack()`: `if (ProposedCards && OpenCards()) return;`. That line also skips Attack's pylon deployment, so every proposed card deploys and dismisses its own Tesla pylons.

`PrepareScore` keeps cueing the shipping score's muzzles from the volley count. The book holds that count on a silent beat, so those cues disappear, and its own `Charge()` and beam warnings provide the anticipation.

Beams always stay bright. Bullets are rice zigzags, arrows and circles. Phase one's bullet rate matches the current cards to within about 10% (about 35 rounds per second for Pendulum Pylons and 33 for Lightning Lance, against 37), on top of its beams. Phase two is about 25 and 17, and Supercell about 27, against 21 for the current lattice phases.

## Phase one

**Current:** a hexagon of six pylons whose beams swing 60 degrees around straight down (the middle is mostly safe), with mirrored lace, aimed chevrons and a three-arc wavefront.

- **Pendulum Pylons.** Four pylons hang on the visible top edge and swing their beams together, 15 degrees either side of straight down. The three corridors between them sway across the arena as one, and the pilot rides a corridor. The lance fires bright zigzag rice bolts down the pilot's corridor. The shoulders crackle with dark arrows that stall and re-aim, and a dark thunder roll (slow rice that suddenly quickens) falls every fourth beat. Mirrored zigzag sparks from the shoulders fall every third beat.
- **Lightning Lance.** The boss fires its own attached beams. First the lance: one wide beam straight down while both shoulders throw dark zigzag rice to the sides. Then the scissors: both shoulder beams start out toward the walls and close to 15 degrees either side of straight down and back. Meanwhile the lance pours bright bolts into the open middle, keeping Miguel's "middle mostly safe" feeling. The boss moves to a new lane between releases (it holds still while a beam is attached). The charges are never quiet: the shoulders crackle with stalling arrows and sparks while the lance charges, and zigzag arrows pour into the middle while the scissors charge. Long phrases alternate rice and zigzag arrows to stay within the rice reserve.

## Phase two

**Current:** a lattice of four pylons on the top edge firing down and six on the wall firing across. The banks alternate and step to new positions, with zigzag lightning and chevrons.

- **Squall Line.** Six pylons sit on the top edge, alternate ones leaning their beams 24 degrees right or left. The two sets take turns. One set fires slanted parallel beams while gliding downwind, then shuts down and turns its emitters to the sky as the other set charges and leans the other way. The rain slants with each gust. The lance throws bright forked lightning: zigzag bolts that split into three beads. Dark capacitor beads quicken from the shoulders.
- **Faraday Cage.** Four wall pylons fire short horizontal beams inward, forming two bars with a door each (right and left of centre). The bars move up and down together like ladder rungs 5.4 units apart. The lower bar reaches the bottom of the arena, and their ranges overlap, so no level stays clear and the bars never cross. The pilot slips through one door, then the other. The shoulders shed dark zigzag arrows, and bright rice triplets are aimed through the doors.

## Hard final

- **Supercell.** The squall never stops: faster gusts that glide further, forked lightning, and dark zigzag arrow fans from the shoulders on alternate beats.

## Further ideas (not implemented)
- *Jacob's Ladder*: two pylons climb the side walls with horizontal beams, and arcs of rice jump between their tips as they rise.
- *Ground Strike*: a pylon drops to the bottom edge and fires straight up, the only upward beam, aimed where the pilot was.
- *Conductor*: rounds that pass a live beam turn into fast zigzags (a beam-proximity trigger, which needs new code).
