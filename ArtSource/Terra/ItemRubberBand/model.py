import bpy
import math
import os
import sys
from mathutils import Vector, Matrix

# Resolve the output folder both for normal execution and blender --python ... -- path.
try:
    OUTPUT_DIR = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    if '--' not in args or not args[args.index('--') + 1:]:
        raise RuntimeError('Pass the script path or output directory after --')
    supplied = os.path.abspath(args[args.index('--') + 1])
    OUTPUT_DIR = supplied if os.path.isdir(supplied) else os.path.dirname(supplied)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for mesh in list(bpy.data.meshes):
    if mesh.users == 0:
        bpy.data.meshes.remove(mesh)

# Warm ochre rubber, with restrained variations for its rounded edges.
def material(name, color):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.78
    return mat

palette = [material('tile_00', (0.82, 0.62, 0.24)),
           material('tile_01', (0.95, 0.75, 0.34)),
           material('tile_02', (0.72, 0.49, 0.16))]

# An elliptical ribbon with a bevelled rectangular section.  The slight
# depth undulation gives the loop an elastic, relaxed shape from either side.
segments = 32
section = [(-0.045, -0.055), (-0.032, -0.075),
           ( 0.032, -0.075), ( 0.045, -0.055),
           ( 0.045,  0.055), ( 0.032,  0.075),
           (-0.032,  0.075), (-0.045,  0.055)]
vertices = []
for i in range(segments):
    a = 2.0 * math.pi * i / segments
    ca, sa = math.cos(a), math.sin(a)
    # Offset along the ellipse normal, preserving a consistent ribbon width.
    n = Vector((ca / 0.355, sa / 0.225)).normalized()
    z = 0.025 * math.sin(2.0 * a)
    for radial, depth in section:
        vertices.append((0.355 * ca + n.x * radial,
                         0.225 * sa + n.y * radial, z + depth))
faces = []
for i in range(segments):
    for j in range(len(section)):
        faces.append((i * 8 + j, ((i + 1) % segments) * 8 + j,
                      ((i + 1) % segments) * 8 + (j + 1) % 8,
                      i * 8 + (j + 1) % 8))
mesh = bpy.data.meshes.new('Rubber band mesh')
mesh.from_pydata(vertices, [], faces)
mesh.update()
model = bpy.data.objects.new('Model', mesh)
bpy.context.collection.objects.link(model)
for mat in palette:
    mesh.materials.append(mat)
for poly in mesh.polygons:
    j = poly.index % 8
    poly.material_index = 1 if j in (0, 4) else (2 if j in (2, 6) else 0)
    poly.use_smooth = False
# Triangulate explicitly, so the OBJ and its triangle count are predictable.
bpy.context.view_layer.objects.active = model
model.select_set(True)
tri = model.modifiers.new('Triangulate', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

bpy.ops.wm.obj_export(filepath=os.path.join(OUTPUT_DIR, 'model.obj'),
                      export_selected_objects=True, export_materials=True,
                      forward_axis='Y', up_axis='Z')

# Set camera axes explicitly: +Y in the model is up in the render.
def orient_toward(obj, target):
    forward = (Vector(target) - obj.location).normalized()
    right = forward.cross(Vector((0, 1, 0))).normalized()
    up = right.cross(forward).normalized()
    rotation = Matrix(((right.x, up.x, -forward.x),
                       (right.y, up.y, -forward.y),
                       (right.z, up.z, -forward.z)))
    obj.rotation_euler = rotation.to_quaternion().to_euler()

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

sun_data = bpy.data.lights.new('Sun', 'SUN')
sun_data.energy = 2.2
sun_data.angle = math.radians(18)
sun = bpy.data.objects.new('Sun', sun_data)
bpy.context.collection.objects.link(sun)
sun.location = (-3, 4, 5)
orient_toward(sun, (0, 0, 0))
fill_data = bpy.data.lights.new('Soft fill', 'AREA')
fill_data.energy = 55
fill_data.shape = 'DISK'
fill_data.size = 4.0
fill = bpy.data.objects.new('Soft fill', fill_data)
bpy.context.collection.objects.link(fill)
fill.location = (3, 0.5, 3)
orient_toward(fill, (0, 0, 0))

cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 3)
orient_toward(cam, (0, 0, 0))
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.15
scene.render.filepath = os.path.join(OUTPUT_DIR, 'top.png')
bpy.ops.render.render(write_still=True)

cam.location = (0.68, 0.45, 1.4)
orient_toward(cam, (0, 0, 0))
cam_data.type = 'PERSP'
cam_data.lens = 50
scene.render.filepath = os.path.join(OUTPUT_DIR, 'angle.png')
bpy.ops.render.render(write_still=True)

