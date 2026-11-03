import bpy, math, os
from mathutils import Vector

OUT = os.path.dirname(os.path.abspath(__file__))
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
colors = [(0.24,0.35,0.40),(0.78,0.83,0.79),(0.35,0.43,0.30),(0.95,0.38,0.12),
          (0.32,0.48,0.55),(0.54,0.18,0.16),(0.53,0.61,0.62),(0.34,0.31,0.48),
          (0.20,0.36,0.40),(0.16,0.83,0.91),(1.0,0.77,0.27),(0.055,0.09,0.12),
          (0.62,0.25,0.19),(0.82,0.64,0.26),(0.53,0.61,0.55),(0.80,0.84,0.83)]
mats=[]
for i,c in enumerate(colors):
    m=bpy.data.materials.new('tile_%02d'%i); m.diffuse_color=(*c,1); m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF'); p.inputs['Base Color'].default_value=(*c,1); p.inputs['Roughness'].default_value=.72
    mats.append(m)
parts=[]
def mesh(name,v,f,mat):
    me=bpy.data.meshes.new(name); me.from_pydata(v,[],f); me.update()
    ob=bpy.data.objects.new(name,me); bpy.context.collection.objects.link(ob); ob.data.materials.append(mats[mat]); parts.append(ob); return ob
def plate(name,pts,z0,z1,mat):
    n=len(pts); v=[(x,y,z0) for x,y in pts]+[(x,y,z1) for x,y in pts]
    return mesh(name,v,[tuple(reversed(range(n))),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)],mat)
def beam(name,a,b,width,mat):
    a=Vector(a); b=Vector(b); d=(b-a).normalized(); side=d.cross(Vector((0,0,1))).normalized()*width/2
    if side.length < .01: side=Vector((width/2,0,0))
    up=d.cross(side).normalized()*width/2
    v=[tuple(p+s*side+t*up) for p in (a,b) for s,t in ((-1,-1),(1,-1),(1,1),(-1,1))]
    return mesh(name,v,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],mat)
def box(name,loc,scale,mat):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc); o=bpy.context.object; o.name=name; o.scale=scale; o.data.materials.append(mats[mat]); parts.append(o); return o
def tube(name,x,y,z,r,mat,front=True):
    n=12; v=[]
    for yy,rr in ((y+.15,r*.9),(y,r),(y,r*.61),(y+.08,r*.61)):
        for i in range(n):
            a=2*math.pi*i/n; v.append((x+rr*math.cos(a),yy,z+rr*math.sin(a)))
    f=[(j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i) for j in range(3) for i in range(n)]
    mesh(name,v,f,mat); mesh(name+' dark bore',v[3*n:4*n],[tuple(range(n))],11)

# Long armored keel, its forward end a thick V-shaped icebreaker prow.
plate('Deep blue hull',[(0,-1.49),(.71,-1.05),(.98,-.37),(1.02,.65),(.79,1.08),(-.79,1.08),(-1.02,.65),(-.98,-.37),(-.71,-1.05)],-.12,.23,4)
plate('Upper deck',[(0,-1.30),(.57,-.98),(.77,-.31),(.78,.66),(.60,.91),(-.60,.91),(-.78,.66),(-.77,-.31),(-.57,-.98)],.23,.31,1)
plate('Dark central keel',[(0,-1.29),(.09,-1.20),(.12,-.45),(0,-.36),(-.12,-.45),(-.09,-1.20)],.315,.34,11)
plate('Raised forecastle',[(0,-1.16),(.46,-.89),(.55,-.46),(.38,-.35),(-.38,-.35),(-.55,-.46),(-.46,-.89)],.31,.35,6)
for s in (-1,1):
    beam('Foredeck ice rib',(s*.10,-1.08,.37),(s*.39,-.49,.37),.045,15)
    beam('Foredeck seam',(s*.14,-.45,.37),(s*.14,-.24,.37),.025,8)
for s in (-1,1):
    # Thick upturned plow faces, extending aft along the side of the hull.
    pts=[(0,-1.47),(s*.78,-1.05),(s*1.14,-.55),(s*.94,-.48),(s*.60,-.88),(0,-1.24)]
    if s<0: pts.reverse()
    plate('Ivory plow wing',pts,.05,.32,15)
    beam('Plow blue cutting edge',(s*.045,-1.44,.34),(s*.78,-1.04,.34),.065,9)
    beam('Plow rising edge',(s*.78,-1.04,.34),(s*1.14,-.55,.34),.065,9)
    beam('Blade panel joint',(s*.42,-1.20,.345),(s*.34,-1.02,.345),.035,8)
    beam('Blade panel joint',(s*.78,-1.04,.345),(s*.63,-.89,.345),.035,8)
    # Long separate diagonal ice runners and their braces.
    beam('Sled runner',(s*1.29,-.75,-.10),(s*1.03,1.02,-.10),.14,8)
    beam('Runner forward brace',(s*.80,-.56,.02),(s*1.25,-.53,-.06),.10,6)
    beam('Runner aft brace',(s*.79,.69,.04),(s*1.08,.71,-.06),.10,6)
    for y,x in ((-.53,1.25),(.71,1.08)):
        box('Skid mount', (s*x,y,.035),(.20,.16,.09),8)
        for dx in (-.055,.055):
            bpy.ops.mesh.primitive_cylinder_add(vertices=8,radius=.025,depth=.015,location=(s*x+dx,y,.087))
            o=bpy.context.object; o.name='Mount rivet'; o.data.materials.append(mats[10]); parts.append(o)
    plate('Engine cowl',[(s*.35,.25),(s*.72,.25),(s*.79,1.00),(s*.40,1.00)] if s>0 else [(s*.40,1.00),(s*.79,1.00),(s*.72,.25),(s*.35,.25)],.30,.53,4)
    box('Recessed cowl panel',(s*.55,.67,.535),(.25,.49,.025),11)
    for y in (.49,.59,.69,.79,.89):
        box('Exhaust cooling fin',(s*.55,y,.557),(.23,.025,.035),6)
    tube('Aft exhaust',s*.52,1.06,.40,.14,6)
    # Firing slits are dark recesses with raised armor lips at the exact ports.
    box('Side slit',(s*.83,-.56,.335),(.24,.095,.035),11)
    beam('Slit eyebrow',(s*.71,-.50,.36),(s*.95,-.50,.36),.045,15)
    beam('Hazard rail',(s*.82,-.10,.48),(s*.82,.63,.48),.035,10)
    for y in (-.07,.25,.60): beam('Rail post',(s*.82,y,.31),(s*.82,y,.48),.025,10)
    # Alternating hazard striping on the shoulder, readable from above.
    box('Hazard stripe bed',(s*.90,.05,.345),(.13,.43,.025),11)
    for y in (-.10,.00,.10,.20):
        stripe=box('Amber hazard stripe',(s*.90,y,.363),(.13,.038,.018),10 if y in (-.10,.10) else 3)
        stripe.rotation_euler[2]=s*.35

# A narrow dark fracture separates the two icebreaking blade shoulders.
plate('Blade central cleft',[(-.045,-1.46),(.045,-1.46),(.09,-1.23),(0,-1.16),(-.09,-1.23)],.345,.36,11)
# Offset heated weather bridge, faceted windows, warm heating pipes and roof.
plate('Bridge foundation',[(-.52,-.10),(.20,-.10),(.25,.50),(-.46,.55)],.31,.47,8)
plate('Bridge walls',[(-.48,-.06),(.15,-.06),(.18,.45),(-.42,.48)],.47,.74,15)
plate('Bridge dark windshield',[(-.46,-.08),(.13,-.08),(.10,.06),(-.43,.06)],.72,.755,11)
box('Bridge port glass',(-.485,.20,.625),(.025,.37,.16),11)
box('Bridge starboard glass',(.165,.20,.625),(.025,.37,.16),11)
for x in (-.34,-.16,.02): beam('Heated windshield division',(x,-.08,.65),(x,-.08,.74),.018,10)
plate('Bridge roof',[(-.53,-.12),(.20,-.12),(.24,.49),(-.47,.53)],.74,.81,4)
plate('Dark glazed canopy',[(-.42,-.075),(.09,-.075),(.12,.29),(-.39,.32)],.815,.83,11)
for x in (-.32,-.16,0): beam('Amber heating element',(x,-.04,.84),(x,.27,.84),.018,10)
beam('Canopy cross brace',(-.40,.12,.845),(.11,.12,.845),.025,6)
beam('Roof hazard rail',(-.47,-.09,.84),(.16,-.09,.84),.034,10)
for x in (-.40,-.10,.12): beam('Roof rail post',(x,-.09,.79),(x,-.09,.84),.022,10)
box('Weather mast',(-.37,.34,.91),(.045,.045,.22),6)
beam('Weather sensor',(-.47,.34,1.02),(-.27,.34,1.02),.025,10)
tube('Bow launcher',0,-1.53,.23,.135,6)

bpy.ops.object.select_all(action='DESELECT')
for o in parts: o.select_set(True)
bpy.context.view_layer.objects.active=parts[0]; bpy.ops.object.join(); bpy.context.object.name='Model'
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
bpy.ops.wm.obj_export(filepath=os.path.join(OUT,'model.obj'),export_selected_objects=True,export_materials=True,forward_axis='Y',up_axis='Z')
scene=bpy.context.scene; scene.render.engine='CYCLES'; scene.cycles.samples=32
scene.render.resolution_x=640; scene.render.resolution_y=640; scene.render.resolution_percentage=100
scene.world.use_nodes=True; bg=scene.world.node_tree.nodes.get('Background'); bg.inputs['Color'].default_value=(.05,.07,.09,1); bg.inputs['Strength'].default_value=.8
def aim(o,t): o.rotation_euler=(Vector(t)-o.location).to_track_quat('-Z','Y').to_euler()
def light(kind,name,loc,energy,size):
    d=bpy.data.lights.new(name,kind); d.energy=energy
    if kind=='AREA': d.shape='DISK'; d.size=size
    o=bpy.data.objects.new(name,d); bpy.context.collection.objects.link(o); o.location=loc; aim(o,(0,0,0))
light('SUN','Sun',(-4,-5,8),2.1,0); light('AREA','Fill',(4,3,5),220,5)
d=bpy.data.cameras.new('Camera'); cam=bpy.data.objects.new('Camera',d); bpy.context.collection.objects.link(cam); scene.camera=cam
cam.location=(0,0,6); cam.rotation_euler=(0,0,0); d.type='ORTHO'; d.ortho_scale=3.7
scene.render.filepath=os.path.join(OUT,'top.png'); bpy.ops.render.render(write_still=True)
d.type='PERSP'; d.lens=50; cam.location=(4.2,-5.7,5.2); aim(cam,(0,0,.2))
scene.render.filepath=os.path.join(OUT,'angle.png'); bpy.ops.render.render(write_still=True)
