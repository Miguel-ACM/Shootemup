import bpy, math, os, sys, json
from mathutils import Vector

# Blender can execute this file directly or receive its path following --.
try:
    SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    SCRIPT_DIR = os.path.dirname(os.path.abspath(args[args.index('--') + 1])) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablock in bpy.data.materials:
    bpy.data.materials.remove(datablock)

palette = [(0.24,0.35,0.40),(0.78,0.83,0.79),(0.35,0.43,0.30),(0.95,0.38,0.12),(0.32,0.48,0.55),(0.54,0.18,0.16),(0.53,0.61,0.62),(0.34,0.31,0.48),(0.20,0.36,0.40),(0.16,0.83,0.91),(1.0,0.77,0.27),(0.055,0.09,0.12),(0.62,0.25,0.19),(0.82,0.64,0.26),(0.53,0.61,0.55),(0.80,0.84,0.83)]
mats = []
for i, color in enumerate(palette):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    p = m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value = (*color, 1)
    p.inputs['Roughness'].default_value = .68
    p.inputs['Metallic'].default_value = .22 if i not in (9, 10, 11) else 0
    if i in (9, 10):
        p.inputs['Emission Color'].default_value = (*color, 1)
        p.inputs['Emission Strength'].default_value = 1.6
    mats.append(m)

parts = []
animated_parts = []
def mesh(name, verts, faces, material, indices=None):
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts, [], faces)
    me.update()
    ob = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(ob)
    for m in mats: me.materials.append(m)
    for p in me.polygons:
        p.material_index = material if indices is None else indices[p.index]
        p.use_smooth = False
    parts.append(ob)
    return ob

def plate(name, outline, bottom, top, mat, inset=.035, side=0):
    # Inset top gives each plate a real, broad chamfer and legible highlight.
    cx = sum(p[0] for p in outline)/len(outline); cy = sum(p[1] for p in outline)/len(outline)
    inner = []
    for x,y in outline:
        d = Vector((cx-x,cy-y)); d = d.normalized() * min(inset, d.length*.25) if d.length else d
        inner.append((x+d.x,y+d.y))
    n=len(outline)
    v=[(x,y,bottom) for x,y in outline]+[(x,y,top-.035) for x,y in outline]+[(x,y,top) for x,y in inner]
    f=[tuple(reversed(range(n))),tuple(range(2*n,3*n))]
    idx=[side,mat]
    for i in range(n):
        j=(i+1)%n
        f.extend([(i,j,n+j,n+i),(n+i,n+j,2*n+j,2*n+i)])
        idx.extend([side,mat])
    return mesh(name,v,f,mat,idx)

def cylinder(name, radius, depth, loc, mat, vertices=12, axis='Z'):
    rot=(math.pi/2,0,0) if axis=='Y' else (0,0,0)
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=rot)
    ob=bpy.context.object; ob.name=name; ob.data.materials.append(mats[mat]); parts.append(ob)
    return ob

def ring(name, x,y,z, outer, inner, depth, mat, axis='Y', segments=12):
    verts=[]
    # Two annuli and inner and outer walls: the opening is actual geometry.
    for along,r in [(-depth/2,outer),(-depth/2,inner),(depth/2,outer),(depth/2,inner)]:
        for i in range(segments):
            a=2*math.pi*i/segments
            verts.append((x+r*math.cos(a),y+along,z+r*math.sin(a)) if axis=='Y' else (x+r*math.cos(a),y+r*math.sin(a),z+along))
    f=[]
    for i in range(segments):
        j=(i+1)%segments
        f += [(i,j,segments+j,segments+i),(2*segments+j,2*segments+i,3*segments+i,3*segments+j), (j,i,2*segments+i,2*segments+j),(segments+i,segments+j,3*segments+j,3*segments+i)]
    return mesh(name,verts,f,mat)

def beam(name,a,b,r,mat,vertices=8):
    a,b=Vector(a),Vector(b); d=b-a
    ob=cylinder(name,r,d.length,(a+b)*.5,mat,vertices)
    ob.rotation_mode='QUATERNION'; ob.rotation_quaternion=d.to_track_quat('Z','Y')
    return ob

# Longitudinal, twelve-sided pressure vessel with fuller upper flanks.
rows=[(-2.13,.16,.13),(-1.90,.59,.31),(-1.36,.99,.49),(-.65,1.03,.56),(.18,.86,.53),(.91,.72,.43),(1.48,.57,.32),(1.95,.31,.22),(2.13,.12,.10)]
verts=[]
for y,w,h in rows:
    for k in range(12):
        a=2*math.pi*k/12
        verts.append((w*math.cos(a),y,h*math.sin(a)))
faces=[]; ids=[]
faces.append(tuple(reversed(range(12)))); ids.append(4)
for j in range(len(rows)-1):
    for k in range(12):
        faces.append(((j+1)*12+k,(j+1)*12+(k+1)%12,j*12+(k+1)%12,j*12+k))
        ids.append(4 if j<2 else (6 if j==6 else 0))
faces.append(tuple((len(rows)-1)*12+k for k in range(12))); ids.append(0)
mesh('Pressure hull',verts,faces,0,ids)

for s in (-1,1):
    def mirror(points):
        p=[(s*x,y) for x,y in points]
        return p if s==1 else list(reversed(p))
    # Swept outer armor wraps the rounded hull and establishes the boss silhouette.
    plate('Shoulder armor',mirror([(.60,.27),(1.31,-.43),(1.32,-1.11),(.84,-1.67),(.54,-1.41),(.88,-.81)]),.10,.38,6,.055,4)
    plate('Swept lateral fin',mirror([(.65,.52),(1.58,.79),(1.45,.12),(.76,-.13)]),.05,.23,4,.045,0)
    plate('Shoulder inlay',mirror([(.91,-.52),(1.22,-.62),(1.22,-1.03),(.93,-1.39),(.79,-1.32),(1.02,-.98)]),.38,.405,8,.025,4)
    start = len(parts)
    plate('Stern fin',mirror([(.24,1.56),(.66,2.10),(.39,2.30),(.11,1.93)]),.05,.19,4,.035,0)
    animated_parts.append(({'object': 'SternFin_' + ('L' if s < 0 else 'R'),
                            'motion': 'swing', 'axis': [0, s, 0],
                            'pivot': [s*.175, 1.745, .12], 'speed': 1.0,
                            'amplitude': 15}, parts[start:]))
    del parts[start:]
    # Broad dark inset beds with raised edges convey recessed launch channels.
    plate('Launch channel',mirror([(.43,-1.52),(.72,-1.49),(.83,-.50),(.58,-.42)]),.47,.505,11,.025,8)
    beam('Channel rail',(s*.43,-1.48,.53),(s*.58,-.43,.53),.035,13)
    beam('Channel rail',(s*.72,-1.46,.53),(s*.83,-.52,.53),.035,6)
    # Raised segmented vent slits: grouped as one distinctive gill assembly.
    plate('Gill recess',mirror([(.80,-.38),(1.01,-.31),(.96,.18),(.76,.10)]),.37,.405,11,.02,0)
    for y in (-.25,-.09,.07):
        beam('Gill vane',(s*.79,y,.42),(s*.98,y+.055,.42),.018,13,6)
    # Exposed pressure tube with two substantial metal collars.
    beam('Pressure tube',(s*.50,.73,.48),(s*.46,1.43,.34),.085,8,10)
    for y,z in ((.86,.45),(1.28,.37)):
        cylinder('Tube clamp',.103,.065,(s*(.50-(y-.73)*.057),y,z),13,10,'Y')
    # Paired ducted impellers, viewed from above; no exhaust or flame.
    x=s*.65; y=1.49
    cylinder('Impeller well',.35,.13,(x,y,.19),0,12)
    ring('Impeller rim',x,y,.275,.34,.265,.075,6,'Z',12)
    cylinder('Impeller dark bed',.263,.025,(x,y,.252),11,12)
    start = len(parts)
    for k in range(3):
        a=k*2*math.pi/3
        def polar(r,t): return (x+r*math.cos(a+t),y+r*math.sin(a+t))
        plate('Impeller blade',[polar(.065,0),polar(.245,.20),polar(.24,.70),polar(.10,.85)],.268,.303,4,.01,4)
    cylinder('Impeller hub',.085,.08,(x,y,.31),13,10)
    animated_parts.append(({'object': 'Impeller_' + ('L' if s < 0 else 'R'),
                            'motion': 'spin', 'axis': [0, 0, 1],
                            'pivot': [x, y, .31], 'speed': 540}, parts[start:]))
    del parts[start:]

# A sculpted multi-level bridge, its front points down the screen.
plate('Sail base',[(-.32,.94),(-.38,.25),(-.21,-.28),(0,-.44),(.21,-.28),(.38,.25),(.32,.94),(0,1.15)],.43,.68,4,.055,11)
plate('Sail crown',[(-.25,.79),(-.29,.23),(-.17,-.19),(0,-.30),(.17,-.19),(.29,.23),(.25,.79),(0,1.00)],.67,.83,8,.05,0)
plate('Bridge visor',[(-.22,-.015),(-.18,-.21),(0,-.30),(.18,-.21),(.22,-.015),(.13,.035),(-.13,.035)],.83,.855,11,.015,11)
plate('Bridge glass',[(-.15,-.055),(-.12,-.16),(0,-.22),(.12,-.16),(.15,-.055)],.855,.87,9,.012,8)
plate('Bridge brow',[(-.25,.04),(-.20,-.015),(.20,-.015),(.25,.04),(.20,.12),(-.20,.12)],.87,.91,6,.02,4)
plate('Dorsal prow keel',[(-.18,-.55),(0,-1.30),(.18,-.55),(0,-.40)],.51,.60,6,.025,0)
beam('Sail spine',(0,.19,.87),(0,.85,.87),.035,13,8)

# Each front face of the five real barrel lips is exactly at the gameplay XY point.
# The dark inner disc sits behind the mouth and the small amber bore is visible from above.
for x,y,z,r in [(0,-1.65,.39,.125),(-.65,-.6,.57,.105),(.65,-.6,.57,.105),(-1.02,.3,.34,.115),(1.02,.3,.34,.115)]:
    cylinder('Launcher housing',r*1.16,.24,(x,y+.12,z),4,12,'Y')
    ring('Muzzle lip',x,y+.035,z,r,r*.61,.07,13,'Y',12)
    cylinder('Bore shadow',r*.60,.012,(x,y+.08,z),11,12,'Y')
    cylinder('Bore glint',r*.23,.014,(x,y+.071,z),10,10,'Y')

# Join stationary geometry and moving assemblies separately. Bake transforms,
# then place each moving object's origin at its hinge or hub without moving it.
def join_meshes(objects, name, pivot=None):
    bpy.ops.object.select_all(action='DESELECT')
    for ob in objects: ob.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    bpy.ops.object.join()
    ob=bpy.context.object; ob.name=name
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    for mod in list(ob.modifiers): bpy.ops.object.modifier_apply(modifier=mod.name)
    for poly in ob.data.polygons: poly.use_smooth=False
    if pivot is not None:
        offset=Vector(pivot)
        for vertex in ob.data.vertices: vertex.co -= offset
        ob.location=offset
    return ob

model=join_meshes(parts, 'Model')
export_objects=[model]
for info, objects in animated_parts:
    export_objects.append(join_meshes(objects, info['object'], info['pivot']))

with open(os.path.join(SCRIPT_DIR, 'parts.json'), 'w', encoding='utf-8') as f:
    json.dump([info for info, objects in animated_parts], f, indent=2)
    f.write('\n')

bpy.ops.object.select_all(action='DESELECT')
for ob in export_objects: ob.select_set(True)
bpy.context.view_layer.objects.active=model
bpy.ops.wm.obj_export(filepath=os.path.join(SCRIPT_DIR,'model.obj'), export_selected_objects=True, export_materials=True, forward_axis='Y', up_axis='Z')
# Studio lighting and two model views, created only after the mesh export.
scene=bpy.context.scene
scene.render.engine='CYCLES'; scene.cycles.device='CPU'; scene.cycles.samples=48
scene.render.resolution_x=640; scene.render.resolution_y=640; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.world=bpy.data.worlds.new('Studio world') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.05,.07,.09,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=1
scene.view_settings.view_transform='AgX'
def aim(ob, target): ob.rotation_euler=(Vector(target)-ob.location).to_track_quat('-Z','Y').to_euler()
def light(name, typ, loc, energy, target, size=1):
    data=bpy.data.lights.new(name,typ); data.energy=energy
    if typ=='AREA': data.shape='DISK'; data.size=size
    ob=bpy.data.objects.new(name,data); scene.collection.objects.link(ob); ob.location=loc; aim(ob,target)
light('Sun','SUN',(-4,-6,8),2.2,(0,0,0))
light('Soft fill','AREA',(4,2,6),350,(0,0,0),5)
camdata=bpy.data.cameras.new('Camera'); cam=bpy.data.objects.new('Camera',camdata); scene.collection.objects.link(cam); scene.camera=cam
cam.location=(0,0,9); cam.rotation_euler=(0,0,0); aim(cam,(0,0,0)); camdata.type='ORTHO'; camdata.ortho_scale=5.25
scene.render.filepath=os.path.join(SCRIPT_DIR,'top.png'); bpy.ops.render.render(write_still=True)
cam.location=(6,-8,7); aim(cam,(0,0,.1)); camdata.type='PERSP'; camdata.lens=48
scene.render.filepath=os.path.join(SCRIPT_DIR,'angle.png'); bpy.ops.render.render(write_still=True)

