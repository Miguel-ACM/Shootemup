import bpy
import math
import os
import sys
from mathutils import Vector

# This model uses Y as its upright axis and +Z as its front.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
    if os.path.isfile(OUT) or OUT.lower().endswith('.py'):
        OUT = os.path.dirname(OUT)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
    for block in list(datablocks):
        if block.users == 0:
            datablocks.remove(block)

palette = [
    (0.95, 0.31, 0.065, 1),  # orange case
    (1.00, 0.48, 0.11, 1),   # orange shoulder
    (0.66, 0.73, 0.74, 1),   # brushed metal
    (0.40, 0.48, 0.49, 1),   # wheel
    (0.94, 0.98, 0.91, 1),   # metal highlight
    (1.00, 0.54, 0.055, 1),  # flame outer
    (1.00, 0.82, 0.22, 1),   # flame inner
    (0.27, 0.33, 0.34, 1),   # small recess
]
mats = []
for i, color in enumerate(palette):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = color
    m.use_nodes = True
    m.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value = color
    m.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value = 0.72
    mats.append(m)

parts = []
def finish(obj, mat):
    obj.data.materials.append(mats[mat])
    for face in obj.data.polygons:
        face.use_smooth = False
    parts.append(obj)
    return obj

def box(name, loc, size, bevel, mat):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    o = bpy.context.object
    o.name = name
    o.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = o.modifiers.new('Broad faceted corners', 'BEVEL')
        mod.width = bevel
        mod.segments = 3
        bpy.context.view_layer.objects.active = o
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return finish(o, mat)

def cylinder(name, loc, radius, depth, mat, vertices=12, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, end_fill_type='NGON', location=loc, rotation=rotation)
    o = bpy.context.object
    o.name = name
    return finish(o, mat)

def flame(name, rings, mat, sides=10):
    # Each ring is (y, x centre, z centre, x radius, z radius).
    verts = []
    for y, x, z, rx, rz in rings:
        for i in range(sides):
            a = 2 * math.pi * i / sides
            verts.append((x + rx * math.cos(a), y, z + rz * math.sin(a)))
    faces = [tuple(reversed(range(sides)))]
    for j in range(len(rings) - 1):
        for i in range(sides):
            a, b = j * sides + i, j * sides + (i + 1) % sides
            c, d = (j + 1) * sides + (i + 1) % sides, (j + 1) * sides + i
            faces.append((a, b, c, d))
    faces.append(tuple((len(rings) - 1) * sides + i for i in range(sides)))
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return finish(obj, mat)

# Rounded rectangular fuel reservoir, orange from every side.
box('Fuel reservoir', (0, -0.165, 0), (0.43, 0.49, 0.245), 0.055, 0)
box('Shoulder', (0, 0.075, 0), (0.42, 0.080, 0.24), 0.025, 1)
# A substantial metal hood and its raised rim read as a familiar disposable lighter.
box('Metal hood', (0, 0.158, 0), (0.43, 0.125, 0.255), 0.027, 2)
box('Hood lip', (0, 0.220, 0), (0.40, 0.035, 0.240), 0.012, 4)
# Striker wheel runs front to back, visible from both faces.
cylinder('Striker wheel', (-0.112, 0.245, 0), 0.074, 0.275, 3, 16)
cylinder('Front wheel hub', (-0.112, 0.245, 0.142), 0.030, 0.012, 2, 12)
cylinder('Rear wheel hub', (-0.112, 0.245, -0.142), 0.030, 0.012, 2, 12)
# Flame nozzle oriented along Y.
cylinder('Nozzle collar', (0.087, 0.244, 0), 0.055, 0.033, 3, 12, (math.pi / 2, 0, 0))
cylinder('Nozzle', (0.087, 0.265, 0), 0.033, 0.035, 4, 12, (math.pi / 2, 0, 0))
# Thick, asymmetrical flame, with a warm yellow core visible on both sides.
flame('Outer flame', [
    (0.276, 0.087, 0, 0.037, 0.038),
    (0.318, 0.087, 0, 0.068, 0.053),
    (0.367, 0.093, 0, 0.061, 0.046),
    (0.415, 0.115, 0, 0.042, 0.032),
    (0.462, 0.145, 0, 0.004, 0.004),
], 5)
flame('Golden flame heart', [
    (0.282, 0.087, 0, 0.025, 0.040),
    (0.315, 0.087, 0, 0.037, 0.055),
    (0.350, 0.095, 0, 0.026, 0.049),
    (0.389, 0.108, 0, 0.003, 0.004),
], 6)

# Join only model geometry, bake all transforms, and ensure flat normals.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for face in model.data.polygons:
    face.use_smooth = False

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
scene.world = bpy.data.worlds.new('Blue gray studio') if scene.world is None else scene.world
scene.world.use_nodes = True
bg = scene.world.node_tree.nodes.get('Background')
bg.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
bg.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'AgX'

def aim(obj, target=(0, 0, 0)):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, loc, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = loc
    aim(obj, target)
    return obj

light('Upper left sun', 'SUN', (-3, 5, 6), 2.0, (0, 0, 0))
light('Soft fill', 'AREA', (4, 1, 4), 100, (0, 0, 0), 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 4)
aim(cam)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.12
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam.location = (1.3, 0.8, 2.2)
aim(cam, (0, 0, 0))
cam_data.lens = 48
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)
