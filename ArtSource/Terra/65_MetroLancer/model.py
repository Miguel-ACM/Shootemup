import bpy
import math
import json
import os
import sys
from mathutils import Vector

# Resolve the output directory both for --python and for a script path after --.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    if not args:
        raise RuntimeError('Pass the script path after -- when __file__ is unavailable')
    OUT = os.path.dirname(os.path.abspath(args[0]))
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
    for block in list(datablocks):
        if block.users == 0:
            datablocks.remove(block)

colors = [
    (0.24,0.35,0.40), (0.78,0.83,0.79), (0.35,0.43,0.30),
    (0.95,0.38,0.12), (0.32,0.48,0.55), (0.54,0.18,0.16),
    (0.53,0.61,0.62), (0.34,0.31,0.48), (0.20,0.36,0.40),
    (0.16,0.83,0.91), (1.0,0.77,0.27), (0.055,0.09,0.12),
    (0.62,0.25,0.19), (0.82,0.64,0.26), (0.53,0.61,0.55),
    (0.80,0.84,0.83),
]
mats = []
for i, color in enumerate(colors):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.72
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*color, 1.0)
        bsdf.inputs['Emission Strength'].default_value = 1.5
    mats.append(mat)

body = []
def mesh(name, verts, faces, material, face_materials=None):
    data = bpy.data.meshes.new(name + '_mesh')
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    for m in mats:
        data.materials.append(m)
    for p in data.polygons:
        p.material_index = material if face_materials is None else face_materials[p.index]
        p.use_smooth = False
    body.append(obj)
    return obj

def slab(name, outline, bottom, top, mat, side=None, inset=0.0):
    # Inset upper ring produces a readable bevel and a faceted edge.
    cx = sum(x for x, y in outline) / len(outline)
    cy = sum(y for x, y in outline) / len(outline)
    n = len(outline)
    verts = [(x,y,bottom) for x,y in outline]
    verts += [(x+(cx-x)*inset,y+(cy-y)*inset,top) for x,y in outline]
    faces = [tuple(reversed(range(n))), tuple(range(n,2*n))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh(name, verts, faces, mat, [side if side is not None else mat, mat] + [side if side is not None else mat]*n)

def loft(name, rings, mat, face_mats=None):
    # Each ring is a same-sized polygon, arranged consistently around the hull.
    n = len(rings[0])
    verts = [v for ring in rings for v in ring]
    faces = [tuple(reversed(range(n)))]
    for j in range(len(rings)-1):
        for i in range(n):
            faces.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
    faces.append(tuple((len(rings)-1)*n+i for i in range(n)))
    return mesh(name, verts, faces, mat, face_mats)

def tube(name, x, y, z, outer, inner, depth, mat, bore=11, n=10):
    # Annular lip, oriented along Y, with an inset dark bore. y is the mouth plane.
    verts=[]
    for yy, radius in [(y,outer),(y,inner),(y+depth,inner),(y+depth,outer)]:
        for i in range(n):
            a=2*math.pi*i/n
            verts.append((x+radius*math.cos(a), yy, z+radius*math.sin(a)))
    faces=[]; indices=[]
    for k, mi in [(0,mat),(1,bore),(2,mat)]:
        for i in range(n):
            j=(i+1)%n
            faces.append((k*n+i,k*n+j,(k+1)*n+j,(k+1)*n+i)); indices.append(mi)
    faces.append(tuple(2*n+i for i in reversed(range(n)))); indices.append(bore)
    return mesh(name, verts, faces, mat, indices)

# Main fuselage: a pointed lance with a broad shoulder and bevelled crown.
outline=[(0,-1.14),(-.20,-.60),(-.24,.68),(-.20,.86),(.20,.86),(.24,.68),(.20,-.60)]
slab('Lower keel', outline, -.08, .12, 0, 11, .05)
slab('Armored crown', [(0,-1.105),(-.17,-.57),(-.205,.59),(-.17,.79),(.17,.79),(.205,.59),(.17,-.57)], .11, .25, 4, 0, .13)
# Central nose ridge tapers into the hull, with a painted center stripe.
slab('Nose ridge', [(0,-1.07),(-.105,-.62),(-.10,-.38),(.10,-.38),(.105,-.62)], .245, .30, 6, 0, .12)
slab('Nose marking', [(-.026,-.98),(-.045,-.66),(.045,-.66),(.026,-.98)], .301, .305, 3)

for s, label in [(-1,'L'),(1,'R')]:
    # Swept, orange-tipped urban interceptor wings retain the original outline.
    wing=[(s*.17,.12),(s*.77,.67),(s*.45,.55),(s*.28,-.18)]
    if s < 0: wing.reverse()
    slab('Wing '+label, wing, -.01, .125, 0, 11, .04)
    panel=[(s*.25,.10),(s*.71,.625),(s*.47,.54),(s*.31,-.09)]
    if s < 0: panel.reverse()
    slab('Wing paint '+label, panel, .126, .153, 3, 12, .05)
    stripe=[(s*.32,.11),(s*.53,.40),(s*.57,.43),(s*.36,.12)]
    if s < 0: stripe.reverse()
    slab('Wing stripe '+label, stripe, .154, .159, 15)
    # Twin gun housings terminate exactly at the gameplay muzzle planes.
    gun=[(s*.145,-.94),(s*.145,-.36),(s*.245,-.30),(s*.255,-.90)]
    if s < 0: gun.reverse()
    slab('Gun fairing '+label, gun, .055, .20, 6, 0, .08)
    tube('Muzzle '+label, s*.20, -1.02, .15, .061, .036, .15, 6, 11, 10)
    # Rear motor fairings and actual recessed openings at the socket coordinates.
    motor=[(s*.15,.42),(s*.34,.42),(s*.37,.66),(s*.32,.70),(s*.16,.70),(s*.12,.66)]
    if s < 0: motor.reverse()
    slab('Motor housing '+label, motor, -.055, .20, 4, 0, .10)
    tube('Socket '+label, s*.24, .74, 0.0, .115, .075, -.085, 6, 11, 12)
    # A visible rim around the socket on top of the fairing identifies the outlet.
    slab('Motor cap '+label, [(s*.15,.48),(s*.32,.48),(s*.33,.67),(s*.16,.67)] if s>0 else [(s*.16,.67),(s*.33,.67),(s*.32,.48),(s*.15,.48)], .202, .245, 0, 11, .16)

# Faceted raised cockpit, opaque dark glazing with pale rim and center highlight.
slab('Canopy surround', [(-.14,-.36),(-.15,.18),(-.10,.45),(.10,.45),(.15,.18),(.14,-.36)], .245, .285, 15, 0, .05)
loft('Canopy', [
    [(-.115,-.32,.286),(-.12,.16,.286),(-.075,.40,.286),(.075,.40,.286),(.12,.16,.286),(.115,-.32,.286)],
    [(-.075,-.23,.37),(-.08,.14,.42),(-.045,.30,.38),(.045,.30,.38),(.08,.14,.42),(.075,-.23,.37)]
], 11)
slab('Canopy glint', [(-.07,-.14),(-.07,.13),(-.045,.25),(-.021,.25),(-.025,-.14)], .421, .424, 4)
slab('Rear spine', [(-.065,.44),(-.075,.70),(.075,.70),(.065,.44)], .255, .30, 6, 0, .12)

# Join static geometry; this interceptor has no naturally articulated parts.
bpy.ops.object.select_all(action='DESELECT')
for ob in body:
    ob.select_set(True)
bpy.context.view_layer.objects.active = body[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
with open(os.path.join(OUT, 'parts.json'), 'w', encoding='utf-8') as f:
    json.dump([], f, indent=2)

# Export the mesh before adding the presentation rig.
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
scene.world = bpy.data.worlds.new('Studio World') if scene.world is None else scene.world
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.05,0.07,0.09,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def point_at(obj, target):
    obj.rotation_euler = (Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, loc, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None and kind == 'AREA': data.shape = 'DISK'; data.size = size
    ob = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(ob)
    ob.location = loc
    point_at(ob, target)
    return ob

sun = light('Sun', 'SUN', (-3,-4,7), 2.0, (0,0,0))
sun.data.angle = math.radians(18)
light('Soft fill', 'AREA', (4,1,5), 180, (0,0,0), 5.0)
camdata = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', camdata)
bpy.context.collection.objects.link(cam)
scene.camera = cam
camdata.type = 'ORTHO'
camdata.ortho_scale = 2.65
cam.location = (0,0,6)
# Camera local Y points to world +Y so screen up matches model +Y.
cam.rotation_euler = (0,0,0)
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
camdata.type = 'PERSP'
camdata.lens = 50
cam.location = (3.0,-4.2,3.6)
point_at(cam, (0,-.05,.10))
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

