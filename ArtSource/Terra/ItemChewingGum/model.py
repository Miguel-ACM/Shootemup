import bpy
import math
import os
import sys
from mathutils import Vector

# The exported model uses X right, Y up, and Z toward the viewer.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.dirname(os.path.abspath(args[args.index('--') + 1])) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for block in bpy.data.materials:
    bpy.data.materials.remove(block)

palette = [
    (0.90, 0.25, 0.49, 1),  # strawberry wrapper
    (1.00, 0.69, 0.76, 1),  # bubble gum
    (1.00, 0.91, 0.78, 1),  # paper ends and badge
    (0.72, 0.12, 0.31, 1),  # raspberry folds
    (1.00, 0.49, 0.64, 1),  # light pink
    (0.98, 0.79, 0.58, 1),  # warm paper fold
]
mats = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = color
    mat.use_nodes = True
    mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value = color
    mat.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value = 0.8
    mats.append(mat)

parts = []
def finish(obj, mat):
    obj.data.materials.append(mats[mat])
    for poly in obj.data.polygons:
        poly.use_smooth = False
    parts.append(obj)
    return obj

def box(loc, size, mat, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = obj.modifiers.new('Broad wrapper bevel', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return finish(obj, mat)

def disc(loc, radius, depth, mat, vertices=12):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=(math.pi / 2, 0, 0))
    return finish(bpy.context.object, mat)

def ball(loc, scale, mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=8, radius=1, location=loc)
    obj = bpy.context.object
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(obj, mat)

# A chunky, bevelled wrapped stick, with substantial paper seals at both ends.
box((0, -0.075, 0), (0.72, 0.42, 0.22), 0, 0.045)
for side in (-1, 1):
    box((side * 0.365, -0.075, 0), (0.12, 0.43, 0.235), 2, 0.025)
    # Broad, readable crimp ridges on the front and back of each seal.
    for zside in (-1, 1):
        for y in (-0.19, -0.075, 0.04):
            box((side * 0.37, y, zside * 0.121), (0.084, 0.022, 0.014), 5, 0.006)

# Framed round gum emblem and its large pink center on both faces.
for side in (-1, 1):
    disc((0, -0.075, side * 0.116), 0.151, 0.025, 3)
    disc((0, -0.075, side * 0.133), 0.124, 0.016, 2)
    disc((0, -0.075, side * 0.144), 0.083, 0.012, 4)
    # A single chunky diagonal glint, designed to survive small icon sizes.
    glint = box((-0.025, -0.048, side * 0.154), (0.064, 0.020, 0.012), 2, 0.006)
    glint.rotation_euler[2] = math.radians(35)

# The raised bubble overlaps the wrapper and reads in silhouette from all sides.
ball((0.215, 0.205, 0), (0.155, 0.155, 0.145), 1)
ball((0.17, 0.257, 0.126), (0.038, 0.029, 0.018), 2)
ball((0.17, 0.257, -0.126), (0.038, 0.029, 0.018), 2)

# Consolidate geometry and bake transforms for a single clean export object.
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

# Preview setup is created only after exporting the selected mesh.
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 512
scene.render.resolution_y = 512
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Blue gray studio') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0

def point_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, loc, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    obj.location = loc
    point_at(obj, target)
    return obj

light('Upper left sun', 'SUN', (-3, 5, 4), 2.0, (0, 0, 0))
light('Soft fill', 'AREA', (4, 1, 4), 90, (0, 0, 0), 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.20
cam.location = (0, 0, 4)
point_at(cam, (0, 0, 0))
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)

cam_data.type = 'PERSP'
cam_data.lens = 50
cam.location = (0.85, 0.60, 1.50)
point_at(cam, (0, 0, 0))
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

