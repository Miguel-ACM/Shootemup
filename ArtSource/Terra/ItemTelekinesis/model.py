import bpy
import math
import os
import sys
from mathutils import Vector

# Output beside this script, including when invoked with Blender's -- argument.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    if '--' not in args or not args[args.index('--') + 1:]:
        raise RuntimeError('Pass the script path after -- when __file__ is unavailable')
    OUT = os.path.dirname(os.path.abspath(args[args.index('--') + 1]))

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

silver = bpy.data.materials.new('tile_00')
silver.diffuse_color = (0.65, 0.68, 0.72, 1.0)
silver.use_nodes = True
bsdf = silver.node_tree.nodes.get('Principled BSDF')
bsdf.inputs['Base Color'].default_value = silver.diffuse_color
bsdf.inputs['Metallic'].default_value = 0.25
bsdf.inputs['Roughness'].default_value = 0.38

# The whole utensil is centred by its overall bounding box. Its bent neck and
# oval head follow the positions and proportions of the original model.
verts, faces = [], []
def vertex(p):
    verts.append((p[0] - 0.13, p[1], p[2]))
    return len(verts) - 1

def bridge(a, b):
    n = len(a)
    for i in range(n):
        j = (i + 1) % n
        faces.append((a[i], a[j], b[j], b[i]))

# Handle: gently rounded, tapered cross sections along the original crooked
# path; the flattened octagonal profile remains substantial in the side view.
sections = [
    (-0.450, 0.000, 0.019, 0.018),
    (-0.438, 0.000, 0.031, 0.026),
    (-0.405, 0.000, 0.035, 0.029),
    (-0.330, 0.000, 0.035, 0.030),
    (-0.220, 0.000, 0.034, 0.030),
    (-0.100, 0.000, 0.032, 0.029),
    (-0.015, 0.000, 0.031, 0.028),
    ( 0.020, 0.008, 0.031, 0.028),
    ( 0.055, 0.035, 0.030, 0.027),
    ( 0.095, 0.075, 0.030, 0.026),
    ( 0.135, 0.115, 0.034, 0.025),
    ( 0.165, 0.145, 0.040, 0.023),
]
handle = []
for y, x, width, depth in sections:
    ring = []
    for i in range(10):
        a = 2 * math.pi * i / 10
        ring.append(vertex((x + width * math.cos(a), y, depth * math.sin(a))))
    handle.append(ring)
for a, b in zip(handle, handle[1:]):
    bridge(a, b)
faces.append(tuple(reversed(handle[0])))
faces.append(tuple(handle[-1]))

# A solid, shallow dished bowl: concentric faceted rings form the concave
# front, rolled edge and convex back. The elongated oval is centred to the
# right of the handle, as on the shipped pickup.
n = 20
cx, cy = 0.18, 0.275
rx, ry = 0.125, 0.175
profiles = [
    (0.16, -0.014), (0.38, -0.011), (0.62, -0.001),
    (0.82, 0.014), (0.96, 0.030), (1.00, 0.034),
    (1.00, 0.022), (0.94, 0.009), (0.78, -0.018),
    (0.54, -0.040), (0.28, -0.052),
]
bowl = []
for radius, z in profiles:
    ring = []
    for i in range(n):
        a = 2 * math.pi * i / n
        ring.append(vertex((cx + rx * radius * math.cos(a),
                            cy + ry * radius * math.sin(a), z)))
    bowl.append(ring)
front_center = vertex((cx, cy, -0.015))
back_center = vertex((cx, cy, -0.055))
for i in range(n):
    j = (i + 1) % n
    faces.append((front_center, bowl[0][i], bowl[0][j]))
for a, b in zip(bowl, bowl[1:]):
    bridge(a, b)
for i in range(n):
    j = (i + 1) % n
    faces.append((bowl[-1][i], back_center, bowl[-1][j]))

mesh = bpy.data.meshes.new('SpoonMesh')
mesh.from_pydata(verts, [], faces)
mesh.update()
model = bpy.data.objects.new('Model', mesh)
bpy.context.collection.objects.link(model)
model.data.materials.append(silver)
for poly in mesh.polygons:
    poly.use_smooth = False
# Recalculate normals on both closed components and triangulate for a stable
# low-poly export. Both operations are applied before export.
bpy.context.view_layer.objects.active = model
model.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.mesh.normals_make_consistent(inside=False)
bpy.ops.mesh.quads_convert_to_tris()
bpy.ops.object.mode_set(mode='OBJECT')
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
assert 300 <= len(mesh.polygons) <= 2000
bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'),
                      export_selected_objects=True, export_materials=True,
                      forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 512
scene.render.resolution_y = 512
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.render.film_transparent = False
world = bpy.data.worlds.new('Blue gray world')
scene.world = world
world.use_nodes = True
world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0

def aim(obj, point=(0, 0, 0)):
    obj.rotation_euler = (Vector(point) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, location, energy, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    obj.location = location
    aim(obj)
    return obj

sun = light('Upper left sun', 'SUN', (-3, 4, 5), 2.0)
sun.data.angle = math.radians(18)
light('Soft fill', 'AREA', (3, 1, 4), 130, 5.0)

cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 3.5)
aim(cam)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.29
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (1.8, 1.0, 3.2)
aim(cam)
cam_data.type = 'PERSP'
cam_data.lens = 50
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)
