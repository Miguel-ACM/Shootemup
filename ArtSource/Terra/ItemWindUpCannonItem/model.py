import bpy
import math
import os
import sys
from mathutils import Vector

# The model uses game coordinates directly: Y is up and Z is the front.
if '__file__' in globals():
    OUT = os.path.dirname(os.path.abspath(__file__))
else:
    args = sys.argv
    if '--' not in args or not args[args.index('--') + 1:]:
        raise RuntimeError('Pass the script path or output directory after --')
    path = os.path.abspath(args[args.index('--') + 1])
    OUT = path if os.path.isdir(path) else os.path.dirname(path)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
    for block in list(datablocks):
        if block.users == 0:
            datablocks.remove(block)

palette = [
    (0.56, 0.20, 0.69, 1),  # enamel violet
    (0.78, 0.35, 0.85, 1),  # bright purple
    (0.38, 0.18, 0.52, 1),  # shaded plum
    (0.95, 0.65, 0.23, 1),  # warm brass
    (0.74, 0.40, 0.14, 1),  # brass shadow
    (0.97, 0.79, 0.39, 1),  # pale brass
    (0.30, 0.27, 0.40, 1),  # bore interior
    (0.72, 0.57, 0.78, 1),  # lavender metal
]
mats = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = 0.72
    mats.append(mat)

parts = []
def finish(obj, material):
    obj.data.materials.append(mats[material])
    for face in obj.data.polygons:
        face.use_smooth = False
    parts.append(obj)
    return obj

def box(loc, size, material, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = obj.modifiers.new('Broad machined corners', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return finish(obj, material)

def cylinder(loc, radius, depth, material, axis='Y', vertices=12):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, end_fill_type='NGON', location=loc)
    obj = bpy.context.object
    if axis == 'Y':
        obj.rotation_euler[0] = math.pi / 2
    elif axis == 'X':
        obj.rotation_euler[1] = math.pi / 2
    return finish(obj, material)

def tube_y(rings, material, sides=12):
    # Successive (height, radius) rings form a continuous hollow, faceted surface.
    verts = []
    for y, radius in rings:
        for i in range(sides):
            a = 2 * math.pi * i / sides + math.pi / sides
            verts.append((math.cos(a) * radius, y, math.sin(a) * radius))
    faces = []
    for j in range(len(rings) - 1):
        for i in range(sides):
            n = (i + 1) % sides
            faces.append((j*sides+i, (j+1)*sides+i, (j+1)*sides+n, j*sides+n))
    mesh = bpy.data.meshes.new('Muzzle rings')
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new('Hollow muzzle', mesh)
    bpy.context.collection.objects.link(obj)
    return finish(obj, material)

# A substantial octagonal breech and foot, with an inset lavender front plate.
box((0, -0.205, 0), (0.36, 0.36, 0.30), 0, 0.045)
box((0, -0.355, 0), (0.29, 0.12, 0.28), 2, 0.025)
box((0, -0.405, 0), (0.22, 0.07, 0.23), 3, 0.015)
box((0, -0.185, 0.157), (0.245, 0.22, 0.035), 1, 0.025)
box((0, -0.185, -0.157), (0.245, 0.22, 0.035), 1, 0.025)
# The body of the cannon is cylindrical, rather than a stack of flat panels.
cylinder((0, 0.10, 0), 0.125, 0.49, 2)
cylinder((0, 0.10, 0), 0.115, 0.47, 1)
for y in (-0.065, 0.045, 0.155, 0.265):
    cylinder((0, y, 0), 0.163, 0.045, 3)
# Thick lip and actual recessed bore, open toward +Y.
tube_y([(0.285, 0.145), (0.31, 0.19), (0.405, 0.19),
        (0.43, 0.17), (0.43, 0.105), (0.345, 0.105)], 1)
cylinder((0, 0.342, 0), 0.104, 0.008, 6)
cylinder((0, 0.315, 0), 0.193, 0.035, 4)
# Re-expose the purple muzzle above its narrow brass lower collar.
# An asymmetric winding key projects from the left, on a sturdy front axle.
cylinder((-0.205, -0.20, 0.0), 0.12, 0.08, 4, axis='X', vertices=10)
cylinder((-0.258, -0.20, 0.0), 0.095, 0.05, 3, axis='X', vertices=10)
box((-0.315, -0.20, 0.0), (0.14, 0.065, 0.085), 3, 0.015)
cylinder((-0.375, -0.20, 0.035), 0.07, 0.13, 5, axis='Z', vertices=10)
# Matching rear plate and rivets keep the rotating pickup finished on all sides.
for z in (-0.181, 0.181):
    for x in (-0.085, 0.085):
        cylinder((x, -0.185, z), 0.022, 0.012, 5, axis='Z', vertices=8)

bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
# Triangulation is applied so export and the in-game budget are deterministic.
tri = model.modifiers.new('Triangulate', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
for face in model.data.polygons:
    face.use_smooth = False
assert 300 <= len(model.data.polygons) <= 2000, len(model.data.polygons)

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
background.inputs['Strength'].default_value = 1
scene.view_settings.view_transform = 'Standard'

def aim(obj, target=(0, 0, 0)):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, loc, energy, size=1):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if kind == 'AREA':
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    obj.location = loc
    aim(obj)
    return obj

light('Upper left sun', 'SUN', (-3, 5, 4), 2.0)
light('Soft fill', 'AREA', (4, 1, 4), 180, 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 4)
aim(cam)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.15
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (1.55, 1.05, 2.32)
aim(cam)
cam_data.type = 'PERSP'
cam_data.lens = 50
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

