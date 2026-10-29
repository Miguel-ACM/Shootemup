import bpy
import math
import os
import sys
from mathutils import Vector

# Blender runs this either as a file or with its output directory after --.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for material in list(bpy.data.materials):
    bpy.data.materials.remove(material)

palette = [
    (0.51, 0.85, 0.23, 1.0), # bright green enamel
    (0.34, 0.62, 0.15, 1.0), # green housing sides
    (0.68, 0.94, 0.34, 1.0), # raised cell lips
    (0.95, 0.38, 0.12, 1.0), # orange warheads
    (1.00, 0.64, 0.22, 1.0), # warhead shoulders
    (0.30, 0.40, 0.26, 1.0), # olive recesses
    (0.61, 0.70, 0.52, 1.0), # rear hardware
    (0.78, 0.83, 0.60, 1.0), # pale fasteners
]
materials = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = 0.8
    materials.append(mat)

parts = []
def profile(name, x, y, rings, segments=6, phase=math.pi / 6):
    """Create a solid faceted shape from (z, radius, material-for-next-band) rings."""
    verts = []
    for z, radius, _ in rings:
        for i in range(segments):
            a = phase + 2 * math.pi * i / segments
            verts.append((x + radius * math.cos(a), y + radius * math.sin(a), z))
    faces = [tuple(reversed(range(segments)))]
    ids = [rings[0][2]]
    for j in range(len(rings) - 1):
        for i in range(segments):
            k = (i + 1) % segments
            faces.append((j * segments + i, j * segments + k, (j + 1) * segments + k, (j + 1) * segments + i))
            ids.append(rings[j][2])
    faces.append(tuple((len(rings) - 1) * segments + i for i in range(segments)))
    ids.append(rings[-1][2])
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.materials.clear()
    for mat in materials:
        mesh.materials.append(mat)
    for polygon, idx in zip(mesh.polygons, ids):
        polygon.material_index = idx
        polygon.use_smooth = False
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    parts.append(obj)
    return obj

def box(name, location, scale, mat, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(materials[mat])
    if bevel:
        mod = obj.modifiers.new('Chamfer', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    parts.append(obj)
    return obj

# A compact armored spine supports six individual, deep hexagonal tubes.
box('Armored center frame', (0, 0, -0.035), (0.71, 0.77, 0.29), 1, 0.055)
for side in (-1, 1):
    box('Side armor', (side * 0.355, 0, -0.015), (0.075, 0.65, 0.32), 0, 0.024)
    box('Side armor inset', (side * 0.395, 0, -0.015), (0.018, 0.32, 0.18), 2, 0.006)

for x in (-0.195, 0.195):
    for y in (-0.255, 0.0, 0.255):
        # Faceted housing, bevelled outer collar and visible recessed front well.
        profile('Hex launch tube', x, y, [(-0.205, 0.128, 1), (-0.17, 0.15, 0), (0.11, 0.15, 0), (0.145, 0.133, 2)])
        profile('Raised hex collar', x, y, [(0.14, 0.133, 1), (0.155, 0.133, 2), (0.17, 0.115, 2)])
        profile('Launcher well', x, y, [(0.169, 0.105, 5), (0.177, 0.105, 5)])
        # Short projectile protrudes toward +Z, with an unmistakable orange tip.
        profile('Missile', x, y, [(0.175, 0.079, 4), (0.205, 0.079, 3), (0.265, 0.050, 3), (0.305, 0.008, 3)])
        # Rear hex caps make the rotating model as deliberate from the back.
        profile('Rear socket', x, y, [(-0.235, 0.105, 6), (-0.205, 0.115, 6)])
        profile('Rear plug', x, y, [(-0.246, 0.060, 5), (-0.234, 0.060, 5)])

# A broad bridge visibly binds the rack together at its rear.
box('Rear cross brace', (0, 0, -0.255), (0.66, 0.095, 0.045), 0, 0.015)
for x in (-0.27, 0.27):
    profile('Brace bolt', x, 0, [(-0.282, 0.024, 7), (-0.278, 0.024, 7)], segments=6)

bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
tri = model.modifiers.new('Triangulate', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
for poly in model.data.polygons:
    poly.use_smooth = False

# Axis choices preserve the supplied +Y-up, +Z-front coordinates in the OBJ.
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
world = bpy.data.worlds.new('Blue-gray studio') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world = world
world.use_nodes = True
world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.05, 0.07, 0.09, 1.0)
world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def point_at(obj, target=(0, 0, 0)):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, location, energy, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    point_at(obj)
    return obj

light('Upper-left sun', 'SUN', (-3, 5, 6), 2.0)
light('Soft fill', 'AREA', (4, 1, 5), 110, 5.0)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 4)
point_at(cam)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.05
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (0.85, 0.51, 1.36)
point_at(cam)
cam_data.type = 'PERSP'
cam_data.lens = 50
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

