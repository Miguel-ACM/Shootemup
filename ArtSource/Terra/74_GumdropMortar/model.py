import bpy
import math
import os
import sys
import json
from mathutils import Vector

# Locate outputs beside this script, including when launched through Blender's -- convention.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    if not args:
        raise RuntimeError('Pass the script path after -- when __file__ is unavailable')
    candidate = os.path.abspath(args[0])
    OUT = candidate if os.path.isdir(candidate) else os.path.dirname(candidate)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for mat in list(bpy.data.materials):
    bpy.data.materials.remove(mat)

colors = [(0.78,0.19,0.40),(0.97,0.85,0.71),(0.30,0.78,0.66),
          (1.0,0.59,0.25),(0.39,0.17,0.12),(0.62,0.28,0.18),
          (0.45,0.25,0.34),(0.57,0.38,0.79),(0.90,0.44,0.65),
          (0.27,0.91,0.83),(1.0,0.82,0.39),(0.13,0.055,0.13),
          (0.92,0.31,0.49),(0.83,0.66,0.39),(0.51,0.26,0.58),
          (0.98,0.96,0.88)]
mats = []
for i, color in enumerate(colors):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1)
    bsdf.inputs['Roughness'].default_value = 0.32
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*color, 1)
        bsdf.inputs['Emission Strength'].default_value = 1.5
    mats.append(m)

body = []
def mesh(name, verts, faces, tile):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    ob = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(ob)
    ob.data.materials.append(mats[tile])
    body.append(ob)
    return ob

def rings(name, layers, tile, count=16):
    # Elliptical horizontal cross sections, each (x, y, z, rx, ry).
    verts = []
    for x, y, z, rx, ry in layers:
        for j in range(count):
            a = 2 * math.pi * j / count
            verts.append((x + rx * math.cos(a), y + ry * math.sin(a), z))
    faces = [tuple(reversed(range(count)))]
    for k in range(len(layers)-1):
        for j in range(count):
            n = (j + 1) % count
            faces.append((k*count+j, k*count+n, (k+1)*count+n, (k+1)*count+j))
    faces.append(tuple((len(layers)-1)*count+j for j in range(count)))
    return mesh(name, verts, faces, tile)

def plate(name, points, bottom, top, tile):
    if sum(points[i][0] * points[(i+1)%len(points)][1] - points[(i+1)%len(points)][0] * points[i][1] for i in range(len(points))) < 0:
        points = list(reversed(points))
    n = len(points)
    v = [(x,y,bottom) for x,y in points] + [(x,y,top) for x,y in points]
    f = [tuple(reversed(range(n))), tuple(range(n,2*n))]
    f += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh(name,v,f,tile)

def tube(name, mouth, direction, length, outer, inner, tile, count=12):
    # Open annulus with a dark recessed well; mouth is exactly at the gameplay point.
    p = Vector(mouth); d = Vector(direction).normalized()
    u = d.cross(Vector((1,0,0))).normalized(); v = d.cross(u).normalized()
    verts = []
    for center, radius in [(p-d*length,outer*.84),(p,outer),(p,inner),(p-d*length*.65,inner*.85)]:
        for j in range(count):
            a = 2*math.pi*j/count
            q = center + radius*(math.cos(a)*u + math.sin(a)*v)
            verts.append(tuple(q))
    faces = []
    for k in range(3):
        for j in range(count):
            n=(j+1)%count
            faces.append((k*count+j,k*count+n,(k+1)*count+n,(k+1)*count+j))
    ob=mesh(name,verts,faces,tile)
    # A dark inset disk closes the back, leaving the lip visibly hollow.
    center = p-d*length*.65
    disk = [tuple(center)] + [tuple(center + inner*.85*(math.cos(2*math.pi*j/count)*u + math.sin(2*math.pi*j/count)*v)) for j in range(count)]
    mesh(name+'_well', disk, [(0,1+j,1+(j+1)%count) for j in range(count)], 11)
    return ob

# Chocolate wafer chassis and pointed aircraft planform under the gumdrop.
plate('Wafer keel',[(-.34,-.55),(.34,-.55),(.49,-.22),(.49,.28),(.23,.64),(-.23,.64),(-.49,.28),(-.49,-.22)],-.15,.04,4)
rings('Cream collar',[(0,0,-.04,.52,.53),(0,0,.04,.56,.55),(0,0,.10,.52,.52)],1)
# A faceted, domed candy: broad shoulder and a smaller sugar-pink crown.
rings('Gumdrop',[(0,.025,.075,.51,.50),(0,.025,.16,.53,.50),(0,.025,.31,.47,.44),(0,.025,.43,.35,.34),(0,.025,.49,.21,.20),(0,.025,.51,.075,.07)],0)
rings('Crown',[(0,.025,.495,.20,.19),(0,.025,.52,.14,.13),(0,.025,.535,.075,.07)],8)
# A single broad cream chevron makes the forward direction legible from above.
plate('Icing chevron',[(-.25,-.325),(0,-.46),(.25,-.325),(.20,-.275),(0,-.37),(-.20,-.275)],.19,.215,15)
for s in (-1,1):
    # Swept candy-wrapper wings with raised contrasting cuffs.
    plate('Wrapper wing',[(s*.38,-.02),(s*.69,.00),(s*.78,.19),(s*.70,.37),(s*.43,.30)],-.025,.075,7)
    plate('Wing inset',[(s*.51,.08),(s*.70,.10),(s*.72,.19),(s*.67,.27),(s*.52,.24)],.075,.095,2)
    rings('Side candy pod',[(s*.57,.18,.075,.18,.18),(s*.57,.18,.13,.19,.18),(s*.57,.18,.21,.14,.14),(s*.57,.18,.245,.075,.075)],3,12)
    # Fixed cannon casing terminates at the actual muzzle; it is not an animated part.
    tube('Mortar', (s*.46,-.58,.21), (0,-.91,.42), .30,.115,.067,14)
    tube('Muzzle lip', (s*.46,-.58,.21), (0,-.91,.42), .055,.126,.073,1)
    # Engine socket openings at the registered rear socket locations.
    tube('Engine socket',(s*.29,.55,0),(0,1,0),.17,.105,.064,5)
    tube('Engine rim',(s*.29,.55,0),(0,1,0),.035,.112,.068,13)

# Join only model geometry, retaining palette material slots and faceted shading.
bpy.ops.object.select_all(action='DESELECT')
for ob in body:
    ob.select_set(True)
bpy.context.view_layer.objects.active = body[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
for poly in model.data.polygons:
    poly.use_smooth = False
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
with open(os.path.join(OUT,'parts.json'),'w',encoding='utf-8') as f:
    json.dump([],f,indent=2)

# Export before adding the cameras and lights.
bpy.ops.object.select_all(action='DESELECT')
model.select_set(True)
bpy.context.view_layer.objects.active = model
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
scene.world = bpy.data.worlds.new('Blue gray world') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes = True
bg = scene.world.node_tree.nodes.get('Background')
bg.inputs['Color'].default_value = (.05,.07,.09,1)
bg.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def point_at(ob, target):
    ob.rotation_euler = (Vector(target)-ob.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, location, energy, target, size=1):
    data=bpy.data.lights.new(name,kind)
    data.energy=energy
    if kind=='AREA': data.shape='DISK'; data.size=size
    ob=bpy.data.objects.new(name,data); bpy.context.collection.objects.link(ob)
    ob.location=location; point_at(ob,target)
    return ob

light('Sun','SUN',(-3,-4,6),2.0,(0,0,0))
light('Soft fill','AREA',(4,1,5),250,(0,0,0),5)
camdata=bpy.data.cameras.new('Camera')
cam=bpy.data.objects.new('Camera',camdata); bpy.context.collection.objects.link(cam)
scene.camera=cam
cam.location=(0,0,5)
cam.rotation_euler=(0,0,0)  # camera local -Z looks down; local +Y is screen up
camdata.type='ORTHO'; camdata.ortho_scale=1.85
scene.render.filepath=os.path.join(OUT,'top.png')
bpy.ops.render.render(write_still=True)
camdata.type='PERSP'; camdata.lens=50
cam.location=(2.6,-3.5,3.2); point_at(cam,(0,0,.12))
scene.render.filepath=os.path.join(OUT,'angle.png')
bpy.ops.render.render(write_still=True)


