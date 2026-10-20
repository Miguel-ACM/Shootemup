# PSXv2 fleet (Blender)

Replacement player/enemy models and FX built in Blender 5.2 through the Blender MCP addon.
Installed with **Tools > PSX Player > Install PSXv2 Blender fleet** (`Assets/Editor/InstallPSXv2.cs`; it also runs when `ArtSource/PSXv2/install.request` exists, and `capture.request` replays the showcase capture). It sets the importers (Point, legacy looping clips), builds materials, swaps the player visual in L1 and PSX_Showcase, and swaps the visuals in the enemy1/waver/Biggie/stage1boss prefabs. Results go to `InstallReport.json` and `UnityCapture/`.
The old GR-07 player visual is kept in both scenes but disabled. Running the older **Install industrial fleet and UI** again would add its old enemy meshes next to these and turn the PSX filter back off, so do not use it any more.

## Files
- `Assets/Art/PSXv2/*.fbx`: player + 4 enemies, triangulated and flat-shaded, with UVs into one atlas and vertex-colour occlusion ("Col").
- `Assets/Art/PSXv2/PSXv2_Atlas.png`: 128 x 128, 8 x 8 tiles of 16 px, quantised to 15-bit colour. Import as **Point, no mipmaps, no compression**.
  Row 0 is steel/hazard/vents, row 1 faction paint, and row 2 glows (fullbright in the proposed shader).
- `Assets/Art/PSXv2/FX/*.png`: horizontal flipbooks for the Unity Texture Sheet Animation module (frames x px): Explosion 8x32, ExplosionBig 8x64, Spark 4x16, MuzzleFlash 3x16, HitboxPulse 4x32 (loop) and Smoke 6x16. Alpha is 1-bit with ordered-dither fades.
- `ArtSource/PSXv2/*.obj` has OBJ copies. `PSXv2_Catalog.png`, `PSXv2_Playfield.png` and `PSXv2_FX.gif` are the PSX previews.
- `Assets/Shaders/PSXv2Lit.shader` (models), `PSXv2Hitbox.shader` (unlit, always on top) and `PSXv2Flipbook.shader` (point-sampled flipbook particles with dithered cut-out alpha). Materials: `Assets/Art/PSXv2/PSXv2 Steel.mat`, `PSXv2 Hitbox Core/Ring.mat` and `Assets/Resources/Effects/PSXv2Spark/Explosion/ExplosionBig.mat`.

## Models
| Model | Role | Silhouette | Tris |
|---|---|---|---|
| PSXv2_Player | player | white Ikaruga-style interceptor with forward-swept blue wings, wingtip cannons, nose guns at the bullet spawn points, twin cyan burners | 389 |
| PSXv2_Enemy_0_Dart | enemy1 | red arrowhead with a red eye at the tip | 72 |
| PSXv2_Enemy_1_Rotor | waver | orange hex ring with a hole, glowing hub, 3 lamp pods | 130 |
| PSXv2_Enemy_2_Gunboat | Biggie | teal twin-hull catamaran with a turret | 292 |
| PSXv2_Enemy_3_Warden | stage1boss | purple carrier following the boss collider outline, two drum turrets with long guns, white spine and reactor | 791 |

Each enemy has its own colour and shape, so the types can be told apart at a glance in a bullet storm.

## Enemy animation
Each enemy is a root empty plus rigid parts pivoted at their hinges. Loops are keyed on twos (15 Hz at 30 fps) with constant interpolation, which gives stepped PS1-style motion. They are baked into one FBX take per enemy. In Unity, import the take as Legacy with Loop and give each instance a random start time so a wave does not move in lockstep. `PSXv2_EnemyAnim.gif` previews the loops.
| Enemy | Loop | Motion |
|---|---|---|
| Dart | 0.8 s | hull banks side to side, eye blinks, thruster flickers |
| Rotor | 1.6 s | ring and lamp pods spin 120 degrees (seamless for 3-fold symmetry), hub counter-rotates and pulses |
| Gunboat | 2 s | turret sweeps 26 degrees each way, barrels recoil alternately, engines flicker |
| Warden | 3 s | drum turrets sweep in opposite directions, gun pairs recoil alternately, reactor spins and pulses, engines flicker |

## Player hitbox
`PSXv2_Player_HitboxCore` is a separate child object: a white 12-gon disc whose radius is exactly the collider's (0.115 world units), centred at (0, 0.16), which matches CircleCollider2D offset (0, 0.08) × Ship scale 2. `PSXv2_Player_HitboxRing` is a thin magenta halo just outside it, and a black octagonal collar separates both from the white hull.
In Unity the installer moves both into a sibling `PSXv2 Hitbox` that does not bank with the ship. It renders unlit, always on top, on the Bullet sorting layer above the bullets, and stays visible through invulnerability blinking. Install check: the centre error is 0 and the radius is 0.1150 against the collider's 0.1153.

## Conventions
1 Blender unit is 1 world unit. +Y points up the screen and +Z toward the camera. The player faces +Y and enemies face -Y. The FBX is exported with forward Y and up Z. The installer measures the imported axes from the player's asymmetric bounds, which gives an axis correction of (90, 180, 0) and a scale of 1.
Large hull tops are cut into a plate grid (0.6 units for the gunboat, 1.0 for the boss), so the 16 px tiles repeat instead of stretching. PS1 games also subdivided large polygons to limit affine warping.

## Regenerate
```
python ArtSource/PSXv2/make_atlas.py          # atlas + FX sheets
# in Blender with the MCP addon (or blender -b -P):
ArtSource/PSXv2/build_ships.py                # models -> FBX/OBJ
ArtSource/PSXv2/render_preview.py             # 96px / 180x320 renders, no AA, Closest textures
python ArtSource/PSXv2/post_preview.py        # 15-bit + Bayer dither, nearest upscale
```

## PSX rendering (Render96 recipe → Unity)
| Render96 step | Blender preview | Unity status |
|---|---|---|
| Low internal resolution | 180 x 320 / 96 px renders | PSXPresentation 320-line RT exists, but only behind F9 |
| Nearest texture filtering | Image node set to Closest | Atlas must be imported as Point |
| No anti-aliasing | filter 0, 1 sample | MSAA off on the RT (already set) |
| Vertex snapping | n/a (stills) | PSXIndustrialShip.shader has it, but the installer sets `_VertexSnap` to 0; the proposed shader defaults it on |
| Affine textures | n/a | Not implemented; the proposed shader adds it, though it has no visible effect with the orthographic camera |
| Colour depth + dither | 15-bit + 4x4 Bayer | PSXComposite.shader has it, but only behind F9 |
