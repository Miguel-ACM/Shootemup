import bpy, math, os, sys, json
from mathutils import Vector

# Blender background invocation: blender --background --python model.py -- <output-directory>
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
os.makedirs(OUT, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablock in list(bpy.data.materials):
    bpy.data.materials.remove(datablock)

palette = [(0.24,0.35,0.40),(0.78,0.83,0.79),(0.35,0.43,0.30),
 (0.95,0.38,0.12),(0.32,0.48,0.55),(0.54,0.18,0.16),
 (0.53,0.61,0.62),(0.34,0.31,0.48),(0.20,0.36,0.40),
 (0.16,0.83,0.91),(1.0,0.77,0.27),(0.055,0.09,0.12),
 (0.62,0.25,0.19),(0.82,0.64,0.26),(0.53,0.61,0.55),(0.80,0.84,0.83)]
mats = []
for i, color in enumerate(palette):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    bs = m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value = (*color, 1)
    bs.inputs['Roughness'].default_value = 0.76
    bs.inputs['Metallic'].default_value = 0.18 if i in (6, 12, 13) else 0.05
    if i in (9, 10):
        bs.inputs['Emission Color'].default_value = (*color, 1)
        bs.inputs['Emission Strength'].default_value = 1.6
    mats.append(m)
body = []
parts = []

def mesh(name, verts, faces, material, group=body, face_mats=None):
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts, [], faces)
    me.materials.clear()
    for mat in mats: me.materials.append(mat)
    me.update()
    ob = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(ob)
    for p in me.polygons:
        p.material_index = material if face_mats is None else face_mats[p.index]
        p.use_smooth = False
    group.append(ob)
    return ob

def plate(name, points, z0, z1, mat, group=body, inset=0.0):
    # Beveled polygon plate with a smaller raised face.
    cx = sum(p[0] for p in points)/len(points); cy = sum(p[1] for p in points)/len(points)
    n = len(points)
    verts = [(x,y,z0) for x,y in points] + [(cx+(x-cx)*(1-inset), cy+(y-cy)*(1-inset),z1) for x,y in points]
    faces = [tuple(reversed(range(n))), tuple(range(n,2*n))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh(name, verts, faces, mat, group)

def ellipsoid(name, loc, scale, material, group=body, segments=12, rings=6):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=loc)
    ob=bpy.context.object; ob.name=name
    ob.scale=scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    ob.data.materials.append(mats[material])
    group.append(ob)
    return ob

def tube(name, a, b, radius, material, group=body, sides=8, radius2=None):
    # Real open annular rim on the endpoint b; dark recessed inner wall and bottom.
    a=Vector(a); b=Vector(b); direction=(b-a).normalized()
    u=direction.cross(Vector((0,0,1)))
    if u.length < .01: u=direction.cross(Vector((0,1,0)))
    u.normalize(); v=direction.cross(u).normalized()
    inner=radius*.57; depth=min((b-a).length*.32, radius*.65)
    rings=[]
    for center, r in [(a,radius2 or radius),(b,radius),(b,inner),(b-direction*depth,inner)]:
        rings.append([tuple(center+r*(u*math.cos(2*math.pi*i/sides)+v*math.sin(2*math.pi*i/sides))) for i in range(sides)])
    verts=sum(rings,[]); faces=[]; indices=[]
    for j in range(3):
        for i in range(sides):
            faces.append((j*sides+i,j*sides+(i+1)%sides,(j+1)*sides+(i+1)%sides,(j+1)*sides+i))
            indices.append(material if j<2 else 11)
    faces.append(tuple(range(3*sides,4*sides))); indices.append(11)
    return mesh(name,verts,faces,material,group,indices)

def dome_lobe(s):
    # Faceted, inset concentric rings give each wing case a genuinely domed crown.
    outline=[(.10,-.91),(.48,-.91),(.83,-.67),(1.0,-.15),(.92,.48),(.58,.86),(.10,.92)]
    cx,cy=.48,0.0
    verts=[]
    for factor,z in [(1,.13),(.97,.26),(.78,.43),(.46,.53)]:
        verts.extend([(s*(cx+(x-cx)*factor),cy+(y-cy)*factor,z) for x,y in outline])
    n=len(outline); faces=[]; ids=[]
    faces.append(tuple(reversed(range(n)))); ids.append(11)
    for k in range(3):
        for i in range(n):
            faces.append((k*n+i,k*n+(i+1)%n,(k+1)*n+(i+1)%n,(k+1)*n+i))
            ids.append([12,13,13][k])
    faces.append(tuple(range(3*n,4*n))); ids.append(13)
    ob=mesh('Carapace',verts,faces,13,face_mats=ids)
    if s<0:
        for p in ob.data.polygons: p.flip()

# Low central chassis and armored split, rust-edged elytra.
plate('Chassis',[(-.82,-.83),(.82,-.83),(1.0,-.25),(.92,.56),(.55,.89),(-.55,.89),(-.92,.56),(-1,-.25)],-.10,.17,11,inset=.06)
for s in (-1,1):
    dome_lobe(s)
    # Large ceramic insets track the curved shells, with a visible dark border.
    plate('Shell inset',[(s*.18,-.72),(s*.45,-.72),(s*.70,-.50),(s*.82,-.12),(s*.76,.38),(s*.50,.66),(s*.18,.73)],.425,.47,12,inset=.06)
    plate('Ceramic scute',[(s*.21,-.64),(s*.44,-.63),(s*.64,-.43),(s*.73,-.10),(s*.68,.30),(s*.47,.55),(s*.21,.62)],.47,.495,1,inset=.055)
    # Broad angular shell ribs rather than fine surface noise.
    plate('Shell rib',[(s*.30,-.61),(s*.35,-.59),(s*.36,.52),(s*.30,.55)],.497,.515,13)
    plate('Shell rib',[(s*.51,-.51),(s*.56,-.46),(s*.61,.25),(s*.56,.36)],.483,.503,13)
    # Three stout, jointed digging legs; silhouette stays within original 1.62 radius.
    for j,y in enumerate((.45,-.10,-.65)):
        plate('Coxa',[(s*.84,y+.12),(s*1.10,y+.18),(s*1.18,y+.10),(s*1.04,y-.10),(s*.88,y-.10)],.02,.20,12,inset=.12)
        plate('Upper leg',[(s*1.04,y+.12),(s*1.37,y+.27),(s*1.46,y+.16),(s*1.13,y-.04)],.04,.17,6,inset=.08)
        ellipsoid('Knee',(s*1.38,y+.16,.14),(.11,.11,.10),13,segments=8,rings=4)
        plate('Digging foot',[(s*1.35,y+.18),(s*1.45,y+.20),(s*1.62,y-.12),(s*1.53,y-.20),(s*1.30,y+.05)],.01,.15,6,inset=.08)
        plate('Foot tip',[(s*1.53,y-.20),(s*1.62,y-.12),(s*1.59,y-.23)],.035,.12,13)
    # Barrel ends are exactly the specified gameplay positions, facing forward.
    tube('Scoop cannon', (s*.85,-.25,.20),(s*.85,-.45,.20),.145,13,sides=10)
    plate('Cannon brow',[(s*.69,-.42),(s*1.01,-.42),(s*.98,-.22),(s*.72,-.22)],.31,.35,1,inset=.12)
    # Default CombatEngineSockets for hull 56: (+/- .874, .93, 0).
    tube('Rear socket',(s*.874,.76,0),(s*.874,.93,0),.12,6,sides=10)

# Spine, thorax and horned head; the shell's center stays split and dark.
plate('Spinal keel rear',[(-.07,-.39),(.07,-.39),(.07,.90),(-.07,.90)],.19,.42,11,inset=.1)
plate('Spinal keel front',[(-.07,-.96),(.07,-.96),(.07,-.70),(-.07,-.70)],.19,.42,11,inset=.1)
plate('Spine crest',[(-.035,-.38),(.035,-.38),(.035,.77),(-.035,.77)],.42,.46,13)
plate('Thorax',[(-.72,-.91),(.72,-.91),(.59,-1.37),(.26,-1.49),(-.26,-1.49),(-.59,-1.37)],.08,.36,12,inset=.12)
plate('Thorax shield',[(-.56,-.99),(.56,-.99),(.45,-1.29),(.20,-1.38),(-.20,-1.38),(-.45,-1.29)],.36,.405,1,inset=.10)
plate('Thorax crest',[(-.055,-1.38),(.055,-1.38),(.13,-1.02),(-.13,-1.02)],.405,.46,13,inset=.1)
ellipsoid('Head',(0,-1.58,.19),(.43,.29,.23),11,segments=12,rings=6)
for s in (-1,1):
    plate('Eye',[(s*.17,-1.51),(s*.34,-1.56),(s*.29,-1.68),(s*.15,-1.64)],.35,.38,10,inset=.1)
plate('Horn',[(-.09,-1.72),(.09,-1.72),(.14,-1.42),(0,-1.17),(-.14,-1.42)],.37,.52,13,inset=.18)
# Center mouth at (0,-.65,.30). The short tube rises above the center keel.
tube('Central cannon',(0,-.46,.30),(0,-.65,.30),.105,6,sides=10)

# Two articulated mandibles pivot where they meet the head; neither bears a weapon.
for s, name in [(-1,'Mandible_L'),(1,'Mandible_R')]:
    components=[]
    plate('Mandible',[(s*.25,-1.62),(s*.43,-1.70),(s*.52,-1.99),(s*.41,-2.12),(s*.34,-1.91),(s*.16,-1.78)],.08,.26,13,components,inset=.12)
    plate('Mandible blade',[(s*.41,-1.75),(s*.52,-1.99),(s*.41,-2.12),(s*.38,-1.91)],.25,.29,1,components,inset=.07)
    bpy.ops.object.select_all(action='DESELECT')
    for ob in components: ob.select_set(True)
    bpy.context.view_layer.objects.active=components[0]
    bpy.ops.object.join()
    ob=components[0]; ob.name=name
    parts.append((ob, {'object':name,'motion':'swing','axis':[0,0,s], 'pivot':[s*.25,-1.62,.18], 'speed':1.0, 'amplitude':12}))

# Assemble a single static mesh; leave the two movable mandibles separate.
bpy.ops.object.select_all(action='DESELECT')
for ob in body: ob.select_set(True)
bpy.context.view_layer.objects.active=body[0]
bpy.ops.object.join()
model=body[0]; model.name='Model'
for ob in [model] + [p[0] for p in parts]:
    bpy.ops.object.select_all(action='DESELECT'); ob.select_set(True)
    bpy.context.view_layer.objects.active=ob
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    for poly in ob.data.polygons: poly.use_smooth=False

with open(os.path.join(OUT,'parts.json'),'w',encoding='utf-8') as f:
    json.dump([p[1] for p in parts],f,indent=2)

# Export only geometry, preserving objects for the game animation system.
bpy.ops.object.select_all(action='DESELECT')
for ob in [model] + [p[0] for p in parts]: ob.select_set(True)
bpy.context.view_layer.objects.active=model
bpy.ops.wm.obj_export(filepath=os.path.join(OUT,'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

scene=bpy.context.scene
scene.render.engine='CYCLES'
scene.cycles.device='CPU'
scene.cycles.samples=48
scene.render.resolution_x=640; scene.render.resolution_y=640; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.world=bpy.data.worlds.new('Dark blue-gray') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.05,.07,.09,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=1.0
scene.view_settings.view_transform='AgX'

def point_at(ob, target):
    ob.rotation_euler=(Vector(target)-ob.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, loc, target, energy, size=None):
    data=bpy.data.lights.new(name,kind); data.energy=energy
    if kind=='AREA': data.shape='DISK'; data.size=size
    ob=bpy.data.objects.new(name,data); bpy.context.collection.objects.link(ob)
    ob.location=loc; point_at(ob,target)
    return ob
light('Sun','SUN',(-4,-5,8),(0,-.5,0),2.0)
light('Soft fill','AREA',(4,1,6),(0,-.4,0),300,5)
camdata=bpy.data.cameras.new('Camera'); cam=bpy.data.objects.new('Camera',camdata)
bpy.context.collection.objects.link(cam); scene.camera=cam
cam.location=(0,-.54,8); cam.rotation_euler=(0,0,0); point_at(cam,(0,-.54,0))
# Camera's local up points to screen +Y in the top view.
camdata.type='ORTHO'; camdata.ortho_scale=3.85
scene.render.filepath=os.path.join(OUT,'top.png'); bpy.ops.render.render(write_still=True)
cam.location=(4.4,-6.5,5.2); point_at(cam,(0,-.5,.16))
camdata.type='PERSP'; camdata.lens=50
scene.render.filepath=os.path.join(OUT,'angle.png'); bpy.ops.render.render(write_still=True)
