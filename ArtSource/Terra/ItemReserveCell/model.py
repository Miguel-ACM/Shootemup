import bpy
import math
import os
import sys
from mathutils import Vector

# The item uses game coordinates directly: Y up, Z forward.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
    if os.path.splitext(OUT)[1]:
        OUT = os.path.dirname(OUT)
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
    for block in list(datablocks):
        if block.users == 0:
            datablocks.remove(block)

palette = [
    (0.19, 0.66, 0.24, 1),  # cell green
    (0.32, 0.88, 0.37, 1),  # bright green panels
    (0.20, 0.34, 0.34, 1),  # protective cage, blue slate
    (0.43, 0.60, 0.58, 1),  # metal edging
    (0.92, 0.78, 0.20, 1),  # energy bolt
    (0.98, 0.43, 0.16, 1),  # positive terminal
    (0.70, 0.81, 0.70, 1),  # other terminal
    (0.12, 0.42, 0.24, 1),  # inset panel surround
]
materials = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = 0.7
    materials.append(mat)

parts = []
def finish(obj, mat):
    obj.data.materials.append(materials[mat])
    for face in obj.data.polygons:
        face.use_smooth = False
    parts.append(obj)
    return obj

def box(name, loc, dims, mat, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dims
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = obj.modifiers.new('Broad machined corners', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return finish(obj, mat)

def cylinder(name, loc, radius, depth, mat, vertices=8):
    # Blender cylinders start along Z; tip them to the game's Y axis.
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth,
                                       end_fill_type='NGON', location=loc,
                                       rotation=(math.pi / 2, 0, 0))
    return finish(bpy.context.object, mat)

def bolt(name, z, front=True):
    # A thick, unmistakable lightning mark; the back gets the same readable mark.
    xy = [(-0.015, 0.174), (0.118, 0.174), (0.031, 0.020),
          (0.115, 0.020), (-0.094, -0.195), (-0.045, -0.052),
          (-0.122, -0.052)]
    half = 0.012
    verts = [(x, y, z-half) for x,y in xy] + [(x, y, z+half) for x,y in xy]
    n = len(xy)
    faces = [tuple(reversed(range(n))), tuple(range(n, 2*n))]
    faces += [(i, (i+1)%n, (i+1)%n+n, i+n) for i in range(n)]
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return finish(obj, 4)

# A beveled, substantial cell inside a four-corner open frame.
box('Storage cell', (0, -0.035, 0), (0.53, 0.55, 0.40), 0, 0.055)
# Front and rear inset field, with a bold raised charge symbol.
for s in (-1, 1):
    box('Panel surround', (0, -0.035, s*0.206), (0.40, 0.45, 0.035), 7, 0.035)
    box('Green face', (0, -0.035, s*0.227), (0.35, 0.40, 0.022), 1, 0.025)
    bolt('Energy emblem', s*0.249)
# Protective pillars on all four corners; visible openings between the rails.
for x in (-0.285, 0.285):
    for z in (-0.205, 0.205):
        box('Cage upright', (x, -0.025, z), (0.082, 0.62, 0.085), 2, 0.015)
# Solid shoes unify the silhouette and make the frame feel functional.
box('Lower cage shoe', (0, -0.345, 0), (0.68, 0.095, 0.49), 3, 0.025)
box('Upper cage yoke', (0, 0.275, 0), (0.68, 0.09, 0.49), 3, 0.025)
# Layered asymmetric contacts distinguish this as a reserve power cell.
box('Square terminal base', (-0.16, 0.335, 0), (0.19, 0.055, 0.20), 2, 0.012)
box('Square terminal', (-0.16, 0.385, 0), (0.12, 0.075, 0.14), 6, 0.012)
cylinder('Round terminal base', (0.16, 0.335, 0), 0.105, 0.055, 2)
cylinder('Round positive terminal', (0.16, 0.390, 0), 0.070, 0.09, 5)
# Broad side inserts preserve the green identity during the spin.
for s in (-1, 1):
    box('Side charge panel', (s*0.269, -0.035, 0), (0.025, 0.34, 0.24), 1, 0.012)
    box('Side band', (s*0.286, -0.035, 0), (0.025, 0.065, 0.25), 3, 0.008)

# One flat-shaded, triangulated export object, with all transforms baked.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
tri = model.modifiers.new('Triangulate for game', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
for face in model.data.polygons:
    face.use_smooth = False
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
scene.world = bpy.data.worlds.new('Blue gray studio') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
background.inputs['Strength'].default_value = 1.0

def aim(obj, target=(0, 0, 0)):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, loc, energy, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    obj.location = loc
    aim(obj)
    return obj

sun = light('Upper left sun', 'SUN', (-3, 5, 6), 2.0)
sun.data.angle = math.radians(18)
light('Soft fill', 'AREA', (4, 1, 5), 180, 5.0)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 4)
aim(cam)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.10
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (0.88, 0.59, 1.35)
aim(cam)
cam_data.type = 'PERSP'
cam_data.lens = 50
cam_data.sensor_width = 36
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

