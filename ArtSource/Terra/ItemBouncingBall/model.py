import bpy
import math
import os
import sys
from mathutils import Vector


def script_directory():
    if '__file__' in globals():
        return os.path.dirname(os.path.abspath(__file__))
    if '--' in sys.argv:
        args = sys.argv[sys.argv.index('--') + 1:]
        if args:
            path = os.path.abspath(args[0])
            return path if os.path.isdir(path) else os.path.dirname(path)
    raise RuntimeError('Pass the script path after -- when __file__ is unavailable')


OUT = script_directory()
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)


def material(index, color):
    mat = bpy.data.materials.new('tile_%02d' % index)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.7
    return mat


blue = material(0, (0.055, 0.30, 0.80))
light_blue = material(1, (0.12, 0.51, 0.96))
cream = material(2, (0.88, 0.91, 0.82))
charcoal = material(3, (0.10, 0.16, 0.24))

# A broad colored equatorial stripe is built into the faceted ball itself.
bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=12, radius=0.40)
ball = bpy.context.object
ball.data.materials.append(blue)
ball.data.materials.append(light_blue)
# Sphere is generated about Z; assign by model-space Y for an upright stripe.
for face in ball.data.polygons:
    y = sum(ball.data.vertices[v].co.y for v in face.vertices) / len(face.vertices)
    face.material_index = 1 if abs(y) < 0.105 else 0


def badge(angle):
    # Local badge Z points outward, with local Y remaining the vertical axis.
    radial = Vector((math.sin(angle), 0, math.cos(angle)))
    rotation = (0, angle, 0)
    bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=0.145, depth=0.038,
        location=radial * 0.386, rotation=rotation)
    rim = bpy.context.object
    rim.data.materials.append(cream)
    # A chunky plus sign is embedded in the cap, visible at small scale.
    for sx, sy in ((0.176, 0.046), (0.046, 0.176)):
        bpy.ops.mesh.primitive_cube_add(size=1, location=radial * 0.408, rotation=rotation)
        bar = bpy.context.object
        bar.dimensions = (sx, sy, 0.012)
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
        bar.data.materials.append(charcoal)


for i in range(4):
    badge(i * math.pi / 2)

# Join and bake model transforms before exporting, keeping flat polygon normals.
bpy.ops.object.select_all(action='SELECT')
bpy.context.view_layer.objects.active = ball
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for poly in model.data.polygons:
    poly.use_smooth = False

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
scene.world = bpy.data.worlds.new('Blue gray world')
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
background.inputs['Strength'].default_value = 1.0
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
    scene.collection.objects.link(obj)
    obj.location = location
    point_at(obj)
    return obj


light('Upper left sun', 'SUN', (-3, 5, 6), 2.0)
light('Soft fill', 'AREA', (4, 1, 4), 90, 5.0)

cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 4)
point_at(cam)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.02
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)

cam.location = (2.5, 1.6, 3.5)
point_at(cam)
cam_data.type = 'PERSP'
cam_data.lens = 50
# At this distance a roughly .84-unit ball fills about 70% of the frame.
cam.location = cam.location.normalized() * 1.8
point_at(cam)
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

