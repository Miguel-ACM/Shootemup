import bpy
import math
import os
import sys
from mathutils import Vector

# Blender 5.x. The pickup lies in XY, with its front toward +Z.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
    if not os.path.isdir(OUT):
        OUT = os.path.dirname(OUT)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)


def material(name, rgb):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*rgb, 1.0)
    mat.use_nodes = True
    mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value = (*rgb, 1.0)
    mat.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value = 0.8
    return mat

skin = material('tile_00', (226/255, 170/255, 128/255))
sleeve = material('tile_01', (62/255, 66/255, 74/255))
parts = []


def finish(obj, mat):
    obj.data.materials.append(mat)
    for face in obj.data.polygons:
        face.use_smooth = False
    parts.append(obj)
    return obj


def tube(a, b, radius, mat, vertices=10):
    a, b = Vector(a), Vector(b)
    delta = b - a
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=delta.length, location=(a+b)/2)
    obj = bpy.context.object
    obj.rotation_mode = 'QUATERNION'
    obj.rotation_quaternion = delta.to_track_quat('Z', 'Y')
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    bevel = obj.modifiers.new('Soft faceted ends', 'BEVEL')
    bevel.width = 0.018
    bevel.segments = 2
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    return finish(obj, mat)


def ball(pos, scale, mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=10, ring_count=6, location=pos)
    obj = bpy.context.object
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(obj, mat)


def rounded_box(center, size, mat, angle=0.0, bevel_width=0.015):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    obj = bpy.context.object
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bevel = obj.modifiers.new('Rounded edges', 'BEVEL')
    bevel.width = bevel_width
    bevel.segments = 2
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    obj.rotation_euler[2] = angle
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    return finish(obj, mat)

# Same bent-arm construction and skin/sleeve colours as the shipped item.
tube((-0.44, 0.34, 0), (-0.32, 0.22, 0), 0.13, sleeve)
tube((-0.32, 0.22, 0), (0, -0.08, 0), 0.10, skin)
ball((0, -0.08, 0), (0.10, 0.10, 0.10), skin)
tube((0, -0.08, 0), (0.30, 0.20, 0), 0.09, skin)

# Rotate the complete palm and fingers around the same pivot as the source.
angle = math.radians(-40)
pivot = Vector((0.36, 0.30, 0))
def hand_point(x, y):
    dx, dy = x-pivot.x, y-pivot.y
    return (pivot.x + math.cos(angle)*dx - math.sin(angle)*dy,
            pivot.y + math.sin(angle)*dx + math.cos(angle)*dy, 0)

rounded_box(hand_point(0.38, 0.30), (0.16, 0.18, 0.10), skin, angle, 0.022)
for k in range(4):
    rounded_box(hand_point(0.32 + k*0.045, 0.44), (0.035, 0.12, 0.07), skin, angle, 0.012)
rounded_box(hand_point(0.27, 0.30), (0.04, 0.12, 0.07), skin, angle, 0.014)

# Join and center the overall bounds, scaling uniformly to the requested size.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
coords = [v.co for v in model.data.vertices]
lo = Vector(tuple(min(v[i] for v in coords) for i in range(3)))
hi = Vector(tuple(max(v[i] for v in coords) for i in range(3)))
mid = (lo + hi) * 0.5
factor = 0.90 / max(hi.x-lo.x, hi.y-lo.y)
for v in model.data.vertices:
    v.co = (v.co - mid) * factor
model.data.update()

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
scene.world = bpy.data.worlds.new('Blue gray world')
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(obj, point=(0, 0, 0)):
    obj.rotation_euler = (Vector(point) - obj.location).to_track_quat('-Z', 'Y').to_euler()

bpy.ops.object.light_add(type='SUN', location=(-3, 5, 6))
sun = bpy.context.object
sun.name = 'Sun'
sun.data.energy = 2.0
sun.data.angle = math.radians(18)
aim(sun)
bpy.ops.object.light_add(type='AREA', location=(4, 1, 4))
fill = bpy.context.object
fill.name = 'Fill'
fill.data.energy = 80
fill.data.shape = 'DISK'
fill.data.size = 5
aim(fill)

bpy.ops.object.camera_add(location=(0, 0, 4))
cam = bpy.context.object
scene.camera = cam
cam.data.type = 'ORTHO'
cam.data.ortho_scale = 1.22
aim(cam)
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam.data.type = 'PERSP'
cam.data.lens = 50
cam.location = (1.6, 1.15, 2.8)
aim(cam)
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)
