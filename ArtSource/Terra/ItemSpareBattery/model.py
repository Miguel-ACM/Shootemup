import bpy
import math
import os
import sys
from mathutils import Vector

# Resolve output alongside this script, including Blender's -- script-path fallback.
try:
    SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    SCRIPT_DIR = os.path.dirname(os.path.abspath(args[args.index('--') + 1])) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablock in bpy.data.materials:
    bpy.data.materials.remove(datablock)

palette = [
    (0.67, 0.76, 0.16, 1), # citron battery shell
    (0.83, 0.88, 0.29, 1), # lighter shell ends
    (0.33, 0.42, 0.12, 1), # olive straps
    (0.77, 0.81, 0.69, 1), # warm metal
    (0.94, 0.36, 0.12, 1), # rocket vermilion
    (0.98, 0.67, 0.18, 1), # rocket nose
    (0.47, 0.54, 0.48, 1), # nozzle steel
    (0.97, 0.91, 0.55, 1), # charge marking
    (0.23, 0.30, 0.20, 1), # inset detail
]
mats = []
for i, color in enumerate(palette):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = color
    m.use_nodes = True
    m.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value = color
    m.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value = 0.8
    mats.append(m)

parts = []
def finish(obj, mat):
    obj.data.materials.append(mats[mat])
    for face in obj.data.polygons:
        face.use_smooth = False
    parts.append(obj)
    return obj

def box(name, loc, size, mat, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = obj.modifiers.new('Broad machined corners', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return finish(obj, mat)

def cylinder(name, loc, radius, depth, mat, axis='Y', vertices=12, radius2=None):
    # Blender's cone and cylinder primitives are built along Z.
    if radius2 is None:
        bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc)
    else:
        bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=radius, radius2=radius2, depth=depth, location=loc)
    obj = bpy.context.object
    obj.name = name
    if axis == 'Y':
        obj.rotation_euler[0] = -math.pi / 2
    elif axis == 'X':
        obj.rotation_euler[1] = math.pi / 2
    return finish(obj, mat)

def prism(name, points, z0, z1, mat):
    n = len(points)
    verts = [(x,y,z0) for x,y in points] + [(x,y,z1) for x,y in points]
    faces = [tuple(reversed(range(n))), tuple(range(n,2*n))]
    faces += [(i, (i+1)%n, (i+1)%n+n, i+n) for i in range(n)]
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return finish(obj, mat)

# A wide, bevelled battery with its positive terminal on the right.
box('Battery housing', (-0.025, 0.0, -0.06), (0.70, 0.33, 0.34), 0, 0.055)
box('Negative end cap', (-0.367, 0.0, -0.06), (0.075, 0.30, 0.33), 1, 0.025)
box('Positive end cap', (0.315, 0.0, -0.06), (0.075, 0.30, 0.33), 1, 0.025)
cylinder('Positive terminal', (0.397, 0.0, -0.06), 0.085, 0.09, 3, 'X', 10)
cylinder('Negative contact', (-0.412, 0.0, -0.06), 0.09, 0.035, 3, 'X', 10)
# Straps wrap around the cell and hold the rocket in place.
for x in (-0.20, 0.20):
    box('Securing band', (x, 0, -0.06), (0.07, 0.35, 0.36), 2, 0.018)
# Simple plus marks at the positive end on both broad sides.
for z in (-0.238, 0.118):
    box('Positive mark vertical', (0.288, 0, z), (0.038, 0.115, 0.014), 7, 0.004)
    box('Positive mark horizontal', (0.288, 0, z), (0.105, 0.038, 0.014), 7, 0.004)
# Vertical rocket on the front: faceted body, pointed nose, tail and swept fins.
cylinder('Rocket body', (0, 0.025, 0.235), 0.105, 0.54, 4, 'Y', 12)
cylinder('Rocket collar', (0, -0.185, 0.235), 0.113, 0.075, 3, 'Y', 12)
cylinder('Rocket nose', (0, 0.365, 0.235), 0.105, 0.14, 5, 'Y', 12, 0.0)
cylinder('Rocket nozzle', (0, -0.315, 0.235), 0.105, 0.13, 6, 'Y', 10, 0.065)
for side in (-1, 1):
    pts = [(side*0.075, -0.14), (side*0.225, -0.38), (side*0.09, -0.35)]
    if side < 0:
        pts.reverse()
    prism('Swept tail fin', pts, 0.19, 0.28, 4)
# A broad band and raised center panel make the rocket legible at icon size.
cylinder('Rocket stripe', (0, 0.075, 0.235), 0.108, 0.10, 7, 'Y', 12)
box('Rocket guidance panel', (0, 0.12, 0.34), (0.085, 0.17, 0.024), 5, 0.012)
# A rear charge glyph keeps the battery recognizable while rotating.
bolt = [(-0.08, 0.12), (0.015, 0.12), (-0.02, 0.015), (0.07, 0.015), (-0.06, -0.12), (-0.025, -0.025), (-0.10, -0.025)]
prism('Rear charge symbol', bolt, -0.242, -0.225, 7)

# Apply transforms and join all pieces into the one exported object.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for poly in model.data.polygons:
    poly.use_smooth = False
bpy.ops.object.select_all(action='DESELECT')
model.select_set(True)
bpy.context.view_layer.objects.active = model
bpy.ops.wm.obj_export(filepath=os.path.join(SCRIPT_DIR, 'model.obj'), export_selected_objects=True, export_materials=True, forward_axis='Y', up_axis='Z')

# Render only after export, so camera and lights never enter the asset.
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 512
scene.render.resolution_y = 512
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.render.film_transparent = False
world = bpy.data.worlds.new('Blue gray world') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world = world
world.use_nodes = True
world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def point_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, loc, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = loc
    point_at(obj, target)
    return obj

light('Upper left sun', 'SUN', (-3, 5, 6), 2.0, (0, 0, 0))
light('Soft fill', 'AREA', (4, 1, 4), 45, (0, 0, 0), 5.0)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 4)
point_at(cam, (0, 0, 0))
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.10
scene.render.filepath = os.path.join(SCRIPT_DIR, 'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam_data.lens = 50
cam.location = (0.86, 0.52, 1.46)
point_at(cam, (0, 0, 0))
scene.render.filepath = os.path.join(SCRIPT_DIR, 'angle.png')
bpy.ops.render.render(write_still=True)

