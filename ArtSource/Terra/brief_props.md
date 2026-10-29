# Brief: remodel one scenery prop for a top-down shoot-em-up

You are remodeling one background scenery prop of a Unity vertical shoot-em-up. The game renders low-poly, flat-shaded, PS1-flavoured 3D models seen from almost straight above. Props sit behind the combat layer, so they must look rich and clearly shaped but stay calmer than enemies and bullets (no glowing parts). The current prop is built procedurally in C#; its source is given below. Read it to understand the object, its size, orientation, pivot and colours, then build a clearly better model of the same object.

Write ONE Blender 5.x Python script named `model.py` in the current directory. Do not run Blender yourself. The script must:

1. Clear the default scene, then build the model only from bpy primitives, bmesh and modifiers applied before export (no external files, no addons).
2. Coordinates: use exactly the same coordinate frame, orientation, pivot and size as the current C# model (X right, Y up the screen, and +Z toward the camera; the C# code uses -Z toward the camera, so negate its z values). Match its extents within about 10%.
3. Materials: name them `tile_NN` using ONLY the palette tiles listed below for this prop (their colours come from the game's atlas; set each Blender material's Base Color to the listed colour so the renders match). Flat colours, no textures, transparency or emission.
4. Budget: 150-1500 triangles (props appear many times on screen). Flat shading. Real 3D form with a recognisable silhouette from above.
5. Join everything into ONE mesh object named `Model`, apply all transforms and modifiers.
6. Export `model.obj` (+ .mtl) next to the script with `bpy.ops.wm.obj_export` (export materials, forward_axis='Y', up_axis='Z', so coordinates are unchanged).
7. Then render two 512x512 PNGs next to the script with Cycles (CPU, 48 samples), dark blue-gray world (0.05,0.07,0.09), one sun from the upper left plus a soft fill: `top.png` (orthographic, straight down from +Z, ~15% margin) and `angle.png` (perspective 3/4 view, model filling about 70% of the frame). Find the script directory from `__file__`, falling back to the path after `--` in sys.argv.

Use the Blender 5.x API. When done, reply with one short paragraph describing the design.
