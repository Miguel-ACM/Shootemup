import bpy, bmesh, math, os, sys, json
from mathutils import Vector

# BoneHarrier: a skeletal, swept-wing raider. Front is -Y, camera is +Z.
try:
    HERE = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    HERE = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
    if os.path.splitext(HERE)[1]:
        HERE = os.path.dirname(HERE)
os.makedirs(HERE, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

colors = [(0.24,0.35,0.40),(0.78,0.83,0.79),(0.35,0.43,0.30),(0.95,0.38,0.12),
          (0.32,0.48,0.55),(0.54,0.18,0.16),(0.53,0.61,0.62),(0.34,0.31,0.48),
          (0.20,0.36,0.40),(0.16,0.83,0.91),(1.0,0.77,0.27),(0.055,0.09,0.12),
          (0.62,0.25,0.19),(0.82,0.64,0.26),(0.53,0.61,0.55),(0.80,0.84,0.83)]
mats = []
for i, color in enumerate(colors):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*color, 1.0)
    m.use_nodes = True
    p = m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value = (*color, 1.0)
    p.inputs['Roughness'].default_value = 0.78
    if i in (9, 10):
        p.inputs['Emission Color'].default_value = (*color, 1.0)
        p.inputs['Emission Strength'].default_value = 1.5
    mats.append(m)

body, wings = [], {-1: [], 1: []}
def mesh(name, verts, faces, material, group, face_mats=None):
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts, [], faces)
    me.materials.clear()
    for mat in mats: me.materials.append(mat)
    me.update()
    ob = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(ob)
    for poly in me.polygons:
        poly.material_index = material if face_mats is None else face_mats[poly.index]
        poly.use_smooth = False
    group.append(ob)
    return ob

def plate(name, outline, zbottom, zedge, ztop, inset, topmat, sidemat, group):
    # Three polygon rings make a substantial faceted bevel, plus a bottom face.
    n = len(outline)
    cx = sum(p[0] for p in outline)/n
    cy = sum(p[1] for p in outline)/n
    v = [(x,y,zbottom) for x,y in outline] + [(x,y,zedge) for x,y in outline]
    v += [(cx+(x-cx)*inset, cy+(y-cy)*inset, ztop) for x,y in outline]
    f = [tuple(reversed(range(n)))]
    mi = [sidemat]
    for k in range(2):
        for i in range(n):
            j = (i+1)%n
            f.append((k*n+i,k*n+j,(k+1)*n+j,(k+1)*n+i))
            mi.append(sidemat if k == 0 else topmat)
    f.append(tuple(range(2*n,3*n)))
    mi.append(topmat)
    return mesh(name,v,f,topmat,group,mi)

def tapered(name, points, widths, zs, material, side, group):
    # Bone ridge along a polyline, with a high central crest and bevelled edges.
    verts=[]
    for i, (x,y) in enumerate(points):
        tangent = Vector(points[min(i+1,len(points)-1)]) - Vector(points[max(i-1,0)])
        tangent.normalize()
        dx,dy = -tangent.y*widths[i], tangent.x*widths[i]
        z=zs[i]
        verts.extend([(x+dx,y+dy,z-0.025),(x,y,z+0.045),(x-dx,y-dy,z-0.025)])
    faces=[]
    for i in range(len(points)-1):
        a=3*i; b=a+3
        faces += [(a,b,b+1,a+1),(a+1,b+1,b+2,a+2)]
    return mesh(name,verts,faces,material,group)

def tube_y(name, x, y_front, z, radius, length, group):
    # Hollow hexagonal gun: exact center of the forward mouth is the gameplay muzzle.
    n=8; verts=[]
    for y,r in [(y_front+length,radius*0.85),(y_front,radius),(y_front,radius*0.55),(y_front+0.065,radius*0.55)]:
        for j in range(n):
            a=2*math.pi*j/n+math.pi/8
            verts.append((x+math.cos(a)*r,y,z+math.sin(a)*r))
    faces=[]; ids=[]
    for ring,mat in [(0,12),(1,13),(2,11)]:
        for j in range(n):
            k=(j+1)%n
            faces.append((ring*n+j,ring*n+k,(ring+1)*n+k,(ring+1)*n+j)); ids.append(mat)
    faces.append(tuple(range(3*n,4*n))); ids.append(11)
    return mesh(name,verts,faces,12,group,ids)

# The central armored fuselage retains the original pointed, six-sided footprint.
plate('Keel',[(0,-1.03),(-.23,-.42),(-.29,.34),(0,.74),(.29,.34),(.23,-.42)],-.075,.10,.29,.78,15,11,body)
plate('Skull',[(0,-.70),(-.19,-.37),(-.23,.14),(-.18,.40),(0,.56),(.18,.40),(.23,.14),(.19,-.37)],.22,.27,.43,.75,15,6,body)
# A dark inset carries two slanted ember eyes, inset under a bone brow.
plate('Face mask',[(0,-.60),(-.145,-.34),(-.16,-.02),(0,.10),(.16,-.02),(.145,-.34)],.29,.32,.345,.91,11,11,body)
for s in (-1,1):
    def mirror(poly): return [(s*x,y) for x,y in poly]
    plate('Eye socket',mirror([(.035,-.16),(.16,-.09),(.14,.015),(.045,-.035)]),.35,.36,.37,.91,5,11,body)
    plate('Ember eye',mirror([(.055,-.125),(.135,-.08),(.12,-.045),(.06,-.065)]),.371,.374,.378,.95,10,5,body)
    plate('Brow',mirror([(.025,.03),(.19,.10),(.17,.17),(.035,.12)]),.355,.385,.415,.86,1,6,body)
    plate('Cheek',mirror([(.115,-.38),(.22,-.29),(.23,-.12),(.16,-.20)]),.27,.32,.37,.83,14,6,body)
# Crown/spine ridge and rear split tail points.
plate('Crown',[(0,.04),(-.10,.20),(-.11,.42),(0,.61),(.11,.42),(.10,.20)],.36,.40,.48,.7,1,6,body)
for s in (-1,1):
    plate('Tail spur',[(s*.04,.48),(s*.16,.52),(s*.24,.78),(s*.12,.67)],.10,.16,.23,.75,14,11,body)
tube_y('Muzzle',0,-.96,.23,.105,.23,body)

# Wings: broad dark membranes beneath pale bone spars and hooked rearward tips.
for s in (-1,1):
    g=wings[s]
    def mp(poly): return [(s*x,y) for x,y in poly]
    plate('Wing membrane',mp([(.19,.08),(.46,-.35),(.81,.31),(1.15,.51),(.98,.57),(.62,.75)]),-.045,.075,.17,.82,11,12,g)
    plate('Shoulder armor',mp([(.20,.06),(.37,-.15),(.56,.28),(.62,.55),(.39,.39)]),.09,.16,.25,.78,14,6,g)
    plate('Outer bone',mp([(.55,.39),(.66,.64),(.98,.56),(1.15,.51),(.80,.31)]),.08,.14,.20,.82,6,13,g)
    tapered('Leading spar',mp([(.31,-.14),(.53,.27),(.73,.46),(1.11,.51)]),[.045,.055,.055,.018],[.21,.24,.23,.17],15,6,g)
    tapered('Rib',mp([(.40,.17),(.58,.42),(.63,.70)]),[.035,.045,.015],[.22,.24,.19],1,6,g)
    plate('Wing hook',mp([(.32,.25),(.62,.97),(.53,.48),(.44,.38)]),.08,.15,.24,.78,15,6,g)
    plate('Rust wing inset',mp([(.59,.31),(.75,.36),(.94,.52),(.73,.45)]),.176,.18,.19,.9,12,11,g)

# Join and triangulate each independently. Pivots are stored in parts.json.
def finish(name, objects):
    bpy.ops.object.select_all(action='DESELECT')
    for ob in objects: ob.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    bpy.ops.object.join()
    ob=bpy.context.object
    ob.name=name
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    bm=bmesh.new(); bm.from_mesh(ob.data)
    bmesh.ops.triangulate(bm, faces=list(bm.faces))
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(ob.data); bm.free(); ob.data.update()
    return ob
model=finish('Model',body)
left=finish('Wing_L',wings[-1]); right=finish('Wing_R',wings[1])
parts=[{'object':'Wing_L','motion':'swing','axis':[0,-1,0],'pivot':[-.22,.10,.13],'speed':1.4,'amplitude':12},
       {'object':'Wing_R','motion':'swing','axis':[0,1,0],'pivot':[.22,.10,.13],'speed':1.4,'amplitude':12}]
with open(os.path.join(HERE,'parts.json'),'w',encoding='utf-8') as f: json.dump(parts,f,indent=2)
bpy.ops.object.select_all(action='DESELECT')
for ob in (model,left,right): ob.select_set(True)
bpy.context.view_layer.objects.active=model
bpy.ops.wm.obj_export(filepath=os.path.join(HERE,'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

scene=bpy.context.scene
scene.render.engine='CYCLES'
scene.cycles.device='CPU'
scene.cycles.samples=48
scene.render.resolution_x=640; scene.render.resolution_y=640; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.world=bpy.data.worlds.new('Dark blue-gray') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes=True
bg=scene.world.node_tree.nodes.get('Background')
bg.inputs['Color'].default_value=(.05,.07,.09,1)
bg.inputs['Strength'].default_value=1.0
scene.view_settings.view_transform='AgX'

def aim(ob, target): ob.rotation_euler=(Vector(target)-ob.location).to_track_quat('-Z','Y').to_euler()
def light(name, kind, location, energy, target, size=1):
    data=bpy.data.lights.new(name,kind); data.energy=energy
    if kind=='AREA': data.shape='DISK'; data.size=size
    ob=bpy.data.objects.new(name,data); bpy.context.collection.objects.link(ob); ob.location=location; aim(ob,target)
    return ob
sun=light('Sun','SUN',(-3,4,6),2.0,(0,0,0)); sun.data.angle=math.radians(12)
light('Soft fill','AREA',(3,-2,4),110,(0,0,0),5)
camdata=bpy.data.cameras.new('Camera'); cam=bpy.data.objects.new('Camera',camdata)
bpy.context.collection.objects.link(cam); scene.camera=cam
cam.location=(0,0,6); cam.rotation_euler=(0,0,0); aim(cam,(0,0,0)); camdata.type='ORTHO'; camdata.ortho_scale=2.70
# With camera local Y aligned to world +Y, nose points down in the top image.
cam.rotation_euler=(0,0,0)
scene.render.filepath=os.path.join(HERE,'top.png'); bpy.ops.render.render(write_still=True)
camdata.type='PERSP'; camdata.lens=52
cam.location=(3.25,-4.3,3.7); aim(cam,(0,0,.12))
scene.render.filepath=os.path.join(HERE,'angle.png'); bpy.ops.render.render(write_still=True)
