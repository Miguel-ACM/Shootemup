import bpy
import bmesh
import math
import os
import sys
from mathutils import Vector


def output_directory():
    if '__file__' in globals():
        return os.path.dirname(os.path.abspath(__file__))
    if '--' in sys.argv:
        args = sys.argv[sys.argv.index('--') + 1:]
        if args:
            path = os.path.abspath(args[0])
            return path if os.path.isdir(path) else os.path.dirname(path)
    return os.getcwd()


OUT = output_directory()
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)


def material(name, color):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.68
    bsdf.inputs['Metallic'].default_value = 0.0
    return mat


red = material('tile_00', (200 / 255, 30 / 255, 30 / 255))
chrome = material('tile_01', (210 / 255, 216 / 255, 226 / 255))

# Centerline follows the original 0.26-radius horseshoe. A chamfered
# rectangular cross-section gives the complete body continuous front, back,
# inner and outer surfaces, including the open U-shaped gap.
scale = 0.85
center_y = -0.06
half_width = 0.10
half_depth = 0.12
chamfer = 0.018
section = [(-half_width + chamfer, -half_depth),
           ( half_width - chamfer, -half_depth),
           ( half_width, -half_depth + chamfer),
           ( half_width,  half_depth - chamfer),
           ( half_width - chamfer,  half_depth),
           (-half_width + chamfer,  half_depth),
           (-half_width,  half_depth - chamfer),
           (-half_width, -half_depth + chamfer)]

# Each entry is (x, y, tangent-x, tangent-y, end-face inset factor).
path = [(-0.26, 0.47, 0, -1, 0.86),
        (-0.26, 0.45, 0, -1, 1.0),
        (-0.26, 0.33, 0, -1, 1.0),
        (-0.26, -0.23, 0, -1, 1.0)]
for i in range(1, 25):
    a = math.pi + math.pi * i / 24
    path.append((0.26 * math.cos(a), -0.23 + 0.26 * math.sin(a),
                 -math.sin(a), math.cos(a), 1.0))
path += [(0.26, 0.33, 0, 1, 1.0),
         (0.26, 0.45, 0, 1, 1.0),
         (0.26, 0.47, 0, 1, 0.86)]

verts = []
for x, y, tx, ty, factor in path:
    # Perpendicular in the image plane; clipping the corners also bevels the
    # long edges consistently all the way around the curve.
    nx, ny = -ty, tx
    for width, depth in section:
        verts.append(((x + nx * width * factor) * scale,
                      (y + ny * width * factor - center_y) * scale,
                      depth * factor * scale))

faces = []
indices = []
n = len(section)
faces.append(tuple(reversed(range(n))))
indices.append(1)
for i in range(len(path) - 1):
    for j in range(n):
        k = (j + 1) % n
        faces.append((i * n + j, i * n + k, (i + 1) * n + k, (i + 1) * n + j))
        indices.append(1 if i < 2 or i >= len(path) - 3 else 0)
faces.append(tuple((len(path) - 1) * n + j for j in range(n)))
indices.append(1)

mesh = bpy.data.meshes.new('MagnetMesh')
mesh.from_pydata(verts, [], faces)
mesh.materials.append(red)
mesh.materials.append(chrome)
mesh.update()
obj = bpy.data.objects.new('Model', mesh)
bpy.context.collection.objects.link(obj)
for poly, index in zip(mesh.polygons, indices):
    poly.material_index = index
    poly.use_smooth = False

# Correct the winding and triangulate explicitly so the exported budget is
# deterministic and every face remains flat shaded.
bm = bmesh.new()
bm.from_mesh(mesh)
bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
bmesh.ops.triangulate(bm, faces=bm.faces[:])
bm.to_mesh(mesh)
bm.free()
mesh.update()
bpy.context.view_layer.objects.active = obj
obj.select_set(True)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
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
scene.render.film_transparent = False
world = bpy.data.worlds.new('Blue Gray World') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world = world
world.use_nodes = True
world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'


def aim(ob, target=(0, 0, 0)):
    ob.rotation_euler = (Vector(target) - ob.location).to_track_quat('-Z', 'Y').to_euler()


def light(name, kind, location, energy, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    ob = bpy.data.objects.new(name, data)
    scene.collection.objects.link(ob)
    ob.location = location
    aim(ob)
    return ob


sun = light('Sun', 'SUN', (-3, 5, 6), 2.0)
sun.data.angle = math.radians(12)
light('Soft Fill', 'AREA', (4, 1, 4), 90, 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 4)
aim(cam)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.30
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)

cam.location = (1.8, 1.15, 3.1)
aim(cam)
cam_data.type = 'PERSP'
cam_data.lens = 50
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)
