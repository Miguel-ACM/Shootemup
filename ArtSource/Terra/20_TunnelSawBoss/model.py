import bpy
import math
import os
import sys
from mathutils import Vector

# TunnelSawBoss: 16 swept carbide teeth around a six-spoke mining flywheel.
# Hull 20 has neither weapon muzzle points nor engine sockets in the game data.
if '__file__' in globals():
    OUT = os.path.dirname(os.path.abspath(__file__))
else:
    args = sys.argv
    if '--' not in args or not args[args.index('--') + 1:]:
        raise RuntimeError('Pass the script path after -- when __file__ is unavailable')
    OUT = os.path.dirname(os.path.abspath(args[args.index('--') + 1]))

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for block in bpy.data.materials:
    bpy.data.materials.remove(block)

colors = [(.24,.35,.40),(.78,.83,.79),(.35,.43,.30),(.95,.38,.12),
          (.32,.48,.55),(.54,.18,.16),(.53,.61,.62),(.34,.31,.48),
          (.20,.36,.40),(.16,.83,.91),(1,.77,.27),(.055,.09,.12),
          (.62,.25,.19),(.82,.64,.26),(.53,.61,.55),(.80,.84,.83)]
mats = []
for i, color in enumerate(colors):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    bs = m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value = (*color, 1)
    bs.inputs['Roughness'].default_value = .72
    bs.inputs['Metallic'].default_value = .18 if i not in (9, 10) else 0
    if i in (9, 10):
        bs.inputs['Emission Color'].default_value = (*color, 1)
        bs.inputs['Emission Strength'].default_value = 1.5
    mats.append(m)

parts = []
def mesh(name, verts, faces, material, face_mats=None):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    ob = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(ob)
    for m in mats:
        data.materials.append(m)
    for p in data.polygons:
        p.material_index = material if face_mats is None else face_mats[p.index]
        p.use_smooth = False
    parts.append(ob)
    return ob

def radial_profile(name, profile, n, mat, bands=None, phase=0):
    # Profile is a closed cross section (radius, z); successive rings joined by quads.
    verts = [(r*math.cos(phase+2*math.pi*i/n), r*math.sin(phase+2*math.pi*i/n), z)
             for r,z in profile for i in range(n)]
    faces, indices = [], []
    for j in range(len(profile)):
        k = (j+1) % len(profile)
        for i in range(n):
            t = (i+1) % n
            faces.append((j*n+i,j*n+t,k*n+t,k*n+i))
            indices.append(mat if bands is None else bands[j])
    return mesh(name, verts, faces, mat, indices)

def disk(name, radius, depth, z, material, n=32):
    bpy.ops.mesh.primitive_cylinder_add(vertices=n, radius=radius, depth=depth, location=(0,0,z))
    ob = bpy.context.object
    ob.name = name
    ob.data.materials.append(mats[material])
    parts.append(ob)
    return ob

def plate(name, points, bottom, top, material, side=11, inset=.06):
    # Beveled polygon with a smaller raised top; each spoke is a solid casting.
    cx = sum(p[0] for p in points)/len(points)
    cy = sum(p[1] for p in points)/len(points)
    n = len(points)
    inner = [(x+(cx-x)*inset, y+(cy-y)*inset) for x,y in points]
    verts = [(x,y,bottom) for x,y in points] + [(x,y,top-.055) for x,y in points] + [(x,y,top) for x,y in inner]
    faces = [tuple(reversed(range(n)))]
    idx = [side]
    for i in range(n):
        j=(i+1)%n
        faces += [(i,j,n+j,n+i),(n+i,n+j,2*n+j,2*n+i)]
        idx += [side, material]
    faces.append(tuple(range(2*n,3*n)))
    idx.append(material)
    return mesh(name,verts,faces,material,idx)

def rotate_points(points, a):
    c,s=math.cos(a),math.sin(a)
    return [(x*c-y*s,x*s+y*c) for x,y in points]

# A dark, faceted flywheel is visible in the gaps between the six spokes.
disk('Flywheel', 1.105, .22, 0, 11, 32)
radial_profile('Outer forged rim', [(1.02,-.13),(1.16,-.09),(1.16,.13),(1.10,.20),(.84,.20),(.81,.15),(.81,-.08)],32,6,[11,0,6,6,4,11,11])
# Sixteen identical swept teeth keep the silhouette rotationally symmetric.
# Their wedge-like bevels pick up the light, with alternating orange hazard paint.
for i in range(16):
    a = 2*math.pi*i/16
    points = [(1.055,-.095),(1.43,.015),(1.22,.15),(1.055,.12)]
    plate('Cutter tooth', rotate_points(points,a), -.095, .14, 6 if i%2==0 else 3, 11, .10)
    # A narrow bright cutting edge follows the leading point of each tooth.
    edge = [(1.34,.005),(1.43,.015),(1.22,.15),(1.225,.115)]
    plate('Hardened cutting edge', rotate_points(edge,a), .135, .151, 15, 6, .02)

# The raised teal arms leave six broad, dark flywheel slots between them.
for i in range(6):
    a = i*math.pi/3
    spoke = [(.31,-.12),(.82,-.19),(.96,.035),(.47,.205)]
    plate('Swept spoke', rotate_points(spoke,a), .16, .36, 0, 11, .13)
    inset = [(.50,-.065),(.76,-.105),(.83,.005),(.53,.08)]
    plate('Spoke inset', rotate_points(inset,a), .356, .385, 4, 0, .08)
    # Single recessed fastener per arm, broad enough to survive gameplay scale.
    x,y=rotate_points([(.73,0)],a)[0]
    bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=.045, depth=.018, location=(x,y,.395))
    ob=bpy.context.object
    ob.name='Spoke fastener'
    ob.data.materials.append(mats[11])
    parts.append(ob)

# Stepped bearing and orange capped axle make the middle read as machinery.
radial_profile('Bearing collar',[(.25,.24),(.46,.24),(.46,.34),(.40,.43),(.27,.43)],16,6,[11,6,6,4,11])
disk('Axle housing', .255, .13, .43, 11, 12)
radial_profile('Axle cap',[(.10,.495),(.19,.495),(.19,.55),(.14,.60),(.10,.60)],12,12,[11,12,3,3,11])
disk('Axle center', .085, .025, .61, 13, 8)

# Join and bake transforms; OBJ is exported before cameras and lights exist.
bpy.ops.object.select_all(action='DESELECT')
for ob in parts:
    ob.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for poly in model.data.polygons:
    poly.use_smooth = False
# Ensure the saved/exported geometry stays within the stated budget.
model.data.calc_loop_triangles()
assert 300 <= len(model.data.loop_triangles) <= 6000, len(model.data.loop_triangles)
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
scene.world = bpy.data.worlds.new('Dark blue-gray world')
scene.world.use_nodes=True
background=scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value=(.05,.07,.09,1)
background.inputs['Strength'].default_value=1
scene.view_settings.view_transform='Standard'

def point_at(ob, target):
    ob.rotation_euler=(Vector(target)-ob.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data=bpy.data.lights.new(name,kind)
    data.energy=energy
    if size is not None:
        data.shape='DISK'
        data.size=size
    ob=bpy.data.objects.new(name,data)
    bpy.context.collection.objects.link(ob)
    ob.location=location
    point_at(ob,target)
    return ob

sun=light('Upper left sun','SUN',(-4,5,7),2.0,(0,0,0))
sun.data.angle=math.radians(12)
light('Soft fill','AREA',(4,-3,5),260,(0,0,0),5)
camdata=bpy.data.cameras.new('Camera')
cam=bpy.data.objects.new('Camera',camdata)
bpy.context.collection.objects.link(cam)
scene.camera=cam
cam.location=(0,0,8)
cam.rotation_euler=(0,0,0)
camdata.type='ORTHO'
camdata.ortho_scale=3.36
scene.render.filepath=os.path.join(OUT,'top.png')
bpy.ops.render.render(write_still=True)
camdata.type='PERSP'
camdata.lens=50
cam.location=(4.2,-5.8,5.1)
point_at(cam,(0,0,.13))
scene.render.filepath=os.path.join(OUT,'angle.png')
bpy.ops.render.render(write_still=True)
