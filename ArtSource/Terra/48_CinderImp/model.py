import bpy, math, os, sys, json
from mathutils import Vector

# Output location works both with `blender --python model.py` and with `-- path`.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
    if not os.path.isdir(OUT):
        OUT = os.path.dirname(OUT)
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
colors = [(0.24,0.35,0.40),(0.78,0.83,0.79),(0.35,0.43,0.30),(0.95,0.38,0.12),(0.32,0.48,0.55),(0.54,0.18,0.16),(0.53,0.61,0.62),(0.34,0.31,0.48),(0.20,0.36,0.40),(0.16,0.83,0.91),(1.0,0.77,0.27),(0.055,0.09,0.12),(0.62,0.25,0.19),(0.82,0.64,0.26),(0.53,0.61,0.55),(0.80,0.84,0.83)]
mats = []
for i, rgb in enumerate(colors):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*rgb, 1)
    m.use_nodes = True
    bs = m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value = (*rgb, 1)
    bs.inputs['Roughness'].default_value = .72
    if i in (9, 10):
        bs.inputs['Emission Color'].default_value = (*rgb, 1)
        bs.inputs['Emission Strength'].default_value = 1.8
    mats.append(m)

body, wings = [], {-1: [], 1: []}
def mesh(name, verts, faces, material, group=body):
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts, [], faces)
    me.update()
    ob = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(ob)
    ob.data.materials.append(mats[material])
    group.append(ob)
    return ob

def poly(name, points, bottom, top, material, group=body):
    # Flat-sided plate with a faceted, raised central surface.
    n = len(points)
    verts = [(x,y,bottom) for x,y in points] + [(x,y,top) for x,y in points]
    faces = [tuple(reversed(range(n))), tuple(range(n,2*n))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh(name, verts, faces, material, group)

def ellipsoid(name, pos, scale, mat, group=body, segments=12, rings=6):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=pos)
    ob = bpy.context.object
    ob.name = name
    ob.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    ob.data.materials.append(mats[mat])
    group.append(ob)
    return ob

def tube(name, x, y_end, z, radius, inner, length, mat, group=body, direction=-1):
    # Hollow eight-sided tube; y_end is the exact centre of the open mouth.
    n = 8
    verts = []
    for yy, rr in [(y_end-direction*length, radius*.86), (y_end, radius), (y_end, inner), (y_end-direction*length*.72, inner*.85)]:
        for i in range(n):
            a = 2*math.pi*i/n
            verts.append((x+rr*math.cos(a), yy, z+rr*math.sin(a)))
    faces = []
    for j in range(3):
        for i in range(n):
            k = (i+1)%n
            faces.append((j*n+i,j*n+k,(j+1)*n+k,(j+1)*n+i))
    ob = mesh(name, verts, faces, mat, group)
    ob.data.materials.append(mats[11])
    for f in ob.data.polygons:
        if f.index >= 16: f.material_index = 1
    return ob

def tapered(name, a, b, r1, r2, mat, group=body, vertices=7):
    v = Vector(b)-Vector(a)
    mid = (Vector(a)+Vector(b))/2
    bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=r1, radius2=r2, depth=v.length, location=mid)
    ob = bpy.context.object
    ob.name = name
    ob.rotation_euler = v.to_track_quat('Z','Y').to_euler()
    ob.data.materials.append(mats[mat])
    group.append(ob)
    return ob

# Raised tapered carapace, with a dark lower keel and pointed imp snout.
poly('Keel', [(0,-.95),(-.19,-.60),(-.25,-.14),(-.20,.52),(0,.83),(.20,.52),(.25,-.14),(.19,-.60)], -.12,.13,11)
poly('Carapace', [(0,-.91),(-.19,-.48),(-.23,-.08),(-.18,.48),(0,.77),(.18,.48),(.23,-.08),(.19,-.48)], .09,.22,5)
ellipsoid('Armored crown', (0,.16,.20), (.215,.49,.16), 5)
poly('Forehead bone', [(-.16,-.22),(-.13,.04),(0,.15),(.13,.04),(.16,-.22),(0,-.38)], .28,.305,1)
poly('Crown ridge', [(-.055,.08),(-.09,.36),(0,.61),(.09,.36),(.055,.08),(0,-.05)], .32,.35,12)
# A recessed amber forehead core and paired, narrow ember eyes.
ellipsoid('Core bezel', (0,.25,.352), (.11,.14,.045), 11, segments=10)
ellipsoid('Amber core', (0,.25,.388), (.064,.085,.024), 10, segments=10)
for s in (-1,1):
    poly('Eye socket', [(s*.025,-.20),(s*.15,-.14),(s*.145,-.245),(s*.055,-.29)], .309,.322,11)
    poly('Ember eye', [(s*.043,-.205),(s*.126,-.17),(s*.12,-.217),(s*.06,-.245)], .322,.329,3)
    tapered('Horn', (s*.135,.04,.27), (s*.245,-.15,.40), .066,.008,1)
    # Wings follow the original swept-back triangular silhouette; separate flap objects.
    g = wings[s]
    outline = [(s*.16,.38),(s*.98,.78),(s*.79,.29),(s*.63,-.14),(s*.48,-.055),(s*.34,.02),(s*.32,-.45),(s*.20,-.28)]
    poly('Wing membrane', outline, -.045,.055,12,g)
    # Dark scalloped inset and broad structural ribs give a readable bat wing.
    poly('Wing shadow', [(s*.25,.25),(s*.87,.67),(s*.65,.06),(s*.49,.03),(s*.34,.12),(s*.31,-.26)], .056,.068,5,g)
    tapered('Wing leading bone', (s*.20,.36,.09), (s*.97,.77,.09), .052,.018,11,g)
    tapered('Wing outer spar', (s*.28,.23,.095), (s*.63,-.14,.085), .040,.014,1,g)
    tapered('Wing inner spar', (s*.25,.16,.10), (s*.32,-.43,.07), .035,.012,1,g)
    ellipsoid('Wing joint', (s*.23,.31,.11), (.085,.105,.07), 11,g,10,5)
    # The sockets are static body-mounted hardware; their exact openings are at y=.93.
    tapered('Socket mount', (s*.82,.69,.00), (s*.874,.86,.00), .075,.07,11)
    tube('Engine socket', s*.874,.93,0,.072,.045,.15,11,direction=1)

# The weapon mouth is centred on the gameplay muzzle x=0, y=-.55.
# Its near-black hollow and substantial rust lip remain readable in a top view.
ellipsoid('Muzzle housing', (0,-.46,.225), (.13,.15,.09), 11, segments=10)
tube('Muzzle lip', 0,-.55,.25,.085,.052,.18,12)
poly('Muzzle upper band', [(-.08,-.55),(-.08,-.46),(.08,-.46),(.08,-.55)], .30,.325,13)

def join_group(objects, name):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects: obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.join()
    ob = bpy.context.object
    ob.name = name
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    for p in ob.data.polygons: p.use_smooth = False
    return ob
model = join_group(body, 'Model')
left = join_group(wings[-1], 'Wing_L')
right = join_group(wings[1], 'Wing_R')
parts = [
    {'object':'Wing_L','motion':'swing','axis':[0,-1,0],'pivot':[-.20,.31,.08],'speed':1.4,'amplitude':18},
    {'object':'Wing_R','motion':'swing','axis':[0,1,0],'pivot':[.20,.31,.08],'speed':1.4,'amplitude':18},
]
with open(os.path.join(OUT,'parts.json'),'w',encoding='utf-8') as f:
    json.dump(parts,f,indent=2)

bpy.ops.object.select_all(action='DESELECT')
for ob in (model,left,right): ob.select_set(True)
bpy.context.view_layer.objects.active = model
bpy.ops.wm.obj_export(filepath=os.path.join(OUT,'model.obj'), export_selected_objects=True, export_materials=True, forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Blue gray world')
scene.world.use_nodes = True
bg = scene.world.node_tree.nodes.get('Background')
bg.inputs['Color'].default_value = (.05,.07,.09,1)
bg.inputs['Strength'].default_value = 1
scene.view_settings.view_transform = 'Standard'

def aim(ob, target):
    ob.rotation_euler = (Vector(target)-ob.location).to_track_quat('-Z','Y').to_euler()
def light(name, kind, loc, energy, target, size=1):
    data = bpy.data.lights.new(name,kind)
    data.energy = energy
    if kind == 'AREA': data.shape = 'DISK'; data.size = size
    ob = bpy.data.objects.new(name,data)
    bpy.context.collection.objects.link(ob)
    ob.location = loc
    aim(ob,target)
    return ob
light('Sun','SUN',(-3,-4,6),2.0,(0,0,0))
light('Soft fill','AREA',(3,1,4),120,(0,0,0),5)
camdata = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera',camdata)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0,0,6)
cam.rotation_euler = (0,0,0)
aim(cam,(0,0,0))
camdata.type = 'ORTHO'
camdata.ortho_scale = 2.36
scene.render.filepath = os.path.join(OUT,'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (3.0,-4.2,3.5)
aim(cam,(0,0,.09))
camdata.type = 'PERSP'
camdata.lens = 52
scene.render.filepath = os.path.join(OUT,'angle.png')
bpy.ops.render.render(write_still=True)
