import bpy
import json
import math
import os
import sys
from mathutils import Vector

# Run with: blender --background --python model.py [-- output_directory]
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    if '--' not in sys.argv or not sys.argv[sys.argv.index('--') + 1:]:
        raise RuntimeError('Supply the output directory after -- when __file__ is unavailable')
    OUT = os.path.abspath(sys.argv[sys.argv.index('--') + 1])
os.makedirs(OUT, exist_ok=True)
# The approved bell has no naturally moving components.
with open(os.path.join(OUT, 'parts.json'), 'w', encoding='utf-8') as parts_file:
    json.dump([], parts_file)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

colors = [(0.24,0.35,0.40),(0.78,0.83,0.79),(0.35,0.43,0.30),
          (0.95,0.38,0.12),(0.32,0.48,0.55),(0.54,0.18,0.16),
          (0.53,0.61,0.62),(0.34,0.31,0.48),(0.20,0.36,0.40),
          (0.16,0.83,0.91),(1.0,0.77,0.27),(0.055,0.09,0.12),
          (0.62,0.25,0.19),(0.82,0.64,0.26),(0.53,0.61,0.55),
          (0.80,0.84,0.83)]
mats = []
for i, rgb in enumerate(colors):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = (*rgb, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*rgb, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.65
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*rgb, 1.0)
        bsdf.inputs['Emission Strength'].default_value = 1.5
    mats.append(mat)

parts = []
def mesh(name, verts, faces, material):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mats[material])
    parts.append(obj)
    return obj

def ringbody(name, rings, sides, material, phase=0):
    verts = []
    for rx, ry, z, cx, cy in rings:
        for i in range(sides):
            a = 2 * math.pi * i / sides + phase
            verts.append((cx + rx * math.cos(a), cy + ry * math.sin(a), z))
    faces = [tuple(reversed(range(sides)))]
    for j in range(len(rings)-1):
        for i in range(sides):
            n = (i+1) % sides
            faces.append((j*sides+i, j*sides+n, (j+1)*sides+n, (j+1)*sides+i))
    faces.append(tuple((len(rings)-1)*sides+i for i in range(sides)))
    return mesh(name, verts, faces, material)

def plate(name, outline, z0, z1, material):
    n = len(outline)
    verts = [(x,y,z0) for x,y in outline] + [(x,y,z1) for x,y in outline]
    faces = [tuple(reversed(range(n))), tuple(range(n,2*n))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    obj = mesh(name, verts, faces, material)
    mod = obj.modifiers.new('Crisp bevel', 'BEVEL')
    mod.width = 0.025
    mod.segments = 1
    return obj

# Layered pressure bell: a broad lower rim and a faceted, tapered crown.
ringbody('lower dark keel', [(0.57,0.58,-0.19,0,0.02),(0.66,0.65,-0.10,0,0.02),
                            (0.66,0.65,-0.02,0,0.02)], 16, 11)
ringbody('brass rim', [(0.66,0.65,-0.035,0,0.02),(0.665,0.655,0.005,0,0.02),
                       (0.64,0.63,0.045,0,0.02)], 16, 13)
ringbody('bell crown', [(0.64,0.63,0.025,0,0.02),(0.59,0.58,0.13,0,0.02),
                        (0.46,0.45,0.23,0,0.02),(0.27,0.265,0.29,0,0.02),
                        (0.075,0.075,0.31,0,0.02)], 16, 4)

# A continuous coiled raised band, its height following the bell's faceted crown.
def crown_height(r):
    stops = [(0,0.31),(0.075,0.31),(0.27,0.29),(0.46,0.23),(0.59,0.13),(0.64,0.025)]
    for (r0,z0),(r1,z1) in zip(stops, stops[1:]):
        if r <= r1:
            return z0 + (z1-z0)*(r-r0)/(r1-r0)
    return 0.025
verts = []
steps = 112
for i in range(steps+1):
    t = i / steps
    a = -0.4 + t * math.pi * 3.8
    r = 0.055 + 0.525 * t
    w = 0.018 + 0.015*t
    for dr in (-w,w):
        rr = r+dr
        verts.append((rr*math.cos(a), 0.02+rr*0.98*math.sin(a), crown_height(rr)+0.014))
faces = [(2*i,2*i+1,2*i+3,2*i+2) for i in range(steps)]
coil = mesh('raised brass nautilus spiral', verts, faces, 13)
solid = coil.modifiers.new('Raised band', 'SOLIDIFY')
solid.thickness = 0.012

# Broad armored cheeks frame the two launchers and define the forward silhouette.
for s in (-1,1):
    outline = [(s*x,y) for x,y in [(0.30,-0.10),(0.62,-0.20),(0.70,-0.34),
                                   (0.59,-0.68),(0.34,-0.60),(0.29,-0.39)]]
    if s < 0: outline.reverse()
    plate('weapon shoulder', outline, -0.13, 0.075, 8)
    inset = [(s*x,y) for x,y in [(0.37,-0.23),(0.61,-0.29),(0.55,-0.53),(0.37,-0.49)]]
    if s < 0: inset.reverse()
    plate('shoulder steel armor', inset, 0.075, 0.105, 6)

# Annular horizontal ports: the forward face is exactly at the gameplay muzzle.
def muzzle(x):
    n = 12
    # Axis is Y; mouth at y=-0.51. Dark recessed bore remains visible from above and front.
    rings = [(0.095,-0.39),(0.095,-0.51),(0.059,-0.51),(0.059,-0.465)]
    z = 0.145
    verts = [(x+r*math.cos(2*math.pi*i/n), y, z+r*math.sin(2*math.pi*i/n))
             for r,y in rings for i in range(n)]
    faces = []
    for j in range(3):
        for i in range(n):
            k = (i+1)%n
            faces.append((j*n+i,j*n+k,(j+1)*n+k,(j+1)*n+i))
    faces.append(tuple(3*n+i for i in range(n)))
    obj = mesh('brass launcher and hollow mouth', verts, faces, 13)
    obj.data.materials.append(mats[11])
    for p in obj.data.polygons:
        if p.index >= 2*n: p.material_index = 1
    # Small amber sight just aft of the mouth, readable from the gameplay camera.
    ringbody('amber launcher sight', [(0.025,0.025,0.236,x,-0.445),
                                      (0.025,0.025,0.244,x,-0.445)], 8, 10)
for x in (-0.44, 0.44): muzzle(x)

# Central raised observation jewel and protective collar give the spiral a focal point.
ringbody('central collar', [(0.105,0.105,0.305,0,0.02),(0.105,0.105,0.34,0,0.02),
                            (0.08,0.08,0.355,0,0.02)], 10, 11)
ringbody('central lens', [(0.074,0.074,0.355,0,0.02),(0.055,0.055,0.375,0,0.02)], 10, 9)

# Apply all modifiers and transforms, then export a single flat-shaded mesh.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    for mod in list(obj.modifiers):
        bpy.ops.object.modifier_apply(modifier=mod.name)
    obj.select_set(False)
for obj in parts: obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for poly in model.data.polygons: poly.use_smooth = False
bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Dark blue-gray world')
scene.world.use_nodes = True
bg = scene.world.node_tree.nodes.get('Background')
bg.inputs['Color'].default_value = (0.05,0.07,0.09,1)
bg.inputs['Strength'].default_value = 1.0

def aim(obj, point):
    obj.rotation_euler = (Vector(point)-obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None: data.shape = 'DISK'; data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    aim(obj, target)
    return obj
light('Upper left sun', 'SUN', (-3,4,6), 2.0, (0,0,0))
light('Soft fill', 'AREA', (4,-3,5), 160, (0,0,0), 5)
camdata = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', camdata)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0,0,5)
cam.rotation_euler = (0,0,0)
camdata.type = 'ORTHO'
camdata.ortho_scale = 1.75
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
camdata.type = 'PERSP'
camdata.lens = 50
cam.location = (2.4,-3.5,3.1)
aim(cam, (0,0,0.05))
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)
