import bpy
import math
import os
import sys
from mathutils import Vector

# The model uses the game coordinate convention directly: Y up, Z front.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
    if OUT.lower().endswith('.py'):
        OUT = os.path.dirname(OUT)
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for mat in list(bpy.data.materials):
    bpy.data.materials.remove(mat)

colors = [
    (0.82, 0.64, 0.26, 1), # golden desktop
    (0.65, 0.47, 0.17, 1), # wooden edges
    (0.35, 0.43, 0.30, 1), # sage legs
    (0.46, 0.54, 0.38, 1), # sage highlights
    (0.34, 0.31, 0.48, 1), # violet book cover
    (0.78, 0.83, 0.79, 1), # paper
    (0.95, 0.38, 0.12, 1), # orange pencil
    (0.53, 0.61, 0.62, 1), # ceramic cup
    (0.31, 0.38, 0.39, 1), # cup interior
    (0.95, 0.82, 0.52, 1), # pencil wood / hardware
    (0.24, 0.35, 0.40, 1), # graphite
]
mats = []
for i, color in enumerate(colors):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = color
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = 0.8
    mats.append(m)

parts = []
def box(name, loc, size, mat, bevel=0.01, rotation=None):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    ob = bpy.context.object
    ob.name = name
    ob.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = ob.modifiers.new('Chamfer', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = ob
        bpy.ops.object.modifier_apply(modifier=mod.name)
    if rotation:
        ob.rotation_euler = rotation
    ob.data.materials.append(mats[mat])
    parts.append(ob)
    return ob

def cylinder(name, loc, radius, depth, mat, vertices=10, rotation=(math.pi/2, 0, 0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=rotation)
    ob = bpy.context.object
    ob.name = name
    ob.data.materials.append(mats[mat])
    parts.append(ob)
    return ob

# The tabletop is thick enough to read head on, with a darker apron beneath.
box('Solid desktop', (0, 0.035, 0), (0.84, 0.115, 0.52), 0, 0.025)
box('Front apron', (0, -0.059, 0.194), (0.70, 0.095, 0.085), 1, 0.012)
box('Back apron', (0, -0.059, -0.194), (0.70, 0.095, 0.085), 1, 0.012)
box('Left apron', (-0.32, -0.059, 0), (0.08, 0.095, 0.35), 1, 0.01)
box('Right apron', (0.32, -0.059, 0), (0.08, 0.095, 0.35), 1, 0.01)
# Four substantial legs make the desk convincing as it turns.
for x in (-0.31, 0.31):
    for z in (-0.17, 0.17):
        box('Desk leg', (x, -0.258, z), (0.095, 0.39, 0.095), 2, 0.012)
        box('Foot', (x, -0.437, z), (0.105, 0.047, 0.105), 3, 0.008)
# A broad drawer and central handle identify the front and back at a glance.
for z in (-0.244, 0.244):
    box('Drawer panel', (0.0, -0.06, z), (0.34, 0.065, 0.018), 0, 0.008)
    box('Drawer pull', (0.0, -0.055, z + (0.015 if z > 0 else -0.015)), (0.105, 0.023, 0.025), 9, 0.006)

# An unmistakable closed book: purple binding, ivory page block, broad spine.
box('Book bottom cover', (0.075, 0.116, -0.015), (0.42, 0.037, 0.30), 4, 0.009)
box('Book page block', (0.075, 0.150, -0.015), (0.385, 0.039, 0.27), 5, 0.008)
box('Book spine', (-0.123, 0.151, -0.015), (0.035, 0.073, 0.30), 4, 0.008)
# A wide orange pencil resting across the book, deliberately substantial at icon size.
box('Pencil', (0.16, 0.182, -0.02), (0.035, 0.028, 0.20), 6, 0.005)
box('Pencil tip wood', (0.16, 0.182, 0.095), (0.029, 0.025, 0.035), 9, 0.004)
box('Pencil graphite', (0.16, 0.182, 0.116), (0.018, 0.018, 0.014), 10, 0.003)

# Hollow faceted mug built as a ring with a visible interior and bottom.
def mug():
    n = 10
    cx, cz = -0.285, 0.045
    rings = [(0.062, 0.095), (0.073, 0.225), (0.052, 0.225), (0.045, 0.125)]
    verts = [(cx + r*math.cos(2*math.pi*i/n), y, cz + r*math.sin(2*math.pi*i/n)) for r, y in rings for i in range(n)]
    faces = []
    indices = []
    for j in range(3):
        for i in range(n):
            a, b = j*n+i, j*n+(i+1)%n
            faces.append((a, b, b+n, a+n))
            indices.append(8 if j == 2 else 7)
    faces.append(tuple(reversed(range(n))))
    indices.append(7)
    faces.append(tuple(3*n+i for i in range(n)))
    indices.append(8)
    mesh = bpy.data.meshes.new('Mug mesh')
    mesh.from_pydata(verts, [], faces)
    mesh.materials.append(mats[7]); mesh.materials.append(mats[8])
    mesh.update()
    ob = bpy.data.objects.new('Mug', mesh)
    bpy.context.collection.objects.link(ob)
    for p, idx in zip(mesh.polygons, indices):
        p.material_index = 1 if idx == 8 else 0
    parts.append(ob)
mug()
# Chunky open handle on the outer side of the cup.
box('Mug handle outer', (-0.378, 0.166, 0.045), (0.028, 0.093, 0.034), 7, 0.005)
box('Mug handle upper', (-0.352, 0.204, 0.045), (0.065, 0.027, 0.034), 7, 0.005)
box('Mug handle lower', (-0.352, 0.128, 0.045), (0.065, 0.027, 0.034), 7, 0.005)

# Center the complete assembly around the origin along its vertical axis.
for ob in parts:
    ob.location.y += 0.115

# Bake all geometry, join, and triangulate for deterministic flat-shaded export.
bpy.ops.object.select_all(action='DESELECT')
for ob in parts:
    ob.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
tri = model.modifiers.new('Triangulate', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
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
scene.world = bpy.data.worlds.new('Blue gray world') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def point_at(ob, target):
    ob.rotation_euler = (Vector(target) - ob.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, loc, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    ob = bpy.data.objects.new(name, data)
    scene.collection.objects.link(ob)
    ob.location = loc
    point_at(ob, target)
    return ob

light('Upper left sun', 'SUN', (-3, 5, 4), 2.0, (0, 0, 0))
light('Soft fill', 'AREA', (3, 2, 4), 35, (0, 0, 0), 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 3)
point_at(cam, (0, 0, 0))
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.16
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam_data.lens = 50
cam.location = (0.9, 0.65, 1.25)
point_at(cam, (0, 0, 0))
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

