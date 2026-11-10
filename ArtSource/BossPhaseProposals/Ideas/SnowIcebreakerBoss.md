# Snow icebreaker: proposed phases

Code: `Assets/Scripts/Enemies/Bosses/SnowIcebreakerBoss.Proposed.cs` (the class is now `partial`; nothing else in `SnowIcebreakerBoss.cs` changed). Each phase is one concept that runs for the whole phase and develops in movements. Every round leaves the prow or a shoulder. The ship's existing movement helpers (Sail, LineUp, Ram, PrimeAll, Place) are reused. Rounds per second are estimated within about 10% of the current phase.

## Phase one (pack ice): Bow Wave

**Current:** Ramming Speed (rams with fracture fans and re-aiming bow spray) alternating with Fracture Sheet (two-course whole-width sheets that crack under the bow).

Wherever the ship surges, its prow throws both arms of a bow wave, so a V-shaped wake trails behind it. Each crest freezes, then slides off down its own side, slow at first and accelerating. Every fourth crest is round, and the shoulders now and then throw a crystal dart. The course changes with each movement:
1. A W through the arena.
2. Three rams at the pilot: a long wake on the dive, a short one backing astern.
3. Breakers across the middle, then back along the top.
4. A figure eight crossing itself in the upper arena.

From the second round, alternate courses freeze their crests and re-aim them at the pilot instead. The water settles (a short rest) between courses. Tones: bright wake, dark darts.

Alternative single concepts:
- *Fracture Field*: every ram cracks the pack. First single fracture fans, then sheets that open under the bow, then cracks that branch, then a sheet-and-fan quake.
- *Pressure Crack*: one long zigzag crack of hanging arrows runs from the prow. Its segments slide off perpendicular to themselves, and the crack lengthens and forks over the phase.
- *Keel Ripples*: each surge sends mirrored ripple arcs that freeze at different radii and fall. The arcs multiply and interleave as the ship speeds up.

## Phase two (open water): Closing Pack

**Current:** Cut Channel (a Z run lined with frozen walls that zip shut) alternating with Calving Bergs (gravity bergs that rebound and split over swinging pressure ridges).

The shoulders throw ice along the side walls: a column the full height of the arena with one opening, forming from the top down. The columns break off and close in across the arena. The movements are:
1. Staggered pairs: the right column follows half a throw later, so they cross off centre.
2. Sheared columns that break off from the top down and lean as they close.
3. A tide of single walls from one side with a stepping opening.
4. A nip: both walls at once with their openings level, meeting mid-arena.

The openings walk down the arena on one pass and up it on the next. Each throw brings slow prow pellets, and each movement ends in slack water: a charged prow fan that freezes and re-aims. From the second round the walls close faster. Tones: dark walls, bright pellets and fans.

Alternative single concepts:
- *Aurora Curtain*: drifting broadside, the shoulders let down braided, dashed ribbons that sway against each other. Over the phase the braid widens, the dashes shift and the prow joins with a third ribbon.
- *Calving Bergs*: bergs thrown on arcs that rebound and calve. They calve into fans, then into hanging floes, then into bergs that calve twice.
- *Cut Channel*: the ship cuts leads lined with frozen walls that zip shut. The courses become a Z, then an S, then crossing leads.

## Phase three (Hard): Shattered Icicles

**Current:** Icequake (rams that crack a fracture fan and a fresh sheet together, then a zipped channel back up).

Lances of paired arrows at rising speeds freeze and shatter: every pair splits mirror-wise, the deeper links splaying wider, so each lance opens into a tree of chevrons. The crystal tip, in the other tone, re-aims at the pilot. The movements are:
1. A volley of three parallel lances.
2. A fan of five from the prow.
3. Crossfire from both shoulders while the ship slides across the top.
4. A ram whose impact drives a fan of lances straight down.

The dominant tone alternates by movement, and from the second round the lances splay wider.

Alternative single concepts:
- *Glacial Breach*: the bow-wave W with pack walls thrown at the foot of every dive. The walls multiply as the ship speeds up.
- *Icequake*: rams whose impacts lay a sheet, then fracture fans, then both, with the sheet's crack following the pilot.
- *Frozen Armada*: the ship's wake hardens into pack walls behind it, so its own course draws the maze.
