import bpy
import math
import os
import sys
from mathutils import Vector

# The item is modeled in the XY plane, with the field facing +Z.
if '__file__' in globals():
    OUT = os.path.dirname(os.path.abspath(__file__))
else:
    args = sys.argv
    if '--' not in args or args.index('--') + 1 >= len(args):
        raise RuntimeError('Pass the script path after -- when __file__ is unavailable')
    OUT = os.path.dirname(os.path.abspath(args[args.index('--') + 1]))

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for material in list(bpy.data.materials):
    bpy.data.materials.remove(material)

palette = [
    (0.16, 0.37, 0.51, 1),  # blue steel housing
    (0.26, 0.58, 0.73, 1),  # bevels
    (0.42, 0.80, 1.00, 1),  # original shield blue
    (0.22, 0.68, 0.88, 1),  # field facets
    (0.59, 0.91, 1.00, 1),  # field highlight
    (0.75, 0.94, 1.00, 1),  # emitter studs
    (0.30, 0.48, 0.56, 1),  # side hardware
]
mats = []
for i, color in enumerate(palette):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = color
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = 0.7
    mats.append(m)

parts = []
def mesh_part(name, verts, faces, indices):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.materials.clear()
    for m in mats:
        mesh.materials.append(m)
    mesh.update()
    ob = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(ob)
    for p, idx in zip(mesh.polygons, indices):
        p.material_index = idx
        p.use_smooth = False
    parts.append(ob)
    return ob

def layered_hex(name, layers, side_mats, cap_mat, sides=12):
    verts = []
    # Twelve sides retain the six dominant corners with a small corner chamfer.
    # Alternate radii form a chamfered hexagon, not a circular disc.
    for radius, z in layers:
        for i in range(sides):
            a = 2 * math.pi * i / sides + math.pi / 12
            r = radius if i % 2 == 0 else radius
            verts.append((r * math.cos(a), r * math.sin(a), z))
    faces, indices = [], []
    faces.append(tuple(reversed(range(sides))))
    indices.append(cap_mat)
    for j in range(len(layers)-1):
        for i in range(sides):
            faces.append((j*sides+i, j*sides+(i+1)%sides,
                          (j+1)*sides+(i+1)%sides, (j+1)*sides+i))
            indices.append(side_mats[j])
    faces.append(tuple((len(layers)-1)*sides+i for i in range(sides)))
    indices.append(cap_mat)
    return mesh_part(name, verts, faces, indices)

def dome(sign):
    # Faceted, solid field lens on each side of the emitter.
    n = 12
    rings = [(0.268, 0.105), (0.245, 0.165), (0.175, 0.225), (0.085, 0.257)]
    verts = []
    for r, z in rings:
        for i in range(n):
            a = 2*math.pi*i/n + math.pi/12
            verts.append((r*math.cos(a), r*math.sin(a), sign*z))
    verts.append((0, 0, sign*0.267))
    faces, ids = [], []
    for j in range(len(rings)-1):
        for i in range(n):
            f = (j*n+i, j*n+(i+1)%n, (j+1)*n+(i+1)%n, (j+1)*n+i)
            faces.append(f if sign > 0 else tuple(reversed(f)))
            ids.append(2 if (i+j)%3 else 3)
    for i in range(n):
        f = ((len(rings)-1)*n+i, (len(rings)-1)*n+(i+1)%n, len(verts)-1)
        faces.append(f if sign > 0 else tuple(reversed(f)))
        ids.append(4 if i % 3 == 0 else 2)
    mesh_part('Field lens', verts, faces, ids)

def bevel_box(name, loc, scale, material, bevel=0.025):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    ob = bpy.context.object
    ob.name = name
    ob.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    ob.data.materials.append(mats[material])
    mod = ob.modifiers.new('Machined edges', 'BEVEL')
    mod.width = bevel
    mod.segments = 1
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.modifier_apply(modifier=mod.name)
    parts.append(ob)
    return ob

# A thick, stepped, low-poly emitter with a bright field on both faces.
layered_hex('Emitter housing', [(0.355,-0.105), (0.385,-0.075),
    (0.385,0.075), (0.355,0.105)], [1,0,1], 0, sides=12)
layered_hex('Front bezel', [(0.315,0.103),(0.305,0.128)], [1], 1, sides=12)
layered_hex('Rear bezel', [(0.305,-0.128),(0.315,-0.103)], [1], 1, sides=12)
dome(1)
dome(-1)

# Six field nodes are large enough to remain legible as the item rotates.
for side in (-1, 1):
    for i in range(6):
        a = 2*math.pi*i/6 + math.pi/6
        x, y = 0.326*math.cos(a), 0.326*math.sin(a)
        bpy.ops.mesh.primitive_uv_sphere_add(segments=8, ring_count=4,
            location=(x,y,side*0.134))
        ob = bpy.context.object
        ob.name = 'Field node'
        ob.scale = (0.046,0.046,0.035)
        ob.data.materials.append(mats[5])
        parts.append(ob)

# Chunky opposed side grips make the emitter's outline distinctive edge-on.
for s in (-1, 1):
    bevel_box('Side clamp', (s*0.385,0,0), (0.095,0.30,0.22), 1, 0.025)
    bevel_box('Clamp inset', (s*0.438,0,0), (0.024,0.17,0.13), 6, 0.012)

bpy.ops.object.select_all(action='DESELECT')
for ob in parts:
    ob.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
model.scale.y = 1.1
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for p in model.data.polygons:
    p.use_smooth = False

bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'),
    export_selected_objects=True, export_materials=True,
    forward_axis='Y', up_axis='Z')

# Studio setup is added only after export.
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 512
scene.render.resolution_y = 512
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world.color = (0.05, 0.07, 0.09)
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.05,0.07,0.09,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(ob, point=(0,0,0)):
    ob.rotation_euler = (Vector(point)-ob.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, loc, energy, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    obj.location = loc
    aim(obj)
    return obj

light('Upper left sun', 'SUN', (-3,4,6), 2.0)
light('Soft fill', 'AREA', (4,1,4), 120, 5.0)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
cam.location = (0,0,3)
aim(cam)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.08
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam_data.lens = 50
cam.location = (0.87,0.55,1.50)
aim(cam)
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

