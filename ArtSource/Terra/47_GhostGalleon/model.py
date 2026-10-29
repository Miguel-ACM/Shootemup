import bpy, math, os, sys, json
from mathutils import Vector

# Blender may supply the script path either as __file__ or after '--'.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    OUT = os.path.dirname(os.path.abspath(args[0])) if args else os.getcwd()
os.makedirs(OUT, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

palette = [(.24,.35,.40),(.78,.83,.79),(.35,.43,.30),(.95,.38,.12),(.32,.48,.55),(.54,.18,.16),(.53,.61,.62),(.34,.31,.48),(.20,.36,.40),(.16,.83,.91),(1,.77,.27),(.055,.09,.12),(.62,.25,.19),(.82,.64,.26),(.53,.61,.55),(.80,.84,.83)]
mats=[]
for i, rgb in enumerate(palette):
    m=bpy.data.materials.new('tile_%02d'%i); m.diffuse_color=(*rgb,1); m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF'); bs.inputs['Base Color'].default_value=(*rgb,1)
    bs.inputs['Roughness'].default_value=.82
    if i in (9,10):
        bs.inputs['Emission Color'].default_value=(*rgb,1); bs.inputs['Emission Strength'].default_value=1.8
    mats.append(m)
body=[]
def mesh(name, verts, faces, mat):
    me=bpy.data.meshes.new(name); me.from_pydata(verts, [], faces); me.update()
    ob=bpy.data.objects.new(name, me); bpy.context.collection.objects.link(ob)
    ob.data.materials.append(mats[mat]); body.append(ob); return ob

def prism(name, points, z0, z1, mat):
    n=len(points); verts=[(x,y,z0) for x,y in points]+[(x,y,z1) for x,y in points]
    faces=[tuple(reversed(range(n))),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh(name,verts,faces,mat)

def box(name, loc, scale, mat, bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc); ob=bpy.context.object; ob.name=name
    ob.dimensions=scale; bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod=ob.modifiers.new('Cut corners','BEVEL'); mod.width=bevel; mod.segments=1
        bpy.context.view_layer.objects.active=ob; bpy.ops.object.modifier_apply(modifier=mod.name)
    ob.data.materials.append(mats[mat]); body.append(ob); return ob

def ring(name, center, axis, radius, inner, depth, mat, n=8):
    # Open-ended annulus: the front face is exactly at center.
    c=Vector(center); a=Vector(axis).normalized(); u=a.cross(Vector((0,0,1)))
    if u.length < .01: u=a.cross(Vector((0,1,0)))
    u.normalize(); v=a.cross(u).normalized(); verts=[]
    for offset, r in [(0,radius),(0,inner),(-depth,radius),(-depth,inner)]:
        for i in range(n):
            p=c+a*offset+r*(u*math.cos(2*math.pi*i/n)+v*math.sin(2*math.pi*i/n)); verts.append(tuple(p))
    faces=[]
    for i in range(n):
        j=(i+1)%n
        faces.extend([(i,j,n+j,n+i),(i,2*n+i,2*n+j,j),(n+i,n+j,3*n+j,3*n+i),(2*n+i,3*n+i,3*n+j,2*n+j)])
    mesh(name,verts,faces,mat)
    # dark recessed bore makes the opening legible even from directly above
    p=c-a*(depth*.65); vv=[tuple(p+inner*.97*(u*math.cos(2*math.pi*i/n)+v*math.sin(2*math.pi*i/n))) for i in range(n)]
    mesh(name+' bore',vv,[tuple(range(n))],11)

def beam(name, a, b, width, mat):
    a,b=Vector(a),Vector(b); d=b-a
    ob=box(name,(a+b)/2,(width,width,d.length),mat)
    ob.rotation_euler=d.to_track_quat('Z','Y').to_euler()
    return ob

# Tapered, faceted hull: raised gunwale, inset bottom and a pointed bow.
outline=[(0,-1.30),(-.40,-.85),(-.52,-.70),(-.62,.55),(-.28,1.05),(.28,1.05),(.62,.55),(.52,-.70),(.40,-.85)]
inner=[(x*.77,y*.87+.02) for x,y in outline]; n=len(outline)
verts=[(x,y,-.17) for x,y in inner]+[(x,y,.18) for x,y in outline]+[(x*.94,y*.97,.29) for x,y in outline]
faces=[tuple(reversed(range(n))),tuple(range(2*n,3*n))]
for level in range(2):
    for i in range(n):
        j=(i+1)%n; faces.append((level*n+i,level*n+j,(level+1)*n+j,(level+1)*n+i))
mesh('Faceted hull',verts,faces,8)
prism('Inset weathered deck',[(0,-1.08),(-.33,-.58),(-.38,.64),(-.25,.86),(.25,.86),(.38,.64),(.33,-.58)],.29,.34,6)
prism('Bow keel crest',[(0,-1.28),(-.16,-.98),(0,-.87),(.16,-.98)],.25,.37,12)
# Layered quarterdeck and strong dark rails.
prism('Raised stern', [(-.36,.52),(-.26,.94),(.26,.94),(.36,.52)],.33,.47,11)
prism('Stern cap',[(-.29,.59),(-.22,.87),(.22,.87),(.29,.59)],.47,.50,12)
for s in (-1,1):
    beam('Gunwale', (s*.48,-.65,.34),(s*.55,.53,.35),.055,11)
    beam('Bow trim',(s*.48,-.65,.34),(0,-1.23,.34),.055,12)
    beam('Stern rim',(s*.55,.53,.35),(s*.27,1.0,.35),.055,12)
# Six fixed upward-readable cannon mouths, their lip centres at the gameplay coordinates.
for s in (-1,1):
    for y in (.43,.01,-.41):
        ring('Broadside muzzle',(s*.61,y,.22),(0,0,1),.095,.050,.12,12)
        # small cyan glint is set behind the dark mouth
        ring('Port collar',(s*.61,y,.223),(0,0,1),.071,.055,.025,6)
# Sockets are the factory default for hull 47: x = +/- .874, y = .93, z = 0.
for s in (-1,1):
    beam('Socket strut',(s*.32,.77,.08),(s*.874,.87,.02),.09,11)
    ring('Engine socket',(s*.874,.93,0),(0,1,0),.065,.042,.15,6)
# Mast and torn, thick, angular spectral sails, each faceted across a central ridge.
box('Mast',(0,.29,.64),(.09,.12,.78),11,.012)
beam('Cross spar',(-.86,.69,.91),(.86,.69,.91),.045,12)
for s in (-1,1):
    # Broad swept sail is concentrated aft, leaving all gun mouths visible from above.
    v=[(s*.08,.78,.92),(s*.89,.61,.82),(s*.71,.60,.76),(s*.52,.55,.79),(s*.34,.18,.82),(s*.08,.18,.91),
       (s*.37,.55,1.02)]
    ob=mesh('Tattered sail',v,[(0,1,6),(1,2,6),(2,3,6),(3,4,6),(4,5,6),(5,0,6)],15)
    sol=ob.modifiers.new('Sail thickness','SOLIDIFY'); sol.thickness=.025
    bpy.context.view_layer.objects.active=ob; bpy.ops.object.modifier_apply(modifier=sol.name)
    beam('Sail seam',(s*.10,.77,.94),(s*.71,.60,.79),.018,14)
# A bold bone skull figurehead, made from a few facets and dark eye sockets.
bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=1, location=(0,-.92,.48))
skull=bpy.context.object; skull.name='Figurehead skull'; skull.scale=(.16,.19,.105); skull.data.materials.append(mats[1]); body.append(skull)
for s in (-1,1):
    prism('Eye socket',[(s*.025,-.98),(s*.095,-.96),(s*.087,-.90),(s*.025,-.91)],.567,.573,11)
    prism('Ember eye',[(s*.043,-.954),(s*.074,-.95),(s*.071,-.93),(s*.043,-.933)],.574,.578,10)
box('Jaw',(0,-1.057,.46),(.19,.09,.07),1,.015)
# Join all static geometry, bake object transforms, and keep flat shading.
bpy.ops.object.select_all(action='DESELECT')
for ob in body: ob.select_set(True)
bpy.context.view_layer.objects.active=body[0]; bpy.ops.object.join(); model=bpy.context.object; model.name='Model'
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
for poly in model.data.polygons: poly.use_smooth=False
with open(os.path.join(OUT,'parts.json'),'w',encoding='utf-8') as f: json.dump([],f,indent=2)
bpy.ops.object.select_all(action='DESELECT'); model.select_set(True); bpy.context.view_layer.objects.active=model
bpy.ops.wm.obj_export(filepath=os.path.join(OUT,'model.obj'), export_selected_objects=True, export_materials=True, forward_axis='Y', up_axis='Z')

scene=bpy.context.scene
scene.render.engine='CYCLES'; scene.cycles.device='CPU'; scene.cycles.samples=48
scene.render.resolution_x=640; scene.render.resolution_y=640; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
world=bpy.data.worlds.new('Blue gray world') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world=world; world.use_nodes=True
world.node_tree.nodes['Background'].inputs['Color'].default_value=(.05,.07,.09,1)
world.node_tree.nodes['Background'].inputs['Strength'].default_value=1
scene.view_settings.view_transform='Standard'
def aim(ob, target): ob.rotation_euler=(Vector(target)-ob.location).to_track_quat('-Z','Y').to_euler()
def light(name, kind, loc, energy, target, size=None):
    data=bpy.data.lights.new(name,kind); data.energy=energy
    if size is not None: data.shape='DISK'; data.size=size
    ob=bpy.data.objects.new(name,data); bpy.context.collection.objects.link(ob); ob.location=loc; aim(ob,target)
light('Sun','SUN',(-3,-4,7),2.0,(0,0,0))
light('Soft fill','AREA',(3,1,5),160,(0,0,0),5)
cam_data=bpy.data.cameras.new('Camera'); cam=bpy.data.objects.new('Camera',cam_data); bpy.context.collection.objects.link(cam); scene.camera=cam
cam.location=(0,0,7); cam.rotation_euler=(0,0,0); cam_data.type='ORTHO'; cam_data.ortho_scale=2.95
scene.render.filepath=os.path.join(OUT,'top.png'); bpy.ops.render.render(write_still=True)
cam.location=(3.5,-4.8,4.5); aim(cam,(0,-.08,.30)); cam_data.type='PERSP'; cam_data.lens=50
scene.render.filepath=os.path.join(OUT,'angle.png'); bpy.ops.render.render(write_still=True)

