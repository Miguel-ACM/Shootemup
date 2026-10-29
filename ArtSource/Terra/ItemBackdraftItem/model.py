import bpy
import math
import os
import sys
from mathutils import Vector

# Unity pickup axes: X right, Y up, Z front.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
    if OUT.lower().endswith('.py'):
        OUT = os.path.dirname(OUT)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablock in bpy.data.materials:
    bpy.data.materials.remove(datablock)

palette = [
    (0.82, 0.16, 0.07, 1),  # enamel red-orange
    (1.00, 0.34, 0.09, 1),  # vermilion
    (1.00, 0.58, 0.16, 1),  # warm orange
    (0.47, 0.19, 0.12, 1),  # warm iron
    (0.68, 0.36, 0.20, 1),  # copper
    (0.80, 0.73, 0.56, 1),  # pale metal
    (0.30, 0.34, 0.37, 1),  # steel recess
    (1.00, 0.76, 0.29, 1),  # brass
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
def mesh_part(name, verts, faces, indices):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    for mat in mats:
        mesh.materials.append(mat)
    for poly, idx in zip(mesh.polygons, indices):
        poly.material_index = idx
        poly.use_smooth = False
    parts.append(obj)
    return obj

def rings(name, cx, cz, sections, sides=12, side_materials=None, cap=0):
    # Each section is (height, x radius, z radius); bottom to top.
    verts = []
    for y, rx, rz in sections:
        for j in range(sides):
            a = 2 * math.pi * j / sides
            verts.append((cx + rx * math.cos(a), y, cz + rz * math.sin(a)))
    faces, ids = [], []
    faces.append(tuple(reversed(range(sides))))
    ids.append(cap)
    for i in range(len(sections)-1):
        for j in range(sides):
            faces.append((i*sides+j, i*sides+(j+1)%sides, (i+1)*sides+(j+1)%sides, (i+1)*sides+j))
            ids.append(side_materials[i] if side_materials else cap)
    faces.append(tuple((len(sections)-1)*sides+j for j in range(sides)))
    ids.append(cap)
    return mesh_part(name, verts, faces, ids)

def box(name, loc, scale, material, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mats[material])
    if bevel:
        mod = obj.modifiers.new('Hard bevel', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        mod.affect = 'EDGES'
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    for face in obj.data.polygons:
        face.use_smooth = False
    parts.append(obj)
    return obj

# Paired pressure bottles: stepped shoulders, bright collars and stout valves.
for x in (-0.235, 0.235):
    rings('Fuel bottle', x, 0.015, [
        (-0.125, .105, .105), (-0.105, .137, .137),
        (0.285, .137, .137), (0.355, .105, .105), (0.385, .065, .065)
    ], 12, [4, 1, 2, 1], 4)
    rings('Tank lower band', x, 0.015, [(-.090, .141, .141), (-.048, .141, .141)], 12, [4], 4)
    rings('Tank upper band', x, 0.015, [(.235, .141, .141), (.272, .141, .141)], 12, [4], 4)
    rings('Valve neck', x, 0.015, [(.385, .052, .052), (.415, .052, .052), (.430, .042, .042)], 10, [5, 7], 5)
    # Bold front and rear identification panels, readable from both sides.
    for z in (-.126, .156):
        box('Tank marking', (x, .115, z), (.135, .145, .012), 2, .012)
        box('Tank marking stripe', (x, .115, z + (.008 if z > 0 else -.008)), (.096, .038, .009), 7, .005)

# Central manifold and two substantial connections into the nozzle.
box('Manifold', (0, -.105, 0), (.38, .12, .24), 4, .025)
for x in (-.17, .17):
    box('Feed coupling', (x, -.17, .015), (.095, .19, .14), 5, .014)
rings('Nozzle neck', 0, 0, [(-.21, .125, .11), (-.13, .125, .11)], 12, [4], 4)

# A broad, faceted, three-dimensional exhaust bell with a raised lip.
rings('Flared exhaust bell', 0, 0, [
    (-.395, .335, .155), (-.35, .315, .145),
    (-.22, .19, .115), (-.165, .125, .10)
], 12, [4, 0, 1], 4)
rings('Exhaust rim', 0, 0, [(-.445, .355, .174), (-.395, .355, .174), (-.38, .33, .16)], 12, [4, 2], 4)
# Recessed bottom and a smaller inner outlet give the underside a finished read.
rings('Outlet', 0, 0, [(-.452, .255, .115), (-.445, .255, .115)], 12, [6], 6)
rings('Outlet core', 0, 0, [(-.455, .17, .07), (-.452, .17, .07)], 12, [3], 3)
# Prominent protective ribs on both faces of the bell.
for z in (-.125, .125):
    box('Nozzle badge', (0, -.285, z), (.115, .105, .035), 7, .014)

# Consolidate into the sole export mesh, with applied transforms and modifiers.
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
bpy.ops.object.select_all(action='DESELECT')
model.select_set(True)
bpy.context.view_layer.objects.active = model
bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

# Render setup follows export so the OBJ contains only the model.
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 512
scene.render.resolution_y = 512
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Blue gray studio')
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (.05, .07, .09, 1)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def point_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, loc, energy, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    obj.location = loc
    point_at(obj, (0, 0, 0))
    return obj

sun = light('Upper left sun', 'SUN', (-3, 5, 6), 2.0)
sun.data.angle = math.radians(12)
light('Soft fill', 'AREA', (4, 1, 4), 120, 5)
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
cam.location = (0.85, 0.525, 1.5)
point_at(cam, (0, 0, 0))
cam_data.type = 'PERSP'
cam_data.lens = 48
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

