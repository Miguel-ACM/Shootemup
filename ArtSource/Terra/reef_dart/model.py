import bpy
import bmesh
import math
import os
import sys
from mathutils import Vector

# Save beside this script, including when launched with Blender's -- argument.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    OUT = os.path.dirname(os.path.abspath(args[0])) if args else os.getcwd()
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

palette = [
    (0.24,0.35,0.40), (0.78,0.83,0.79), (0.35,0.43,0.30),
    (0.95,0.38,0.12), (0.32,0.48,0.55), (0.54,0.18,0.16),
    (0.53,0.61,0.62), (0.34,0.31,0.48), (0.20,0.36,0.40),
    (0.16,0.83,0.91), (1.0,0.77,0.27), (0.055,0.09,0.12),
    (0.62,0.25,0.19), (0.82,0.64,0.26), (0.53,0.61,0.55),
    (0.80,0.84,0.83),
]
mats = []
for i, color in enumerate(palette):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1)
    bsdf.inputs['Roughness'].default_value = 0.72
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*color, 1)
        bsdf.inputs['Emission Strength'].default_value = 1.5
    mats.append(m)

parts = []
def mesh_part(name, verts, faces, material, face_mats=None):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    for m in mats:
        mesh.materials.append(m)
    for p in mesh.polygons:
        p.material_index = material if face_mats is None else face_mats[p.index]
        p.use_smooth = False
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    bm.to_mesh(mesh)
    bm.free()
    parts.append(obj)
    return obj

def plate(name, outline, bottom, top, material, side=0):
    # Convex plates: raised, beveled perimeter and a smaller top face.
    n = len(outline)
    cx = sum(p[0] for p in outline) / n
    cy = sum(p[1] for p in outline) / n
    verts = [(x,y,bottom) for x,y in outline]
    verts += [(x,y,top-0.025) for x,y in outline]
    verts += [(cx+(x-cx)*0.87, cy+(y-cy)*0.87, top) for x,y in outline]
    faces = [tuple(reversed(range(n)))]
    ids = [side]
    for i in range(n):
        j = (i+1) % n
        faces.extend([(i,j,n+j,n+i), (n+i,n+j,2*n+j,2*n+i)])
        ids.extend([side, material])
    faces.append(tuple(range(2*n,3*n)))
    ids.append(material)
    return mesh_part(name, verts, faces, material, ids)

# A tapered, faceted torpedo body. Each section is an eight-sided ellipse.
sections = [(-0.92,0.035,0.035,0.015), (-0.78,0.17,0.12,0.015),
            (-0.52,0.27,0.20,0.015), (-0.16,0.34,0.25,0.015),
            (0.25,0.32,0.23,0.015), (0.60,0.22,0.16,0.015),
            (0.85,0.105,0.09,0.015), (0.92,0.05,0.05,0.015)]
verts = []
for y, width, height, zc in sections:
    for j in range(8):
        a = 2*math.pi*j/8
        verts.append((width*math.cos(a), y, zc+height*math.sin(a)))
faces = [tuple(reversed(range(8)))]
ids = [0]
for k in range(len(sections)-1):
    for j in range(8):
        faces.append((8*k+j,8*k+(j+1)%8,8*(k+1)+(j+1)%8,8*(k+1)+j))
        ids.append(8 if j in (0,1,2,3) else 0)
faces.append(tuple(range(8*(len(sections)-1),8*len(sections))))
ids.append(0)
mesh_part('torpedo hull', verts, faces, 8, ids)

# Broad swept pectoral fins and the brass forked tail give a fish silhouette.
for s in (-1, 1):
    plate('swept fin', [(s*.21,-.23),(s*.43,-.20),(s*.85,.63),
                        (s*.56,.52),(s*.24,.35)], -.035,.085, 6, 0)
    plate('fin inset', [(s*.38,-.07),(s*.75,.53),(s*.54,.43),
                        (s*.31,.22)], .087,.104, 4, 4)
    plate('tail lobe', [(s*.035,.67),(s*.17,.82),(s*.37,1.12),
                        (s*.14,1.03),(0,.91)], -.02,.095, 13, 0)
    # Layered cheek armor points toward the nose, echoing gill covers.
    plate('cheek', [(s*.08,-.72),(s*.23,-.56),(s*.29,-.20),
                    (s*.23,.03),(s*.14,-.15)], .15,.205, 4, 0)

# A prominent central dorsal carapace, with a tapered brass ridge.
plate('dorsal carapace', [(0,-.78),(-.13,-.53),(-.19,-.08),(-.15,.35),
                         (0,.67),(.15,.35),(.19,-.08),(.13,-.53)],
      .205,.285, 0, 8)
plate('spine', [(0,-.68),(-.045,-.42),(-.055,.25),(0,.51),
                (.055,.25),(.045,-.42)], .286,.31, 13, 0)

def gun(x):
    # Annular muzzle faces -Y. The front lip is exactly at the gameplay point.
    # An open dark bore and a small amber inset remain readable from above.
    z = .22
    outer, inner, n = .084, .047, 8
    verts = []
    for y, radius in [(-.46,.070),(-.55,outer),(-.55,inner),(-.515,inner)]:
        for j in range(n):
            a = 2*math.pi*j/n
            verts.append((x+radius*math.cos(a), y, z+radius*math.sin(a)))
    faces = []
    ids = []
    for j in range(n):
        k=(j+1)%n
        faces.extend([(j,k,n+k,n+j),(n+j,n+k,2*n+k,2*n+j),
                      (2*n+j,2*n+k,3*n+k,3*n+j)])
        ids.extend([0,13,11])
    faces.append(tuple(range(3*n,4*n)))
    ids.append(11)
    mesh_part('barrel', verts, faces, 0, ids)
    # Raised sight line over each barrel makes the paired weapons legible top-down.
    plate('gun housing', [(x-.066,-.49),(x-.061,-.34),(x,-.29),
                          (x+.061,-.34),(x+.066,-.49)], .20,.255, 6, 0)
    plate('muzzle marker', [(x-.025,-.545),(x-.025,-.505),
                            (x+.025,-.505),(x+.025,-.545)], .286,.292, 10, 13)
for x in (-.17,.17):
    gun(x)

# Join before export; all pieces already use final world-space coordinates.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for p in model.data.polygons:
    p.use_smooth = False
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
scene.world = bpy.data.worlds.new('Blue-gray world')
scene.world.use_nodes = True
bg = scene.world.node_tree.nodes.get('Background')
bg.inputs['Color'].default_value = (0.05,0.07,0.09,1)
bg.inputs['Strength'].default_value = 1.0

def aim(obj, point):
    obj.rotation_euler = (Vector(point)-obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    aim(obj, target)
    return obj

light('Sun', 'SUN', (-3,-4,7), 2.0, (0,0,0))
light('Soft fill', 'AREA', (4,1,4), 180, (0,0,0), 5.0)
camera_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', camera_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
camera_data.type = 'ORTHO'
camera_data.ortho_scale = 2.4
cam.location = (0,0.10,6)
aim(cam, (0,0.10,0))
scene.render.filepath = os.path.join(OUT,'top.png')
bpy.ops.render.render(write_still=True)
camera_data.type = 'PERSP'
camera_data.lens = 50
cam.location = (3.0,-4.1,3.8)
aim(cam, (0,0.08,0.06))
scene.render.filepath = os.path.join(OUT,'angle.png')
bpy.ops.render.render(write_still=True)
