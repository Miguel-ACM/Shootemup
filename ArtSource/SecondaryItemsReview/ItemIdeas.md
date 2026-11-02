# Secondary fire and stat items — 2 November 2026

Implemented items use the ordinary hangar unlock and run reward pools. The original
items, AK45, equipped guns and active slots keep their identities.

## Added

| Item | Rarity | Effect at base fire rate |
| --- | --- | --- |
| Paperclip Box | Common | 1.65 piercing needles/sec; alternating wide aim, tightened by focus |
| Firefly Jar | Rare | 1.2 small homing sparks/sec |
| Salt Shaker | Common | 2.2 three-grain rear fans/sec; focus narrows the fan |
| Pocket Mortar | Rare | 0.65 shells/sec; forward shrapnel burst |
| Wind-up Teeth | Common | 3.5 alternating side shots/sec; focus brings them forward |
| Ribbon Spool | Rare | 5 weaving shots/sec; focus draws a narrow seam |
| Sewing Kit | Common | 7 weak piercing needles/sec; nearly parallel in focus |
| Steel Washers | Common | +0.25 base shot damage |
| Clockwork Spring | Common | +0.8 base firing rate (8% of the initial rate) |
| Roller Bearings | Common | +0.48 movement speed (6% of the initial speed) |
| Thermos | Common | +20 maximum energy; fills the extra capacity |
| Heat Sink | Common | ×0.9 active energy cost per copy |
| Lucky Penny | Common | +0.05 luck |

Secondary shots follow either held fire button, use no energy or weapon slot, and
pause for decisions. Their cadence scales with firing rate, capped at three times
base cadence. Every copy has its own removable battery. Copies share one small
visual option per item type, and leave fifty friendly projectile slots in reserve.
Secondary hits carry coatings but do not recursively generate hit procs. These
shots reuse approved projectile artwork and the existing projectile pool.

## Further proposals — not implemented

Two modest increases can support a build without replacing the bigger original
items. Tradeoffs should remove a little of another stat, never a hit segment.

| Combined item | Proposed changes | Model idea |
| --- | --- | --- |
| Pilot's Lunchbox | +0.15 damage, +0.4 firing rate | Bright lunchbox with a clipped flask |
| Silk Bearings | +0.32 movement, +1.6 bullet speed | Bearing wrapped in a trailing silk ribbon |
| Copper Winding | +10 max energy, 5% lower active cost | Copper coil on a ceramic bobbin |
| Four-leaf Locket | +0.025 luck, +0.2 pickup range | Hinged clover charm with a gold clasp |
| Balanced Flywheel | +0.4 firing rate, +0.15 damage | Spoked brass wheel with steel rim |

| Tradeoff item | Proposed changes | Decision it creates |
| --- | --- | --- |
| Heavy Flywheel | +0.45 damage, −0.5 firing rate | Strong individual hits for slower weapons |
| Hot Coil | +1.5 firing rate, −10 max energy | Frequent gunfire versus active capacity |
| Dense Core | +0.35 damage, −0.24 movement | More power with a small handling cost |
| Featherweight Frame | +0.8 movement, −0.15 damage | Mobility for precision dodging |
| Wide-bore Nozzle | +0.3 shot size, −1.6 bullet speed | More coverage, less reach against fast craft |
| Bent Penny | +0.08 luck, −0.4 firing rate | Better future rewards for a modest present cost |
| Racing Spring | +1.2 firing rate, −0.1 shot size | Narrower rapid fire; rewards accurate alignment |

Use small absolute changes for additive stats so later pacts and item trades can
reverse the correct contribution. Floors on damage, firing rate, size, movement
and energy would prevent bad combinations from disabling the ship. Recheck actual
hits and total damage for those tradeoffs before admitting them to the run pool.
