import bpy
import math
import os
import sys
from mathutils import Vector, Matrix

# Twerp: the shipped ceramic-headed, steel-bodied helper robot.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
    if not os.path.isdir(OUT):
        OUT = os.path.dirname(OUT)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

colors = [
    (0.80, 0.79, 0.75, 1),  # ceramic head
    (0.50, 0.53, 0.56, 1),  # steel body and head sides
    (0.25, 0.27, 0.30, 1),  # dark steel arms and legs
    (0.23, 0.83, 0.98, 1),  # cyan eyes and antenna
    (0.035, 0.12, 0.15, 1), # chest display
    (0.65, 0.69, 0.72, 1),  # chrome antenna
]
mats = []
for i, color in enumerate(colors):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = color
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = 0.78
    mats.append(m)

parts = []
def box(name, center, size, material, bevel=0.015, front=None):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    ob = bpy.context.object
    ob.name = name
    ob.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    for m in mats:
        ob.data.materials.append(m)
    for face in ob.data.polygons:
        face.material_index = front if front is not None and face.normal.z > 0.9 else material
    mod = ob.modifiers.new('Soft machined edges', 'BEVEL')
    mod.width = bevel
    mod.segments = 1
    mod.material = material
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.modifier_apply(modifier=mod.name)
    parts.append(ob)
    return ob

def cylinder(name, center, radius, depth, material, vertices=8):
    # Blender cylinders start along Z; rotate so the stem runs along item Y.
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth,
                                       location=center, rotation=(math.pi / 2, 0, 0))
    ob = bpy.context.object
    ob.name = name
    ob.data.materials.append(mats[material])
    parts.append(ob)
    return ob

# Original proportions and placement, with broad single-facet bevels.
box('Head', (0, .24, 0), (.52, .38, .40), 1, .035, front=0)
for x in (-.11, .11):
    box('Cyan eye', (x, .26, .215), (.10, .08, .05), 3, .009)
box('Torso', (0, -.13, 0), (.58, .38, .34), 2, .035, front=1)
box('Chest screen', (0, -.12, .183), (.24, .16, .035), 4, .012)
# The atlas screen has cyan horizontal traces on a dark field.
for y, width in [(-.075, .18), (-.12, .18), (-.16, .115)]:
    box('Display trace', (-.025 if width < .15 else 0, y, .204), (width, .012, .008), 3, .002)
for x in (-.15, .15):
    box('Leg', (x, -.42, 0), (.12, .22, .20), 2, .018)
for x in (-.36, .36):
    box('Arm', (x, -.12, 0), (.12, .30, .16), 2, .018)
cylinder('Antenna', (0, .515, 0), .02, .17, 5, 8)
bpy.ops.mesh.primitive_uv_sphere_add(segments=8, ring_count=4, radius=.05, location=(0, .62, 0))
ob = bpy.context.object
ob.name = 'Antenna tip'
ob.data.materials.append(mats[3])
parts.append(ob)

# Uniformly scale the original 1.20-unit robot to 0.96 units tall and center it.
bpy.ops.object.select_all(action='DESELECT')
for ob in parts:
    ob.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
for v in model.data.vertices:
    # Joined geometry is in the active object's local coordinate system.
    p = model.matrix_world @ v.co
    v.co = (p.x * .8, (p.y - .075) * .8, p.z * .8)
model.matrix_world = Matrix.Identity(4)
for face in model.data.polygons:
    face.use_smooth = False
bpy.context.view_layer.objects.active = model
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
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
bg = scene.world.node_tree.nodes.get('Background')
bg.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
bg.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def point_at(ob, target=(0, 0, 0)):
    ob.rotation_euler = (Vector(target) - ob.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, location, energy, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if kind == 'AREA':
        data.shape = 'DISK'
        data.size = size
    ob = bpy.data.objects.new(name, data)
    scene.collection.objects.link(ob)
    ob.location = location
    point_at(ob)
    return ob

sun = light('Upper left sun', 'SUN', (-3, 5, 6), 2.0)
sun.data.angle = math.radians(12)
light('Soft fill', 'AREA', (4, 1, 5), 180, 5)
camdata = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', camdata)
scene.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 4)
point_at(cam)
camdata.type = 'ORTHO'
camdata.ortho_scale = 1.38
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (0.95, 0.65, 1.7)
point_at(cam)
camdata.type = 'PERSP'
camdata.lens = 50
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

