import bpy
import math
import os
import sys
from mathutils import Vector

# The game uses +Y as up and +Z as the front of the pickup.
def script_directory():
    if globals().get('__file__'):
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


def material(name, color):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.8
    return mat

moon = material('tile_00', (0.61, 0.61, 0.63))
crater = material('tile_01', (0.47, 0.47, 0.49))
brow = material('tile_02', (0.085, 0.085, 0.10))
red = material('tile_03', (0.86, 0.055, 0.075))
teeth = material('tile_04', (0.92, 0.90, 0.86))
parts = []

# A faceted full sphere, with shallow depressions integrated into its surface.
# The three large marks echo the craters in the original moon atlas tile;
# additional craters on the reverse keep the spinning sphere complete.
bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=20, radius=0.46)
body = bpy.context.object
body.name = 'Moon'
body.data.materials.append(moon)
body.data.materials.append(crater)
marks = [
    (Vector((-0.49, 0.58, 0.65)).normalized(), 0.20),
    (Vector((0.66, -0.16, 0.73)).normalized(), 0.22),
    (Vector((-0.57, -0.68, 0.46)).normalized(), 0.17),
    (Vector((0.75, 0.43, -0.51)).normalized(), 0.22),
    (Vector((-0.45, 0.55, -0.70)).normalized(), 0.18),
    (Vector((0.12, -0.72, -0.69)).normalized(), 0.21),
]
for vert in body.data.vertices:
    direction = vert.co.normalized()
    depth = 0.0
    for center, radius in marks:
        d = (direction - center).length
        if d < radius:
            t = d / radius
            depth = max(depth, 0.014 * (1.0 - t * t))
    vert.co *= (0.46 - depth) / 0.46
body.data.update()
for poly in body.data.polygons:
    direction = poly.center.normalized() if poly.center.length else Vector((0, 0, 1))
    # Assign a restrained gray to the innermost crater facets.
    if any((direction - center).length < radius * 0.62 for center, radius in marks):
        poly.material_index = 1
    poly.use_smooth = False
body.data.update()
parts.append(body)


def beveled_box(name, location, dimensions, mat, rotation=0.0, bevel=0.008):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dimensions
    obj.rotation_euler[2] = rotation
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    mod = obj.modifiers.new('Small edge bevel', 'BEVEL')
    mod.width = bevel
    mod.segments = 1
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=mod.name)
    for face in obj.data.polygons:
        face.use_smooth = False
    parts.append(obj)
    return obj

# Preserve the original V-shaped brows, small rectangular red eyes and three
# pale vertical bars across the straight dark mouth.
for side in (-1, 1):
    beveled_box('Brow', (side * 0.15, 0.16, 0.429),
                (0.22, 0.055, 0.102), brow, side * math.radians(28), 0.008)
    beveled_box('Eye', (side * 0.14, 0.04, 0.451),
                (0.08, 0.06, 0.056), red, bevel=0.006)
beveled_box('Mouth', (0, -0.20, 0.421), (0.28, 0.07, 0.068), brow, bevel=0.007)
for x in (-0.07, 0.0, 0.07):
    beveled_box('Tooth', (x, -0.20, 0.459), (0.025, 0.067, 0.018), teeth, bevel=0.003)

# Join and bake transforms before exporting, leaving just one mesh in the OBJ.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = body
bpy.ops.object.join()
body.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for face in body.data.polygons:
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
scene.world = bpy.data.worlds.new('Blue gray world')
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1.0)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'


def point_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

bpy.ops.object.light_add(type='SUN', location=(-3, 4, 5))
sun = bpy.context.object
sun.name = 'Upper left sun'
sun.data.energy = 2.0
sun.data.angle = math.radians(18)
point_at(sun, (0, 0, 0))
bpy.ops.object.light_add(type='AREA', location=(3, 1, 4))
fill = bpy.context.object
fill.name = 'Soft fill'
fill.data.energy = 50
fill.data.shape = 'DISK'
fill.data.size = 5
point_at(fill, (0, 0, 0))

bpy.ops.object.camera_add(location=(0, 0, 3))
cam = bpy.context.object
scene.camera = cam
cam.data.type = 'ORTHO'
cam.data.ortho_scale = 1.32
point_at(cam, (0, 0, 0))
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)

cam.data.type = 'PERSP'
cam.data.lens = 50
cam.location = (0.88, 0.49, 1.56)
point_at(cam, (0, 0, 0))
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

