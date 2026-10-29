# Underwater scenery

The reef sections now use seeded, faceted rock clusters and four distinct coral forms: forked branches, ribbed fans, grooved brain mounds and hollow-rimmed tube clusters. Bent kelp stalks carry alternating folded leaf blades. Each side also has a seeded wreck with a keel, raised articulated ribs and a broken hull plate. All use the existing vertex-colour scenery material; sections and scrolling are unchanged. Reef placements stay away from the central lane and section ends.

The previous underwater builder used approximately 6,096 vertices per section. A conservative maximum for the replacement is approximately 6,248 (all coral colonies as the most expensive tube clusters), before any renderer-specific overhead. No objects, materials, lights or per-frame work were added.

Visual review in Unity 6000.6.2f1 is still needed: inspect several seeds at gameplay scale for coral species and colour readability, depth ordering of tube mouths and brain ridges, kelp/rock intersections, wreck silhouettes, clear central lanes and the four scrolling section joins. Check scenery density and frame time during combat. Compile and check the Console for warnings; Unity was not available for this pass.

