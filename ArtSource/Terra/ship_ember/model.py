import bpy
import math
import json
import os
import sys
from mathutils import Vector

# Ember: a compact, three-gun, twin-engine racing interceptor.
if '__file__' in globals():
    OUT = os.path.dirname(os.path.abspath(__file__))
else:
    args = sys.argv
    if '--' not in args or not args[args.index('--') + 1:]:
        raise RuntimeError('Pass the script path or output directory after --')
    path = os.path.abspath(args[args.index('--') + 1])
    OUT = path if os.path.isdir(path) else os.path.dirname(path)
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
    for block in list(datablocks):
        if block.users == 0:
            datablocks.remove(block)

palette = [
    (0.64, 0.075, 0.085, 1), # racing red
    (0.38, 0.045, 0.055, 1), # crimson sides
    (0.78, 0.13, 0.12, 1),  # vermilion highlight
    (0.22, 0.25, 0.27, 1),   # charcoal armour
    (0.075, 0.09, 0.10, 1),  # recesses
    (0.12, 0.72, 0.29, 1),   # emerald canopy
    (0.43, 0.95, 0.31, 1),   # lime canopy facets
    (0.96, 0.30, 0.035, 1),  # orange heat markings
    (1.0, 0.68, 0.10, 1),    # amber
    (0.42, 0.46, 0.45, 1),   # metal
    (0.27, 0.035, 0.045, 1), # deep red
    (0.70, 0.72, 0.65, 1),   # pale markings
]
mats = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = 0.72
    bsdf.inputs['Metallic'].default_value = 0.12 if i in (3, 4, 9) else 0.0
    mats.append(mat)

pieces = []
def mesh_piece(name, verts, faces, indices):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.materials.clear()
    for mat in mats:
        mesh.materials.append(mat)
    mesh.update()
    ob = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(ob)
    for p, idx in zip(mesh.polygons, indices):
        p.material_index = idx
        p.use_smooth = False
    pieces.append(ob)
    return ob

def slab(name, outline, bottom, top, inset, top_mat, side_mat, bevel_mat=None):
    # Two nested rings create actual chamfered armour edges and a closed underside.
    n = len(outline)
    cx = sum(p[0] for p in outline) / n
    cy = sum(p[1] for p in outline) / n
    inner = [(cx + (x-cx)*inset, cy + (y-cy)*inset) for x,y in outline]
    verts = [(x,y,bottom) for x,y in outline] + [(x,y,top-0.008) for x,y in outline] + [(x,y,top) for x,y in inner]
    faces = [tuple(reversed(range(n))), tuple(range(2*n,3*n))]
    ids = [side_mat, top_mat]
    for i in range(n):
        j = (i+1)%n
        faces.extend([(i,j,n+j,n+i), (n+i,n+j,2*n+j,2*n+i)])
        ids.extend([side_mat, bevel_mat if bevel_mat is not None else top_mat])
    return mesh_piece(name, verts, faces, ids)

def ring_y(name, x, y_front, y_back, z, outer, inner, rim_mat=9):
    # The opening faces -Y. The inner wall and inset back disk make real depth.
    n = 12
    verts = []
    for y, rad in [(y_front,outer),(y_front,inner),(y_back,inner*0.82)]:
        verts += [(x+math.cos(2*math.pi*i/n)*rad, y, z+math.sin(2*math.pi*i/n)*rad) for i in range(n)]
    faces, ids = [], []
    for i in range(n):
        j=(i+1)%n
        faces += [(i,j,n+j,n+i),(n+i,n+j,2*n+j,2*n+i)]
        ids += [rim_mat,4]
    faces.append(tuple(range(2*n,3*n)))
    ids.append(4)
    return mesh_piece(name,verts,faces,ids)

def barrel(name, x, tip, z, radius):
    # Barrel points toward +Y; dark annulus at the true forward muzzle.
    n=10
    verts=[]
    for y,r in [(tip-0.105,radius*1.25),(tip-0.012,radius),(tip,radius),(tip,radius*0.53),(tip-0.009,radius*0.53)]:
        verts += [(x+math.cos(2*math.pi*i/n)*r,y,z+math.sin(2*math.pi*i/n)*r) for i in range(n)]
    faces=[]; ids=[]
    for k in range(4):
        for i in range(n):
            j=(i+1)%n
            faces.append((k*n+i,k*n+j,(k+1)*n+j,(k+1)*n+i))
            ids.append([3,9,9,4][k])
    faces.append(tuple(range(4*n,5*n))); ids.append(4)
    return mesh_piece(name,verts,faces,ids)

# A narrow spear and two independent nacelles leave open channels on either side.
slab('Central hull',[(-.045,-.255),(-.073,-.185),(-.066,.075),(-.045,.19),(0,.342),(.045,.19),(.066,.075),(.073,-.185),(.045,-.255)],-.040,.035,.84,0,1,2)
slab('Dorsal armour',[(-.042,-.19),(-.049,-.07),(-.041,.10),(-.025,.18),(0,.278),(.025,.18),(.041,.10),(.049,-.07),(.042,-.19),(0,-.226)],.035,.047,.88,0,1,2)
slab('Belly keel',[(-.045,-.20),(-.050,-.055),(0,.285),(.050,-.055),(.045,-.20),(0,-.24)],-.053,-.039,.86,1,3,10)
slab('Nose channel',[(-.014,.16),(0,.324),(.014,.16)],.047,.050,.92,3,1,9)
barrel('Center cannon',0,.40,.032,.009)
slab('Center gun collar',[(-.018,.29),(-.015,.325),(.015,.325),(.018,.29)],.030,.045,.84,3,4,9)

for s, tag in [(-1,'L'),(1,'R')]:
    def mirror(poly):
        return [(s*x,y) for x,y in (poly if s==1 else list(reversed(poly)))]
    # Two short swept bridges connect each nacelle, leaving a long visible gap.
    slab('Forward bridge '+tag,mirror([(.050,.085),(.082,.142),(.127,.196),(.153,.151),(.086,.028),(.061,.018)]),-.018,.021,.88,1,10,0)
    slab('Aft bridge '+tag,mirror([(.056,-.177),(.083,-.145),(.119,-.185),(.130,-.221),(.070,-.223)]),-.024,.014,.88,3,10,9)
    slab('Nacelle '+tag,mirror([(.106,-.267),(.105,-.095),(.115,.061),(.137,.133),(.160,.092),(.188,-.053),(.199,-.245),(.183,-.277)]),-.038,.037,.83,1,10,0)
    slab('Nacelle top '+tag,mirror([(.116,-.240),(.115,-.096),(.126,.052),(.139,.093),(.153,.058),(.176,-.075),(.184,-.238),(.169,-.255),(.130,-.255)]),.037,.048,.86,0,1,2)
    slab('Outer blade '+tag,mirror([(.168,-.054),(.213,-.105),(.300,-.192),(.267,-.217),(.224,-.202),(.207,-.264),(.185,-.250)]),-.024,.018,.88,0,1,2)
    slab('Blade armour '+tag,mirror([(.183,-.103),(.228,-.147),(.280,-.191),(.259,-.198),(.217,-.181),(.203,-.214)]),.018,.024,.88,1,3,0)
    slab('Hot wing tip '+tag,mirror([(.267,-.176),(.300,-.192),(.278,-.204),(.254,-.190)]),.024,.028,.90,7,1,8)
    slab('Cannon mount '+tag,mirror([(.102,.114),(.104,.192),(.112,.225),(.128,.225),(.137,.192),(.137,.110)]),.018,.038,.83,3,1,9)
    slab('Cannon shoulder '+tag,mirror([(.106,.13),(.108,.19),(.115,.207),(.126,.207),(.132,.19),(.132,.13)]),.038,.044,.86,0,3,2)
    barrel('Root cannon '+tag,s*.12,.30,.033,.009)
    slab('Nozzle housing '+tag,mirror([(.118,-.250),(.182,-.250),(.184,-.278),(.175,-.288),(.125,-.288),(.116,-.278)]),-.032,.032,.85,3,10,9)
    ring_y('Recessed engine '+tag,s*.15,-.30,-.264,-.003,.030,.021,9)
    slab('Cooling recess '+tag,mirror([(.124,-.085),(.167,-.095),(.175,-.205),(.125,-.205)]),.048,.051,.96,4,3,3)
    for j in range(3):
        y=-.112-j*.029
        slab('Cooling rib '+tag,mirror([(.129,y),(.168,y-.002),(.169,y-.010),(.129,y-.008)]),.051,.054,.92,9,3,9)
    slab('Caution patch '+tag,mirror([(.166,-.025),(.177,-.049),(.184,-.077),(.173,-.060)]),.048,.051,.94,8,3,7)
    slab('Caution slash '+tag,mirror([(.169,-.036),(.172,-.043),(.180,-.055),(.178,-.047)]),.051,.053,.95,4,4,4)
    slab('Nacelle underside '+tag,mirror([(.120,-.23),(.119,-.095),(.135,.035),(.153,.025),(.176,-.095),(.181,-.23)]),-.045,-.038,.87,3,10,9)
    slab('Lower engine panel '+tag,mirror([(.13,-.21),(.17,-.21),(.17,-.13),(.13,-.13)]),-.049,-.045,.90,1,3,9)

# A shallow twelve-sided glass dome, exactly centered and below z=0.08.
n=12
angles=[2*math.pi*i/n for i in range(n)]
outline=[(.051*math.cos(a),.08+.051*math.sin(a)) for a in angles]
slab('Canopy rim',outline,.046,.051,.97,3,1,9)
verts=[(r*math.cos(a),.08+r*math.sin(a),z) for r,z in [(.047,.051),(.038,.065),(.023,.075)] for a in angles]
verts.append((0,.08,.079))
faces=[]; ids=[]
for k in range(2):
    for i in range(n):
        j=(i+1)%n
        faces.append((k*n+i,k*n+j,(k+1)*n+j,(k+1)*n+i))
        ids.append(6 if i in (2,3,4) else 5)
for i in range(n):
    faces.append((2*n+i,2*n+(i+1)%n,3*n)); ids.append(6 if i in (2,3,4) else 5)
mesh_piece('Emerald dome',verts,faces,ids)
slab('Aft spine well',[(-.037,-.20),(-.045,-.12),(-.032,-.075),(.032,-.075),(.045,-.12),(.037,-.20)],.047,.050,.95,4,1,3)
slab('Aft spine armour',[(-.029,-.19),(-.035,-.125),(-.026,-.09),(.026,-.09),(.035,-.125),(.029,-.19)],.050,.056,.86,3,4,9)
for y in (-.12,-.145,-.17):
    slab('Aft cooling fin',[(-.024,y),(-.024,y-.007),(.024,y-.007),(.024,y)],.056,.059,.93,9,3,9)
slab('Ember spine mark',[(-.006,-.205),(-.006,-.19),(.006,-.19),(.006,-.205)],.048,.051,.95,8,1,7)

# Join, bake all transforms, and triangulate once for a predictable game mesh.
bpy.ops.object.select_all(action='DESELECT')
for ob in pieces:
    ob.select_set(True)
bpy.context.view_layer.objects.active = pieces[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
tri = model.modifiers.new('Export triangulation', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
for poly in model.data.polygons:
    poly.use_smooth = False
with open(os.path.join(OUT, 'parts.json'), 'w', encoding='utf-8') as f:
    json.dump([], f)
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
scene.render.film_transparent=False
scene.world = bpy.data.worlds.new('Studio world') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes=True
background=scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value=(.05,.07,.09,1)
background.inputs['Strength'].default_value=1.0
scene.view_settings.view_transform='Standard'

def point_at(ob, target):
    ob.rotation_euler=(Vector(target)-ob.location).to_track_quat('-Z','Y').to_euler()
def light(name, kind, location, energy, target, size=None):
    data=bpy.data.lights.new(name, kind)
    data.energy=energy
    if size is not None:
        data.shape='DISK'; data.size=size
    ob=bpy.data.objects.new(name,data)
    bpy.context.collection.objects.link(ob)
    ob.location=location
    point_at(ob,target)
    return ob
sun=light('Upper left sun','SUN',(-3,4,6),2.0,(0,0,0))
sun.data.angle=math.radians(15)
light('Soft fill','AREA',(3,-2,4),150,(0,0,0),5)
cam_data=bpy.data.cameras.new('Camera')
cam=bpy.data.objects.new('Camera',cam_data)
bpy.context.collection.objects.link(cam)
scene.camera=cam
cam.location=(0,.05,3)
cam.rotation_euler=(0,0,0)
cam_data.type='ORTHO'
cam_data.ortho_scale=.92
scene.render.filepath=os.path.join(OUT,'top.png')
bpy.ops.render.render(write_still=True)
cam.location=(.70,.90,.95)
point_at(cam,(0,.04,0))
cam_data.type='PERSP'
cam_data.lens=50
cam_data.sensor_width=36
scene.render.filepath=os.path.join(OUT,'angle.png')
bpy.ops.render.render(write_still=True)


