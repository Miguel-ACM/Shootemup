import bpy
import math
import os
import sys
from mathutils import Vector

# The original pickup is a twelve-sided gold coin with a brass rim and a
# dark dollar sign on each face. All geometry is in the game's Y-up space.
if globals().get('__file__'):
    OUTPUT = os.path.dirname(os.path.abspath(__file__))
else:
    args = sys.argv
    OUTPUT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
    if os.path.isfile(OUTPUT) or OUTPUT.lower().endswith('.py'):
        OUTPUT = os.path.dirname(OUTPUT)
os.makedirs(OUTPUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)


def material(index, color):
    mat = bpy.data.materials.new('tile_%02d' % index)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    shader = mat.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value = (*color, 1.0)
    shader.inputs['Roughness'].default_value = 0.68
    shader.inputs['Metallic'].default_value = 0.0
    return mat

# Palette from the original atlas: coin face, gold, brass and the dark mark.
face = material(0, (230/255, 180/255, 40/255))
gold = material(1, (236/255, 186/255, 40/255))
brass = material(2, (196/255, 150/255, 60/255))
ink = material(3, (110/255, 70/255, 10/255))

# Cross section traverses the back face, rim, edge, and front face. The bevels
# and inset face are integral to the coin, with a finished profile all around.
N = 12
profile = [(0.0, -0.060), (0.405, -0.060), (0.420, -0.075),
           (0.455, -0.075), (0.480, -0.053), (0.480, 0.053),
           (0.455, 0.075), (0.420, 0.075), (0.405, 0.060), (0.0, 0.060)]
vertices = []
for radius, z in profile:
    for i in range(N):
        a = 2.0 * math.pi * i / N + math.pi / 12.0
        vertices.append((radius * math.cos(a), radius * math.sin(a), z))
faces = []
material_ids = []
for j in range(len(profile) - 1):
    for i in range(N):
        k = (i + 1) % N
        faces.append((j*N+i, j*N+k, (j+1)*N+k, (j+1)*N+i))
        material_ids.append([0, 2, 1, 1, 2, 1, 1, 2, 0][j])
mesh = bpy.data.meshes.new('Coin')
mesh.from_pydata(vertices, [], faces)
mesh.materials.clear()
for mat in (face, gold, brass):
    mesh.materials.append(mat)
for poly, idx in zip(mesh.polygons, material_ids):
    poly.material_index = idx
mesh.update()
coin = bpy.data.objects.new('Coin', mesh)
bpy.context.collection.objects.link(coin)
parts = [coin]

# Broad, pixel-inspired strokes reproduce the atlas dollar sign. The reverse
# is mirrored in object coordinates so it reads correctly when viewed from back.
def stroke(a, b, width, z):
    x0, y0 = a
    x1, y1 = b
    dx, dy = x1-x0, y1-y0
    length = math.hypot(dx, dy)
    bpy.ops.mesh.primitive_cube_add(size=1, location=((x0+x1)/2, (y0+y1)/2, z))
    ob = bpy.context.object
    ob.dimensions = (length + width * 0.35, width, 0.012)
    ob.rotation_euler[2] = math.atan2(dy, dx)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    ob.data.materials.append(ink)
    parts.append(ob)

# S shape and the central vertical bar, proportioned to the original tile.
points = [(0.105, 0.190), (-0.045, 0.190), (-0.115, 0.130),
          (-0.115, 0.065), (-0.055, 0.015), (0.065, 0.015),
          (0.115, -0.045), (0.115, -0.110), (0.055, -0.175),
          (-0.105, -0.175)]
for side in (1, -1):
    p = [(x * side, y) for x, y in points]
    for a, b in zip(p, p[1:]):
        stroke(a, b, 0.047, side * 0.066)
    stroke((0, -0.225), (0, 0.235), 0.040, side * 0.067)

bpy.ops.object.select_all(action='DESELECT')
for ob in parts:
    ob.select_set(True)
bpy.context.view_layer.objects.active = coin
bpy.ops.object.join()
coin.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for poly in coin.data.polygons:
    poly.use_smooth = False
# Ensure consistent outward normals and remove the repeated centre vertices.
bpy.ops.object.mode_set(mode='EDIT')
bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.mesh.remove_doubles(threshold=0.00001)
bpy.ops.mesh.normals_make_consistent(inside=False)
bpy.ops.object.mode_set(mode='OBJECT')

bpy.ops.wm.obj_export(filepath=os.path.join(OUTPUT, 'model.obj'),
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
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'


def aim(obj, target=(0, 0, 0)):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

bpy.ops.object.light_add(type='SUN', location=(-3, 4, 5))
sun = bpy.context.object
sun.data.energy = 2.0
sun.data.angle = math.radians(18)
aim(sun)
bpy.ops.object.light_add(type='AREA', location=(4, 1, 4))
fill = bpy.context.object
fill.data.energy = 90
fill.data.shape = 'DISK'
fill.data.size = 5
aim(fill)

bpy.ops.object.camera_add(location=(0, 0, 3))
camera = bpy.context.object
scene.camera = camera
camera.data.type = 'ORTHO'
camera.data.ortho_scale = 1.26
aim(camera)
scene.render.filepath = os.path.join(OUTPUT, 'top.png')
bpy.ops.render.render(write_still=True)

camera.location = (1.55, 0.85, 2.65)
camera.data.type = 'PERSP'
camera.data.lens = 50
aim(camera)
scene.render.filepath = os.path.join(OUTPUT, 'angle.png')
bpy.ops.render.render(write_still=True)
