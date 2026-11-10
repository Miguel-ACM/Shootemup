# Threshing Drum: proposed phases

Code: `Assets/Scripts/Enemies/Bosses/PlainsThreshingDrumBoss.Proposed.cs`. Each phase is one concept that develops over the phase (Miguel, 10 November 2026). The machine fires from its chute (0) and two drums (1-2). It moves through `station` at its existing drum speed. Its traverse still reaches the visible edge. Bales stop and burst flat. Petals are the only added Plains shape.

**Pressure (Hard, rounds per second averaged over the concept's cycle; current → proposed):** phase one 102 → 106, phase two 101 → 99, phase three 111 → 108.

## Phase one: Winnowing

**Current:** Windrows (an edge-to-edge traverse plants evenly spaced columns that release on one beat around a clear furrow), alternating with Winnower (chute pods thrown sideways that turn into diagonal combs).

**Proposed:** from a new station for every toss, the drums throw six interleaved volleys into a mirrored dome over the upper arena. Everything brakes and hangs, then the wind takes it on one beat. Heavy dark grain (arrows and kernels) falls fast and straight. Light bright chaff (petals) drifts outward slowly and sways. One dome separates into two layers, which is what winnowing does.

The wind develops over an eight-toss cycle of about 17 s:
- Still air.
- Gusts leaning the grain inward.
- Gusts leaning it out to the walls.
- Doubled tosses, with a lower second dome that lets go just after the first.

A dark grain trickle answers the pilot while each dome hangs. Chaff is limited to keep at most about 130 petals live (reserve 192). Tones: dark grain, bright chaff.

Alternative single concepts (not implemented):
- *Reel*: the drums throw flat bars of arrows like a harvester's reel bats. The reel speeds up, reverses and changes bar length over the phase.
- *Windrows, developed*: the shared-release columns with a furrow that moves and splits as the field is worked.
- *Stubble Field*: short planted sticks released from the edges inward, closing faster each traverse.

## Phase two: Scything

**Current:** the same Windrows and Winnower, plus the counter-turning Return stroke (teeth that bounce three times).

**Proposed:** both drums swing scythes. Each stroke is a crescent of bright arrows whose middle runs ahead of its tips, so the blade bows forward and widens. Strokes sweep out, in and back, crossing under the machine. A dark gleaning cup is lobbed at the pilot every other stroke; its rims run ahead of its middle, closing around anyone who waits.

The swing develops in four 3.6-s variations:
1. Mirrored strokes.
2. The right drum half a sweep behind the left, so the blades interleave.
3. Backhand strokes whose tips lead.
4. The machine walks the field from side to side while scything.

Tones: bright blades, dark cups.

Alternative single concepts (not implemented):
- *Double Scythe*: each stroke trails a slower inverted crescent, making lens-shaped gaps that shrink over the phase.
- *Tedder*: a planted swath turns over, alternate rounds released left and right to fluff it into crossing waves, faster each pass.
- *Threshing Floor*: bales ringing the machine's lane burst inward in mirrored pairs, the ring widening each time.

## Phase three (Hard): Bale Line

**Current:** deeper windrows, more pods and a longer return stroke.

**Proposed:** the machine traverses edge to edge and sets dark bales down at a fixed spacing. When it stops, a fuse bursts them into flat downward fans over 160 degrees. Only one fan bursts at a time, except the two far-apart ends of a paired fuse.

The concept develops over an eight-traverse cycle of about 21 s:
- **The fuse** changes every traverse: toward the machine, away from it, both ends inward, then the centre outward.
- **The row** changes too: a zigzag of two depths, a level row, then three tiers.
- **Drum bales** are added every other mark and burst between their neighbours.

Bright twine flicked from the drums brakes high and drops down the sides, and aimed twine binds the pilot while each fuse burns. Tones: dark bales, bright twine.

Alternative single concepts (not implemented):
- *Harvest Home*: a bale line whose bales each throw a small winnowing dome instead of a fan.
- *Last Furrow*: reel bats planted along a traverse and released on one beat as stacked flat bars, more layers each pass.
