import bpy
import math
import json
import os
import sys
from mathutils import Vector

# Run in Blender: blender --background --python model.py -- /path/to/output
# The optional argument is only needed when __file__ is unavailable.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    if '--' not in sys.argv or not sys.argv[sys.argv.index('--') + 1:]:
        raise RuntimeError('Pass the script directory after -- when __file__ is unavailable')
    path = os.path.abspath(sys.argv[sys.argv.index('--') + 1])
    OUT = path if os.path.isdir(path) else os.path.dirname(path)
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablock in bpy.data.materials:
    bpy.data.materials.remove(datablock)

colors = [(0.24,0.35,0.40),(0.78,0.83,0.79),(0.35,0.43,0.30),
          (0.95,0.38,0.12),(0.32,0.48,0.55),(0.54,0.18,0.16),
          (0.53,0.61,0.62),(0.34,0.31,0.48),(0.20,0.36,0.40),
          (0.16,0.83,0.91),(1.0,0.77,0.27),(0.055,0.09,0.12),
          (0.62,0.25,0.19),(0.82,0.64,0.26),(0.53,0.61,0.55),
          (0.80,0.84,0.83)]
mats = []
for i, color in enumerate(colors):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    p = m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value = (*color, 1)
    p.inputs['Roughness'].default_value = 0.82
    if i in (9, 10):
        p.inputs['Emission Color'].default_value = (*color, 1)
        p.inputs['Emission Strength'].default_value = 1.8
    mats.append(m)

body = []
def mesh(name, verts, faces, mat):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.materials.append(mats[mat])
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    body.append(obj)
    return obj

def plate(name, points, bottom, top, mat, inset=0.0):
    # Each plate has a bevelled perimeter and a raised, planar top.
    if sum(points[i][0]*points[(i+1)%len(points)][1] - points[(i+1)%len(points)][0]*points[i][1] for i in range(len(points))) < 0:
        points = list(reversed(points))
    n = len(points)
    cx = sum(p[0] for p in points)/n
    cy = sum(p[1] for p in points)/n
    inner = [(cx+(x-cx)*(1-inset), cy+(y-cy)*(1-inset)) for x,y in points]
    verts = [(x,y,bottom) for x,y in points] + [(x,y,top-0.035) for x,y in points] + [(x,y,top) for x,y in inner]
    faces = [tuple(reversed(range(n)))]
    for i in range(n):
        j = (i+1)%n
        faces.extend([(i,j,n+j,n+i),(n+i,n+j,2*n+j,2*n+i)])
    faces.append(tuple(range(2*n,3*n)))
    return mesh(name, verts, faces, mat)

def loft(name, rings, mat):
    if sum(rings[0][i][0]*rings[0][(i+1)%len(rings[0])][1] - rings[0][(i+1)%len(rings[0])][0]*rings[0][i][1] for i in range(len(rings[0]))) < 0:
        rings = [list(reversed(r)) for r in rings]
    n = len(rings[0]); verts = [p for ring in rings for p in ring]
    faces = [tuple(reversed(range(n)))]
    for k in range(len(rings)-1):
        for i in range(n):
            j = (i+1)%n
            faces.append((k*n+i,k*n+j,(k+1)*n+j,(k+1)*n+i))
    faces.append(tuple(range((len(rings)-1)*n,len(rings)*n)))
    return mesh(name, verts, faces, mat)

def ellipsoid(name, loc, scale, mat, segments=12, rings=6):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=loc)
    o = bpy.context.object; o.name = name; o.scale = scale
    o.data.materials.append(mats[mat]); body.append(o)
    return o

def ring(name, center, outer, inner, depth, mat, direction='Z', segments=8):
    # A true annulus with a recessed inner wall, not a painted disk.
    x,y,z = center
    verts = []
    for radius, offset in ((outer,0),(inner,0),(inner,-depth),(outer,-depth)):
        for i in range(segments):
            a = 2*math.pi*i/segments
            if direction == 'Z':
                verts.append((x+radius*math.cos(a),y+radius*math.sin(a),z+offset))
            else: # outward is +Y, for aft sockets
                verts.append((x+radius*math.cos(a),y+offset,z+radius*math.sin(a)))
    faces=[]
    for k in range(4):
        for i in range(segments):
            j=(i+1)%segments
            faces.append((k*segments+i,k*segments+j,((k+1)%4)*segments+j,((k+1)%4)*segments+i))
    return mesh(name,verts,faces,mat)

def disk(name, center, radius, mat, direction='Z', segments=8):
    x,y,z=center
    if direction == 'Z':
        verts=[(x+radius*math.cos(2*math.pi*i/segments),y+radius*math.sin(2*math.pi*i/segments),z) for i in range(segments)]
    else:
        verts=[(x+radius*math.cos(2*math.pi*i/segments),y,z+radius*math.sin(2*math.pi*i/segments)) for i in range(segments)]
    return mesh(name,verts,[tuple(range(segments))],mat)

# Tapered, three-level coffin hull: the stern is high and the prow knife-like.
outline=[(0,-1.55),(-.61,-.90),(-.76,-.48),(-.83,.85),(-.66,1.10),(0,1.32),(.66,1.10),(.83,.85),(.76,-.48),(.61,-.90)]
def scaled_ring(scale,z):
    return [(x*scale,y*scale,z) for x,y in outline]
loft('Keel', [scaled_ring(.76,-.24),scaled_ring(1.0,-.08),scaled_ring(.94,.23),scaled_ring(.78,.32)],11)
plate('Deck',[(0,-1.42),(-.53,-.86),(-.65,-.35),(-.69,.84),(0,1.17),(.69,.84),(.65,-.35),(.53,-.86)],.25,.35,5,.12)
# The broken pale sails are fixed, sculptural armour rather than animated wings.
for s in (-1,1):
    def side(p): return [(s*x,y) for x,y in p]
    plate('Gunwale', side([(.59,-.94),(.83,-.78),(1.07,-.48),(1.10,.72),(.88,1.02),(.68,.86)]),.02,.23,12,.14)
    plate('Bone canopy', side([(.13,1.19),(1.22,.63),(.95,-.27),(.62,-.11),(.13,-.39)]),.54,.69,15,.08)
    plate('Canopy root',side([(.13,1.09),(.39,.92),(.40,-.16),(.14,-.34)]),.68,.73,6,.12)
    plate('Sail shadow',side([(.39,.85),(1.08,.57),(.93,.14),(.55,.25)]),.695,.715,14,.09)
    # Broad bone braces divide the pale canopy into a few large readable planes.
    plate('Bone rib',side([(.20,.92),(.27,.97),(.99,.51),(.96,.44)]),.72,.755,1,.08)
    plate('Bone rib',side([(.24,.06),(.29,.14),(.92,.32),(.90,.23)]),.72,.755,1,.08)
    # Fixed gun cradles, with the lip centered on each gameplay muzzle.
    for y in (.45,-.10,-.65):
        plate('Gun cradle',side([(.70,y-.17),(1.02,y-.17),(1.08,y-.08),(1.08,y+.13),(.72,y+.15)]),.20,.28,6,.15)
        ring('Muzzle lip',(s*.88,y,.30),.115,.065,.075,13)
        disk('Muzzle bore',(s*.88,y,.226),.064,11)
    # Two rear thruster connection sockets, centered at gameplay coordinates.
    ring('Engine socket',(s*.874,.93,0),.105,.065,.10,12,direction='Y')
    disk('Engine darkness',(s*.874,.831,0),.064,11,direction='Y')

# A raised central spine and ferryman's skull mask on the pointed prow.
plate('Spine', [(-.12,-.35),(-.14,.99),(0,1.20),(.14,.99),(.12,-.35),(0,-.56)],.34,.53,11,.10)
plate('Skull', [(-.33,-.51),(-.32,-.91),(-.22,-1.14),(0,-1.27),(.22,-1.14),(.32,-.91),(.33,-.51),(.23,-.39),(-.23,-.39)],.35,.57,1,.18)
for s in (-1,1):
    # Dark eye sockets with small ember pupils face upward for gameplay readability.
    plate('Eye socket',[(s*.06,-.70),(s*.28,-.63),(s*.26,-.83),(s*.10,-.87)],.56,.605,11,.10)
    ellipsoid('Ember eye',(s*.175,-.755,.611),(.064,.048,.024),10,8,4)
    plate('Cheek',[(s*.27,-.87),(s*.35,-.84),(s*.25,-1.09),(s*.16,-1.12)],.55,.62,15,.14)
plate('Nasal cavity',[(-.06,-.91),(0,-.84),(.06,-.91),(0,-1.03)],.58,.615,11,.10)
plate('Jaw',[(-.20,-1.08),(-.14,-1.23),(0,-1.30),(.14,-1.23),(.20,-1.08)],.40,.51,6,.13)
for x in (-.12,0,.12):
    plate('Tooth',[(x-.035,-1.11),(x+.035,-1.11),(x+.026,-1.21),(x-.026,-1.21)],.51,.55,15,.10)

# Merge all static geometry, bake its object transforms, and keep flat facets.
bpy.ops.object.select_all(action='DESELECT')
for obj in body:
    obj.select_set(True)
bpy.context.view_layer.objects.active = body[0]
bpy.ops.object.convert(target='MESH')
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for poly in model.data.polygons:
    poly.use_smooth = False
model.data.calc_loop_triangles()
if len(model.data.loop_triangles) > 4000:
    raise RuntimeError('Triangle budget exceeded: %d' % len(model.data.loop_triangles))

# This ferryman has no naturally separate moving parts.
with open(os.path.join(OUT,'parts.json'),'w',encoding='utf-8') as f:
    json.dump([],f,indent=2)
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
scene.world = bpy.data.worlds.new('Dark blue gray') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes = True
bg = scene.world.node_tree.nodes.get('Background')
bg.inputs['Color'].default_value = (.05,.07,.09,1)
bg.inputs['Strength'].default_value = 1
scene.view_settings.view_transform = 'Standard'

def aim(obj, target):
    obj.rotation_euler = (Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, loc, energy, target, size=None):
    data = bpy.data.lights.new(name,kind); data.energy = energy
    obj = bpy.data.objects.new(name,data); bpy.context.collection.objects.link(obj)
    obj.location=loc; aim(obj,target)
    if size is not None: data.size = size
    return obj
sun = light('Sun','SUN',(-3,4,7),2.0,(0,0,0))
sun.data.angle = math.radians(18)
light('Soft fill','AREA',(4,-2,5),300,(0,0,0),5)
camdata = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera',camdata); bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0,0,7); aim(cam,(0,0,0))
camdata.type = 'ORTHO'; camdata.ortho_scale = 3.38
scene.render.filepath = os.path.join(OUT,'top.png')
bpy.ops.render.render(write_still=True)
camdata.type = 'PERSP'; camdata.lens = 50
cam.location = (4.1,-5.4,4.2); aim(cam,(0,-.05,.2))
scene.render.filepath = os.path.join(OUT,'angle.png')
bpy.ops.render.render(write_still=True)

