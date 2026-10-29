import bpy
import bmesh
import math
import os
import sys
from mathutils import Vector

# Blender background usage: blender -b -P model.py -- <output directory>
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
    for block in list(datablocks):
        if block.users == 0:
            datablocks.remove(block)

def material(name, color, metallic=0.0):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1.0)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Metallic'].default_value = metallic
    bsdf.inputs['Roughness'].default_value = 0.72
    return m

# The shipped ceramic, chrome and steel atlas colors; its cap is chrome,
# despite the older descriptive comment calling it gold.
fur = material('tile_00', (0.925, 0.910, 0.871))
chrome = material('tile_01', (0.824, 0.847, 0.886), 0.12)
steel = material('tile_02', (0.502, 0.525, 0.557), 0.08)
parts = []

def loft(name, rings, segments, mat, caps=True):
    # Horizontal XZ rings along the vertical Y axis. Each ring is
    # (y, x-radius, z-radius); small bevel rings make a solid rounded form.
    vertices = []
    for y, rx, rz in rings:
        for i in range(segments):
            a = 2 * math.pi * i / segments
            vertices.append((rx * math.cos(a), y, rz * math.sin(a)))
    faces = []
    for j in range(len(rings) - 1):
        for i in range(segments):
            ni = (i + 1) % segments
            faces.append((j * segments + i, (j + 1) * segments + i,
                          (j + 1) * segments + ni, j * segments + ni))
    if caps:
        faces.append(tuple(range(segments)))
        faces.append(tuple((len(rings) - 1) * segments + i for i in reversed(range(segments))))
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    ob = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(ob)
    ob.data.materials.append(mat)
    parts.append(ob)
    return ob

# Broad, tapered white foot: a real rounded cross section all the way around.
# Heights and widths preserve the original's tall, narrow paw silhouette.
loft('Paw', [(-0.333, 0.055, 0.065), (-0.300, 0.135, 0.105),
             (-0.245, 0.166, 0.130), (-0.120, 0.157, 0.139),
             (0.080, 0.147, 0.132), (0.195, 0.140, 0.119),
             (0.290, 0.111, 0.101), (0.328, 0.082, 0.080)], 12, fur)

# Three rounded toe lobes, matching the original central low toe and side toes.
for x, y in [(-0.107, -0.326), (0.0, -0.375), (0.107, -0.326)]:
    bpy.ops.mesh.primitive_uv_sphere_add(segments=10, ring_count=6, location=(x, y, 0.015))
    ob = bpy.context.object
    ob.name = 'Toe'
    ob.scale = (0.074, 0.074, 0.074)
    ob.data.materials.append(fur)
    parts.append(ob)

# Faceted chrome ferrule, with restrained bevels and the steel end face.
cap = loft('Cap', [(0.302, 0.095, 0.095), (0.312, 0.107, 0.107),
                   (0.430, 0.107, 0.107), (0.438, 0.098, 0.098)], 12, chrome)
cap.data.materials.append(steel)
cap.data.polygons[-1].material_index = 1

# Join and bake, including triangulation, with no smooth shading.
bpy.ops.object.select_all(action='DESELECT')
for ob in parts:
    ob.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
bm = bmesh.new()
bm.from_mesh(model.data)
bmesh.ops.triangulate(bm, faces=list(bm.faces))
bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
bm.to_mesh(model.data)
bm.free()
model.data.update()
for face in model.data.polygons:
    face.use_smooth = False

bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'),
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
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    obj.location = location
    aim(obj, target)

light('Sun', 'SUN', (-3, 5, 5), 2.0, (0, 0, 0))
light('Fill', 'AREA', (3, 1, 4), 120, (0, 0, 0), 5.0)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 4)
aim(cam, (0, 0, 0))
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.06
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (1.25, 0.8, 2.35)
aim(cam, (0, 0, 0))
cam_data.type = 'PERSP'
cam_data.lens = 50
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

