import bpy
import math
import os
import sys
import json
from mathutils import Vector

# Blender's --python sets __file__; the argument after -- is a fallback.
if '__file__' in globals() and __file__:
    OUT = os.path.dirname(os.path.abspath(__file__))
else:
    args = sys.argv
    if '--' not in args or args.index('--') + 1 >= len(args):
        raise RuntimeError('Pass the script path or output directory after --')
    path = os.path.abspath(args[args.index('--') + 1])
    OUT = path if os.path.isdir(path) else os.path.dirname(path)
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for mat in list(bpy.data.materials):
    bpy.data.materials.remove(mat)

palette = [
    (0.24,0.35,0.40), (0.78,0.83,0.79), (0.35,0.43,0.30),
    (0.95,0.38,0.12), (0.32,0.48,0.55), (0.54,0.18,0.16),
    (0.53,0.61,0.62), (0.34,0.31,0.48), (0.20,0.36,0.40),
    (0.16,0.83,0.91), (1.0,0.77,0.27), (0.055,0.09,0.12),
    (0.62,0.25,0.19), (0.82,0.64,0.26), (0.53,0.61,0.55),
    (0.80,0.84,0.83),
]
materials = []
for i, rgb in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = (*rgb, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*rgb, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.77
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*rgb, 1.0)
        bsdf.inputs['Emission Strength'].default_value = 1.8
    materials.append(mat)

body = []
def mesh(name, verts, faces, tile):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(materials[tile])
    body.append(obj)
    return obj

def profile(name, rings, sides, tile, phase=math.pi/8):
    # Closed faceted stepped surface of revolution; rings are (radius, height).
    verts = [(r*math.cos(phase+2*math.pi*i/sides),
              r*math.sin(phase+2*math.pi*i/sides), z)
             for r, z in rings for i in range(sides)]
    faces = [tuple(reversed(range(sides)))]
    for j in range(len(rings)-1):
        for i in range(sides):
            k = (i+1) % sides
            faces.append((j*sides+i, j*sides+k, (j+1)*sides+k, (j+1)*sides+i))
    faces.append(tuple((len(rings)-1)*sides+i for i in range(sides)))
    return mesh(name, verts, faces, tile)

def plate(name, points, bottom, top, tile):
    verts = [(x,y,bottom) for x,y in points] + [(x,y,top) for x,y in points]
    n = len(points)
    faces = [tuple(reversed(range(n))), tuple(range(n, 2*n))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh(name, verts, faces, tile)

def mouth(x, y):
    # Annular lip and recessed bore; its opening is exactly at the gameplay point.
    sides = 8
    verts = []
    for radius,z in ((.083,.185),(.083,.25),(.049,.25),(.049,.22)):
        verts.extend((x+radius*math.cos(2*math.pi*i/sides),
                      y+radius*math.sin(2*math.pi*i/sides),z) for i in range(sides))
    faces=[]
    for ring in range(3):
        for i in range(sides):
            k=(i+1)%sides
            faces.append((ring*sides+i,ring*sides+k,(ring+1)*sides+k,(ring+1)*sides+i))
    lip=mesh('Barrel lip', verts, faces, 13)
    lip.data.materials.append(materials[11])
    for p in lip.data.polygons:
        if p.index >= sides*2:
            p.material_index = 1
    disk = [(x+.049*math.cos(2*math.pi*i/sides),y+.049*math.sin(2*math.pi*i/sides)) for i in range(sides)]
    plate('Dark bore', disk, .216, .22, 11)

# A low buried octagonal drum with a raised collar and ceramic central cap.
profile('Buried rust drum', [(.60,-.10),(.70,-.055),(.70,.055),(.64,.13)], 12, 12, math.pi/12)
profile('Dark seam', [(.605,.105),(.605,.145),(.575,.16)], 12, 11, math.pi/12)
profile('Brass turret collar', [(.575,.14),(.575,.19),(.51,.21)], 12, 13, math.pi/12)
# Eight pointed radial sand-breaker petals retain the original star silhouette.
for i in range(8):
    a = i*math.pi/4
    d = Vector((math.cos(a),math.sin(a)))
    n = Vector((-math.sin(a),math.cos(a)))
    def point(r, w):
        v=d*r+n*w
        return (v.x,v.y)
    points = [point(.41,-.12), point(.70,-.105), point(.82,0), point(.70,.105), point(.41,.12)]
    plate('Rust sand tooth', points, .045, .145, 12)
    inset = [point(.60,-.065),point(.72,-.05),point(.79,0),point(.72,.05),point(.60,.065)]
    plate('Ceramic tooth inset', inset, .145, .158, 14)
    mouth(.52*math.cos(a), .52*math.sin(a))

profile('Central dark gasket', [(.33,.19),(.33,.225),(.30,.235)], 8, 11)
profile('Ceramic crown', [(.30,.225),(.30,.30),(.245,.36),(.19,.38)], 8, 1)
profile('Rust crown rim', [(.19,.375),(.19,.392),(.16,.405)], 8, 12)
profile('Orange heart', [(.135,.404),(.135,.42),(.105,.435)], 8, 3)
profile('Small amber core', [(.052,.435),(.052,.44)], 8, 10)

# Join only the static body. No natural moving element carries these fixed muzzles.
bpy.ops.object.select_all(action='DESELECT')
for obj in body:
    obj.select_set(True)
bpy.context.view_layer.objects.active = body[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for poly in model.data.polygons:
    poly.use_smooth = False
model.data.calc_loop_triangles()
print('Model triangles:', len(model.data.loop_triangles))
with open(os.path.join(OUT, 'parts.json'), 'w', encoding='utf-8') as f:
    json.dump([], f, indent=2)

# Export before adding presentation-only lights and cameras.
bpy.ops.wm.obj_export(filepath=os.path.join(OUT,'model.obj'),
    export_selected_objects=True, export_materials=True,
    forward_axis='Y', up_axis='Z')

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
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (0.05,0.07,0.09,1)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(obj, target):
    obj.rotation_euler = (Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, loc, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None and kind == 'AREA':
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = loc
    aim(obj, target)
    return obj

light('Sun upper left', 'SUN', (-3,4,6), 2.0, (0,0,0))
light('Soft fill', 'AREA', (3,-2,4), 110, (0,0,0), 5.0)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0,0,5)
cam.rotation_euler = (0,0,0)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.93
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam_data.lens = 70
cam.location = (2.9,-3.7,3.4)
aim(cam, (0,0,.10))
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

