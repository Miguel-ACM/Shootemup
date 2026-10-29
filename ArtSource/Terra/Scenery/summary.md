# Scenery model pass

This is a partial pass. Unity compilation, section vertex measurements and visual review have not been run. Check multiple seeds and scrolling seams in the isolated review checkout before accepting the changes.

- **Plains:** Crown cards have seeded width variation without adding vertices or moving their centers. Check that trees retain full layered silhouettes and do not obscure the road or river. Trunks, banks and road still need a modeling pass.
- **Snow:** Snow-capped rocks have seeded proportions and off-center peaks, with their feet sampled against the existing terrain. Fallen logs vary in direction and sample terrain at the far end. Check snow caps, log contact and grove clearances across seeds and seams. Trees and other scenery still need a modeling pass.
- **City:** Garden path intersections have a small raised paved center. Check it reads as a plaza detail at gameplay scale. Architecture and plazas still need a substantial modeling pass.
- **Desert:** Silo caps have a subtly offset apex, without changing the registered solid or its footprint. Check the cap lighting and attached shadows; dunes and buildings still need a modeling pass.
- **Underwater:** Coral branches now have two shaded facets and wreck ribs bend through two faceted segments. Kelp and the seabed are unchanged. Check silhouettes and color at gameplay scale, and check rib joins and section edges. Source-level counting estimates approximately 1.43 times the prior section vertices; measure in Unity.
- **Hell:** The sparse fossil ribs use the same bent, faceted geometry as the underwater ribs. Check their restrained color and connection to the caldera. Basalt shelves and craters still need a modeling pass.
- **Candyland:** No changes. Pink clouds need review; falling sweets are outside this pass.
- **Space:** No changes. Stars and planets need review.
- **Thunder and Tunnel:** No changes in this pass; review their existing scenery separately.
