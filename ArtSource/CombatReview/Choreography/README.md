# District choreography review

Forty compositions, eight per district, using twenty district-exclusive routes. Catmull-Rom curves keep headings continuous; arc-length timing keeps enemies moving through bends. Mirrored trains, parallel ranks, staggered arrivals and shared firing beats compose the routes. Subsequent passages change order and entrance side without immediately repeating a composition.

| District | Movement identity | Normal handoff |
| --- | --- | --- |
| Plains | Answering swoops, opening fans, figure eights, descending river streams | 4.2 s |
| Snow | Unwinding spirals, petals, crosswinds, moving siege arcs | 4.4 s |
| Thunder | Diagonal strikes, hooked returns, beam pendulums, paired relay shuttles | 4.0 s |
| Tunnel | Wall banks, rebound chains, descending helices, sliding gates | 4.1 s |
| City | Overtaking lanes, slingshots, interchanges, express cross-traffic | 3.25 s |

The videos and contact sheets show the actual Unity scene, captured at six frames per second. Each video covers eight waves through the real wave director. The first four waves show complete formations without player fire; the last four use the unmodified base gun and a scripted lateral sweep. The test pilot has extra health so the entire sequence can be inspected. This checks composition, flow and projectile behavior; it is not a human difficulty playthrough.

The initial review found overlapping Snow siege hulls and merged bomber volleys. The final Snow capture widens the siege rank and staggers the three bombers by 0.32 seconds, with wider lanes. Each district has its own implementation commit; the Snow refinement is separate.

Results are in `../ChoreographyVerification.txt`, `../SnowChoreographyRefinement.txt`, and `../PlainsFormationTtkVerification.txt`. The separate stock-gun benchmark uses real projectile collisions, no upgrades, and tracking capped at normal player speed. All new Plains enemy/route combinations die before departing; measured first-hit-to-kill times range from 0.40 to 2.00 seconds.

To repeat a combined review while Unity is idle, write `choreography` to `Temp/verify-combat.request` after compilation completes. It runs all forty compositions and then the Plains firing benchmark in one play session, and restores edit mode. `snow-choreography` repeats only Snow plus the composition checks. Run `python Tools/render_choreography_review.py` to encode the generated frames; pass a district name such as `Snow` to replace an existing review video and sheets. The renderer needs Pillow and ffmpeg. It only reads the verification's generated frames under `Temp/ChoreographyReview`.
