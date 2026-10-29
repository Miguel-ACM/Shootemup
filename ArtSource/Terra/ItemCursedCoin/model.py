import bpy
import math
import os
import sys
from mathutils import Vector

# Resolve the output folder even when the script is launched via Blender's -- argument.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.dirname(os.path.abspath(args[args.index('--') + 1])) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablock in bpy.data.materials:
    bpy.data.materials.remove(datablock)

palette = [
    (0.72, 0.40, 0.075, 1), # warm coin edge
    (0.96, 0.68, 0.16, 1),  # gold bevel
    (0.78, 0.49, 0.10, 1),  # inset field
    (0.48, 0.82, 0.30, 1),  # cursed green rim
    (0.83, 0.88, 0.61, 1),  # bone
    (0.25, 0.32, 0.13, 1),  # sockets
    (1.00, 0.79, 0.29, 1),  # bright rim studs
]
mats = []
for i, color in enumerate(palette):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = color
    m.use_nodes = True
    m.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value = color
    m.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value = 0.75
    mats.append(m)

parts = []
def mesh_obj(name, verts, faces, indices):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.materials.clear()
    for m in mats:
        mesh.materials.append(m)
    mesh.update()
    ob = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(ob)
    for p, index in zip(mesh.polygons, indices):
        p.material_index = index
        p.use_smooth = False
    parts.append(ob)
    return ob

# Concentric faceted profiles produce an actual thick coin, with a green
# enamel stripe and a raised gold lip on each face. The axis is Z.
N = 24
profile = [(-.090, .335), (-.085, .365), (-.060, .400), (-.036, .420),
           (.036, .420), (.060, .400), (.085, .365), (.090, .335)]
verts = [(r*math.cos(2*math.pi*i/N), r*math.sin(2*math.pi*i/N), z)
         for z, r in profile for i in range(N)]
faces = []
indices = []
for j in range(len(profile)-1):
    for i in range(N):
        faces.append((j*N+i, j*N+(i+1)%N, (j+1)*N+(i+1)%N, (j+1)*N+i))
        indices.append([1, 1, 3, 0, 3, 1, 1][j])
faces.extend([tuple(reversed(range(N))), tuple((len(profile)-1)*N+i for i in range(N))])
indices.extend([2, 2])
mesh_obj('Coin', verts, faces, indices)

# Extruded shapes on both faces: a broad, unmistakable skull and jaw.
def plaque(name, points, z0, z1, material):
    n = len(points)
    vs = [(x,y,z0) for x,y in points] + [(x,y,z1) for x,y in points]
    fs = [tuple(reversed(range(n))), tuple(range(n,2*n))]
    fs += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh_obj(name, vs, fs, [material]*len(fs))

skull = [(-.18,.12),(-.16,.20),(-.09,.25),(.09,.25),(.16,.20),(.18,.12),
         (.16,.045),(.12,.005),(.105,-.075),(.07,-.105),(-.07,-.105),
         (-.105,-.075),(-.12,.005),(-.16,.045)]
jaw = [(-.105,-.10),(.105,-.10),(.09,-.17),(.055,-.195),(-.055,-.195),(-.09,-.17)]
for side in (-1, 1):
    # Reversing X on the back keeps the design consistent when viewed from behind.
    def face_shape(name, points, base, height, mat):
        pts = [(side*x, y) for x,y in points]
        plaque(name, pts, side*base, side*(base+height), mat)
    face_shape('Skull', skull, .091, .025, 4)
    face_shape('Jaw', jaw, .091, .025, 4)
    for x in (-.075, .075):
        eye = [(x-.048,.105), (x-.035,.145), (x+.035,.145),
               (x+.048,.105), (x+.030,.065), (x-.030,.065)]
        face_shape('Eye socket', eye, .117, .006, 5)
    face_shape('Nose', [(-.025,.014),(0,.052),(.025,.014)], .117, .006, 5)
    for x in (-.045, 0, .045):
        face_shape('Tooth groove', [(x-.008,-.135),(x+.008,-.135),
                                   (x+.008,-.18),(x-.008,-.18)], .117, .006, 5)
    # Chunky diamond-shaped relief around the rim; all stay within the coin.
    for i in range(8):
        a = 2*math.pi*i/8 + math.pi/8
        cx, cy = .305*math.cos(a), .305*math.sin(a)
        radial = Vector((math.cos(a), math.sin(a)))
        tangent = Vector((-math.sin(a), math.cos(a)))
        pts = []
        for dr, dt in ((.024,0),(0,.014),(-.024,0),(0,-.014)):
            pts.append((cx+dr*radial.x+dt*tangent.x, cy+dr*radial.y+dt*tangent.y))
        face_shape('Rim mark', pts, .091, .008, 6)

# Combine into the required single object, with all transforms baked.
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
bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

# Studio lighting and two views, created after export so they never enter the OBJ.
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 512
scene.render.resolution_y = 512
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Studio world') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (.05,.07,.09,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(obj, target=(0,0,0)):
    obj.rotation_euler = (Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, loc, energy, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = loc
    aim(obj)
    return obj
light('Sun', 'SUN', (-3, 5, 6), 2.0)
light('Soft fill', 'AREA', (4, 1, 5), 110, 5.0)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.20
cam.location = (0,0,4)
aim(cam)
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam_data.lens = 50
cam.location = (0.70, 0.43, 1.48)
aim(cam)
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

