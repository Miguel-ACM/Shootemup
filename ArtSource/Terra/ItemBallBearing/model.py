import bpy
import math
import os
import sys
from mathutils import Vector

# Resolve outputs even when Blender executes a script without defining __file__.
try:
    SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    if '--' not in args or not args[args.index('--') + 1:]:
        raise RuntimeError('Pass the script path after -- when __file__ is unavailable')
    SCRIPT_DIR = os.path.dirname(os.path.abspath(args[args.index('--') + 1]))

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

palette = [
    (0.60, 0.73, 0.92, 1),  # blue steel outer race
    (0.78, 0.87, 0.98, 1),  # bright machined bevels
    (0.32, 0.44, 0.62, 1),  # recessed race
    (0.69, 0.81, 0.95, 1),  # inner race
    (0.90, 0.95, 1.00, 1),  # bearing balls
    (0.46, 0.60, 0.79, 1),  # edge band
]
mats = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Metallic'].default_value = 0.18
    bsdf.inputs['Roughness'].default_value = 0.4
    mats.append(mat)

parts = []
def ring(name, profile, bands, segments=16):
    # Profile is a closed radial cross-section (radius, depth along Z).
    vertices = []
    for radius, z in profile:
        for j in range(segments):
            a = 2 * math.pi * j / segments
            vertices.append((radius * math.cos(a), radius * math.sin(a), z))
    faces = []
    for k in range(len(profile)):
        n = (k + 1) % len(profile)
        for j in range(segments):
            q = (j + 1) % segments
            faces.append((k * segments + j, k * segments + q,
                          n * segments + q, n * segments + j))
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    for mat in mats:
        mesh.materials.append(mat)
    for k, material in enumerate(bands):
        for poly in mesh.polygons[k * segments:(k + 1) * segments]:
            poly.material_index = material
    parts.append(obj)
    return obj

# Continuous through-bore and substantial rims with machined, faceted bevels.
ring('Outer race', [(.300, -.105), (.340, -.105), (.360, -.085),
                    (.360, .085), (.340, .105), (.300, .105),
                    (.292, .083), (.292, -.083)],
     [1, 1, 5, 1, 1, 0, 0, 0])
ring('Inner race', [(.100, -.085), (.115, -.105), (.178, -.105),
                    (.190, -.087), (.190, .087), (.178, .105),
                    (.115, .105), (.100, .085)],
     [1, 3, 1, 3, 1, 3, 1, 5])
# A recessed annular cage supports the balls without closing the central hole.
ring('Recessed ball cage', [(.187, -.042), (.295, -.042),
                             (.295, .042), (.187, .042)], [2, 2, 2, 2])
for i in range(8):
    a = i * math.tau / 8
    bpy.ops.mesh.primitive_uv_sphere_add(segments=10, ring_count=8, radius=1,
        location=(.243 * math.cos(a), .243 * math.sin(a), 0))
    ball = bpy.context.object
    ball.name = 'Ball %02d' % (i + 1)
    ball.scale = (.065, .065, .065)
    ball.data.materials.append(mats[4])
    parts.append(ball)

bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for polygon in model.data.polygons:
    polygon.use_smooth = False

bpy.ops.wm.obj_export(filepath=os.path.join(SCRIPT_DIR, 'model.obj'),
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
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1

def point_at(obj, target=(0, 0, 0)):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, location, energy, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    obj.location = location
    point_at(obj)
    return obj

light('Upper left sun', 'SUN', (-3, 5, 6), 2.0)
light('Soft fill', 'AREA', (4, 1, 4), 180, 5.0)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 4)
point_at(cam)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = .85
scene.render.filepath = os.path.join(SCRIPT_DIR, 'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (0.65, 0.38, 1.20)
point_at(cam)
cam_data.type = 'PERSP'
cam_data.lens = 50
scene.render.filepath = os.path.join(SCRIPT_DIR, 'angle.png')
bpy.ops.render.render(write_still=True)

