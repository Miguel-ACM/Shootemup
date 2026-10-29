# Brief: model one player ship for a top-down shoot-em-up

You are modeling one of the PLAYER's selectable ships for a Unity vertical shoot-em-up. The game renders low-poly, flat-shaded, PS1-flavoured 3D ships seen from almost straight above. The player ship is about 110-150 pixels tall on screen, banks up to 28 degrees around its nose axis when strafing and rolls a full 360 degrees during a barrel roll, so the sides and a little of the underside must look finished. The attached image is the current default ship (GR-07, a gray twin-boom interceptor). The new ship must sit in the same family (same PS1 low-poly technique, panel lines, layered armour plates, same scale) but have its own clearly different silhouette and colours.

Write ONE Blender 5.x Python script named `model.py` in the current directory. Do not run Blender yourself. The script must:

1. Clear the default scene, then build the model only from bpy primitives, bmesh and modifiers applied before export (no external files, no addons).
2. Coordinates: X = right, Y = screen up, the ship's NOSE POINTS TOWARD +Y (it flies up the screen). +Z is toward the camera above. Center the model near the origin. Units: the default GR-07 is about 0.62 wide (X) and 0.56 long (Y), from y = -0.27 to +0.30, about 0.12 thick. Follow the footprint given below for this ship.
3. Engines: put an engine nozzle opening (a recessed ring or bell) at each engine point listed below, facing -Y. The game draws animated exhaust flames there; do NOT model flames.
4. Weapons: put a real, visible barrel lip or emitter at each muzzle point listed below (if any). If the ship has no gun, it must look unarmed.
5. Materials: name them `tile_00` .. `tile_15` (at most 16), flat colours, no textures, transparency or emission. YOU choose each material's Base Color: it is exactly the colour the game uses (dark bevel rims and highlights are added by the game). Use mid-to-bright colours; near-black only for small details (vents, nozzle interiors).
6. Budget: 600-3000 triangles. Flat shading. A distinct, readable silhouette at small size; avoid noisy greebles that turn into mush, rows of identical buttons and stretched rods. Real 3D form (bevels, curvature, layered plates), not a flat extrusion.
7. Join everything into ONE mesh object named `Model` (multiple material slots are fine), apply all transforms and modifiers. Animated parts are optional (see below).
8. Export `model.obj` (+ .mtl) next to the script with `bpy.ops.wm.obj_export` (export materials, forward_axis='Y', up_axis='Z', so coordinates are unchanged).
9. Render two 640x640 PNGs next to the script with Cycles (CPU, 48 samples), neutral dark blue-gray world (0.05,0.07,0.09), one sun light from the upper left plus a soft fill:
   - `top.png`: orthographic camera straight down (+Z looking at -Z), framing the model with ~15% margin (this is the gameplay view).
   - `angle.png`: perspective 3/4 view from the front-right-above, so the 3D form is visible.
   Find the script directory from `__file__`, falling back to the path after `--` in sys.argv.

Use the Blender 5.x API (`bpy.ops.wm.obj_export`, `Material.use_nodes`, Principled BSDF input "Base Color"). When done, reply with one short paragraph describing the design.

## Animated parts (optional)

If the ship naturally has a moving part (a spinning radar dish, turning sensor, small rotor), model it as a SEPARATE mesh object with a short unique name and write `parts.json` next to the script: a JSON list with one entry per part, `{"object": "Radar", "motion": "spin", "axis": [0, 0, 1], "pivot": [x, y, z], "speed": 90}` (degrees per second) or `{"object": "Fin_R", "motion": "swing", "axis": [0, 1, 0], "pivot": [x, y, z], "speed": 1.2, "amplitude": 10}` (cycles per second, degrees). Use 0-2 parts; write an empty list if nothing moves. Never animate a part that carries a muzzle. Export the body and the parts together into the one model.obj and include them in both renders.
