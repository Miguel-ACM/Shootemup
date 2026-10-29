import bpy
import math
import json
import os
import sys
from mathutils import Vector

# DuneMoth: a ceramic-winged desert machine, with independently hinged wings.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
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
for i, rgb in enumerate(palette):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*rgb, 1.0)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*rgb, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.7
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*rgb, 1.0)
        bsdf.inputs['Emission Strength'].default_value = 1.8
    mats.append(m)

body = []
wings = {-1: [], 1: []}
def mesh_obj(name, verts, faces, material, group, face_materials=None):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    if face_materials:
        for n in sorted(set(face_materials)):
            mesh.materials.append(mats[n])
        keys = sorted(set(face_materials))
        for poly, n in zip(mesh.polygons, face_materials):
            poly.material_index = keys.index(n)
    else:
        mesh.materials.append(mats[material])
    group.append(obj)
    return obj

def layered(name, outline, inset, zbottom, zedge, ztop, material, topmat, group):
    # Corresponding edge and inset vertices give a broad faceted bevel and crown.
    n = len(outline)
    verts = [(x,y,zbottom) for x,y in outline] + [(x,y,zedge) for x,y in outline] + [(x,y,ztop) for x,y in inset]
    faces = [tuple(reversed(range(n)))]
    fm = [material]
    for i in range(n):
        j = (i+1)%n
        faces.append((i,j,n+j,n+i)); fm.append(material)
        faces.append((n+i,n+j,2*n+j,2*n+i)); fm.append(material)
    faces.append(tuple(range(2*n,3*n))); fm.append(topmat)
    return mesh_obj(name, verts, faces, material, group, fm)

def plate(name, coords, z0, z1, mat, group):
    n = len(coords)
    verts = [(x,y,z0) for x,y in coords] + [(x,y,z1) for x,y in coords]
    faces = [tuple(reversed(range(n))), tuple(range(n,2*n))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh_obj(name, verts, faces, mat, group)

def tube(name, x, yfront, z, radius, length, outermat, group, direction=-1):
    # A hollow, faceted lip with an inset dark bore; the opening is exactly at yfront.
    count = 10
    inner = radius * 0.58
    back = yfront - direction * length
    deep = yfront - direction * min(length * .65, .075)
    vs = []
    for yy, rr in [(back,radius*.85),(yfront,radius),(yfront,inner),(deep,inner)]:
        vs += [(x+rr*math.cos(2*math.pi*i/count), yy, z+rr*math.sin(2*math.pi*i/count)) for i in range(count)]
    fs=[]; fm=[]
    for ring in range(3):
        for i in range(count):
            j=(i+1)%count
            fs.append((ring*count+i,ring*count+j,(ring+1)*count+j,(ring+1)*count+i))
            fm.append(outermat if ring < 2 else 11)
    fs.append(tuple(range(3*count,4*count))); fm.append(11)
    return mesh_obj(name, vs, fs, outermat, group, fm)

# Sculpted central thorax: tapered nose, raised shoulder, and segmented abdomen.
outline = [(0,-.83),(-.17,-.39),(-.21,.14),(-.18,.67),(0,.74),(.18,.67),(.21,.14),(.17,-.39)]
inset = [(0,-.70),(-.105,-.36),(-.13,.12),(-.11,.57),(0,.63),(.11,.57),(.13,.12),(.105,-.36)]
layered('Thorax', outline, inset, -.075, .12, .29, 12, 13, body)
plate('Nose ceramic mask', [(0,-.76),(-.11,-.43),(-.075,-.31),(0,-.39),(.075,-.31),(.11,-.43)], .205,.24,15,body)
# Angular raised cockpit, its dark inset reading as a moth's head.
layered('Cockpit', [(-.13,-.35),(-.16,-.16),(-.13,.08),(0,.16),(.13,.08),(.16,-.16),(.13,-.35)],
        [(-.08,-.29),(-.10,-.14),(-.08,.04),(0,.09),(.08,.04),(.10,-.14),(.08,-.29)], .245,.285,.37, 11, 8, body)
plate('Cockpit crest', [(-.022,-.26),(-.026,.035),(0,.075),(.026,.035),(.022,-.26),(0,-.31)], .372,.379,13,body)
for yy in (.26,.40,.54):
    plate('Abdomen chevron', [(-.105,yy), (0,yy-.035),(.105,yy),(.10,yy+.035),(0,yy+.005),(-.10,yy+.035)], .291,.302,12,body)

for s in (-1,1):
    def mirror(points):
        pts=[(s*x,y) for x,y in points]
        return pts if s == 1 else list(reversed(pts))
    group = wings[s]
    # Broad moth wings with a scalloped trailing edge and faceted raised crowns.
    edge = mirror([(.15,.39),(.46,.53),(1.20,.67),(1.10,.32),(.92,-.27),(.74,-.19),(.59,-.25),(.43,-.08),(.19,.02)])
    inset = mirror([(.22,.34),(.48,.45),(1.07,.57),(.99,.31),(.87,-.16),(.73,-.105),(.60,-.15),(.43,-.015),(.24,.07)])
    layered('Wing',edge,inset,-.045,.075,.145,12,15,group)
    # Bold warm inner panels and a pair of graphic eye-spots, deliberately large at gameplay size.
    plate('Wing brass field',mirror([(.25,.30),(.49,.41),(.99,.54),(.82,.10),(.63,-.08),(.46,.035)]),.147,.154,13,group)
    plate('Wing rust slash',mirror([(.39,.27),(.91,.48),(.78,.32),(.48,.13)]),.155,.162,12,group)
    plate('Wing eye surround',mirror([(.61,.22),(.75,.30),(.85,.24),(.79,.11),(.64,.09),(.57,.14)]),.163,.17,11,group)
    plate('Wing eye ceramic',mirror([(.64,.20),(.74,.25),(.79,.22),(.75,.15),(.66,.14),(.62,.17)]),.171,.177,15,group)
    plate('Wing eye core',mirror([(.68,.20),(.72,.225),(.75,.205),(.72,.175),(.68,.175)]),.178,.183,5,group)
    # The structural arm and gun remain static while the wing flaps behind them.
    plate('Gun strut',mirror([(.13,-.10),(.19,.04),(.69,.02),(.76,-.10),(.68,-.16),(.31,-.10)]),.015,.085,11,body)
    tube('Gun barrel', s*.72, -.27, .16, .075, .25, 13, body, -1)
    tube('Engine socket', s*.29, .68, 0.0, .09, .20, 12, body, 1)
    # Brass collars around the rear sockets remain part of the hull.
    tube('Engine collar', s*.29, .635, 0.0, .106, .10, 13, body, 1)

# Join each collection without changing world-space geometry.
def join_group(objects, name):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = name
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    for p in obj.data.polygons:
        p.use_smooth = False
    return obj

model = join_group(body, 'Model')
left = join_group(wings[-1], 'Wing_L')
right = join_group(wings[1], 'Wing_R')
# Object origins at the actual hinge make the parts convenient in editors as well.
for obj, pivot in [(left,(-.18,.30,.08)),(right,(.18,.30,.08))]:
    bpy.context.scene.cursor.location = pivot
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
parts = [
    {'object':'Wing_L','motion':'swing','axis':[0,1,0],'pivot':[-.18,.30,.08],'speed':1.4,'amplitude':14},
    {'object':'Wing_R','motion':'swing','axis':[0,-1,0],'pivot':[.18,.30,.08],'speed':1.4,'amplitude':14},
]
with open(os.path.join(OUT,'parts.json'),'w',encoding='utf-8') as f:
    json.dump(parts,f,indent=2)

# Apply transforms after setting hinge origins; vertex coordinates remain in place.
bpy.ops.object.select_all(action='DESELECT')
for obj in (model,left,right):
    obj.select_set(True)
bpy.context.view_layer.objects.active = model
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
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
scene.world = bpy.data.worlds.new('Desert twilight') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (.05,.07,.09,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(obj, point):
    obj.rotation_euler = (Vector(point)-obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data = bpy.data.lights.new(name,kind)
    data.energy = energy
    if size is not None and kind == 'AREA':
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name,data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    aim(obj,target)
    return obj

sun=light('Sun','SUN',(-3,-4,6),2.0,(0,0,0))
sun.data.angle = math.radians(12)
light('Fill','AREA',(3,1,4),120,(0,0,0),5)
cam_data=bpy.data.cameras.new('Camera')
cam=bpy.data.objects.new('Camera',cam_data)
bpy.context.collection.objects.link(cam)
scene.camera=cam
cam.location=(0,0,5)
cam.rotation_euler=(0,0,0)
cam_data.type='ORTHO'
cam_data.ortho_scale=2.85
scene.render.filepath=os.path.join(OUT,'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type='PERSP'
cam_data.lens=48
cam.location=(3.1,-4.2,3.6)
aim(cam,(0,0,.08))
scene.render.filepath=os.path.join(OUT,'angle.png')
bpy.ops.render.render(write_still=True)
