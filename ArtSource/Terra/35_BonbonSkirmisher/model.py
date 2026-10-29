import bpy, math, os, sys, json
from mathutils import Vector

# Run with Blender's Python; all assets are generated in this file.
def output_dir():
    if globals().get('__file__'):
        return os.path.dirname(os.path.abspath(__file__))
    if '--' in sys.argv:
        tail = sys.argv[sys.argv.index('--') + 1:]
        if tail:
            p = os.path.abspath(tail[0])
            return p if os.path.isdir(p) else os.path.dirname(p)
    return os.getcwd()
OUT = output_dir()
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for m in list(bpy.data.materials):
    bpy.data.materials.remove(m)
colors = [(0.78,0.19,0.40),(0.97,0.85,0.71),(0.30,0.78,0.66),(1,0.59,0.25),(0.39,0.17,0.12),(0.62,0.28,0.18),(0.45,0.25,0.34),(0.57,0.38,0.79),(0.90,0.44,0.65),(0.27,0.91,0.83),(1,0.82,0.39),(0.13,0.055,0.13),(0.92,0.31,0.49),(0.83,0.66,0.39),(0.51,0.26,0.58),(0.98,0.96,0.88)]
mats=[]
for i,c in enumerate(colors):
    m=bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color=(*c,1)
    m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value=(*c,1)
    bs.inputs['Roughness'].default_value=0.32 if i not in (9,10) else 0.4
    if i in (9,10):
        bs.inputs['Emission Color'].default_value=(*c,1)
        bs.inputs['Emission Strength'].default_value=1.5
    mats.append(m)
body=[]
def mesh(name, verts, faces, material, indices=None):
    data=bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces); data.update()
    ob=bpy.data.objects.new(name,data); bpy.context.collection.objects.link(ob)
    if isinstance(material, int): material=[material]
    for i in material: data.materials.append(mats[i])
    if indices:
        for p, idx in zip(data.polygons, indices): p.material_index=idx
    body.append(ob)
    return ob

def ellipsoid(name, loc, scale, material, segments=16, rings=8):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=loc)
    o=bpy.context.object; o.name=name; o.scale=scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    o.data.materials.append(mats[material]); body.append(o); return o

def polyplate(name, points, bottom, top, material, side=None):
    n=len(points); v=[(x,y,bottom) for x,y in points]+[(x,y,top) for x,y in points]
    f=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh(name,v,f,[material, side if side is not None else material],[1,0]+[1]*n)

def ring_y(name, x, y, z, outer, inner, depth, material, inset=11, n=12, direction=-1):
    # The front rim is exactly at y, with a recessed, dark bore.
    vs=[]
    for yy, rad in [(y,outer),(y,inner),(y-direction*depth,outer),(y-direction*depth*0.65,inner)]:
        for i in range(n):
            a=2*math.pi*i/n; vs.append((x+rad*math.cos(a),yy,z+rad*math.sin(a)))
    fs=[]; ids=[]
    for i in range(n):
        j=(i+1)%n
        fs.extend([(i,j,n+j,n+i),(i,2*n+i,2*n+j,j),(n+i,n+j,3*n+j,3*n+i)])
        ids.extend([0,0,1])
    fs.append(tuple(range(3*n,4*n))); ids.append(1)
    return mesh(name,vs,fs,[material,inset],ids)

# A faceted spherical candy with a pale wrapper seam on either shoulder.
ellipsoid('Candy', (0,0,0), (.57,.55,.30),0,20,10)
# Sculpt the wrapper as two folded, thick, tapered fan surfaces. Vertices trace
# radial creases across each fan, including a raised center fold.
for s in (-1,1):
    root=[(s*.43,-.17,.015),(s*.48,.0,.13),(s*.44,.19,.01)]
    edge=[(s*1.02,-.42,.02),(s*.92,-.15,.10),(s*.91,.04,.02),(s*.96,.20,.09),(s*1.04,.39,.025)]
    verts=root+edge+[(x,y,z-.12) for x,y,z in root+edge]
    faces=[(0,3,4,1),(1,4,5),(1,5,6),(1,6,7,2), (8,9,12,11),(9,13,12),(9,14,13),(9,10,15,14), (0,8,11,3),(3,11,12,4),(4,12,13,5),(5,13,14,6),(6,14,15,7),(7,15,10,2),(2,10,8,0)]
    mesh('Wrapper',verts,faces,[8,12,14],[0,1,0,0,2,2,2,2,2,0,0,0,0,0,2])
    # Crimp bands at the narrow neck; faceted layers signal twisted foil.
    polyplate('Crimp',[(s*.405,-.23),(s*.49,-.20),(s*.53,0),(s*.49,.20),(s*.405,.23)],.12,.205,1,5)
    polyplate('Crimp stripe',[(s*.438,-.17),(s*.48,-.14),(s*.505,0),(s*.48,.14),(s*.438,.17)],.205,.218,13,5)

# Raised angular cockpit and cream brow, oriented toward the muzzle.
polyplate('Cockpit base',[(-.34,-.08),(0,-.28),(.34,-.08),(.25,.22),(-.25,.22)],.25,.315,14,11)
polyplate('Cockpit',[(-.255,-.08),(0,-.22),(.255,-.08),(.19,.15),(-.19,.15)],.315,.35,11,6)
polyplate('Cockpit glint',[(-.18,.035),(.18,.035),(.155,.105),(-.155,.105)],.35,.359,9,11)
# Twin shallow swept rear shoulders make the confection read as an aircraft.
for s in (-1,1):
    polyplate('Rear shoulder',[(s*.18,.26),(s*.38,.24),(s*.48,.47),(s*.32,.50)],.08,.19,5,4)
    ring_y('Engine socket',s*.30,.53,-.02,.095,.052,.13,4,11,12,1)
# Short front weapon with a true annular lip; opening centered at gameplay muzzle.
polyplate('Gun mount',[(-.13,-.42),(.13,-.42),(.12,-.57),(-.12,-.57)],.025,.16,5,4)
ring_y('Muzzle',0,-.63,.10,.088,.046,.16,4,11,12,-1)

# Join fixed geometry only. This design has no naturally moving appendages.
bpy.ops.object.select_all(action='DESELECT')
for o in body: o.select_set(True)
bpy.context.view_layer.objects.active=body[0]
bpy.ops.object.join()
model=bpy.context.object; model.name='Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for p in model.data.polygons: p.use_smooth=False
with open(os.path.join(OUT,'parts.json'),'w',encoding='utf-8') as f: json.dump([],f,indent=2)
bpy.ops.object.select_all(action='DESELECT'); model.select_set(True)
bpy.context.view_layer.objects.active=model
bpy.ops.wm.obj_export(filepath=os.path.join(OUT,'model.obj'), export_selected_objects=True, export_materials=True, forward_axis='Y', up_axis='Z')

scene=bpy.context.scene
scene.render.engine='CYCLES'; scene.cycles.device='CPU'; scene.cycles.samples=48
scene.render.resolution_x=640; scene.render.resolution_y=640; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.world=bpy.data.worlds.new('Studio world') if not scene.world else scene.world
scene.world.use_nodes=True
bg=scene.world.node_tree.nodes.get('Background'); bg.inputs['Color'].default_value=(.05,.07,.09,1); bg.inputs['Strength'].default_value=1
scene.view_settings.view_transform='Standard'

def point_at(o, target):
    o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
def light(name, typ, loc, energy, target, size=None):
    data=bpy.data.lights.new(name,typ); data.energy=energy
    if size is not None and typ=='AREA': data.shape='DISK'; data.size=size
    ob=bpy.data.objects.new(name,data); bpy.context.collection.objects.link(ob); ob.location=loc; point_at(ob,target)
    return ob
sun=light('Sun','SUN',(-3,-4,6),2.0,(0,0,0)); sun.data.angle=math.radians(18)
light('Fill','AREA',(3,1,4),140,(0,0,0),5)
camdata=bpy.data.cameras.new('Camera'); cam=bpy.data.objects.new('Camera',camdata); bpy.context.collection.objects.link(cam); scene.camera=cam
cam.location=(0,0,5); cam.rotation_euler=(0,0,0); point_at(cam,(0,0,0)); camdata.type='ORTHO'; camdata.ortho_scale=2.4
scene.render.filepath=os.path.join(OUT,'top.png'); bpy.ops.render.render(write_still=True)
camdata.type='PERSP'; camdata.lens=50; cam.location=(2.8,-4.0,3.2); point_at(cam,(0,0,0))
scene.render.filepath=os.path.join(OUT,'angle.png'); bpy.ops.render.render(write_still=True)
