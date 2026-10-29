import bpy
import math
import os
import sys
from mathutils import Vector

# The pickup is built upright along Y, with its front facing +Z.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for material in list(bpy.data.materials):
    bpy.data.materials.remove(material)

palette = [
    ('tile_00', (0.53, 0.28, 0.76, 1)),  # violet housing
    ('tile_01', (0.76, 0.47, 0.96, 1)),  # signature lavender
    ('tile_02', (0.87, 0.72, 1.00, 1)),  # pale barrel and needle
    ('tile_03', (0.35, 0.20, 0.53, 1)),  # inset violet
    ('tile_04', (0.43, 0.79, 0.86, 1)),  # cool medicine window
    ('tile_05', (0.93, 0.85, 1.00, 1)),  # markings
    ('tile_06', (0.98, 0.60, 0.31, 1)),  # warm plunger accent
]
mats = []
for name, rgba in palette:
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = rgba
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = rgba
    bsdf.inputs['Roughness'].default_value = 0.72
    mats.append(mat)

parts = []
def finish(obj, material):
    obj.data.materials.append(mats[material])
    for poly in obj.data.polygons:
        poly.use_smooth = False
    parts.append(obj)
    return obj

def bevel_box(name, center, size, material, bevel=0.015):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    mod = obj.modifiers.new('Broad machined edges', 'BEVEL')
    mod.width = bevel
    mod.segments = 1
    mod.affect = 'EDGES'
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=mod.name)
    return finish(obj, material)

def cylinder(name, y, depth, radius, material, vertices=12, radius_top=None):
    # Blender cones are Z-aligned; rotate them to point along the item axis.
    bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=radius,
        radius2=radius if radius_top is None else radius_top,
        depth=depth, location=(0, y, 0), rotation=(-math.pi / 2, 0, 0))
    obj = bpy.context.object
    obj.name = name
    return finish(obj, material)

# The broad barrel, stepped collars, and a recessed-looking fluid core make
# the syringe read as a substantial object from every side.
cylinder('Barrel lower rim', -0.220, 0.055, 0.122, 0)
cylinder('Lavender barrel', -0.050, 0.300, 0.108, 1)
cylinder('Barrel shoulder', 0.115, 0.055, 0.122, 0)
cylinder('Tapered nozzle', 0.165, 0.065, 0.101, 2, radius_top=0.052)
cylinder('Needle socket', 0.208, 0.035, 0.053, 3)
cylinder('Needle', 0.307, 0.165, 0.023, 2, vertices=8, radius_top=0.016)
cylinder('Needle point', 0.416, 0.055, 0.016, 5, vertices=8, radius_top=0.0)

# Four broad inset panels wrap the barrel so the spinning pickup remains legible.
for angle in (0, math.pi / 2, math.pi, 3 * math.pi / 2):
    x, z = math.sin(angle) * 0.106, math.cos(angle) * 0.106
    panel = bevel_box('Fluid window', (x, -0.048, z), (0.082, 0.205, 0.018), 4, 0.008)
    panel.rotation_euler[1] = angle
    # Two large graduation marks rather than fine print.
    for y in (-0.09, 0.0):
        mark = bevel_box('Graduation', (math.sin(angle) * 0.117, y, math.cos(angle) * 0.117),
                         (0.052, 0.012, 0.009), 5, 0.003)
        mark.rotation_euler[1] = angle

# Finger flange and rear plunger preserve the original syringe silhouette.
bevel_box('Finger wings', (0, -0.235, 0), (0.48, 0.065, 0.18), 1, 0.026)
for x in (-0.196, 0.196):
    bevel_box('Wing end', (x, -0.235, 0), (0.075, 0.075, 0.19), 0, 0.018)
cylinder('Plunger stem', -0.320, 0.145, 0.041, 2, vertices=8)
bevel_box('Plunger pad', (0, -0.414, 0), (0.245, 0.065, 0.155), 0, 0.022)
bevel_box('Plunger pad inset', (0, -0.450, 0), (0.165, 0.012, 0.105), 6, 0.006)

# Join and bake every transform and modifier, including triangulation.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
tri = model.modifiers.new('Export triangles', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
for poly in model.data.polygons:
    poly.use_smooth = False

bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'),
    export_selected_objects=True, export_materials=True,
    forward_axis='Y', up_axis='Z')

# Preview setup: the game itself uses only the exported mesh and base colors.
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
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(obj, target=(0, 0, 0)):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, location, energy, size=1):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if kind == 'AREA':
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    obj.location = location
    aim(obj)
    return obj

light('Upper left sun', 'SUN', (-3, 5, 6), 2.0)
light('Soft fill', 'AREA', (4, 1, 4), 110, 5)
cam_data = bpy.data.cameras.new('Preview camera')
cam = bpy.data.objects.new('Preview camera', cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 3)
aim(cam)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.29
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam_data.lens = 50
cam.location = (0.70, 0.52, 1.45)
aim(cam)
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

