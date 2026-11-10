# Void Sovereign: proposed cards

Code: `Assets/Scripts/Enemies/Bosses/VoidSovereignBoss.Proposed.cs`. No shipping file changed.

The book keeps the shipping structure: teleport, prime, card A, rest, teleport, prime, card B, rest. The fifteen-second one-sided interludes and the phase breaks are untouched; only the form cards are new.

- **Ports.** Every round leaves the form's visible eyes: mask eyes, scythe eyes, mandible eyes, star points.
- **The top.** Every card deliberately reaches the top edge.
- **Giant orbs.** Giants are lone accents. They take the end lane of a blade, row or bar with its neighbouring lanes left empty (`CrownSpacer`), or travel down an otherwise empty lane.
- **Rests.** Every card has rest beats and at least one passage that moves.
- **Rounds.** The cards use kunai, crescents, gems, chevrons, needles, ricochets, pearls and giants, plus Void rays.

Tones flip card to card and form to form. In each form, the first card has the dark body and the second the bright body:

| Form | Dark body | Bright body |
| --- | --- | --- |
| One | Weeping Mask | Ringed Horns |
| Two | Praying Mantis | Mantis Strike |
| Three | Mandible Bite | Gnashing Rows |
| Four | Shattered Lattice | Prism Cross |

Each card's giants and aimed replies take the other tone.

Rough pressure on Hard, in rounds per second:

| Card | Proposed | Shipping counterpart |
| --- | --- | --- |
| Weeping Mask | ~76 | Witnesses ~94 |
| Ringed Horns | ~49 | Funeral Psalm ~54 |
| Mantis Strike | ~55 | Severing ~59 |
| Praying Mantis | ~35, plus rays | Cut the Firmament ~35, plus rays |
| Mandible Bite | ~52 | Hunger ~60 |
| Gnashing Rows | ~66 | False Stars ~84 |
| Prism Cross | ~38, plus four rays | World Without Edges ~32 |
| Shattered Lattice | ~17, plus crossfire | Last Equation ~22 |

## Form one: horned mask

**Current:** Witnesses opens butterfly seams up and down. Funeral Psalm lays a pearl pall that hangs, then folds into diagonal aisles.

- **Weeping Mask** (dark kunai, bright gems and tears). Each eye wells a fountain of kunai that rises toward the top edge and arcs down under gravity. Inner lanes cross over the mask; outer lanes fold off the side walls once, so the corners are covered. A slit moves through each fountain and the fountains breathe. Lone slow giant tears fall down the empty middle. Gem pairs answer a pilot who stays still.
- **Ringed Horns** (bright arrows, dark crescents and tips). Each eye draws a horn: one stream sweeps from straight up, over the outside, to straight down and back. Upper lanes are fast and lower lanes slow, so the line curls. Every fifth step a crescent pair rings it. Down-strokes end in a single giant tip with its lane cleared. Up-strokes end in an aimed gem reply.

Further ideas:
- *Mask's Smile*: a slow crescent arc hung beneath the mask that tilts and drops in pieces.
- *Horn Echo*: chevrons that run the ceiling and drop at the horn tips.

## Form two: mantis

**Current:** Severing cuts six-beat ribbons with ricochets. Cut the Firmament holds ±38° rays over pleats that pause and turn.

- **Mantis Strike** (bright kunai and ricochets, dark crescents and giants). The scythe eyes throw curved blades of kunai, bowed forward because each blade's middle flies fastest. A blade halts, swings through 40°, and carries on as the same curve: a hooked cut. The arms alternate and cross the arena. Every third strike, both arms hurl ricochet blades up off the ceiling.
- **Praying Mantis** (dark chevrons, needles and pearls, bright crescents). The forelegs are two rays held almost straight down that open outward 22° and fold back; they never close the central aisle. The wings open upward as fans that reach the ceiling or walls, then run along them into the corners and down the sides. Pearls drip and sway between the forelegs.

Further ideas:
- *Compound Eye*: a hexagonal gem grid that stops in place and blinks out lane by lane.
- *Molting*: a blade that splits into a forward shrapnel cone when it halts.

## Form three: octagonal maw

**Current:** Hunger flares nested parabolic teeth with plasma. False Stars sends comets that split into narrow fans.

- **Mandible Bite** (dark kunai, bright crescents and giants). Each mandible eye throws a sideways jaw of kunai that halts and snaps inward, so the two jaws close across each other below the maw. Alternate bites open upward above the maw and snap down. A lone giant leaves outside the jaw, and its mandible drops the outer tooth for it.
- **Gnashing Rows** (bright gems and pearls, dark crescents and giant end teeth). The mandibles fling teeth to exact places and build three straight rows across the upper arena. Each row hangs, then falls with alternating fast and slow teeth. The rows open as jagged double edges, and each row's gap sits at its own moving seam. Teeth never land on the pilot.

Further ideas:
- *Swallow*: ricochets thrown out that curve back into the maw with `Absorb` (it borrows Space's gravity grammar, so this one is held back).
- *Overbite*: two rows that slide sideways in opposite directions before they fall.

## Form four: crystal star

**Current:** World Without Edges stitches a lattice from opposed fracture pairs. Last Equation sends ricochets with giant tips. Both have charged crossfire.

- **Prism Cross** (bright gems, dark giants and crossfire). The four star points hold rays along their own axes: a cross that turns slowly, so every quadrant drifts and nothing stays still. Each point refracts a gem fan into the quadrant next to it, anticlockwise. The gems halt and bend 20° with the turn. A lone giant sometimes replaces the lower point's fan. The side tips cross charged aims through the pilot, which closes the safe spot above and on the body.
- **Shattered Lattice** (dark kunai, bright giants and crossfire). The side points lay horizontal bars of kunai out to the walls. The top and bottom points lay vertical bars to the ceiling and floor. Each bar hangs as a crystal seam, then shatters: alternate teeth turn 90° each way, and the bar opens into two dotted walls that travel apart. Every bar keeps an open slit. Charged crossfire closes the body's shadow.

Further ideas:
- *Snowblind Facets*: the star's points throw crescents that halt on a ring and leave on the tangent (a moving circle, but in Space's grammar, so held back).
- *Refraction Chain*: one aimed gem that splits into a three-way fan at each pause, twice.

## Concerns

- Weeping Mask kunai keep accelerating under gravity, reaching about 5 to 6 units/s near the floor before they fade. Check that the speed reads well in the recording.
- Gnashing Rows and Shattered Lattice compute their launch speed with `WorldSpeed` so the teeth stop on exact places. The far teeth launch fast (up to about 8 units/s) before braking, but always outward from the body.
