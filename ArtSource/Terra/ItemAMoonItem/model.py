import bpy
import math
import os
import sys
from mathutils import Vector


def output_directory():
    script = globals().get('__file__')
    if script:
        return os.path.dirname(os.path.abspath(script))
    if '--' in sys.argv:
        args = sys.argv[sys.argv.index('--') + 1:]
        if args:
            path = os.path.abspath(args[0])
            return path if os.path.isdir(path) else os.path.dirname(path)
    return os.getcwd()


out_dir = output_directory()
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

# The source atlas uses neutral cool grays (150–166 for the surface,
# 104–124 for crater interiors, and a lighter gray on parts of the rims).
def make_material(name, rgb):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*rgb, 1.0)
    mat.use_nodes = True
    shader = mat.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value = (*rgb, 1.0)
    shader.inputs['Roughness'].default_value = 0.9
    return mat

materials = [
    make_material('tile_00', (0.62, 0.62, 0.635)),
    make_material('tile_01', (0.48, 0.48, 0.495)),
    make_material('tile_02', (0.43, 0.43, 0.445)),
    make_material('tile_03', (0.70, 0.70, 0.715)),
]

# A single spherical shell, with broad, shallow impact depressions integrated
# into its surface. Craters are distributed around the entire spinning moon.
radius = 0.46
craters = [
    ((-0.38, 0.37, 0.85), 0.30),
    ((0.37, -0.12, 0.92), 0.34),
    ((-0.25, -0.55, 0.80), 0.24),
    ((0.95, 0.26, 0.18), 0.27),
    ((0.51, -0.42, -0.75), 0.31),
    ((-0.20, 0.40, -0.89), 0.34),
    ((-0.83, -0.25, -0.50), 0.25),
    ((-0.92, 0.24, 0.30), 0.27),
]
craters = [(Vector(p).normalized(), r) for p, r in craters]

bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=24, radius=radius)
moon = bpy.context.object
moon.name = 'Model'
for mat in materials:
    moon.data.materials.append(mat)

for vertex in moon.data.vertices:
    direction = vertex.co.normalized()
    depression = 0.0
    for center, angular_radius in craters:
        angle = math.acos(max(-1.0, min(1.0, direction.dot(center))))
        t = angle / angular_radius
        if t < 1.0:
            # Rounded bowl with a gentle, broad rim, retaining the round outline.
            bowl = 0.025 * (1.0 - t * t) ** 2
            depression = max(depression, bowl)
    vertex.co = direction * (radius - depression)

for face in moon.data.polygons:
    direction = sum((moon.data.vertices[i].co for i in face.vertices), Vector()).normalized()
    face.material_index = 0
    for center, angular_radius in craters:
        t = math.acos(max(-1.0, min(1.0, direction.dot(center)))) / angular_radius
        if t < 0.67:
            face.material_index = 1 if direction.y < center.y else 2
            break
        if 0.75 < t < 1.08 and direction.y < center.y:
            face.material_index = 3
            break
    face.use_smooth = False

bpy.context.view_layer.objects.active = moon
moon.select_set(True)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

# The Y-forward/Z-up setting leaves the authored XYZ coordinates unchanged.
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
scene.world = bpy.data.worlds.new('Blue gray world') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1.0)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'


def aim(obj, target=(0, 0, 0)):
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
    aim(obj)
    return obj

sun = light('Upper left sun', 'SUN', (-3, 5, 5), 2.0)
sun.data.angle = math.radians(18)
light('Soft fill', 'AREA', (4, 1, 4), 35, 5.0)

camera_data = bpy.data.cameras.new('Camera')
camera = bpy.data.objects.new('Camera', camera_data)
scene.collection.objects.link(camera)
scene.camera = camera
camera.location = (0, 0, 3)
aim(camera)
camera_data.type = 'ORTHO'
camera_data.ortho_scale = 1.32
scene.render.filepath = os.path.join(out_dir, 'top.png')
bpy.ops.render.render(write_still=True)

camera.location = (1.0, 0.5, 1.5)
aim(camera)
camera_data.type = 'PERSP'
camera_data.lens = 50
scene.render.filepath = os.path.join(out_dir, 'angle.png')
bpy.ops.render.render(write_still=True)

