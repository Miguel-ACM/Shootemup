# Threshing Drum: proposed cards

Code: `Assets/Scripts/Enemies/Bosses/PlainsThreshingDrumBoss.Proposed.cs`. The machine fires from its chute (0) and two drums (1-2). It moves through `station` at its existing drum speed. The traverse still reaches the visible edge (`Reach`). Bales stop and burst flat. Petals are the only added Plains shape.

**Pressure (Hard, rounds per second including rests; current → proposed):** phase one 102 → 95, phase two 101 → 93, phase three 111 → 118. Reel bats have nine arrows, and grain falls every third beat. Winnowing throws six volleys of 14 lanes. Chaff is only every fourth lane of alternate volleys, so at most about 130 petals are live (reserve 192). Scything blades have 13 arrows, with an 11-round cup every other stroke. In Bale Line the fuse burns one fan at a time: 0.2 s between chute bales, with drum bales halfway between. A row's fuse ends before the next row's begins.

## Phase one

**Current:** Windrows (an edge-to-edge traverse plants evenly spaced columns that release on one shared beat around a clear furrow) and Winnower (chute pods thrown sideways that turn into diagonal combs).

- **Reel.** The drums turn like a harvester's reel. Each beat, a drum throws one bat: a flat bar of bright arrows square to its heading. The side arrows fly faster in proportion, so the bar stays straight as it lengthens. Eight bats per sweep come over the top, out and down through the field, mirrored. Alternate sweeps shift half a step, so the bars never stack into fixed lanes. The machine drifts slowly, and dark grain from the chute answers the pilot. Tones: bright bats, dark grain.
- **Winnowing.** From a new station for every toss, the drums throw six interleaved volleys into a mirrored dome over the upper arena. Everything brakes and hangs glinting. On one shared beat the wind takes it all: heavy dark grain falls fast and straight, and light bright chaff (petals) drifts outward slowly and sways. One dome separates into two layers, which is what winnowing does. Grain trickles at the pilot while each dome hangs. Tones: dark grain, bright chaff.

Further ideas (not implemented):
- *Stubble Field*: short vertical sticks planted along the traverse, released from the edges inward.
- *Sheaves*: three-arrow bundles bound with a circle that untie into small fans after a glint.
- *Combine Header*: the reel bats only on the downstroke, in a fast continuous comb.

## Phase two

**Current:** the same Windrows and Winnower, deeper, plus the Return stroke: counter-turning drums comb the furrow with arrows that bounce three times.

- **Scything.** Both drums swing scythes at once. Each stroke is a crescent of bright arrows across 55 degrees, with the middle running ahead of the tips, so the blade bows forward and widens. Strokes sweep 55, 30, 5, -20 degrees and back, so the blades cross under the machine and the open ground keeps moving. Every few strokes, the chute lobs a dark gleaning cup at the pilot. Its rims run ahead of its middle, so it closes around anyone who waits. Tones: bright blades, dark cups.
- **Bale Line.** The machine traverses edge to edge and back. As the chute passes each mark (a fixed 0.75-unit spacing), it sets a dark bale down at one of two depths, a zigzag row. Every other mark, a drum adds a deeper, smaller bale. When the machine stops, a fuse burns along the row from the first bale toward it. Each bale bursts into a flat fan over 160 degrees below it, one fan at a time, so the fans chase the machine. Bright twine flicked up from the drums brakes high and drops down the sides, and aimed twine follows while the fuse burns. Tones: dark bales, bright twine.

Further ideas (not implemented):
- *Threshing Floor*: bales ring the machine's lane and burst inward in mirrored pairs.
- *Tedder*: a planted row turns over, releasing alternate rounds left and right to fluff the swath into crossing waves.
- *Double Scythe*: each stroke trails a slower, inverted crescent, giving lens-shaped gaps.

## Phase three (Hard)

**Current:** deeper windrows, more pods, and a longer return stroke with 24 teeth.

- **Last Sheaf.** Slower scythe strokes keep running while the drums toss four winnowing domes from shifting stations. Bright blades sweep under the dome's dark grain rain and drifting chaff. Tones: bright blades and chaff, dark grain and cups.

Further ideas (not implemented):
- *Harvest Home*: a bale line whose bales each throw a small winnowing dome instead of a fan.
- *Last Furrow*: the reel's bats planted along a traverse, released on one beat as stacked flat bars.
