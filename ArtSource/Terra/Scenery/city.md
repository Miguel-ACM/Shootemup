# City scenery

The seeded neighbourhood grid retains its streets, palettes and block types. Commercial blocks now have stepped upper massing and two-sided window bands; existing landmark setbacks, courtyard, rotunda, residential and industrial forms remain. Flat roofs gain parapets and a seeded mix of HVAC, water tanks, antennas and planted beds. Civic courtyards have paths and raised planting. Gardens and plazas use distinct paved or green layouts, corner trees, seating and a small fountain.

Details share the existing vertex-colour mesh and are built with the section, without new materials, lights, colliders or per-frame work. Window bands use single facade planes instead of shallow boxes. Placement remains within the existing lots and the section grid; the run seed still controls the layout.

## Visual and performance review in Unity

- Inspect multiple seeds and both block orientations at gameplay scale: roof equipment should sit on its roof and parapets and windows should read without flicker, protrusion or gaps at setbacks.
- Check gardens, fountain, courtyard planting and rooftop beds for readable silhouettes and intersections, and check the perceived density of the coloured neighbourhoods.
- Watch several section boundaries while scrolling for seams and verify that buildings and their projected tops do not obscure the streets or combat view.
- Compare mesh vertex counts and section rebuild time against the previous city for representative seeds; the approximately 1.5x budget has not been measured here.
- Compile with Unity 6000.6.2f1 and visually review the actual scenery. Unity was not available for this pass.
