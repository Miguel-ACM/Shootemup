import bpy
import math
import os
import sys
from mathutils import Vector

# The shipped pickup is a brass case, broad base rim and gold conical tip.
# Build it as a single lathed mesh, including small bevels at the rim and tip.
def output_directory():
    try:
        return os.path.dirname(os.path.abspath(__file__))
    except NameError:
        args = sys.argv
        if '--' in args and args.index('--') + 1 < len(args):
            path = os.path.abspath(args[args.index('--') + 1])
            return path if os.path.isdir(path) else os.path.dirname(path)
        return os.getcwd()

out_dir = output_directory()
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)


def make_material(name, color):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Metallic'].default_value = 0.12
    bsdf.inputs['Roughness'].default_value = 0.5
    return mat

brass = make_material('tile_00', (196/255, 150/255, 60/255))
gold = make_material('tile_01', (236/255, 186/255, 40/255))

# Profile in (height, radius), uniformly scaled from the original 0.98 height.
s = 0.9 / 0.98
profile = [
    (-0.50, 0.171), (-0.493, 0.184), (-0.482, 0.19),
    (-0.452, 0.19), (-0.44, 0.181), (-0.44, 0.16),
    (-0.425, 0.16), (-0.20, 0.16), (0.10, 0.16),
    (0.14, 0.16), (0.17, 0.146), (0.30, 0.085),
    (0.43, 0.023), (0.48, 0.0),
]
segments = 16
vertices = []
for y, radius in profile[:-1]:
    for i in range(segments):
        a = 2 * math.pi * i / segments + math.pi / segments
        vertices.append((s * radius * math.cos(a), s * y, s * radius * math.sin(a)))
tip = len(vertices)
vertices.append((0, s * profile[-1][0], 0))
faces = []
material_ids = []
# Winding points outward, including bottom and apex.
faces.append(tuple(range(segments)))
material_ids.append(0)
for j in range(len(profile) - 2):
    for i in range(segments):
        nxt = (i + 1) % segments
        faces.append((j * segments + i, (j + 1) * segments + i,
                      (j + 1) * segments + nxt, j * segments + nxt))
        material_ids.append(1 if j >= 9 else 0)
last = (len(profile) - 2) * segments
for i in range(segments):
    faces.append((last + i, tip, last + (i + 1) % segments))
    material_ids.append(1)
mesh = bpy.data.meshes.new('CartridgeMesh')
mesh.from_pydata(vertices, [], faces)
mesh.materials.append(brass)
mesh.materials.append(gold)
mesh.update()
model = bpy.data.objects.new('Model', mesh)
bpy.context.collection.objects.link(model)
for poly, mat_index in zip(mesh.polygons, material_ids):
    poly.material_index = mat_index
    poly.use_smooth = False
bpy.ops.object.select_all(action='DESELECT')
model.select_set(True)
bpy.context.view_layer.objects.active = model
bpy.ops.wm.obj_export(filepath=os.path.join(out_dir, 'model.obj'),
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
scene.world = bpy.data.worlds.new('Blue Gray World')
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def point_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    point_at(obj, target)
    return obj

light('Sun', 'SUN', (-3, 4, 5), 2.0, (0, 0, 0))
light('Soft Fill', 'AREA', (3, 1, 4), 35, (0, 0, 0), 4.0)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 3)
point_at(cam, (0, 0, 0))
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 0.9 / 0.70
scene.render.filepath = os.path.join(out_dir, 'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (1.28, 0.67, 2.30)
point_at(cam, (0, 0, 0))
cam_data.type = 'PERSP'
cam_data.lens = 55
scene.render.filepath = os.path.join(out_dir, 'angle.png')
bpy.ops.render.render(write_still=True)

