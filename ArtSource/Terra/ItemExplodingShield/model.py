import bpy
import math
import os
import sys
from mathutils import Vector

# Exploding Shield: a forged heater shield with a raised blast on each face.
if '__file__' in globals():
    OUT = os.path.dirname(os.path.abspath(__file__))
else:
    args = sys.argv
    OUT = os.path.dirname(os.path.abspath(args[args.index('--') + 1])) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for mat in list(bpy.data.materials):
    bpy.data.materials.remove(mat)

palette = [
    (0.43, 0.19, 0.10, 1),  # warm bronze edge
    (0.95, 0.36, 0.075, 1), # orange enamel
    (1.00, 0.57, 0.13, 1), # bevel
    (1.00, 0.78, 0.23, 1), # blast gold
    (1.00, 0.94, 0.55, 1), # blast core
    (0.60, 0.26, 0.10, 1), # fuse collar
    (0.83, 0.66, 0.35, 1), # wick
    (1.00, 0.28, 0.08, 1), # spark
]
mats = []
for i, color in enumerate(palette):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = color
    m.use_nodes = True
    m.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value = color
    m.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value = 0.72
    mats.append(m)

def mesh_obj(name, verts, faces, indices):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    for m in mats:
        mesh.materials.append(m)
    for poly, idx in zip(mesh.polygons, indices):
        poly.material_index = idx
        poly.use_smooth = False
    return obj

# Seven-sided shield, with a thick rounded-over perimeter and crowned faces.
outline = [(-.345,.305), (.345,.305), (.375,.035), (.235,-.22), (0,-.405), (-.235,-.22), (-.375,.035)]
# Rings go from the rear raised face, over the outside edge to the front.
rings = [(0.78,-.105), (0.94,-.095), (1.0,-.055), (1.0,.055), (0.94,.095), (0.78,.105)]
verts = [(x*s,y*s,z) for s,z in rings for x,y in outline]
faces = [tuple(reversed(range(7)))]
ids = [1]
for j in range(len(rings)-1):
    for k in range(7):
        n = (k+1)%7
        faces.append((j*7+k,j*7+n,(j+1)*7+n,(j+1)*7+k))
        ids.append([2,0,0,0,2][j])
faces.append(tuple(range(35,42)))
ids.append(1)
mesh_obj('Forged shield', verts, faces, ids)

def star(side):
    # A broad eight-point burst, raised as a solid emblem on both sides.
    points = []
    for i in range(16):
        a = math.pi/2 + i*math.pi/8
        r = .223 if i%2 == 0 else .103
        points.append((r*math.cos(a), .005+r*math.sin(a)))
    z0, z1 = side*.108, side*.145
    verts = [(x,y,z) for z in (z0,z1) for x,y in points]
    faces = []
    ids = []
    # Triangular fan prevents concave ngon tessellation artifacts.
    verts.append((0,.005,z1))
    for i in range(16):
        n = (i+1)%16
        faces.append((i,n,16+n,16+i) if side > 0 else (16+i,16+n,n,i))
        ids.append(2)
        faces.append((32,16+i,16+n) if side > 0 else (32,16+n,16+i))
        ids.append(3)
    mesh_obj('Burst', verts, faces, ids)

def sphere(name, location, scale, mat, segments=12, rings=6):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(mats[mat])
    for p in obj.data.polygons:
        p.use_smooth = False
    return obj

for side in (-1, 1):
    star(side)
    sphere('Blast core', (0,.005,side*.151), (.067,.067,.035), 4)
    # Large rivets anchor the frame and read at pickup scale.
    for x,y in [(-.275,.245),(.275,.245),(-.29,.025),(.29,.025),(0,-.335)]:
        sphere('Rivet', (x,y,side*.088), (.028,.028,.018), 3, 8, 4)

def cylinder_between(name, a, b, radius, mat, vertices=10):
    a, b = Vector(a), Vector(b)
    d = b-a
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=d.length, location=(a+b)/2)
    obj = bpy.context.object
    obj.name = name
    obj.rotation_mode = 'QUATERNION'
    obj.rotation_quaternion = d.to_track_quat('Z','Y')
    obj.data.materials.append(mats[mat])
    return obj

# Compact fuse, substantial enough to survive an 80-pixel render.
cylinder_between('Fuse socket', (0,.292,0), (0,.36,0), .055, 5, 12)
cylinder_between('Wick', (0,.35,0), (.022,.412,0), .023, 6, 8)
sphere('Spark', (.028,.426,0), (.052,.052,.052), 7, 10, 5)
sphere('Spark center', (.028,.43,.03), (.027,.027,.028), 4, 8, 4)

# Join and bake all transforms and triangulation before export.
bpy.ops.object.select_all(action='SELECT')
bpy.context.view_layer.objects.active = bpy.context.selected_objects[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
tri = model.modifiers.new('Triangulate', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
for p in model.data.polygons:
    p.use_smooth = False
bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 512
scene.render.resolution_y = 512
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Blue gray studio') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (.05,.07,.09,1)
background.inputs['Strength'].default_value = 1.0

def aim(obj, target=(0,0,0)):
    obj.rotation_euler = (Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, location, energy, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if kind == 'AREA':
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    aim(obj)

light('Upper left sun', 'SUN', (-3,4,5), 2.0)
light('Soft fill', 'AREA', (3,1,4), 150, 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0,0,4)
aim(cam)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.10
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam_data.lens = 50
cam.location = (1.45, .8, 2.75)
aim(cam, (0,0,0))
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)
