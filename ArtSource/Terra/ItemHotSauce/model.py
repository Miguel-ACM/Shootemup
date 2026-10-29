import bpy
import bmesh
import math
import os
import sys
from mathutils import Vector

# The game uses Y for up and sees the +Z face as the front.
def script_directory():
    try:
        return os.path.dirname(os.path.abspath(__file__))
    except NameError:
        if '--' in sys.argv:
            args = sys.argv[sys.argv.index('--') + 1:]
            if args:
                return os.path.dirname(os.path.abspath(args[0]))
        return os.getcwd()

out_dir = script_directory()
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
    for datablock in list(datablocks):
        if datablock.users == 0:
            datablocks.remove(datablock)

palette = [
    (0.66, 0.19, 0.12, 1),  # sauce bottle, brick red
    (0.83, 0.29, 0.16, 1),  # glass shoulders
    (0.95, 0.40, 0.12, 1),  # orange cap
    (0.76, 0.25, 0.08, 1),  # cap lower lip
    (0.96, 0.84, 0.59, 1),  # cream paper
    (0.78, 0.43, 0.20, 1),  # label borders
    (0.76, 0.13, 0.08, 1),  # flame red
    (1.00, 0.53, 0.12, 1),  # flame orange
    (1.00, 0.77, 0.30, 1),  # flame heart
]
mats = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = color
    mat.use_nodes = True
    shader = mat.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value = color
    shader.inputs['Roughness'].default_value = 0.72
    mats.append(mat)

parts = []
def mesh_object(name, verts, faces, material):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mats[material])
    parts.append(obj)
    return obj

def octagon(x, z, cut):
    return [(-x + cut, -z), (x - cut, -z), (x, -z + cut),
            (x, z - cut), (x - cut, z), (-x + cut, z),
            (-x, z - cut), (-x, -z + cut)]

def rings(name, profiles, material):
    verts = []
    for y, x, z, cut in profiles:
        verts.extend((px, y, pz) for px, pz in octagon(x, z, cut))
    faces = [tuple(reversed(range(8)))]
    for j in range(len(profiles) - 1):
        for i in range(8):
            k = (i + 1) % 8
            faces.append((j * 8 + i, j * 8 + k, (j + 1) * 8 + k, (j + 1) * 8 + i))
    faces.append(tuple((len(profiles) - 1) * 8 + i for i in range(8)))
    return mesh_object(name, verts, faces, material)

def cylinder_y(name, y, radius, depth, material, vertices=16):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth,
                                       end_fill_type='NGON', location=(0, y, 0),
                                       rotation=(math.pi / 2, 0, 0))
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mats[material])
    parts.append(obj)
    return obj

def block(name, location, scale, material, angle=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    obj.rotation_euler[1] = angle
    obj.data.materials.append(mats[material])
    parts.append(obj)
    return obj

def emblem(name, points, z, depth, material):
    n = len(points)
    verts = [(x, y, z - depth / 2) for x, y in points] + [(x, y, z + depth / 2) for x, y in points]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2*n))]
    faces += [(i, (i+1)%n, (i+1)%n+n, i+n) for i in range(n)]
    return mesh_object(name, verts, faces, material)

# An octagonal bottle, with a broad foot, straight sides and tapered shoulders.
rings('Bottle', [(-.42,.165,.125,.035), (-.395,.193,.150,.040),
                 (-.35,.198,.154,.040), (.105,.198,.154,.040),
                 (.155,.185,.145,.038), (.265,.105,.093,.025),
                 (.295,.100,.090,.025)], 0)
rings('Shoulder collar', [(.265,.105,.093,.025), (.300,.105,.093,.025)], 1)
cylinder_y('Neck', .316, .095, .076, 1)
cylinder_y('Cap lower lip', .335, .123, .035, 3)
cylinder_y('Cap', .387, .119, .088, 2)
cylinder_y('Cap top bevel', .433, .109, .012, 2)
# Chunky shallow grip ribs survive at pickup size and wrap around the cap.
for i in range(12):
    a = 2 * math.pi * i / 12
    block('Cap grip', (.119 * math.sin(a), .388, .119 * math.cos(a)),
          (.022, .060, .014), 3, a)

# The paper sleeve follows the bottle around all four sides.
rings('Label', [(-.265,.201,.157,.040), (-.245,.202,.158,.040),
                (.025,.202,.158,.040), (.045,.201,.157,.040)], 4)
rings('Label lower stripe', [(-.265,.203,.159,.040), (-.250,.203,.159,.040)], 5)
rings('Label upper stripe', [(.030,.203,.159,.040), (.045,.203,.159,.040)], 5)

# An uncomplicated layered flame on front and back, large enough for the HUD.
outer = [(-.072,-.205), (-.103,-.157), (-.095,-.103), (-.053,-.049),
         (-.043,-.102), (.004,-.026), (.029,-.081), (.065,-.060),
         (.102,-.128), (.091,-.179), (.055,-.209), (0,-.220)]
inner = [(-.044,-.192), (-.058,-.151), (-.033,-.108), (-.018,-.140),
         (.010,-.089), (.044,-.140), (.053,-.171), (.031,-.201), (0,-.207)]
heart = [(-.019,-.194), (-.026,-.169), (.004,-.134), (.027,-.173), (.018,-.196), (0,-.202)]
for side in (-1, 1):
    emblem('Flame silhouette', outer, side*.162, .006, 6)
    emblem('Flame middle', inner, side*.167, .006, 7)
    emblem('Flame heart', heart, side*.172, .006, 8)

# Apply transforms and triangulate for predictable flat-shaded export.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.convert(target='MESH')
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
bm = bmesh.new()
bm.from_mesh(model.data)
bmesh.ops.triangulate(bm, faces=list(bm.faces))
bm.to_mesh(model.data)
bm.free()
model.data.update()
for face in model.data.polygons:
    face.use_smooth = False

bpy.ops.wm.obj_export(filepath=os.path.join(out_dir, 'model.obj'),
                      export_selected_objects=True, export_materials=True,
                      forward_axis='Y', up_axis='Z')

# Presentation setup is created after export, keeping the OBJ model-only.
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 512
scene.render.resolution_y = 512
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Blue gray world') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def point_at(obj, target=(0, 0, 0)):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, position, energy, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if kind == 'AREA':
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = position
    point_at(obj)
    return obj

sun = light('Upper left sun', 'SUN', (-3, 5, 6), 2.0)
sun.data.angle = math.radians(15)
light('Soft fill', 'AREA', (4, 1.5, 4), 35, 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 4)
point_at(cam)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.23
scene.render.filepath = os.path.join(out_dir, 'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (1.35, 0.85, 2.6)
point_at(cam)
cam_data.type = 'PERSP'
cam_data.lens = 50
scene.render.filepath = os.path.join(out_dir, 'angle.png')
bpy.ops.render.render(write_still=True)
