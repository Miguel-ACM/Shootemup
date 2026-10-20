"""PSX-style preview renders of the PSXv2 fleet (run inside Blender after build_ships.py).

Render96-style settings: tiny internal resolution, no anti-aliasing (filter 0, 1 sample),
nearest texture sampling, flat shading, hard sun light. Colour-depth reduction and
ordered dithering are applied afterwards by post_preview.py (PIL), then nearest upscaled.
Atlas row 2 (v 0.625-0.75) is treated as emissive, matching the proposed Unity shader.
"""
import math
import os
import bpy
from mathutils import Vector

ROOT = r"C:\Users\Miguel\Shootemup"
TMP = os.path.join(ROOT, "ArtSource", "PSXv2", "renders")
os.makedirs(TMP, exist_ok=True)
sc = bpy.context.scene


def glow_rows(mat):
    nt = mat.node_tree
    tex = next(n for n in nt.nodes if n.type == "TEX_IMAGE")
    diff = next(n for n in nt.nodes if n.type == "BSDF_DIFFUSE")
    out = next(n for n in nt.nodes if n.type == "OUTPUT_MATERIAL")
    if any(n.type == "MIX_SHADER" for n in nt.nodes):
        return
    uv = nt.nodes.new("ShaderNodeUVMap")
    sep = nt.nodes.new("ShaderNodeSeparateXYZ")
    a = nt.nodes.new("ShaderNodeMath"); a.operation = "GREATER_THAN"; a.inputs[1].default_value = 0.625
    b = nt.nodes.new("ShaderNodeMath"); b.operation = "LESS_THAN"; b.inputs[1].default_value = 0.75
    m = nt.nodes.new("ShaderNodeMath"); m.operation = "MULTIPLY"
    em = nt.nodes.new("ShaderNodeEmission"); em.inputs["Strength"].default_value = 1.6
    mix = nt.nodes.new("ShaderNodeMixShader")
    nt.links.new(uv.outputs[0], sep.inputs[0])
    nt.links.new(sep.outputs[1], a.inputs[0]); nt.links.new(sep.outputs[1], b.inputs[0])
    nt.links.new(a.outputs[0], m.inputs[0]); nt.links.new(b.outputs[0], m.inputs[1])
    nt.links.new(tex.outputs[0], em.inputs["Color"])
    nt.links.new(m.outputs[0], mix.inputs[0])
    nt.links.new(diff.outputs[0], mix.inputs[1]); nt.links.new(em.outputs[0], mix.inputs[2])
    nt.links.new(mix.outputs[0], out.inputs[0])


def setup():
    sc.render.engine = "BLENDER_EEVEE"
    sc.eevee.taa_render_samples = 1
    sc.render.filter_size = 0.0
    sc.render.film_transparent = True
    sc.render.image_settings.file_format = "PNG"
    sc.render.image_settings.color_mode = "RGBA"
    sc.view_settings.view_transform = "Standard"
    sc.view_settings.look = "None"
    sc.render.resolution_percentage = 100
    for o in list(bpy.data.objects):
        if o.type in ("CAMERA", "LIGHT"):
            bpy.data.objects.remove(o, do_unlink=True)
    sun = bpy.data.objects.new("PSX_Sun", bpy.data.lights.new("PSX_Sun", "SUN"))
    sun.data.energy = 3.2
    sun.data.angle = 0.0
    sun.rotation_euler = (math.radians(35), math.radians(-25), math.radians(20))
    sc.collection.objects.link(sun)
    world = sc.world or bpy.data.worlds.new("World")
    sc.world = world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.32, 0.33, 0.36, 1)
    world.node_tree.nodes["Background"].inputs[1].default_value = 0.9
    cam = bpy.data.objects.new("PSX_Cam", bpy.data.cameras.new("PSX_Cam"))
    cam.data.type = "ORTHO"
    sc.collection.objects.link(cam)
    sc.camera = cam
    glow_rows(bpy.data.materials["PSXv2_Steel"])
    return cam


def shot(cam, name, center, size, res, tilt=0.0):
    cam.data.ortho_scale = size
    d = 30
    cam.location = Vector(center) + Vector((0, -d * math.sin(tilt), d * math.cos(tilt)))
    cam.rotation_euler = (tilt, 0, 0)
    cam.data.clip_end = 100
    sc.render.resolution_x, sc.render.resolution_y = res
    sc.render.filepath = os.path.join(TMP, name + ".png")
    bpy.ops.render.render(write_still=True)


cam = setup()
sc.frame_set(0)
names = ["PSXv2_Player", "PSXv2_Enemy_0_Dart", "PSXv2_Enemy_1_Rotor", "PSXv2_Enemy_2_Gunboat", "PSXv2_Enemy_3_Warden"]
sizes = {"PSXv2_Player": 1.6, "PSXv2_Enemy_0_Dart": 1.6, "PSXv2_Enemy_1_Rotor": 1.6, "PSXv2_Enemy_2_Gunboat": 4.4, "PSXv2_Enemy_3_Warden": 13.5}
for n in names:
    ob = bpy.data.objects[n]
    c = ob.location.copy()
    shot(cam, n + "_top", c, sizes[n], (96, 96))
    shot(cam, n + "_tilt", c + Vector((0, 0.1 * sizes[n], 0)), sizes[n] * 1.1, (96, 96), tilt=math.radians(40))

# In-game framing mock: portrait playfield 10 x 17.7 units at a 180x320 internal resolution.
layout = {"PSXv2_Player": (0, -6.0), "PSXv2_Enemy_0_Dart": (-2.5, 0.5), "PSXv2_Enemy_1_Rotor": (2.8, -1.2),
          "PSXv2_Enemy_2_Gunboat": (-2.2, -3.0), "PSXv2_Enemy_3_Warden": (0, 4.8)}
saved = {n: bpy.data.objects[n].location.copy() for n in names}
for n, (x, y) in layout.items():
    bpy.data.objects[n].location = (x, y, 0)
bpy.data.objects["PSXv2_Enemy_3_Warden"].scale = (0.5, 0.5, 0.5)  # stage1boss root scale: in-game size = collider
cam.data.sensor_fit = "VERTICAL"
shot(cam, "Playfield", (0, 0, 0), 17.7, (180, 320))
cam.data.sensor_fit = "AUTO"
for n in names:
    bpy.data.objects[n].location = saved[n]
bpy.data.objects["PSXv2_Enemy_3_Warden"].scale = (1, 1, 1)
# Animation strips: every key (on twos) of each enemy loop, top view.
for n in names[1:]:
    loop = bpy.data.objects[n]["loop_frames"]
    c = bpy.data.objects[n].location.copy()
    for fr in range(0, loop, 2):
        sc.frame_set(fr)
        shot(cam, f"anim/{n}_{fr:03d}", c, sizes[n] * 1.1, (96, 96))
sc.frame_set(0)
# Item catalogue: all 17 equipment models in a grid, slightly turned to show their depth.
items = [o for o in bpy.data.objects if o.name.startswith("Item_")]
home = {o.name: (o.parent, o.matrix_world.copy()) for o in items}
for k, o in enumerate(items):
    o.parent = None
    o.location = (8 + (k % 6) * 1.4, -20 - (k // 6) * 1.4, 0)
    o.rotation_euler = (math.radians(-20), math.radians(25), 0)
cam.data.sensor_fit = "AUTO"
shot(cam, "Items", (8 + 2.5 * 1.4, -20 - 1.4, 0), 8.6, (192, 96))
for o in items:
    o.parent = home[o.name][0]
    o.matrix_world = home[o.name][1]
print("rendered")
