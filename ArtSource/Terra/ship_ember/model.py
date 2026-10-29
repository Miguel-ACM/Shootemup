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
    (0.80, 0.035, 0.045, 1), # racing red
    (0.49, 0.018, 0.032, 1), # crimson sides
    (0.98, 0.09, 0.065, 1),  # vermilion highlight
    (0.19, 0.22, 0.24, 1),   # charcoal armour
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
    for y,r in [(tip-0.057,radius*1.25),(tip-0.008,radius),(tip,radius),(tip,radius*0.53),(tip-0.009,radius*0.53)]:
        verts += [(x+math.cos(2*math.pi*i/n)*r,y,z+math.sin(2*math.pi*i/n)*r) for i in range(n)]
    faces=[]; ids=[]
    for k in range(4):
        for i in range(n):
            j=(i+1)%n
            faces.append((k*n+i,k*n+j,(k+1)*n+j,(k+1)*n+i))
            ids.append([3,9,9,4][k])
    faces.append(tuple(range(4*n,5*n))); ids.append(4)
    return mesh_piece(name,verts,faces,ids)

# Broad pointed delta silhouette. The split trailing edge reveals the twin nacelles.
slab('Main delta', [(-.105,-.255),(-.215,-.235),(-.300,-.205),(-.235,-.075),(-.145,.095),(0,.383),(.145,.095),(.235,-.075),(.300,-.205),(.215,-.235),(.105,-.255)], -.042, .018, .88, 0, 1, 2)
# Independently layered swept blades create long directional lines.
for s, tag in [(-1,'L'),(1,'R')]:
    def mirror(poly):
        return [(s*x,y) for x,y in (poly if s==1 else list(reversed(poly)))]
    slab('Wing '+tag, mirror([(.080,.065),(.130,.205),(.170,.148),(.225,-.055),(.298,-.196),(.205,-.177),(.147,-.116)]), .012,.034,.88,0,1,2)
    slab('Wing dark inset '+tag, mirror([(.155,.099),(.186,.040),(.239,-.145),(.212,-.136)]), .034,.038,.94,10,1,10)
    slab('Wing tip heat '+tag, mirror([(.244,-.153),(.296,-.194),(.271,-.193),(.235,-.174)]), .035,.039,.90,7,1,8)
    # Long raised engine shoulders, visible from top and in a roll.
    slab('Engine cowling '+tag, mirror([(.104,-.263),(.104,-.115),(.128,.008),(.176,-.037),(.196,-.164),(.194,-.270)]), -.025,.047,.84,1,10,0)
    slab('Engine upper plate '+tag, mirror([(.115,-.230),(.115,-.125),(.135,-.048),(.166,-.066),(.182,-.155),(.182,-.235)]), .047,.053,.88,0,1,2)
    # Grouped heat slits, kept large enough to read at game scale.
    for j in range(3):
        y=-.105-j*.033
        slab('Heat vent', mirror([(.132,y),(.166,y-.006),(.169,y-.016),(.133,y-.010)]), .054,.056,.92,7,10,8)
    ring_y('Engine nozzle '+tag, s*.15, -.300, -.264, -.002, .030, .021, 9)
    barrel('Root cannon '+tag, s*.12, .30, .034, .009)
    slab('Gun root mount '+tag, mirror([(.101,.188),(.105,.257),(.119,.282),(.137,.256),(.137,.184)]), .020,.041,.83,3,1,9)

# Faceted central fuselage: angular loft with ridge and underside facets.
verts=[(-.075,-.25,.012),(.075,-.25,.012),(-.090,-.08,.018),(.090,-.08,.018),(-.065,.18,.016),(.065,.18,.016),(0,.375,.012),
       (-.050,-.23,.058),(.050,-.23,.058),(-.058,-.075,.075),(.058,-.075,.075),(-.041,.16,.050),(.041,.16,.050),(0,.326,.025),
       (0,-.24,-.046),(-.055,-.07,-.049),(.055,-.07,-.049),(0,.32,-.018)]
faces=[(0,2,9,7),(2,4,11,9),(4,6,13,11),(1,8,10,3),(3,10,12,5),(5,12,13,6),
       (7,9,10,8),(9,11,12,10),(11,13,12),(0,7,8,1),(0,14,15,2),(2,15,17,6,4),(1,3,16,14),(3,5,6,17,16),(0,1,14),(14,16,17,15)]
mesh_piece('Fuselage',verts,faces,[1,0,2,1,0,2,0,2,0,10,10,1,10,1,10,3])
# Compact octagonal hitbox canopy centered at (0, .08). The existing
# fuselage plating remains underneath the rim and fills the former canopy area.
n = 8
cx, cy = 0.0, .08
angles = [2 * math.pi * i / n + math.pi / 8 for i in range(n)]
outline = [(cx + .065 * math.cos(a), cy + .065 * math.sin(a)) for a in angles]
slab('Canopy rim', outline, .052, .075, .97, 3, 3, 9)
# Two octagonal rings and a crown form a shallow, faceted glass bubble.
verts = [(cx + r * math.cos(a), cy + r * math.sin(a), z)
         for r, z in [(.058, .075), (.044, .095), (.023, .108)] for a in angles]
verts.append((cx, cy, .112))
faces, ids = [], []
for ring in range(2):
    for i in range(n):
        j = (i + 1) % n
        faces.append((ring*n+i, ring*n+j, (ring+1)*n+j, (ring+1)*n+i))
        ids.append(5 if ring == 0 else (6 if i in (1, 2, 3) else 5))
for i in range(n):
    faces.append((2*n+i, 2*n+(i+1)%n, 3*n))
    ids.append(6 if i in (1, 2, 3) else 5)
mesh_piece('Emerald canopy', verts, faces, ids)
# Dorsal spine and three clear forward muzzle lips.
slab('Nose spine',[(-.022,.151),(0,.346),(.022,.151)], .026,.039,.90,2,1,0)
barrel('Nose cannon',0,.40,.022,.009)
slab('Tail spine',[(-.035,-.24),(-.043,-.145),(0,-.115),(.043,-.145),(.035,-.24)], .056,.063,.89,3,1,9)
slab('Tail stripe',[(-.007,-.224),(-.007,-.155),(.007,-.155),(.007,-.224)], .064,.066,.93,8,3,7)
# Contrasting armour facets underneath keep the model finished through rolls.
slab('Under keel',[(-.048,-.21),(-.058,-.06),(0,.27),(.058,-.06),(.048,-.21),(0,-.25)], -.055,-.048,.88,10,3,1)

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

