import bpy
import math
import os
import sys
from mathutils import Vector, Matrix

# The original is a blue rectangular battery, silver top strip, two contacts,
# and a white lightning mark. Dimensions are the original scaled to 0.90 high.
if '__file__' in globals():
    OUT = os.path.dirname(os.path.abspath(__file__))
else:
    args = sys.argv
    if '--' not in args or not args[args.index('--') + 1:]:
        raise RuntimeError('Pass the script path after -- when __file__ is unavailable')
    OUT = os.path.dirname(os.path.abspath(args[args.index('--') + 1]))

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for mat in list(bpy.data.materials):
    bpy.data.materials.remove(mat)

# Atlas source: blue plastic (40,96,230), bolt (240,244,255),
# chrome (210,216,226). These are solid substitutes for the original tiles.
def material(name, rgb):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*rgb, 1)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*rgb, 1)
    bsdf.inputs['Roughness'].default_value = 0.65
    return mat

blue = material('tile_00', (40/255, 96/255, 230/255))
white = material('tile_01', (240/255, 244/255, 1.0))
chrome = material('tile_02', (210/255, 216/255, 226/255))
steel = material('tile_03', (0.58, 0.63, 0.70))

S = 0.90 / 1.01
# Center the original vertical bounds (-0.46, 0.55) around the origin.
def p(x, y, z):
    return (x*S, (y-0.045)*S, z*S)

parts = []
def rounded_box(name, center, size, radius, mat, segments=3):
    bpy.ops.mesh.primitive_cube_add(size=1, location=p(*center))
    ob = bpy.context.object
    ob.name = name
    ob.dimensions = tuple(v*S for v in size)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    ob.data.materials.append(mat)
    mod = ob.modifiers.new('Small molded bevels', 'BEVEL')
    mod.width = radius*S
    mod.segments = segments
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.modifier_apply(modifier=mod.name)
    parts.append(ob)
    return ob

rounded_box('Blue battery case', (0, -0.05, 0), (0.52, 0.82, 0.36), 0.026, blue)
rounded_box('Chrome top', (0, 0.40, 0), (0.52, 0.06, 0.36), 0.012, chrome, 2)

# Pair of vertical hexagonal chrome terminals, just as on the original.
for x in (-0.13, 0.13):
    bpy.ops.mesh.primitive_cylinder_add(vertices=6, radius=0.06*S, depth=0.12*S,
                                      end_fill_type='NGON', location=p(x, 0.49, 0),
                                      rotation=(math.pi/2, 0, 0))
    ob = bpy.context.object
    ob.name = 'Chrome terminal'
    ob.data.materials.append(chrome)
    ob.data.materials.append(steel)
    # The upper contact end is steel.
    for poly in ob.data.polygons:
        if poly.normal.z < -0.9:
            poly.material_index = 1
    bevel = ob.modifiers.new('Contact edge', 'BEVEL')
    bevel.width = 0.006*S
    bevel.segments = 2
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    parts.append(ob)

# The atlas bolt is pale white on blue. A shallow solid emblem replaces the
# texture while keeping the same compact central lightning shape.
outline = [(0.065, 0.285), (-0.105, -0.055), (-0.005, -0.055),
           (-0.065, -0.355), (0.105, -0.015), (0.005, -0.015)]
# Face is explicitly triangulated to handle the concave lightning outline.
verts = [p(x, y, z) for z in (0.179, 0.187) for x, y in outline]
n = len(outline)
faces = [tuple(reversed(range(n))), tuple(range(n, 2*n))]
faces += [(i, (i+1)%n, (i+1)%n+n, i+n) for i in range(n)]
mesh = bpy.data.meshes.new('Bolt geometry')
mesh.from_pydata(verts, [], faces)
mesh.update()
ob = bpy.data.objects.new('White bolt', mesh)
bpy.context.collection.objects.link(ob)
ob.data.materials.append(white)
parts.append(ob)

# Consolidate the export mesh, triangulate, and ensure transforms are identity.
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
scene.world = bpy.data.worlds.new('Blue gray world')
scene.world.use_nodes = True
bg = scene.world.node_tree.nodes.get('Background')
bg.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
bg.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(ob, target):
    # Local camera/light -Z points at target; local Y follows world +Y.
    back = (ob.location - Vector(target)).normalized()
    right = Vector((0, 1, 0)).cross(back).normalized()
    up = back.cross(right).normalized()
    ob.rotation_euler = Matrix(((right.x, up.x, back.x),
                                (right.y, up.y, back.y),
                                (right.z, up.z, back.z))).to_quaternion().to_euler()

def light(name, kind, location, energy, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    obj.location = location
    aim(obj, (0, 0, 0))

light('Upper left sun', 'SUN', (-3, 5, 6), 2.0)
light('Soft fill', 'AREA', (4, 1, 5), 90, 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 4)
aim(cam, (0, 0, 0))
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.29
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (0.95, 0.55, 1.65)
aim(cam, (0, 0, 0))
cam_data.type = 'PERSP'
cam_data.lens = 55
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

