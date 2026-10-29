import bpy
import math
import os
import sys
from mathutils import Vector

# Blender's --python usually defines __file__; the argument after -- is a fallback.
try:
    SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    SCRIPT_DIR = os.path.dirname(os.path.abspath(args[args.index('--') + 1])) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)


def material(name, color):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.diffuse_color = (*color, 1.0)
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.76
    return mat

# The shipped atlas uses an off-white face with a red cross, red sides,
# and a black plastic handle.
red = material('tile_00', (0.70, 0.035, 0.035))
white = material('tile_01', (0.88, 0.86, 0.81))
cross_red = material('tile_02', (0.78, 0.045, 0.045))
handle = material('tile_03', (0.085, 0.09, 0.10))
parts = []


def bevel_box(name, location, dimensions, mat, radius, segments=3):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    bevel = obj.modifiers.new('Molded edges', 'BEVEL')
    bevel.width = radius
    bevel.segments = segments
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    parts.append(obj)
    return obj

# Main case retains the original 0.92 x 0.66 x 0.40 proportions.
bevel_box('Case', (0, -0.06, 0), (0.92, 0.66, 0.40), red, 0.045)
# The light-colored face replaces the original atlas tile with real geometry.
bevel_box('Face', (0, -0.06, 0.198), (0.872, 0.612, 0.018), white, 0.018)

# A single raised, bevelled plus, matching the red cross on the shipped tile.
def make_cross():
    a, x, y = 0.075, 0.325, 0.235
    outline = [(-a,-y),(a,-y),(a,-a),(x,-a),(x,a),(a,a),
               (a,y),(-a,y),(-a,a),(-x,a),(-x,-a),(-a,-a)]
    z0, z1 = 0.205, 0.218
    verts = [(px, py - 0.06, z) for z in (z0, z1) for px, py in outline]
    n = len(outline)
    faces = [tuple(reversed(range(n))), tuple(range(n, 2*n))]
    faces += [(i, (i+1)%n, (i+1)%n+n, i+n) for i in range(n)]
    mesh = bpy.data.meshes.new('Cross mesh')
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new('Cross', mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(cross_red)
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bevel = obj.modifiers.new('Soft emblem edge', 'BEVEL')
    bevel.width = 0.004
    bevel.segments = 2
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    parts.append(obj)

make_cross()
# Three pieces form the same open, squared carrying handle as the original.
for x in (-0.18, 0.18):
    bevel_box('Handle support', (x, 0.285, 0), (0.08, 0.12, 0.14), handle, 0.012)
bevel_box('Handle grip', (0, 0.34, 0), (0.44, 0.10, 0.14), handle, 0.022)

bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for polygon in model.data.polygons:
    polygon.use_smooth = False
triangulate = model.modifiers.new('Export triangles', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=triangulate.name)
assert 300 <= len(model.data.polygons) <= 2000, 'Triangle budget exceeded'

bpy.ops.wm.obj_export(filepath=os.path.join(SCRIPT_DIR, 'model.obj'),
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
scene.world = bpy.data.worlds.new('Blue gray world')
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'


def aim(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()


def light(name, kind, location, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.size = size
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    obj.location = location
    aim(obj, target)
    return obj

sun = light('Upper left sun', 'SUN', (-3, 5, 6), 2.0, (0, 0, 0))
sun.data.angle = math.radians(12)
light('Soft fill', 'AREA', (4, 1, 5), 130, (0, 0, 0), 5.0)

cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 4)
aim(cam, (0, 0, 0))
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.09
scene.render.filepath = os.path.join(SCRIPT_DIR, 'top.png')
bpy.ops.render.render(write_still=True)

cam.location = (0.85, 0.55, 1.5)
aim(cam, (0, 0, 0))
cam_data.type = 'PERSP'
cam_data.lens = 50
scene.render.filepath = os.path.join(SCRIPT_DIR, 'angle.png')
bpy.ops.render.render(write_still=True)

