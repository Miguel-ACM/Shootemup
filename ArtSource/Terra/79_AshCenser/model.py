import bpy
import math
import os
import sys
import json
from mathutils import Vector

# Blender is normally invoked with --python model.py; the fallback also accepts
# a script path or output directory after -- for embedded execution.
def script_dir():
    if globals().get('__file__'):
        return os.path.dirname(os.path.abspath(__file__))
    args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    if args:
        path = os.path.abspath(args[0])
        return path if os.path.isdir(path) else os.path.dirname(path)
    return os.getcwd()

OUT = script_dir()
os.makedirs(OUT, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

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
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1)
    bsdf.inputs['Roughness'].default_value = 0.82
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*color, 1)
        bsdf.inputs['Emission Strength'].default_value = 1.8
    mats.append(m)

body = []
parts = []
def mesh(name, verts, faces, material_indices):
    data = bpy.data.meshes.new(name + '_mesh')
    data.from_pydata(verts, [], faces)
    data.materials.clear()
    for m in mats:
        data.materials.append(m)
    data.update()
    ob = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(ob)
    for poly, mi in zip(data.polygons, material_indices):
        poly.material_index = mi
        poly.use_smooth = False
    return ob

def loft(name, rings, side_tiles, cap_bottom, cap_top, n=8, phase=math.pi/8):
    # Each ring is (cx, cy, z, radius); facets remain deliberately planar.
    v = []
    for cx, cy, z, radius in rings:
        for j in range(n):
            a = phase + 2 * math.pi * j / n
            v.append((cx + radius * math.cos(a), cy + radius * math.sin(a), z))
    faces = [tuple(reversed(range(n)))]
    ids = [cap_bottom]
    for k in range(len(rings)-1):
        for j in range(n):
            faces.append((k*n+j, k*n+(j+1)%n, (k+1)*n+(j+1)%n, (k+1)*n+j))
            ids.append(side_tiles[k])
    faces.append(tuple((len(rings)-1)*n+j for j in range(n)))
    ids.append(cap_top)
    ob = mesh(name, v, faces, ids)
    body.append(ob)
    return ob

def plate(name, outline, z0, z1, top, side, target=body):
    n = len(outline)
    verts = [(x,y,z0) for x,y in outline] + [(x,y,z1) for x,y in outline]
    faces = [tuple(reversed(range(n))), tuple(range(n,2*n))]
    ids = [side, top]
    for j in range(n):
        faces.append((j,(j+1)%n,(j+1)%n+n,j+n))
        ids.append(side)
    ob = mesh(name, verts, faces, ids)
    target.append(ob)
    return ob

def tube_y(name, x, y_front, y_back, z, outer, inner, tile_outer, tile_inside):
    # Four octagonal rings create an actual recessed, open muzzle mouth.
    n=8
    verts=[]
    for y, r in ((y_back,outer*.82),(y_front,outer),(y_front-.001,inner),(y_front+.085,inner*.8)):
        for j in range(n):
            a=2*math.pi*j/n + math.pi/8
            verts.append((x+r*math.cos(a), y, z+r*math.sin(a)))
    faces=[]; ids=[]
    for k, tile in enumerate((tile_outer,13,tile_inside)):
        for j in range(n):
            faces.append((k*n+j,k*n+(j+1)%n,(k+1)*n+(j+1)%n,(k+1)*n+j))
            ids.append(tile)
    faces.append(tuple(3*n+j for j in range(n)))
    ids.append(11)
    ob=mesh(name,verts,faces,ids)
    body.append(ob)

# A squat, faceted iron incense bowl. The dark inset and raised outer rim are
# distinct from the central firebox, even in a top-down view.
loft('Bowl', [(0,0,-.12,.35),(0,0,-.06,.48),(0,0,.16,.51),(0,0,.24,.47)], [11,12,11], 11, 11)
loft('Rim', [(0,0,.205,.49),(0,0,.26,.50),(0,0,.29,.43)], [13,13], 11, 11)
loft('Lid', [(0,-.045,.255,.39),(0,-.045,.31,.36),(0,-.045,.38,.25),(0,-.045,.40,.20)], [12,5,11], 11, 11)
# Large angular vent cuts are rendered as dark insets over the sloping lid.
plate('Vent_L', [(-.27,-.05),(-.19,-.25),(-.09,-.23),(-.14,-.05)], .367,.374,11,11)
plate('Vent_R', [( .27,-.05),( .14,-.05),( .09,-.23),( .19,-.25)], .367,.374,11,11)
# Small ember heart, kept subordinate to the iron and bone.
loft('Ember', [(0,.02,.399,.105),(0,.02,.415,.075)], [12], 11, 10, 6, 0)
# Censer's broad brass prow and layered ridge.
plate('Prow', [(-.24,-.37),(0,-.94),(.24,-.37)], -.04,.16,13,11)
plate('Prow_ridge', [(-.105,-.41),(0,-.83),(.105,-.41),(0,-.49)], .16,.205,12,13)
# Heavy shoulder housings support the fixed-position gun mouths.
for s in (-1,1):
    plate('Gun_mount', [(s*.30,-.20),(s*.51,-.18),(s*.53,-.43),(s*.44,-.49),(s*.31,-.40)], .03,.20,12,11)
    tube_y('Muzzle', s*.41, -.52, -.30, .24, .095, .053, 11, 11)
    # Broad, beveled-looking cage straps around the central bowl.
    plate('Strap', [(s*.38,.20),(s*.47,.16),(s*.46,-.20),(s*.37,-.24)], .225,.275,13,12)
    # Swept bone vanes preserve the two original rear prongs. Separate hinged
    # meshes permit a gentle mirrored flap without carrying the weapons.
    fin=[]
    plate('Fin', [(s*.17,.39),(s*.48,.98),(s*.14,.76)], .015,.16,15,6, fin)
    plate('Fin_ridge', [(s*.19,.43),(s*.43,.91),(s*.20,.72)], .16,.185,6,14, fin)
    bpy.ops.object.select_all(action='DESELECT')
    for ob in fin: ob.select_set(True)
    bpy.context.view_layer.objects.active=fin[0]
    bpy.ops.object.join()
    fin_ob=bpy.context.object
    fin_ob.name='Fin_L' if s < 0 else 'Fin_R'
    # Origin remains the local hinge after transforms are applied below.
    pivot=Vector((s*.17,.39,.08))
    for vert in fin_ob.data.vertices:
        vert.co -= pivot
    fin_ob.location=pivot
    parts.append((fin_ob, {'object':fin_ob.name, 'motion':'swing',
                           'axis':[0,s,0], 'pivot':list(pivot),
                           'speed':1.4, 'amplitude':12}))

# Join the stationary geometry only, apply transforms, and triangulate explicitly.
bpy.ops.object.select_all(action='DESELECT')
for ob in body: ob.select_set(True)
bpy.context.view_layer.objects.active=body[0]
bpy.ops.object.join()
model=bpy.context.object
model.name='Model'
for ob in [model]+[p[0] for p in parts]:
    bpy.ops.object.select_all(action='DESELECT')
    ob.select_set(True)
    bpy.context.view_layer.objects.active=ob
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    mod=ob.modifiers.new('Triangulate', 'TRIANGULATE')
    bpy.ops.object.modifier_apply(modifier=mod.name)

with open(os.path.join(OUT,'parts.json'),'w',encoding='utf-8') as f:
    json.dump([p[1] for p in parts], f, indent=2)

bpy.ops.object.select_all(action='DESELECT')
model.select_set(True)
for ob, _ in parts: ob.select_set(True)
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
scene.world=bpy.data.worlds.new('Dark blue-gray world')
scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.05,.07,.09,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=1
scene.view_settings.view_transform='Standard'

def aim(ob, point):
    ob.rotation_euler=(Vector(point)-ob.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, location, energy, size=None):
    data=bpy.data.lights.new(name,kind)
    data.energy=energy
    if kind == 'AREA': data.shape='DISK'; data.size=size
    ob=bpy.data.objects.new(name,data)
    bpy.context.collection.objects.link(ob)
    ob.location=location
    aim(ob,(0,0,0))
    return ob

sun=light('Sun','SUN',(-3,4,6),2.0)
sun.data.angle=math.radians(18)
light('Soft fill','AREA',(4,-3,5),180,5)
camdata=bpy.data.cameras.new('Camera')
cam=bpy.data.objects.new('Camera',camdata)
bpy.context.collection.objects.link(cam)
scene.camera=cam
cam.location=(0,0,6)
cam.rotation_euler=(0,0,0) # camera local -Z looks down; local +Y is screen up
camdata.type='ORTHO'
camdata.ortho_scale=2.25
scene.render.filepath=os.path.join(OUT,'top.png')
bpy.ops.render.render(write_still=True)
cam.location=(2.8,-3.7,3.4)
aim(cam,(0,0,.08))
camdata.type='PERSP'
camdata.lens=48
scene.render.filepath=os.path.join(OUT,'angle.png')
bpy.ops.render.render(write_still=True)
