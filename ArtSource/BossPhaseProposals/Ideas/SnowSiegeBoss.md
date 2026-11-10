# Snow fortress: proposed phases

Code: `Assets/Scripts/Enemies/Bosses/SnowSiegeBoss.Proposed.cs`. Each phase is one concept that runs for the whole phase and develops in movements, with rests that belong to it. The fortress keeps its mouth, side ports and the comb on the top edge. The sides throw only arrows, and the comb rains arrows straight down with no stop-and-go. Shell snow is thrown up harmlessly and falls from the top. Rounds per second are estimated within about 10% of the current phase (Hard and Normal).

## Phase one (rain): Sleet Lattice

**Current:** Frozen Petals (mirrored fans freeze and re-aim) alternating with Icicle Comb (a woven vertical lattice of rain with loosely aimed mouth rounds).

The comb rains two crossing families of slanted arrows: even columns lean one way and odd columns the other. The rain falls as a diamond (argyle) lattice. About every ten seconds the comb gathers (an aperture charge) and the lattice changes:
1. Still air.
2. A swinging crosswind tilts both families, so the diamond holes drift sideways.
3. The lattice breathes: its lean widens and narrows, so the diamonds stretch and squash.
4. Gusts: the families swap every few beats (the holes jump half a diamond), the side ports carry the lattice on beneath the hull, and the comb slides half a column.

From the second round the side ports stay in the lattice. Loosely aimed mouth rounds fall between beats while the sides are quiet. Tones: dark rain, bright mouth rounds. Lighter difficulties drop a diagonal of strings, never a whole column.

Alternative single concepts:
- *Dendrite Bloom*: the side ports grow mirrored snow-crystal arms whose barbs freeze and branch at sixty degrees. Over the phase the fronds sweep lower, branch twice, then the comb grows dendrites from the top.
- *Icicle Drip*: the comb drips in a travelling sine that slows, reverses and doubles, so the rain falls as a rippling, then braided, curtain.
- *Hoarfrost Window*: the comb rains around a sliding rectangular pane whose frame freezes and re-aims. The pane narrows and speeds up over the phase.

## Phase two (snow): Avalanche

**Current:** Whiteout (sideways gusts that halt and turn, with petals and shells) alternating with Snowdrift (wide shell fans whose snow falls from the top, with stop-and-go orb arcs).

Each avalanche starts with a mouth charge. A fan of shells lands along a line, and the first to burst lands near and high, so the snow sweeps across the arena in order. Beneath it the gust ports pour layered arrows downhill with the slide, the slope rippling, each arrow starting slow and accelerating. The avalanches run:
1. From the left.
2. From the right.
3. Closing in from both sides (the slides converge).
4. Bursting out from the centre (the slides diverge), followed by a longer settling rest.

From the second round, the right-hand and centre avalanches end with a mouth fan that freezes and re-aims. Tones: bright shells and snow, dark slides. It carries fewer snowflakes than Snowdrift on Hard (better for the warmed flake pool) and makes up the pressure with arrows.

Alternative single concepts:
- *Whiteout*: gusts thrown sideways halt and turn downwind. The wind direction swings, then splits, then spirals as the storm builds.
- *Snow Braid*: harmless spray up, then flakes fall in mirrored braided columns that drift across the width and multiply over the phase.
- *Cornice*: shells park along the top edge and burst from the outside in, then the inside out, then in a travelling wave.

## Phase three (Hard): Diamond Dust

**Current:** Absolute Zero (the woven comb alternates with fast/slow pairs while petals freeze and re-aim).

The comb becomes a harp. Each strung column drops a fast arrow and a slower echo, so a strum falls as two separating diagonal slashes, with two muted strings for a gap that walks across the comb. The movements are:
1. Arpeggio: single strums alternating direction, with re-aiming mouth fans.
2. Crossing: a dark and a bright strum from both ends at once fall as Xs, with side dendrites between them.
3. Glissando: quick back-and-forth strums alternating tone.

The harp is retuned (a comb charge) between movements. From the second round the strums are plucked tighter (flatter, faster slashes) and the comb shifts half a column each time.

Alternative single concepts:
- *Polar Night*: Sleet Lattice under a wind that also tilts falling snow, the wind strengthening until the lattice and snow cross.
- *Absolute Zero (stillness)*: every comb volley freezes mid-screen as a sheet, then the sheets release in order. This needs care with Miguel's "no stop-and-go comb" rule.
- *Rime Cathedral*: the dendrites grow from the comb downward and from the sides upward, meeting in arches.
