import bpy
import bmesh
import math
import os
import sys
from mathutils import Vector

# The item uses game coordinates: X right, Y up, Z toward the viewer.
def script_directory():
    if globals().get('__file__'):
        return os.path.dirname(os.path.abspath(__file__))
    if '--' in sys.argv:
        args = sys.argv[sys.argv.index('--') + 1:]
        if args:
            path = os.path.abspath(args[0])
            return path if os.path.isdir(path) else os.path.dirname(path)
    return os.getcwd()

OUT = script_directory()
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablock in bpy.data.materials:
    bpy.data.materials.remove(datablock)

palette = [
    (0.28, 0.78, 0.75, 1),  # turquoise blade face
    (0.48, 0.94, 0.88, 1),  # sharpened edge
    (0.17, 0.54, 0.56, 1),  # blade thickness
    (0.19, 0.46, 0.50, 1),  # cradle body
    (0.35, 0.71, 0.69, 1),  # cradle shoulders
    (0.92, 0.69, 0.27, 1),  # warm brass hub
    (0.99, 0.83, 0.43, 1),  # hub cap
    (0.35, 0.55, 0.58, 1),  # blue steel
]
mats = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = 0.68
    mats.append(mat)

parts = []
def mesh_part(name, verts, faces, indices):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
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

def box(name, loc, size, material, bevel=0.025):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mats[material])
    if bevel:
        mod = obj.modifiers.new('Faceted bevel', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    parts.append(obj)
    return obj

def cylinder(name, loc, radius, depth, material, vertices=12):
    # Blender cylinders naturally run along Z, the axle of the blade.
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mats[material])
    parts.append(obj)
    return obj

# An eight-tooth blade, with a broad bevel on both faces and a real thick edge.
# Four vertices per tooth give the teeth a swept, circular saw profile.
N = 32
cy = 0.015
outer = [0.345, 0.360, 0.435, 0.365]
rings = []
for layer in range(4):
    ring = []
    for i in range(N):
        a = 2 * math.pi * i / N
        r = (0.285 if layer in (0, 3) else outer[i % 4])
        z = (0.062, 0.024, -0.024, -0.062)[layer]
        ring.append((r * math.cos(a), cy + r * math.sin(a), z))
    rings.append(ring)
verts = sum(rings, [])
faces = []
inds = []
# Caps are covered at their centers by the raised axle, but remain closed.
faces.append(tuple(range(N - 1, -1, -1))); inds.append(0)
faces.append(tuple(3 * N + i for i in range(N))); inds.append(0)
for layer in range(3):
    for i in range(N):
        j = (i + 1) % N
        faces.append((layer*N+i, layer*N+j, (layer+1)*N+j, (layer+1)*N+i))
        inds.append((1, 2, 1)[layer])
blade = mesh_part('Swept toothed blade', verts, faces, inds)
# Recalculate normals for the custom closed mesh.
bm = bmesh.new()
bm.from_mesh(blade.data)
bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
bm.to_mesh(blade.data)
bm.free()

# Curved, thick lower launch cradle: a semi-annular shell around the disc.
segments = 12
verts = []
for z in (-0.135, 0.135):
    for r in (0.355, 0.425):
        for i in range(segments + 1):
            a = math.pi + math.pi * i / segments
            verts.append((r * math.cos(a), cy + r * math.sin(a), z))
k = segments + 1
faces = []
inds = []
for i in range(segments):
    j = i + 1
    faces.extend([(i, j, k+j, k+i), (2*k+i, 3*k+i, 3*k+j, 2*k+j),
                  (i, 2*k+i, 2*k+j, j), (k+i, k+j, 3*k+j, 3*k+i)])
    inds.extend([4, 4, 3, 3])
faces.extend([(0, k, 3*k, 2*k), (segments, 2*k+segments, 3*k+segments, k+segments)])
inds.extend([3, 3])
mesh_part('Half moon cradle', verts, faces, inds)

# Rounded housing, a pair of strong end stops, and a gold axle on both sides.
box('Launch base', (0, -0.365, 0), (0.58, 0.16, 0.34), 3, 0.035)
box('Base center panel', (0, -0.383, 0), (0.32, 0.14, 0.37), 4, 0.025)
for side in (-1, 1):
    box('Cradle end stop', (side * 0.378, -0.055, 0), (0.105, 0.19, 0.30), 4, 0.025)
    cylinder('Axle collar', (0, cy, side * 0.075), 0.144, 0.07, 7)
    cylinder('Brass rotor', (0, cy, side * 0.118), 0.110, 0.035, 5)
    cylinder('Axle cap', (0, cy, side * 0.140), 0.055, 0.018, 6, 8)

# Join the actual model before adding presentation objects.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = blade
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for poly in model.data.polygons:
    poly.use_smooth = False
# Triangulate before export, so the OBJ has exactly the intended flat facets.
tri = model.modifiers.new('Triangulate', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
assert 300 <= len(model.data.polygons) <= 2000, 'Model exceeds triangle budget'

bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'),
    export_selected_objects=True, export_materials=True,
    forward_axis='Y', up_axis='Z')

# Presentation: only the model was selected for export.
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
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
background.inputs['Strength'].default_value = 1.0

def aim(obj, target=(0, 0, 0)):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, loc, energy, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None and kind == 'AREA':
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    obj.location = loc
    aim(obj)
    return obj

sun = light('Upper left sun', 'SUN', (-3, 5, 6), 2.0)
sun.data.angle = math.radians(12)
light('Soft fill', 'AREA', (4, 1, 4), 180, 5)

camera_data = bpy.data.cameras.new('Camera')
camera = bpy.data.objects.new('Camera', camera_data)
scene.collection.objects.link(camera)
scene.camera = camera
camera.location = (0, 0, 3)
aim(camera)
camera_data.type = 'ORTHO'
camera_data.ortho_scale = 1.16
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)

camera.location = (0.9, 0.58, 1.49)
aim(camera)
camera_data.type = 'PERSP'
camera_data.lens = 50
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

