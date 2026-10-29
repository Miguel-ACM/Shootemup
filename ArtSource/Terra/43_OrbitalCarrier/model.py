import bpy
import math
import json
import os
import sys
from mathutils import Vector

# Run with Blender, optionally passing the output directory after --.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for material in list(bpy.data.materials):
    bpy.data.materials.remove(material)

palette = [(0.24,0.35,0.40),(0.78,0.83,0.79),(0.35,0.43,0.30),
           (0.95,0.38,0.12),(0.32,0.48,0.55),(0.54,0.18,0.16),
           (0.105,0.15,0.20),(0.34,0.31,0.48),(0.20,0.36,0.40),
           (0.16,0.83,0.91),(1.0,0.77,0.27),(0.055,0.09,0.12),
           (0.62,0.25,0.19),(0.48,0.36,0.18),(0.53,0.61,0.55),
           (0.66,0.70,0.71)]
# The atlas maps tile_07 to blue solar cells in the Space district.
palette[7] = (0.16, 0.24, 0.40)
mats = []
for i, color in enumerate(palette):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    p = m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value = (*color, 1)
    p.inputs['Roughness'].default_value = 0.7
    if i in (9, 10):
        p.inputs['Emission Color'].default_value = (*color, 1)
        p.inputs['Emission Strength'].default_value = 1.5
    mats.append(m)

body = []
dish = []
def assign(obj, tile, group=body):
    obj.data.materials.append(mats[tile])
    for p in obj.data.polygons:
        p.use_smooth = False
    group.append(obj)
    return obj

def box(loc, size, tile, bevel=0.0, group=body):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    o = bpy.context.object
    o.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = o.modifiers.new('Crisp edges', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = o
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return assign(o, tile, group)

def mesh(name, vertices, faces, tile, group=body):
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    data.update()
    o = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(o)
    return assign(o, tile, group)

def plate(points, z0, z1, tile, inset=0.0):
    # Raised inset top provides a broad, readable bevel around an armor plate.
    cx = sum(p[0] for p in points)/len(points)
    cy = sum(p[1] for p in points)/len(points)
    top = [(cx + (x-cx)*(1-inset), cy + (y-cy)*(1-inset), z1) for x,y in points]
    v = [(x,y,z0) for x,y in points] + top
    n = len(points)
    faces = [tuple(reversed(range(n))), tuple(range(n,2*n))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh('Armor', v, faces, tile)

def ring(cx, cy, outer, inner, z0, z1, tile, n=12, group=body):
    v = []
    for z, radius in [(z0,outer),(z1,outer),(z1,inner),(z0,inner)]:
        for i in range(n):
            a = 2*math.pi*i/n + math.pi/n
            v.append((cx+radius*math.cos(a),cy+radius*math.sin(a),z))
    faces=[]
    for k in range(4):
        for i in range(n):
            j=(i+1)%n
            faces.append((k*n+i,k*n+j,((k+1)%4)*n+j,((k+1)%4)*n+i))
    return mesh('Ring',v,faces,tile,group)

def cylinder(loc, radius, depth, tile, n=12, rotation=(0,0,0), group=body):
    bpy.ops.mesh.primitive_cylinder_add(vertices=n, radius=radius, depth=depth, location=loc, rotation=rotation)
    return assign(bpy.context.object,tile,group)

def barrel_lip(y, z):
    # Open annular face is precisely centered on the gameplay muzzle point.
    n=12; ro=0.115; ri=0.065; back=y+0.16
    v=[]
    for yy,r in [(back,ro),(y,ro),(y,ri),(back,ri)]:
        for i in range(n):
            a=2*math.pi*i/n
            v.append((r*math.cos(a),yy,z+r*math.sin(a)))
    faces=[]
    for k in range(3):
        for i in range(n):
            j=(i+1)%n
            faces.append((k*n+i,k*n+j,(k+1)*n+j,(k+1)*n+i))
    mesh('Muzzle lip',v,faces,13)
    cylinder((0,y+0.055,z),ri*0.94,0.012,11,12,(math.pi/2,0,0))

# Central station ring and recessed carrier deck.
ring(0,0,0.65,0.30,-0.11,0.14,6)
ring(0,0,0.65,0.49,0.14,0.24,15)
ring(0,0,0.40,0.30,0.145,0.20,13)
cylinder((0,0,0.075),0.295,0.17,6)
cylinder((0,0,0.18),0.215,0.075,15)
cylinder((0,0,0.224),0.115,0.018,8)
cylinder((0,0,0.237),0.048,0.012,9)

# Side armor and four framed blue solar wings, a distinctive carrier silhouette.
for s in (-1,1):
    plate([(s*0.36,-0.34),(s*0.76,-0.55),(s*0.91,0.57),(s*0.50,0.92)],-0.11,0.18,15,0.13)
    plate([(s*0.48,-0.22),(s*0.72,-0.34),(s*0.81,0.48),(s*0.59,0.65)],0.18,0.23,6,0.12)
    box((s*0.88,0.26,0.03),(0.72,0.12,0.14),13,0.018)
    for y in (-0.23,0.47):
        box((s*1.08,y,0.035),(0.40,0.62,0.10),13,0.025)
        box((s*1.08,y,0.095),(0.35,0.56,0.028),7,0.012)
        # Two broad cell divisions remain visible at gameplay scale.
        for dy in (-0.095,0.095):
            box((s*1.08,y+dy,0.112),(0.33,0.012,0.008),4)
        box((s*1.08,y,0.112),(0.012,0.54,0.008),4)
    box((s*0.52,-0.43,0.235),(0.14,0.39,0.13),6,0.025)
    box((s*0.52,-0.43,0.31),(0.09,0.29,0.025),13,0.008)
    box((s*0.45,0.66,0.24),(0.17,0.26,0.08),13,0.018)

# Forward keel and a real hollow weapon, with its opening at y=-0.93.
plate([(-0.19,-0.84),(0.19,-0.84),(0.24,-0.31),(0.16,0.17),(-0.16,0.17),(-0.24,-0.31)],-0.10,0.22,6,0.12)
plate([(-0.13,-0.72),(0.13,-0.72),(0.15,-0.33),(-0.15,-0.33)],0.22,0.27,15,0.12)
barrel_lip(-0.93,0.20)

# A compact rotating communications dish on the aft mast.
box((0,0.66,0.02),(0.30,0.42,0.28),15,0.035)
cylinder((0,0.91,0.19),0.10,0.16,13,10)
# Shallow faceted bowl: solid back, sloped inner face and central feed.
n=12; cx=0; cy=0.92
v=[]
for radius,z in [(0.25,0.26),(0.25,0.30),(0.08,0.22)]:
    for i in range(n):
        a=2*math.pi*i/n
        v.append((cx+radius*math.cos(a),cy+radius*math.sin(a),z))
faces=[]
for i in range(n):
    j=(i+1)%n
    faces.extend([(i,j,n+j,n+i),(n+i,n+j,2*n+j,2*n+i)])
faces.append(tuple(reversed(range(n))))
mesh('Dish bowl',v,faces,15,dish)
ring(0,0.92,0.25,0.222,0.295,0.315,13,12,dish)
cylinder((0,0.92,0.285),0.035,0.13,6,8,group=dish)
cylinder((0,0.92,0.355),0.04,0.025,10,8,group=dish)

def join(objects, name):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    bpy.ops.object.join()
    obj=bpy.context.object
    obj.name=name
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    for face in obj.data.polygons:
        face.use_smooth=False
    return obj

model=join(body,'Model')
antenna=join(dish,'Dish')
with open(os.path.join(OUT,'parts.json'),'w',encoding='utf-8') as f:
    json.dump([{'object':'Dish','motion':'spin','axis':[0,0,1],
                'pivot':[0,0.92,0.19],'speed':45}],f,indent=2)

bpy.ops.object.select_all(action='DESELECT')
model.select_set(True)
antenna.select_set(True)
bpy.context.view_layer.objects.active=model
bpy.ops.wm.obj_export(filepath=os.path.join(OUT,'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

scene=bpy.context.scene
scene.render.engine='CYCLES'
scene.cycles.device='CPU'
scene.cycles.samples=48
scene.render.resolution_x=640
scene.render.resolution_y=640
scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
world=bpy.data.worlds.new('Dark blue-gray') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world=world
world.use_nodes=True
world.node_tree.nodes['Background'].inputs['Color'].default_value=(0.05,0.07,0.09,1)
world.node_tree.nodes['Background'].inputs['Strength'].default_value=1.0
scene.view_settings.view_transform='Standard'

def point_at(obj, target):
    obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, loc, energy, target, size=1):
    data=bpy.data.lights.new(name,kind)
    data.energy=energy
    if kind=='AREA':
        data.shape='DISK'
        data.size=size
    obj=bpy.data.objects.new(name,data)
    bpy.context.collection.objects.link(obj)
    obj.location=loc
    point_at(obj,target)
    return obj

light('Sun','SUN',(-3,4,7),2.0,(0,0,0))
light('Soft fill','AREA',(3,-4,5),180,(0,0,0),5)
camdata=bpy.data.cameras.new('Camera')
cam=bpy.data.objects.new('Camera',camdata)
bpy.context.collection.objects.link(cam)
scene.camera=cam
cam.location=(0,0.10,7)
cam.rotation_euler=(0,0,0)
camdata.type='ORTHO'
camdata.ortho_scale=3.05
scene.render.filepath=os.path.join(OUT,'top.png')
bpy.ops.render.render(write_still=True)
camdata.type='PERSP'
camdata.lens=50
cam.location=(3.8,-5.0,5.0)
point_at(cam,(0,0.08,0.05))
scene.render.filepath=os.path.join(OUT,'angle.png')
bpy.ops.render.render(write_still=True)
