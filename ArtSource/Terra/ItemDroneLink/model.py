import bpy
import math
import os
import sys
from mathutils import Vector

# Drone Link: a compact magenta support drone with paired sensor/drive pods.
try:
    OUT_DIR = os.path.dirname(os.path.abspath(__file__))
except NameError:
    if '--' not in sys.argv or not sys.argv[sys.argv.index('--') + 1:]:
        raise RuntimeError('Pass the script directory after -- when __file__ is unavailable')
    OUT_DIR = os.path.abspath(sys.argv[sys.argv.index('--') + 1])
    if os.path.isfile(OUT_DIR):
        OUT_DIR = os.path.dirname(OUT_DIR)
os.makedirs(OUT_DIR, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for material in list(bpy.data.materials):
    bpy.data.materials.remove(material)

palette = [
    (0.72, 0.25, 0.66, 1), # magenta shell
    (0.92, 0.48, 0.82, 1), # pink face
    (0.46, 0.22, 0.51, 1), # plum structural band
    (0.42, 0.70, 0.76, 1), # pale blue-gray struts
    (0.14, 0.67, 0.78, 1), # cyan lens
    (0.53, 0.92, 0.92, 1), # lens highlight
    (0.90, 0.70, 0.87, 1), # lavender edge
    (0.28, 0.36, 0.49, 1), # muted blue inset
]
mats = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.use_nodes = True
    mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value = color
    mat.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value = 0.68
    mat.diffuse_color = color
    mats.append(mat)

parts = []
def mesh_part(name, vertices, faces, material_indices):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.materials.clear()
    for mat in mats:
        mesh.materials.append(mat)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    for poly, index in zip(mesh.polygons, material_indices):
        poly.material_index = index
        poly.use_smooth = False
    parts.append(obj)
    return obj

def drum(name, x, y, rings, sides=12):
    # Rings are (z, radius, material for the band leading to the next ring).
    vertices = []
    for z, radius, _ in rings:
        for i in range(sides):
            a = 2 * math.pi * i / sides + math.pi / sides
            vertices.append((x + radius * math.cos(a), y + radius * math.sin(a), z))
    faces = [tuple(reversed(range(sides)))]
    indices = [rings[0][2]]
    for j in range(len(rings) - 1):
        for i in range(sides):
            k = (i + 1) % sides
            faces.append ( (j*sides+i, j*sides+k, (j+1)*sides+k, (j+1)*sides+i) )
            indices.append(rings[j][2])
    faces.append(tuple((len(rings)-1)*sides+i for i in range(sides)))
    indices.append(rings[-1][2])
    return mesh_part(name, vertices, faces, indices)

def box(name, location, scale, mat, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mats[mat])
    if bevel:
        mod = obj.modifiers.new('Chamfer', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    parts.append(obj)
    return obj

# A substantial octagonal hull with a lighter faceted front and back.
drum('Core hull', 0, 0, [(-.135,.145,2),(-.115,.212,0),(.095,.212,0),(.145,.155,1)], 12)
drum('Front bezel', 0, 0, [(.143,.137,6),(.166,.126,2),(.173,.100,2)], 12)
drum('Front eye', 0, 0, [(.174,.092,4),(.190,.078,4)], 12)
drum('Eye glint', -.024, .027, [(.191,.023,5),(.197,.021,5)], 8)
drum('Rear bezel', 0, 0, [(-.158,.112,2),(-.139,.132,6)], 12)
drum('Rear eye', 0, 0, [(-.179,.075,4),(-.158,.090,4)], 12)

# Short sturdy wings and matching pods. The side pods have colored faces on both sides.
for sign in (-1, 1):
    box('Wing', (sign*.275, 0, 0), (.25,.125,.16), 3, .025)
    box('Wing shoulder', (sign*.205, 0, 0), (.105,.17,.19), 6, .018)
    x = sign*.365
    drum('Pod housing', x, 0, [(-.105,.091,2),(-.083,.114,0),(.083,.114,0),(.105,.091,6)], 12)
    drum('Pod front lens', x, 0, [(.106,.071,4),(.119,.064,4)], 12)
    drum('Pod rear lens', x, 0, [(-.119,.064,4),(-.106,.071,4)], 12)
    drum('Pod front hub', x, 0, [(.120,.026,5),(.127,.023,5)], 8)
    drum('Pod rear hub', x, 0, [(-.127,.023,5),(-.120,.026,5)], 8)

# Thick, compact crown and keel give the central body a drone-like outline.
box('Top fin', (0,.220,0), (.135,.105,.16), 0, .025)
box('Top fin cap', (0,.270,0), (.085,.032,.12), 6, .012)
box('Lower fin', (0,-.210,0), (.12,.085,.15), 0, .022)

# Join and bake all geometry into a single flat-shaded mesh.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for poly in model.data.polygons:
    poly.use_smooth = False
# OBJ exporter performs its normal coordinate conversion with the requested axes.
bpy.ops.wm.obj_export(filepath=os.path.join(OUT_DIR, 'model.obj'),
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
scene.world = bpy.data.worlds.new('Blue gray studio')
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def point_at(obj, target=(0,0,0)):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, location, energy, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    obj.location = location
    point_at(obj)
    return obj

light('Upper left sun', 'SUN', (-3, 5, 5), 2.0)
light('Soft fill', 'AREA', (4, 1, 4), 180, 5.0)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 4)
point_at(cam)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.12
scene.render.filepath = os.path.join(OUT_DIR, 'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (.90, .61, 1.65)
point_at(cam)
cam_data.type = 'PERSP'
cam_data.lens = 52
scene.render.filepath = os.path.join(OUT_DIR, 'angle.png')
bpy.ops.render.render(write_still=True)

