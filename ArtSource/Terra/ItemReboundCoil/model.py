import bpy
import math
import os
import sys
from mathutils import Vector

# Outputs live alongside this script, including when launched with blender -- ...
try:
    OUT_DIR = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    if '--' not in args or not args[args.index('--') + 1:]:
        raise RuntimeError('Pass the script path after -- when __file__ is unavailable')
    OUT_DIR = os.path.dirname(os.path.abspath(args[args.index('--') + 1]))

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)


def material(index, color):
    mat = bpy.data.materials.new('tile_%02d' % index)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.65
    bsdf.inputs['Metallic'].default_value = 0.0
    return mat

purple = material(0, (0.65, 0.28, 0.91))
light_purple = material(1, (0.83, 0.56, 1.0))
plate = material(2, (0.36, 0.25, 0.59))
edge = material(3, (0.55, 0.41, 0.76))
gold = material(4, (1.0, 0.68, 0.20))
core = material(5, (0.68, 0.75, 0.84))
ball_mat = material(6, (1.0, 0.81, 0.33))

parts = []

def cylinder(name, radius, depth, center, mat, vertices=12, axis='Y'):
    rotation = (math.pi / 2, 0, 0) if axis == 'Y' else (0, 0, 0)
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth,
                                       end_fill_type='NGON', location=center, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    parts.append(obj)
    return obj

# A continuous, faceted wire spring. Its Y-axis helix has a generously sized
# cross section so the individual turns remain readable in the small HUD icon.
turns = 3.5
segments = 56
sides = 8
radius = 0.226
wire = 0.046
verts = []
faces = []
for i in range(segments + 1):
    t = i / segments
    a = 2 * math.pi * turns * t
    center = Vector((radius * math.cos(a), -0.325 + 0.65 * t, radius * math.sin(a)))
    radial = Vector((math.cos(a), 0, math.sin(a)))
    tangent = Vector((-radius * math.sin(a) * 2 * math.pi * turns,
                      0.65, radius * math.cos(a) * 2 * math.pi * turns)).normalized()
    binormal = tangent.cross(radial).normalized()
    for j in range(sides):
        angle = 2 * math.pi * j / sides
        p = center + wire * (math.cos(angle) * radial + math.sin(angle) * binormal)
        verts.append(tuple(p))
for i in range(segments):
    for j in range(sides):
        a = i * sides + j
        b = i * sides + (j + 1) % sides
        faces.append((a, b, b + sides, a + sides))
faces.append(tuple(reversed(range(sides))))
faces.append(tuple(segments * sides + j for j in range(sides)))
mesh = bpy.data.meshes.new('Helical spring mesh')
mesh.from_pydata(verts, [], faces)
mesh.materials.append(purple)
mesh.update()
spring = bpy.data.objects.new('Continuous spring', mesh)
bpy.context.collection.objects.link(spring)
parts.append(spring)

# Guide piston and stepped, octagonal end plates read as a complete mechanism
# from every direction. Each collar is tucked into the spring ends.
cylinder('Piston', 0.068, 0.69, (0, 0, 0), core, 10)
for sign in (-1, 1):
    cylinder('Inner collar', 0.13, 0.07, (0, sign * 0.315, 0), gold, 10)
    cylinder('End plate', 0.32, 0.09, (0, sign * 0.385, 0), plate, 8)
    cylinder('Plate rim', 0.325, 0.026, (0, sign * 0.432, 0), light_purple, 8)
    cylinder('Plate center', 0.155, 0.025, (0, sign * 0.45, 0), edge, 10)
    cylinder('Plate button', 0.082, 0.032, (0, sign * 0.465, 0), gold, 10)

# A warm faceted ball gives the spring its ricochet cue. Duplicate on the
# reverse face so the item stays legible during its complete rotation.
for sign in (-1, 1):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=0.092,
                                         location=(0, 0.015, sign * 0.245))
    obj = bpy.context.object
    obj.name = 'Rebound ball'
    obj.data.materials.append(ball_mat)
    parts.append(obj)

bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
bpy.context.view_layer.objects.active = spring
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
# Bake position as well as orientation and scale into the mesh.
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for poly in model.data.polygons:
    poly.use_smooth = False

bpy.ops.wm.obj_export(filepath=os.path.join(OUT_DIR, 'model.obj'),
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

bpy.ops.object.light_add(type='SUN', location=(-3, 5, 6))
sun = bpy.context.object
sun.name = 'Upper left sun'
sun.data.energy = 2.0
sun.data.angle = math.radians(18)
point_at(sun)
bpy.ops.object.light_add(type='AREA', location=(4, 1, 5))
fill = bpy.context.object
fill.name = 'Soft fill'
fill.data.energy = 160
fill.data.shape = 'DISK'
fill.data.size = 5
point_at(fill)

bpy.ops.object.camera_add(location=(0, 0, 4))
camera = bpy.context.object
camera.name = 'Camera'
scene.camera = camera
point_at(camera)
camera.data.type = 'ORTHO'
camera.data.ortho_scale = 1.15
scene.render.filepath = os.path.join(OUT_DIR, 'top.png')
bpy.ops.render.render(write_still=True)

camera.location = (1.9, 1.17, 2.65)
point_at(camera)
camera.data.type = 'PERSP'
camera.data.lens = 50
scene.render.filepath = os.path.join(OUT_DIR, 'angle.png')
bpy.ops.render.render(write_still=True)

