# Flowing caldera lava

Replaces the smooth sine-band orange floor with a textured molten surface under the existing raised basalt shelves. Torn cooling rafts have dark textured centers, orange margins and narrow hotter seams. A curl-velocity texture bends the surface into eddies while the landscape scrolls downstream.

The implementation uses the same dual-phase advection principle as the local `PSXValleyRiver` material. Each phase resets while its contribution is hidden by the other, preventing a visible reset or accumulated stretching. Surface colors are calculated before blending so thin seams survive the transition. The two repeating maps are original deterministic Python-generated data, imported in linear space without texture compression; no third-party texture assets or shader code are bundled.

References consulted:

- [Catlike Coding: Texture Distortion](https://catlikecoding.com/unity/tutorials/flow/texture-distortion/) — flow vectors, staggered phases and smooth distortion reset.
- [Daniel Ilett: Voronoi-based Lava](https://danielilett.com/2023-06-20-tut7-2-voronoi-lava/) — separating cooling crust from emissive seams. Our edge distances are baked offline rather than evaluated in per-pixel neighbor loops.

Run `build_lava_maps.py` with Python, NumPy and Pillow to regenerate `Assets/Resources/CalderaCrust.png` and `CalderaFlow.png`. The shader uses three texture samples in the existing single background pass. No runtime baking, extra lights, new animated objects or postprocessing passes are added.

`ReviewLavaFlow.Run` renders 96 native Unity frames over eight seconds in a preview scene, without entering play mode. It includes the terrain scroll, a complete seven-second advection cycle and the normal PSX compositor at 270×480 combat resolution. `LavaFlow.mp4` and `LavaFlow.png` are the resulting animation and still; `Verification.txt` records import and shader checks. This is a material review, not a gameplay performance benchmark.
