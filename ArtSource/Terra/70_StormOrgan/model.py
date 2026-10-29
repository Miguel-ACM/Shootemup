import bpy
import math
import os
import sys
import json
from mathutils import Vector

# Outputs live beside this script, including when run with blender --python.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
    if os.path.isfile(OUT):
        OUT = os.path.dirname(OUT)
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for data in bpy.data.materials:
    bpy.data.materials.remove(data)

colors = [(.24,.35,.40),(.78,.83,.79),(.35,.43,.30),(.95,.38,.12),
          (.32,.48,.55),(.54,.18,.16),(.53,.61,.62),(.34,.31,.48),
          (.20,.36,.40),(.16,.83,.91),(1.0,.77,.27),(.055,.09,.12),
          (.62,.25,.19),(.82,.64,.26),(.53,.61,.55),(.80,.84,.83)]
mats = []
for i, rgb in enumerate(colors):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*rgb, 1)
    m.use_nodes = True
    p = m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value = (*rgb, 1)
    p.inputs['Roughness'].default_value = .55
    p.inputs['Metallic'].default_value = .25 if i not in (9, 10) else .05
    if i in (9, 10):
        p.inputs['Emission Color'].default_value = (*rgb, 1)
        p.inputs['Emission Strength'].default_value = 1.7
    mats.append(m)

body = []
def mesh_obj(name, verts, faces, mat, face_mats=None):
    mesh = bpy.data.meshes.new(name + 'Mesh')
    mesh.from_pydata(verts, [], faces)
    mesh.materials.clear()
    for m in mats:
        mesh.materials.append(m)
    mesh.update()
    ob = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(ob)
    for p in mesh.polygons:
        p.material_index = mat if face_mats is None else face_mats[p.index]
        p.use_smooth = False
    body.append(ob)
    return ob

def layered(name, outline, inset, z0, z1, z2, top, side, bevel):
    # Nested silhouette rings create visible sloping armor, rather than flat slabs.
    n = len(outline)
    verts = [(x,y,z0) for x,y in outline] + [(x,y,z1) for x,y in outline] + [(x,y,z2) for x,y in inset]
    faces = [tuple(reversed(range(n)))]
    indices = [side]
    for j in range(n):
        k = (j+1) % n
        faces.append((j,k,n+k,n+j)); indices.append(side)
        faces.append((n+j,n+k,2*n+k,2*n+j)); indices.append(bevel)
    faces.append(tuple(range(2*n,3*n))); indices.append(top)
    return mesh_obj(name, verts, faces, top, indices)

def box(name, loc, scale, mat, bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    ob = bpy.context.object
    ob.name = name
    ob.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    ob.data.materials.append(mats[mat])
    if bevel:
        mod = ob.modifiers.new('Cut corners', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = ob
        bpy.ops.object.modifier_apply(modifier=mod.name)
    body.append(ob)
    return ob

def cylinder(name, loc, radius, depth, mat, axis='Z', vertices=8):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc)
    ob = bpy.context.object
    ob.name = name
    if axis == 'Y':
        ob.rotation_euler[0] = math.pi/2
    ob.data.materials.append(mats[mat])
    body.append(ob)
    return ob

def ring_y(name, x, y, z, ro, ri, depth, mat, inner=11, n=8):
    # Open annular lip; its front plane is exactly at y, with a recessed dark bore.
    verts = []
    for yy, rr in [(y,ro), (y,ri), (y+depth,ro), (y+depth,ri)]:
        for i in range(n):
            a = 2*math.pi*i/n + math.pi/8
            verts.append((x+rr*math.cos(a), yy, z+rr*math.sin(a)))
    faces=[]; indices=[]
    for i in range(n):
        j=(i+1)%n
        faces.extend([(i,j,n+j,n+i), (i,2*n+i,2*n+j,j), (n+i,n+j,3*n+j,3*n+i)])
        indices.extend([mat,mat,inner])
    mesh_obj(name, verts, faces, mat, indices)

# Violet main keel and two long pale armored side chambers.
layered('Keel', [(-.45,-.65),(.45,-.65),(.52,.65),(.38,.9),(-.38,.9),(-.52,.65)],
        [(-.35,-.56),(.35,-.56),(.40,.60),(.30,.78),(-.30,.78),(-.40,.60)],
        -.10,.09,.20,7,11,4)
for s in (-1,1):
    outline=[(s*.20,-.82),(s*.55,-.82),(s*.62,.74),(s*.30,.74)]
    inset=[(s*.25,-.75),(s*.50,-.75),(s*.55,.68),(s*.33,.68)]
    if s < 0:
        outline.reverse(); inset.reverse()
    layered('Armored pod', outline, inset, -.11,.13,.24,15,7,6)
    # Continuous weapon casing terminates at the fixed gameplay point.
    cylinder('Beam housing', (s*.38,-.73,.23), .105,.44,4,'Y',8)
    ring_y('Beam muzzle', s*.38,-1.02,.23,.112,.066,.11,6)
    cylinder('Muzzle bore', (s*.38,-.952,.23), .064,.012,11,'Y',8)
    cylinder('Muzzle core', (s*.38,-.960,.23), .025,.013,9,'Y',8)
    # Raised capacitor face and its octagonal shoulder.
    cylinder('Capacitor drum', (s*.38,.10,.265), .165,.085,8,vertices=8)
    cylinder('Capacitor rim', (s*.38,.10,.316), .128,.025,13,vertices=8)
    cylinder('Capacitor glass', (s*.38,.10,.334), .094,.015,9,vertices=8)
    cylinder('Capacitor center', (s*.38,.10,.346), .043,.012,11,vertices=8)
    # Open rear socket centered on the game's engine position.
    ring_y('Engine socket', s*.38,.82,0,.115,.074,-.08,7)
    cylinder('Engine recess', (s*.38,.805,0), .070,.012,11,'Y',8)
    tail = [(s*.53,.68),(s*.62,.74),(s*.57,1.04),(s*.49,.96)]
    tail_in = [(s*.54,.72),(s*.59,.76),(s*.55,.99),(s*.51,.94)]
    if s < 0:
        tail.reverse(); tail_in.reverse()
    layered('Rear prong', tail, tail_in, -.08,.10,.19,7,11,6)

# Five organ pipes, staggered in length, with contrasting end collars.
# They are fixed capacitor housings, not spinning parts.
for x, length, z in [(-.22,.57,.255),(-.11,.73,.27),(0,.88,.29),(.11,.73,.27),(.22,.57,.255)]:
    y = .31 - length/2
    cylinder('Organ pipe', (x,y,z), .043,length,4,'Y',8)
    cylinder('Pipe foot', (x,y-length/2+.025,z), .052,.055,6,'Y',8)
    cylinder('Pipe crown', (x,y+length/2-.025,z), .055,.055,13,'Y',8)

box('Rear bridge', (0,.52,.20), (1.42,.095,.12), 6, .025)
box('Bridge inset', (0,.52,.27), (.52,.045,.025), 7, .008)
# A small central lightning-shaped badge gives an instantly readable focal point.
mesh_obj('Lightning insignia', [(-.045,-.47,.222),(.012,-.47,.222),(-.012,-.39,.222),(.045,-.39,.222),(-.025,-.28,.222),(-.008,-.36,.222),(-.052,-.36,.222)], [tuple(range(7))], 13)

# All fixed geometry is one mesh, with transforms and any modifiers applied.
bpy.ops.object.select_all(action='DESELECT')
for ob in body:
    ob.select_set(True)
bpy.context.view_layer.objects.active = body[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for poly in model.data.polygons:
    poly.use_smooth = False

with open(os.path.join(OUT, 'parts.json'), 'w', encoding='utf-8') as f:
    json.dump([], f, indent=2)

bpy.ops.object.select_all(action='DESELECT')
model.select_set(True)
bpy.context.view_layer.objects.active = model
bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

# Presentation lights and cameras are added only after export.
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Dark blue gray')
scene.world.use_nodes = True
bg = scene.world.node_tree.nodes.get('Background')
bg.inputs['Color'].default_value = (.05,.07,.09,1)
bg.inputs['Strength'].default_value = 1.0

def point_at(ob, point):
    ob.rotation_euler = (Vector(point) - ob.location).to_track_quat('-Z', 'Y').to_euler()

bpy.ops.object.light_add(type='SUN', location=(-3,-4,6))
sun = bpy.context.object
sun.data.energy = 2.2
sun.data.angle = math.radians(18)
point_at(sun, (0,0,0))
bpy.ops.object.light_add(type='AREA', location=(3,1,4))
fill = bpy.context.object
fill.data.energy = 220
fill.data.shape = 'DISK'
fill.data.size = 5
point_at(fill, (0,0,0))

bpy.ops.object.camera_add(location=(0,0,6))
cam = bpy.context.object
cam.name = 'Camera'
scene.camera = cam
cam.data.type = 'ORTHO'
cam.data.ortho_scale = 2.46
# Camera up points toward +Y, keeping the nose at the bottom of the image.
cam.rotation_euler = (0,0,0)
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)

cam.location = (3.3,-4.4,3.7)
point_at(cam, (0,0,.10))
cam.data.type = 'PERSP'
cam.data.lens = 52
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

