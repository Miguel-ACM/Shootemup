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
layered('Chassis',[(-1.48,-.85),(-1.67,-.72),(-1.57,-.40),(-1.72,-.28),(-1.58,.02),(-1.69,.25),(-1.53,.71),(-1.32,1.02),(1.32,1.02),(1.53,.71),(1.69,.25),(1.58,.02),(1.72,-.28),(1.57,-.40),(1.67,-.72),(1.48,-.85)], [(-1.37,-.75),(-1.55,-.64),(-1.45,-.36),(-1.59,-.25),(-1.46,.03),(-1.56,.25),(-1.42,.65),(-1.22,.91),(1.22,.91),(1.42,.65),(1.56,.25),(1.46,.03),(1.59,-.25),(1.45,-.36),(1.55,-.64),(1.37,-.75)],-.12,.25,2)
plate('Front rotor channel',[(-1.40,-.43),(-1.40,-.02),(1.40,-.02),(1.40,-.43)],.252,.03,11,.98)
plate('Rear rotor channel',[(-1.40,.18),(-1.40,.62),(1.40,.62),(1.40,.18)],.252,.03,11,.98)
plate('Raised hopper',[(-.58,-.70),(-.72,.18),(-.54,.80),(.54,.80),(.72,.18),(.58,-.70)],.26,.22,14,.88)
plate('Threshing chute',[(-.35,-.96),(-.46,-.36),(-.32,.38),(.32,.38),(.46,-.36),(.35,-.96)],.49,.09,13,.90)
plate('Chute throat',[(-.24,-.88),(-.30,-.38),(.30,-.38),(.24,-.88)],.59,.035,11,.92)
plate('Chute central feed',[(-.17,-.79),(-.21,-.43),(.21,-.43),(.17,-.79)],.63,.025,2,.94)
for yfeed in (-.72,-.61,-.50):
 plate('Feed crossbar',[(-.20,yfeed),(.20,yfeed),(.20,yfeed+.035),(-.20,yfeed+.035)],.66,.025,13,.95)
plate('Hopper crown',[(-.31,.32),(-.40,.70),(.40,.70),(.31,.32)],.49,.12,2,.9)
for s in (-1,1):
 plate('Notched outer guard',mirror([(1.47,-.78),(1.72,-.75),(1.78,-.57),(1.61,-.40),(1.75,-.28),(1.68,-.12),(1.48,-.20)],s),.27,.12,13,.95)
 plate('Harvest tooth',mirror([(.62,-.78),(.76,-1.07),(.86,-1.08),(.83,-.72)],s),.25,.09,15,.96)
 plate('Outer harvest tooth',mirror([(1.00,-.72),(1.10,-1.02),(1.21,-1.03),(1.20,-.70)],s),.25,.09,6,.96)
 plate('Shoulder armor',mirror([(.76,-.58),(1.51,-.53),(1.47,.62),(.77,.62)],s),.26,.12,2,.9)
 plate('Brass guard',mirror([(.80,-.58),(1.52,-.57),(1.52,-.46),(.80,-.46)],s),.39,.05,13,.92)
 plate('Rear vent',mirror([(.94,.49),(1.29,.49),(1.29,.76),(.94,.76)],s),.39,.05,11,.9)
 ring_y('Side muzzle',s*1.35,-.65,.35,.12,.07,.17,13,11,10,-1)
 ring_y('Engine socket',s*1.0,.90,.10,.16,.10,.18,6,11,10,1)
ring_y('Chute cannon',0,-1.10,.42,.13,.075,.19,13,11,10,-1)
for idx,y in enumerate((-.22,.40)):
 bpy.ops.mesh.primitive_cylinder_add(vertices=12,radius=.17,depth=2.65,location=(0,y,.51),rotation=(0,math.pi/2,0))
 drum=bpy.context.object;drum.name='Drum%d'%idx;drum.data.materials.append(mats[13]);pieces=[drum]
 for j in range(10):
  # Alternating short cleats progress around the shaft, revealing a helical feed.
  x=-1.17+j*.26
  for side in (0,1):
   a=(j*.58+side*math.pi+idx*.4)
   bpy.ops.mesh.primitive_cube_add(size=1,location=(x,y+.19*math.cos(a),.51+.19*math.sin(a)))
   o=bpy.context.object;o.dimensions=(.19,.09,.075);o.rotation_euler[0]=a;o.rotation_euler[2]=.28 if side==0 else -.28
   bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(mats[13 if (j+side)%2==0 else 2]);pieces.append(o)
 bpy.ops.object.select_all(action='DESELECT')
 for o in pieces:o.select_set(True)
 bpy.context.view_layer.objects.active=drum;bpy.ops.object.join();drum.name='Drum%d'%idx
 animated.append(drum);specs.append({'object':drum.name,'motion':'spin','axis':[1,0,0],'pivot':[0,y,.51],'speed':180 if idx==0 else -180})

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
