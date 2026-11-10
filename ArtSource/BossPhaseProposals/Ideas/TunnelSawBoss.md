# Tunnel saw: proposed cards

Code: `Assets/Scripts/Enemies/Bosses/TunnelSawBoss.Proposed.cs`. Every round leaves the centre of the blade. Phase one fires widening rings from stations, with no bouncing. From phase two the saw fires only on impact: never on a timer and never when hit. There are no minions. Ring rotation stays at or below 30 degrees a second, with the 2.8 units/s rim cap. Pressure is within about 25% of the current cards.

Phase two needs the proposed burst on each impact, and the shipping `ImpactBurst` cannot be changed. The proposed book therefore drives the ricochet itself (`Patrol`/`Glide`): same speed formula, walls, impact warning and contact damage. The shipping patrol stands down through `haltUntil`. A guarded `OnPhaseEntered` override keeps it still through phase breaks while the review switch is on. Nothing changes when the switch is off.

## Phase one (stations)

**Current:** Frogger Crossing (six widening rings that turn in alternate directions, each with a four-bead gap that slides).

- **Gear Train.** At each new station the saw throws three pairs of widening cogs. The bright cog has six raised teeth of large beads and turns one way. The dark cog has six notches, turns the other way and widens more slowly. Their openings sweep past each other like meshing gears. A fast rice tracer flies at the pilot with each pair, pairing slow and fast. Tones: bright outer cog, dark inner cog and tracer. It keeps Frogger's count and turn speed but gives each ring a saw-like silhouette.
- **Spiral Stair.** Five pairs of non-rotating rings: a fast ring of small bright beads and a slow ring of large dark teeth, sharing one opening. Each pair's opening turns 16 degrees further round (the direction alternates by station), so the openings climb a spiral staircase. The first opening faces the pilot. Tones: dark slow teeth, bright fast beads. The fast ring passes first, then the slow one, so the pilot must hold the opening through both.

Further ideas (not implemented):
- *Escapement*: rings that tick (turn in steps of one bead) rather than turning smoothly, with the gap moving on each tick.
- *Planetary Gears*: three small cogs orbiting a big one, all widening. This is close to Space's circles, so it would need Miguel's approval.
- *Clutch*: two rings with identical gaps released together, one widening faster, so the gap closes and reopens.

## Phase two (ricochet, impact-only)

**Current:** Ricochet Storm (each impact: a 360-degree burst of fast arrows and slow teeth, some bouncing, an aimed shotgun and wall runners).

- **Buzzsaw Bloom.** Each impact opens a full turn of hooked blades: three rice grains per blade launched together at rising speeds, each lagging further behind the spin. The burst opens as a turning saw silhouette. The spin direction alternates by impact. Slow round teeth sit between the blades, and some blade tips rebound once. A fast aimed rice trio makes an impact near the pilot a threat. Tones: bright blades, dark teeth.
- **Spark Fountain.** Each impact throws a 150-degree fountain of fast and slow rice sparks that arc back down under gravity, like grinding sparks. It adds a full turn of slow teeth (half rebound once) and four runners grinding both ways along the struck wall. Tones: dark sparks, bright teeth and runners. Floor impacts make a classic fountain, and ceiling impacts become a spark shower.

Further ideas (not implemented):
- *Kickback*: on each impact, rice fans from the previous impact's direction, so the saw's path is echoed across the arena.
- *Tooth Chain*: one long bouncing chain of rice launched along the new heading that follows the saw's own ricochet one wall behind.
- *Sawdust*: tiny slow beads that hang where the saw struck and then fall under gravity.

## Phase three (Hard)

**Current:** Overdrive (faster ricochet; embedded wall teeth keep spitting fans for seconds after each impact).

- **Chain Reaction.** A ricochet 15% faster. Impacts alternate Buzzsaw blades and Spark Fountains, and each impact adds a crown of slow shrapnel teeth that crack into small fast fans as they widen. Every release happens on impact. The current Overdrive's embedded teeth fire on a timer; this card does not.

Further idea: *Gearbox*: impacts lay down stationary Gear Train cogs that widen from the struck point.
