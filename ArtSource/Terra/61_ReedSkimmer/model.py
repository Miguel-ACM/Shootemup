import bpy
import math
import os
import sys
import json
from mathutils import Vector

# Run with: blender --background --python model.py [-- output_directory]
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for m in list(bpy.data.materials):
    bpy.data.materials.remove(m)

colors = [(0.24,0.35,0.40),(0.78,0.83,0.79),(0.35,0.43,0.30),
          (0.95,0.38,0.12),(0.32,0.48,0.55),(0.54,0.18,0.16),
          (0.53,0.61,0.62),(0.34,0.31,0.48),(0.20,0.36,0.40),
          (0.16,0.83,0.91),(1.0,0.77,0.27),(0.055,0.09,0.12),
          (0.62,0.25,0.19),(0.82,0.64,0.26),(0.53,0.61,0.55),
          (0.80,0.84,0.83)]
mats = []
for i, color in enumerate(colors):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1)
    bsdf.inputs['Roughness'].default_value = 0.78
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*color, 1)
        bsdf.inputs['Emission Strength'].default_value = 1.4
    mats.append(m)

body = []
def mesh(name, verts, faces, material, group=body):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mats[material])
    if group is not None:
        group.append(obj)
    return obj

def layered(name, outline, bottom, shoulder, top, inset, material, group=body):
    # Three rings give the armour a sloped side and a raised, faceted crown.
    cx = sum(p[0] for p in outline) / len(outline)
    cy = sum(p[1] for p in outline) / len(outline)
    n = len(outline)
    verts = [(x, y, bottom) for x,y in outline]
    verts += [(x, y, shoulder) for x,y in outline]
    verts += [(cx+(x-cx)*inset, cy+(y-cy)*inset, top) for x,y in outline]
    faces = [tuple(reversed(range(n)))]
    for k in range(2):
        for i in range(n):
            j = (i+1)%n
            faces.append((k*n+i, k*n+j, (k+1)*n+j, (k+1)*n+i))
    faces.append(tuple(range(2*n, 3*n)))
    return mesh(name, verts, faces, material, group)

def tube_y(name, x, y_front, y_back, z, outer, inner, mat, inside=11, n=10, group=body):
    # A real annular rim and recessed dark bore, axis along Y.
    verts=[]
    for y,r in [(y_front,outer),(y_back,outer),(y_front,inner),(y_front+(y_back-y_front)*0.60,inner)]:
        verts += [(x + r*math.cos(2*math.pi*i/n), y, z + r*math.sin(2*math.pi*i/n)) for i in range(n)]
    faces=[]
    for a,b in [(0,1),(0,2),(2,3)]:
        for i in range(n):
            j=(i+1)%n
            faces.append((a*n+i,a*n+j,b*n+j,b*n+i))
    faces.append(tuple(range(3*n,4*n)))
    ob=mesh(name, verts, faces, mat, group)
    ob.data.materials.append(mats[inside])
    for poly in list(ob.data.polygons)[2*n:]:
        poly.material_index=1
    return ob

# Long, tapered centre hull: a pointed prow and a tall, armored spine.
layered('Keel', [(0,-.92),(-.24,-.40),(-.27,.48),(-.20,.75),(.20,.75),(.27,.48),(.24,-.40)], -.075,.11,.24,.76,0)
layered('Nose armour', [(0,-.89),(-.17,-.42),(-.16,-.21),(.16,-.21),(.17,-.42)], .13,.21,.27,.78,4)
layered('Spine', [(-.17,.12),(-.18,.49),(-.13,.67),(.13,.67),(.18,.49),(.17,.12)], .20,.25,.31,.72,8)
# Broad, swept, low-set wings and inset armor: mirror the same hand-built shape.
for side in (-1,1):
    def mirror(points):
        pts=[(side*x,y) for x,y in points]
        return pts if side == 1 else list(reversed(pts))
    layered('Swept wing', mirror([(.16,-.26),(1.20,.24),(.96,.53),(.32,.24)]), -.055,.065,.13,.82,2)
    layered('Wing upper plate', mirror([(.30,-.13),(1.08,.25),(.94,.42),(.40,.20)]), .10,.135,.16,.84,14)
    layered('Wing leading edge', mirror([(.29,-.19),(1.16,.23),(1.09,.29),(.35,-.08)]), .11,.14,.165,.88,0)
    # Compact gun pods terminate exactly at the gameplay muzzle points.
    layered('Gun pod', mirror([(.49,-.54),(.49,.06),(.67,.10),(.67,-.54)]), -.015,.12,.19,.78,0)
    layered('Pod cap', mirror([(.51,-.42),(.51,-.02),(.65,.02),(.65,-.42)]), .16,.19,.22,.82,6)
    tube_y('Muzzle lip', side*.58, -.62, -.49, .16, .071, .038, 6)
    # Quiet contrasting wing-root strake, not a repeated row of details.
    layered('Root strake', mirror([(.27,.14),(.46,.23),(.51,.39),(.31,.34)]), .16,.18,.20,.83,4)

# Faceted opaque cockpit with a small central highlight.
layered('Cockpit surround', [(-.16,-.39),(-.19,-.11),(-.14,.18),(.14,.18),(.19,-.11),(.16,-.39)], .22,.255,.31,.82,11)
layered('Canopy', [(-.12,-.34),(-.145,-.10),(-.105,.13),(.105,.13),(.145,-.10),(.12,-.34)], .28,.31,.37,.69,4)
layered('Canopy glint', [(-.095,-.19),(-.095,-.08),(-.07,.06),(-.025,.06),(-.04,-.11)], .355,.37,.38,.85,6)
# Rear motor shroud and an annular nozzle at the fixed socket; no flames.
layered('Motor housing', [(-.18,.39),(-.20,.63),(-.13,.75),(.13,.75),(.20,.63),(.18,.39)], .02,.16,.24,.72,0)
tube_y('Engine socket', 0, .64, .55, 0, .12, .074, 6)

# Rear stabilizers are separate hinged moving meshes, with mirrored swing axes.
parts=[]
part_meta=[]
for side, suffix in [(-1,'L'),(1,'R')]:
    group=[]
    p=[(side*.20,.45),(side*.50,.78),(side*.42,.99),(side*.27,.78)]
    if side < 0: p.reverse()
    layered('Fin armor', p, .06,.13,.18,.78,1,group)
    q=[(side*.27,.55),(side*.44,.78),(side*.41,.88),(side*.32,.73)]
    if side < 0: q.reverse()
    layered('Fin inset', q, .16,.18,.195,.82,6,group)
    bpy.ops.object.select_all(action='DESELECT')
    for ob in group: ob.select_set(True)
    bpy.context.view_layer.objects.active=group[0]
    bpy.ops.object.join()
    ob=group[0]
    ob.name='Fin_'+suffix
    parts.append(ob)
    part_meta.append({'object':ob.name,'motion':'swing','axis':[0,side,0], 'pivot':[side*.20,.45,.12], 'speed':1.0,'amplitude':12})

# Consolidate all static geometry; triangulate explicitly for a predictable OBJ.
bpy.ops.object.select_all(action='DESELECT')
for ob in body: ob.select_set(True)
bpy.context.view_layer.objects.active=body[0]
bpy.ops.object.join()
model=body[0]
model.name='Model'
for ob in [model]+parts:
    bpy.ops.object.select_all(action='DESELECT')
    ob.select_set(True)
    bpy.context.view_layer.objects.active=ob
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    mod=ob.modifiers.new('Triangulate', 'TRIANGULATE')
    bpy.ops.object.modifier_apply(modifier=mod.name)
    for poly in ob.data.polygons: poly.use_smooth=False

with open(os.path.join(OUT, 'parts.json'), 'w', encoding='utf-8') as f:
    json.dump(part_meta, f, indent=2)

bpy.ops.object.select_all(action='DESELECT')
for ob in [model]+parts: ob.select_set(True)
bpy.context.view_layer.objects.active=model
bpy.ops.wm.obj_export(filepath=os.path.join(OUT,'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

scene=bpy.context.scene
scene.render.engine='CYCLES'
scene.cycles.device='CPU'
scene.cycles.samples=48
scene.render.resolution_x=640
scene.render.resolution_y=640
scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.world=bpy.data.worlds.new('Blue gray world') if not scene.world else scene.world
scene.world.use_nodes=True
bg=scene.world.node_tree.nodes.get('Background')
bg.inputs['Color'].default_value=(.05,.07,.09,1)
bg.inputs['Strength'].default_value=1.0
scene.view_settings.view_transform='Standard'

def point_at(obj, target):
    obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, location, energy, size=None):
    data=bpy.data.lights.new(name, kind)
    data.energy=energy
    if kind == 'AREA': data.shape='DISK'; data.size=size
    if kind == 'SUN': data.angle=math.radians(18)
    ob=bpy.data.objects.new(name,data)
    bpy.context.collection.objects.link(ob)
    ob.location=location
    point_at(ob,(0,0,0))
    return ob
light('Sun', 'SUN', (-4,-5,7), 2.0)
light('Soft fill', 'AREA', (4,1,5), 250, 5)
camdata=bpy.data.cameras.new('Camera')
cam=bpy.data.objects.new('Camera',camdata)
bpy.context.collection.objects.link(cam)
scene.camera=cam
cam.location=(0,0,6)
cam.rotation_euler=(0,0,0)
camdata.type='ORTHO'
camdata.ortho_scale=2.82
scene.render.filepath=os.path.join(OUT,'top.png')
bpy.ops.render.render(write_still=True)
camdata.type='PERSP'
cam.location=(3.2,-4.3,4.0)
point_at(cam,(0,0,.08))
camdata.lens=48
scene.render.filepath=os.path.join(OUT,'angle.png')
bpy.ops.render.render(write_still=True)
