import bpy
import math
import os
import sys
import json
from mathutils import Vector

# Blender may omit __file__ when invoked from an interactive console.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.dirname(os.path.abspath(args[args.index('--') + 1])) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
    for block in list(datablocks):
        if block.users == 0:
            datablocks.remove(block)

palette = [
    (0.24,0.35,0.40), (0.78,0.83,0.79), (0.35,0.43,0.30), (0.95,0.38,0.12),
    (0.32,0.48,0.55), (0.54,0.18,0.16), (0.53,0.61,0.62), (0.34,0.31,0.48),
    (0.20,0.36,0.40), (0.16,0.83,0.91), (1.0,0.77,0.27), (0.055,0.09,0.12),
    (0.62,0.25,0.19), (0.82,0.64,0.26), (0.53,0.61,0.55), (0.80,0.84,0.83)
]
mats = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.78
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*color, 1.0)
        bsdf.inputs['Emission Strength'].default_value = 1.5
    mats.append(mat)

body = []
def mesh(name, vertices, faces, indices):
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    data.materials.clear()
    for mat in mats:
        data.materials.append(mat)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    for poly, index in zip(data.polygons, indices):
        poly.material_index = index
        poly.use_smooth = False
    body.append(obj)
    return obj

def armor(name, outline, z_bottom, z_edge, z_top, inset, topmat, sidemat, bevelmat):
    # Three independently shaped rings yield a broad faceted bevel and a raised crown.
    n = len(outline)
    cx = sum(p[0] for p in outline) / n
    cy = sum(p[1] for p in outline) / n
    inner = [(cx + (x-cx)*inset, cy + (y-cy)*inset) for x,y in outline]
    verts = [(x,y,z_bottom) for x,y in outline] + [(x,y,z_edge) for x,y in outline] + [(x,y,z_top) for x,y in inner]
    faces = [tuple(reversed(range(n)))]
    ids = [sidemat]
    for i in range(n):
        j = (i+1) % n
        faces += [(i,j,n+j,n+i), (n+i,n+j,2*n+j,2*n+i)]
        ids += [sidemat, bevelmat]
    faces.append(tuple(range(2*n,3*n)))
    ids.append(topmat)
    return mesh(name, verts, faces, ids)

def ring_y(name, x, y_front, y_back, z, outer, inner, material, bore=11, segments=10):
    # Open annulus with a recessed dark bore, facing along the Y axis.
    v = []
    for y, radius in ((y_front,outer),(y_back,outer),(y_front,inner),(y_back,inner)):
        for i in range(segments):
            a = 2*math.pi*i/segments
            v.append((x + radius*math.cos(a), y, z + radius*math.sin(a)))
    f, ids = [], []
    for i in range(segments):
        j = (i+1)%segments
        f.extend([(i,j,segments+j,segments+i), (i,2*segments+i,2*segments+j,j),
                  (2*segments+i,3*segments+i,3*segments+j,2*segments+j)])
        ids.extend([material, material, bore])
    f.append(tuple(range(3*segments,4*segments)))
    ids.append(bore)
    return mesh(name, v, f, ids)

def poly(name, vertices, mat):
    return mesh(name, vertices, [tuple(range(len(vertices)))], [mat])

# A long brass spear and broad swept ceramic wings keep the original interceptor outline.
armor('Keel', [(0,-1.18),(-.22,-.42),(-.24,.59),(0,.83),(.24,.59),(.22,-.42)],
      -.12,.075,.24,.76,13,12,13)
for s in (-1,1):
    wing = [(s*.18,-.18),(s*.87,.30),(s*.76,.65),(s*.36,.42),(s*.16,.61)]
    if s < 0: wing.reverse()
    armor('Swept wing', wing, -.12,.025,.105,.78,6,11,14)
    plate = [(s*.29,.36),(s*.48,.70),(s*.52,.15),(s*.38,-.31)]
    if s < 0: plate.reverse()
    armor('Raised brass shoulder', plate, .07,.15,.205,.78,13,12,13)
    # Broad inset ceramic flash breaks up the wing and reinforces its sweep.
    stripe = [(s*.48,.31,.115),(s*.78,.38,.085),(s*.72,.51,.095),(s*.48,.43,.12)]
    if s < 0: stripe.reverse()
    poly('Ceramic wing flash', stripe, 15)
    # The socket mouth is exactly at y=.68, x=+/- .38, z=0.
    ring_y('Rear socket', s*.38,.68,.48,0,.115,.070,6,11,10)

# Raised cockpit and spine: dark faceted canopy, bordered by brass and a warm sight stripe.
armor('Canopy surround', [(-.15,.36),(-.14,-.13),(0,-.39),(.14,-.13),(.15,.36)],
      .18,.245,.30,.82,13,12,13)
armor('Canopy', [(-.105,.30),(-.09,-.12),(0,-.28),(.09,-.12),(.105,.30)],
      .27,.30,.355,.73,11,11,4)
poly('Sight stripe', [(-.024,-.20,.357),(.024,-.20,.357),(.024,.12,.357),(-.024,.12,.357)], 3)
# Low collar and an actual open muzzle, ending at the fixed gameplay point.
armor('Gun housing', [(-.10,-.66),(-.105,-.97),(0,-1.09),(.105,-.97),(.10,-.66)],
      .055,.14,.19,.78,11,11,6)
ring_y('Muzzle lip', 0,-1.10,-.91,.13,.075,.046,6,11,12)

# Join only model geometry, retaining flat faces and palette slots.
bpy.ops.object.select_all(action='DESELECT')
for obj in body:
    obj.select_set(True)
bpy.context.view_layer.objects.active = body[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for p in model.data.polygons:
    p.use_smooth = False
with open(os.path.join(OUT, 'parts.json'), 'w', encoding='utf-8') as f:
    json.dump([], f, indent=2)

bpy.ops.object.select_all(action='DESELECT')
model.select_set(True)
bpy.context.view_layer.objects.active = model
bpy.ops.wm.obj_export(filepath=os.path.join(OUT,'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Dark blue-gray') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes = True
bg = scene.world.node_tree.nodes.get('Background')
bg.inputs['Color'].default_value = (.05,.07,.09,1)
bg.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(obj, target):
    obj.rotation_euler = (Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, location, energy, target, size=1):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if kind == 'AREA': data.shape = 'DISK'; data.size = size
    if kind == 'SUN': data.angle = math.radians(18)
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    aim(obj, target)

light('Sun', 'SUN', (-3,-4,6), 2.0, (0,0,0))
light('Soft fill', 'AREA', (4,2,5), 180, (0,0,0), 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0,0,5)
cam.rotation_euler = (0,0,0)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 2.38
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam_data.lens = 50
cam.location = (3.3,-4.5,3.6)
aim(cam, (0,-.12,.03))
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)
