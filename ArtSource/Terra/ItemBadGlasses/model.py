import bpy
import math
import os
import sys
from mathutils import Vector

# Blender's Y axis is the item's vertical axis; the spectacle face points +Z.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
    if os.path.splitext(OUT)[1]:
        OUT = os.path.dirname(OUT)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

palette = [
    (0.56, 0.25, 0.70, 1),  # purple frame sides
    (0.75, 0.42, 0.86, 1),  # lavender front rim
    (0.88, 0.61, 0.94, 1),  # rim bevel and temple highlights
    (0.36, 0.67, 0.75, 1),  # muted blue lens
    (0.52, 0.79, 0.83, 1),  # lens facets
    (0.78, 0.91, 0.88, 1),  # lens glint and crack
    (0.39, 0.18, 0.50, 1),  # hinge accents
    (0.95, 0.72, 0.40, 1),  # little brass hinge pins
]
mats = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = 0.7
    mats.append(mat)

parts = []
def mesh_object(name, verts, faces, indices):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.materials.clear()
    for mat in mats:
        mesh.materials.append(mat)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    for poly, index in zip(mesh.polygons, indices):
        poly.material_index = index
        poly.use_smooth = False
    parts.append(obj)
    return obj

def ring(cx):
    # Four concentric profiles form real front and rear chamfers around a hole.
    n = 12
    profiles = [(0.194, -0.055), (0.215, -0.035), (0.215, 0.035),
                (0.194, 0.055), (0.153, 0.055), (0.145, 0.038),
                (0.145, -0.038), (0.153, -0.055)]
    verts = []
    for radius, z in profiles:
        for i in range(n):
            a = 2 * math.pi * (i + 0.5) / n
            verts.append((cx + radius * math.cos(a), radius * math.sin(a), z))
    faces, ids = [], []
    for j in range(len(profiles)):
        for i in range(n):
            k = (i + 1) % n
            faces.append((j*n+i, j*n+k, ((j+1)%len(profiles))*n+k, ((j+1)%len(profiles))*n+i))
            ids.append([0, 0, 2, 1, 2, 0, 0, 1][j])
    mesh_object('Chamfered rim', verts, faces, ids)

def lens(cx):
    n = 12
    verts = []
    for z in (-0.025, 0.025):
        for i in range(n):
            a = 2 * math.pi * (i + 0.5) / n
            verts.append((cx + 0.151 * math.cos(a), 0.151 * math.sin(a), z))
    verts += [(cx, 0, -0.025), (cx, 0, 0.025)]
    faces, ids = [], []
    for i in range(n):
        k = (i+1) % n
        faces += [(i, k, n+k, n+i), (2*n, k, i), (2*n+1, n+i, n+k)]
        ids += [3, 3 if i % 4 else 4, 3 if i % 4 else 4]
    mesh_object('Solid faceted lens', verts, faces, ids)

def box(name, center, size, material, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mats[material])
    if bevel:
        mod = obj.modifiers.new('Small solid bevels', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    parts.append(obj)
    return obj

def bar(name, a, b, width, depth, material, bevel=0):
    # In the XY plane, with thickness measured in Z.
    mid = (Vector(a) + Vector(b)) * 0.5
    obj = box(name, mid, (width, (Vector(b)-Vector(a)).length, depth), material, bevel)
    d = Vector(b) - Vector(a)
    obj.rotation_euler[2] = -math.atan2(d.x, d.y)
    return obj

for side in (-1, 1):
    cx = side * 0.225
    ring(cx)
    lens(cx)
    # Thick temples travel backwards, then curl gently down at the ear.
    box('Hinge', (side*0.427, 0.045, -0.045), (0.055, 0.105, 0.12), 0, 0.012)
    box('Temple', (side*0.429, 0.045, -0.205), (0.045, 0.073, 0.30), 1, 0.012)
    box('Temple tip', (side*0.429, 0.013, -0.363), (0.049, 0.105, 0.085), 0, 0.012)
    box('Hinge pin', (side*0.451, 0.047, -0.043), (0.009, 0.033, 0.033), 7, 0.004)

# A raised saddle bridge gives a readable gap between the lenses.
bar('Bridge left', (-0.077, 0.025, 0.008), (0, 0.067, 0.008), 0.052, 0.105, 1, 0.008)
bar('Bridge right', (0, 0.067, 0.008), (0.077, 0.025, 0.008), 0.052, 0.105, 1, 0.008)

# The large zigzag crack, with a branch, is inset against both lens faces.
for z in (0.028, -0.028):
    points = [(0.195, 0.132, z), (0.235, 0.062, z), (0.205, 0.015, z),
              (0.249, -0.035, z), (0.222, -0.128, z)]
    for a, b in zip(points, points[1:]):
        bar('Crack', a, b, 0.018, 0.007, 5)
    bar('Crack branch', (0.205, 0.015, z), (0.155, -0.025, z), 0.015, 0.007, 5)
    # One broad glint on the other lens, kept well inside its rim.
    bar('Lens glint', (-0.283, 0.018, z), (-0.245, 0.085, z), 0.025, 0.007, 5)

# Join and bake transforms. The entire asset remains one flat-shaded mesh.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for p in model.data.polygons:
    p.use_smooth = False

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
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1
scene.view_settings.view_transform = 'Standard'

def point_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, location, energy, target, size=1):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if kind == 'AREA':
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    obj.location = location
    point_at(obj, target)
    return obj

light('Upper left sun', 'SUN', (-3, 5, 6), 2.0, (0, 0, 0))
light('Soft fill', 'AREA', (4, 1, 4), 100, (0, 0, 0), 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 4)
point_at(cam, (0, 0, 0))
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.12
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)

cam.location = (1.35, 0.75, 2.5)
point_at(cam, (0, 0, -0.14))
cam_data.type = 'PERSP'
cam_data.lens = 80
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

