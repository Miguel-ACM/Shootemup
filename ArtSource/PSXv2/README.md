# PSXv2 fleet (Blender)

Player ship, four animated enemies, 17 equipment/pickup models and sphere explosions, built in Blender 5.2 through the Blender MCP addon.

Install with **Tools > PSX Player > Install PSXv2 Blender fleet** (`Assets/Editor/InstallPSXv2.cs`). It also runs when `ArtSource/PSXv2/install.request` exists, and `capture.request` replays the showcase capture. The installer:
- sets the importers: Point filtering, legacy looping clips and Repeat wrap for the fire skin;
- builds the materials;
- swaps the player visual in L1 and PSX_Showcase;
- swaps the visuals in the enemy1/waver/Biggie/stage1boss prefabs;
- bakes the item meshes and swaps them into the pickups, shop item and companions;
- plays the showcase with an inert boss, a gunboat and test explosions staged, and captures frames.

Results go to `InstallReport.json` and `UnityCapture/`.
The old GR-07 player visual is kept in both scenes, but disabled. Do not run the older **Install industrial fleet and UI** again: it rebuilds the old enemy meshes and turns the PSX filter back off.

## Files
- `Assets/Art/PSXv2/*.fbx`: player, 4 enemies and `PSXv2_Items.fbx`. All are triangulated and flat-shaded, with UVs into one atlas and vertex-colour occlusion ("Col"). Baked item meshes live in `Items/*.asset`.
- `Assets/Art/PSXv2/PSXv2_Atlas.png`: 128 x 128, 8 x 8 tiles of 16 px, quantised to 15-bit colour.
  - Row 0: steel, hazard and vents.
  - Row 1: faction paint.
  - Row 2: glows (fullbright in the shader).
  - Row 3: item materials.
  - Row 4: item decals (med cross, battery bolt, coin face, P label, chrome, brass).
  - Row 5: boss detail (deep purple, lit windows, hull number, caution stripes, grating, screen, white armour).
- `Assets/Art/PSXv2/FX/Fireball_64.png`: tileable fire skin for the explosion spheres.
- Shaders in `Assets/Shaders`: `PSXv2Lit` (vertex snap to the low-res grid, affine UVs, banded light, fullbright glow row), `PSXv2Hitbox` (unlit, always on top) and `PSXv2Fireball`.
- `ArtSource/PSXv2`: the generator scripts, OBJ copies and the previews (`PSXv2_Catalog.png`, `PSXv2_Items.png`, `PSXv2_EnemyAnim.gif`, `PSXv2_Playfield.png`).

## Modelling
Parts are lofted rather than straight extrusions: tops taper, so the sides slope and catch the banded light. Raised inset panels, round tubes (nacelles, barrels, pipes) and UV spheres add volume. Large hull tops are cut into a plate grid (0.6 units for the gunboat, 1.0 for the boss), so the 16 px tiles repeat instead of stretching. PS1 games also subdivided large polygons.

| Model | Role | Look | Tris |
|---|---|---|---|
| PSXv2_Player | player | GR-X: split Ikaruga-style gun prongs (bullets spawn from them), raised cockpit collar around the hitbox, swept two-layer white/blue wings with red fins, round engine nacelles, missile pods | 541 |
| Dart | enemy1 | red arrowhead with canopy, hazard stripes and wing missiles | 146 |
| Rotor | waver | orange hex ring with a hole, domed glowing hub, 3 lamp pods | 182 |
| Gunboat | Biggie | teal twin hull, bridge with lit windows, domed turret with round barrels, deck pipes | 418 |
| Warden | stage1boss | layered purple carrier: steel deck, stepped white spine with hull number, bridge tower with windows, screen and radar, two drum turrets with twin cannons, four point-defence turrets, pipe runs, caution armour, engine block with four nozzles | 2010 |

The boss is now fitted to its collider in world space. Its prefab root is scaled 0.5, which previously drew it at twice its hull size.

## Enemy animation
Each enemy is a root empty plus rigid parts pivoted at their hinges. Loops are keyed on twos (15 Hz at 30 fps) with constant interpolation, giving stepped PS1-style motion. Each enemy's loop is baked as one legacy clip, and every instance starts at a random time.

| Enemy | Loop | Motion |
|---|---|---|
| Dart | 0.8 s | banks, eye blinks, thruster flickers |
| Rotor | 1.6 s | ring and pods spin, hub counter-rotates and pulses |
| Gunboat | 2 s | turret sweeps, barrels recoil alternately, engines flicker |
| Warden | 3 s | drum turrets sweep and fire alternately, radar spins, point-defence turrets spin, reactor pulses, lamps blink, engine plumes flicker |

## Items
Each item is one closed mesh, standing upright and facing the camera. The installer bakes the meshes into Unity orientation and centres them, then swaps them into the existing spinning `Industrial model` children:
- the life/mana/money pickups;
- `Item.prefab`, which picks its mesh per item kind;
- the four companions.

The items are: medical kit, battery, coin, rabbit paw, blank cartridge, helper robot, horseshoe magnet, rifle, coffee mug, protein tub, moon, mini player ship, lightning bolt, arm, angry moon, bent spoon and three moons.

## Effects
- **Hit sparks:** the original streak sparks (`PSXImpactEffects.Burst`).
- **Explosions** (`PSXFireballs`): several low-poly spheres with a scrolling fire skin. They pop in staggered, grow with a stepped ease-out, spin, and cool from white-hot to yellow, orange and deep red, then dissolve with ordered dither. Blending is additive (PS1 semi-transparency B+F). Small enemies get 4 spheres; large hulls get 6–14 spread over 0.7 s.

## Player hitbox
`PSXv2_Player_HitboxCore` is a white 12-gon whose radius is exactly the collider's (0.115 world units). It is centred at (0, 0.16), which matches the CircleCollider2D offset (0, 0.08) × Ship scale 2. A thin magenta ring sits just outside it.
In Unity both live in a sibling `PSXv2 Hitbox` that does not bank with the ship. They render unlit, always on top, above the bullets, and stay visible through invulnerability blinking.

## Conventions
1 Blender unit is 1 world unit. +Y points up the screen and +Z toward the camera. The player faces +Y and enemies face -Y. The installer derives the FBX axis correction (90, 180, 0) from the player's asymmetric bounds.

## Regenerate
```
python ArtSource/PSXv2/make_atlas.py          # atlas + fire skin
# in Blender with the MCP addon (or blender -b -P):
ArtSource/PSXv2/build_ships.py                # models -> FBX/OBJ
ArtSource/PSXv2/render_preview.py             # low-res renders, no AA, Closest textures
python ArtSource/PSXv2/post_preview.py        # 15-bit + Bayer dither, nearest upscale, GIF
```
