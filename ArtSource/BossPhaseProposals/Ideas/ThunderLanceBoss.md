# Thunder lance: proposed cards

Code: `Assets/Scripts/Enemies/Bosses/ThunderLanceBoss.Proposed.cs`. The only change to existing code is the first line of `Attack()`: `if (ProposedCards && OpenCards()) return;`. That line also skips Attack's pylon deployment, so the book deploys and dismisses its own Tesla pylons.

`PrepareScore` keeps cueing the shipping score's muzzles from the volley count. The book holds that count on a silent beat, so those cues disappear, and its own `Charge()` and beam warnings provide the anticipation.

Each phase has **one concept** that runs for the whole phase. It develops through four movements of about fifteen seconds. Beams always stay bright.

Bullet pressure is measured in rounds per second on Hard (current figure in brackets), on top of the beams: Pendulum Pylons about 35 (37), Squall Line about 21 (21), Faraday Cage about 21 (21).

## Phase one: Pendulum Pylons

**Current:** a hexagon of six pylons whose beams swing 60 degrees around straight down, with mirrored lace, aimed chevrons and a wavefront.

Tesla pylons hang on the visible top edge and swing their beams like pendulums, and the pilot rides the corridors between them. The lance fires bright zigzag rice down the pilot's corridor. The shoulders crackle with dark arrows that stall and re-aim and with mirrored sparks, and a dark thunder roll (slow rice that suddenly quickens) falls every fourth beat. Each movement recalls the pylons and deploys a new set from the lance; that lull belongs to the card.

The movements:
- **In Step:** four pylons, ±15 degrees together.
- **Breathing:** the right pair mirrors the left, so the middle corridor opens and closes.
- **Six Pendulums:** a smaller, slower swing with narrower corridors.
- **Lance:** In Step, with the boss's own attached lance beam driven down the middle every eight beats. The shoulders take over its bolts.

Alternative single concepts:
- *Lightning Lance*: the boss's own beams, a lance, then shoulder scissors closing from the walls, with rice bolts into the middle.
- *Metronome*: one pylon pair whose swing speeds up beat by beat.
- *Jacob's Ladder*: wall pylons climbing with arcs of rice between their tips.

## Phase two: Squall Line

**Current:** a lattice of four top and six wall pylons, banks alternating and stepping, with zigzag lightning and chevrons.

Six pylons on the top edge lean their beams alternately 24 degrees right or left, like slanting rain. The whole phase uses the same pylons. The lance throws bright forked lightning (zigzag bolts that split into three beads), and dark capacitor beads quicken from the shoulders.

The movements:
- **Gusts:** the sets take turns, gliding downwind while they fire.
- **Gale:** shorter gusts that glide further.
- **Crosswind:** both sets fire together as a diamond lattice, shut down, step half a spacing and fire again.
- **Squall:** each set crosses a third of the arena while it fires.

Alternative single concepts:
- *Storm Front*: a comb of downward beams marching across with one moving gap.
- *Downpour*: slanted beams whose lean grows gust by gust.
- *Ground Strike*: a pylon drops to the bottom edge and fires straight up where the pilot was.

## Hard final: Faraday Cage

**Current:** Superstorm (the full lattice sweeping while it fires), with lightning fans and zigzag rings.

Wall pylons fire short horizontal beams inward: bars with a 1.7-unit door, which climb and fall through the arena like ladder rungs. The lowest bar reaches the bottom of the arena. Bar ranges overlap, so no level stays clear and the bars never cross. The shoulders shed dark zigzag arrows, and bright rice triplets are aimed through the doors. Each movement deploys a new cage.

The movements:
- **Ladder:** two bars.
- **Swapped Doors:** the doors change sides and it climbs faster.
- **Three Rungs:** three bars, doors alternating.
- **Storm Cage:** two bars, with forked lightning through the doors.

Alternative single concepts:
- *Supercell*: the squall without rests, gusting faster and further.
- *Tesla Coil*: pylons that orbit the boss's lance beam as it sweeps.
- *Conductor*: rounds passing a live beam turn into fast zigzags (needs new code).
