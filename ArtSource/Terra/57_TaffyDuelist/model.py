import bpy, math, os, sys, json
from mathutils import Vector

# Run with: blender --background --python model.py [-- /output/directory]
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    OUT = os.path.abspath(args[0]) if args else os.getcwd()
os.makedirs(OUT, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

colors = [(0.78,0.19,0.40),(0.97,0.85,0.71),(0.30,0.78,0.66),(1,0.59,0.25),(0.39,0.17,0.12),(0.62,0.28,0.18),(0.45,0.25,0.34),(0.57,0.38,0.79),(0.90,0.44,0.65),(0.27,0.91,0.83),(1,0.82,0.39),(0.13,0.055,0.13),(0.92,0.31,0.49),(0.83,0.66,0.39),(0.51,0.26,0.58),(0.98,0.96,0.88)]
mats = []
for i, color in enumerate(colors):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    bs = mat.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value = (*color, 1)
    bs.inputs['Roughness'].default_value = .34
    if i in (9,10):
        bs.inputs['Emission Color'].default_value = (*color,1)
        bs.inputs['Emission Strength'].default_value = 1.4
    mats.append(mat)

body = []
def mesh(name, verts, faces, material, group=body):
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts, [], faces)
    me.update()
    ob = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(ob)
    ob.data.materials.append(mats[material])
    group.append(ob)
    return ob

def layered(name, outline, levels, material, group=body):
    # Rings shrink toward the upper surface, producing a faceted candy bevel.
    cx = sum(p[0] for p in outline)/len(outline)
    cy = sum(p[1] for p in outline)/len(outline)
    n = len(outline)
    vs = [(cx+(x-cx)*scale, cy+(y-cy)*scale, z) for z,scale in levels for x,y in outline]
    fs = [tuple(reversed(range(n)))]
    for j in range(len(levels)-1):
        for i in range(n):
            a=j*n+i; b=j*n+(i+1)%n
            fs.append((a,b,b+n,a+n))
    fs.append(tuple((len(levels)-1)*n+i for i in range(n)))
    return mesh(name,vs,fs,material,group)

def disc(name, x,y, rings, material, n=12, group=body):
    vs=[]
    for z,rx,ry in rings:
        for i in range(n):
            a=2*math.pi*i/n+math.pi/12
            vs.append((x+rx*math.cos(a), y+ry*math.sin(a), z))
    fs=[tuple(reversed(range(n)))]
    for j in range(len(rings)-1):
        for i in range(n):
            a=j*n+i; b=j*n+(i+1)%n
            fs.append((a,b,b+n,a+n))
    fs.append(tuple((len(rings)-1)*n+i for i in range(n)))
    return mesh(name,vs,fs,material,group)

def tube(name, x, yfront, z, radius, length, outer, inner, direction=-1):
    # An actual annular lip and inset dark bore, oriented along Y.
    n=10; vs=[]
    for y,r in [(yfront-direction*length,radius*.88),(yfront,radius),(yfront,radius*.57),(yfront-direction*.065,radius*.57)]:
        for i in range(n):
            a=2*math.pi*i/n
            vs.append((x+r*math.cos(a),y,z+r*math.sin(a)))
    fs=[]
    for j in range(3):
        for i in range(n):
            a=j*n+i; b=j*n+(i+1)%n
            fs.append((a,b,b+n,a+n))
    ob=mesh(name,vs,fs,outer)
    ob.data.materials.append(mats[inner])
    for p in ob.data.polygons:
        if p.index >= 2*n: p.material_index=1
    cap=[(x+radius*.57*math.cos(2*math.pi*i/n),yfront-direction*.066,z+radius*.57*math.sin(2*math.pi*i/n)) for i in range(n)]
    mesh(name+'_bore',cap,[tuple(range(n))],inner)

# Main sweet: a faceted cream lozenge with a raspberry collar and domed center.
disc('lower_lozenge',0,0,[(-.13,.57,.62),(-.08,.69,.69),(.12,.69,.69),(.21,.57,.59)],1)
disc('berry_rim',0,0,[(.19,.58,.60),(.24,.57,.59),(.29,.49,.51)],0)
disc('cream_dome',0,0,[(.27,.48,.50),(.36,.42,.44),(.43,.28,.30),(.46,.12,.13)],1)
disc('center_gem',0,-.02,[(.455,.13,.14),(.49,.10,.11)],10,10)

# Two candy-wrapper wings, with raised, broad diagonal cream ribbons.
for s in (-1,1):
    def mirror(poly):
        p=[(s*x,y) for x,y in poly]
        return p if s==1 else list(reversed(p))
    wing=mirror([(.40,.58),(.61,.72),(1.30,1.04),(1.34,.88),(1.25,-.62),(1.16,-.77),(.59,-1.00),(.45,-.70)])
    layered('taffy_wing',wing,[(-.12,.94),(-.06,1),(.12,1),(.20,.91)],12)
    inner=mirror([(.52,.49),(.69,.57),(1.20,.83),(1.13,-.53),(.65,-.78),(.53,-.60)])
    layered('wing_glaze',inner,[(.199,.98),(.235,.92)],8)
    # Three broad ribbons follow the wrapper, rather than a row of buttons.
    for y in (-.48,-.03,.42):
        stripe=mirror([(.61,y-.12),(1.15,y+.10),(1.16,y+.23),(.61,y+.01)])
        layered('cream_ribbon',stripe,[(.236,1),(.251,.97)],1)
    # Muzzle mouth is centered exactly at the gameplay point.
    disc('gun_boss',s*.74,-.42,[(.16,.20,.23),(.23,.19,.22),(.29,.15,.18)],13,10)
    tube('muzzle',s*.74,-.55,.25,.115,.22,15,11)
    # Nozzle opening faces aft at the two default socket coordinates.
    tube('engine_socket',s*.874,.93,0,.115,.19,5,11,direction=1)

# A tapered duelist's blade-like nose and raised central spine.
layered('nose',[(0,-1.32),(-.28,-.44),(.28,-.44)],[(-.10,1),(.08,1),(.18,.82)],6)
layered('nose_inlay',[(0,-1.22),(-.12,-.58),(.12,-.58)],[(.18,1),(.205,.90)],15)
layered('tail_spine',[(-.16,.43),(-.23,.85),(0,1.00),(.23,.85),(.16,.43)],[(.12,1),(.24,.85),(.29,.46)],0)

# Join static pieces, bake all transforms and preserve flat shading.
bpy.ops.object.select_all(action='DESELECT')
for ob in body: ob.select_set(True)
bpy.context.view_layer.objects.active=body[0]
bpy.ops.object.join()
model=bpy.context.object
model.name='Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for p in model.data.polygons: p.use_smooth=False
parts=[]
with open(os.path.join(OUT,'parts.json'),'w',encoding='utf-8') as f:
    json.dump(parts,f,indent=2)

# Export only the mesh, before creating the render rig.
bpy.ops.object.select_all(action='DESELECT')
model.select_set(True)
bpy.context.view_layer.objects.active=model
bpy.ops.wm.obj_export(filepath=os.path.join(OUT,'model.obj'), export_selected_objects=True, export_materials=True, forward_axis='Y', up_axis='Z')

scene=bpy.context.scene
scene.render.engine='CYCLES'
scene.cycles.device='CPU'
scene.cycles.samples=48
scene.render.resolution_x=640
scene.render.resolution_y=640
scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.world=bpy.data.worlds.new('Dark blue gray') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.05,.07,.09,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=1.0
scene.view_settings.view_transform='Standard'

def point_at(ob, target):
    ob.rotation_euler=(Vector(target)-ob.location).to_track_quat('-Z','Y').to_euler()
def light(name, typ, pos, energy, target, size=1):
    data=bpy.data.lights.new(name,typ); data.energy=energy
    if typ=='AREA': data.shape='DISK'; data.size=size
    ob=bpy.data.objects.new(name,data); bpy.context.collection.objects.link(ob)
    ob.location=pos; point_at(ob,target)
light('Sun','SUN',(-4,-4,7),2.0,(0,0,0))
light('Soft fill','AREA',(4,2,5),300,(0,0,0),5)
camdata=bpy.data.cameras.new('Camera')
cam=bpy.data.objects.new('Camera',camdata); bpy.context.collection.objects.link(cam)
scene.camera=cam
camdata.type='ORTHO'; camdata.ortho_scale=3.15
cam.location=(0,0,7); cam.rotation_euler=(0,0,0)
scene.render.filepath=os.path.join(OUT,'top.png')
bpy.ops.render.render(write_still=True)
camdata.type='PERSP'; camdata.lens=50
cam.location=(3.8,-5.2,4.5); point_at(cam,(0,-.05,.06))
scene.render.filepath=os.path.join(OUT,'angle.png')
bpy.ops.render.render(write_still=True)
