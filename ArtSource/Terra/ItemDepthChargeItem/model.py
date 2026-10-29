import bpy
import math
import os
import sys
from mathutils import Vector

# The game uses Y as vertical and looks at the pickup from +Z.
if '__file__' in globals():
    OUT_DIR = os.path.dirname(os.path.abspath(__file__))
else:
    args = sys.argv
    if '--' not in args or not args[args.index('--') + 1:]:
        raise RuntimeError('Pass the script path or output directory after --')
    supplied = os.path.abspath(args[args.index('--') + 1])
    OUT_DIR = supplied if os.path.isdir(supplied) else os.path.dirname(supplied)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablock in bpy.data.materials:
    bpy.data.materials.remove(datablock)


def material(index, color):
    mat = bpy.data.materials.new('tile_%02d' % index)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.65
    bsdf.inputs['Metallic'].default_value = 0.0
    return mat

blue = material(0, (0.12, 0.40, 0.82))
blue_light = material(1, (0.25, 0.63, 0.98))
rack = material(2, (0.30, 0.46, 0.57))
rack_light = material(3, (0.55, 0.70, 0.74))
gold = material(4, (1.00, 0.66, 0.12))
cream = material(5, (0.93, 0.86, 0.60))
parts = []


def bevel_box(name, location, size, mat, bevel=0.014):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    if bevel:
        mod = obj.modifiers.new('Machined edges', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    parts.append(obj)
    return obj


def drum(name, y, radius, height, mat):
    # Blender cylinders start on Z; rotate them to the game's vertical Y.
    bpy.ops.mesh.primitive_cylinder_add(vertices=10, radius=radius, depth=height,
                                       end_fill_type='NGON', location=(0, y, 0),
                                       rotation=(math.pi / 2, 0, 0))
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    parts.append(obj)
    return obj

# Deep, bevelled frame and two downward-projecting release feet.
for side in (-1, 1):
    bevel_box('Rack upright', (side * 0.34, -0.035, 0), (0.105, 0.76, 0.24), rack, 0.018)
    bevel_box('Release foot', (side * 0.34, -0.407, 0), (0.135, 0.075, 0.30), rack_light, 0.014)
    # Chunky gold latches make the rack legible from either side of the spin.
    for z in (-0.128, 0.128):
        bevel_box('Latch', (side * 0.34, -0.28, z), (0.075, 0.095, 0.035), gold, 0.008)
bevel_box('Upper crossbar', (0, 0.36, 0), (0.79, 0.115, 0.27), rack_light, 0.018)
bevel_box('Crossbar inset', (0, 0.36, 0.141), (0.35, 0.058, 0.025), blue_light, 0.008)
bevel_box('Crossbar inset back', (0, 0.36, -0.141), (0.35, 0.058, 0.025), blue_light, 0.008)

# Three individually readable shells, with broad bands visible from every angle.
for i, y in enumerate((0.205, -0.015, -0.235)):
    drum('Blue shell %d' % i, y, 0.146, 0.18, blue)
    drum('Shell middle band %d' % i, y, 0.151, 0.046, blue_light)
    for dy in (-0.075, 0.075):
        drum('Shell end rim', y + dy, 0.153, 0.030, gold)
    # Large inset caps give the cylinders a finished end when viewed from above.
    for dy in (-0.092, 0.092):
        drum('Shell cap', y + dy, 0.112, 0.012, cream)

# Join and triangulate before export; leave the origin at the item centre.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
tri = model.modifiers.new('Triangulate', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
for poly in model.data.polygons:
    poly.use_smooth = False
bpy.ops.wm.obj_export(filepath=os.path.join(OUT_DIR, 'model.obj'),
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
scene.world = bpy.data.worlds.new('Blue gray studio')
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'


def aim(obj, point=(0, 0, 0)):
    obj.rotation_euler = (Vector(point) - obj.location).to_track_quat('-Z', 'Y').to_euler()


def light(name, kind, position, energy, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    obj.location = position
    aim(obj)
    return obj

light('Upper left sun', 'SUN', (-3, 5, 4), 2.0)
light('Soft fill', 'AREA', (3, 1, 4), 80, 5.0)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 4)
aim(cam)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.06
scene.render.filepath = os.path.join(OUT_DIR, 'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (2.6, 1.65, 3.8)
aim(cam)
cam_data.type = 'PERSP'
cam_data.lens = 50
# Perspective field of view frames the 0.9-unit pickup at about 70% height.
cam.location = (1.65, 1.05, 2.42)
aim(cam)
scene.render.filepath = os.path.join(OUT_DIR, 'angle.png')
bpy.ops.render.render(write_still=True)
