import bpy
import math
import os
import sys
from mathutils import Vector, Matrix

# This model uses Y for vertical and Z for its front.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    tail = args[args.index('--') + 1:] if '--' in args else []
    if not tail:
        raise RuntimeError('Pass the script path or output directory after --')
    path = os.path.abspath(tail[0])
    OUT = path if os.path.isdir(path) else os.path.dirname(path)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for mat in list(bpy.data.materials):
    bpy.data.materials.remove(mat)

palette = [
    ('tile_00', (0.82, 0.075, 0.055, 1)), # tomato red paper
    ('tile_01', (1.00, 0.88, 0.65, 1)),  # warm cream stripes
    ('tile_02', (0.96, 0.24, 0.12, 1)),  # rolled red rim
    ('tile_03', (0.97, 0.78, 0.39, 1)),  # buttery yellow
    ('tile_04', (1.00, 0.93, 0.72, 1)),  # popcorn cream
    ('tile_05', (0.91, 0.65, 0.27, 1)),  # toasted gold
]
mats = []
for name, color in palette:
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = 0.8
    mats.append(mat)

parts = []
def mesh_part(name, verts, faces, indices):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.materials.clear()
    for mat in mats:
        mesh.materials.append(mat)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    for poly, idx in zip(mesh.polygons, indices):
        poly.material_index = idx
        poly.use_smooth = False
    parts.append(obj)
    return obj

# Four tapered paper walls, divided into bold vertical stripes on every side.
# Separate faces share coordinates, so stripes follow the carton's taper.
verts, faces, ids = [], [], []
for side in range(4):
    for i in range(5):
        a, b = -1 + 2*i/5, -1 + 2*(i+1)/5
        def point(t, y, half):
            if side == 0: return (t*half, y, half)
            if side == 1: return (half, y, -t*half)
            if side == 2: return (-t*half, y, -half)
            return (-half, y, t*half)
        k = len(verts)
        verts.extend([point(a, -.43, .235), point(b, -.43, .235),
                      point(b, .15, .325), point(a, .15, .325)])
        faces.append((k, k+1, k+2, k+3))
        ids.append(0 if i % 2 == 0 else 1)
# Bottom face closes the carton.
k = len(verts)
verts.extend([(-.235,-.43,-.235), (.235,-.43,-.235),
              (.235,-.43,.235), (-.235,-.43,.235)])
faces.append((k,k+1,k+2,k+3)); ids.append(1)
mesh_part('Striped tapered carton', verts, faces, ids)

# Thick square lip: outside walls, top ledge and inside walls make it solid
# and readable from above and from all sides.
def square_ring(name, rings, material_indices):
    vertices = []
    for y, h in rings:
        vertices.extend([(-h,y,-h), (h,y,-h), (h,y,h), (-h,y,h)])
    faces, indexes = [], []
    for r in range(len(rings)-1):
        for j in range(4):
            faces.append((4*r+j, 4*r+(j+1)%4, 4*(r+1)+(j+1)%4, 4*(r+1)+j))
            indexes.append(material_indices[r])
    return mesh_part(name, vertices, faces, indexes)
square_ring('Sturdy rolled lip', [(.125,.329), (.18,.34), (.18,.292), (.135,.289)], [2,1,0])

# Low-poly irregular popped kernels, arranged as a heaped crown. Each kernel
# has a large body and a smaller off-centre lobe, giving a popcorn silhouette.
def kernel(pos, size, material, lobe_offset, lobe_size):
    for center, scale, mat_index in [(pos, size, material),
                                      ((pos[0]+lobe_offset[0], pos[1]+lobe_offset[1], pos[2]+lobe_offset[2]), lobe_size, 4)]:
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=1, location=center)
        obj = bpy.context.object
        obj.name = 'Popped kernel'
        obj.scale = scale
        obj.data.materials.append(mats[mat_index])
        for poly in obj.data.polygons:
            poly.use_smooth = False
        parts.append(obj)

kernels = [
    ((-.23,.205,.20),(.115,.10,.105),4,(.045,.045,.012),(.065,.058,.06)),
    ((0,.205,.23),(.13,.105,.10),3,(-.042,.047,.00),(.065,.06,.06)),
    ((.23,.205,.20),(.115,.105,.105),4,(-.04,.045,.01),(.065,.06,.06)),
    ((-.24,.215,-.04),(.11,.105,.12),3,(.04,.045,-.015),(.065,.06,.06)),
    ((.00,.255,.015),(.13,.12,.12),4,(.04,.065,.015),(.07,.065,.065)),
    ((.24,.22,-.04),(.11,.105,.12),4,(-.04,.045,-.015),(.065,.06,.06)),
    ((-.20,.22,-.22),(.115,.105,.10),4,(.04,.04,.01),(.065,.06,.06)),
    ((.015,.255,-.20),(.13,.115,.105),3,(-.04,.05,.015),(.065,.06,.06)),
    ((.22,.22,-.21),(.115,.105,.10),4,(-.04,.045,.015),(.065,.06,.06)),
    ((-.11,.32,-.075),(.105,.105,.10),4,(.035,.045,.02),(.06,.055,.06)),
    ((.13,.315,.08),(.105,.10,.10),3,(-.03,.045,-.01),(.06,.055,.06)),
]
for p, s, m, off, ls in kernels:
    kernel(p, s, m, off, ls)

# Join before adding any render-only objects, bake transforms, triangulate.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
tri = model.modifiers.new('Export triangles', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
for face in model.data.polygons:
    face.use_smooth = False
bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'),
                      export_selected_objects=True, export_materials=True,
                      forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 512
scene.render.resolution_y = 512
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Blue gray world')
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(obj, target=(0,0,0)):
    forward = (Vector(target) - obj.location).normalized()
    right = forward.cross(Vector((0, 1, 0))).normalized()
    up = right.cross(forward).normalized()
    obj.rotation_euler = Matrix((right, up, -forward)).transposed().to_euler()

def light(name, kind, location, energy, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    obj.location = location
    aim(obj)
    return obj

sun = light('Upper left sun', 'SUN', (-3, 5, 6), 2.0)
sun.data.angle = math.radians(15)
light('Soft fill', 'AREA', (4, 2, 5), 110, 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 5)
aim(cam)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.08
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (1.04, 0.67, 1.48)
aim(cam, (0, 0, 0))
cam_data.type = 'PERSP'
cam_data.lens = 50
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

