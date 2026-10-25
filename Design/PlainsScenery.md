# Plains scenery

The Plains landscape uses a single run-seeded plan for the river, bridge, lookout and vegetation. Four 24-unit sections join into a 96-unit loop. Geometry, river UVs and procedural water noise repeat at that boundary. Groves have independent centres, sizes, species and spacing; shoreline reeds use a separate sparse distribution. Roads and landmarks reserve empty space before plants are accepted.

Foliage uses original, procedurally drawn 64-pixel leaf cards in a point-filtered 128-pixel atlas. Several tilted cards form each crown above a branched trunk. Alpha clipping writes depth, avoiding the sorting problems of transparent leaf layers. Broadleaf and hanging crowns have different proportions and tones. Small wind offsets stay subordinate to combat movement. The texture and two runtime materials are owned and released by `PSXGrassField`; all foliage is batched per section.

The bridge has battered stone abutments, wing walls, cutwater piers and open eight-bay steel side trusses. Its road connects to the service road and east apron. Deck markings and wear remain subdued.

The river has its own opaque shader and channel-space UVs. Cross-channel position controls shallow colour and flow speed; the longitudinal coordinate follows each bend. Broken ripples, stretched current highlights, intermittent shoreline foam and short pier wakes move downstream. There are no ocean waves, static reflection rectangles, scene-colour grabs or transparent sorting dependencies. The normal game compositor supplies the PSX colour treatment. Both water and foliage animation use scaled game time, so they freeze when the game is paused.

## References researched

- [David Rosen: Rendering Plants with Smooth Edges](https://www.gamedeveloper.com/programming/rendering-plants-with-smooth-edges): intersecting alpha-mapped leaf planes and their depth/sorting implications.
- [Elbolilloduro: Trees psx](https://elbolilloduro.itch.io/trees-psx): low-poly, low-resolution tree style reference. No assets or textures from this pack were imported.
- [John Sietsma: Flow maps](https://johnsietsma.com/posts/flow-maps/): directional advection and keeping flow coherent. Here the river ribbon's coordinates supply the direction directly, rather than requiring a flow-map texture.

## Review

Run `VerifyPlainsScenery.Run` in a disposable project copy with graphics enabled. It checks three seeds for placement clearances, finite meshes, vertex budgets, section edges and flow UV continuity, then renders bridge, river and wrap views through the game compositor. A separate runtime pass captures scrolling scenery with the actual HUD and combat, exercises lateral parallax and crosses the full loop. Raw frames go to `ArtSource/PlainsSceneryReview/Frames` in the disposable copy; selected evidence belongs in `ArtSource/PlainsSceneryReview`.

The landscape intentionally repeats after 96 units; new runs receive different layouts. Gameplay rules, enemy formations and equipment are unchanged by these scenery components.
