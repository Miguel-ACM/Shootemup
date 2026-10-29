import bpy
import math
import os
import sys
from mathutils import Vector

# Unity-facing model axes: X right, Y up, Z front.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
    if OUT.lower().endswith('.py'):
        OUT = os.path.dirname(OUT)
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
    for block in list(datablocks):
        if block.users == 0:
            datablocks.remove(block)

palette = [
    (0.78, 0.83, 0.79, 1), # ivory hull
    (0.53, 0.61, 0.62, 1), # blue-gray hull edge
    (0.34, 0.31, 0.48, 1), # violet wing
    (0.49, 0.46, 0.66, 1), # wing highlight
    (0.16, 0.83, 0.91, 1), # cyan laser
    (0.46, 0.95, 0.98, 1), # lens highlight
    (0.24, 0.35, 0.40, 1), # slate recess
    (0.91, 0.92, 0.82, 1), # pale armor
    (0.95, 0.55, 0.20, 1), # small amber marks
]
mats = []
for i, color in enumerate(palette):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = color
    m.use_nodes = True
    m.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value = color
    m.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value = 0.72
    mats.append(m)

parts = []
def finish(obj, mat):
    obj.data.materials.append(mats[mat])
    for face in obj.data.polygons:
        face.use_smooth = False
    parts.append(obj)
    return obj

def box(name, loc, size, mat, bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    o = bpy.context.object
    o.name = name
    o.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = o.modifiers.new('Machined corners', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = o
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return finish(o, mat)

def prism(name, outline, z0, z1, mat):
    # Counter-clockwise XY polygon, extruded through depth.
    n = len(outline)
    verts = [(x, y, z0) for x, y in outline] + [(x, y, z1) for x, y in outline]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2*n))]
    faces += [(i, (i+1)%n, (i+1)%n+n, i+n) for i in range(n)]
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return finish(obj, mat)

def drum(name, loc, radius, depth, mat, axis='Z', vertices=10):
    rotation = (math.pi / 2, 0, 0) if axis == 'Y' else (0, 0, 0)
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=rotation)
    return finish(bpy.context.object, mat)

# Thick, faceted central fuselage with a pointed nose and tapered stern.
prism('Armored fuselage', [(-.16,-.30),(.16,-.30),(.205,.10),(.13,.25),(0,.36),(-.13,.25),(-.205,.10)], -.13,.13, 1)
prism('Front armor', [(-.12,-.24),(.12,-.24),(.16,.09),(.10,.21),(0,.30),(-.10,.21),(-.16,.09)], .125,.168, 0)
prism('Back armor', [(-.12,-.24),(.12,-.24),(.16,.09),(.10,.21),(0,.30),(-.10,.21),(-.16,.09)], -.168,-.125, 0)

for s in (-1, 1):
    # Winding is reversed on the mirrored half to keep outward normals.
    outline = [(s*.13,.10),(s*.38,-.07),(s*.43,-.18),(s*.39,-.27),(s*.14,-.20)]
    if s > 0: outline.reverse()
    prism('Swept wing', outline, -.085,.085, 2)
    inset = [(s*.18,.035),(s*.355,-.10),(s*.39,-.17),(s*.36,-.19),(s*.18,-.13)]
    if s > 0: inset.reverse()
    prism('Wing front plate', inset, .082,.104, 3)
    prism('Wing back plate', inset, -.104,-.082, 3)
    # Chunky engine pods are visible from every direction.
    box('Engine pod', (s*.245,-.245,0), (.14,.19,.21), 1, .025)
    drum('Engine rim front', (s*.245,-.245,.112), .061,.025, 6, vertices=8)
    drum('Engine cap front', (s*.245,-.245,.128), .038,.012, 4, vertices=8)
    drum('Engine rim back', (s*.245,-.245,-.112), .061,.025, 6, vertices=8)
    drum('Engine cap back', (s*.245,-.245,-.128), .038,.012, 4, vertices=8)

# A forward-pointing laser barrel and a broad cyan optical sight on each face.
box('Emitter mounting', (0,.255,0), (.17,.25,.21), 1, .025)
box('Emitter barrel', (0,.36,0), (.14,.18,.15), 0, .018)
drum('Muzzle collar', (0,.445,0), .085,.052, 6, axis='Y', vertices=10)
drum('Muzzle aperture', (0,.474,0), .054,.012, 4, axis='Y', vertices=10)
for sign in (-1, 1):
    drum('Sight bezel', (0,.19,sign*.179), .105,.035, 6, vertices=10)
    drum('Cyan sight', (0,.19,sign*.201), .075,.016, 4, vertices=10)
    drum('Sight center', (0,.19,sign*.212), .040,.009, 5, vertices=10)
    box('Center stripe', (0,-.085,sign*.179), (.065,.19,.023), 4, .008)
    box('Tail badge', (0,-.245,sign*.177), (.12,.045,.025), 7, .008)

# Merge all authored pieces and bake object transforms.
bpy.ops.object.select_all(action='DESELECT')
for o in parts:
    o.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for p in model.data.polygons:
    p.use_smooth = False

# OBJ axis settings deliberately preserve these authored coordinates.
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
scene.render.film_transparent = False
scene.world = bpy.data.worlds.new('Blue gray studio') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(obj, target=(0,0,0)):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, loc, energy, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = loc
    aim(obj)
    return obj

light('Upper left sun', 'SUN', (-3, 5, 6), 2.0)
light('Soft fill', 'AREA', (4, 1, 4), 120, 5)
camdata = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', camdata)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 4)
aim(cam)
camdata.type = 'ORTHO'
camdata.ortho_scale = 1.15
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (0.85, 0.55, 1.5)
aim(cam)
camdata.type = 'PERSP'
camdata.lens = 50
camdata.sensor_width = 36
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

