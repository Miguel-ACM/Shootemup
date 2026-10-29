import bpy
import math
import os
import sys
from mathutils import Vector

# The item uses Y as its vertical axis and +Z as its front.
def output_dir():
    if '__file__' in globals():
        return os.path.dirname(os.path.abspath(__file__))
    if '--' in sys.argv:
        args = sys.argv[sys.argv.index('--') + 1:]
        if args:
            path = os.path.abspath(args[0])
            return path if os.path.isdir(path) else os.path.dirname(path)
    return os.getcwd()

OUT = output_dir()
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

palette = [
    (0.19, 0.46, 0.82, 1),  # blue enamel
    (0.31, 0.66, 1.00, 1),  # blue shoulders
    (0.48, 0.74, 0.97, 1),  # pale metal
    (0.25, 0.39, 0.53, 1),  # blue steel
    (0.95, 0.72, 0.27, 1),  # warm brass
    (0.94, 0.94, 0.79, 1),  # clock face
    (0.19, 0.36, 0.52, 1),  # clock hands
]
mats = []
for i, rgba in enumerate(palette):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = rgba
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = rgba
    bsdf.inputs['Roughness'].default_value = 0.72
    mats.append(m)

parts = []
def mesh_part(name, verts, faces, material_ids):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.materials.clear()
    for mat in mats:
        mesh.materials.append(mat)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    for poly, mat_id in zip(mesh.polygons, material_ids):
        poly.material_index = mat_id
        poly.use_smooth = False
    parts.append(obj)
    return obj

def lathe_y(name, x, profile, segment_materials, n=12):
    # A closed, stepped profile creates actual bevels and shoulders.
    verts = []
    for y, radius in profile:
        for i in range(n):
            a = 2 * math.pi * i / n + math.pi / n
            verts.append((x + radius * math.cos(a), y, radius * math.sin(a)))
    faces = [tuple(reversed(range(n)))]
    ids = [segment_materials[0]]
    for j in range(len(profile) - 1):
        for i in range(n):
            k = (i + 1) % n
            faces.append((j*n+i, j*n+k, (j+1)*n+k, (j+1)*n+i))
            ids.append(segment_materials[j])
    faces.append(tuple((len(profile)-1)*n+i for i in range(n)))
    ids.append(segment_materials[-1])
    return mesh_part(name, verts, faces, ids)

def box(name, location, scale, mat, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mats[mat])
    if bevel:
        mod = obj.modifiers.new('Machined corners', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    parts.append(obj)
    return obj

def disc(name, center, radius, depth, mat, n=12):
    bpy.ops.mesh.primitive_cylinder_add(vertices=n, radius=radius, depth=depth, end_fill_type='NGON', location=center)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mats[mat])
    parts.append(obj)
    return obj

# Twin thick, round cans, with integrated tapered end caps and wide retaining rings.
profile = [(-.405,.125), (-.390,.160), (-.345,.170), (-.320,.155),
           (.260,.155), (.305,.170), (.350,.160), (.370,.125)]
for x in (-.205, .205):
    lathe_y('Capacitor can', x, profile, [2,3,2,0,1,3,2])
    # Top insulator and raised connector post remain visible from all sides.
    lathe_y('Terminal', x, [(.365,.082), (.385,.085), (.405,.055), (.435,.055)], [4,4,2], 8)

# The bridge and foot unify the two cans without concealing their separated silhouette.
box('Bus bridge', (0, .424, 0), (.49, .052, .105), 2, .014)
box('Lower mounting saddle', (0, -.365, 0), (.48, .075, .22), 3, .018)

# A clock on each face remains legible throughout rotation. Large hands are geometry.
for side in (-1, 1):
    disc('Clock bezel', (0, -.025, side*.181), .155, .065, 4)
    disc('Clock face', (0, -.025, side*.222), .120, .025, 5)
    z = side*.240
    box('Clock hour hand', (0, .010, z), (.026, .083, .014), 6)
    box('Clock minute hand', (.039, -.025, z), (.095, .026, .014), 6)
    disc('Clock center', (0, -.025, side*.250), .023, .016, 6, 8)

# Join and triangulate explicitly so the OBJ's polygon budget is deterministic.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
tri = model.modifiers.new('Triangulate', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
for face in model.data.polygons:
    face.use_smooth = False
assert 300 <= len(model.data.polygons) <= 2000, 'Triangle budget exceeded'

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
scene.world = bpy.data.worlds.new('Blue gray studio')
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def point_at(obj, target=(0, 0, 0)):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, loc, energy, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if kind == 'AREA':
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    obj.location = loc
    point_at(obj)
    return obj

sun = light('Upper left sun', 'SUN', (-3, 5, 6), 2.0)
sun.data.angle = math.radians(18)
light('Soft fill', 'AREA', (4, 1, 5), 150, 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
scene.collection.objects.link(cam)
scene.camera = cam

cam.location = (0, 0, 4)
point_at(cam)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.16
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)

cam.location = (1.65, 1.05, 2.7)
point_at(cam)
cam_data.type = 'PERSP'
cam_data.lens = 95
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

