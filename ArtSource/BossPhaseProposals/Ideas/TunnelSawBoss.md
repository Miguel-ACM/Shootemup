# Tunnel saw: proposed phases

Code: `Assets/Scripts/Enemies/Bosses/TunnelSawBoss.Proposed.cs`. Each phase is one concept that runs for the whole phase. Every round leaves the centre of the blade. Phase one fires widening rings from stations, with no bouncing. From phase two the saw fires only on impact: never on a timer and never when hit. There are no minions. Ring rotation is at most 26 degrees a second, with the 2.8 units/s rim cap. Rounds per second (or rounds per impact) are estimated within about 10% of the current phase.

Phase two needs the proposed burst on each impact, and the shipping `ImpactBurst` cannot be changed. The proposed book therefore drives the ricochet itself (`Patrol`/`Glide`): same speed formula, walls, impact warning and contact damage. The shipping patrol stands down through `haltUntil`. A guarded `OnPhaseEntered` override keeps it still through phase breaks while the review switch is on. Nothing changes when the switch is off.

## Phase one (stations): Gearbox

**Current:** Frogger Crossing (six widening rings turning in alternate directions, each with a sliding four-bead gap).

At each new station the saw throws widening cogs: rings with six raised teeth and one opening. The gearbox shifts station by station:
1. A drive gear: single cogs turning in alternate directions.
2. A meshing pair: a bright cog with teeth out and a slower dark cog with teeth in, counter-rotating.
3. A ratchet: still cogs, fast and slow alternately, whose opening steps round from the pilot like a spiral stair.
4. An idler pair: a fast and a slow cog turning together, so their openings stay aligned but arrive at different times.

From the second round the teeth stand taller, the turning direction flips and the rice tracer becomes a trio. The count per station matches Frogger.

Alternative single concepts:
- *Escapement*: rings that tick (turn one bead at a time) rather than turning smoothly. The tick quickens and reverses over the phase.
- *Spiral Stair*: fast-and-slow ring pairs sharing one opening that climbs round the saw. The step widens, reverses and doubles to two openings.
- *Clutch*: two rings with identical gaps, one widening faster, so the gap closes and reopens. The slip grows over the phase.

## Phase two (ricochet, impact-only): Buzzsaw Bloom

**Current:** Ricochet Storm (each impact: a 360-degree burst of fast arrows and slow teeth, some bouncing, an aimed shotgun and wall runners).

Every impact opens a full turn of hooked blades: four rice grains per blade launched together at rising speeds, each lagging further behind the spin. Slow round teeth sit between the blades, and two sparks grind away along the struck wall. Impacts cycle through:
1. A ripsaw: every blade hooked with the spin.
2. A crosscut: alternate blades hooked against the spin.
3. A rebound: near-straight spokes whose tips ricochet.

The spin direction flips every three impacts, and the hook deepens from 5 to 12 degrees as the saw wears through the phase. A fast trio flies at the pilot from every impact. Tones: bright blades, dark teeth and sparks.

Alternative single concepts:
- *Spark Fountain*: each impact throws a fountain of rice sparks that arcs back down under gravity. The fountain widens, then splits, then gains a ring of rebounding teeth.
- *Kickback*: each impact echoes the previous impact's direction across the arena, so the saw's path draws itself in rice.
- *Tooth Chain*: a long bouncing chain launched along the new heading follows the saw's ricochet one wall behind, and the chain lengthens with each impact.

## Phase three (Hard): Chain Reaction

**Current:** Overdrive (faster ricochet; embedded wall teeth keep spitting fans on a timer after each impact).

A faster ricochet. Every impact throws two crowns of shrapnel teeth that crack as they widen. The large dark crown cracks forward into a cone, into wide petals, or after a rebound off the walls. The small bright crown cracks into sideways pairs, or into pairs aimed at the pilot. The cracks come sooner (1.7 down to 1.1 seconds) as the saw wears down, so the shrapnel bursts ever closer to the saw. Every release happens on impact; the current Overdrive's timer-fed embedded teeth are gone.

Alternative single concepts:
- *Gearbox Overdrive*: each impact lays a stationary widening cog at the struck point, and the cogs mesh as impacts accumulate.
- *Sawdust*: impacts leave slow beads hanging along the walls that fall under gravity when the next impact shakes them.
- *Double Bloom*: Buzzsaw blades whose tips split into smaller blades, adding a generation each third of the phase.
