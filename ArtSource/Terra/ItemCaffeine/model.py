import bpy
import math
import os
import sys
from mathutils import Vector

# The source item is a white mug with a brown coffee surface, a right-hand
# handle and two vertical white steam bars. Model it in its original proportions,
# then uniformly fit and center the complete silhouette.
if '__file__' in globals():
    HERE = os.path.dirname(os.path.abspath(__file__))
else:
    args = sys.argv
    if '--' not in args or args.index('--') + 1 >= len(args):
        raise RuntimeError('Pass the script path after -- when __file__ is unavailable')
    HERE = os.path.dirname(os.path.abspath(args[args.index('--') + 1]))

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for material in list(bpy.data.materials):
    bpy.data.materials.remove(material)

def material(name, color):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.7
    return mat

ceramic = material('tile_00', (0.86, 0.86, 0.81))
coffee = material('tile_01', (0.35, 0.18, 0.08))
parts = []

def mesh_object(name, verts, faces, mat):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    parts.append(obj)
    return obj

# Radial cross-section: rounded foot, subtly tapered walls, rolled lip and
# a short inner wall. Axis is Y, as in the game's item coordinates.
N = 16
profile = [(-0.32, 0.245), (-0.315, 0.270), (-0.292, 0.289),
           (-0.26, 0.294), (0.205, 0.300), (0.255, 0.300),
           (0.275, 0.292), (0.282, 0.278), (0.275, 0.263),
           (0.257, 0.255), (0.215, 0.255)]
verts = []
for y, radius in profile:
    for i in range(N):
        a = 2 * math.pi * i / N
        verts.append((radius * math.cos(a), y, radius * math.sin(a)))
faces = []
for j in range(len(profile) - 1):
    for i in range(N):
        k = (i + 1) % N
        faces.append((j*N+i, (j+1)*N+i, (j+1)*N+k, j*N+k))
faces.append(tuple(reversed(range(N))))
mesh_object('Mug', verts, faces, ceramic)

# Recessed, opaque coffee surface inside the lip.
verts = [(0, 0.235, 0)] + [(0.256*math.cos(2*math.pi*i/N), 0.235,
                                  0.256*math.sin(2*math.pi*i/N)) for i in range(N)]
faces = [(0, 1+(i+1)%N, 1+i) for i in range(N)]
mesh_object('Coffee', verts, faces, coffee)

# One continuous chunky handle; the cross-section follows a rounded rectangular
# path in the XY plane, with genuine depth on both faces.
path = [(0.265, 0.16), (0.34, 0.16), (0.415, 0.16), (0.455, 0.135),
        (0.46, 0.09), (0.46, -0.11), (0.455, -0.155),
        (0.415, -0.18), (0.34, -0.18), (0.265, -0.18)]
segments = 8
verts = []
for i, (x, y) in enumerate(path):
    if i == 0:
        tangent = Vector((path[1][0]-x, path[1][1]-y))
    elif i == len(path)-1:
        tangent = Vector((x-path[i-1][0], y-path[i-1][1]))
    else:
        tangent = Vector((path[i+1][0]-path[i-1][0], path[i+1][1]-path[i-1][1]))
    tangent.normalize()
    normal = Vector((-tangent.y, tangent.x))
    for j in range(segments):
        a = 2*math.pi*j/segments
        verts.append((x + 0.05*math.cos(a)*normal.x,
                      y + 0.05*math.cos(a)*normal.y, 0.05*math.sin(a)))
faces = []
for i in range(len(path)-1):
    for j in range(segments):
        k = (j+1) % segments
        faces.append((i*segments+j, i*segments+k, (i+1)*segments+k, (i+1)*segments+j))
faces.extend([tuple(reversed(range(segments))),
              tuple((len(path)-1)*segments+j for j in range(segments))])
mesh_object('Handle', verts, faces, ceramic)

# Straight steam strokes retain the shipped graphic, with small bevels so their
# depth remains legible when the pickup rotates.
def steam_bar(x, height):
    bpy.ops.mesh.primitive_cube_add(size=1, location=(x, 0.30 + height/2, 0))
    obj = bpy.context.object
    obj.name = 'Steam'
    obj.dimensions = (0.04, height, 0.04)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(ceramic)
    bevel = obj.modifiers.new('Soft edges', 'BEVEL')
    bevel.width = 0.006
    bevel.segments = 1
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    parts.append(obj)

steam_bar(-0.08, 0.50)
steam_bar(0.08, 0.44)

bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
# Source bounds are x [-.30,.51], y [-.32,.80].
for v in model.data.vertices:
    v.co.x = (v.co.x - 0.105) * 0.8
    v.co.y = (v.co.y - 0.24) * 0.8
    v.co.z *= 0.8
for p in model.data.polygons:
    p.use_smooth = False
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

bpy.ops.wm.obj_export(filepath=os.path.join(HERE, 'model.obj'),
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
scene.world = bpy.data.worlds.new('Blue gray world')
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(obj, target=(0, 0, 0)):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

bpy.ops.object.light_add(type='SUN', location=(-3, 5, 6))
sun = bpy.context.object
sun.data.energy = 2.0
sun.data.angle = math.radians(12)
aim(sun)
bpy.ops.object.light_add(type='AREA', location=(3, 1, 4))
fill = bpy.context.object
fill.data.energy = 100
fill.data.shape = 'DISK'
fill.data.size = 5
aim(fill)

bpy.ops.object.camera_add(location=(0, 0, 4))
cam = bpy.context.object
scene.camera = cam
cam.data.type = 'ORTHO'
cam.data.ortho_scale = 1.29
aim(cam)
scene.render.filepath = os.path.join(HERE, 'top.png')
bpy.ops.render.render(write_still=True)

cam.data.type = 'PERSP'
cam.data.lens = 50
cam.location = (1.65, 1.25, 2.8)
aim(cam)
scene.render.filepath = os.path.join(HERE, 'angle.png')
bpy.ops.render.render(write_still=True)
