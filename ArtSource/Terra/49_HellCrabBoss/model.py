import bpy, math, os, sys, json
from mathutils import Vector

# Blender 5.x: run with blender --background --python model.py [-- directory]
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    OUT = os.path.abspath(sys.argv[sys.argv.index('--') + 1]) if '--' in sys.argv and len(sys.argv) > sys.argv.index('--') + 1 else os.getcwd()
os.makedirs(OUT, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

colors = [(0.24,0.35,0.40),(0.78,0.83,0.79),(0.35,0.43,0.30),(0.95,0.38,0.12),(0.32,0.48,0.55),(0.54,0.18,0.16),(0.53,0.61,0.62),(0.34,0.31,0.48),(0.20,0.36,0.40),(0.16,0.83,0.91),(1.0,0.77,0.27),(0.055,0.09,0.12),(0.62,0.25,0.19),(0.82,0.64,0.26),(0.53,0.61,0.55),(0.80,0.84,0.83)]
mats=[]
for i, col in enumerate(colors):
    m=bpy.data.materials.new('tile_%02d'%i); m.diffuse_color=(*col,1); m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF'); p.inputs['Base Color'].default_value=(*col,1)
    p.inputs['Roughness'].default_value=.72
    if i in (9,10):
        p.inputs['Emission Color'].default_value=(*col,1); p.inputs['Emission Strength'].default_value=1.5
    mats.append(m)

def mesh(name, verts, faces, mat):
    me=bpy.data.meshes.new(name); me.from_pydata(verts, [], faces); me.update()
    ob=bpy.data.objects.new(name,me); bpy.context.collection.objects.link(ob); ob.data.materials.append(mats[mat]); return ob

def layered(name, outline, z0, z1, inset, mat, side=11):
    # A faceted bevel and a shallow raised crown, rather than a flat extrusion.
    if sum(outline[i][0]*outline[(i+1)%len(outline)][1] - outline[(i+1)%len(outline)][0]*outline[i][1] for i in range(len(outline))) < 0: outline=list(reversed(outline))
    n=len(outline); cx=sum(p[0] for p in outline)/n; cy=sum(p[1] for p in outline)/n
    inner=[(cx+(x-cx)*inset,cy+(y-cy)*inset) for x,y in outline]
    vs=[(x,y,z0) for x,y in outline]+[(x,y,z1-.09) for x,y in outline]+[(x,y,z1) for x,y in inner]+[(cx,cy,z1+.025)]
    fs=[tuple(reversed(range(n)))]; ids=[side]
    for j in range(n):
        k=(j+1)%n; fs.extend([(j,k,n+k,n+j),(n+j,n+k,2*n+k,2*n+j),(2*n+j,2*n+k,3*n)]); ids.extend([side,mat,mat])
    o=mesh(name,vs,fs,mat); o.data.materials.append(mats[side])
    for p, idx in zip(o.data.polygons,ids): p.material_index=0 if idx==mat else 1
    return o

def ring(name, center, outer, inner, depth, mat, axis='Z', n=12):
    # Annular walls leave a genuine opening and an inset dark interior.
    x,y,z=center; vs=[]
    for rr, d in [(outer,0),(outer,-depth),(inner,0),(inner,-depth)]:
        for i in range(n):
            a=2*math.pi*i/n; u=rr*math.cos(a); v=rr*math.sin(a)
            vs.append((x+u,y+v,z+d) if axis=='Z' else (x+u,y+d,z+v))
    fs=[]
    for i in range(n):
        j=(i+1)%n; fs.extend([(i,j,2*n+j,2*n+i),(i,n+i,n+j,j),(2*n+i,2*n+j,3*n+j,3*n+i)])
    return mesh(name,vs,fs,mat)

def disk(name, center, radius, mat, axis='Z', n=12):
    x,y,z=center; vs=[center]
    for i in range(n):
        a=2*math.pi*i/n; u=radius*math.cos(a); v=radius*math.sin(a)
        vs.append((x+u,y+v,z) if axis=='Z' else (x+u,y,z+v))
    return mesh(name,vs,[(0,1+i,1+(i+1)%n) for i in range(n)],mat)

def cut_cylinder(target, location, radius, depth, axis='Z'):
    bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=radius, depth=depth, location=location, rotation=(math.pi/2,0,0) if axis=='Y' else (0,0,0))
    cutter=bpy.context.object
    bpy.context.view_layer.objects.active=target
    mod=target.modifiers.new('Carved socket','BOOLEAN'); mod.operation='DIFFERENCE'; mod.object=cutter
    bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.data.objects.remove(cutter,do_unlink=True)

def prism(name, points, z0, z1, mat):
    return layered(name, points,z0,z1,.72,mat,11)

outline=[(-1.78,.10),(-1.38,1.05),(-.55,1.45),(.55,1.45),(1.38,1.05),(1.78,.10),(1.15,-.95),(.52,-1.35),(-.52,-1.35),(-1.15,-.95)]
hull=layered('Carapace underbody',outline,-.39,.45,.73,5,11)
# Eye pits are cut down into the shell; these accept the game's separate eyes.
for s in (-1,1):
    cut_cylinder(hull,(s*.67,-.52,.48),.40,.72)
    ring('Eye socket rim',(s*.67,-.52,.49),.46,.40,.20,12)
    disk('Eye socket floor',(s*.67,-.52,.16),.395,11)
    # Engine exits are exactly at the gameplay socket centres, facing +Y.
    cut_cylinder(hull,(s*.874,1.24,0),.16,.85,'Y')
    ring('Engine socket',(s*.874,.93,0),.16,.115,.16,6,'Y')
    disk('Engine dark bore',(s*.874,.91,0),.113,11,'Y')

# Four distinct overlapping plates: tapered seams, raised central ridge, rust-red shell.
plates=[([(-1.16,.80),(-.88,1.15),(0,1.25),(.88,1.15),(1.16,.80),(.94,.54),(0,.43),(-.94,.54)],.53,12),
        ([(-1.40,.47),(-1.10,.73),(0,.82),(1.10,.73),(1.40,.47),(1.10,.16),(0,.06),(-1.10,.16)],.61,5),
        ([(-1.43,.12),(-1.11,.36),(0,.45),(1.11,.36),(1.43,.12),(1.10,-.18),(0,-.29),(-1.10,-.18)],.67,12),
        ([(-1.15,-.23),(-.98,-.02),(0,.08),(.98,-.02),(1.15,-.23),(.92,-.42),(0,-.60),(-.92,-.42)],.69,5)]
for i,(p,z,mat) in enumerate(plates):
    ob=layered('Overlapping shell %d'%i,p,z-.14,z,.80,mat,11)
    for s in (-1,1): cut_cylinder(ob,(s*.67,-.52,.55),.40,1.4)
# A small raised central keel and angular, swept bone spurs on the rear.
prism('Crown ridge',[(-.18,.91),(0,1.08),(.18,.91),(.23,.02),(0,-.20),(-.23,.02)],.66,.75,5)
for s in (-1,1):
    for i in range(3):
        x=1.12-i*.18; y=.75+i*.19
        prism('Bone spur',[(s*(x-.28),y+.15),(s*(1.75-i*.32),1.48+i*.18),(s*x,y-.05)],.18,.43-i*.025,1)
    # Three stout articulated-looking legs remain part of the static hull.
    for j in range(3):
        y=.58-j*.50
        prism('Leg root',[(s*1.20,y+.11),(s*1.62,y-.05),(s*1.62,y-.29),(s*1.38,y-.24)],-.08,.20,5)
        prism('Leg armor',[(s*1.46,y-.05),(s*2.08,y-.42-j*.08),(s*1.74,y-.82-j*.02),(s*1.57,y-.45)],-.09,.24,6)
        prism('Leg ridge',[(s*1.55,y-.11),(s*1.94,y-.40-j*.08),(s*1.70,y-.58-j*.02)],.23,.28,12)
    # Claw attachment: an open, dark recessed socket at the side, not an extra claw.
    ring('Claw socket rim',(s*1.60,-.28,.28),.28,.205,.12,5)
    disk('Claw socket recess',(s*1.60,-.28,.18),.20,11)
# Dark mouth and individually sculpted blunt bone teeth.
prism('Mouth',[(-.52,-.92),(.52,-.92),(.49,-1.20),(.27,-1.30),(-.27,-1.30),(-.49,-1.20)],.38,.49,11)
for x in (-.30,-.15,0,.15,.30):
    prism('Tooth',[(x-.055,-1.00),(x+.055,-1.00),(x+.045,-1.17),(x,-1.23),(x-.045,-1.17)],.48,.58,1)

# Join the static body. There are no naturally moving parts on this body: claws and
# eyes are provided as independent game objects, not duplicated here.
bpy.ops.object.select_all(action='DESELECT')
body=[o for o in bpy.context.scene.objects if o.type=='MESH']
for o in body: o.select_set(True)
bpy.context.view_layer.objects.active=hull
bpy.ops.object.join(); hull.name='Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for p in hull.data.polygons: p.use_smooth=False
with open(os.path.join(OUT,'parts.json'),'w',encoding='utf-8') as f: json.dump([],f,indent=2)
# Select only geometry before export; no cameras or lights enter the OBJ.
bpy.ops.wm.obj_export(filepath=os.path.join(OUT,'model.obj'), export_selected_objects=True, export_materials=True, forward_axis='Y', up_axis='Z')

scene=bpy.context.scene
scene.render.engine='CYCLES'; scene.cycles.device='CPU'; scene.cycles.samples=48
scene.render.resolution_x=640; scene.render.resolution_y=640; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'; scene.render.film_transparent=False
world=bpy.data.worlds.new('Blue gray world') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world=world; world.use_nodes=True
world.node_tree.nodes['Background'].inputs['Color'].default_value=(.05,.07,.09,1)
world.node_tree.nodes['Background'].inputs['Strength'].default_value=1
scene.view_settings.view_transform='Standard'
def point_at(obj, target): obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()
def light(name, typ, loc, energy, target, size=1):
    data=bpy.data.lights.new(name,typ); data.energy=energy
    if typ=='AREA': data.shape='DISK'; data.size=size
    if typ=='SUN': data.angle=math.radians(12)
    obj=bpy.data.objects.new(name,data); bpy.context.collection.objects.link(obj); obj.location=loc; point_at(obj,target)
light('Upper left sun','SUN',(-4,-5,8),2.0,(0,0,0))
light('Soft fill','AREA',(4,1,6),320,(0,0,0),5)
camdata=bpy.data.cameras.new('Camera'); cam=bpy.data.objects.new('Camera',camdata); bpy.context.collection.objects.link(cam); scene.camera=cam
camdata.type='ORTHO'; camdata.ortho_scale=4.9; cam.location=(0,0,10); cam.rotation_euler=(0,0,0)
scene.render.filepath=os.path.join(OUT,'top.png'); bpy.ops.render.render(write_still=True)
camdata.type='PERSP'; camdata.lens=50; cam.location=(5.4,-7.4,6.7); point_at(cam,(0,0,.1))
scene.render.filepath=os.path.join(OUT,'angle.png'); bpy.ops.render.render(write_still=True)

