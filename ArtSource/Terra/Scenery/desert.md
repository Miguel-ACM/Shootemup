# Desert scenery: buildings and silos

The existing seeded outpost locations, dune surface, terrain sampling, solid footprints and terrain-shadow calculation remain in place. Paired silos now have narrow facet-following ribs, ladders and a linked pipe manifold. Their plinths and bands no longer have extra conical roofs; the main caps have seed-dependent pitches. Compact outposts have raised roof parapets, doorway awnings and wall vent slats, with seed-dependent awning tones. Details use the existing vertex colors and lighting; they do not add materials, lights or gameplay collision.

The fitted details do not add shadow or clearance solids. The original main solids continue to cast the attached terrain shadows and determine buried-enemy clearance. Counting the emitted faces gives approximately 1,888 building-mesh vertices per section versus 1,688 before this pass (about 1.12x); the much larger dune mesh is unchanged.

## Visual checks still needed

- Review several seeds and all scrolling section joins at gameplay scale. Check that ribs and ladders read cleanly without flicker, the manifold reaches both tanks, and awnings and parapets remain legible on the shortest buildings.
- Check the outposts on steep dunes for grounded contact and believable attached shadows, including that the light plinths do not look like oversized shadow rectangles. Check the small details against the existing clearance around buried enemies.
- Verify the established desert lighting and frame time in Unity 6000.6.2f1. No Unity build or visual review was possible in this run.
