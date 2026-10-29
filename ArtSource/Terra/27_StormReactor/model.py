import bpy, math, os, sys
from mathutils import Vector

# Blender 5.x: run in background with --python model.py -- <output directory>.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
os.makedirs(OUT, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

palette = [(0.24,0.35,0.40),(0.78,0.83,0.79),(0.35,0.43,0.30),(0.95,0.38,0.12),(0.32,0.48,0.55),(0.54,0.18,0.16),(0.53,0.61,0.62),(0.34,0.31,0.48),(0.20,0.36,0.40),(0.16,0.83,0.91),(1.0,0.77,0.27),(0.055,0.09,0.12),(0.62,0.25,0.19),(0.82,0.64,0.26),(0.53,0.61,0.55),(0.80,0.84,0.83)]
mats = []
for i, color in enumerate(palette):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    bs = m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value = (*color, 1)
    bs.inputs['Roughness'].default_value = 0.68
    bs.inputs['Metallic'].default_value = 0.18 if i not in (9,10) else 0.0
    if i in (9,10):
        bs.inputs['Emission Color'].default_value = (*color, 1)
        bs.inputs['Emission Strength'].default_value = 2.0
    mats.append(m)

parts = []
def mesh(name, verts, faces, mat):
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts, [], faces)
    me.materials.append(mats[mat])
    me.update()
    ob = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(ob)
    parts.append(ob)
    return ob

def loft(name, outline, z0, z1, inset, mat):
    # Matching lower and inset upper contours create broad, faceted bevels.
    cx = sum(p[0] for p in outline)/len(outline)
    cy = sum(p[1] for p in outline)/len(outline)
    n = len(outline)
    verts = [(x,y,z0) for x,y in outline] + [(cx+(x-cx)*(1-inset),cy+(y-cy)*(1-inset),z1) for x,y in outline]
    faces = [tuple(reversed(range(n))), tuple(range(n,2*n))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh(name, verts, faces, mat)

def box(name, loc, scale, mat, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    o = bpy.context.object
    o.name = name
    o.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    o.data.materials.append(mats[mat])
    if bevel:
        mod = o.modifiers.new('Machined edges', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = o
        bpy.ops.object.modifier_apply(modifier=mod.name)
    parts.append(o)
    return o

def cylinder(name, loc, radius, depth, mat, axis='Z', vertices=8):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, end_fill_type='NGON', location=loc)
    o = bpy.context.object
    o.name = name
    if axis == 'Y': o.rotation_euler[0] = math.pi/2
    o.data.materials.append(mats[mat])
    parts.append(o)
    return o

def ring(name, center, outer, inner, depth, mat, axis='Z', n=12):
    # An actual annular lip, with a recessed dark bottom visible through it.
    verts = []
    for d, r in [(-depth/2,outer),(depth/2,outer),(depth/2,inner),(-depth/2,inner)]:
        for i in range(n):
            a = 2*math.pi*i/n + math.pi/n
            u, v = r*math.cos(a), r*math.sin(a)
            x,y,z = center
            verts.append((x+u,y+v,z+d) if axis == 'Z' else (x+u,y+d,z+v))
    faces=[]
    for k in range(4):
        for i in range(n):
            j=(i+1)%n
            faces.append((k*n+i,k*n+j,((k+1)%4)*n+j,((k+1)%4)*n+i))
    return mesh(name, verts, faces, mat)

# Broad crossbar and tapered central spine, retaining the original 2.8 x 2.27 footprint.
loft('Transformer crossbar', [(-1.38,-.29),(-1.17,-.50),(1.17,-.50),(1.38,-.29),(1.38,.28),(1.19,.57),(-1.19,.57),(-1.38,.28)], -.16,.19,.08,11)
loft('Spine', [(-.18,-1.15),(.18,-1.15),(.35,-.91),(.42,.68),(.29,1.12),(-.29,1.12),(-.42,.68),(-.35,-.91)], -.20,.24,.12,7)
loft('Nose armor', [(-.16,-1.11),(.16,-1.11),(.29,-.90),(.29,-.65),(-.29,-.65),(-.29,-.90)], .22,.31,.12,6)
loft('Rear spine cap', [(-.25,.38),(.25,.38),(.28,.88),(.18,1.06),(-.18,1.06),(-.28,.88)], .22,.31,.13,4)

for s in (-1,1):
    # Two substantial, chamfered capacitor housings with inset laminated tops.
    outline=[(.77,-.49),(1.18,-.58),(1.40,-.37),(1.40,.28),(1.28,.50),(.77,.58)]
    loft('Capacitor housing', [(s*x,y) for x,y in (outline if s == 1 else list(reversed(outline)))], -.14,.43,.10,7)
    outline2=[(.88,-.32),(1.19,-.30),(1.27,-.20),(1.27,.19),(1.19,.32),(.88,.36)]
    loft('Capacitor crown', [(s*x,y) for x,y in (outline2 if s == 1 else list(reversed(outline2)))], .425,.53,.10,6)
    box('Capacitor dark channel',(s*1.07,.01,.535),(.18,.48,.025),11,.015)
    box('Capacitor cyan winding',(s*1.07,.01,.552),(.065,.30,.014),9,.007)
    # Open-looking steel braces bridge the recessed gap to the center.
    box('Crossbar bridge',(s*.58,.08,.21),(.38,.27,.19),4,.035)
    box('Bridge inset',(s*.57,.08,.315),(.22,.12,.025),11,.012)
    box('Forward contact',(s*.63,-.33,.27),(.20,.18,.17),11,.025)
    box('Contact copper face',(s*.63,-.35,.36),(.11,.075,.015),13,.006)
    # Rear motors: annular openings are centered exactly on gameplay sockets.
    cylinder('Motor casing',(s*.56,.72,0),.145,.37,7,'Y',10)
    ring('Rear nozzle rim',(s*.56,.93,0),.145,.09,.045,6,'Y',10)
    cylinder('Rear nozzle dark throat',(s*.56,.908,0),.088,.012,11,'Y',10)

# Layered cheeks frame a real, recessed, upward-facing beam emitter at the gameplay point.
box('Socket left cheek',(-.26,-.35,.30),(.12,.49,.20),6,.025)
box('Socket right cheek',(.26,-.35,.30),(.12,.49,.20),6,.025)
box('Socket rear cheek',(0,-.10,.30),(.43,.11,.20),4,.02)
box('Socket front cheek',(0,-.60,.30),(.43,.11,.20),4,.02)
cylinder('Beam socket shadow',(0,-.35,.382),.165,.02,11,'Z',12)
ring('Beam emitter lip',(0,-.35,.40),.145,.095,.045,6,'Z',12)
cylinder('Beam emitter recessed core',(0,-.35,.402),.078,.012,9,'Z',12)
# A small raised rear transformer plate reinforces the central axis.
loft('Rear inlet', [(-.15,.48),(.15,.48),(.18,.78),(.11,.88),(-.11,.88),(-.18,.78)], .305,.38,.13,11)
box('Rear inlet crosspiece',(0,.67,.39),(.22,.07,.035),6,.008)

# Consolidate the complete asset before exporting, excluding the render rig.
bpy.ops.object.select_all(action='DESELECT')
for ob in parts:
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for poly in model.data.polygons:
    poly.use_smooth = False
# Explicit triangulation makes the budget and export topology deterministic.
tri = model.modifiers.new('Export triangulation', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
assert len(model.data.polygons) <= 4000, 'Triangle budget exceeded'
bpy.ops.wm.obj_export(filepath=os.path.join(OUT,'model.obj'), export_selected_objects=True, export_materials=True, forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Dark blue gray world')
scene.world.use_nodes = True
bg = scene.world.node_tree.nodes.get('Background')
bg.inputs['Color'].default_value = (.05,.07,.09,1)
bg.inputs['Strength'].default_value = 1
scene.view_settings.view_transform = 'Standard'

def aim(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, typ, loc, energy, target, size=1):
    data = bpy.data.lights.new(name, typ)
    data.energy = energy
    if typ == 'AREA': data.shape = 'DISK'; data.size = size
    if typ == 'SUN': data.angle = math.radians(18)
    ob = bpy.data.objects.new(name,data)
    bpy.context.collection.objects.link(ob)
    ob.location = loc
    aim(ob,target)
    return ob
light('Upper left sun','SUN',(-4, -3, 7),2.0,(0,0,0))
light('Soft fill','AREA',(4,2,5),300,(0,0,0),5)
camdata = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera',camdata)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0,0,8)
# Camera local up is +Y in the top image.
cam.rotation_euler = (0,0,0)
camdata.type = 'ORTHO'
camdata.ortho_scale = 3.3
scene.render.filepath = os.path.join(OUT,'top.png')
bpy.ops.render.render(write_still=True)
camdata.type = 'PERSP'
camdata.lens = 48
cam.location = (4.2,-5.4,4.5)
aim(cam,(0,0,.1))
scene.render.filepath = os.path.join(OUT,'angle.png')
bpy.ops.render.render(write_still=True)
