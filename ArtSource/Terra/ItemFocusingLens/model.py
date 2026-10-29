import bpy
import math
import os
import sys
from mathutils import Vector

# The export uses the game's coordinates directly: Y up, Z toward the viewer.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
    if os.path.isfile(OUT):
        OUT = os.path.dirname(OUT)
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for mat in list(bpy.data.materials):
    bpy.data.materials.remove(mat)

palette = [
    (0.24, 0.68, 0.72, 1), # turquoise body
    (0.40, 0.89, 0.91, 1), # bright aqua rim
    (0.12, 0.47, 0.55, 1), # petrol blue recess
    (0.22, 0.71, 0.79, 1), # glass outer facets
    (0.39, 0.88, 0.91, 1), # glass center
    (0.67, 0.98, 0.96, 1), # lens glint
    (0.63, 0.80, 0.76, 1), # pale metal
    (0.16, 0.56, 0.63, 1), # grip
    (0.92, 0.74, 0.33, 1), # warm calibration marks
]
mats = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = 0.62
    mats.append(mat)

parts = []
def mesh_part(name, vertices, faces, indices):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.materials.clear()
    for m in mats:
        mesh.materials.append(m)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    for poly, idx in zip(mesh.polygons, indices):
        poly.material_index = idx
        poly.use_smooth = False
    parts.append(obj)
    return obj

# Cross-section through the objective: a thick scallop-edged housing with
# inset, slightly convex, opaque glass on both faces. Each loop is (radius, z).
def objective():
    n = 16
    cy = 0.155
    loops = [(0.0,-0.168),(0.11,-0.165),(0.225,-0.151),
             (0.253,-0.135),(0.263,-0.153),(0.31,-0.153),
             (0.342,-0.120),(0.35,-0.085),(0.35,0.085),
             (0.342,0.120),(0.31,0.153),(0.263,0.153),
             (0.253,0.135),(0.225,0.151),(0.11,0.165),(0.0,0.168)]
    verts = []
    for rad, z in loops:
        for j in range(n):
            a = 2 * math.pi * j / n
            verts.append((rad * math.cos(a), cy + rad * math.sin(a), z))
    faces, ids = [], []
    # Alternating broad lens facets read as glass without transparency.
    band_mats = [4,3,3,2,1,0,1,0,1,0,1,2,3,3,4]
    for k in range(len(loops)-1):
        for j in range(n):
            faces.append((k*n+j, k*n+(j+1)%n, (k+1)*n+(j+1)%n, (k+1)*n+j))
            idx = band_mats[k]
            if k in (1, 2, 12, 13) and j in (4, 5, 6):
                idx = 4
            ids.append(idx)
    mesh_part('Faceted objective', verts, faces, ids)

objective()

def bevel_box(name, location, scale, material, bevel=0.025):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mats[material])
    mod = obj.modifiers.new('Cut corners', 'BEVEL')
    mod.width = bevel
    mod.segments = 1
    mod.affect = 'EDGES'
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=mod.name)
    for p in obj.data.polygons:
        p.use_smooth = False
    parts.append(obj)
    return obj

# The handle overlaps the housing, giving a solid, readable silhouette.
bevel_box('Lens neck', (0,-0.235,0), (0.20,0.34,0.20), 0, 0.035)
bevel_box('Grip band', (0,-0.305,0), (0.225,0.095,0.225), 7, 0.025)
bevel_box('Eyepiece', (0,-0.405,0), (0.32,0.13,0.255), 1, 0.032)
bevel_box('Eyepiece inset', (0,-0.447,0), (0.23,0.045,0.18), 2, 0.012)

# Two substantial highlight facets on each lens face remain legible at HUD size.
def glint(sign):
    z = sign * 0.169
    v = [(-0.17,0.225,z),(-0.12,0.275,z),(-0.025,0.30,z),(-0.075,0.25,z)]
    face = (0,1,2,3) if sign > 0 else (3,2,1,0)
    mesh_part('Glass reflection', v, [face], [5])
for side in (-1,1):
    glint(side)

# A pair of broad warm calibration tabs on the outer frame.
for x in (-0.285, 0.285):
    for z in (-0.156, 0.156):
        bevel_box('Calibration tab', (x,0.155,z), (0.045,0.075,0.018), 8, 0.006)

bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for p in model.data.polygons:
    p.use_smooth = False

# Export only the model, before introducing lights and cameras.
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

def aim(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    aim(obj, target)
    return obj

light('Upper left sun', 'SUN', (-3, 5, 6), 2.0, (0,0,0))
light('Soft fill', 'AREA', (4, 1, 4), 100, (0,0,0), 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 4)
aim(cam, (0,0,0))
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.15
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)

cam_data.type = 'PERSP'
cam_data.lens = 50
cam.location = (0.90, 0.50, 1.55)
aim(cam, (0,0,0))
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

