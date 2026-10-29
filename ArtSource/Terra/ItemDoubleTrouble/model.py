import bpy
import math
import os
import sys
from mathutils import Vector

# The game uses Y for vertical and views the front from positive Z.
def script_directory():
    if '__file__' in globals():
        return os.path.dirname(os.path.abspath(__file__))
    if '--' in sys.argv:
        args = sys.argv[sys.argv.index('--') + 1:]
        if args:
            return os.path.dirname(os.path.abspath(args[0]))
    return os.getcwd()

OUT = script_directory()
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

palette = [
    (0.92, 0.43, 0.075, 1),  # warm orange grip
    (1.00, 0.67, 0.19, 1),  # gold housing
    (0.70, 0.27, 0.055, 1), # inset grip panels
    (0.95, 0.12, 0.10, 1),  # trigger red
    (0.96, 0.76, 0.34, 1),  # guard / linkage brass
    (0.28, 0.34, 0.39, 1),  # steel joint
    (0.58, 0.65, 0.66, 1),  # steel face
    (1.00, 0.82, 0.42, 1),  # golden edge details
]
materials = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = 0.68
    materials.append(mat)

parts = []
def finish(obj, mat):
    obj.data.materials.append(materials[mat])
    for poly in obj.data.polygons:
        poly.use_smooth = False
    parts.append(obj)
    return obj

def bevel(obj, amount, segments=1):
    mod = obj.modifiers.new('Solid edge bevel', 'BEVEL')
    mod.width = amount
    mod.segments = segments
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=mod.name)

def box(loc, size, mat, edge=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if edge:
        bevel(obj, edge)
    return finish(obj, mat)

def prism(points, depth, z, mat, edge=0.0):
    n = len(points)
    verts = [(x, y, z - depth / 2) for x, y in points] + [(x, y, z + depth / 2) for x, y in points]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2*n))]
    faces += [(i, (i+1)%n, (i+1)%n+n, i+n) for i in range(n)]
    mesh = bpy.data.meshes.new('profile')
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new('profile', mesh)
    bpy.context.collection.objects.link(obj)
    if edge:
        bevel(obj, edge)
    return finish(obj, mat)

def rod(a, b, radius, mat, vertices=8):
    a, b = Vector(a), Vector(b)
    delta = b - a
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=delta.length, location=(a+b)/2)
    obj = bpy.context.object
    obj.rotation_euler = delta.to_track_quat('Z', 'Y').to_euler()
    return finish(obj, mat)

# Mirrored, slightly flared pistol grips.  The separate broad inset panels are
# on both faces, so the reverse has the same clear visual hierarchy.
for side in (-1, 1):
    def mirrored(points):
        p = [(side*x, y) for x, y in points]
        return p if side == 1 else list(reversed(p))
    prism(mirrored([(0.145, 0.16), (0.335, 0.16), (0.405, -0.35),
                    (0.375, -0.43), (0.235, -0.45), (0.195, -0.39)]),
          0.235, 0, 0, 0.018)
    for face in (-1, 1):
        prism(mirrored([(0.204, 0.035), (0.300, 0.045), (0.350, -0.335),
                        (0.320, -0.371), (0.255, -0.382)]),
              0.018, face * 0.126, 2, 0.006)
        # Two stout angled bars give the panel a grip texture without noise.
        for y in (-0.16, -0.27):
            rod((side*0.235, y+0.012, face*0.139),
                (side*0.315, y+0.030, face*0.139), 0.012, 1, 6)
    # Broad receiver cap, raised ridge and heel reinforce the outline.
    box((side*0.235, 0.230, 0), (0.36, 0.17, 0.30), 1, 0.025)
    box((side*0.235, 0.326, 0), (0.29, 0.035, 0.23), 7, 0.008)
    box((side*0.318, -0.416, 0), (0.19, 0.065, 0.255), 1, 0.012)
    # Open, thick trigger guard sits inward of each grip, through the depth.
    # Its gap is deliberately large and the red trigger is set inside it.
    guard = [(side*0.150, 0.130, 0), (side*0.045, 0.095, 0),
             (side*0.038, -0.145, 0), (side*0.090, -0.225, 0),
             (side*0.215, -0.235, 0)]
    for a, b in zip(guard, guard[1:]):
        rod(a, b, 0.026, 4)
    # Trigger is a broad hooked profile, visible from either face.
    prism(mirrored([(0.150, 0.105), (0.187, 0.100), (0.175, -0.050),
                    (0.137, -0.100), (0.115, -0.075), (0.140, -0.035)]),
          0.085, 0, 3, 0.005)
    # Receiver pins are visible on front and back.
    for face in (-1, 1):
        rod((side*0.285, 0.235, face*0.151),
            (side*0.285, 0.235, face*0.162), 0.035, 6, 8)

# A clearly mechanical bridge communicates the linked triggers.
rod((-0.155, 0.235, 0), (0.155, 0.235, 0), 0.065, 5, 10)
box((0, 0.235, 0), (0.125, 0.13, 0.325), 4, 0.020)
for face in (-1, 1):
    rod((0, 0.235, face*0.164), (0, 0.235, face*0.174), 0.038, 3, 8)

# Consolidate all geometry; no object transforms or modifiers survive export.
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
scene.render.film_transparent = False
scene.world = bpy.data.worlds.new('Blue gray studio')
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(obj, target=(0, 0, 0)):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(kind, loc, energy, size=None):
    data = bpy.data.lights.new(kind, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(kind, data)
    scene.collection.objects.link(obj)
    obj.location = loc
    aim(obj)

light('SUN', (-3, 5, 6), 2.0)
light('AREA', (4, 1, 5), 160, 5.0)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, -0.045, 4)
aim(cam, (0, -0.045, 0))
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.08
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam_data.lens = 50
cam.location = (0.80, 0.56, 1.38)
aim(cam, (0, -0.035, 0))
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

