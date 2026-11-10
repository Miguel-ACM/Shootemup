# City duo: proposed cards

Code: `Assets/Scripts/Enemies/Bosses/CityDuoBoss.Proposed.cs`. The roles stay the same. In phase one the interceptor still circles the hauler, and both ships fly each card together for the same length. The enraged survivor flies its own pair of cards, and Hard adds a final spell. Both ships still cover behind themselves: arrows rebound off the ceiling, and crates hang on the top edge. Neither fires a full circle; crates burst into downward cones. Pressure matches the current cards to within about 10%, measured in rounds per second on Hard: Green Wave about 71 (current 70), Zebra Crossing about 78 (83), Container Drop about 61 (62), Jackknife about 39 (39), Hairpin Bends about 55 (57), Skid Marks about 40 (39), Total Gridlock about 73 (75) and Spaghetti Junction about 89 (86). Parked rows also stand as obstacles.

## Phase one (both ships)

**Current:** Ring Road (the interceptor's outward spiral arms, and the hauler's side fans that rebound off the walls), then Rush Hour (the hauler's two-family street grid and the interceptor's headlights).

- **Green Wave.** One burst from the hauler's nose brakes into six level rows of parked cars. Each row has a two-car crossing marked by two gem lamps in the other tone, and the crossings snake from row to row. The lights turn green from the bottom row up. Alternate rows skid off down-left and down-right at one street speed and rebound once off the side walls. The crossings slide apart, and rows passing through the rebounded cars weave together near the walls. Meanwhile the interceptor covers the roof: mirrored arrow fans thrown up behind it rebound off the ceiling and rain across the top. Every other volley it also fires a chevron headlight train. Tones: dark street with bright lamps, and bright roof cover.
- **Zebra Crossing.** Every round in a stripe shares one descent speed, so the hauler's stripes stay level while they widen. They alternate between dark arrows and bright circles, like a zebra crossing. A two-round crossing snakes from stripe to stripe. The interceptor fires three headlight trains (dark chevrons, bright beads) that brake together, glint and skid 40 degrees sideways. The skid direction alternates.

## Phase two (enraged survivor)

**Current:** the hauler's Cargo Storm (360-degree crates, a rear guard and headlights) and Headlights. The interceptor's Junction (a four-arm spiral) and Headlights.

- **Hauler: Container Drop.** Ten crates are hoisted from both wings and brake onto mirrored stations along the visible top edge. They hang there, then drop in a ripple from the outside in. Each crate bursts into a nine-round downward cone. Dark high beams (a kunai followed by slowing cars) are aimed from both wings while the crates hang. The tone is bright.
- **Hauler: Jackknife.** From each wing, a bright gem cab runs outward and down ahead of a dark trailer on the same line. The trailer brakes and folds 95 degrees toward the middle, so the mirrored trailers slide through each other as a moving X. The truck's lean changes from volley to volley, and a kunai horn sounds every third truck. The tone is dark.
- **Interceptor: Hairpin Bends.** Two nested fans (dark arrows outside, bright circles inside) brake into arcs. Every round then turns back on a hairpin, crossing its mirror twin on the way up, and rebounds off the ceiling. Every other fan is centred on the pilot. The tone is dark.
- **Interceptor: Skid Marks.** Both wings lay stalled beads along the interceptor's weaving path. After a wait, each bead launches at the pilot in the order it was laid: arrows fast, circles slow. A kunai fan also freezes and snaps onto the pilot from time to time. The tone is bright.

## Hard final

- **Total Gridlock (hauler).** A parked street with crates hanging above it. The street skids off as the crates drop through it.
- **Spaghetti Junction (interceptor).** Hairpin fans whose centre swings further each volley, laid over the skid-mark trail.

## Further ideas (not implemented)
- *Traffic Lights*: rows that stop and go twice, first sideways, then down, with a tone change on each aspect.
- *Tow Truck*: the interceptor tows a short chain of rounds behind its orbit that it releases on the turn.
- *Bus Lane*: one wide, empty lane that the hauler keeps open and moves, while everything else fills.
