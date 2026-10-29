import bpy
import math
import os
import sys
from mathutils import Vector

# Blender's -- argument may point either to this script or to its output folder.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    if not args:
        raise RuntimeError('Pass the script path or output directory after --')
    path = os.path.abspath(args[0])
    OUT = path if os.path.isdir(path) else os.path.dirname(path)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
    for data in list(datablocks):
        if data.users == 0:
            datablocks.remove(data)

palette = [
    (0.82, 0.56, 0.16, 1),  # warm gold drill edges
    (1.00, 0.78, 0.32, 1),  # cutting face
    (0.48, 0.58, 0.59, 1),  # steel flutes
    (0.66, 0.73, 0.71, 1),  # bright steel
    (0.27, 0.36, 0.39, 1),  # collar steel
    (0.49, 0.18, 0.15, 1),  # oxblood grip
    (0.68, 0.28, 0.20, 1),  # grip facets
    (0.72, 0.65, 0.43, 1),  # warm metal trim
]
mats = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = 0.72
    mats.append(mat)

parts = []
def mesh_object(name, vertices, faces, indices):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], [tuple(reversed(face)) for face in faces])
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    for mat in mats:
        mesh.materials.append(mat)
    for poly, index in zip(mesh.polygons, indices):
        poly.material_index = index
        poly.use_smooth = False
    parts.append(obj)
    return obj

def lathe(name, rings, sides, material, phase=0.0):
    verts = []
    for y, r in rings:
        for j in range(sides):
            a = 2 * math.pi * j / sides + phase
            verts.append((r * math.cos(a), y, r * math.sin(a)))
    faces = [tuple(reversed(tuple(range(sides))))]
    indices = [material]
    for k in range(len(rings) - 1):
        for j in range(sides):
            n = (j + 1) % sides
            faces.append((k*sides+j, k*sides+n, (k+1)*sides+n, (k+1)*sides+j))
            indices.append(material)
    faces.append(tuple((len(rings)-1)*sides+j for j in range(sides)))
    indices.append(material)
    return mesh_object(name, verts, faces, indices)

# A tapered solid with three broad helical cutting lands.  The same sculpted
# section wraps all the way around, so the spinning pickup reads from every side.
sides, steps = 24, 24
verts, faces, indices = [], [], []
for k in range(steps + 1):
    t = k / steps
    y = -0.095 + 0.555 * t
    base_r = 0.206 * (1.0 - t) ** 0.82 + 0.003
    for j in range(sides):
        a = 2 * math.pi * j / sides
        phase = (3 * (a - 2.5 * t)) % (2 * math.pi)
        # A broad raised land with a recessed, gently sloped steel flute.
        relief = 0.82 + 0.18 * (0.5 + 0.5 * math.cos(phase))
        r = base_r * relief
        verts.append((r * math.cos(a), y, r * math.sin(a)))
for k in range(steps):
    for j in range(sides):
        n = (j + 1) % sides
        faces.append((k*sides+j, k*sides+n, (k+1)*sides+n, (k+1)*sides+j))
        phase = (3 * (2 * math.pi * (j + 0.5) / sides - 2.5 * (k + 0.5) / steps)) % (2 * math.pi)
        indices.append(1 if phase < 0.85 else 0 if phase < 1.65 else 2 if phase < 4.8 else 3)
faces.append(tuple(reversed(tuple(range(sides)))))
indices.append(2)
faces.append(tuple(steps*sides+j for j in range(sides)))
indices.append(1)
mesh_object('Spiral cutting head', verts, faces, indices)

# Layered, wide twelve-sided chuck and its bright mounting lip.
lathe('Chuck', [(-0.205, .195), (-0.19, .232), (-0.115, .232), (-0.095, .205)], 12, 4, math.pi/12)
lathe('Chuck front lip', [(-0.113, .230), (-0.099, .230), (-0.090, .207)], 12, 3, math.pi/12)
lathe('Chuck base rim', [(-0.211, .190), (-0.205, .222), (-0.187, .228)], 12, 7, math.pi/12)

# A chunky, octagonal grip with a socket and a capped heel.
lathe('Grip socket', [(-0.242, .145), (-0.211, .175), (-0.200, .175)], 8, 3, math.pi/8)
lathe('Grip', [(-0.425, .115), (-0.403, .135), (-0.258, .135), (-0.236, .145)], 8, 5, math.pi/8)
lathe('Heel', [(-0.450, .120), (-0.438, .145), (-0.414, .145)], 8, 4, math.pi/8)
lathe('Grip band', [(-0.335, .136), (-0.310, .136)], 8, 6, math.pi/8)

# Symmetric stout side lugs preserve the reference's distinctive guard.
def box(name, location, scale, mat_index, bevel):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mats[mat_index])
    mod = obj.modifiers.new('Broad machined corners', 'BEVEL')
    mod.width = bevel
    mod.segments = 1
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=mod.name)
    for poly in obj.data.polygons:
        poly.use_smooth = False
    parts.append(obj)
for x in (-0.188, 0.188):
    box('Guard lug', (x, -0.292, 0), (0.095, 0.152, 0.150), 2, 0.018)

bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for p in model.data.polygons:
    p.use_smooth = False

# Export only the asset, before adding the preview rig.
bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'),
                      export_selected_objects=True, export_materials=True,
                      forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.cycles.use_denoising = True
scene.render.resolution_x = 512
scene.render.resolution_y = 512
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.render.film_transparent = False
world = bpy.data.worlds.new('Blue gray studio') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world = world
world.use_nodes = True
world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1
scene.view_settings.view_transform = 'Standard'

def point_at(obj, point):
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
    point_at(obj, (0, 0, 0))
    return obj
light('Upper left sun', 'SUN', (-3, 5, 4), 2.0).data.angle = math.radians(12)
light('Soft fill', 'AREA', (3, 1, 4), 35, 4.0)

cam_data = bpy.data.cameras.new('Preview camera')
cam = bpy.data.objects.new('Preview camera', cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 4)
point_at(cam, (0, 0, 0))
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.22
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam_data.lens = 50
cam.location = (0.95, 0.62, 1.55)
point_at(cam, (0, 0, 0))
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

