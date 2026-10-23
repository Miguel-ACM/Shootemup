# District choreography review

Each district has eight authored compositions using four continuous routes exclusive to that district. Movement and firing share a phrase clock: staggered arrivals answer the same accents instead of firing independently. Formation offsets use the leader's arc-length progress so groups keep their shape through turns.

- Plains: swoops, opening fans, figure eights and river streams.
- Snow: mirrored falling petals, unwinding coils, crosswind artillery and siege arcs.
- Thunder: lightning diagonals, hooked diamonds, beam pendulums and capacitor crescents, with room between laser strikes and answering escorts.
- Tunnel: layered coils, wall-following groups and coordinated rebounds.
- City: fast traffic streams, overtakes and compact drone volleys.

The first four waves in each video expose complete formations. The following four include moving, unmodified base-gun fire. The pilot has test-only inflated health so the capture can continue through every composition.

`../ChoreographyVerification.txt` records the complete forty-composition run, timing/geometry checks and higher-loop synchronization. `../PlainsFormationTtkVerification.txt` measures real kills with the stock gun at normal player tracking speed; leaving the screen never counts as a kill. A subsequent Snow-only refinement is recorded separately in `../SnowChoreographyRefinement.txt` when present.

To regenerate after a successful review run: `python Tools/render_choreography_review.py Fields Snow Thunder Tunnel City`. Existing clips are skipped unless their district is named. Completed media replace earlier review files atomically so an open viewer cannot leave a half-written result.
