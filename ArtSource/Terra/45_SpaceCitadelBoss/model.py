import bpy
import math
import os
import sys
import json
from mathutils import Vector

# Space Citadel: a faceted orbital station with an open reactor ring.
if '__file__' in globals():
    OUT = os.path.dirname(os.path.abspath(__file__))
else:
    args = sys.argv
    OUT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
    if os.path.isfile(OUT) or OUT.lower().endswith('.py'):
        OUT = os.path.dirname(OUT)
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
    for block in list(datablocks):
        if block.users == 0:
            datablocks.remove(block)

colors = [(0.24,0.35,0.40),(0.78,0.83,0.79),(0.35,0.43,0.30),
          (0.95,0.38,0.12),(0.32,0.48,0.55),(0.54,0.18,0.16),
          (0.105,0.15,0.20),(0.18,0.28,0.48),(0.20,0.36,0.40),
          (0.16,0.83,0.91),(1.0,0.77,0.27),(0.055,0.09,0.12),
          (0.62,0.25,0.19),(0.48,0.36,0.18),(0.53,0.61,0.55),
          (0.66,0.70,0.71)]
mats = []
for i, color in enumerate(colors):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*color, 1.0)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.7
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*color, 1.0)
        bsdf.inputs['Emission Strength'].default_value = 1.6
    mats.append(m)

body = []
dish = []

def assign(obj, mat, group=body):
    obj.data.materials.append(mats[mat])
    for p in obj.data.polygons:
        p.use_smooth = False
    group.append(obj)
    return obj

def box(pos, size, mat, bevel=0.0, group=body):
    bpy.ops.mesh.primitive_cube_add(size=1, location=pos)
    ob = bpy.context.object
    ob.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = ob.modifiers.new('Chamfer', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = ob
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return assign(ob, mat, group)

def cylinder(pos, radius, depth, mat, vertices=8, group=body):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=pos)
    return assign(bpy.context.object, mat, group)

def mesh(name, verts, faces, mat, group=body):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    ob = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(ob)
    return assign(ob, mat, group)

def beam(a, b, width, height, mat, group=body):
    mid = (Vector(a) + Vector(b)) * 0.5
    direction = Vector(b) - Vector(a)
    ob = box(mid, (width, width, direction.length), mat, 0, group)
    ob.rotation_mode = 'QUATERNION'
    ob.rotation_quaternion = direction.to_track_quat('Z', 'Y')
    return ob

def ring(ro, ri, bottom, top, mat, n=8, center=(0,0), group=body):
    verts = []
    for z, r in ((bottom,ro),(bottom,ri),(top,ro),(top,ri)):
        for i in range(n):
            a = 2*math.pi*i/n + math.pi/8
            verts.append((center[0]+r*math.cos(a), center[1]+r*math.sin(a), z))
    faces = []
    for i in range(n):
        j = (i+1)%n
        faces.extend([(i,j,2*n+j,2*n+i), (n+j,n+i,3*n+i,3*n+j),
                      (2*n+i,2*n+j,3*n+j,3*n+i), (j,i,n+i,n+j)])
    return mesh('Annulus', verts, faces, mat, group)

# Stepped octagonal ring; the opening stays legible around the suspended core.
ring(0.99, 0.72, -0.18, 0.15, 6)
ring(0.99, 0.78, 0.15, 0.23, 15)
ring(0.76, 0.70, 0.15, 0.20, 13)
for i in range(8):
    a = i * math.pi / 4 + math.pi / 8
    x, y = math.cos(a), math.sin(a)
    beam((0.48*x,0.48*y,0.02),(0.78*x,0.78*y,0.12),0.065,0.065,13)
    cylinder((0.87*x,0.87*y,0.25), 0.075, 0.045, 13, 6)

# Raised, faceted reactor, with a small luminous center and structural cage.
cylinder((0,0,-0.02), 0.48, 0.24, 6, 8)
cylinder((0,0,0.12), 0.43, 0.12, 13, 8)
cylinder((0,0,0.22), 0.32, 0.10, 15, 8)
cylinder((0,0,0.29), 0.19, 0.05, 11, 8)
cylinder((0,0,0.325), 0.115, 0.025, 9, 8)
ring(0.28, 0.235, 0.28, 0.33, 13)

# Gold side spars, ivory forward rails and braced, open bays.
for s in (-1,1):
    box((s*1.25,0.24,0.02),(1.25,0.18,0.25),13,0.035)
    box((s*1.17,-0.70,0.02),(0.18,1.58,0.24),15,0.035)
    box((s*1.78,0.78,0.02),(0.12,1.00,0.16),13,0.025)
    beam((s*0.88,-0.36,0.02),(s*1.17,-1.22,0.02),0.065,0.065,6)
    beam((s*1.17,-0.28,0.04),(s*1.78,0.24,0.04),0.07,0.07,15)
    box((s*1.17,-1.37,0.09),(0.26,0.23,0.17),6,0.035)
    # Solar arrays remain large clean color fields, with a restrained grid.
    box((s*1.42,1.20,0.05),(0.70,0.46,0.09),7,0.025)
    for x in (s*1.08,s*1.76):
        box((x,1.20,0.115),(0.035,0.47,0.035),13)
    for y in (0.98,1.42):
        box((s*1.42,y,0.115),(0.70,0.035,0.035),15)
    box((s*1.42,1.20,0.104),(0.018,0.43,0.012),6)
    box((s*1.42,1.20,0.105),(0.66,0.018,0.012),6)

# Central armored prow, preserving clearance for the externally supplied modules.
box((0,-1.13,0.02),(0.55,0.62,0.38),6,0.07)
box((0,-1.14,0.23),(0.42,0.48,0.10),15,0.04)
beam((-0.72,-0.65,0.02),(-0.30,-1.02,0.08),0.08,0.08,13)
beam((0.72,-0.65,0.02),(0.30,-1.02,0.08),0.08,0.08,13)

# Each mouth's front plane is exactly at the gameplay muzzle coordinate.
# Hollow octagonal lips face forward (-Y), with a recessed dark bore.
def muzzle(x,y,z,r):
    n = 8
    verts = []
    for yy, rr in ((y+0.15,r*0.9),(y,r),(y,r*0.59),(y+0.075,r*0.59)):
        for i in range(n):
            a = 2*math.pi*i/n
            verts.append((x+rr*math.cos(a),yy,z+rr*math.sin(a)))
    faces=[]
    for i in range(n):
        j=(i+1)%n
        faces.extend([(i,j,n+j,n+i),(n+i,n+j,2*n+j,2*n+i),(2*n+i,2*n+j,3*n+j,3*n+i)])
    mesh('Muzzle lip',verts,faces,13)
    # Recessed bore, visible from a 3/4 view.
    verts2=[(x,y+0.076,z)] + [(x+r*0.59*math.cos(2*math.pi*i/n),y+0.076,z+r*0.59*math.sin(2*math.pi*i/n)) for i in range(n)]
    mesh('Bore',verts2,[(0,i+1,(i+1)%n+1) for i in range(n)],11)
    box((x,y+0.19,z-0.11),(r*1.65,0.25,0.17),6,0.025)

for x,y,z,r in [(0,-1.48,0.30,0.13),(-0.4,-0.9,0.4,0.105),(0.4,-0.9,0.4,0.105),(0,-0.7,0.5,0.11)]:
    muzzle(x,y,z,r)

# Separate, slowly rotating rear communications dish.
box((0,1.10,0.06),(0.18,0.40,0.18),6,0.025)
cylinder((0,1.10,0.18),0.13,0.12,13,8)
# Shallow faceted bowl, with raised rim and a central feed.
n=10
verts=[(0,1.10,0.22)]
for i in range(n):
    a=2*math.pi*i/n
    verts.append((0.36*math.cos(a),1.10+0.36*math.sin(a),0.36))
mesh('Dish bowl',verts,[(0,i+1,(i+1)%n+1) for i in range(n)],15,dish)
ring(0.37,0.33,0.345,0.38,13,10,(0,1.10),dish)
cylinder((0,1.10,0.33),0.035,0.22,13,8,dish)
cylinder((0,1.10,0.45),0.055,0.04,9,8,dish)

def join(group, name):
    bpy.ops.object.select_all(action='DESELECT')
    for ob in group:
        ob.select_set(True)
    bpy.context.view_layer.objects.active = group[0]
    bpy.ops.object.join()
    ob = bpy.context.object
    ob.name = name
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    return ob

model = join(body,'Model')
dish_obj = join(dish,'Dish')
with open(os.path.join(OUT,'parts.json'),'w',encoding='utf-8') as f:
    json.dump([{'object':'Dish','motion':'spin','axis':[0,0,1], 'pivot':[0,1.10,0.18], 'speed':45}],f,indent=2)

bpy.ops.object.select_all(action='DESELECT')
model.select_set(True)
dish_obj.select_set(True)
bpy.context.view_layer.objects.active=model
bpy.ops.wm.obj_export(filepath=os.path.join(OUT,'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Studio world') if scene.world is None else scene.world
scene.world.use_nodes = True
bg = scene.world.node_tree.nodes.get('Background')
bg.inputs['Color'].default_value = (0.05,0.07,0.09,1)
bg.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def point_at(ob, target):
    ob.rotation_euler = (Vector(target)-ob.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, loc, energy, target, size=5):
    data=bpy.data.lights.new(name,kind)
    data.energy=energy
    if kind == 'AREA': data.shape='DISK'; data.size=size
    ob=bpy.data.objects.new(name,data)
    bpy.context.collection.objects.link(ob)
    ob.location=loc
    point_at(ob,target)
    return ob

light('Sun','SUN',(-4,-4,8),2.0,(0,0,0))
light('Fill','AREA',(4,1,6),350,(0,0,0),5)
cam_data=bpy.data.cameras.new('Camera')
cam=bpy.data.objects.new('Camera',cam_data)
bpy.context.collection.objects.link(cam)
scene.camera=cam
cam.location=(0,0,8)
cam.rotation_euler=(0,0,0)
cam_data.type='ORTHO'
cam_data.ortho_scale=4.35
scene.render.filepath=os.path.join(OUT,'top.png')
bpy.ops.render.render(write_still=True)
cam.location=(4.6,-6.5,5.4)
point_at(cam,(0,0,0.05))
cam_data.type='PERSP'
cam_data.lens=50
scene.render.filepath=os.path.join(OUT,'angle.png')
bpy.ops.render.render(write_still=True)

