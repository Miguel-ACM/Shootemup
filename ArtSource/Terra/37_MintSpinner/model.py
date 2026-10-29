import bpy
import math
import json
import os
import sys
from mathutils import Vector

# Run with: blender --background --python model.py [-- output_directory]
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for block in bpy.data.materials:
    bpy.data.materials.remove(block)

colors = [(0.78,0.19,0.40),(0.97,0.85,0.71),(0.30,0.78,0.66),(1,0.59,0.25),
          (0.39,0.17,0.12),(0.62,0.28,0.18),(0.45,0.25,0.34),(0.57,0.38,0.79),
          (0.90,0.44,0.65),(0.27,0.91,0.83),(1,0.82,0.39),(0.13,0.055,0.13),
          (0.92,0.31,0.49),(0.83,0.66,0.39),(0.51,0.26,0.58),(0.98,0.96,0.88)]
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
        bsdf.inputs['Emission Strength'].default_value = 1.8
    mats.append(m)

def mesh(name, vertices, faces, material, indices=None):
    data = bpy.data.meshes.new(name + '_mesh')
    data.from_pydata(vertices, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    for m in mats:
        data.materials.append(m)
    for p in data.polygons:
        p.material_index = material if indices is None else indices[p.index]
        p.use_smooth = False
    return obj

def rings(name, levels, n, mat, face_color=None, twist=0):
    # Each level is (radius, height); faceted, closed, convex spindle forms.
    verts = []
    for j, (r,z) in enumerate(levels):
        for i in range(n):
            a = 2*math.pi*i/n + j*twist
            verts.append((r*math.cos(a), r*math.sin(a), z))
    faces = [tuple(reversed(range(n)))]
    indices = [mat]
    for j in range(len(levels)-1):
        for i in range(n):
            k = (i+1)%n
            faces.append((j*n+i,j*n+k,(j+1)*n+k,(j+1)*n+i))
            indices.append(face_color(j,i) if face_color else mat)
    faces.append(tuple((len(levels)-1)*n+i for i in range(n)))
    indices.append(mat)
    return mesh(name, verts, faces, mat, indices)

def prism(name, points, bottom, top, mat, topmat=None):
    n = len(points)
    vertices = [(x,y,bottom) for x,y in points] + [(x,y,top) for x,y in points]
    faces = [tuple(reversed(range(n))), tuple(range(n,2*n))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh(name, vertices, faces, mat, [mat, mat if topmat is None else topmat] + [mat]*n)

def tube_y(name, x, y, z, radius, inner, length, mat, interior=11, n=12):
    # Hollow annular lip, its front opening exactly at y; axis points along Y.
    verts=[]
    for yy, rr in ((y+length,radius*0.85),(y,radius),(y,inner),(y+length*0.8,inner)):
        for i in range(n):
            a=2*math.pi*i/n
            verts.append((x+rr*math.cos(a),yy,z+rr*math.sin(a)))
    faces=[]; idx=[]
    for j in range(3):
        for i in range(n):
            k=(i+1)%n
            faces.append((j*n+i,j*n+k,(j+1)*n+k,(j+1)*n+i))
            idx.append(interior if j==2 else mat)
    faces.append(tuple(3*n+i for i in reversed(range(n)))); idx.append(interior)
    return mesh(name, verts, faces, mat, idx)

def join(objects, name):
    bpy.ops.object.select_all(action='DESELECT')
    for ob in objects: ob.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = name
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    for p in obj.data.polygons: p.use_smooth = False
    return obj

body=[]
# Thick fixed candy casing, with sloped rim and a recessed spindle bearing.
body.append(rings('lower_casing',[(.46,-.15),(.59,-.11),(.63,-.025),(.61,.075),(.54,.12)],24,2,
                  lambda j,i: 2 if j in (0,1) else (15 if j==2 else 1)))
body.append(rings('bearing',[(.31,.105),(.31,.15),(.26,.17)],24,11))
# Short swept nacelles visually connect the exact rear engine sockets to the body.
for s in (-1,1):
    points=[(s*.25,.30),(s*.43,.32),(s*.46,.49),(s*.40,.55),(s*.25,.53)]
    if s < 0: points.reverse()
    body.append(prism('nacelle',points,-.105,.095,2,15))
    # Rear opening is at socket (±.34,.52,0); tube extends forward into nacelle.
    body.append(tube_y('engine_socket',s*.34,.52,0,.084,.050,-.10,2))
# Front-mounted muzzle, with dark recessed mouth centered at the gameplay point.
body.append(prism('nose', [(-.13,-.43),(.13,-.43),(.105,-.69),(-.105,-.69)],-.09,.12,2,15))
body.append(tube_y('muzzle',0,-.73,.08,.088,.052,.14,1))
model=join(body,'Model')

rotor=[]
# Six broad swept blades: tapering candy vanes give the rotating silhouette its identity.
for k in range(6):
    a=k*math.tau/6
    def turn(x,y): return (x*math.cos(a)-y*math.sin(a),x*math.sin(a)+y*math.cos(a))
    blade=[turn(*p) for p in ((.26,-.10),(.49,-.15),(.76,-.12),(.80,.055),(.67,.20),(.39,.14))]
    rotor.append(prism('vane',blade,.15,.215,2,15))
    stripe=[turn(*p) for p in ((.34,-.045),(.55,-.09),(.755,-.075),(.77,.02),(.63,.105),(.45,.07))]
    rotor.append(prism('mint_stripe',stripe,.216,.229,2))
# Twisted segments form a bold peppermint pinwheel rather than a busy decal.
rotor.append(rings('swirl',[(.24,.18),(.48,.225),(.52,.25),(.46,.31),(.25,.355)],24,15,
                   lambda j,i: 2 if ((i//2 + j) % 2 == 0) else 15, twist=.075))
rotor.append(rings('hub',[(.18,.345),(.18,.39),(.13,.425),(.065,.44)],12,8,
                   lambda j,i: 8 if j < 2 else 15))
rotor.append(rings('hub_cap',[(.055,.438),(.055,.455),(.035,.465)],12,2))
spindle=join(rotor,'Spindle')
with open(os.path.join(OUT,'parts.json'),'w',encoding='utf-8') as f:
    json.dump([{'object':'Spindle','motion':'spin','axis':[0,0,1],
                'pivot':[0,0,0.17],'speed':360}],f,indent=2)

# Export the two mesh objects together, before adding any render-only objects.
bpy.ops.object.select_all(action='DESELECT')
model.select_set(True); spindle.select_set(True)
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
scene.world=bpy.data.worlds.new('Blue gray studio')
scene.world.use_nodes=True
bg=scene.world.node_tree.nodes.get('Background')
bg.inputs['Color'].default_value=(.05,.07,.09,1)
bg.inputs['Strength'].default_value=1
scene.view_settings.view_transform='Standard'

def aim(obj, target):
    obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, location, energy, size=None):
    data=bpy.data.lights.new(name,kind)
    data.energy=energy
    if size is not None: data.shape='DISK'; data.size=size
    ob=bpy.data.objects.new(name,data); bpy.context.collection.objects.link(ob)
    ob.location=location; aim(ob,(0,0,0))
    return ob
sun=light('Sun','SUN',(-4,-5,8),2.0)
sun.data.angle=math.radians(18)
light('Soft fill','AREA',(4,1,5),150,5)
cam_data=bpy.data.cameras.new('Camera')
cam=bpy.data.objects.new('Camera',cam_data)
bpy.context.collection.objects.link(cam)
scene.camera=cam
cam.location=(0,0,5)
cam.rotation_euler=(0,0,0)  # camera local -Z down; local +Y is screen up
cam_data.type='ORTHO'
cam_data.ortho_scale=1.90
scene.render.filepath=os.path.join(OUT,'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type='PERSP'
cam_data.lens=50
cam.location=(2.6,-3.8,3.4)
aim(cam,(0,0,.12))
scene.render.filepath=os.path.join(OUT,'angle.png')
bpy.ops.render.render(write_still=True)
