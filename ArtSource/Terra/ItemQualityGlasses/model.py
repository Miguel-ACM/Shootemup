import bpy
import math
import os
import sys
from mathutils import Vector

# This asset uses Y as its vertical axis and faces toward +Z.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
    if OUT.lower().endswith('.py'):
        OUT = os.path.dirname(OUT)
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
    for block in list(datablocks):
        if block.users == 0:
            datablocks.remove(block)

palette = [
    (0.82, 0.59, 0.19, 1),  # warm gold frame
    (1.00, 0.77, 0.32, 1),  # bevel highlight
    (0.62, 0.40, 0.12, 1),  # frame side
    (0.12, 0.67, 0.75, 1),  # cyan glass
    (0.25, 0.82, 0.87, 1),  # glass facets
    (0.64, 0.96, 0.94, 1),  # reflection
    (0.14, 0.48, 0.54, 1),  # glass edge
]
mats = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = 0.7
    mats.append(mat)

parts = []
def mesh_obj(name, verts, faces, indices):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    for mat in mats:
        mesh.materials.append(mat)
    for poly, idx in zip(mesh.polygons, indices):
        poly.material_index = idx
        poly.use_smooth = False
    parts.append(obj)
    return obj

def rim(cx):
    # Six concentric loops create real beveled front, back and inner edges.
    n = 12
    loops = [(0.197, -0.073), (0.215, -0.054), (0.215, 0.045),
             (0.197, 0.067), (0.161, 0.067), (0.150, 0.052),
             (0.150, -0.055), (0.161, -0.073)]
    verts = []
    for rad, z in loops:
        for j in range(n):
            a = 2 * math.pi * (j + 0.5) / n
            verts.append((cx + rad * math.cos(a), rad * math.sin(a), z))
    faces, ids = [], []
    for k in range(len(loops)):
        nxt = (k + 1) % len(loops)
        for j in range(n):
            faces.append((k*n+j, k*n+(j+1)%n, nxt*n+(j+1)%n, nxt*n+j))
            ids.append([2, 0, 1, 1, 1, 2, 2, 0][k])
    mesh_obj('Faceted gold rim', verts, faces, ids)

def lens(cx):
    n = 12
    verts = []
    for z in (-0.057, 0.053):
        for j in range(n):
            a = 2 * math.pi * (j + 0.5) / n
            verts.append((cx + 0.154 * math.cos(a), 0.154 * math.sin(a), z))
    verts += [(cx, 0, -0.058), (cx, 0, 0.054)]
    faces, ids = [], []
    for j in range(n):
        k = (j+1) % n
        faces += [(j, k, n+k, n+j), (2*n, k, j), (2*n+1, n+j, n+k)]
        ids += [6, 3 if j % 3 else 4, 3 if j % 3 else 4]
    mesh_obj('Solid cyan lens', verts, faces, ids)

def prism(name, points, z0, z1, mat):
    n = len(points)
    verts = [(x,y,z0) for x,y in points] + [(x,y,z1) for x,y in points]
    faces = [tuple(reversed(range(n))), tuple(range(n,2*n))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh_obj(name, verts, faces, [mat] * len(faces))

def bevel_box(name, center, dimensions, material, bevel=0.012):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mats[material])
    mod = obj.modifiers.new('Rounded edges', 'BEVEL')
    mod.width = bevel
    mod.segments = 1
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=mod.name)
    for poly in obj.data.polygons:
        poly.use_smooth = False
    parts.append(obj)
    return obj

for side in (-1, 1):
    cx = side * 0.222
    rim(cx)
    lens(cx)
    # Broad, graphic glints are physical opaque inlays on both sides of the lens.
    glint = [(cx-0.095, 0.028), (cx-0.045, 0.095),
             (cx-0.006, 0.095), (cx-0.056, 0.028)]
    prism('Lens reflection front', glint, 0.0545, 0.057, 5)
    prism('Lens reflection back', glint, -0.061, -0.0585, 5)
    # Hinges and thick rearward arms, with a lowered earpiece at the far end.
    bevel_box('Hinge', (side*0.426, 0.045, -0.045), (0.055, 0.085, 0.105), 1)
    bevel_box('Temple', (side*0.426, 0.045, -0.208), (0.052, 0.066, 0.32), 0)
    bevel_box('Temple tip', (side*0.426, 0.006, -0.369), (0.055, 0.125, 0.075), 2)

# Raised bridge ties the two round frames together and remains visible on both sides.
bevel_box('Bridge', (0, 0.025, 0.003), (0.13, 0.058, 0.13), 0, 0.013)
bevel_box('Bridge crest', (0, 0.052, 0.038), (0.105, 0.025, 0.075), 1, 0.008)

bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
# Triangulation is explicit so the game and preview share the same flat geometry.
tri = model.modifiers.new('Triangulate', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
for face in model.data.polygons:
    face.use_smooth = False

bpy.ops.object.select_all(action='DESELECT')
model.select_set(True)
bpy.context.view_layer.objects.active = model
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
scene.render.film_transparent = False
scene.world = bpy.data.worlds.new('Blue gray studio') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def point_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, loc, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = loc
    point_at(obj, target)
    return obj

light('Upper left sun', 'SUN', (-3, 5, 6), 2.0, (0, 0, 0))
light('Soft fill', 'AREA', (4, 1, 4), 150, (0, 0, -0.1), 5.0)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 4)
point_at(cam, (0, 0, 0))
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.08
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam_data.lens = 50
cam.location = (1.35, 0.82, 2.55)
point_at(cam, (0, 0, -0.15))
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)
