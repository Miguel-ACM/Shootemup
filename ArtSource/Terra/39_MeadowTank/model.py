import bpy
import math
import json
import os
import sys
from mathutils import Vector

# Run with: blender --background --python model.py [-- <script directory>]
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    if '--' not in args or not args[args.index('--') + 1:]:
        raise RuntimeError('Pass the script directory after -- when __file__ is unavailable')
    path = os.path.abspath(args[args.index('--') + 1])
    OUT = path if os.path.isdir(path) else os.path.dirname(path)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablock in bpy.data.materials:
    bpy.data.materials.remove(datablock)

palette = [
    (0.24,0.35,0.40), (0.78,0.83,0.79), (0.35,0.43,0.30),
    (0.95,0.38,0.12), (0.32,0.48,0.55), (0.54,0.18,0.16),
    (0.53,0.61,0.62), (0.34,0.31,0.48), (0.20,0.36,0.40),
    (0.16,0.83,0.91), (1.0,0.77,0.27), (0.055,0.09,0.12),
    (0.62,0.25,0.19), (0.82,0.64,0.26), (0.53,0.61,0.55),
    (0.80,0.84,0.83),
]
mats = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.8
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*color, 1.0)
        bsdf.inputs['Emission Strength'].default_value = 1.4
    mats.append(mat)

body = []
def mesh(name, verts, faces, material):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mats[material])
    body.append(obj)
    return obj

def tier(name, lower, upper, z0, z1, material):
    n = len(lower)
    vertices = [(x,y,z0) for x,y in lower] + [(x,y,z1) for x,y in upper]
    faces = [tuple(reversed(range(n))), tuple(range(n,2*n))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh(name, vertices, faces, material)

def box(name, loc, scale, material, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mats[material])
    if bevel:
        mod = obj.modifiers.new('Armor edge', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    body.append(obj)
    return obj

def cylinder(name, loc, radius, depth, material, vertices=12, rotation=(0,0,0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mats[material])
    body.append(obj)
    return obj

def ring(name, center, outer, inner, z0, z1, material, count=12):
    cx, cy = center
    verts = []
    for z, r in ((z0,outer),(z1,outer),(z1,inner),(z0,inner)):
        verts += [(cx+r*math.cos(2*math.pi*i/count), cy+r*math.sin(2*math.pi*i/count), z) for i in range(count)]
    faces = []
    for k in range(4):
        for i in range(count):
            j = (i+1)%count
            faces.append((k*count+i,k*count+j,((k+1)%4)*count+j,((k+1)%4)*count+i))
    return mesh(name, verts, faces, material)

# Twin continuous tracks: broad, chamfered footprint with raised traction cleats.
for side in (-1, 1):
    x = side * 0.56
    outline = [(x-.20,-.82),(x+.20,-.82),(x+.23,-.61),(x+.23,.61),
               (x+.16,.82),(x-.16,.82),(x-.23,.61),(x-.23,-.61)]
    inset = [(x+(px-x)*.94, py*.97) for px,py in outline]
    tier('Track belt', outline, inset, -.13, .17, 11)
    for y in (-.68,-.50,-.32,-.14,.04,.22,.40,.58,.73):
        box('Track cleat', (x,y,.183), (.38,.052,.038), 6, .009)
    # Side wheels are partly buried in the track, visible in the 3/4 view.
    for y in (-.55,-.19,.19,.55):
        cylinder('Road wheel', (side*.775,y,.005), .115, .035, 0, 10, (0,math.pi/2,0))
        cylinder('Wheel hub', (side*.796,y,.005), .042, .012, 6, 8, (0,math.pi/2,0))

# Faceted lower chassis and raised, sloping olive upper armor.
base = [(-.41,-.75),(.41,-.75),(.48,-.52),(.46,.58),(.32,.78),(-.32,.78),(-.46,.58),(-.48,-.52)]
upper = [(-.34,-.67),(.34,-.67),(.40,-.48),(.39,.51),(.28,.69),(-.28,.69),(-.39,.51),(-.40,-.48)]
tier('Lower hull', base, base, -.13, .065, 0)
tier('Sloped armor', base, upper, .065, .245, 2)
# Nose glacis adds a readable front without projecting beyond the original outline.
tier('Front glacis', [(-.31,-.71),(.31,-.71),(.34,-.50),(-.34,-.50)],
     [(-.28,-.67),(.28,-.67),(.30,-.51),(-.30,-.51)], .19,.262,14)
# Rear service deck, broken into a single recess and four broad vents.
box('Rear deck recess', (0,.50,.25), (.55,.30,.035), 11, .025)
for y in (.39,.46,.53,.60):
    box('Vent slat', (0,y,.274), (.43,.025,.022), 6, .005)
# Small amber marker lamps, echoing the existing chassis.
for side in (-1,1):
    box('Lamp housing', (side*.32,-.635,.263), (.13,.12,.045), 11, .012)
    box('Amber marker', (side*.32,-.64,.289), (.083,.075,.012), 10, .004)
# Fixed turret bearing: open central aperture leaves the separate turret unobstructed.
ring('Turret bearing', (0,-.01), .365,.300,.245,.265, 11, 16)
ring('Bearing rim', (0,-.01), .365,.335,.265,.282, 6, 16)

# Consolidate the static chassis; tracks are fixed geometry, not rigid rotating parts.
bpy.ops.object.select_all(action='DESELECT')
for obj in body:
    obj.select_set(True)
bpy.context.view_layer.objects.active = body[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for poly in model.data.polygons:
    poly.use_smooth = False
with open(os.path.join(OUT, 'parts.json'), 'w', encoding='utf-8') as f:
    json.dump([], f, indent=2)

# Export only the model before adding render-only lights and cameras.
bpy.ops.object.select_all(action='DESELECT')
model.select_set(True)
bpy.context.view_layer.objects.active = model
bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Blue gray world') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (0.05,0.07,0.09,1)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def point_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None and kind == 'AREA':
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    obj.location = location
    point_at(obj, target)
    return obj

light('Sun', 'SUN', (-3,-4,6), 2.0, (0,0,0))
light('Soft fill', 'AREA', (4,1,4), 180, (0,0,0), 5)
camdata = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', camdata)
scene.collection.objects.link(cam)
scene.camera = cam
camdata.type = 'ORTHO'
camdata.ortho_scale = 2.20
cam.location = (0,0,6)
cam.rotation_euler = (0,0,0)
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
camdata.type = 'PERSP'
camdata.lens = 50
cam.location = (3.0,-4.0,3.5)
point_at(cam, (0,0,.04))
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)
