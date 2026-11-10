# Seraph Judicator (Heaven): proposed cards

Code: `Assets/Scripts/Enemies/Bosses/SeraphJudicatorBoss.Proposed.cs`. The only shipping edit is the `partial` keyword on the class.

The book keeps the shipping rhythm: pray, charge the core eye, card A, rest, pray, charge, card B, rest. The angel's anatomy draws every card:

- **Wheels.** `WheelLash` fires from every eye on a wheel's lower rim, each along its own line out from the core. As the three wheels tumble, their projected outlines change, so the fans come out different each beat.
- **Lances.** Lances leave an eye or the core eye. The Six Wings and Feather Fall side lances ride their eye around the turning wheel, as the shipping lances do. The thin aimed Many Eyes lances stay pinned where the eye opened: riding, a thin beam would slide sideways onto a pilot who had already stepped clear of it.
- **Structured pressure.** Mirrored rim fans from the side eyes (`RimFans`) each keep one drifting open lane. Aimed chords from the outer side eyes (`Stare`) punish a pilot who stays still. Every card's beat is shorter than in the first draft.
- **Empty air.** The six choir glows are reused as unbodied eyes at authored sites (`Apparition`).

Rounds are feathers, stars, pearls (`SimpleRound`) and the scored needles. Upward fire is sparse and never bounces off the ceiling.

Tones follow the existing `Body`/`Accent` split. The bright card body is listed first in each pair:

| Phase | Bright body | Dark body |
| --- | --- | --- |
| One | Wheel Within a Wheel | Six Wings |
| Two | Many Eyes | Feather Fall |
| Three | Wheels of Fire | Sea of Glass |

Lances stay bright.

Rough pressure on Hard, in rounds per second over each card. Both columns include the unbodied fire (the shipping choir; the proposals' watchers, Holy eyes, plumes and glass sheets):

| Card | Proposed | Shipping counterpart | Difference |
| --- | --- | --- | --- |
| Wheel Within a Wheel | ~290 | Dawn Liturgy ~330 | -12% |
| Six Wings | ~200 | Cathedral of Eyes ~225 | -9% |
| Feather Fall | ~390 | Sevenfold Procession ~455 | -15% |
| Many Eyes | ~250 | Judgment Pendulum ~280 | -12% |
| Wheels of Fire | ~475 | Last Trumpet ~530 | -10% |
| Sea of Glass | ~360 | Covenant ~425 | -15% |

The first draft compared against the shipping banks without the choir and left phases two and three about 35-60% lighter. The cards were then raised with mirrored wheel fans, aimed stares and shorter rests, not with denser single fans. Every solid fan still has a moving aisle or open lane. Run `MeasureDifficultyCurve` before choosing.

## Phase one

**Current:** Dawn Liturgy: a golden nave between two ±14° lances, with up and down feather vaults. Cathedral of Eyes: mirrored arches under a slow central lance. Both have the unbodied choir.

- **Wheel Within a Wheel** (bright feathers, dark pearls). The middle wheel's lower eyes cast feather lashes along the wheel's own outline, so each fan bends into a crescent: eyes deeper below the core throw faster. Each fan opens or narrows as the wheel tumbles. One aisle sways through the lashes. On the off beats the outer side eyes open mirrored rim fans into the side lanes. The inner wheel answers with slow two-speed pearls. The core eye lays a wide lance along the pilot's line twice per card. Watchers open in the upper and middle corners and fan feathers across the far floor, so the corners are not safe.
- **Six Wings** (Isaiah's seraph; dark feathers, bright pearls). The upper wings are two lances, riding the outer side eyes, held out to the side walls; they beat 36° down and back. The middle wings are feathers thrown sideways, tips fastest: they halt in the shape of a wing, then shed downward as side curtains. The lower wings are short mixed fans under the body. "Holy, holy, holy": three pairs of eyes along the top edge drop small chords that halt, stare and are thrown at the pilot. The side eyes stare back with aimed chords on alternate beats.

Further ideas:
- *Eyelid Blink*: every wheel eye closes (stops firing) in a wave around the rim, and the blink is the moving gap.
- *Ladder of Jacob*: rungs of stars climb and descend between two rail lances.

## Phase two

**Current:** Sevenfold Procession: a wide lace from ±22° lances. Judgment Pendulum: one sweeping lance with pearled arch ribs.

- **Feather Fall** (dark feathers, bright stars). Two gate lances ride the outer side eyes and cross beneath the angel in an X (the cherubim and the turning sword). They breathe only outward, so the crossing rises and never sinks onto the floor. Riding is fair here: the wheel's tumble draws the eyes inward, which raises the crossing and widens the lower aisle, and the side regions stay open up the walls. Along the whole top edge, six plumes shed sparse feathers that sway at different rates. Their crossing arcs leave diamond gaps, and each verse shifts by half a plume. Below, the middle wheel lashes on every beat around its own aisle. The gate eyes open mirrored fans into the side aisles outside the X, and stare at a resting pilot. The inner wheel throws star chords that halt and dart at the pilot.
- **Many Eyes** (bright feathers, dark pearls). Pairs of eyes on the outer wheel's lower rim open in turn. Each pair lays thin lances that cross on the pilot's position, so the pilot has to keep moving. Each lancing eye opens two layered feather lids along its line of sight but never on it: the line itself is the lance. The inner wheel weeps pearls, and the upper rim sends a few feathers toward the top edge. The thin lances stay pinned where each eye opened (see the lance note at the top).

Further ideas:
- *Ophanim Lattice*: two wheels fire counter-turning lashes that halt and rotate ±25°, weaving a moving diamond mesh.
- *Censer*: a swinging pearl plume hung from the lowest eye, scattering stars at the end of each swing.

## Phase three (Hard)

**Current:** Last Trumpet: a central column and ±47° lancets over dense vaults. Covenant: two closing scythes over ribbed windows.

- **Wheels of Fire** (bright feathers and stars, dark pearls). Two wheels lash at a time, in turn: outer feathers, middle feathers with star beads, inner pearls. The three outlines interleave, and one aisle moves through them all. Two lances through the core eye turn together like a slow propeller (16°/s). The lower arm sweeps the lower arena and the upper arm crosses the top, so neither the floor nor the ceiling is a resting place. The middle side eyes add mirrored rim fans, the outer side eyes stare, and corner watchers return.
- **Sea of Glass** (dark feathers, bright pearls). Six eyes draw three glass sheets at once: at the top edge, below the angel and mid-arena. Every feather flies to its exact place and the sheet hangs. Then it falls as a woven lattice of ±18° diagonals, with one open seam per sheet. The rows trade heights each verse. The outer wheel keeps lashing around an aisle. The middle side eyes throw refracted rim fans that halt and turn straight down. The core eye glances thin lances at the pilot, and the side eyes stare.

Further ideas:
- *Throne of Sapphire*: four pinned lances form a square around the pilot's position after a long charge, and feathers fall inside it.
- *Last Seal*: the three wheels fire one at a time in a counted seven-beat procession, finishing in a full-width plume sheet.

## Concerns

- Wheel lashes depend on the wheels' 3D tumble. When a wheel turns edge-on, its lower rim holds fewer eyes and that beat is lighter. This is intended (the fan breathes) but should be watched in the recording.
- The Six Wings lances ride the outer eyes. A wing beam on the rising eye can lift toward the ceiling, which shrinks, but does not close, the pocket above it in that top corner. The 1.6 s charge shows the line first.
- At about 360-475 rounds/s on Hard, phases two and three run well inside the 3,600 live-round cap, but they are as busy as the shipping cards. Check readability in the recording.
