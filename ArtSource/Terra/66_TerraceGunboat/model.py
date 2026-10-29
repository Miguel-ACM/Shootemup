import bpy
import math
import json
import os
import sys
from mathutils import Vector

# The gameplay axes are preserved: nose -Y, camera +Z.
if '__file__' in globals():
    OUT = os.path.dirname(os.path.abspath(__file__))
elif '--' in sys.argv and sys.argv.index('--') + 1 < len(sys.argv):
    p = os.path.abspath(sys.argv[sys.argv.index('--') + 1])
    OUT = p if os.path.isdir(p) else os.path.dirname(p)
else:
    OUT = os.getcwd()
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablock in list(bpy.data.materials):
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
    mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1)
    bsdf.inputs['Roughness'].default_value = 0.72
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*color, 1)
        bsdf.inputs['Emission Strength'].default_value = 1.5
    mats.append(mat)

body = []
def mesh(name, verts, faces, material):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.materials.append(mats[material])
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    body.append(obj)
    return obj

def layered(name, outline, rings, material):
    # Similar polygons at successive heights form faceted sloping armor edges.
    cx = sum(p[0] for p in outline) / len(outline)
    cy = sum(p[1] for p in outline) / len(outline)
    n = len(outline)
    verts = [(cx + (x-cx)*scale, cy + (y-cy)*scale, z)
             for z, scale in rings for x, y in outline]
    faces = [tuple(reversed(range(n)))]
    for k in range(len(rings)-1):
        for i in range(n):
            j = (i+1) % n
            faces.append((k*n+i, k*n+j, (k+1)*n+j, (k+1)*n+i))
    faces.append(tuple((len(rings)-1)*n+i for i in range(n)))
    return mesh(name, verts, faces, material)

def tube(name, x, y, z, radius, length, mat, inner=0.0, sides=12):
    # Ring in the XZ plane, longitudinal axis Y. The given y is the mouth.
    verts = []
    for yy, rr in ((y, radius), (y+length, radius), (y, inner), (y+length*0.32, inner)):
        for i in range(sides):
            a = 2*math.pi*i/sides
            verts.append((x+rr*math.cos(a), yy, z+rr*math.sin(a)))
    faces = []
    for i in range(sides):
        j = (i+1)%sides
        faces.extend([(i,j,sides+j,sides+i), (i,2*sides+i,2*sides+j,j),
                      (2*sides+i,3*sides+i,3*sides+j,2*sides+j)])
    return mesh(name, verts, faces, mat)

def box(name, loc, scale, material, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mats[material])
    if bevel:
        mod = obj.modifiers.new('Machined edges', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    body.append(obj)
    return obj

# Central deep hull: an orange armored bow under a raised wheelhouse.
layered('Keel', [(-.34,-.49),(.34,-.49),(.46,.34),(.17,.75),(-.17,.75),(-.46,.34)],
        [(-.10,.86),(.06,1.0),(.25,.89),(.30,.72)], 3)
layered('Bow armor', [(-.29,-.46),(.29,-.46),(.34,-.19),(.24,-.10),(-.24,-.10),(-.34,-.19)],
        [(.255,1),(.295,.88)], 12)
layered('Raised bridge', [(-.25,-.16),(.25,-.16),(.30,.20),(.19,.39),(-.19,.39),(-.30,.20)],
        [(.27,1),(.40,.82),(.43,.70)], 0)
# Faceted steel-blue canopy and pale brow read clearly from above.
layered('Bridge glazing', [(-.17,-.13),(.17,-.13),(.20,.10),(.12,.22),(-.12,.22),(-.20,.10)],
        [(.414,1),(.455,.78)], 4)
layered('Bridge brow', [(-.21,.23),(.21,.23),(.16,.30),(-.16,.30)],
        [(.422,1),(.445,.9)], 6)
layered('Bow stripe', [(-.045,-.44),(.045,-.44),(.045,-.22),(-.045,-.22)],
        [(.301,1),(.306,1)], 15)

for s in (-1, 1):
    # Mirror outlines with corrected winding.
    def mirrored(poly):
        points = [(s*x,y) for x,y in poly]
        return points if s == 1 else list(reversed(points))
    wing = mirrored([(.25,-.10),(1.05,-.27),(1.12,.38),(.36,.56)])
    layered('Terrace', wing, [(-.075,.91),(.08,1),(.20,.96),(.25,.84)], 13)
    layered('Terrace deck', mirrored([(.38,-.06),(.96,-.20),(1.01,.32),(.43,.45)]),
            [(.245,1),(.275,.91)], 0)
    # A single broad, pale painted stripe across each terrace.
    layered('Deck marking', mirrored([(.43,.16),(.98,.03),(.99,.12),(.44,.25)]),
            [(.277,1),(.282,1)], 6)
    layered('Gun mount', mirrored([(.68,-.27),(.96,-.27),(.96,.02),(.68,.02)]),
            [(.19,1),(.29,.8)], 11)
    # Forward barrel mouth is exactly at the gameplay position, including height.
    tube('Gun barrel', s*.82, -.48, .18, .095, .25, 6, .056)
    tube('Muzzle lip', s*.82, -.48, .18, .105, .035, 11, .058)
    # Rear socket is at the nozzle opening centre; it points aft (+Y).
    tube('Motor casing', s*.65, .52, 0, .125, -.23, 0, .079)
    tube('Nozzle rim', s*.65, .52, 0, .127, -.038, 6, .078)
    # Low, visible motor cowling stays within the original terrace footprint.
    layered('Motor fairing', mirrored([(.51,.30),(.79,.30),(.79,.48),(.72,.56),(.58,.56),(.51,.48)]),
            [(.13,1),(.24,.79),(.27,.62)], 3)
    box('Terrace cap', (s*.55,.36,.294), (.21,.09,.035), 13, .015)

# Join static geometry, retaining only palette material slots.
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

# This compact craft has no independently moving surfaces.
with open(os.path.join(OUT, 'parts.json'), 'w', encoding='utf-8') as f:
    json.dump([], f, indent=2)

bpy.ops.object.select_all(action='DESELECT')
model.select_set(True)
bpy.context.view_layer.objects.active = model
bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'),
                      export_selected_objects=True, export_materials=True,
                      forward_axis='Y', up_axis='Z')

# Render setup after export so cameras and lights are not included in the OBJ.
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Dark blue gray') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def point_at(obj, point):
    obj.rotation_euler = (Vector(point) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, loc, energy, target, size=1):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if kind == 'AREA':
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = loc
    point_at(obj, target)
    return obj

light('Sun', 'SUN', (-3,-4,6), 2.0, (0,0,0))
light('Soft fill', 'AREA', (3,1,4), 180, (0,0,0), 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0,0,6)
cam.rotation_euler = (0,0,0)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 2.65
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (3.2,-4.4,3.8)
point_at(cam, (0,0.05,0.13))
cam_data.type = 'PERSP'
cam_data.lens = 48
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)
