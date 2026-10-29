# Brief: model the hangar hub of a top-down shoot-em-up

You are modeling the background of the hub screen of a Unity vertical shoot-em-up roguelite. Between runs the player flies their small ship around inside this hangar: ships to switch to are parked on the LEFT, a shop with items for sale is on the RIGHT, and the run starts by flying out through the hangar doors at the TOP. The scene is seen from straight above by an orthographic camera and rendered unlit with PS1-style low-poly flat shading baked into vertex colours, so modelled 3D form (raised walls, recessed pits, beams, crates, cranes) reads through its colours and the light baked from the upper left. Inspiration: the cozy, cluttered hub of a roguelite (a gunship hangar bay at night): readable, warm and lived-in, not noisy.

Write ONE Blender 5.x Python script named `model.py` in the current directory. Do not run Blender yourself. The script must:

1. Clear the default scene, then build everything only from bpy primitives, bmesh and modifiers applied before export (no external files, no addons).
2. Coordinates: X = right, Y = screen up, +Z toward the camera above. The camera sees exactly X from -5.0 to +5.0 and Y from -8.9 to +8.9. Fill that whole rectangle (extend walls and floor a little past it). The floor is at z = 0; walls, machinery and props rise toward +Z (up to about z = 1.5); pits and the launch shaft go below 0.
3. Layout (these zones are fixed by gameplay, keep them clear and readable; the player's ship is about 0.6 wide and flies over the floor):
   - Central open deck: a wide clean floor lane down the middle (x from -2.0 to +2.0) with painted guide lines and arrows pointing UP toward the doors.
   - TOP (y from +6.6 to +8.9): the launch gate. Huge armoured blast doors slid open in the top wall, with hazard stripes, a glimpse of night sky / clouds beyond them (dark blue with a few pale cloud shapes), and warning lights on both sides. This must clearly read as "the way out".
   - LEFT bay (x from -5.0 to -2.0): FOUR circular landing pads for parked ships, centered at (-3.4, +4.2), (-3.4, +1.4), (-3.4, -1.4), (-3.4, -4.2), each about 1.7 across, slightly raised, with a ring of small lights and a painted marking. Keep the pad surfaces empty (the game places the ships on them), with gantries, fuel lines and tool racks around them.
   - RIGHT bay (x from +2.0 to +5.0): the SHOP: three display pedestals centered at (+3.4, +3.4), (+3.4, +0.6), (+3.4, -2.2), each a raised round plinth about 1.2 across with a glowing rim, inside a shop counter area with shelves, crates, a vending machine and a sign shape (no letters or text anywhere). Keep the plinth tops empty (the game places items on them).
   - BOTTOM (y from -8.9 to -6.4): the back of the hangar: workshop machinery, a crane rail, stacked crates, barrels, a workbench. The player starts at (0, -5.8), so keep that spot clear.
4. Materials: give each material a flat Base Color (no textures, no transparency). Materials whose name starts with `glow_` are lamps, screens and light strips: they are drawn at full brightness, so use them for the pad light rings, plinth rims, warning lights and a few lamps (keep them small; the ships and bullets must stay the brightest things). Everything else gets the baked lighting. Palette: dark slate and steel blue-gray floor and walls, worn concrete, yellow/black hazard stripes, rust, olive and a few warm amber/teal lamp accents. Keep the floor darker and calmer than the props so the player's ship and the items stand out.
5. Budget: 4000-20000 triangles. Flat shading. Big readable shapes first, then a moderate amount of detail. Avoid noisy greebles, rows of identical buttons and stretched rods.
6. Join everything into ONE mesh object named `Model` (multiple material slots are fine), apply all transforms and modifiers.
7. Export `model.obj` (+ .mtl) next to the script with `bpy.ops.wm.obj_export` (export materials, forward_axis='Y', up_axis='Z', so coordinates are unchanged).
8. Render two PNGs next to the script with Cycles (CPU, 48 samples), one sun light from the upper left plus a soft fill:
   - `top.png` (450x800): orthographic camera straight down (+Z looking at -Z) framing exactly X -5..5, Y -8.9..8.9 (the gameplay view).
   - `angle.png` (800x800): perspective view from the bottom-front, tilted, so the 3D form is visible.
   Find the script directory from `__file__`, falling back to the path after `--` in sys.argv.

Use the Blender 5.x API (`bpy.ops.wm.obj_export`, `Material.use_nodes`, Principled BSDF input "Base Color"). When done, reply with one short paragraph describing the design.
