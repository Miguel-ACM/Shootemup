import bpy, math, os, sys, json
from mathutils import Vector
if '__file__' in globals():
    OUT = os.path.dirname(os.path.abspath(__file__))
elif '--' in sys.argv and sys.argv.index('--') + 1 < len(sys.argv):
    p = os.path.abspath(sys.argv[sys.argv.index('--') + 1]); OUT = p if os.path.isdir(p) else os.path.dirname(p)
else: OUT = os.getcwd()
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
colors = [( .24,.35,.40),(.78,.83,.79),(.35,.43,.30),(.95,.38,.12),(.32,.48,.55),(.54,.18,.16),(.53,.61,.62),(.34,.31,.48),(.20,.36,.40),(.16,.83,.91),(1,.77,.27),(.055,.09,.12),(.62,.25,.19),(.82,.64,.26),(.53,.61,.55),(.80,.84,.83)]
mats=[]
for i,c in enumerate(colors):
    m=bpy.data.materials.new('tile_%02d'%i); m.diffuse_color=(*c,1); m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF'); bs.inputs['Base Color'].default_value=(*c,1); bs.inputs['Roughness'].default_value=.65
    if i in (9,10): bs.inputs['Emission Color'].default_value=(*c,1); bs.inputs['Emission Strength'].default_value=1.5
    mats.append(m)
body=[]; parts={}
def mesh(name,verts,faces,mat,g):
    data=bpy.data.meshes.new(name); data.from_pydata(verts,[],faces); data.materials.append(mats[mat]); data.update()
    obj=bpy.data.objects.new(name,data); bpy.context.collection.objects.link(obj); g.append(obj); return obj
def loft(name,rings,mat,g):
    n=12; v=[]
    for y,w,z,h in rings:
        for j in range(n):
            a=2*math.pi*j/n; v.append((w*math.cos(a),y,z+h*math.sin(a)))
    f=[tuple(reversed(range(n)))]
    for i in range(len(rings)-1):
        for j in range(n): f.append((i*n+j,i*n+(j+1)%n,(i+1)*n+(j+1)%n,(i+1)*n+j))
    f.append(tuple((len(rings)-1)*n+j for j in range(n)))
    return mesh(name,v,f,mat,g)
def plate(name,outline,bottom,top,mat,g):
    n=len(outline); cx=sum(p[0] for p in outline)/n; cy=sum(p[1] for p in outline)/n
    v=[(x,y,bottom) for x,y in outline]+[(cx+(x-cx)*.82,cy+(y-cy)*.82,top) for x,y in outline]
    f=[tuple(reversed(range(n))),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh(name,v,f,mat,g)
def ball(name,loc,scale,mat,g,segments=12,rings=6):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,location=loc); o=bpy.context.object; o.name=name; o.scale=scale; o.data.materials.append(mats[mat]); g.append(o)
def cyl(name,loc,r,depth,mat,g,axis='Z',vertices=10):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=r,depth=depth,location=loc); o=bpy.context.object; o.name=name
    if axis=='Y': o.rotation_euler[0]=math.pi/2
    o.data.materials.append(mats[mat]); g.append(o)
def cone(name,base,tip,r,mat,g):
    a=Vector(base); b=Vector(tip); d=b-a
    bpy.ops.mesh.primitive_cone_add(vertices=6,radius1=r,radius2=0,depth=d.length,location=(a+b)/2); o=bpy.context.object; o.name=name; o.rotation_euler=d.to_track_quat('Z','Y').to_euler(); o.data.materials.append(mats[mat]); g.append(o)
loft('Muscular body',[(-.91,.38,.10,.15),(-.75,.58,.09,.23),(-.46,.65,.08,.30),(-.15,.60,.06,.33),(.20,.49,.04,.29),(.50,.34,.03,.22),(.76,.20,.02,.15),(.91,.13,.02,.10)],8,body)
loft('Pale belly',[(-.87,.38,-.045,.10),(-.68,.53,-.055,.10),(-.39,.52,-.07,.09)],14,body)
plate('Mouth shadow',[(-.52,-.64),(-.44,-.95),(-.25,-1.02),(.25,-1.02),(.44,-.95),(.52,-.64)],.09,.15,11,body)
plate('Skull rim',[(-.59,-.43),(-.50,-.77),(-.33,-.88),(0,-.92),(.33,-.88),(.50,-.77),(.59,-.43),(.42,-.24),(-.42,-.24)],.19,.30,13,body)
plate('Skull crown',[(-.51,-.42),(-.41,-.70),(-.25,-.80),(0,-.84),(.25,-.80),(.41,-.70),(.51,-.42),(.34,-.25),(-.34,-.25)],.29,.36,0,body)
plate('Nasal blade',[(-.10,-.35),(-.13,-.70),(0,-.81),(.13,-.70),(.10,-.35),(0,-.22)],.35,.405,6,body)
for s in (-1,1):
    ball('Eye socket',(s*.40,-.49,.305),(.16,.12,.055),11,body)
    ball('Cyan eye',(s*.40,-.50,.351),(.095,.065,.025),9,body,10,5)
    plate('Brow',[(s*.26,-.40),(s*.53,-.34),(s*.53,-.42),(s*.29,-.48)],.35,.395,13,body)
    ball('Gill cannon housing',(s*.82,-.42,.12),(.18,.29,.15),0,body)
    cyl('Barrel sleeve',(s*.82,-.56,.20),.105,.25,13,body,'Y')
    cyl('Muzzle rim',(s*.82,-.69,.20),.105,.02,6,body,'Y')
    cyl('Muzzle bore',(s*.82,-.701,.20),.073,.004,11,body,'Y')
    cyl('Muzzle ember',(s*.82,-.704,.20),.029,.004,10,body,'Y')
    g=[]; parts['Fin_L' if s<0 else 'Fin_R']=g
    plate('Pectoral fin',[(s*.46,-.18),(s*.72,-.09),(s*1.08,.29),(s*1.03,.46),(s*.67,.30),(s*.48,.20)],.02,.13,8,g)
    plate('Fin leading armor',[(s*.52,-.13),(s*.72,-.07),(s*1.05,.30),(s*.96,.30),(s*.68,.12)],.13,.18,13,g)
    plate('Fin inset',[(s*.59,.13),(s*.91,.31),(s*.99,.40),(s*.70,.27)],.13,.16,6,g)
for y,w,l,z in [(-.06,.27,.30,.37),(.20,.24,.28,.34),(.43,.19,.25,.28),(.63,.14,.20,.22),(.78,.10,.16,.16)]:
    shape=[(0,y-l*.52),(-w,y-l*.20),(-w*.86,y+l*.27),(0,y+l*.52),(w*.86,y+l*.27),(w,y-l*.20)]
    plate('Dorsal brass edge',shape,z-.065,z,13,body)
    plate('Dorsal steel crown',[(x*.79,y+(yy-y)*.79) for x,yy in shape],z-.005,z+.035,4,body)
    plate('Dorsal keel',[(-.035,y-l*.34),(0,y-l*.43),(.035,y-l*.34),(.025,y+l*.28),(0,y+l*.40),(-.025,y+l*.28)],z+.03,z+.075,6,body)
for x,y,z in [(-.18,.04,.40),(.15,.30,.37),(-.10,.53,.30)]:
    ball('Coral base',(x,y,z),(.065,.055,.035),12,body,8,4)
    cone('Coral branch',(x,y,z),(x-.025,y+.025,z+.11),.024,12,body)
    cone('Coral branch',(x+.02,y,z),(x+.055,y+.015,z+.075),.018,3,body)
for x,y,z,r in [(.25,-.03,.37,.045),(.30,.02,.35,.032),(-.19,.30,.34,.042)]:
    cyl('Barnacle shell',(x,y,z),r,.042,14,body,vertices=7); cyl('Barnacle opening',(x,y,z+.022),r*.42,.004,11,body,vertices=7)
g=[]; parts['Tail']=g
plate('Fluke',[(-.13,.82),(-.23,.94),(-.54,1.03),(-.60,1.18),(-.30,1.12),(0,1.00),(.30,1.12),(.60,1.18),(.54,1.03),(.23,.94),(.13,.82)],-.04,.10,8,g)
plate('Fluke center',[(-.12,.83),(-.17,.96),(0,1.03),(.17,.96),(.12,.83)],.10,.16,6,g)
for s in (-1,1): plate('Fluke leading edge',[(s*.13,.93),(s*.30,1.10),(s*.58,1.17),(s*.52,1.10),(s*.29,1.02)],.10,.14,13,g)
g=[]; parts['Jaw']=g
plate('Jaw frame',[(-.50,-.66),(-.48,-.91),(-.35,-1.04),(0,-1.10),(.35,-1.04),(.48,-.91),(.50,-.66),(.40,-.73),(.34,-.91),(0,-.98),(-.34,-.91),(-.40,-.73)],-.07,.12,14,g)
for s in (-1,1):
    for x,y in [(.18,-.97),(.32,-.94),(.40,-.85)]: cone('Tooth',(s*x,y,.10),(s*x*.86,y+.075,.18),.035,15,g)
def join_group(objects,name):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:
        o.select_set(True); bpy.context.view_layer.objects.active=o; bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
        for mod in list(o.modifiers): bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.context.view_layer.objects.active=objects[0]; bpy.ops.object.join(); o=bpy.context.object; o.name=name
    for p in o.data.polygons: p.use_smooth=False
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True); return o
model=join_group(body,'Model'); moving=[join_group(g,n) for n,g in parts.items()]
manifest=[{'object':'Fin_L','motion':'swing','axis':[0,-1,0],'pivot':[-.50,.02,.09],'speed':1.0,'amplitude':14},{'object':'Fin_R','motion':'swing','axis':[0,1,0],'pivot':[.50,.02,.09],'speed':1.0,'amplitude':14},{'object':'Tail','motion':'swing','axis':[0,0,1],'pivot':[0,.82,.03],'speed':1.15,'amplitude':14},{'object':'Jaw','motion':'swing','axis':[1,0,0],'pivot':[0,-.66,.02],'speed':.8,'amplitude':9}]
with open(os.path.join(OUT,'parts.json'),'w',encoding='utf-8') as f: json.dump(manifest,f,indent=2)
bpy.ops.object.select_all(action='DESELECT')
for o in [model]+moving: o.select_set(True)
bpy.context.view_layer.objects.active=model
bpy.ops.wm.obj_export(filepath=os.path.join(OUT,'model.obj'),export_selected_objects=True,export_materials=True,forward_axis='Y',up_axis='Z')
scene=bpy.context.scene; scene.render.engine='CYCLES'; scene.cycles.device='CPU'; scene.cycles.samples=48
scene.render.resolution_x=640; scene.render.resolution_y=640; scene.render.resolution_percentage=100; scene.render.image_settings.file_format='PNG'
scene.world=bpy.data.worlds.new('Deep water studio'); scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.05,.07,.09,1); scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=1
scene.view_settings.view_transform='Standard'
def point_at(o,target): o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
def light(name,kind,loc,energy,target,size=1):
    d=bpy.data.lights.new(name,kind); d.energy=energy
    if kind=='AREA': d.shape='DISK'; d.size=size
    o=bpy.data.objects.new(name,d); bpy.context.collection.objects.link(o); o.location=loc; point_at(o,target)
light('Sun','SUN',(-3,-4,7),2,(0,0,0)); light('Soft fill','AREA',(4,1,5),250,(0,0,0),5)
d=bpy.data.cameras.new('Camera'); cam=bpy.data.objects.new('Camera',d); bpy.context.collection.objects.link(cam); scene.camera=cam
cam.location=(0,0,6); cam.rotation_euler=(0,0,0); d.type='ORTHO'; d.ortho_scale=2.72
scene.render.filepath=os.path.join(OUT,'top.png'); bpy.ops.render.render(write_still=True)
d.type='PERSP'; d.lens=48; cam.location=(3,-4.5,3.3); point_at(cam,(0,0,.10))
scene.render.filepath=os.path.join(OUT,'angle.png'); bpy.ops.render.render(write_still=True)
