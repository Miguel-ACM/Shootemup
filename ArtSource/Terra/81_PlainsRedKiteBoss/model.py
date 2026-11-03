import bpy
import math
import os
import sys
from mathutils import Vector

# Outputs are placed beside this script, including when invoked with -- <directory>.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for m in list(bpy.data.materials):
    bpy.data.materials.remove(m)

colors = [(0.24,0.35,0.40),(0.78,0.83,0.79),(0.35,0.43,0.30),
          (0.95,0.38,0.12),(0.32,0.48,0.55),(0.54,0.18,0.16),
          (0.53,0.61,0.62),(0.34,0.31,0.48),(0.20,0.36,0.40),
          (0.16,0.83,0.91),(1.0,0.77,0.27),(0.055,0.09,0.12),
          (0.62,0.25,0.19),(0.82,0.64,0.26),(0.53,0.61,0.55),
          (0.80,0.84,0.83)]
mats = []
for i, rgb in enumerate(colors):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*rgb, 1)
    m.use_nodes = True
    bs = m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value = (*rgb, 1)
    bs.inputs['Roughness'].default_value = 0.7
    if i in (9, 10):
        bs.inputs['Emission Color'].default_value = (*rgb, 1)
        bs.inputs['Emission Strength'].default_value = 1.4
    mats.append(m)

parts = []
def mesh(name, verts, faces, material):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.materials.append(mats[material])
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    parts.append(obj)
    return obj

def layered(name, lower, upper, z0, z1, material):
    # Corresponding contours make chamfered, genuinely volumetric armor.
    n = len(lower)
    verts = [(x,y,z0) for x,y in lower] + [(x,y,z1) for x,y in upper]
    faces = [tuple(reversed(range(n))), tuple(range(n,2*n))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh(name, verts, faces, material)

def plate(name, outline, z, thick, material, inset=0.88):
    cx = sum(p[0] for p in outline)/len(outline)
    cy = sum(p[1] for p in outline)/len(outline)
    top = [(cx+(x-cx)*inset,cy+(y-cy)*inset) for x,y in outline]
    return layered(name, outline, top, z, z+thick, material)

def ring_y(name, x, y, z, radius, inner, depth, material, interior=11, segments=10, direction=-1):
    # The lip is exactly on y; the tube extends into the craft.
    vs=[]
    for yy, rr in [(y,radius),(y,inner),(y-direction*depth,radius*0.92),(y-direction*depth,inner*0.85)]:
        for i in range(segments):
            a=2*math.pi*i/segments + math.pi/segments
            vs.append((x+rr*math.cos(a), yy, z+rr*math.sin(a)))
    faces=[]
    for i in range(segments):
        j=(i+1)%segments
        faces.extend([(i,j,segments+j,segments+i), (i,2*segments+i,2*segments+j,j),
                      (segments+i,segments+j,3*segments+j,3*segments+i)])
    obj=mesh(name,vs,faces,material)
    obj.data.materials.append(mats[interior])
    for i,p in enumerate(obj.data.polygons):
        if i%3==2: p.material_index=1
    # A dark recessed back gives each opening an actual visible bore.
    yy=y-direction*depth*0.88
    disc=[(x,yy,z)]+[(x+inner*.85*math.cos(2*math.pi*i/segments+math.pi/segments),yy,z+inner*.85*math.sin(2*math.pi*i/segments+math.pi/segments)) for i in range(segments)]
    mesh(name+' bore',disc,[(0,i+1,(i+1)%segments+1) for i in range(segments)],interior)

def mirror(points, side):
    p=[(side*x,y) for x,y in points]
    return p if side>0 else list(reversed(p))


animated=[];specs=[]
layered('Kite hull',[(-.38,-1.22),(-1.52,.26),(-1.28,.59),(-.52,.38),(-.34,1.04),(.34,1.04),(.52,.38),(1.28,.59),(1.52,.26),(.38,-1.22)],[(-.28,-1.05),(-1.35,.28),(-1.18,.48),(-.45,.29),(-.26,.92),(.26,.92),(.45,.29),(1.18,.48),(1.35,.28),(.28,-1.05)],-.12,.22,5)
plate('Ivory spine',[(-.22,-1.15),(0,-1.42),(.22,-1.15),(.30,.55),(0,.87),(-.30,.55)],.23,.13,15,.9)
plate('Canopy frame',[(-.19,-.62),(0,-.96),(.19,-.62),(.16,-.08),(0,.10),(-.16,-.08)],.37,.08,11,.9)
plate('Canopy',[(-.12,-.57),(0,-.79),(.12,-.57),(.10,-.14),(0,-.04),(-.10,-.14)],.455,.025,4,.9)
for s in (-1,1):
 plate('Forward spear',mirror([(.18,-.52),(.31,-1.38),(.44,-1.26),(.48,-.40)],s),.24,.09,15,.92)
 plate('Wing inset',mirror([(.43,-.35),(1.37,.25),(1.20,.42),(.49,.20)],s),.23,.06,12,.9)
 plate('Wing stripe',mirror([(.72,-.12),(1.26,.24),(1.18,.32),(.67,.02)],s),.30,.025,15,.95)
 plate('Intake frame',mirror([(.46,-.13),(.75,-.18),(.73,.25),(.47,.30)],s),.31,.10,15,.94)
 plate('Intake throat',mirror([(.51,-.10),(.70,-.13),(.68,.20),(.51,.24)],s),.415,.02,11,.97)
 plate('Intake lip',mirror([(.48,-.17),(.75,-.21),(.74,-.14),(.49,-.10)],s),.44,.035,6,.96)
 plate('Swept tail stabilizer',mirror([(.45,.59),(1.05,.76),(1.30,1.06),(.56,.88)],s),.14,.07,5,.95)
 plate('Tail edge',mirror([(.52,.69),(1.12,.89),(1.22,1.02),(.55,.80)],s),.22,.025,15,.96)
 plate('Tucked engine',mirror([(.29,.18),(.60,.23),(.64,.82),(.53,.94),(.34,.90)],s),.24,.15,0,.88)
 ring_y('Engine socket',s*.45,.90,.10,.13,.08,.15,6,11,10,1)
 plate('Side cannon',mirror([(.80,-.30),(1.05,-.30),(1.04,.10),(.82,.10)],s),.23,.12,6,.87)
 ring_y('Side muzzle',s*.93,-.32,.31,.10,.06,.16,15,11,10,-1)
 plate('Side muzzle sight',mirror([(.86,-.31),(1.0,-.31),(.99,-.20),(.87,-.20)],s),.425,.018,11,.94)
ring_y('Central muzzle',0,-1.50,.30,.105,.06,.18,6,11,10,-1)
plate('Central muzzle sight',[(-.075,-1.48),(.075,-1.48),(.07,-1.37),(-.07,-1.37)],.415,.018,11,.94)

import json
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=parts[0]
bpy.ops.object.join()
model=bpy.context.object; model.name='Model'
for o in [model]+animated:
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
 bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
 tri=o.modifiers.new('Triangulate','TRIANGULATE');bpy.ops.object.modifier_apply(modifier=tri.name)
with open(os.path.join(OUT,'parts.json'),'w') as f:json.dump(specs,f,indent=2)
bpy.ops.object.select_all(action='DESELECT')
for o in [model]+animated:o.select_set(True)
bpy.context.view_layer.objects.active=model
bpy.ops.wm.obj_export(filepath=os.path.join(OUT,'model.obj'),export_selected_objects=True,export_materials=True,forward_axis='Y',up_axis='Z')
scene=bpy.context.scene
scene.render.engine='CYCLES'
scene.cycles.device='CPU'
scene.cycles.samples=48
scene.render.resolution_x=640
scene.render.resolution_y=640
scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.world=bpy.data.worlds.new('Blue gray studio') if not scene.world else scene.world
scene.world.use_nodes=True
bg=scene.world.node_tree.nodes.get('Background')
bg.inputs['Color'].default_value=(.05,.07,.09,1)
bg.inputs['Strength'].default_value=1.0
scene.view_settings.view_transform='AgX'

def aim(obj, target):
    obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data=bpy.data.lights.new(name,kind)
    data.energy=energy
    if size is not None and kind=='AREA': data.shape='DISK'; data.size=size
    obj=bpy.data.objects.new(name,data)
    bpy.context.collection.objects.link(obj)
    obj.location=location
    aim(obj,target)
    return obj

sun=light('Upper left sun','SUN',(-4,-4,7),2.0,(0,0,0))
sun.data.angle=math.radians(18)
light('Soft fill','AREA',(4,2,5),350,(0,0,0),5)
camdata=bpy.data.cameras.new('Camera')
cam=bpy.data.objects.new('Camera',camdata)
bpy.context.collection.objects.link(cam)
scene.camera=cam
cam.location=(0,0,8)
cam.rotation_euler=(0,0,0)
camdata.type='ORTHO'
camdata.ortho_scale=4.5
scene.render.filepath=os.path.join(OUT,'top.png')
bpy.ops.render.render(write_still=True)
camdata.type='PERSP'
camdata.lens=48
cam.location=(4.8,-6.5,5.5)
aim(cam,(0,-.12,.08))
scene.render.filepath=os.path.join(OUT,'angle.png')
bpy.ops.render.render(write_still=True)
