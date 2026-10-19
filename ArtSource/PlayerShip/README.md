# GR-07 industrial interceptor

Original player ship authored in Blender 5.2.2 for Shootemup.

- **236 triangles**, 164 source vertices, flat face normals.
- One material and one **256 × 256** PNG atlas.
- Every face has planar atlas UVs; tile reuse is intentional. Four-pixel tile gutters keep neighbouring tiles out of the sampled surface. No collapsed UV triangles.
- Grey armour, recessed exhausts, twin cannons, swept wings, stabilizers, ventilation grilles and restrained warning markings.

`GR07_Interceptor.blend` is the editable source, including its inspection camera and lights. The texture is packed into the file. `build_ship.py` reproduces the geometry, atlas, UVs and FBX in a dedicated scene without clearing other Blender scenes. `GR07_UVLayout.svg` shows the mapped triangle islands over the texture atlas. `GR07_AssetReport.json` records the Blender validation results.

The game-ready files are in `Assets/Art/PlayerShip`: FBX, PNG, material and `GR07_PlayerVisual.prefab`. FBX uses Y-up coordinates. The visual applies a 180° local yaw and a -75° X tilt so the imported nose points up the existing XY gameplay plane, with subtle banking while moving sideways. All player movement, weapons and 2D collision components remain on the existing Ship object in `L1`.

The custom URP 2D shader supplies stepped directional shading, five-bit colour quantization and optional vertex snapping. Texture filtering is Point, mipmaps are disabled and compression is off. `PSXShipVisual` mirrors the existing player's damage/death tint onto the 3D mesh.

Use **Tools → PSX Player → Install GR-07 Ship** in Unity to repeat the import/attachment. It changes only the L1 player's visual and reuses the same material/prefab. The pre-install L1 scene is preserved in `MigrationBackup/BeforeGR07`.
