# Snow icebreaker: proposed cards

Code: `Assets/Scripts/Enemies/Bosses/SnowIcebreakerBoss.Proposed.cs` (the class is now `partial`; nothing else in `SnowIcebreakerBoss.cs` changed). Every round leaves the prow or a shoulder. The cards reuse the ship's existing movement helpers (Sail, LineUp, PrimeAll, Place). Pressure is within about 25% of the current cards.

## Phase one (pack ice)

**Current:** Ramming Speed (rams down; mirrored fracture fans and bow spray that freezes and re-aims) and Fracture Sheet (two-course whole-width sheets crack open under the bow or shoulders).

- **Bow Wave.** The ship surges through the arena on a W, down and up twice. Its prow throws both arms of a bow wave as it goes, so a V-shaped wake trails behind it. Each crest freezes, then slides off down its own side, slow at first and accelerating. Diving legs leave the wake over the top of the arena, and rising legs leave it below. Every fourth crest is round. Both shoulders now and then throw a crystal dart that crawls at the pilot and accelerates. Tones: bright wake, dark darts. The ship's movement draws the pattern, which fits an icebreaker better than a fixed fan.
- **Shattered Icicles.** Lined up over the pilot after a charge, all three ports drive parallel lances along the prow's aim. Each lance is a string of paired arrows at rising speeds. The lances freeze, then shatter: each pair splits mirror-wise, the deeper links splaying wider, so each lance opens into a tree of chevrons. The crystal tip re-aims at the pilot after the shatter. Tones: dark lances, bright tips.

Further ideas (not implemented):
- *Pressure Crack*: one long crack of hanging arrows runs zigzag down from the prow, then each segment slides off perpendicular to itself.
- *Ice Floe Mosaic*: the shoulders place small floes (clusters of five) in a staggered grid, which drift apart from the ship's track as it passes.
- *Keel Ripples*: each ram sends three mirrored ripple arcs outward that freeze at different radii and fall.

## Phase two (open water)

**Current:** Cut Channel (a Z run lined with frozen walls that zip shut) and Calving Bergs (gravity bergs rebound and split at their apex, over swinging pressure ridges).

- **Aurora Curtain.** Drifting broadside, each shoulder lets down a braided ribbon of arrows, two strands swaying against each other. The ribbons are dashed: the right curtain's gaps fall half a beat after the left one's. The curtains ripple and sweep as the ship drifts. The prow charges and fires fans that freeze and re-aim, so standing between curtains is not safe. Tones: bright curtains, dark prow fans.
- **Closing Pack.** The shoulders throw ice along both side walls: a column the full height of the arena on each side, with one opening. The column forms from the top down. The left column breaks off and closes in across the arena. The right one follows half a throw later, so they cross off centre. Both openings walk along the walls throw by throw, the right one a little behind. The direction alternates between passes. The prow fires slow, accelerating pellets. Tones: dark walls, bright pellets. It is a horizontal frogger that covers the top and bottom of the arena alike.

Further ideas (not implemented):
- *Lead Ice*: the ship cuts straight leads across the arena that stay open as moving corridors in an otherwise frozen sheet.
- *Growler Field*: large round "growlers" bob down with Sway and split into short arrow fans when they pass the ship's depth.
- *Whirlpool Wake*: the ship circles; its wake freezes and slides toward the centre of the loop before falling.

## Phase three (Hard)

**Current:** Icequake (rams that crack a fracture fan and a fresh sheet together, then a zipped channel back up).

- **Glacial Breach.** The bow-wave W again. A fresh pair of staggered pack walls is thrown at the foot of each dive, and a lance is driven at the pilot from each crest. Pressure is similar to Icequake.

Further idea: *Polar Convoy*: Aurora Curtains dropped while the ship rams the pack walls apart.
