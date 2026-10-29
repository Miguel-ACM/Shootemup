import bpy
import math
import os
import sys
from mathutils import Vector

# The pickup is built in game coordinates: Y up, Z toward the front.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
    if not os.path.isdir(OUT):
        OUT = os.path.dirname(OUT)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

palette = [
    (0.88, 0.09, 0.14, 1),  # red charge shell
    (1.00, 0.22, 0.25, 1),  # coral shell plates
    (0.53, 0.10, 0.15, 1),  # red hardware
    (0.88, 0.48, 0.20, 1),  # warm metal
    (1.00, 0.74, 0.32, 1),  # sensor bezel
    (0.12, 0.32, 0.39, 1),  # lens surround
    (0.24, 0.79, 0.83, 1),  # sensor glass
    (0.68, 0.96, 0.91, 1),  # lens glint
]
mats = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.use_nodes = True
    mat.diffuse_color = color
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = 0.7
    mats.append(mat)

parts = []
def finish(obj, mat):
    obj.data.materials.append(mats[mat])
    for p in obj.data.polygons:
        p.use_smooth = False
    parts.append(obj)
    return obj

def sphere(loc, scale, mat, segments=12, rings=8):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=loc)
    obj = bpy.context.object
    obj.scale = scale
    return finish(obj, mat)

def cylinder(loc, radius, depth, mat, axis='Y', vertices=12):
    rotation = (math.pi / 2, 0, 0) if axis == 'Y' else (0, 0, 0)
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=rotation)
    return finish(bpy.context.object, mat)

def rod(a, b, radius, mat):
    a, b = Vector(a), Vector(b)
    delta = b - a
    bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=radius, depth=delta.length, location=(a + b) / 2)
    obj = bpy.context.object
    obj.rotation_mode = 'QUATERNION'
    obj.rotation_quaternion = delta.to_track_quat('Z', 'Y')
    return finish(obj, mat)

# Faceted spherical charge, reinforced around its equator.
sphere((0, 0.015, 0), (0.265, 0.285, 0.235), 0, 16, 10)
cylinder((0, 0.015, 0), 0.269, 0.105, 2, 'Y', 16)
cylinder((0, 0.015, 0), 0.273, 0.052, 1, 'Y', 16)

# Raised sensor assemblies on both sides remain legible as the item spins.
for side in (-1, 1):
    cylinder((0, 0.025, side * 0.220), 0.164, 0.075, 2, 'Z')
    cylinder((0, 0.025, side * 0.263), 0.145, 0.035, 4, 'Z')
    cylinder((0, 0.025, side * 0.286), 0.112, 0.022, 5, 'Z')
    sphere((0, 0.025, side * 0.302), (0.086, 0.086, 0.031), 6, 12, 6)
    sphere((-0.025, 0.052, side * 0.327), (0.021, 0.021, 0.009), 7, 8, 4)

# Stout forked sensor feelers and their high-contrast rounded terminals.
for side in (-1, 1):
    root = (side * 0.225, 0.045, 0)
    elbow = (side * 0.355, 0.105, 0)
    tip = (side * 0.389, 0.285, 0)
    sphere(root, (0.075, 0.075, 0.075), 3, 10, 6)
    rod(root, elbow, 0.041, 3)
    sphere(elbow, (0.046, 0.046, 0.046), 4, 8, 6)
    rod(elbow, tip, 0.036, 3)
    sphere(tip, (0.060, 0.060, 0.060), 4, 10, 6)

# Bottom mounting plug and crown give the charge a designed mechanical profile.
cylinder((0, -0.279, 0), 0.120, 0.100, 2, 'Y')
cylinder((0, -0.340, 0), 0.105, 0.045, 3, 'Y')
cylinder((0, -0.372, 0), 0.083, 0.020, 4, 'Y')
cylinder((0, 0.285, 0), 0.105, 0.045, 2, 'Y')
cylinder((0, 0.314, 0), 0.073, 0.025, 4, 'Y')

# Merge all meshes and bake transforms for a single exportable object.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for poly in model.data.polygons:
    poly.use_smooth = False

bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 512
scene.render.resolution_y = 512
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Blue gray world')
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(obj, target=(0, 0, 0)):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

bpy.ops.object.light_add(type='SUN', location=(-3, 4, 5))
sun = bpy.context.object
sun.data.energy = 2.0
sun.data.angle = math.radians(15)
aim(sun)
bpy.ops.object.light_add(type='AREA', location=(3, 1, 4))
fill = bpy.context.object
fill.data.energy = 120
fill.data.shape = 'DISK'
fill.data.size = 5
aim(fill)

bpy.ops.object.camera_add(location=(0, 0, 3))
cam = bpy.context.object
scene.camera = cam
cam.data.type = 'ORTHO'
cam.data.ortho_scale = 1.10
aim(cam)
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)

cam.location = (1.65, 1.15, 2.7)
cam.data.type = 'PERSP'
cam.data.lens = 50
aim(cam)
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)
